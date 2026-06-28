using System.Text;
using System.Text.Json;
using AuditLib.Abstractions;
using AuditLib.Domain;
using AuditLib.Options;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Metadata;

namespace AuditLib.Infrastructure;

public class AuditInterceptor : SaveChangesInterceptor
{
    private readonly IUserContext? _userContext;
    private readonly AuditLibOptions _options;

    public AuditInterceptor(AuditLibOptions options, IUserContext? userContext = null)
    {
        _options = options;
        _userContext = userContext;
    }

    public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
    {
        HandleAudit(eventData.Context).Wait();
        return base.SavingChanges(eventData, result);
    }

    public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        await HandleAudit(eventData.Context);
        return await base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private async Task HandleAudit(DbContext? context)
    {
        if (context == null) return;

        context.ChangeTracker.DetectChanges();

        var allEntries = context.ChangeTracker
            .Entries<IAuditEntity>()
            .ToList();

        var now = _options.TimestampProvider();

        // Capture deleted entries BEFORE soft-delete changes their state
        var deletedRoots = allEntries
            .Where(e => e.State == EntityState.Deleted)
            .Select(e => (EntityEntry)e)
            .Where(e => _options.EntitySelector(e.Metadata.ClrType))
            .Where(e => e.Metadata.ClrType != typeof(AuditLog))
            .Where(e => ShouldTrack(e.State))
            .ToList();

        foreach (var entry in allEntries)
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    entry.Entity.CreatedAt = now;
                    break;
                case EntityState.Modified:
                    entry.Entity.UpdatedAt = now;
                    break;
                case EntityState.Deleted:
                    if (_options.SoftDeleteEnabled)
                        await SoftDeleteHandler.HandleDeleteAsync(context, entry, now, _options);
                    break;
            }
        }

        var usuarioId = _options.UserIdResolver(_userContext);

        var explicitRoots = context.ChangeTracker
            .Entries<IAuditEntity>()
            .Where(e => e.State is EntityState.Added or EntityState.Modified)
            .Select(e => (Entry: (EntityEntry)e, OriginalState: e.State))
            .Where(e => _options.EntitySelector(e.Entry.Metadata.ClrType))
            .Where(e => e.Entry.Metadata.ClrType != typeof(AuditLog))
            .Where(e => ShouldTrack(e.Entry.State))
            .ToList();

        var allRoots = explicitRoots
            .Concat(deletedRoots.Select(e => (Entry: e, OriginalState: EntityState.Deleted)))
            .ToList();

        if (_options.TrackUnchangedRootsWithChangedChildren)
        {
            var unchangedRootsWithChangedChildren = context.ChangeTracker
                .Entries<IAuditEntity>()
                .Where(e => e.State == EntityState.Unchanged)
                .Select(e => (Entry: (EntityEntry)e, OriginalState: e.State))
                .Where(e => _options.EntitySelector(e.Entry.Metadata.ClrType))
                .Where(e => e.Entry.Metadata.ClrType != typeof(AuditLog))
                .Where(e => HasChangedChildren(context, e.Entry))
                .ToList();

            allRoots.AddRange(unchangedRootsWithChangedChildren);
        }

        if (allRoots.Count == 0) return;

        var loggedAggregateRoots = new HashSet<object>();

        foreach (var (root, originalState) in allRoots)
        {
            var (resolvedRoot, _) = ResolveAggregateRoot(context, root);

            // Skip unchanged roots that were already logged via an aggregate child
            if (!(originalState is EntityState.Added or EntityState.Modified or EntityState.Deleted)
                && loggedAggregateRoots.Contains(resolvedRoot.Entity))
                continue;

            var children = GetChildren(context, root);
            var isExplicitlyChanged = originalState is EntityState.Added or EntityState.Modified or EntityState.Deleted;
            var effectiveState = isExplicitlyChanged ? originalState : (EntityState?)EntityState.Modified;
            var auditRecord = BuildAuditRecord(context, root, children, usuarioId, effectiveState);
            if (auditRecord == null)
                continue;

            context.Set<AuditLog>().Add(auditRecord);

            if (resolvedRoot != root)
                loggedAggregateRoots.Add(resolvedRoot.Entity);
        }
    }

    private bool ShouldTrack(EntityState state) => state switch
    {
        EntityState.Added => _options.TrackAddedEntities,
        EntityState.Modified => _options.TrackModifiedEntities,
        EntityState.Deleted => _options.TrackDeletedEntities,
        _ => false
    };

    private static bool HasChangedChildren(DbContext context, EntityEntry root)
        => GetChildren(context, root).Count != 0;

    private static List<EntityEntry> GetChildren(DbContext context, EntityEntry root)
    {
        var allEntries = context.ChangeTracker.Entries().ToList();
        var rootPkValues = root.Metadata.FindPrimaryKey()!.Properties
            .Select(p => root.Property(p.Name).CurrentValue)
            .ToList();

        var childEntries = new List<EntityEntry>();

        // Include owned types via navigation
        foreach (var nav in root.Metadata.GetNavigations().Where(n => n.TargetEntityType.IsOwned()))
        {
            childEntries.AddRange(allEntries
                .Where(e => e.Metadata.ClrType == nav.TargetEntityType.ClrType && e.Metadata.IsOwned())
                .Where(e => e.State is EntityState.Added or EntityState.Modified or EntityState.Deleted));
        }

        // Include FK-related children
        foreach (var candidate in allEntries)
        {
            if (candidate.Entity == root.Entity)
                continue;

            if (candidate.State is not (EntityState.Added or EntityState.Modified or EntityState.Deleted))
                continue;

            if (candidate.Metadata.IsOwned())
                continue;

            if (candidate.Metadata.ClrType == typeof(AuditLog))
                continue;

            foreach (var fk in candidate.Metadata.GetForeignKeys())
            {
                if (fk.PrincipalEntityType.ClrType != root.Metadata.ClrType)
                    continue;

                if (fk.IsOwnership)
                    continue;

                var matches = true;
                for (var i = 0; i < fk.Properties.Count; i++)
                {
                    var candidateValue = candidate.Property(fk.Properties[i].Name).CurrentValue
                                        ?? candidate.Property(fk.Properties[i].Name).OriginalValue;
                    if (!Equals(candidateValue, rootPkValues[i]))
                    {
                        matches = false;
                        break;
                    }
                }

                if (matches)
                {
                    childEntries.Add(candidate);
                    break;
                }
            }
        }

        childEntries.AddRange(GetChangedReferences(root, allEntries));
        return childEntries.Distinct().ToList();
    }

    private static IEnumerable<EntityEntry> GetChangedReferences(EntityEntry root, List<EntityEntry> allEntries)
    {
        foreach (var nav in root.Metadata.GetNavigations().Where(nav => !nav.IsCollection))
        {
            if (nav.TargetEntityType.IsOwned())
                continue;

            var fk = nav.ForeignKey;

            if (fk.DeclaringEntityType.ClrType != root.Metadata.ClrType)
                continue;

            foreach (var candidate in allEntries.Where(e =>
                         e.Metadata.ClrType == nav.TargetEntityType.ClrType &&
                         e.State is EntityState.Added or EntityState.Modified or EntityState.Deleted))
            {
                if (BelongsToReference(root, candidate, fk))
                    yield return candidate;
            }
        }
    }

    private static bool BelongsToReference(EntityEntry root, EntityEntry reference, IForeignKey foreignKey)
    {
        for (var index = 0; index < foreignKey.Properties.Count; index++)
        {
            var dependentProperty = foreignKey.Properties[index];
            var principalProperty = foreignKey.PrincipalKey.Properties[index];

            var rootValue = root.Property(dependentProperty.Name).CurrentValue
                            ?? root.Property(dependentProperty.Name).OriginalValue;
            var referenceValue = reference.Property(principalProperty.Name).CurrentValue
                                 ?? reference.Property(principalProperty.Name).OriginalValue;

            if (!Equals(rootValue, referenceValue))
                return false;
        }

        return true;
    }

    private static bool BelongsToRoot(EntityEntry root, EntityEntry child, IForeignKey foreignKey)
    {
        for (var index = 0; index < foreignKey.Properties.Count; index++)
        {
            var childProperty = foreignKey.Properties[index];
            var principalProperty = foreignKey.PrincipalKey.Properties[index];

            var childValue = child.Property(childProperty.Name).CurrentValue
                             ?? child.Property(childProperty.Name).OriginalValue;
            var rootValue = root.Property(principalProperty.Name).CurrentValue
                            ?? root.Property(principalProperty.Name).OriginalValue;

            if (!Equals(childValue, rootValue))
                return false;
        }

        return true;
    }

    private AuditLog? BuildAuditRecord(
        DbContext context,
        EntityEntry entry,
        List<EntityEntry> children,
        Guid? usuarioId,
        EntityState? effectiveState = null)
    {
        var state = effectiveState ?? entry.State;

        var acao = state switch
        {
            EntityState.Added => _options.ActionAdded,
            EntityState.Modified => _options.ActionModified,
            EntityState.Deleted => _options.ActionDeleted,
            _ => null
        };

        if (acao == null) return null;

        var (effectiveEntry, effectiveClrType) = ResolveAggregateRoot(context, entry);
        var isAggregate = effectiveEntry != entry;

        var chavePrimaria = string.Join(",", effectiveEntry.Metadata.FindPrimaryKey()!.Properties
            .Select(p => effectiveEntry.Property(p.Name).CurrentValue?.ToString()));

        var entityName = _options.AggregateRootDisplayFormatter != null && isAggregate
            ? _options.AggregateRootDisplayFormatter(effectiveEntry, entry, ResolveDisplayName(effectiveClrType.Name))
            : ResolveDisplayName(effectiveClrType.Name);

        var snapshotEntry = isAggregate && _options.AggregateSnapshot == AggregateSnapshotMode.FullRoot
            ? effectiveEntry
            : entry;

        var snapshotChildren = isAggregate && _options.AggregateSnapshot == AggregateSnapshotMode.FullRoot
            ? GetChildren(context, effectiveEntry)
            : children;

        var estadoAnterior = state switch
        {
            EntityState.Modified => SerializeSnapshot(context, snapshotEntry, useOriginal: true, snapshotChildren, useBeforeState: true),
            EntityState.Deleted => SerializeSnapshot(context, snapshotEntry, useOriginal: true, snapshotChildren, useBeforeState: true),
            _ => null
        };

        var estadoAtual = state switch
        {
            EntityState.Added => SerializeSnapshot(context, snapshotEntry, useOriginal: false, snapshotChildren, useBeforeState: false),
            EntityState.Modified => SerializeSnapshot(context, snapshotEntry, useOriginal: false, snapshotChildren, useBeforeState: false),
            _ => null
        };

        var diff = BuildDiff(context, snapshotEntry, snapshotChildren, effectiveState: effectiveState);

        if (state == EntityState.Modified && diff == null)
            return null;

        var aggregateLog = AuditLog.Create(
            entityName,
            chavePrimaria,
            acao,
            usuarioId,
            _options.TimestampProvider(),
            estadoAnterior,
            estadoAtual,
            diff
        );

        if (_options.DualAuditForAggregates && isAggregate)
        {
            var childPk = string.Join(",", entry.Metadata.FindPrimaryKey()!.Properties
                .Select(p => entry.Property(p.Name).CurrentValue?.ToString()));
            var childName = ResolveDisplayName(entry.Metadata.ClrType.Name);
            var childDiff = BuildDiff(context, entry, [], effectiveState: effectiveState);

            if (state != EntityState.Modified || childDiff != null)
            {
                var childLog = AuditLog.Create(
                    childName,
                    childPk,
                    acao,
                    usuarioId,
                    _options.TimestampProvider(),
                    estadoAnterior,
                    estadoAtual,
                    childDiff
                );
                context.Set<AuditLog>().Add(childLog);
            }
        }

        return aggregateLog;
    }

    private string? SerializeSnapshot(
        DbContext context,
        EntityEntry entry,
        bool useOriginal,
        List<EntityEntry> children,
        bool useBeforeState)
    {
        var snapshot = new Dictionary<string, object?>();

        foreach (var prop in entry.Properties
            .Where(p => !_options.IgnoredProperties.Contains(p.Metadata.Name) &&
                        !p.Metadata.IsPrimaryKey() &&
                        !p.Metadata.IsForeignKey()))
        {
            var value = useOriginal ? prop.OriginalValue : prop.CurrentValue;
            if (value is not null || !useOriginal)
                snapshot[prop.Metadata.Name] = value;
        }

        foreach (var fk in entry.Metadata.GetForeignKeys())
        {
            foreach (var fkProp in fk.Properties)
            {
                if (_options.IgnoredProperties.Contains(fkProp.Name)) continue;

                var fkValue = useOriginal
                    ? entry.Property(fkProp.Name).OriginalValue
                    : entry.Property(fkProp.Name).CurrentValue;

                if (fkValue is null && useOriginal)
                    continue;

                var navName = fk.DependentToPrincipal?.Name ?? fkProp.Name;

                if (fkValue is not null)
                {
                    var (id, nome) = _options.FkNameResolver(context, entry, fk, fkValue);
                    if (nome is not null)
                    {
                        snapshot[navName] = new Dictionary<string, object?>
                        {
                            ["id"] = id,
                            ["nome"] = nome
                        };
                    }
                    else
                    {
                        snapshot[navName] = fkValue;
                    }
                }
                else
                {
                    snapshot[navName] = null;
                }
            }
        }

        foreach (var nav in entry.Metadata.GetNavigations().Where(n => n.TargetEntityType.IsOwned()))
        {
            var targetType = nav.TargetEntityType.ClrType;

            EntityEntry? ownedEntry;
            if (useOriginal)
                ownedEntry = children.FirstOrDefault(c =>
                    c.Metadata.ClrType == targetType && c.Metadata.IsOwned() && c.State == EntityState.Deleted)
                             ?? children.FirstOrDefault(c =>
                                 c.Metadata.ClrType == targetType && c.Metadata.IsOwned());
            else
                ownedEntry = children.FirstOrDefault(c =>
                    c.Metadata.ClrType == targetType && c.Metadata.IsOwned() && c.State == EntityState.Added)
                             ?? children.FirstOrDefault(c =>
                                 c.Metadata.ClrType == targetType && c.Metadata.IsOwned());

            if (ownedEntry == null) continue;

            var ownedSnapshot = SerializeOwnedSnapshot(ownedEntry, useOriginal);
            if (ownedSnapshot.Count > 0)
                snapshot[nav.Name] = ownedSnapshot;
        }

        var childrenByNav = GroupChildrenByNavigation(entry, children, useBeforeState);
        foreach (var (navName, items) in childrenByNav)
        {
            snapshot[navName] = items;
        }

        return JsonSerializer.Serialize(snapshot, _options.JsonSerializerOptions);
    }

    private Dictionary<string, object?> SerializeOwnedSnapshot(EntityEntry ownedEntry, bool useOriginal)
    {
        var snapshot = new Dictionary<string, object?>();

        foreach (var prop in ownedEntry.Properties)
        {
            if (_options.IgnoredProperties.Contains(prop.Metadata.Name)) continue;
            if (prop.Metadata.IsShadowProperty()) continue;
            if (prop.Metadata.IsPrimaryKey()) continue;
            if (prop.Metadata.IsForeignKey()) continue;

            var value = useOriginal ? prop.OriginalValue : prop.CurrentValue;
            if (value is not null || !useOriginal)
                snapshot[prop.Metadata.Name] = value;
        }

        foreach (var nav in ownedEntry.Metadata.GetNavigations().Where(n => n.TargetEntityType.IsOwned()))
        {
            var nestedEntry = ownedEntry.References.FirstOrDefault(r =>
                r.Metadata.TargetEntityType.ClrType == nav.TargetEntityType.ClrType);

            if (nestedEntry?.TargetEntry is not { } target) continue;

            var nestedSnapshot = SerializeOwnedSnapshot(target, useOriginal);
            if (nestedSnapshot.Count > 0)
                snapshot[nav.Name] = nestedSnapshot;
        }

        return snapshot;
    }

    private Dictionary<string, List<Dictionary<string, object?>>> GroupChildrenByNavigation(
        EntityEntry root,
        List<EntityEntry> children,
        bool useBeforeState)
    {
        var result = new Dictionary<string, List<Dictionary<string, object?>>>();

        var added = children.Where(child => child.State == EntityState.Added).ToList();
        var removed = children.Where(child => child.State == EntityState.Deleted).ToList();

        DiscardEquivalentPairsFromSameNavigation(root, added, removed);

        var childrenToSkip = new HashSet<EntityEntry>(children.Except(added).Except(removed)
            .Where(child => child.State is EntityState.Added or EntityState.Deleted));

        foreach (var child in children)
        {
            if (childrenToSkip.Contains(child))
                continue;

            if (useBeforeState && child.State == EntityState.Added)
                continue;
            if (!useBeforeState && child.State == EntityState.Deleted)
                continue;

            var navName = DiscoverNavigation(root, child);
            if (navName == null) continue;

            if (!result.ContainsKey(navName))
                result[navName] = [];

            var serialized = SerializeChild(child, useBeforeState);
            result[navName].Add(serialized);
        }

        return result;
    }

    private static string? DiscoverNavigation(EntityEntry root, EntityEntry child)
    {
        foreach (var nav in root.Metadata.GetNavigations())
        {
            if (!nav.IsCollection) continue;
            if (nav.TargetEntityType.ClrType == child.Metadata.ClrType)
                return nav.Name;
        }

        foreach (var skipNav in root.Metadata.GetSkipNavigations())
        {
            var foreignKeyTypes = child.Metadata.GetForeignKeys()
                .Select(fk => fk.PrincipalEntityType.ClrType)
                .ToHashSet();

            if (foreignKeyTypes.Contains(root.Metadata.ClrType)
                && foreignKeyTypes.Contains(skipNav.TargetEntityType.ClrType))
            {
                return skipNav.Name;
            }
        }

        return null;
    }

    private static string? DiscoverReferenceNavigation(EntityEntry root, EntityEntry child)
    {
        foreach (var nav in root.Metadata.GetNavigations())
        {
            if (nav.IsCollection) continue;
            if (nav.TargetEntityType.ClrType == child.Metadata.ClrType)
                return nav.Name;
        }

        return null;
    }

    private Dictionary<string, object?> SerializeChild(EntityEntry child, bool useOriginal)
    {
        var dict = new Dictionary<string, object?>();

        foreach (var prop in child.Properties)
        {
            if (prop.Metadata.IsShadowProperty()) continue;
            if (_options.NavigationBackReferences.Contains(prop.Metadata.Name)) continue;
            if (_options.IgnoredProperties.Contains(prop.Metadata.Name)) continue;

            dict[prop.Metadata.Name] = useOriginal ? prop.OriginalValue : prop.CurrentValue;
        }

        return dict;
    }

    private static Dictionary<string, object?> GetPersistedChildValues(EntityEntry root, EntityEntry child, bool useOriginal)
    {
        var valores = new Dictionary<string, object?>();
        var foreignKeysParaRaiz = child.Metadata.GetForeignKeys()
            .Where(fk => fk.PrincipalEntityType.ClrType == root.Metadata.ClrType)
            .SelectMany(fk => fk.Properties)
            .Select(prop => prop.Name)
            .ToHashSet();

        foreach (var prop in child.Properties.OrderBy(prop => prop.Metadata.Name))
        {
            if (prop.Metadata.IsShadowProperty()) continue;
            if (foreignKeysParaRaiz.Contains(prop.Metadata.Name)) continue;

            valores[prop.Metadata.Name] = useOriginal ? prop.OriginalValue : prop.CurrentValue;
        }

        return valores;
    }

    private static bool ChildrenAreEquivalent(EntityEntry root, EntityEntry removedChild, EntityEntry addedChild, AuditLibOptions options)
    {
        if (removedChild.Metadata.ClrType != addedChild.Metadata.ClrType)
            return false;

        var removedValues = GetPersistedChildValues(root, removedChild, useOriginal: true);
        var addedValues = GetPersistedChildValues(root, addedChild, useOriginal: false);

        if (removedValues.Count != addedValues.Count)
            return false;

        foreach (var (propertyName, removedValue) in removedValues)
        {
            if (!addedValues.TryGetValue(propertyName, out var addedValue))
                return false;

            if (!AreValuesEqual(removedValue, addedValue, options))
                return false;
        }

        return true;
    }

    private void DiscardEquivalentPairsFromSameNavigation(EntityEntry root, List<EntityEntry> added, List<EntityEntry> removed)
    {
        var addedToDiscard = new HashSet<EntityEntry>();
        var removedToDiscard = new HashSet<EntityEntry>();

        foreach (var removedGroup in removed.GroupBy(child => DiscoverNavigation(root, child) ?? "Desconhecido"))
        {
            var addedCandidates = added
                .Where(child => (DiscoverNavigation(root, child) ?? "Desconhecido") == removedGroup.Key)
                .Except(addedToDiscard)
                .ToList();

            foreach (var removedChild in removedGroup)
            {
                var match = addedCandidates.FirstOrDefault(addedChild => ChildrenAreEquivalent(root, removedChild, addedChild, _options));
                if (match == null)
                    continue;

                removedToDiscard.Add(removedChild);
                addedToDiscard.Add(match);
                addedCandidates.Remove(match);
            }
        }

        added.RemoveAll(addedToDiscard.Contains);
        removed.RemoveAll(removedToDiscard.Contains);
    }

    private string? BuildDiff(DbContext context, EntityEntry entry, List<EntityEntry> children, EntityState? effectiveState = null)
    {
        var lines = new List<string>();
        AppendDiffLines(context, entry, children, lines, effectiveState);

        return lines.Count == 0 ? null : string.Join("\n", lines);
    }

    private void AppendDiffLines(
        DbContext context,
        EntityEntry entry,
        List<EntityEntry> children,
        List<string> lines,
        EntityState? effectiveState = null,
        string? prefix = null)
    {
        var state = effectiveState ?? entry.State;

        if (state == EntityState.Added)
        {
            lines.Add(string.IsNullOrWhiteSpace(prefix) ? _options.DiffAddedMessage : $"{prefix}{_options.DiffAddedMessage}");
            return;
        }

        if (state == EntityState.Deleted)
        {
            lines.Add(string.IsNullOrWhiteSpace(prefix) ? _options.DiffDeletedMessage : $"{prefix}{_options.DiffDeletedMessage}");
            return;
        }

        var hasChanges = false;

        foreach (var prop in entry.Properties)
        {
            if (_options.IgnoredProperties.Contains(prop.Metadata.Name))
                continue;

            var original = prop.OriginalValue;
            var current = prop.CurrentValue;

            if (AreValuesEqual(original, current, _options))
                continue;

            hasChanges = true;

            var fk = entry.Metadata.GetForeignKeys()
                .FirstOrDefault(f => f.Properties.Any(p => p.Name == prop.Metadata.Name));

            if (fk != null)
            {
                var navName = ComposeName(prefix, ResolveDisplayName(fk.DependentToPrincipal?.Name ?? prop.Metadata.Name));
                var (_, oldNome) = _options.FkNameResolver(context, entry, fk, original);
                var (_, newNome) = _options.FkNameResolver(context, entry, fk, current);

                var oldVal = oldNome != null
                    ? $"{FormatDiffValue(original)} ({oldNome})"
                    : FormatDiffValue(original);
                var newVal = newNome != null
                    ? $"{FormatDiffValue(current)} ({newNome})"
                    : FormatDiffValue(current);
                lines.Add($"  - {navName}: {oldVal} -> {newVal}");
            }
            else
            {
                lines.Add($"  - {ComposeName(prefix, ResolvePropertyName(entry.Metadata.ClrType.Name, prop.Metadata.Name))}: {FormatDiffValue(original)} -> {FormatDiffValue(current)}");
            }
        }

        var entityChildren = new List<EntityEntry>();
        var ownedDeleted = new List<EntityEntry>();
        var ownedAdded = new List<EntityEntry>();
        var referenceModified = new List<(string NavName, EntityEntry Entry)>();

        foreach (var child in children)
        {
            var refNav = DiscoverReferenceNavigation(entry, child);
            if (refNav != null)
            {
                if (child.Metadata.IsOwned())
                {
                    if (child.State == EntityState.Deleted)
                        ownedDeleted.Add(child);
                    else if (child.State == EntityState.Added)
                        ownedAdded.Add(child);
                }
                else if (child.State == EntityState.Modified)
                {
                    referenceModified.Add((refNav, child));
                }

                continue;
            }

            entityChildren.Add(child);
        }

        foreach (var ownedType in ownedDeleted.Select(o => o.Metadata.ClrType)
                     .Union(ownedAdded.Select(o => o.Metadata.ClrType)).Distinct())
        {
            var navName = ResolveDisplayName(
                entry.Metadata.GetNavigations()
                    .FirstOrDefault(n => !n.IsCollection && n.TargetEntityType.ClrType == ownedType)
                    ?.Name ?? ownedType.Name);

            var deletedEntry = ownedDeleted.FirstOrDefault(o => o.Metadata.ClrType == ownedType);
            var addedEntry = ownedAdded.FirstOrDefault(o => o.Metadata.ClrType == ownedType);

            if (deletedEntry == null && addedEntry == null) continue;

            var propNames = (deletedEntry?.Properties ?? addedEntry!.Properties)
                .Where(p => !_options.IgnoredProperties.Contains(p.Metadata.Name) && !p.Metadata.IsShadowProperty())
                .Select(p => p.Metadata.Name)
                .Distinct();

            foreach (var propName in propNames)
            {
                var oldVal = deletedEntry?.Property(propName).OriginalValue ?? deletedEntry?.Property(propName).CurrentValue;
                var newVal = addedEntry?.Property(propName).CurrentValue;

                if (AreValuesEqual(oldVal, newVal, _options))
                    continue;

                hasChanges = true;
                lines.Add($"  - {ComposeName(prefix, navName)}.{ResolvePropertyName(ownedType.Name, propName)}: {FormatDiffValue(oldVal)} -> {FormatDiffValue(newVal)}");
            }
        }

        foreach (var (navName, referenceEntry) in referenceModified)
        {
            var referenceLines = new List<string>();
            AppendDiffLines(context, referenceEntry, GetChildren(context, referenceEntry), referenceLines, prefix: ComposeName(prefix, ResolveDisplayName(navName)));

            if (referenceLines.Count == 0)
                continue;

            hasChanges = true;
            lines.AddRange(referenceLines);
        }

        var added = entityChildren.Where(c => c.State == EntityState.Added).ToList();
        var removed = entityChildren.Where(c => c.State == EntityState.Deleted).ToList();
        var modified = entityChildren.Where(c => c.State == EntityState.Modified).ToList();

        DiscardEquivalentPairsFromSameNavigation(entry, added, removed);

        if (added.Count == 0 && removed.Count == 0 && modified.Count == 0)
            return;

        if (hasChanges || modified.Count > 0)
            lines.Add("");

        var removedByNav = removed.GroupBy(c => ComposeName(prefix, ResolveDisplayName(DiscoverNavigation(entry, c) ?? "Desconhecido")));
        var addedByNav = added.GroupBy(c => ComposeName(prefix, ResolveDisplayName(DiscoverNavigation(entry, c) ?? "Desconhecido")));
        var modifiedByNav = modified.GroupBy(c => ComposeName(prefix, ResolveDisplayName(DiscoverNavigation(entry, c) ?? "Desconhecido")));

        foreach (var group in removedByNav)
        {
            lines.Add(string.Format(_options.DiffRemovedFormat, group.Key));
            foreach (var child in group)
                lines.Add($"  - {DescribeChild(entry, child, context)}");
        }

        foreach (var group in addedByNav)
        {
            lines.Add(string.Format(_options.DiffAddedFormat, group.Key));
            foreach (var child in group)
                lines.Add($"  - {DescribeChild(entry, child, context)}");
        }

        foreach (var group in modifiedByNav)
        {
            foreach (var child in group)
            {
                var childDiff = BuildDiff(context, child, [], effectiveState: EntityState.Modified);
                if (childDiff != null)
                {
                    lines.Add($"  // {group.Key} alterado:");
                    foreach (var line in childDiff.Split('\n'))
                        lines.Add($"  {line}");
                }
            }
        }
    }

    private static string ComposeName(string? prefix, string nome)
        => string.IsNullOrWhiteSpace(prefix) ? nome : $"{prefix}.{nome}";

    private string DescribeChild(EntityEntry root, EntityEntry child, DbContext context)
    {
        var parts = new List<string>();

        foreach (var fk in child.Metadata.GetForeignKeys())
        {
            if (fk.PrincipalEntityType.ClrType == root.Metadata.ClrType)
                continue;

            foreach (var fkProp in fk.Properties)
            {
                if (_options.IgnoredProperties.Contains(fkProp.Name)) continue;

                var fkValue = child.Property(fkProp.Name).CurrentValue;
                if (fkValue is null) continue;

                var (_, nome) = _options.FkNameResolver(context, child, fk, fkValue);
                if (string.IsNullOrWhiteSpace(nome))
                    continue;

                var navName = fk.DependentToPrincipal?.Name ?? fk.PrincipalEntityType.ClrType.Name;
                parts.Add($"{ResolveDisplayName(navName)}: {nome}");
            }
        }

        foreach (var prop in child.Properties)
        {
            if (prop.Metadata.IsShadowProperty()) continue;
            if (prop.Metadata.IsPrimaryKey()) continue;
            if (_options.NavigationBackReferences.Contains(prop.Metadata.Name)) continue;
            if (_options.IgnoredProperties.Contains(prop.Metadata.Name)) continue;

            var isForeignKeyProperty = child.Metadata.GetForeignKeys()
                .SelectMany(fk => fk.Properties)
                .Any(fkProp => fkProp.Name == prop.Metadata.Name);
            if (isForeignKeyProperty) continue;

            var value = prop.CurrentValue;
            if (value is null) continue;

            if (value is bool b)
                parts.Add($"{prop.Metadata.Name}: {(b ? _options.BoolTrueDisplay : _options.BoolFalseDisplay)}");
            else
                parts.Add($"{prop.Metadata.Name}: {value}");
        }

        if (parts.Count > 0)
            return string.Join(", ", parts);

        foreach (var fk in child.Metadata.GetForeignKeys())
        {
            if (fk.PrincipalEntityType.ClrType == root.Metadata.ClrType)
                continue;

            foreach (var fkProp in fk.Properties)
            {
                if (_options.IgnoredProperties.Contains(fkProp.Name)) continue;

                var fkValue = child.Property(fkProp.Name).CurrentValue;
                if (fkValue is null) continue;

                var (_, nome) = _options.FkNameResolver(context, child, fk, fkValue);
                if (nome != null)
                    parts.Add(nome);
                else
                    parts.Add(fkValue.ToString()!);
            }
        }

        if (parts.Count > 0)
            return string.Join(", ", parts);

        foreach (var prop in child.Properties)
        {
            if (!prop.Metadata.IsPrimaryKey()) continue;
            if (prop.Metadata.IsShadowProperty()) continue;
            var value = prop.CurrentValue?.ToString();
            if (value != null)
                parts.Add(value);
        }

        return parts.Count > 0 ? string.Join(", ", parts) : "(identificador)";
    }

    private string FormatDiffValue(object? value)
    {
        return value switch
        {
            null => _options.NullDisplay,
            string s => $"\"{s}\"",
            DateTime dt => $"{_options.DateTimeFormat.Prefix}{dt.ToString(_options.DateTimeFormat.Format)}{_options.DateTimeFormat.Suffix}",
            DateOnly d => $"{_options.DateOnlyFormat.Prefix}{d.ToString(_options.DateOnlyFormat.Format)}{_options.DateOnlyFormat.Suffix}",
            bool b => b ? _options.BoolTrueDisplay : _options.BoolFalseDisplay,
            _ => value.ToString() ?? _options.NullDisplay
        };
    }

    private static bool AreValuesEqual(object? a, object? b, AuditLibOptions options)
    {
        if (ReferenceEquals(a, b)) return true;
        if (a is null || b is null) return false;

        var type = a.GetType();
        if (type != b.GetType()) return false;

        if (type.IsPrimitive || type.IsEnum ||
            a is string or DateTime or DateOnly or Guid or decimal)
            return Equals(a, b);

        var jsonA = JsonSerializer.Serialize(a, options.JsonSerializerOptions);
        var jsonB = JsonSerializer.Serialize(b, options.JsonSerializerOptions);
        return jsonA == jsonB;
    }

    private string ResolveDisplayName(string technicalName)
        => _options.DisplayNames.TryGetValue(technicalName, out var displayName) ? displayName : technicalName;

    private (EntityEntry Entry, Type ClrType) ResolveAggregateRoot(DbContext context, EntityEntry entry)
    {
        var clrType = entry.Metadata.ClrType;

        if (!_options.AggregateRootMappings.TryGetValue(clrType, out var rootType))
            return (entry, clrType);

        var rootEntry = FindRootInTracker(context, entry, rootType);
        if (rootEntry != null)
            return (rootEntry, rootType);

        return (entry, clrType);
    }

    private static EntityEntry? FindRootInTracker(DbContext context, EntityEntry childEntry, Type rootType)
    {
        foreach (var fk in childEntry.Metadata.GetForeignKeys())
        {
            if (fk.PrincipalEntityType.ClrType != rootType)
                continue;

            var fkValues = fk.Properties
                .Select(p => childEntry.Property(p.Name).CurrentValue
                          ?? childEntry.Property(p.Name).OriginalValue)
                .ToList();

            foreach (var tracked in context.ChangeTracker.Entries())
            {
                if (tracked.Metadata.ClrType != rootType)
                    continue;

                var matches = true;
                for (var i = 0; i < fkValues.Count; i++)
                {
                    var pkValue = tracked.Property(fk.PrincipalKey.Properties[i].Name).CurrentValue;
                    if (!Equals(pkValue, fkValues[i]))
                    {
                        matches = false;
                        break;
                    }
                }

                if (matches)
                    return tracked;
            }
        }

        return null;
    }

    private string ResolvePropertyName(string entityName, string propertyName)
    {
        if (_options.PropertyDisplayNames.TryGetValue(entityName, out var entityProps)
            && entityProps.TryGetValue(propertyName, out var displayName))
            return displayName;
        return propertyName;
    }
}

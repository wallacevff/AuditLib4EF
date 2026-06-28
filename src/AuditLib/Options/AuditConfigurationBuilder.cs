using System.Linq.Expressions;
using AuditLib.Abstractions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata;

namespace AuditLib.Options;

public sealed class AuditConfigurationBuilder
{
    private readonly AuditLibOptions _options = new();

    internal AuditLibOptions Build() => _options;

    public AuditConfigurationBuilder AuditLogTable(string tableName, string? schema = null)
    {
        _options.AuditLogTableName = tableName;
        _options.AuditLogTableSchema = schema;
        return this;
    }

    public AuditConfigurationBuilder UseTimestampProvider(Func<DateTime> provider)
    {
        _options.TimestampProvider = provider;
        return this;
    }

    public AuditConfigurationBuilder SoftDelete(bool enabled)
    {
        _options.SoftDeleteEnabled = enabled;
        return this;
    }

    public AuditConfigurationBuilder TrackAdded(bool track)
    {
        _options.TrackAddedEntities = track;
        return this;
    }

    public AuditConfigurationBuilder TrackModified(bool track)
    {
        _options.TrackModifiedEntities = track;
        return this;
    }

    public AuditConfigurationBuilder TrackDeleted(bool track)
    {
        _options.TrackDeletedEntities = track;
        return this;
    }

    public AuditConfigurationBuilder TrackUnchangedRootsWithChangedChildren(bool track)
    {
        _options.TrackUnchangedRootsWithChangedChildren = track;
        return this;
    }

    public AuditConfigurationBuilder WithUserIdResolver(UserIdResolver resolver)
    {
        _options.UserIdResolver = resolver;
        return this;
    }

    public AuditConfigurationBuilder WithFkNameResolver(FkNameResolver resolver)
    {
        _options.FkNameResolver = resolver;
        return this;
    }

    public AuditConfigurationBuilder WithEntitySelector(Func<Type, bool> selector)
    {
        _options.EntitySelector = selector;
        return this;
    }

    public EntityConfigurationBuilder<TEntity> Entity<TEntity>() where TEntity : class
    {
        return new EntityConfigurationBuilder<TEntity>(_options);
    }

    public AuditConfigurationBuilder WithAggregateSnapshot(AggregateSnapshotMode mode)
    {
        _options.AggregateSnapshot = mode;
        return this;
    }

    public AuditConfigurationBuilder WithAggregateRootDisplayFormatter(AggregateRootDisplayFormatter formatter)
    {
        _options.AggregateRootDisplayFormatter = formatter;
        return this;
    }

    public AuditConfigurationBuilder SetActionAdded(string action)
    {
        _options.ActionAdded = action;
        return this;
    }

    public AuditConfigurationBuilder SetActionModified(string action)
    {
        _options.ActionModified = action;
        return this;
    }

    public AuditConfigurationBuilder SetActionDeleted(string action)
    {
        _options.ActionDeleted = action;
        return this;
    }

    public AuditConfigurationBuilder SetDiffAddedMessage(string message)
    {
        _options.DiffAddedMessage = message;
        return this;
    }

    public AuditConfigurationBuilder SetDiffDeletedMessage(string message)
    {
        _options.DiffDeletedMessage = message;
        return this;
    }

    public AuditConfigurationBuilder SetDiffRemovedFormat(string format)
    {
        _options.DiffRemovedFormat = format;
        return this;
    }

    public AuditConfigurationBuilder SetDiffAddedFormat(string format)
    {
        _options.DiffAddedFormat = format;
        return this;
    }

    public AuditConfigurationBuilder SetNullDisplay(string display)
    {
        _options.NullDisplay = display;
        return this;
    }

    public AuditConfigurationBuilder SetBoolTrueDisplay(string display)
    {
        _options.BoolTrueDisplay = display;
        return this;
    }

    public AuditConfigurationBuilder SetBoolFalseDisplay(string display)
    {
        _options.BoolFalseDisplay = display;
        return this;
    }
}

public sealed class EntityConfigurationBuilder<TEntity> where TEntity : class
{
    private readonly AuditLibOptions _options;
    private readonly string _entityName;

    internal EntityConfigurationBuilder(AuditLibOptions options)
    {
        _options = options;
        _entityName = typeof(TEntity).Name;
    }

    public EntityConfigurationBuilder<TEntity> DisplayName(string name)
    {
        _options.DisplayNames[_entityName] = name;
        return this;
    }

    public EntityConfigurationBuilder<TEntity> MapToAggregateRoot<TRoot>() where TRoot : class
    {
        _options.AggregateRootMappings[typeof(TEntity)] = typeof(TRoot);
        return this;
    }

    public EntityConfigurationBuilder<TEntity> Property(string propertyName, string displayName)
    {
        if (!_options.PropertyDisplayNames.ContainsKey(_entityName))
            _options.PropertyDisplayNames[_entityName] = [];
        _options.PropertyDisplayNames[_entityName][propertyName] = displayName;
        return this;
    }

    public EntityConfigurationBuilder<TEntity> Property<TProperty>(
        Expression<Func<TEntity, TProperty>> propertyExpression,
        string displayName)
    {
        var memberName = propertyExpression.Body switch
        {
            MemberExpression m => m.Member.Name,
            UnaryExpression { Operand: MemberExpression m } => m.Member.Name,
            _ => throw new ArgumentException("Expression must be a property access.")
        };
        return Property(memberName, displayName);
    }

    public EntityConfigurationBuilder<TEntity> IgnoreProperty(string propertyName)
    {
        _options.IgnoredProperties.Add(propertyName);
        return this;
    }
}

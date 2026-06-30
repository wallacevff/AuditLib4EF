This file is a merged representation of a subset of the codebase, containing files not matching ignore patterns, combined into a single document by Repomix.

# File Summary

## Purpose
This file contains a packed representation of a subset of the repository's contents that is considered the most important context.
It is designed to be easily consumable by AI systems for analysis, code review,
or other automated processes.

## File Format
The content is organized as follows:
1. This summary section
2. Repository information
3. Directory structure
4. Repository files (if enabled)
5. Multiple file entries, each consisting of:
  a. A header with the file path (## File: path/to/file)
  b. The full contents of the file in a code block

## Usage Guidelines
- This file should be treated as read-only. Any changes should be made to the
  original repository files, not this packed version.
- When processing this file, use the file path to distinguish
  between different files in the repository.
- Be aware that this file may contain sensitive information. Handle it with
  the same level of security as you would the original repository.

## Notes
- Some files may have been excluded based on .gitignore rules and Repomix's configuration
- Binary files are not included in this packed representation. Please refer to the Repository Structure section for a complete list of file paths, including binary files
- Files matching these patterns are excluded: *.user, *.suo, *.sln.docstates, .vs/, packages/, obj/, bin/, *.nupkg, .idea/, .kilo/, repomix-output.md, repomix.config.json
- Files matching patterns in .gitignore are excluded
- Files matching default ignore patterns are excluded
- Files are sorted by Git change count (files with more changes are at the bottom)

# Directory Structure
```
.gitignore
AuditLib.slnx
README.md
src/AuditLib/Abstractions/IAuditEntity.cs
src/AuditLib/Abstractions/IAuditLogRepository.cs
src/AuditLib/Abstractions/IAuditLogService.cs
src/AuditLib/Abstractions/IEntityNameResolver.cs
src/AuditLib/Abstractions/IUserContext.cs
src/AuditLib/AuditLib.csproj
src/AuditLib/Domain/AuditEntity.cs
src/AuditLib/Domain/AuditLog.cs
src/AuditLib/Extensions/ServiceCollectionExtensions.cs
src/AuditLib/Infrastructure/AuditInterceptor.cs
src/AuditLib/Infrastructure/AuditLogEntityTypeConfiguration.cs
src/AuditLib/Infrastructure/AuditLogRepository.cs
src/AuditLib/Infrastructure/AuditLogService.cs
src/AuditLib/Infrastructure/DefaultUserContext.cs
src/AuditLib/Infrastructure/SoftDeleteHandler.cs
src/AuditLib/Options/AuditConfigurationBuilder.cs
src/AuditLib/Options/AuditLibOptions.cs
tests/AuditLib.Tests/AuditInterceptorTests.cs
tests/AuditLib.Tests/AuditLib.Tests.csproj
tests/AuditLib.Tests/AuditLogServiceTests.cs
tests/AuditLib.Tests/TestInfrastructure.cs
```

# Files

## File: AuditLib.slnx
````
<Solution>
  <Folder Name="/src/">
    <Project Path="src/AuditLib/AuditLib.csproj" />
  </Folder>
  <Folder Name="/tests/">
    <Project Path="tests/AuditLib.Tests/AuditLib.Tests.csproj" />
  </Folder>
</Solution>
````

## File: src/AuditLib/Abstractions/IAuditEntity.cs
````csharp
namespace AuditLib.Abstractions;

public interface IAuditEntity
{
    DateTime CreatedAt { get; set; }
    DateTime? UpdatedAt { get; set; }
    DateTime? DeletedAt { get; set; }
    bool IsDeleted { get; set; }
}
````

## File: src/AuditLib/Abstractions/IAuditLogRepository.cs
````csharp
using AuditLib.Domain;

namespace AuditLib.Abstractions;

public interface IAuditLogRepository
{
    IQueryable<AuditLog> Query();
    IQueryable<AuditLog> QueryByEntity(string entityName, string primaryKey);
    void Add(AuditLog auditLog);
}
````

## File: src/AuditLib/Abstractions/IAuditLogService.cs
````csharp
using AuditLib.Domain;

namespace AuditLib.Abstractions;

public interface IAuditLogService
{
    Task<List<AuditLog>> GetByEntityAsync(
        string entityName,
        string primaryKey,
        int page = 1,
        int pageSize = 50,
        CancellationToken ct = default);

    Task<long> GetCountByEntityAsync(
        string entityName,
        string primaryKey,
        CancellationToken ct = default);

    Task<List<AuditLog>> GetByUserAsync(
        Guid userId,
        int page = 1,
        int pageSize = 50,
        CancellationToken ct = default);

    Task<long> GetCountByUserAsync(
        Guid userId,
        CancellationToken ct = default);
}
````

## File: src/AuditLib/Abstractions/IEntityNameResolver.cs
````csharp
namespace AuditLib.Abstractions;

public interface IEntityNameResolver
{
    string GetEntityName(Type clrType);
}
````

## File: src/AuditLib/Abstractions/IUserContext.cs
````csharp
namespace AuditLib.Abstractions;

public interface IUserContext
{
    Guid? GetCurrentUserId();
}
````

## File: src/AuditLib/AuditLib.csproj
````
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <RootNamespace>AuditLib</RootNamespace>
    <ManagePackageVersionsCentrally>false</ManagePackageVersionsCentrally>
    <GeneratePackageOnBuild>true</GeneratePackageOnBuild>
    <PackageId>AuditLib</PackageId>
    <Description>Biblioteca de auditoria limpa e parametrizável para EF Core. Gera automaticamente trilhas de auditoria com soft-delete, diff legível e resolução de nomes.</Description>
    <Version>1.0.0</Version>
    <Authors>AuditLib</Authors>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Microsoft.EntityFrameworkCore" Version="10.0.9" />
    <PackageReference Include="Microsoft.EntityFrameworkCore.Relational" Version="10.0.9" />
    <PackageReference Include="Microsoft.AspNetCore.Http.Abstractions" Version="2.3.0" />
    <PackageReference Include="Microsoft.Extensions.DependencyInjection.Abstractions" Version="10.0.9" />
  </ItemGroup>

</Project>
````

## File: src/AuditLib/Domain/AuditEntity.cs
````csharp
using AuditLib.Abstractions;

namespace AuditLib.Domain;

public abstract class AuditEntity : IAuditEntity
{
    public Guid Id { get; init; } = Guid.CreateVersion7();
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public DateTime? DeletedAt { get; set; }
    public bool IsDeleted { get; set; }
}
````

## File: src/AuditLib/Domain/AuditLog.cs
````csharp
namespace AuditLib.Domain;

public sealed class AuditLog
{
    public Guid Id { get; init; } = Guid.CreateVersion7();
    public string EntityName { get; private set; }
    public string PrimaryKey { get; private set; }
    public string Action { get; private set; }
    public Guid? UserId { get; private set; }
    public DateTime Timestamp { get; private set; }
    public string? PreviousState { get; private set; }
    public string? CurrentState { get; private set; }
    public string? Diff { get; private set; }

    private AuditLog(
        string entityName,
        string primaryKey,
        string action,
        Guid? userId,
        DateTime timestamp,
        string? previousState,
        string? currentState,
        string? diff)
    {
        EntityName = entityName;
        PrimaryKey = primaryKey;
        Action = action;
        UserId = userId;
        Timestamp = timestamp;
        PreviousState = previousState;
        CurrentState = currentState;
        Diff = diff;
    }

    private AuditLog() 
    {
        EntityName = string.Empty;
        PrimaryKey = string.Empty;
        Action = string.Empty;
    }

    public static AuditLog Create(
        string entityName,
        string primaryKey,
        string action,
        Guid? userId,
        DateTime timestamp,
        string? previousState,
        string? currentState,
        string? diff) => new(
            entityName,
            primaryKey,
            action,
            userId,
            timestamp,
            previousState,
            currentState,
            diff);
}
````

## File: src/AuditLib/Extensions/ServiceCollectionExtensions.cs
````csharp
using AuditLib.Abstractions;
using AuditLib.Infrastructure;
using AuditLib.Options;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace AuditLib.Extensions;

public static class ServiceCollectionExtensions
{

    public static IServiceCollection AddAuditLib(
        this IServiceCollection services,
        Action<AuditLibOptions>? configureOptions = null)
    {
        if (configureOptions == null)
            return services;
        var options = AuditLibOptions.Default;
        configureOptions(options);

        return services.AddAuditLibCore(options);
    }

    public static IServiceCollection AddAuditLib(
        this IServiceCollection services,
        Action<AuditConfigurationBuilder> configure)
    {
        var builder = new AuditConfigurationBuilder();
        configure(builder);
        return services.AddAuditLibCore(builder.Build());
    }

    public static IServiceCollection AddAuditLibWithInterceptor(
        this IServiceCollection services,
        Action<AuditConfigurationBuilder> configure)
    {
        return services.AddAuditLibWithInterceptorCore(configure.BuildFromBuilder());
    }

    public static IServiceCollection AddAuditLibWithInterceptor(
        this IServiceCollection services,
        Action<AuditLibOptions>? configureOptions = null)
    {
        return configureOptions == null
            ? services.AddAuditLibWithInterceptorCore(AuditLibOptions.Default)
            : services.AddAuditLibWithInterceptorCore(ApplyOptions(configureOptions));
    }

    private static AuditLibOptions ApplyOptions(Action<AuditLibOptions> configure)
    {
        var options = AuditLibOptions.Default;
        configure(options);
        return options;
    }

    private static AuditLibOptions BuildFromBuilder(this Action<AuditConfigurationBuilder> configure)
    {
        var builder = new AuditConfigurationBuilder();
        configure(builder);
        return builder.Build();
    }

    private static IServiceCollection AddAuditLibCore(this IServiceCollection services, AuditLibOptions options)
    {
        services.AddSingleton(options);
        services.TryAddScoped<IUserContext>(sp =>
        {
            var httpContextAccessor = sp.GetService<Microsoft.AspNetCore.Http.IHttpContextAccessor>();
            if (httpContextAccessor?.HttpContext?.User?.Identity?.IsAuthenticated == true)
            {
                return new DefaultUserContext(() =>
                {
                    var user = httpContextAccessor.HttpContext?.User;
                    var userIdClaim = user?.FindFirst("user_id")?.Value;
                    if (userIdClaim is not null && Guid.TryParse(userIdClaim, out var id))
                        return id;
                    if (httpContextAccessor.HttpContext?.Items is { } items &&
                        items.TryGetValue("CurrentUserId", out var currentUserId) &&
                        currentUserId is Guid uid)
                        return uid;
                    var subClaim = user?.FindFirst("sub")?.Value;
                    if (subClaim is not null && Guid.TryParse(subClaim, out var subId))
                        return subId;
                    return null;
                });
            }
            return new DefaultUserContext(() => null);
        });
        services.TryAddScoped<AuditInterceptor>();
        services.TryAddScoped<IAuditLogRepository>(sp =>
        {
            var dbContext = sp.GetRequiredService<DbContext>();
            return new AuditLogRepository(dbContext);
        });
        services.TryAddScoped<IAuditLogService, AuditLogService>();
        return services;
    }

    private static IServiceCollection AddAuditLibWithInterceptorCore(this IServiceCollection services, AuditLibOptions options)
    {
        services.AddSingleton(options);
        services.TryAddScoped(sp =>
        {
            var userContext = sp.GetService<IUserContext>();
            return new AuditInterceptor(options, userContext);
        });
        services.TryAddScoped<IUserContext>(sp =>
        {
            var httpContextAccessor = sp.GetService<Microsoft.AspNetCore.Http.IHttpContextAccessor>();
            if (httpContextAccessor?.HttpContext?.User?.Identity?.IsAuthenticated == true)
            {
                return new DefaultUserContext(() =>
                {
                    var user = httpContextAccessor.HttpContext?.User;
                    var userIdClaim = user?.FindFirst("user_id")?.Value;
                    if (userIdClaim is not null && Guid.TryParse(userIdClaim, out var id))
                        return id;
                    if (httpContextAccessor.HttpContext?.Items is { } items &&
                        items.TryGetValue("CurrentUserId", out var currentUserId) &&
                        currentUserId is Guid uid)
                        return uid;
                    var subClaim = user?.FindFirst("sub")?.Value;
                    if (subClaim is not null && Guid.TryParse(subClaim, out var subId))
                        return subId;
                    return null;
                });
            }
            return new DefaultUserContext(() => null);
        });
        services.TryAddScoped<IAuditLogRepository>(sp =>
        {
            var dbContext = sp.GetRequiredService<DbContext>();
            return new AuditLogRepository(dbContext);
        });
        services.TryAddScoped<IAuditLogService, AuditLogService>();
        return services;
    }

    public static DbContextOptionsBuilder UseAuditInterceptor(
        this DbContextOptionsBuilder builder,
        IServiceProvider serviceProvider)
    {
        var interceptor = serviceProvider.GetRequiredService<AuditInterceptor>();
        builder.AddInterceptors(interceptor);
        return builder;
    }
}
````

## File: src/AuditLib/Infrastructure/AuditLogEntityTypeConfiguration.cs
````csharp
using AuditLib.Domain;
using AuditLib.Options;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AuditLib.Infrastructure;

public sealed class AuditLogEntityTypeConfiguration(AuditLibOptions options)
    : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> builder)
    {
        builder.ToTable(options.AuditLogTableName, options.AuditLogTableSchema);

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .ValueGeneratedNever();

        builder.Property(x => x.EntityName)
            .HasColumnName("EntityName")
            .HasMaxLength(255)
            .IsRequired();

        builder.Property(x => x.PrimaryKey)
            .HasColumnName("PrimaryKey")
            .HasMaxLength(255)
            .IsRequired();

        builder.Property(x => x.Action)
            .HasColumnName("Action")
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(x => x.UserId)
            .HasColumnName("UserId");

        builder.Property(x => x.Timestamp)
            .HasColumnName("Timestamp")
            .HasColumnType("datetime2")
            .IsRequired();

        builder.Property(x => x.PreviousState)
            .HasColumnName("PreviousState")
            .HasColumnType("nvarchar(max)");

        builder.Property(x => x.CurrentState)
            .HasColumnName("CurrentState")
            .HasColumnType("nvarchar(max)");

        builder.Property(x => x.Diff)
            .HasColumnName("Diff")
            .HasColumnType("nvarchar(max)");

        builder.HasIndex(x => new { x.EntityName, x.PrimaryKey })
            .HasDatabaseName($"IX_{options.AuditLogTableName}_EntityName_PrimaryKey");

        builder.HasIndex(x => x.Timestamp)
            .HasDatabaseName($"IX_{options.AuditLogTableName}_Timestamp");

        builder.HasIndex(x => x.UserId)
            .HasDatabaseName($"IX_{options.AuditLogTableName}_UserId");
    }
}
````

## File: src/AuditLib/Infrastructure/AuditLogRepository.cs
````csharp
using AuditLib.Abstractions;
using AuditLib.Domain;
using Microsoft.EntityFrameworkCore;

namespace AuditLib.Infrastructure;

public sealed class AuditLogRepository(DbContext context) : IAuditLogRepository
{
    public IQueryable<AuditLog> Query()
        => context.Set<AuditLog>().AsNoTracking();

    public IQueryable<AuditLog> QueryByEntity(string entityName, string primaryKey)
        => Query().Where(a => a.EntityName == entityName && a.PrimaryKey == primaryKey);

    public void Add(AuditLog auditLog)
        => context.Set<AuditLog>().Add(auditLog);
}
````

## File: src/AuditLib/Infrastructure/AuditLogService.cs
````csharp
using AuditLib.Abstractions;
using AuditLib.Domain;
using Microsoft.EntityFrameworkCore;

namespace AuditLib.Infrastructure;

public sealed class AuditLogService(
    IAuditLogRepository repository) : IAuditLogService
{
    public async Task<List<AuditLog>> GetByEntityAsync(
        string entityName,
        string primaryKey,
        int page = 1,
        int pageSize = 50,
        CancellationToken ct = default)
    {
        var query = repository
            .QueryByEntity(entityName, primaryKey)
            .OrderByDescending(a => a.Timestamp);

        return await query
            .Skip((Math.Max(1, page) - 1) * Math.Max(1, pageSize))
            .Take(Math.Max(1, pageSize))
            .ToListAsync(ct);
    }

    public async Task<long> GetCountByEntityAsync(
        string entityName,
        string primaryKey,
        CancellationToken ct = default)
        => await repository
            .QueryByEntity(entityName, primaryKey)
            .LongCountAsync(ct);

    public async Task<List<AuditLog>> GetByUserAsync(
        Guid userId,
        int page = 1,
        int pageSize = 50,
        CancellationToken ct = default)
    {
        var query = repository
            .Query()
            .Where(a => a.UserId == userId)
            .OrderByDescending(a => a.Timestamp);

        return await query
            .Skip((Math.Max(1, page) - 1) * Math.Max(1, pageSize))
            .Take(Math.Max(1, pageSize))
            .ToListAsync(ct);
    }

    public async Task<long> GetCountByUserAsync(
        Guid userId,
        CancellationToken ct = default)
        => await repository
            .Query()
            .LongCountAsync(a => a.UserId == userId, ct);
}
````

## File: src/AuditLib/Infrastructure/DefaultUserContext.cs
````csharp
using AuditLib.Abstractions;

namespace AuditLib.Infrastructure;

public sealed class DefaultUserContext : IUserContext
{
    private readonly Func<Guid?> _resolver;

    public DefaultUserContext(Func<Guid?> resolver)
    {
        _resolver = resolver;
    }

    public Guid? GetCurrentUserId() => _resolver();
}
````

## File: src/AuditLib/Infrastructure/SoftDeleteHandler.cs
````csharp
using System.Linq.Expressions;
using System.Reflection;
using AuditLib.Abstractions;
using AuditLib.Options;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata;

namespace AuditLib.Infrastructure;

public static class SoftDeleteHandler
{
    public static async Task HandleDeleteAsync(
        DbContext context,
        EntityEntry entry,
        DateTime deletedAt,
        AuditLibOptions options)
        => await HandleDeleteAsync(context, entry, deletedAt, new HashSet<object>(), options);

    private static async Task HandleDeleteAsync(
        DbContext context,
        EntityEntry entry,
        DateTime deletedAt,
        HashSet<object> visited,
        AuditLibOptions options)
    {
        if (!visited.Add(entry.Entity))
            return;

        MarkSoftDeleted(entry, deletedAt);

        foreach (var fk in entry.Metadata.GetReferencingForeignKeys().Where(ShouldProcessForeignKey))
        {
            switch (fk.DeleteBehavior)
            {
                case DeleteBehavior.Restrict:
                case DeleteBehavior.NoAction:
                    await ValidateRestrictAsync(context, entry, fk);
                    break;
                case DeleteBehavior.Cascade:
                    await ProcessCascadeAsync(context, entry, deletedAt, fk, visited, options);
                    break;
                case DeleteBehavior.SetNull:
                case DeleteBehavior.ClientSetNull:
                    await ProcessSetNullAsync(context, entry, fk);
                    break;
            }
        }
    }

    private static void MarkSoftDeleted(EntityEntry entry, DateTime deletedAt)
    {
        if (entry.State == EntityState.Modified && (bool)entry.Property("IsDeleted").CurrentValue!)
            return;

        if (entry.State == EntityState.Deleted)
            entry.State = EntityState.Unchanged;

        entry.Property("IsDeleted").CurrentValue = true;
        entry.Property("IsDeleted").IsModified = true;

        entry.Property("DeletedAt").CurrentValue = deletedAt;
        entry.Property("DeletedAt").IsModified = true;

        BlindOwnedEntities(entry);
    }

    private static void BlindOwnedEntities(EntityEntry currentEntry)
    {
        foreach (var navigation in currentEntry.Navigations)
        {
            if (navigation.Metadata is not INavigation navMetadata || !navMetadata.TargetEntityType.IsOwned())
                continue;

            if (navigation.CurrentValue == null)
                continue;

            var ownedEntry = currentEntry.Context.Entry(navigation.CurrentValue);
            ownedEntry.State = EntityState.Unchanged;
            BlindOwnedEntities(ownedEntry);
        }
    }

    private static async Task ValidateRestrictAsync(DbContext context, EntityEntry entry, IForeignKey fk)
    {
        var existsDependents = await ExistsDependentsAsync(context, entry, fk);
        if (existsDependents)
        {
            var pkValues = GetPrimaryKeyValues(entry);
            throw new InvalidOperationException(
                $"Cannot delete {entry.Metadata.Name} with PK {string.Join(",", pkValues)} because it has dependent entities.");
        }
    }

    private static async Task ProcessCascadeAsync(
        DbContext context,
        EntityEntry entry,
        DateTime deletedAt,
        IForeignKey fk,
        HashSet<object> visited,
        AuditLibOptions options)
    {
        var dependents = await LoadDependentsAsync(context, entry, fk);

        foreach (var dep in dependents)
        {
            var depEntry = context.Entry(dep);
            var dependentClrType = depEntry.Metadata.ClrType;

            var hasSoftDelete = typeof(IAuditEntity).IsAssignableFrom(dependentClrType)
                                && depEntry.Metadata.FindProperty(nameof(IAuditEntity.IsDeleted)) != null;

            if (hasSoftDelete)
            {
                MarkSoftDeleted(depEntry, deletedAt);
                await HandleDeleteAsync(context, depEntry, deletedAt, visited, options);
            }
            else
            {
                depEntry.State = EntityState.Deleted;
            }
        }
    }

    private static async Task ProcessSetNullAsync(DbContext context, EntityEntry entry, IForeignKey fk)
    {
        var dependents = await LoadDependentsAsync(context, entry, fk);
        foreach (var dep in dependents)
        {
            var depEntry = context.Entry(dep);
            foreach (var prop in fk.Properties)
            {
                depEntry.Property(prop.Name).CurrentValue = null;
                depEntry.Property(prop.Name).IsModified = true;
            }
            depEntry.State = EntityState.Modified;
        }
    }

    private static async Task<bool> ExistsDependentsAsync(DbContext context, EntityEntry entry, IForeignKey fk)
    {
        var clrType = fk.DeclaringEntityType.ClrType;
        var dbSet = GetDynamicQueryable(context, clrType);

        var ignoreFiltersMethod = typeof(EntityFrameworkQueryableExtensions)
            .GetMethods(BindingFlags.Public | BindingFlags.Static)
            .First(m => m is { Name: nameof(EntityFrameworkQueryableExtensions.IgnoreQueryFilters), IsGenericMethodDefinition: true }
                        && m.GetParameters().Length == 1)
            .MakeGenericMethod(clrType);

        var query = ignoreFiltersMethod.Invoke(null, [dbSet]) ?? dbSet;
        var predicate = BuildPredicate(entry, fk);
        return await InvokeAnyAsync(clrType, query, predicate);
    }

    private static async Task<List<object>> LoadDependentsAsync(DbContext context, EntityEntry entry, IForeignKey fk)
    {
        var clrType = fk.DeclaringEntityType.ClrType;
        var dbSet = GetDynamicQueryable(context, clrType);
        var predicate = BuildPredicate(entry, fk);

        var whereMethod = typeof(Queryable)
            .GetMethods()
            .First(m => m is { Name: nameof(Queryable.Where), IsGenericMethodDefinition: true } && m.GetParameters().Length == 2)
            .MakeGenericMethod(clrType);

        var filtered = whereMethod.Invoke(null, [dbSet, predicate])!;

        var toListAsync = typeof(EntityFrameworkQueryableExtensions)
            .GetMethods(BindingFlags.Public | BindingFlags.Static)
            .Where(m => m.Name == nameof(EntityFrameworkQueryableExtensions.ToListAsync))
            .Select(m => m.MakeGenericMethod(clrType))
            .First(m => m.GetParameters().Length == 2)
            .Invoke(null, [filtered, CancellationToken.None])!;

        await ((Task)toListAsync).ConfigureAwait(false);
        return ((dynamic)toListAsync).Result as List<object> ?? [];
    }

    private static LambdaExpression BuildPredicate(EntityEntry entry, IForeignKey fk)
    {
        var keyValues = GetPrimaryKeyValues(entry);
        var param = Expression.Parameter(fk.DeclaringEntityType.ClrType, "e");
        Expression? body = null;

        for (var i = 0; i < fk.Properties.Count; i++)
        {
            var prop = fk.Properties[i];
            var left = BuildPropertyAccess(param, prop);
            var right = Expression.Constant(keyValues[i], prop.ClrType);
            var eq = Expression.Equal(left, right);
            body = body == null ? eq : Expression.AndAlso(body, eq);
        }

        return Expression.Lambda(body!, param);
    }

    private static bool ShouldProcessForeignKey(IForeignKey fk)
        => !fk.IsOwnership && !fk.DeclaringEntityType.IsOwned() && !fk.PrincipalEntityType.IsOwned();

    private static Expression BuildPropertyAccess(ParameterExpression param, IProperty property)
    {
        if (property.PropertyInfo != null)
            return Expression.Property(param, property.PropertyInfo);

        var efPropertyMethod = typeof(EF)
            .GetMethod(nameof(EF.Property), BindingFlags.Public | BindingFlags.Static)!
            .MakeGenericMethod(property.ClrType);

        return Expression.Call(efPropertyMethod, param, Expression.Constant(property.Name));
    }

    private static object[] GetPrimaryKeyValues(EntityEntry entry)
    {
        var pk = entry.Metadata.FindPrimaryKey()!;
        return pk.Properties.Select(p => entry.Property(p.Name).CurrentValue!).ToArray();
    }

    private static IQueryable GetDynamicQueryable(DbContext context, Type clrType)
        => (IQueryable)context.GetType()
            .GetMethod(nameof(DbContext.Set), [])!
            .MakeGenericMethod(clrType)
            .Invoke(context, null)!;

    private static Task<bool> InvokeAnyAsync(Type clrType, object dbSet, LambdaExpression predicate)
    {
        var anyMethod = typeof(EntityFrameworkQueryableExtensions)
            .GetMethods(BindingFlags.Public | BindingFlags.Static)
            .Where(m => m.Name == nameof(EntityFrameworkQueryableExtensions.AnyAsync))
            .Select(m => m.MakeGenericMethod(clrType))
            .First(m => m.GetParameters().Length == 3);

        return (Task<bool>)anyMethod.Invoke(null, [dbSet, predicate, CancellationToken.None])!;
    }
}
````

## File: tests/AuditLib.Tests/AuditLib.Tests.csproj
````
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <ManagePackageVersionsCentrally>false</ManagePackageVersionsCentrally>
    <IsPackable>false</IsPackable>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="xunit" Version="2.9.3" />
    <PackageReference Include="xunit.runner.visualstudio" Version="3.1.5">
      <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
      <PrivateAssets>all</PrivateAssets>
    </PackageReference>
    <PackageReference Include="Microsoft.EntityFrameworkCore.Sqlite" Version="10.0.9" />
    <PackageReference Include="Microsoft.EntityFrameworkCore" Version="10.0.9" />
    <PackageReference Include="FluentAssertions" Version="7.2.2" />
    <PackageReference Include="Microsoft.Extensions.DependencyInjection" Version="10.0.9" />
    <PackageReference Include="Microsoft.NET.Test.Sdk" Version="18.6.0" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="..\..\src\AuditLib\AuditLib.csproj" />
  </ItemGroup>

</Project>
````

## File: tests/AuditLib.Tests/AuditLogServiceTests.cs
````csharp
using AuditLib.Abstractions;
using AuditLib.Domain;
using AuditLib.Infrastructure;
using AuditLib.Options;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace AuditLib.Tests;

public class AuditLogServiceTests : IDisposable
{
    private readonly TestDbContext _context;
    private readonly AuditLogRepository _repository;
    private readonly AuditLogService _service;
    private static readonly DateTime FixedTimestamp = new(2026, 6, 28, 14, 0, 0, DateTimeKind.Utc);
    private readonly Guid _userId1 = Guid.CreateVersion7();
    private readonly Guid _userId2 = Guid.CreateVersion7();

    public AuditLogServiceTests()
    {
        var options = new AuditLibOptions { TimestampProvider = () => FixedTimestamp };
        var userContext = new TestUserContext(_userId1);
        var interceptor = new AuditInterceptor(options, userContext);
        _context = TestDbContextFactory.Create(interceptor);
        _repository = new AuditLogRepository(_context);
        _service = new AuditLogService(_repository);

        SeedData();
    }

    private void SeedData()
    {
        var logs = new List<AuditLog>
        {
            AuditLog.Create("EntityA", "1", "Adicionar", _userId1, FixedTimestamp.AddMinutes(-5), null, "{}", "Registro criado."),
            AuditLog.Create("EntityA", "1", "Alterar", _userId1, FixedTimestamp.AddMinutes(-4), "{}", "{\"name\":\"new\"}", "Name alterado."),
            AuditLog.Create("EntityA", "1", "Alterar", _userId2, FixedTimestamp.AddMinutes(-3), "{\"name\":\"new\"}", "{\"name\":\"newer\"}", "Name alterado."),
            AuditLog.Create("EntityA", "2", "Adicionar", _userId1, FixedTimestamp.AddMinutes(-2), null, "{}", "Registro criado."),
            AuditLog.Create("EntityB", "1", "Adicionar", _userId2, FixedTimestamp.AddMinutes(-1), null, "{}", "Registro criado."),
        };

        _context.Set<AuditLog>().AddRange(logs);
        _context.SaveChanges();
    }

    public void Dispose()
    {
        _context.Database.CloseConnection();
        _context.Dispose();
    }

    [Fact]
    public async Task GetByEntityAsync_should_return_paginated_results()
    {
        var results = await _service.GetByEntityAsync("EntityA", "1", page: 1, pageSize: 10);

        results.Should().HaveCount(3);
        results.Should().BeInDescendingOrder(r => r.Timestamp);
        results.Should().AllSatisfy(r =>
        {
            r.EntityName.Should().Be("EntityA");
            r.PrimaryKey.Should().Be("1");
        });
    }

    [Fact]
    public async Task GetByEntityAsync_should_return_empty_for_nonexistent_entity()
    {
        var results = await _service.GetByEntityAsync("NonExistent", "1");

        results.Should().BeEmpty();
    }

    [Fact]
    public async Task GetByEntityAsync_should_respect_pagination()
    {
        var page1 = await _service.GetByEntityAsync("EntityA", "1", page: 1, pageSize: 2);
        var page2 = await _service.GetByEntityAsync("EntityA", "1", page: 2, pageSize: 2);

        page1.Should().HaveCount(2);
        page2.Should().HaveCount(1);
        page1.Should().NotIntersectWith(page2);
    }

    [Fact]
    public async Task GetCountByEntityAsync_should_return_correct_count()
    {
        var count = await _service.GetCountByEntityAsync("EntityA", "1");

        count.Should().Be(3);
    }

    [Fact]
    public async Task GetCountByEntityAsync_should_return_zero_for_nonexistent()
    {
        var count = await _service.GetCountByEntityAsync("NonExistent", "1");

        count.Should().Be(0);
    }

    [Fact]
    public async Task GetByUserAsync_should_return_user_audit_logs()
    {
        var results = await _service.GetByUserAsync(_userId1);

        results.Should().HaveCount(3);
        results.Should().AllSatisfy(r => r.UserId.Should().Be(_userId1));
    }

    [Fact]
    public async Task GetByUserAsync_should_return_empty_for_user_with_no_logs()
    {
        var results = await _service.GetByUserAsync(Guid.NewGuid());

        results.Should().BeEmpty();
    }

    [Fact]
    public async Task GetByUserAsync_should_respect_pagination()
    {
        var page1 = await _service.GetByUserAsync(_userId1, page: 1, pageSize: 2);
        var page2 = await _service.GetByUserAsync(_userId1, page: 2, pageSize: 2);

        page1.Should().HaveCount(2);
        page2.Should().HaveCount(1);
    }

    [Fact]
    public async Task GetCountByUserAsync_should_return_correct_count()
    {
        var count = await _service.GetCountByUserAsync(_userId1);

        count.Should().Be(3);
    }

    [Fact]
    public async Task GetCountByUserAsync_should_return_zero_for_nonexistent_user()
    {
        var count = await _service.GetCountByUserAsync(Guid.NewGuid());

        count.Should().Be(0);
    }
}
````

## File: tests/AuditLib.Tests/TestInfrastructure.cs
````csharp
using AuditLib.Abstractions;
using AuditLib.Domain;
using AuditLib.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace AuditLib.Tests;

public class TestAuditEntity : AuditEntity
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal Value { get; set; }
    public bool IsActive { get; set; }
}

public class TestAddress
{
    public string Street { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string ZipCode { get; set; } = string.Empty;
}

public class TestParentEntity : AuditEntity
{
    public string Name { get; set; } = string.Empty;
    public TestAddress? Address { get; set; }
    public ICollection<TestChildEntity> Children { get; set; } = [];
}

public class TestChildEntity
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public string Name { get; set; } = string.Empty;
    public Guid ParentId { get; set; }
    public TestParentEntity? Parent { get; set; }
}

public class TestNonAuditedEntity
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public string Name { get; set; } = string.Empty;
}

public class TestUserContext(Guid? userId) : IUserContext
{
    public Guid? GetCurrentUserId() => userId;
}

public class TestDbContext : DbContext
{
    public TestDbContext(DbContextOptions<TestDbContext> options)
        : base(options)
    {
    }

    public DbSet<TestAuditEntity> TestAuditEntities => Set<TestAuditEntity>();
    public DbSet<TestParentEntity> TestParentEntities => Set<TestParentEntity>();
    public DbSet<TestChildEntity> TestChildEntities => Set<TestChildEntity>();
    public DbSet<TestNonAuditedEntity> TestNonAuditedEntities => Set<TestNonAuditedEntity>();

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<TestAuditEntity>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).ValueGeneratedNever();
            e.HasQueryFilter(x => !x.IsDeleted);
        });

        modelBuilder.Entity<TestParentEntity>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).ValueGeneratedNever();
            e.HasQueryFilter(x => !x.IsDeleted);
            e.OwnsOne(x => x.Address, a =>
            {
                a.Property(p => p.Street).HasColumnName("Street");
                a.Property(p => p.City).HasColumnName("City");
                a.Property(p => p.ZipCode).HasColumnName("ZipCode");
            });
            e.HasMany(x => x.Children)
                .WithOne(x => x.Parent)
                .HasForeignKey(x => x.ParentId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<TestChildEntity>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).ValueGeneratedNever();
        });

        modelBuilder.Entity<TestNonAuditedEntity>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).ValueGeneratedNever();
        });

        modelBuilder.Entity<AuditLog>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).ValueGeneratedNever();
            e.ToTable("AuditLogs");
        });
    }
}

public static class TestDbContextFactory
{
public static TestDbContext Create(AuditInterceptor interceptor)
{
    var options = new DbContextOptionsBuilder<TestDbContext>()
        .UseSqlite("DataSource=:memory:")
        .AddInterceptors(interceptor)
        .Options;

    var context = new TestDbContext(options);
    context.Database.OpenConnection();
    context.Database.EnsureCreated();
    return context;
}
}
````

## File: .gitignore
````
bin/
obj/
*.nupkg
*.user
*.suo
.vs/
packages/
*.sln.docstates
.DS_Store
Thumbs.db
.idea/
````

## File: src/AuditLib/Options/AuditConfigurationBuilder.cs
````csharp
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

    public AuditConfigurationBuilder WithDualAuditForAggregates(bool enabled = true)
    {
        _options.DualAuditForAggregates = enabled;
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
````

## File: src/AuditLib/Options/AuditLibOptions.cs
````csharp
using System.Text.Json;
using System.Text.Json.Serialization;
using AuditLib.Abstractions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata;

namespace AuditLib.Options;

public delegate Guid? UserIdResolver(IUserContext? userContext);

public delegate (object? Id, string? Name) FkNameResolver(
    DbContext context,
    EntityEntry entry,
    IForeignKey fk,
    object? fkValue);

public delegate string AggregateRootDisplayFormatter(
    EntityEntry rootEntry,
    EntityEntry childEntry,
    string defaultDisplay);

public enum AggregateSnapshotMode
{
    /// <summary>Snapshot apenas a entidade filha que mudou. EntityName e PK sao da raiz.</summary>
    ChildOnly,
    /// <summary>Snapshot o agregado completo (raiz + todos os filhos). EntityName e PK sao da raiz.</summary>
    FullRoot
}

public sealed class AuditLibOptions
{
    public static readonly AuditLibOptions Default = new();

    public Func<DateTime> TimestampProvider { get; set; } = () => DateTime.UtcNow;
    public bool SoftDeleteEnabled { get; set; } = true;
    public string AuditLogTableName { get; set; } = "AuditLogs";
    public string? AuditLogTableSchema { get; set; }
    public bool TrackAddedEntities { get; set; } = true;
    public bool TrackModifiedEntities { get; set; } = true;
    public bool TrackDeletedEntities { get; set; } = true;
    public bool TrackUnchangedRootsWithChangedChildren { get; set; } = true;
    public HashSet<string> IgnoredProperties { get; set; } =
    [
        "CreatedAt",
        "UpdatedAt",
        "DeletedAt",
        "IsDeleted",
        "Version"
    ];
    public HashSet<string> NavigationBackReferences { get; set; } = [];
    public string[] NamePropertyCandidates { get; set; } =
        ["Nome", "NomeCompleto", "Descricao", "RazaoSocial"];
    public JsonSerializerOptions JsonSerializerOptions { get; set; } = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
        ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles,
        WriteIndented = false
    };
    public UserIdResolver UserIdResolver { get; set; } = DefaultUserIdResolver;
    public FkNameResolver FkNameResolver { get; set; } = DefaultFkNameResolver;
    public Func<Type, bool> EntitySelector { get; set; } = DefaultEntitySelector;

    /// <summary>Maps child entity types to their aggregate root types. When a child changes, the audit log uses the root's EntityName and PK.</summary>
    public Dictionary<Type, Type> AggregateRootMappings { get; set; } = [];

    /// <summary>Controls snapshot behavior when aggregate root is resolved. ChildOnly = snapshot apenas o filho. FullRoot = snapshot o agregado completo.</summary>
    public AggregateSnapshotMode AggregateSnapshot { get; set; } = AggregateSnapshotMode.ChildOnly;

    /// <summary>Custom display formatter for aggregate root entity names. Receives (rootEntry, childEntry, defaultDisplay) and returns the display string. Example: "Notif #123 (via Paciente)"</summary>
    public AggregateRootDisplayFormatter? AggregateRootDisplayFormatter { get; set; }

    /// <summary>Quando true e a entidade possui aggregate root mapping, gera um segundo log de auditoria para a entidade original, alem do log atribuido a raiz.</summary>
    public bool DualAuditForAggregates { get; set; } = false;

    /// <summary>Friendly display names for entity type names and navigation names in diffs. Key: technical name, Value: display name.
    /// Example: "DoencaNaoRara" -> "Doença Rara", "Notificacao" -> "Notificação"</summary>
    public Dictionary<string, string> DisplayNames { get; set; } = [];

    /// <summary>Friendly display names for property/field names in diffs, grouped by entity.
    /// Key: entity name, Value: dictionary mapping property name -> display name.
    /// Example: ["Contrato"]["Numero"] -> "Número", ["Contrato"]["Valor"] -> "Valor do Contrato"</summary>
    public Dictionary<string, Dictionary<string, string>> PropertyDisplayNames { get; set; } = [];
    public string ActionAdded { get; set; } = "Adicionar";
    public string ActionModified { get; set; } = "Alterar";
    public string ActionDeleted { get; set; } = "Excluir";
    public string DiffAddedMessage { get; set; } = "Registro criado.";
    public string DiffDeletedMessage { get; set; } = "Registro excluido.";
    public string DiffRemovedFormat { get; set; } = "{0} removidos(as):";
    public string DiffAddedFormat { get; set; } = "{0} incluidos(as):";
    public string NullDisplay { get; set; } = "null";
    public string BoolTrueDisplay { get; set; } = "true";
    public string BoolFalseDisplay { get; set; } = "false";
    public DateTimeFormatSettings DateTimeFormat { get; set; } = new();
    public DateOnlyFormatSettings DateOnlyFormat { get; set; } = new();

    public sealed class DateTimeFormatSettings
    {
        public string Format { get; set; } = "yyyy-MM-dd HH:mm:ss";
        public string? Prefix { get; set; } = "\"";
        public string? Suffix { get; set; } = "\"";
    }

    public sealed class DateOnlyFormatSettings
    {
        public string Format { get; set; } = "yyyy-MM-dd";
        public string? Prefix { get; set; } = "\"";
        public string? Suffix { get; set; } = "\"";
    }

    private static Guid? DefaultUserIdResolver(IUserContext? userContext)
        => userContext?.GetCurrentUserId();

    private static (object? Id, string? Name) DefaultFkNameResolver(
        DbContext context,
        EntityEntry entry,
        IForeignKey fk,
        object? fkValue)
    {
        if (fkValue == null) return (null, null);
        return (fkValue, null);
    }

    private static bool DefaultEntitySelector(Type type)
        => typeof(Abstractions.IAuditEntity).IsAssignableFrom(type);
}
````

## File: tests/AuditLib.Tests/AuditInterceptorTests.cs
````csharp
using AuditLib.Abstractions;
using AuditLib.Domain;
using AuditLib.Infrastructure;
using AuditLib.Options;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace AuditLib.Tests;

public class AuditInterceptorTests : IDisposable
{
    private readonly TestDbContext _context;
    private readonly TestUserContext _userContext;
    private readonly AuditInterceptor _interceptor;
    private readonly AuditLibOptions _options;
    private static readonly DateTime FixedTimestamp = new(2026, 6, 28, 14, 0, 0, DateTimeKind.Utc);

    public AuditInterceptorTests()
    {
        _options = new AuditLibOptions
        {
            TimestampProvider = () => FixedTimestamp,
            AuditLogTableName = "AuditLogs"
        };
        _userContext = new TestUserContext(Guid.CreateVersion7());
        _interceptor = new AuditInterceptor(_options, _userContext);
        _context = TestDbContextFactory.Create(_interceptor);
    }

    public void Dispose()
    {
        _context.Database.CloseConnection();
        _context.Dispose();
    }

    [Fact]
    public void Should_set_CreatedAt_on_added_entity()
    {
        var entity = new TestAuditEntity { Name = "Test" };
        _context.TestAuditEntities.Add(entity);
        _context.SaveChanges();

        entity.CreatedAt.Should().Be(FixedTimestamp);
    }

    [Fact]
    public void Should_set_UpdatedAt_on_modified_entity()
    {
        var entity = new TestAuditEntity { Name = "Original", CreatedAt = FixedTimestamp };
        _context.TestAuditEntities.Add(entity);
        _context.SaveChanges();

        entity.Name = "Modified";
        _context.SaveChanges();

        entity.UpdatedAt.Should().Be(FixedTimestamp);
    }

    [Fact]
    public void Should_soft_delete_instead_of_physical_delete()
    {
        var entities = new List<TestAuditEntity>
        {
            new() { Name = "Entity1" },
            new() { Name = "Entity2" }
        };
        _context.TestAuditEntities.AddRange(entities);
        _context.SaveChanges();

        _context.TestAuditEntities.Remove(entities[0]);
        _context.SaveChanges();

        var all = _context.TestAuditEntities.IgnoreQueryFilters().ToList();
        all.Should().HaveCount(2);
        all.Should().ContainSingle(e => e.IsDeleted);
        all.Should().ContainSingle(e => !e.IsDeleted);
        all.First(e => e.IsDeleted).DeletedAt.Should().Be(FixedTimestamp);
    }

    [Fact]
    public void Should_create_audit_log_on_add()
    {
        var entity = new TestAuditEntity { Name = "NewEntity" };
        _context.TestAuditEntities.Add(entity);
        _context.SaveChanges();

        var logs = _context.Set<AuditLog>().ToList();
        logs.Should().ContainSingle();
        logs[0].EntityName.Should().Be(nameof(TestAuditEntity));
        logs[0].Action.Should().Be(_options.ActionAdded);
        logs[0].UserId.Should().Be(_userContext.GetCurrentUserId());
        logs[0].PreviousState.Should().BeNull();
        logs[0].CurrentState.Should().NotBeNull();
        logs[0].Diff.Should().Contain(_options.DiffAddedMessage);
    }

    [Fact]
    public void Should_create_audit_log_on_modify()
    {
        var entity = new TestAuditEntity { Name = "Original", Description = "Desc" };
        _context.TestAuditEntities.Add(entity);
        _context.SaveChanges();

        entity.Name = "Modified";
        _context.SaveChanges();

        var logs = _context.Set<AuditLog>().ToList();
        logs.Should().HaveCount(2);
        var modifyLog = logs.First(l => l.Action == _options.ActionModified);
        modifyLog.PreviousState.Should().NotBeNull();
        modifyLog.CurrentState.Should().NotBeNull();
        modifyLog.Diff.Should().Contain("Name");
        modifyLog.Diff.Should().Contain("Original");
        modifyLog.Diff.Should().Contain("Modified");
    }

    [Fact]
    public void Should_create_audit_log_on_delete()
    {
        var entity = new TestAuditEntity { Name = "ToDelete" };
        _context.TestAuditEntities.Add(entity);
        _context.SaveChanges();

        _context.TestAuditEntities.Remove(entity);
        _context.SaveChanges();

        var logs = _context.Set<AuditLog>().ToList();
        logs.Should().HaveCount(2);
        var deleteLog = logs.First(l => l.Action == _options.ActionDeleted);
        deleteLog.PreviousState.Should().NotBeNull();
        deleteLog.CurrentState.Should().BeNull();
        deleteLog.Diff.Should().Contain(_options.DiffDeletedMessage);
        deleteLog.PrimaryKey.Should().NotBeNull();
    }

    [Fact]
    public void Should_not_audit_non_audited_entities()
    {
        var entity = new TestNonAuditedEntity { Name = "NotTracked" };
        _context.TestNonAuditedEntities.Add(entity);
        _context.SaveChanges();

        entity.Name = "Changed";
        _context.SaveChanges();

        _context.TestNonAuditedEntities.Remove(entity);
        _context.SaveChanges();

        var logs = _context.Set<AuditLog>().ToList();
        logs.Should().BeEmpty();
    }

    [Fact]
    public void Should_detect_unchanged_root_with_added_child()
    {
        var parent = new TestParentEntity { Name = "Parent" };
        _context.TestParentEntities.Add(parent);
        _context.SaveChanges();

        var child = new TestChildEntity { Name = "Child", Parent = parent };
        _context.TestChildEntities.Add(child);
        _context.SaveChanges();

        var logs = _context.Set<AuditLog>().ToList();
        logs.Should().HaveCount(2);
        var parentModifyLog = logs.Last(l => l.Action == _options.ActionModified);
        parentModifyLog.EntityName.Should().Be(nameof(TestParentEntity));
        parentModifyLog.Diff.Should().Contain("Child");
    }

    [Fact]
    public void Should_track_owned_type_changes()
    {
        var parent = new TestParentEntity
        {
            Name = "Parent",
            Address = new TestAddress { Street = "Old St", City = "Old City", ZipCode = "12345" }
        };
        _context.TestParentEntities.Add(parent);
        _context.SaveChanges();

        parent.Address = new TestAddress { Street = "New St", City = "New City", ZipCode = "12345" };
        _context.SaveChanges();

        var logs = _context.Set<AuditLog>().ToList();
        logs.Should().HaveCount(2);
        var modifyLog = logs.Last(l => l.Action == _options.ActionModified);
        modifyLog.Diff.Should().Contain("Address");
        modifyLog.Diff.Should().Contain("Old St");
        modifyLog.Diff.Should().Contain("New St");
    }

    [Fact]
    public void Should_track_child_collections()
    {
        var parent = new TestParentEntity { Name = "Parent" };
        var child1 = new TestChildEntity { Name = "Child1", Parent = parent };
        var child2 = new TestChildEntity { Name = "Child2", Parent = parent };
        _context.TestParentEntities.Add(parent);
        _context.TestChildEntities.Add(child1);
        _context.TestChildEntities.Add(child2);
        _context.SaveChanges();

        var child3 = new TestChildEntity { Name = "Child3", Parent = parent };
        _context.TestChildEntities.Add(child3);
        _context.SaveChanges();

        var logs = _context.Set<AuditLog>().ToList();
        logs.Should().HaveCount(2);
        var parentModifyLog = logs.Last(l => l.Action == _options.ActionModified);
        parentModifyLog.Diff.Should().Contain("Child3");
    }

    [Fact]
    public void Should_not_create_audit_log_when_no_scalar_changes()
    {
        var entity = new TestAuditEntity { Name = "NoChange" };
        _context.TestAuditEntities.Add(entity);
        _context.SaveChanges();

        entity.Name = "NoChange";
        _context.SaveChanges();

        var logs = _context.Set<AuditLog>().ToList();
        logs.Should().ContainSingle();
    }

    [Fact]
    public void Should_use_custom_entity_selector()
    {
        var options = new AuditLibOptions
        {
            TimestampProvider = () => FixedTimestamp,
            EntitySelector = t => t == typeof(TestParentEntity)
        };
        var interceptor = new AuditInterceptor(options, _userContext);
        using var context = TestDbContextFactory.Create(interceptor);

        var parent = new TestParentEntity { Name = "Parent" };
        var child = new TestChildEntity { Name = "Child", Parent = parent };
        context.TestParentEntities.Add(parent);
        context.TestChildEntities.Add(child);
        context.SaveChanges();

        var logs = context.Set<AuditLog>().ToList();
        logs.Should().ContainSingle();
        logs[0].EntityName.Should().Be(nameof(TestParentEntity));
    }

    [Fact]
    public void Should_not_audit_when_tracking_disabled()
    {
        var options = new AuditLibOptions
        {
            TimestampProvider = () => FixedTimestamp,
            TrackModifiedEntities = false
        };
        var interceptor = new AuditInterceptor(options, _userContext);
        using var context = TestDbContextFactory.Create(interceptor);

        var entity = new TestAuditEntity { Name = "Test" };
        context.TestAuditEntities.Add(entity);
        context.SaveChanges();

        entity.Name = "Modified";
        context.SaveChanges();

        var logs = context.Set<AuditLog>().ToList();
        logs.Should().ContainSingle();
        logs[0].Action.Should().Be(options.ActionAdded);
    }

    [Fact]
    public void Should_resolve_user_id_via_user_context()
    {
        var userId = Guid.CreateVersion7();
        var userContext = new TestUserContext(userId);
        var interceptor = new AuditInterceptor(_options, userContext);
        using var context = TestDbContextFactory.Create(interceptor);

        var entity = new TestAuditEntity { Name = "Test" };
        context.TestAuditEntities.Add(entity);
        context.SaveChanges();

        var logs = context.Set<AuditLog>().ToList();
        logs.Should().ContainSingle();
        logs[0].UserId.Should().Be(userId);
    }

    [Fact]
    public void Should_support_custom_timestamp_provider()
    {
        var customTime = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var options = new AuditLibOptions { TimestampProvider = () => customTime };
        var interceptor = new AuditInterceptor(options, _userContext);
        using var context = TestDbContextFactory.Create(interceptor);

        var entity = new TestAuditEntity { Name = "Test" };
        context.TestAuditEntities.Add(entity);
        context.SaveChanges();

        entity.CreatedAt.Should().Be(customTime);
        var logs = context.Set<AuditLog>().ToList();
        logs[0].Timestamp.Should().Be(customTime);
    }

    [Fact]
    public void Should_not_create_audit_log_when_no_trackable_entities()
    {
        var nonAudited = new TestNonAuditedEntity { Name = "NotTracked" };
        _context.TestNonAuditedEntities.Add(nonAudited);
        _context.SaveChanges();

        var logs = _context.Set<AuditLog>().ToList();
        logs.Should().BeEmpty();
    }

    [Fact]
    public void Should_handle_batch_add()
    {
        var entities = Enumerable.Range(0, 10)
            .Select(i => new TestAuditEntity { Name = $"Entity{i}" })
            .ToList();

        _context.TestAuditEntities.AddRange(entities);
        _context.SaveChanges();

        var logs = _context.Set<AuditLog>().ToList();
        logs.Should().HaveCount(10);
        logs.Should().AllSatisfy(l => l.Action.Should().Be(_options.ActionAdded));
    }

    [Fact]
    public void Should_cascade_soft_delete_to_children()
    {
        var parent = new TestParentEntity { Name = "Parent" };
        var child1 = new TestChildEntity { Name = "Child1", Parent = parent };
        var child2 = new TestChildEntity { Name = "Child2", Parent = parent };
        _context.TestParentEntities.Add(parent);
        _context.TestChildEntities.Add(child1);
        _context.TestChildEntities.Add(child2);
        _context.SaveChanges();

        _context.TestParentEntities.Remove(parent);
        _context.SaveChanges();

        var children = _context.TestChildEntities.ToList();
        children.Should().BeEmpty();
    }
}

public class Notificacao : AuditEntity
{
    public string Descricao { get; set; } = string.Empty;
    public Paciente? Paciente { get; set; }
    public ICollection<DoencaNaoRara> DoencasNaoRaras { get; set; } = [];
}

public class Paciente
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public string Nome { get; set; } = string.Empty;
    public Guid NotificacaoId { get; set; }
    public Notificacao? Notificacao { get; set; }
}

public class DoencaNaoRara
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public string Nome { get; set; } = string.Empty;
    public Guid NotificacaoId { get; set; }
    public Notificacao? Notificacao { get; set; }
}

public class LogAuditavel : AuditEntity
{
    public string Valor { get; set; } = string.Empty;
    public Guid NotificacaoId { get; set; }
    public Notificacao? Notificacao { get; set; }
}

public class AggregateTestDbContext : DbContext
{
    public AggregateTestDbContext(DbContextOptions<AggregateTestDbContext> options) : base(options) { }

    public DbSet<Notificacao> Notificacoes => Set<Notificacao>();
    public DbSet<Paciente> Pacientes => Set<Paciente>();
    public DbSet<DoencaNaoRara> DoencasNaoRaras => Set<DoencaNaoRara>();
    public DbSet<LogAuditavel> LogsAuditaveis => Set<LogAuditavel>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Notificacao>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).ValueGeneratedNever();
            e.HasQueryFilter(x => !x.IsDeleted);
            e.HasOne(x => x.Paciente)
                .WithOne(x => x.Notificacao)
                .HasForeignKey<Paciente>(x => x.NotificacaoId)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasMany(x => x.DoencasNaoRaras)
                .WithOne(x => x.Notificacao)
                .HasForeignKey(x => x.NotificacaoId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Paciente>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).ValueGeneratedNever();
        });

        modelBuilder.Entity<DoencaNaoRara>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).ValueGeneratedNever();
        });

        modelBuilder.Entity<LogAuditavel>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).ValueGeneratedNever();
            e.HasQueryFilter(x => !x.IsDeleted);
            e.HasOne(x => x.Notificacao)
                .WithMany()
                .HasForeignKey(x => x.NotificacaoId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<AuditLog>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).ValueGeneratedNever();
            e.ToTable("AuditLogs");
        });
    }
}

public class AggregateRootTests : IDisposable
{
    private readonly AggregateTestDbContext _context;
    private readonly AuditInterceptor _interceptor;
    private readonly AuditLibOptions _options;
    private static readonly DateTime FixedTimestamp = new(2026, 6, 28, 14, 0, 0, DateTimeKind.Utc);
    private readonly Guid _userId = Guid.CreateVersion7();

    public AggregateRootTests()
    {
        _options = new AuditLibOptions
        {
            TimestampProvider = () => FixedTimestamp,
            AggregateRootMappings = new Dictionary<Type, Type>
            {
                { typeof(Paciente), typeof(Notificacao) },
                { typeof(DoencaNaoRara), typeof(Notificacao) }
            },
            DisplayNames = new Dictionary<string, string>
            {
                { "Notificacao", "Notificação" },
                { "DoencasNaoRaras", "Doenças Não Raras" },
                { "DoencaNaoRara", "Doença Não Rara" },
                { "Paciente", "Paciente" }
            },
            AuditLogTableName = "AuditLogs"
        };
        _interceptor = new AuditInterceptor(_options, new TestUserContext(_userId));
        _context = CreateContext(_interceptor);
    }

    public void Dispose()
    {
        _context.Database.CloseConnection();
        _context.Dispose();
    }

    private static AggregateTestDbContext CreateContext(AuditInterceptor interceptor)
    {
        var options = new DbContextOptionsBuilder<AggregateTestDbContext>()
            .UseSqlite("DataSource=:memory:")
            .AddInterceptors(interceptor)
            .Options;

        var context = new AggregateTestDbContext(options);
        context.Database.OpenConnection();
        context.Database.EnsureCreated();
        return context;
    }

    [Fact]
    public void Should_use_aggregate_root_name_when_child_is_modified()
    {
        var notificacao = new Notificacao { Descricao = "Teste" };
        var paciente = new Paciente { Nome = "João", Notificacao = notificacao };
        _context.Notificacoes.Add(notificacao);
        _context.Pacientes.Add(paciente);
        _context.SaveChanges();

        paciente.Nome = "Maria";
        _context.SaveChanges();

        var logs = _context.Set<AuditLog>().ToList();
        logs.Should().HaveCount(2);
        var modifyLog = logs.Last(l => l.Action == _options.ActionModified);
        modifyLog.EntityName.Should().Be("Notificação");
        modifyLog.PrimaryKey.Should().Be(notificacao.Id.ToString());
        modifyLog.Diff.Should().Contain("Paciente");
    }

    [Fact]
    public void Should_use_aggregate_root_name_when_child_is_deleted()
    {
        var notificacao = new Notificacao { Descricao = "Teste" };
        var doenca = new DoencaNaoRara { Nome = "Doença X", Notificacao = notificacao };
        _context.Notificacoes.Add(notificacao);
        _context.DoencasNaoRaras.Add(doenca);
        _context.SaveChanges();

        _context.DoencasNaoRaras.Remove(doenca);
        _context.SaveChanges();

        var logs = _context.Set<AuditLog>().ToList();
        logs.Should().HaveCount(2);
        var modifyLog = logs.Last(l => l.Action == _options.ActionModified);
        modifyLog.EntityName.Should().Be("Notificação");
        modifyLog.PrimaryKey.Should().Be(notificacao.Id.ToString());
    }

    [Fact]
    public void Should_use_display_names_in_diff()
    {
        var notificacao = new Notificacao { Descricao = "Teste" };
        var doenca = new DoencaNaoRara { Nome = "Doença X", Notificacao = notificacao };
        _context.Notificacoes.Add(notificacao);
        _context.DoencasNaoRaras.Add(doenca);
        _context.SaveChanges();

        var doenca2 = new DoencaNaoRara { Nome = "Doença Y", Notificacao = notificacao };
        _context.DoencasNaoRaras.Add(doenca2);
        _context.SaveChanges();

        var logs = _context.Set<AuditLog>().ToList();
        logs.Should().HaveCount(2);
        var modifyLog = logs.Last(l => l.Action == _options.ActionModified);
        modifyLog.Diff.Should().Contain("Doenças Não Raras");
        modifyLog.Diff.Should().Contain("Doença Y");
    }

    [Fact]
    public void Should_map_child_to_aggregate_root_in_entity_name()
    {
        var notificacao = new Notificacao { Descricao = "Teste" };
        var paciente = new Paciente { Nome = "João", Notificacao = notificacao };
        _context.Notificacoes.Add(notificacao);
        _context.Pacientes.Add(paciente);
        _context.SaveChanges();

        var logs = _context.Set<AuditLog>().ToList();
        logs.Should().ContainSingle();
        logs[0].EntityName.Should().Be("Notificação");
    }

    [Fact]
    public void Should_use_child_only_snapshot_by_default()
    {
        var notificacao = new Notificacao { Descricao = "Teste" };
        var paciente = new Paciente { Nome = "João", Notificacao = notificacao };
        var doenca = new DoencaNaoRara { Nome = "Doença X", Notificacao = notificacao };
        _context.Notificacoes.Add(notificacao);
        _context.Pacientes.Add(paciente);
        _context.DoencasNaoRaras.Add(doenca);
        _context.SaveChanges();

        paciente.Nome = "Maria";
        _context.SaveChanges();

        var logs = _context.Set<AuditLog>().ToList();
        logs.Should().ContainSingle(l =>
            l.Action == _options.ActionModified && l.EntityName == "Notificação");
        var modifyLog = logs.First(l => l.Action == _options.ActionModified && l.EntityName == "Notificação");
        modifyLog.EntityName.Should().Be("Notificação");
        // Child nao e IAuditEntity, entao entry = root (Notificacao)
        modifyLog.CurrentState.Should().Contain("Descricao");
        modifyLog.Diff.Should().Contain("Paciente");
    }

    [Fact]
    public void Should_use_full_root_snapshot_when_child_is_IAuditEntity()
    {
        var options = new AuditLibOptions
        {
            TimestampProvider = () => FixedTimestamp,
            AggregateRootMappings = new Dictionary<Type, Type>
            {
                { typeof(LogAuditavel), typeof(Notificacao) }
            },
            DisplayNames = new Dictionary<string, string> { { "Notificacao", "Notificação" } },
            AggregateSnapshot = AggregateSnapshotMode.FullRoot,
            AuditLogTableName = "AuditLogs"
        };
        var interceptor = new AuditInterceptor(options, new TestUserContext(_userId));
        using var context = CreateContext(interceptor);

        var notificacao = new Notificacao { Descricao = "Teste" };
        var log = new LogAuditavel { Valor = "A", Notificacao = notificacao };
        context.Notificacoes.Add(notificacao);
        context.LogsAuditaveis.Add(log);
        context.SaveChanges();

        log.Valor = "B";
        context.SaveChanges();

        var logs = context.Set<AuditLog>().ToList();
        var fullRootLogs = logs.Where(l =>
            l.Action == options.ActionModified && l.EntityName == "Notificação").ToList();
        fullRootLogs.Should().HaveCount(1, "found {0} modify logs for Notificação. All logs: {1}",
            fullRootLogs.Count,
            string.Join(" | ", logs.Select(l => $"{l.Action} {l.EntityName}")));
        fullRootLogs[0].CurrentState.Should().Contain("Descricao");
    }

    [Fact]
    public void Should_use_aggregate_root_display_formatter_when_child_is_IAuditEntity()
    {
        var options = new AuditLibOptions
        {
            TimestampProvider = () => FixedTimestamp,
            AggregateRootMappings = new Dictionary<Type, Type>
            {
                { typeof(LogAuditavel), typeof(Notificacao) }
            },
            AggregateRootDisplayFormatter = (root, child, defaultDisplay) =>
                $"Notificação (via {child.Metadata.ClrType.Name})",
            AuditLogTableName = "AuditLogs"
        };
        var interceptor = new AuditInterceptor(options, new TestUserContext(_userId));
        using var context = CreateContext(interceptor);

        var notificacao = new Notificacao { Descricao = "Teste" };
        var log = new LogAuditavel { Valor = "A", Notificacao = notificacao };
        context.Notificacoes.Add(notificacao);
        context.LogsAuditaveis.Add(log);
        context.SaveChanges();

        log.Valor = "B";
        context.SaveChanges();

        var logs = context.Set<AuditLog>().ToList();
        logs.Should().ContainSingle(l =>
            l.Action == options.ActionModified && l.EntityName == "Notificação (via LogAuditavel)");
    }

    [Fact]
    public void Should_generate_dual_logs_when_dual_audit_is_enabled()
    {
        var options = new AuditLibOptions
        {
            TimestampProvider = () => FixedTimestamp,
            AggregateRootMappings = new Dictionary<Type, Type>
            {
                { typeof(LogAuditavel), typeof(Notificacao) }
            },
            DisplayNames = new Dictionary<string, string>
            {
                { "Notificacao", "Notificação" },
                { "LogAuditavel", "Log Auditável" }
            },
            DualAuditForAggregates = true,
            AuditLogTableName = "AuditLogs"
        };
        var interceptor = new AuditInterceptor(options, new TestUserContext(_userId));
        using var context = CreateContext(interceptor);

        var notificacao = new Notificacao { Descricao = "Teste" };
        var log = new LogAuditavel { Valor = "A", Notificacao = notificacao };
        context.Notificacoes.Add(notificacao);
        context.LogsAuditaveis.Add(log);
        context.SaveChanges();

        log.Valor = "B";
        context.SaveChanges();

        var logs = context.Set<AuditLog>().ToList();
        var aggregateLogs = logs.Where(l => l.EntityName == "Notificação" && l.Action == options.ActionModified).ToList();
        var childLogs = logs.Where(l => l.EntityName == "Log Auditável" && l.Action == options.ActionModified).ToList();

        aggregateLogs.Should().HaveCount(1);
        childLogs.Should().HaveCount(1);
        childLogs[0].PrimaryKey.Should().Be(log.Id.ToString());
    }

    [Fact]
    public void Should_NOT_generate_dual_logs_when_dual_audit_is_disabled()
    {
        var options = new AuditLibOptions
        {
            TimestampProvider = () => FixedTimestamp,
            AggregateRootMappings = new Dictionary<Type, Type>
            {
                { typeof(LogAuditavel), typeof(Notificacao) }
            },
            DisplayNames = new Dictionary<string, string>
            {
                { "Notificacao", "Notificação" },
                { "LogAuditavel", "Log Auditável" }
            },
            DualAuditForAggregates = false,
            AuditLogTableName = "AuditLogs"
        };
        var interceptor = new AuditInterceptor(options, new TestUserContext(_userId));
        using var context = CreateContext(interceptor);

        var notificacao = new Notificacao { Descricao = "Teste" };
        var log = new LogAuditavel { Valor = "A", Notificacao = notificacao };
        context.Notificacoes.Add(notificacao);
        context.LogsAuditaveis.Add(log);
        context.SaveChanges();

        log.Valor = "B";
        context.SaveChanges();

        var logs = context.Set<AuditLog>().ToList();
        var childLogs = logs.Where(l => l.EntityName == "Log Auditável" && l.Action == options.ActionModified).ToList();
        childLogs.Should().BeEmpty();
    }
}
````

## File: README.md
````markdown
# AuditLib

Biblioteca de auditoria limpa e parametrizável para **EF Core (.NET 10)**. Gera automaticamente trilhas de auditoria com soft-delete, diff legível, resolução de FK, suporte a agregados e nomes amigáveis.

> Baseada na arquitetura de auditoria do projeto Sindras, extraída como uma lib reutilizável e parametrizável.

---

## Fluent API (Registrador)

Alternativa à configuração direta de propriedades. Use o `AuditConfigurationBuilder` para uma experiência fluente e com tipagem forte:

### Configuração Geral

```csharp
services.AddAuditLibWithInterceptor(builder =>
{
    builder
        .AuditLogTable("AuditoriaRegistro", schema: "audit")
        .UseTimestampProvider(() => TimeZoneInfo.ConvertTime(
            DateTime.UtcNow,
            TimeZoneInfo.FindSystemTimeZoneById("E. South America Standard Time")))
        .SoftDelete(true)
        .TrackAdded(true)
        .TrackModified(true)
        .TrackDeleted(true)
        .TrackUnchangedRootsWithChangedChildren(true)
        .SetActionAdded("Criado")
        .SetActionModified("Editado")
        .SetActionDeleted("Removido")
        .SetDiffAddedMessage("Novo registro adicionado.")
        .SetDiffDeletedMessage("Registro removido.")
        .SetDiffRemovedFormat("{0} excluídos(as):")
        .SetDiffAddedFormat("{0} incluídos(as):")
        .WithUserIdResolver(ctx => ctx?.GetCurrentUserId())
        .WithFkNameResolver((context, entry, fk, value) =>
        {
            // resolução customizada de FK para nome legível
            return (value, null);
        });
});
```

### Configuração por Entidade

```csharp
services.AddAuditLibWithInterceptor(builder =>
{
    builder.Entity<Notificacao>()
        .DisplayName("Notificação");

    builder.Entity<Paciente>()
        .MapToAggregateRoot<Notificacao>();

    builder.Entity<DoencaNaoRara>()
        .DisplayName("Doença Rara")
        .MapToAggregateRoot<Notificacao>();

    builder.Entity<Contrato>()
        .DisplayName("Contrato")
        .Property(c => c.Numero, "Número")
        .Property(c => c.Contratada, "Empresa Contratada")
        .Property(c => c.DataInicio, "Data de Início")
        .Property(c => c.Valor, "Valor do Contrato");
});
```

### Métodos disponíveis

#### `AuditConfigurationBuilder`

| Método | Descrição |
|---|---|
| `AuditLogTable(string, string?)` | Nome da tabela e schema |
| `UseTimestampProvider(Func<DateTime>)` | Provider de timestamp |
| `SoftDelete(bool)` | Habilitar soft-delete |
| `TrackAdded/Modified/Deleted(bool)` | Quais ações auditar |
| `TrackUnchangedRootsWithChangedChildren(bool)` | Detectar raízes com filhos alterados |
| `WithUserIdResolver(UserIdResolver)` | Resolvedor de usuário |
| `WithFkNameResolver(FkNameResolver)` | Resolvedor de FK |
| `WithEntitySelector(Func<Type, bool>)` | Seletor de entidades |
| `WithAggregateSnapshot(AggregateSnapshotMode)` | Modo de snapshot para agregados (`ChildOnly` ou `FullRoot`) |
| `WithAggregateRootDisplayFormatter(AggregateRootDisplayFormatter)` | Delegate para personalizar o display do agregado |
| `WithDualAuditForAggregates(bool)` | Gera log duplo (raiz + entidade original) para entidades com aggregate root mapping |
| `Entity<TEntity>()` | Configura entidade específica |
| `SetActionAdded/Modified/Deleted(string)` | Nomes das ações |
| `SetDiffAddedMessage/SetDiffDeletedMessage(string)` | Mensagens de diff |
| `SetDiffRemovedFormat/SetDiffAddedFormat(string)` | Formato de coleções |
| `SetNullDisplay/BoolTrueDisplay/BoolFalseDisplay(string)` | Display de valores |

#### `EntityConfigurationBuilder<TEntity>`

| Método | Descrição |
|---|---|
| `DisplayName(string)` | Nome amigável da entidade no diff |
| `MapToAggregateRoot<TRoot>()` | Mapeia para aggregate root |
| `Property(string, string)` | Nome amigável de propriedade (nome técnico) |
| `Property(Expression<Func<TEntity,TProperty>>, string)` | Nome amigável de propriedade (lambda) |
| `IgnoreProperty(string)` | Propriedade ignorada no diff |
- [Instalação](#instalação)
- [Visão Geral](#visão-geral)
- [Configuração Rápida](#configuração-rápida)
- [Entidades Auditáveis](#entidades-auditáveis)
- [Como Funciona](#como-funciona)
- [Parâmetros de Configuração](#parâmetros-de-configuração)
    - [Geral](#geral)
    - [Ações e Diffs](#ações-e-diffs)
    - [Display Names](#display-names)
    - [Agregados](#agregados)
    - [Serialização](#serialização)
- [Agregados e Nomes Amigáveis](#agregados-e-nomes-amigáveis)
- [Soft Delete](#soft-delete)
- [Consulta de Auditoria](#consulta-de-auditoria)
- [Exemplo Completo](#exemplo-completo)
- [Estrutura do Projeto](#estrutura-do-projeto)

---

## Instalação

```bash
dotnet add package AuditLib
```

Ou adicione manualmente ao seu `.csproj`:

```xml
<PackageReference Include="AuditLib" Version="1.0.0" />
```

---

## Visão Geral

A AuditLib intercepta chamadas `SaveChanges` do EF Core e automaticamente:

1. Gera **timestamps** (`CreatedAt`, `UpdatedAt`, `DeletedAt`) em entidades auditáveis
2. Aplica **soft-delete** (marca `IsDeleted = true` em vez de excluir fisicamente)
3. Registra um **log de auditoria** com:
   - Quem fez a alteração (via JWT ou customizável)
   - O que mudou (entidade, PK, ação)
   - Snapshot do **estado anterior** (JSON)
   - Snapshot do **estado atual** (JSON)
   - **Diff legível** comparando antes/depois
4. **Cascade soft-delete** respeitando comportamentos de FK (Restrict, Cascade, SetNull)
5. Suporte a **aggregate roots**: alterações em filhos são atribuídas à raiz
6. **Nomes amigáveis** para entidades, navegações e propriedades nos diffs

---

## Configuração Rápida

### 1. Configure o `DbContext`

```csharp
public class AppDbContext : DbContext
{
    private readonly AuditLibOptions _auditOptions;

    public AppDbContext(DbContextOptions<AppDbContext> options, AuditLibOptions auditOptions)
        : base(options)
    {
        _auditOptions = auditOptions;
    }

    // ... DbSets ...
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new AuditLogEntityTypeConfiguration(_auditOptions));
        
        // Filtro global para soft-delete (opcional, mas recomendado)
        foreach (var entity in modelBuilder.Model.GetEntityTypes()
            .Where(e => typeof(IAuditEntity).IsAssignableFrom(e.ClrType)))
        {
            modelBuilder.Entity(entity.ClrType)
                .HasQueryFilter(e => !EF.Property<bool>(e, "IsDeleted"));
        }
    }
}
```

### 2. Registre no DI

```csharp
builder.Services.AddAuditLibWithInterceptor(options =>
{
    options.TimestampProvider = () => 
        TimeZoneInfo.ConvertTime(DateTime.UtcNow, 
            TimeZoneInfo.FindSystemTimeZoneById("E. South America Standard Time"));
    
    options.AuditLogTableName = "AuditoriaRegistro";
    
    // Nomes amigáveis para entidades no diff
    options.DisplayNames["Notificacao"] = "Notificação";
    options.DisplayNames["DoencaNaoRara"] = "Doença Rara";
    
    // Nomes amigáveis para propriedades no diffs, agrupados por entidade
    options.PropertyDisplayNames["Contrato"] = new()
    {
        ["Numero"] = "Número",
        ["Contratada"] = "Empresa Contratada",
        ["DataInicio"] = "Data de Início",
        ["Valor"] = "Valor do Contrato",
    };
    
    // Aggregate roots (opcional)
    options.AggregateRootMappings[typeof(Paciente)] = typeof(Notificacao);
    options.AggregateRootMappings[typeof(DoencaNaoRara)] = typeof(Notificacao);
    
    // Resolvedor customizado de FK (opcional)
    options.FkNameResolver = (context, entry, fk, fkValue) =>
    {
        // Busca nome legível da entidade relacionada
        // por padrão só retorna (fkValue, null)
        return ResolveNome(context, fk, fkValue);
    };
});

// No DbContext, use o provedor de serviço para configurar o interceptor:
builder.Services.AddDbContext<AppDbContext>((sp, ob) =>
{
    ob.UseSqlServer(connectionString);
    ob.UseAuditInterceptor(sp); // adiciona o interceptor
});
```

### 3. Faça suas entidades herdarem de `AuditEntity`

```csharp
public class Usuario : AuditEntity
{
    public string Nome { get; set; }
    public string Email { get; set; }
}
```

Agora toda alteração em `Usuario` é automaticamente auditada.

---

## Entidades Auditáveis

### Interface `IAuditEntity`

```csharp
public interface IAuditEntity
{
    DateTime CreatedAt { get; set; }
    DateTime? UpdatedAt { get; set; }
    DateTime? DeletedAt { get; set; }
    bool IsDeleted { get; set; }
}
```

### Classe base `AuditEntity`

```csharp
public abstract class AuditEntity : IAuditEntity
{
    public Guid Id { get; init; } = Guid.CreateVersion7();
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public DateTime? DeletedAt { get; set; }
    public bool IsDeleted { get; set; }
}
```

Basta herdar de `AuditEntity` (ou implementar `IAuditEntity` manualmente) para que sua entidade seja automaticamente auditada.

---

## Como Funciona

### AuditInterceptor (SaveChangesInterceptor)

A cada `SaveChanges`, o interceptor:

1. **Detecta mudanças** no ChangeTracker
2. **Gerencia timestamps**: define `CreatedAt`, `UpdatedAt`, `DeletedAt`
3. **Aplica soft-delete** (se habilitado) — entidades "excluídas" são marcadas como `IsDeleted = true` 
4. **Identifica o usuário** através de 3 estratégias em cascata:
   - Claim `user_id` do JWT
   - `HttpContext.Items["CurrentUserId"]` 
   - Claim `sub` do JWT
5. **Identifica as raízes** dos agregados que mudaram:
   - Entidades com `Added`, `Modified` ou `Deleted` explícitos
   - Entidades `Unchanged` mas com filhos alterados (FK cascade)
6. **Gera snapshot** do estado anterior e atual (JSON)
7. **Gera diff legível** comparando os estados
8. **Registra o log** em `AuditLog`

### Exemplo de Diff

```
  - Nome: "João" -> "José"
  - UnidadeTratamento: "guid" (Hospital A) -> "guid" (Hospital B)
  - EnderecoAtual.Logradouro: "Rua A" -> "Rua B"
  
Medicamentos removidos(as):
  - Medicamento: Dipirona
  - Medicamento: Paracetamol

Medicamentos incluidos(as):
  - Medicamento: Ibuprofeno
```

---

## Parâmetros de Configuração

### Geral

| Propriedade | Tipo | Default | Descrição |
|---|---|---|---|
| `TimestampProvider` | `Func<DateTime>` | `DateTime.UtcNow` | Provider de data/hora. Use para timezone específico |
| `SoftDeleteEnabled` | `bool` | `true` | Habilitar/desabilitar soft-delete |
| `AuditLogTableName` | `string` | `"AuditLogs"` | Nome da tabela de auditoria |
| `AuditLogTableSchema` | `string?` | `null` | Schema da tabela |
| `TrackAddedEntities` | `bool` | `true` | Auditar entidades adicionadas |
| `TrackModifiedEntities` | `bool` | `true` | Auditar entidades modificadas |
| `TrackDeletedEntities` | `bool` | `true` | Auditar entidades excluídas |
| `TrackUnchangedRootsWithChangedChildren` | `bool` | `true` | Detectar raízes inalteradas com filhos alterados |
| `IgnoredProperties` | `HashSet<string>` | `CreatedAt, UpdatedAt, DeletedAt, IsDeleted, Version` | Propriedades ignoradas no snapshot/diff |
| `NavigationBackReferences` | `HashSet<string>` | vazio | Referências de navegação para ignorar em coleções filhas |
| `NamePropertyCandidates` | `string[]` | `Nome, NomeCompleto, Descricao, RazaoSocial` | Propriedades candidatas a nome legível |
| `EntitySelector` | `Func<Type, bool>` | `typeof(IAuditEntity)` | Filtro de quais tipos auditar |

### Ações e Diffs

| Propriedade | Tipo | Default | Descrição |
|---|---|---|---|
| `UserIdResolver` | `UserIdResolver` | `IUserContext?.GetCurrentUserId()` | Delegate para resolver userId |
| `FkNameResolver` | `FkNameResolver` | retorna só o ID | Delegate para resolver FK → `(id, nome)` |
| `ActionAdded` | `string` | `"Adicionar"` | Nome da ação "Adicionar" |
| `ActionModified` | `string` | `"Alterar"` | Nome da ação "Alterar" |
| `ActionDeleted` | `string` | `"Excluir"` | Nome da ação "Excluir" |
| `DiffAddedMessage` | `string` | `"Registro criado."` | Mensagem para registros adicionados |
| `DiffDeletedMessage` | `string` | `"Registro excluido."` | Mensagem para registros excluídos |
| `DiffRemovedFormat` | `string` | `"{0} removidos(as):"` | Formato para coleções removidas |
| `DiffAddedFormat` | `string` | `"{0} incluidos(as):"` | Formato para coleções incluídas |
| `NullDisplay` | `string` | `"null"` | Representação de null no diff |
| `BoolTrueDisplay` | `string` | `"true"` | Representação de true no diff |
| `BoolFalseDisplay` | `string` | `"false"` | Representação de false no diff |
| `DateTimeFormat` | `DateTimeFormatSettings` | formato `yyyy-MM-dd HH:mm:ss` com aspas | Formatação de DateTime no diff |
| `DateOnlyFormat` | `DateOnlyFormatSettings` | formato `yyyy-MM-dd` com aspas | Formatação de DateOnly no diff |

### Display Names

| Propriedade | Tipo | Default | Descrição |
|---|---|---|---|
| `DisplayNames` | `Dictionary<string, string>` | vazio | Nomes amigáveis para entidades e navegações. Ex: `"DoencaNaoRara" → "Doença Rara"` |
| `PropertyDisplayNames` | `Dictionary<string, Dictionary<string, string>>` | vazio | Nomes amigáveis para propriedades no diff, agrupados por entidade. Ex: `["Contrato"]["Numero"] → "Número"` |

### Agregados

| Propriedade | Tipo | Default | Descrição |
|---|---|---|---|
| `AggregateRootMappings` | `Dictionary<Type, Type>` | vazio | Mapeia tipos filhos para raiz do agregado. Ex: `typeof(Paciente) → typeof(Notificacao)` |
| `AggregateSnapshot` | `AggregateSnapshotMode` | `ChildOnly` | Modo de snapshot para agregados: `ChildOnly` (só o filho) ou `FullRoot` (agregado completo) |
| `AggregateRootDisplayFormatter` | `AggregateRootDisplayFormatter?` | `null` | Delegate para personalizar o display do agregado. Recebe `(rootEntry, childEntry, defaultDisplay)` e retorna a string exibida |
| `DualAuditForAggregates` | `bool` | `false` | Gera um segundo log para a entidade original quando mapeada para aggregate root |

### Serialização

| Propriedade | Tipo | Default | Descrição |
|---|---|---|---|
| `JsonSerializerOptions` | `JsonSerializerOptions` | camelCase, IgnoreCycles | Configuração do JSON para snapshots |

---

## Agregados e Nomes Amigáveis

### Mapeamento de Aggregate Roots

Quando uma entidade filha é alterada, o log de auditoria pode ser atribuído à raiz do agregado:

```csharp
options.AggregateRootMappings[typeof(Paciente)] = typeof(Notificacao);
options.AggregateRootMappings[typeof(DoencaNaoRara)] = typeof(Notificacao);
```

**Efeito**: uma alteração em `Paciente` gera um log com:
- `EntityName = "Notificacao"` (ou o display name configurado)
- `PrimaryKey = "id-da-notificacao"` (PK da raiz, não do paciente)
- O diff descreve o que mudou no paciente

### Nomes Amigáveis (Display Names)

Configure nomes legíveis para entidades, navegações e propriedades:

```csharp
options.DisplayNames["Notificacao"] = "Notificação";
options.DisplayNames["DoencaNaoRara"] = "Doença Rara";
options.DisplayNames["DoencasNaoRaras"] = "Doenças Raras";

options.PropertyDisplayNames["Contrato"] = new()
{
    ["Numero"] = "Número",
    ["Contratada"] = "Empresa Contratada",
    ["DataInicio"] = "Data de Início",
    ["Valor"] = "Valor do Contrato",
};
```

**Efeito** no diff:

```
Entidade: Notificação (em vez de "Notificacao")
Ação: Alterar
Diff:
  - Nome Completo: "João" -> "José"
  - Descrição: "antiga" -> "nova"
  
  Doenças Raras incluidos(as):
    - Doença Rara: Varicela
```

### Modo de Snapshot do Agregado

```csharp
// ChildOnly (padrão): snapshot apenas da entidade filha que mudou
options.AggregateSnapshot = AggregateSnapshotMode.ChildOnly;

// FullRoot: snapshot do agregado completo (raiz + todos os filhos)
options.AggregateSnapshot = AggregateSnapshotMode.FullRoot;
```

No modo `FullRoot`, quando um filho é alterado, o snapshot captura o estado completo da raiz (incluindo propriedades não alteradas), e o diff lista o que mudou nos filhos:

```
  // Desconhecido alterado:
    - Valor: "A" -> "B"
```

### Formatter Personalizado do Agregado

```csharp
options.AggregateRootDisplayFormatter = (rootEntry, childEntry, defaultDisplay) =>
    $"Notificação (via {childEntry.Metadata.ClrType.Name})";
```

Isto substitui o `EntityName` no log de auditoria. Ex:
```
EntityName = "Notificação (via LogAuditavel)"
```

### Dual Audit para Agregados

Por padrão, quando um filho mapeado para aggregate root é modificado, apenas um log é gerado (atribuído à raiz).  
Ative `DualAuditForAggregates` para gerar **dois logs**: um da raiz e um da entidade original.

```csharp
// No configure global:
options.DualAuditForAggregates = true;
// ou via Fluent API:
builder.WithDualAuditForAggregates(true);
```

**Efeito**: ao modificar `ItemContrato` (mapeado para `Contrato`):
- Log 1: `EntityName = "Contrato"` / PK = id-do-contrato — diff completo do agregado
- Log 2: `EntityName = "Item do Contrato"` / PK = id-do-item — diff escalar do item

Isto permite rastrear tanto a visão agregada quanto a visão individual da entidade que efetivamente mudou.

---

## Soft Delete

O soft-delete é aplicado automaticamente a entidades que implementam `IAuditEntity`:

- Em vez de excluir fisicamente (`DELETE`), o interceptor emite um `UPDATE`:
  - `IsDeleted = true`
  - `DeletedAt = timestamp`
- **Cascade**: respeita os comportamentos de FK:
  - `Restrict` / `NoAction`: valida se há dependentes, lança exceção se houver
  - `Cascade`: soft-deleta recursivamente os dependentes
  - `SetNull` / `ClientSetNull`: define FK como null nos dependentes
- **Owned types**: são preservados corretamente durante soft-delete

### Filtro Global (recomendado)

```csharp
protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    foreach (var entity in modelBuilder.Model.GetEntityTypes()
        .Where(e => typeof(IAuditEntity).IsAssignableFrom(e.ClrType)))
    {
        modelBuilder.Entity(entity.ClrType)
            .HasQueryFilter(e => !EF.Property<bool>(e, "IsDeleted"));
    }
}
```

---

## Consulta de Auditoria

### Serviço de Consulta

```csharp
public interface IAuditLogService
{
    // Busca logs por entidade
    Task<List<AuditLog>> GetByEntityAsync(
        string entityName, string primaryKey,
        int page = 1, int pageSize = 50,
        CancellationToken ct = default);

    // Contagem total de logs por entidade
    Task<long> GetCountByEntityAsync(
        string entityName, string primaryKey,
        CancellationToken ct = default);

    // Busca logs por usuário
    Task<List<AuditLog>> GetByUserAsync(
        Guid userId,
        int page = 1, int pageSize = 50,
        CancellationToken ct = default);

    // Contagem total de logs por usuário
    Task<long> GetCountByUserAsync(
        Guid userId,
        CancellationToken ct = default);
}
```

### Repositório

```csharp
public interface IAuditLogRepository
{
    IQueryable<AuditLog> Query();
    IQueryable<AuditLog> QueryByEntity(string entityName, string primaryKey);
    void Add(AuditLog auditLog);
}
```

Registrado automaticamente no DI via `AddAuditLib()`.

---

## Exemplo Completo

### Program.cs

```csharp
using AuditLib.Extensions;
using AuditLib.Infrastructure;
using AuditLib.Options;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddAuditLibWithInterceptor(options =>
{
    options.TimestampProvider = () => BrasilDateTime.Now;
    options.AuditLogTableName = "AuditoriaRegistro";
    options.SoftDeleteEnabled = true;

    // Aggregate roots
    options.AggregateRootMappings[typeof(Paciente)] = typeof(Notificacao);
    options.AggregateRootMappings[typeof(DoencaNaoRara)] = typeof(Notificacao);

    // Display names
    options.DisplayNames["Notificacao"] = "Notificação";
    options.DisplayNames["DoencaNaoRara"] = "Doença Rara";
    options.DisplayNames["DoencasNaoRaras"] = "Doenças Raras";

    options.PropertyDisplayNames["Contrato"] = new()
    {
        ["Numero"] = "Número",
        ["Contratada"] = "Empresa Contratada",
        ["DataInicio"] = "Data de Início",
        ["Valor"] = "Valor do Contrato",
    };
});

builder.Services.AddDbContext<AppDbContext>((sp, ob) =>
{
    ob.UseSqlServer(builder.Configuration.GetConnectionString("Default"));
    ob.UseAuditInterceptor(sp);
});

var app = builder.Build();
app.Run();
```

### AppDbContext.cs

```csharp
using AuditLib.Infrastructure;
using Microsoft.EntityFrameworkCore;

public class AppDbContext : DbContext
{
    private readonly AuditLibOptions _auditOptions;

    public AppDbContext(DbContextOptions<AppDbContext> options, AuditLibOptions auditOptions)
        : base(options)
    {
        _auditOptions = auditOptions;
    }

    public DbSet<Notificacao> Notificacoes => Set<Notificacao>();
    public DbSet<Paciente> Pacientes => Set<Paciente>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new AuditLogEntityTypeConfiguration(_auditOptions));

        foreach (var entity in modelBuilder.Model.GetEntityTypes()
            .Where(e => typeof(IAuditEntity).IsAssignableFrom(e.ClrType)))
        {
            modelBuilder.Entity(entity.ClrType)
                .HasQueryFilter(e => !EF.Property<bool>(e, "IsDeleted"));
        }

        modelBuilder.Entity<Notificacao>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).ValueGeneratedNever();
            e.HasQueryFilter(x => !x.IsDeleted);
            e.HasOne(x => x.Paciente)
                .WithOne(x => x.Notificacao)
                .HasForeignKey<Paciente>(x => x.NotificacaoId);
        });

        modelBuilder.Entity<Paciente>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).ValueGeneratedNever();
        });
    }
}
```

### Domain/Notificacao.cs

```csharp
using AuditLib.Domain;

public class Notificacao : AuditEntity
{
    public string Descricao { get; set; } = string.Empty;
    public Paciente? Paciente { get; set; }
}

public class Paciente
{
    public Guid Id { get; set; }
    public string Nome { get; set; } = string.Empty;
    public Guid NotificacaoId { get; set; }
    public Notificacao? Notificacao { get; set; }
}
```

---

## Estrutura do Projeto

```
AuditLib/
├── AuditLib.slnx
└── src/AuditLib/
    ├── AuditLib.csproj
    ├── Abstractions/
    │   ├── IAuditEntity.cs
    │   ├── IAuditLogRepository.cs
    │   ├── IAuditLogService.cs
    │   ├── IEntityNameResolver.cs
    │   └── IUserContext.cs
    ├── Domain/
    │   ├── AuditEntity.cs
    │   └── AuditLog.cs
    ├── Infrastructure/
    │   ├── AuditInterceptor.cs
    │   ├── AuditLogRepository.cs
    │   ├── AuditLogService.cs
    │   ├── AuditLogEntityTypeConfiguration.cs
    │   ├── DefaultUserContext.cs
    │   └── SoftDeleteHandler.cs
    ├── Options/
    │   └── AuditLibOptions.cs
    └── Extensions/
        └── ServiceCollectionExtensions.cs

AuditLib.Tests/ (32 testes unitários)
```

---

## Licença

MIT
````

## File: src/AuditLib/Infrastructure/AuditInterceptor.cs
````csharp
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

            var propDisplayName = ResolvePropertyName(child.Metadata.ClrType.Name, prop.Metadata.Name);
            if (value is bool b)
                parts.Add($"{propDisplayName}: {(b ? _options.BoolTrueDisplay : _options.BoolFalseDisplay)}");
            else
                parts.Add($"{propDisplayName}: {value}");
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
````

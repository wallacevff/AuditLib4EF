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
}

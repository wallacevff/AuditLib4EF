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

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

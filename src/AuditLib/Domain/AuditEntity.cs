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

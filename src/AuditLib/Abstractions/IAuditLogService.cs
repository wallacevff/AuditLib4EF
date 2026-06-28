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

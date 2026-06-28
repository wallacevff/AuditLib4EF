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

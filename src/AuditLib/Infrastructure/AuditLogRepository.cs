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

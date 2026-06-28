using AuditLib.Domain;

namespace AuditLib.Abstractions;

public interface IAuditLogRepository
{
    IQueryable<AuditLog> Query();
    IQueryable<AuditLog> QueryByEntity(string entityName, string primaryKey);
    void Add(AuditLog auditLog);
}

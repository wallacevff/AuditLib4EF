namespace AuditLib.Abstractions;

public interface IEntityNameResolver
{
    string GetEntityName(Type clrType);
}

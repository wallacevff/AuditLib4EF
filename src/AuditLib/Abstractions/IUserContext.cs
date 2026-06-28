namespace AuditLib.Abstractions;

public interface IUserContext
{
    Guid? GetCurrentUserId();
}

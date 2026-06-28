using AuditLib.Abstractions;

namespace AuditLib.Infrastructure;

public sealed class DefaultUserContext : IUserContext
{
    private readonly Func<Guid?> _resolver;

    public DefaultUserContext(Func<Guid?> resolver)
    {
        _resolver = resolver;
    }

    public Guid? GetCurrentUserId() => _resolver();
}

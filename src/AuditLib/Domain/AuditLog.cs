namespace AuditLib.Domain;

public sealed class AuditLog
{
    public Guid Id { get; init; } = Guid.CreateVersion7();
    public string EntityName { get; private set; }
    public string PrimaryKey { get; private set; }
    public string Action { get; private set; }
    public Guid? UserId { get; private set; }
    public DateTime Timestamp { get; private set; }
    public string? PreviousState { get; private set; }
    public string? CurrentState { get; private set; }
    public string? Diff { get; private set; }

    private AuditLog(
        string entityName,
        string primaryKey,
        string action,
        Guid? userId,
        DateTime timestamp,
        string? previousState,
        string? currentState,
        string? diff)
    {
        EntityName = entityName;
        PrimaryKey = primaryKey;
        Action = action;
        UserId = userId;
        Timestamp = timestamp;
        PreviousState = previousState;
        CurrentState = currentState;
        Diff = diff;
    }

    private AuditLog() 
    {
        EntityName = string.Empty;
        PrimaryKey = string.Empty;
        Action = string.Empty;
    }

    public static AuditLog Create(
        string entityName,
        string primaryKey,
        string action,
        Guid? userId,
        DateTime timestamp,
        string? previousState,
        string? currentState,
        string? diff) => new(
            entityName,
            primaryKey,
            action,
            userId,
            timestamp,
            previousState,
            currentState,
            diff);
}

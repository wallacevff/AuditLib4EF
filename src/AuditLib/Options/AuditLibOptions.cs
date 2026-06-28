using System.Text.Json;
using System.Text.Json.Serialization;
using AuditLib.Abstractions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata;

namespace AuditLib.Options;

public delegate Guid? UserIdResolver(IUserContext? userContext);

public delegate (object? Id, string? Name) FkNameResolver(
    DbContext context,
    EntityEntry entry,
    IForeignKey fk,
    object? fkValue);

public delegate string AggregateRootDisplayFormatter(
    EntityEntry rootEntry,
    EntityEntry childEntry,
    string defaultDisplay);

public enum AggregateSnapshotMode
{
    /// <summary>Snapshot apenas a entidade filha que mudou. EntityName e PK sao da raiz.</summary>
    ChildOnly,
    /// <summary>Snapshot o agregado completo (raiz + todos os filhos). EntityName e PK sao da raiz.</summary>
    FullRoot
}

public sealed class AuditLibOptions
{
    public static readonly AuditLibOptions Default = new();

    public Func<DateTime> TimestampProvider { get; set; } = () => DateTime.UtcNow;
    public bool SoftDeleteEnabled { get; set; } = true;
    public string AuditLogTableName { get; set; } = "AuditLogs";
    public string? AuditLogTableSchema { get; set; }
    public bool TrackAddedEntities { get; set; } = true;
    public bool TrackModifiedEntities { get; set; } = true;
    public bool TrackDeletedEntities { get; set; } = true;
    public bool TrackUnchangedRootsWithChangedChildren { get; set; } = true;
    public HashSet<string> IgnoredProperties { get; set; } =
    [
        "CreatedAt",
        "UpdatedAt",
        "DeletedAt",
        "IsDeleted",
        "Version"
    ];
    public HashSet<string> NavigationBackReferences { get; set; } = [];
    public string[] NamePropertyCandidates { get; set; } =
        ["Nome", "NomeCompleto", "Descricao", "RazaoSocial"];
    public JsonSerializerOptions JsonSerializerOptions { get; set; } = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
        ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles,
        WriteIndented = false
    };
    public UserIdResolver UserIdResolver { get; set; } = DefaultUserIdResolver;
    public FkNameResolver FkNameResolver { get; set; } = DefaultFkNameResolver;
    public Func<Type, bool> EntitySelector { get; set; } = DefaultEntitySelector;

    /// <summary>Maps child entity types to their aggregate root types. When a child changes, the audit log uses the root's EntityName and PK.</summary>
    public Dictionary<Type, Type> AggregateRootMappings { get; set; } = [];

    /// <summary>Controls snapshot behavior when aggregate root is resolved. ChildOnly = snapshot apenas o filho. FullRoot = snapshot o agregado completo.</summary>
    public AggregateSnapshotMode AggregateSnapshot { get; set; } = AggregateSnapshotMode.ChildOnly;

    /// <summary>Custom display formatter for aggregate root entity names. Receives (rootEntry, childEntry, defaultDisplay) and returns the display string. Example: "Notif #123 (via Paciente)"</summary>
    public AggregateRootDisplayFormatter? AggregateRootDisplayFormatter { get; set; }

    /// <summary>Friendly display names for entity type names and navigation names in diffs. Key: technical name, Value: display name.
    /// Example: "DoencaNaoRara" -> "Doença Rara", "Notificacao" -> "Notificação"</summary>
    public Dictionary<string, string> DisplayNames { get; set; } = [];

    /// <summary>Friendly display names for property/field names in diffs, grouped by entity.
    /// Key: entity name, Value: dictionary mapping property name -> display name.
    /// Example: ["Contrato"]["Numero"] -> "Número", ["Contrato"]["Valor"] -> "Valor do Contrato"</summary>
    public Dictionary<string, Dictionary<string, string>> PropertyDisplayNames { get; set; } = [];
    public string ActionAdded { get; set; } = "Adicionar";
    public string ActionModified { get; set; } = "Alterar";
    public string ActionDeleted { get; set; } = "Excluir";
    public string DiffAddedMessage { get; set; } = "Registro criado.";
    public string DiffDeletedMessage { get; set; } = "Registro excluido.";
    public string DiffRemovedFormat { get; set; } = "{0} removidos(as):";
    public string DiffAddedFormat { get; set; } = "{0} incluidos(as):";
    public string NullDisplay { get; set; } = "null";
    public string BoolTrueDisplay { get; set; } = "true";
    public string BoolFalseDisplay { get; set; } = "false";
    public DateTimeFormatSettings DateTimeFormat { get; set; } = new();
    public DateOnlyFormatSettings DateOnlyFormat { get; set; } = new();

    public sealed class DateTimeFormatSettings
    {
        public string Format { get; set; } = "yyyy-MM-dd HH:mm:ss";
        public string? Prefix { get; set; } = "\"";
        public string? Suffix { get; set; } = "\"";
    }

    public sealed class DateOnlyFormatSettings
    {
        public string Format { get; set; } = "yyyy-MM-dd";
        public string? Prefix { get; set; } = "\"";
        public string? Suffix { get; set; } = "\"";
    }

    private static Guid? DefaultUserIdResolver(IUserContext? userContext)
        => userContext?.GetCurrentUserId();

    private static (object? Id, string? Name) DefaultFkNameResolver(
        DbContext context,
        EntityEntry entry,
        IForeignKey fk,
        object? fkValue)
    {
        if (fkValue == null) return (null, null);
        return (fkValue, null);
    }

    private static bool DefaultEntitySelector(Type type)
        => typeof(Abstractions.IAuditEntity).IsAssignableFrom(type);
}

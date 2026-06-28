# AuditLib

Biblioteca de auditoria limpa e parametrizável para **EF Core (.NET 10)**. Gera automaticamente trilhas de auditoria com soft-delete, diff legível, resolução de FK, suporte a agregados e nomes amigáveis.

> Baseada na arquitetura de auditoria do projeto Sindras, extraída como uma lib reutilizável e parametrizável.

---

## Fluent API (Registrador)

Alternativa à configuração direta de propriedades. Use o `AuditConfigurationBuilder` para uma experiência fluente e com tipagem forte:

### Configuração Geral

```csharp
services.AddAuditLibWithInterceptor(builder =>
{
    builder
        .AuditLogTable("AuditoriaRegistro", schema: "audit")
        .UseTimestampProvider(() => TimeZoneInfo.ConvertTime(
            DateTime.UtcNow,
            TimeZoneInfo.FindSystemTimeZoneById("E. South America Standard Time")))
        .SoftDelete(true)
        .TrackAdded(true)
        .TrackModified(true)
        .TrackDeleted(true)
        .TrackUnchangedRootsWithChangedChildren(true)
        .SetActionAdded("Criado")
        .SetActionModified("Editado")
        .SetActionDeleted("Removido")
        .SetDiffAddedMessage("Novo registro adicionado.")
        .SetDiffDeletedMessage("Registro removido.")
        .SetDiffRemovedFormat("{0} excluídos(as):")
        .SetDiffAddedFormat("{0} incluídos(as):")
        .WithUserIdResolver(ctx => ctx?.GetCurrentUserId())
        .WithFkNameResolver((context, entry, fk, value) =>
        {
            // resolução customizada de FK para nome legível
            return (value, null);
        });
});
```

### Configuração por Entidade

```csharp
services.AddAuditLibWithInterceptor(builder =>
{
    builder.Entity<Notificacao>()
        .DisplayName("Notificação");

    builder.Entity<Paciente>()
        .MapToAggregateRoot<Notificacao>();

    builder.Entity<DoencaNaoRara>()
        .DisplayName("Doença Rara")
        .MapToAggregateRoot<Notificacao>();

    builder.Entity<Contrato>()
        .DisplayName("Contrato")
        .Property(c => c.Numero, "Número")
        .Property(c => c.Contratada, "Empresa Contratada")
        .Property(c => c.DataInicio, "Data de Início")
        .Property(c => c.Valor, "Valor do Contrato");
});
```

### Métodos disponíveis

#### `AuditConfigurationBuilder`

| Método | Descrição |
|---|---|
| `AuditLogTable(string, string?)` | Nome da tabela e schema |
| `UseTimestampProvider(Func<DateTime>)` | Provider de timestamp |
| `SoftDelete(bool)` | Habilitar soft-delete |
| `TrackAdded/Modified/Deleted(bool)` | Quais ações auditar |
| `TrackUnchangedRootsWithChangedChildren(bool)` | Detectar raízes com filhos alterados |
| `WithUserIdResolver(UserIdResolver)` | Resolvedor de usuário |
| `WithFkNameResolver(FkNameResolver)` | Resolvedor de FK |
| `WithEntitySelector(Func<Type, bool>)` | Seletor de entidades |
| `WithAggregateSnapshot(AggregateSnapshotMode)` | Modo de snapshot para agregados (`ChildOnly` ou `FullRoot`) |
| `WithAggregateRootDisplayFormatter(AggregateRootDisplayFormatter)` | Delegate para personalizar o display do agregado |
| `Entity<TEntity>()` | Configura entidade específica |
| `SetActionAdded/Modified/Deleted(string)` | Nomes das ações |
| `SetDiffAddedMessage/SetDiffDeletedMessage(string)` | Mensagens de diff |
| `SetDiffRemovedFormat/SetDiffAddedFormat(string)` | Formato de coleções |
| `SetNullDisplay/BoolTrueDisplay/BoolFalseDisplay(string)` | Display de valores |

#### `EntityConfigurationBuilder<TEntity>`

| Método | Descrição |
|---|---|
| `DisplayName(string)` | Nome amigável da entidade no diff |
| `MapToAggregateRoot<TRoot>()` | Mapeia para aggregate root |
| `Property(string, string)` | Nome amigável de propriedade (nome técnico) |
| `Property(Expression<Func<TEntity,TProperty>>, string)` | Nome amigável de propriedade (lambda) |
| `IgnoreProperty(string)` | Propriedade ignorada no diff |
- [Instalação](#instalação)
- [Visão Geral](#visão-geral)
- [Configuração Rápida](#configuração-rápida)
- [Entidades Auditáveis](#entidades-auditáveis)
- [Como Funciona](#como-funciona)
- [Parâmetros de Configuração](#parâmetros-de-configuração)
    - [Geral](#geral)
    - [Ações e Diffs](#ações-e-diffs)
    - [Display Names](#display-names)
    - [Agregados](#agregados)
    - [Serialização](#serialização)
- [Agregados e Nomes Amigáveis](#agregados-e-nomes-amigáveis)
- [Soft Delete](#soft-delete)
- [Consulta de Auditoria](#consulta-de-auditoria)
- [Exemplo Completo](#exemplo-completo)
- [Estrutura do Projeto](#estrutura-do-projeto)

---

## Instalação

```bash
dotnet add package AuditLib
```

Ou adicione manualmente ao seu `.csproj`:

```xml
<PackageReference Include="AuditLib" Version="1.0.0" />
```

---

## Visão Geral

A AuditLib intercepta chamadas `SaveChanges` do EF Core e automaticamente:

1. Gera **timestamps** (`CreatedAt`, `UpdatedAt`, `DeletedAt`) em entidades auditáveis
2. Aplica **soft-delete** (marca `IsDeleted = true` em vez de excluir fisicamente)
3. Registra um **log de auditoria** com:
   - Quem fez a alteração (via JWT ou customizável)
   - O que mudou (entidade, PK, ação)
   - Snapshot do **estado anterior** (JSON)
   - Snapshot do **estado atual** (JSON)
   - **Diff legível** comparando antes/depois
4. **Cascade soft-delete** respeitando comportamentos de FK (Restrict, Cascade, SetNull)
5. Suporte a **aggregate roots**: alterações em filhos são atribuídas à raiz
6. **Nomes amigáveis** para entidades, navegações e propriedades nos diffs

---

## Configuração Rápida

### 1. Configure o `DbContext`

```csharp
public class AppDbContext : DbContext
{
    // ... DbSets ...
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Aplica o mapeamento da tabela de auditoria
        var options = serviceProvider.GetRequiredService<AuditLibOptions>();
        modelBuilder.ApplyConfiguration(new AuditLogEntityTypeConfiguration(options));
        
        // Filtro global para soft-delete (opcional, mas recomendado)
        foreach (var entity in modelBuilder.Model.GetEntityTypes()
            .Where(e => typeof(IAuditEntity).IsAssignableFrom(e.ClrType)))
        {
            modelBuilder.Entity(entity.ClrType)
                .HasQueryFilter(e => !EF.Property<bool>(e, "IsDeleted"));
        }
    }
}
```

### 2. Registre no DI

```csharp
builder.Services.AddAuditLibWithInterceptor(options =>
{
    options.TimestampProvider = () => 
        TimeZoneInfo.ConvertTime(DateTime.UtcNow, 
            TimeZoneInfo.FindSystemTimeZoneById("E. South America Standard Time"));
    
    options.AuditLogTableName = "AuditoriaRegistro";
    
    // Nomes amigáveis para entidades no diff
    options.DisplayNames["Notificacao"] = "Notificação";
    options.DisplayNames["DoencaNaoRara"] = "Doença Rara";
    
    // Nomes amigáveis para propriedades no diffs, agrupados por entidade
    options.PropertyDisplayNames["Contrato"] = new()
    {
        ["Numero"] = "Número",
        ["Contratada"] = "Empresa Contratada",
        ["DataInicio"] = "Data de Início",
        ["Valor"] = "Valor do Contrato",
    };
    
    // Aggregate roots (opcional)
    options.AggregateRootMappings[typeof(Paciente)] = typeof(Notificacao);
    options.AggregateRootMappings[typeof(DoencaNaoRara)] = typeof(Notificacao);
    
    // Resolvedor customizado de FK (opcional)
    options.FkNameResolver = (context, entry, fk, fkValue) =>
    {
        // Busca nome legível da entidade relacionada
        // por padrão só retorna (fkValue, null)
        return ResolveNome(context, fk, fkValue);
    };
});

// No DbContext, use o provedor de serviço para configurar o interceptor:
builder.Services.AddDbContext<AppDbContext>((sp, ob) =>
{
    ob.UseSqlServer(connectionString);
    ob.UseAuditInterceptor(sp); // adiciona o interceptor
});
```

### 3. Faça suas entidades herdarem de `AuditEntity`

```csharp
public class Usuario : AuditEntity
{
    public string Nome { get; set; }
    public string Email { get; set; }
}
```

Agora toda alteração em `Usuario` é automaticamente auditada.

---

## Entidades Auditáveis

### Interface `IAuditEntity`

```csharp
public interface IAuditEntity
{
    DateTime CreatedAt { get; set; }
    DateTime? UpdatedAt { get; set; }
    DateTime? DeletedAt { get; set; }
    bool IsDeleted { get; set; }
}
```

### Classe base `AuditEntity`

```csharp
public abstract class AuditEntity : IAuditEntity
{
    public Guid Id { get; init; } = Guid.CreateVersion7();
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public DateTime? DeletedAt { get; set; }
    public bool IsDeleted { get; set; }
}
```

Basta herdar de `AuditEntity` (ou implementar `IAuditEntity` manualmente) para que sua entidade seja automaticamente auditada.

---

## Como Funciona

### AuditInterceptor (SaveChangesInterceptor)

A cada `SaveChanges`, o interceptor:

1. **Detecta mudanças** no ChangeTracker
2. **Gerencia timestamps**: define `CreatedAt`, `UpdatedAt`, `DeletedAt`
3. **Aplica soft-delete** (se habilitado) — entidades "excluídas" são marcadas como `IsDeleted = true` 
4. **Identifica o usuário** através de 3 estratégias em cascata:
   - Claim `user_id` do JWT
   - `HttpContext.Items["CurrentUserId"]` 
   - Claim `sub` do JWT
5. **Identifica as raízes** dos agregados que mudaram:
   - Entidades com `Added`, `Modified` ou `Deleted` explícitos
   - Entidades `Unchanged` mas com filhos alterados (FK cascade)
6. **Gera snapshot** do estado anterior e atual (JSON)
7. **Gera diff legível** comparando os estados
8. **Registra o log** em `AuditLog`

### Exemplo de Diff

```
  - Nome: "João" -> "José"
  - UnidadeTratamento: "guid" (Hospital A) -> "guid" (Hospital B)
  - EnderecoAtual.Logradouro: "Rua A" -> "Rua B"
  
Medicamentos removidos(as):
  - Medicamento: Dipirona
  - Medicamento: Paracetamol

Medicamentos incluidos(as):
  - Medicamento: Ibuprofeno
```

---

## Parâmetros de Configuração

### Geral

| Propriedade | Tipo | Default | Descrição |
|---|---|---|---|
| `TimestampProvider` | `Func<DateTime>` | `DateTime.UtcNow` | Provider de data/hora. Use para timezone específico |
| `SoftDeleteEnabled` | `bool` | `true` | Habilitar/desabilitar soft-delete |
| `AuditLogTableName` | `string` | `"AuditLogs"` | Nome da tabela de auditoria |
| `AuditLogTableSchema` | `string?` | `null` | Schema da tabela |
| `TrackAddedEntities` | `bool` | `true` | Auditar entidades adicionadas |
| `TrackModifiedEntities` | `bool` | `true` | Auditar entidades modificadas |
| `TrackDeletedEntities` | `bool` | `true` | Auditar entidades excluídas |
| `TrackUnchangedRootsWithChangedChildren` | `bool` | `true` | Detectar raízes inalteradas com filhos alterados |
| `IgnoredProperties` | `HashSet<string>` | `CreatedAt, UpdatedAt, DeletedAt, IsDeleted, Version` | Propriedades ignoradas no snapshot/diff |
| `NavigationBackReferences` | `HashSet<string>` | vazio | Referências de navegação para ignorar em coleções filhas |
| `NamePropertyCandidates` | `string[]` | `Nome, NomeCompleto, Descricao, RazaoSocial` | Propriedades candidatas a nome legível |
| `EntitySelector` | `Func<Type, bool>` | `typeof(IAuditEntity)` | Filtro de quais tipos auditar |

### Ações e Diffs

| Propriedade | Tipo | Default | Descrição |
|---|---|---|---|
| `UserIdResolver` | `UserIdResolver` | `IUserContext?.GetCurrentUserId()` | Delegate para resolver userId |
| `FkNameResolver` | `FkNameResolver` | retorna só o ID | Delegate para resolver FK → `(id, nome)` |
| `ActionAdded` | `string` | `"Adicionar"` | Nome da ação "Adicionar" |
| `ActionModified` | `string` | `"Alterar"` | Nome da ação "Alterar" |
| `ActionDeleted` | `string` | `"Excluir"` | Nome da ação "Excluir" |
| `DiffAddedMessage` | `string` | `"Registro criado."` | Mensagem para registros adicionados |
| `DiffDeletedMessage` | `string` | `"Registro excluido."` | Mensagem para registros excluídos |
| `DiffRemovedFormat` | `string` | `"{0} removidos(as):"` | Formato para coleções removidas |
| `DiffAddedFormat` | `string` | `"{0} incluidos(as):"` | Formato para coleções incluídas |
| `NullDisplay` | `string` | `"null"` | Representação de null no diff |
| `BoolTrueDisplay` | `string` | `"true"` | Representação de true no diff |
| `BoolFalseDisplay` | `string` | `"false"` | Representação de false no diff |
| `DateTimeFormat` | `DateTimeFormatSettings` | formato `yyyy-MM-dd HH:mm:ss` com aspas | Formatação de DateTime no diff |
| `DateOnlyFormat` | `DateOnlyFormatSettings` | formato `yyyy-MM-dd` com aspas | Formatação de DateOnly no diff |

### Display Names

| Propriedade | Tipo | Default | Descrição |
|---|---|---|---|
| `DisplayNames` | `Dictionary<string, string>` | vazio | Nomes amigáveis para entidades e navegações. Ex: `"DoencaNaoRara" → "Doença Rara"` |
| `PropertyDisplayNames` | `Dictionary<string, Dictionary<string, string>>` | vazio | Nomes amigáveis para propriedades no diff, agrupados por entidade. Ex: `["Contrato"]["Numero"] → "Número"` |

### Agregados

| Propriedade | Tipo | Default | Descrição |
|---|---|---|---|
| `AggregateRootMappings` | `Dictionary<Type, Type>` | vazio | Mapeia tipos filhos para raiz do agregado. Ex: `typeof(Paciente) → typeof(Notificacao)` |
| `AggregateSnapshot` | `AggregateSnapshotMode` | `ChildOnly` | Modo de snapshot para agregados: `ChildOnly` (só o filho) ou `FullRoot` (agregado completo) |
| `AggregateRootDisplayFormatter` | `AggregateRootDisplayFormatter?` | `null` | Delegate para personalizar o display do agregado. Recebe `(rootEntry, childEntry, defaultDisplay)` e retorna a string exibida |

### Serialização

| Propriedade | Tipo | Default | Descrição |
|---|---|---|---|
| `JsonSerializerOptions` | `JsonSerializerOptions` | camelCase, IgnoreCycles | Configuração do JSON para snapshots |

---

## Agregados e Nomes Amigáveis

### Mapeamento de Aggregate Roots

Quando uma entidade filha é alterada, o log de auditoria pode ser atribuído à raiz do agregado:

```csharp
options.AggregateRootMappings[typeof(Paciente)] = typeof(Notificacao);
options.AggregateRootMappings[typeof(DoencaNaoRara)] = typeof(Notificacao);
```

**Efeito**: uma alteração em `Paciente` gera um log com:
- `EntityName = "Notificacao"` (ou o display name configurado)
- `PrimaryKey = "id-da-notificacao"` (PK da raiz, não do paciente)
- O diff descreve o que mudou no paciente

### Nomes Amigáveis (Display Names)

Configure nomes legíveis para entidades, navegações e propriedades:

```csharp
options.DisplayNames["Notificacao"] = "Notificação";
options.DisplayNames["DoencaNaoRara"] = "Doença Rara";
options.DisplayNames["DoencasNaoRaras"] = "Doenças Raras";

options.PropertyDisplayNames["Contrato"] = new()
{
    ["Numero"] = "Número",
    ["Contratada"] = "Empresa Contratada",
    ["DataInicio"] = "Data de Início",
    ["Valor"] = "Valor do Contrato",
};
```

**Efeito** no diff:

```
Entidade: Notificação (em vez de "Notificacao")
Ação: Alterar
Diff:
  - Nome Completo: "João" -> "José"
  - Descrição: "antiga" -> "nova"
  
  Doenças Raras incluidos(as):
    - Doença Rara: Varicela
```

### Modo de Snapshot do Agregado

```csharp
// ChildOnly (padrão): snapshot apenas da entidade filha que mudou
options.AggregateSnapshot = AggregateSnapshotMode.ChildOnly;

// FullRoot: snapshot do agregado completo (raiz + todos os filhos)
options.AggregateSnapshot = AggregateSnapshotMode.FullRoot;
```

No modo `FullRoot`, quando um filho é alterado, o snapshot captura o estado completo da raiz (incluindo propriedades não alteradas), e o diff lista o que mudou nos filhos:

```
  // Desconhecido alterado:
    - Valor: "A" -> "B"
```

### Formatter Personalizado do Agregado

```csharp
options.AggregateRootDisplayFormatter = (rootEntry, childEntry, defaultDisplay) =>
    $"Notificação (via {childEntry.Metadata.ClrType.Name})";
```

Isto substitui o `EntityName` no log de auditoria. Ex:
```
EntityName = "Notificação (via LogAuditavel)"
```

---

## Soft Delete

O soft-delete é aplicado automaticamente a entidades que implementam `IAuditEntity`:

- Em vez de excluir fisicamente (`DELETE`), o interceptor emite um `UPDATE`:
  - `IsDeleted = true`
  - `DeletedAt = timestamp`
- **Cascade**: respeita os comportamentos de FK:
  - `Restrict` / `NoAction`: valida se há dependentes, lança exceção se houver
  - `Cascade`: soft-deleta recursivamente os dependentes
  - `SetNull` / `ClientSetNull`: define FK como null nos dependentes
- **Owned types**: são preservados corretamente durante soft-delete

### Filtro Global (recomendado)

```csharp
protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    foreach (var entity in modelBuilder.Model.GetEntityTypes()
        .Where(e => typeof(IAuditEntity).IsAssignableFrom(e.ClrType)))
    {
        modelBuilder.Entity(entity.ClrType)
            .HasQueryFilter(e => !EF.Property<bool>(e, "IsDeleted"));
    }
}
```

---

## Consulta de Auditoria

### Serviço de Consulta

```csharp
public interface IAuditLogService
{
    // Busca logs por entidade
    Task<List<AuditLog>> GetByEntityAsync(
        string entityName, string primaryKey,
        int page = 1, int pageSize = 50,
        CancellationToken ct = default);

    // Contagem total de logs por entidade
    Task<long> GetCountByEntityAsync(
        string entityName, string primaryKey,
        CancellationToken ct = default);

    // Busca logs por usuário
    Task<List<AuditLog>> GetByUserAsync(
        Guid userId,
        int page = 1, int pageSize = 50,
        CancellationToken ct = default);

    // Contagem total de logs por usuário
    Task<long> GetCountByUserAsync(
        Guid userId,
        CancellationToken ct = default);
}
```

### Repositório

```csharp
public interface IAuditLogRepository
{
    IQueryable<AuditLog> Query();
    IQueryable<AuditLog> QueryByEntity(string entityName, string primaryKey);
    void Add(AuditLog auditLog);
}
```

Registrado automaticamente no DI via `AddAuditLib()`.

---

## Exemplo Completo

### Program.cs

```csharp
using AuditLib.Extensions;
using AuditLib.Infrastructure;
using AuditLib.Options;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddAuditLibWithInterceptor(options =>
{
    options.TimestampProvider = () => BrasilDateTime.Now;
    options.AuditLogTableName = "AuditoriaRegistro";
    options.SoftDeleteEnabled = true;

    // Aggregate roots
    options.AggregateRootMappings[typeof(Paciente)] = typeof(Notificacao);
    options.AggregateRootMappings[typeof(DoencaNaoRara)] = typeof(Notificacao);

    // Display names
    options.DisplayNames["Notificacao"] = "Notificação";
    options.DisplayNames["DoencaNaoRara"] = "Doença Rara";
    options.DisplayNames["DoencasNaoRaras"] = "Doenças Raras";

    options.PropertyDisplayNames["Contrato"] = new()
    {
        ["Numero"] = "Número",
        ["Contratada"] = "Empresa Contratada",
        ["DataInicio"] = "Data de Início",
        ["Valor"] = "Valor do Contrato",
    };
});

builder.Services.AddDbContext<AppDbContext>((sp, ob) =>
{
    ob.UseSqlServer(builder.Configuration.GetConnectionString("Default"));
    ob.UseAuditInterceptor(sp);
});

var app = builder.Build();
app.Run();
```

### AppDbContext.cs

```csharp
using AuditLib.Infrastructure;
using Microsoft.EntityFrameworkCore;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Notificacao> Notificacoes => Set<Notificacao>();
    public DbSet<Paciente> Pacientes => Set<Paciente>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(
            new AuditLogEntityTypeConfiguration(AuditLibOptions.Default));

        foreach (var entity in modelBuilder.Model.GetEntityTypes()
            .Where(e => typeof(IAuditEntity).IsAssignableFrom(e.ClrType)))
        {
            modelBuilder.Entity(entity.ClrType)
                .HasQueryFilter(e => !EF.Property<bool>(e, "IsDeleted"));
        }

        modelBuilder.Entity<Notificacao>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).ValueGeneratedNever();
            e.HasQueryFilter(x => !x.IsDeleted);
            e.HasOne(x => x.Paciente)
                .WithOne(x => x.Notificacao)
                .HasForeignKey<Paciente>(x => x.NotificacaoId);
        });

        modelBuilder.Entity<Paciente>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).ValueGeneratedNever();
        });
    }
}
```

### Domain/Notificacao.cs

```csharp
using AuditLib.Domain;

public class Notificacao : AuditEntity
{
    public string Descricao { get; set; } = string.Empty;
    public Paciente? Paciente { get; set; }
}

public class Paciente
{
    public Guid Id { get; set; }
    public string Nome { get; set; } = string.Empty;
    public Guid NotificacaoId { get; set; }
    public Notificacao? Notificacao { get; set; }
}
```

---

## Estrutura do Projeto

```
AuditLib/
├── AuditLib.slnx
└── src/AuditLib/
    ├── AuditLib.csproj
    ├── Abstractions/
    │   ├── IAuditEntity.cs
    │   ├── IAuditLogRepository.cs
    │   ├── IAuditLogService.cs
    │   ├── IEntityNameResolver.cs
    │   └── IUserContext.cs
    ├── Domain/
    │   ├── AuditEntity.cs
    │   └── AuditLog.cs
    ├── Infrastructure/
    │   ├── AuditInterceptor.cs
    │   ├── AuditLogRepository.cs
    │   ├── AuditLogService.cs
    │   ├── AuditLogEntityTypeConfiguration.cs
    │   ├── DefaultUserContext.cs
    │   └── SoftDeleteHandler.cs
    ├── Options/
    │   └── AuditLibOptions.cs
    └── Extensions/
        └── ServiceCollectionExtensions.cs

AuditLib.Tests/ (32 testes unitários)
```

---

## Licença

MIT

---
name: readme
description: Mantem o README.md sincronizado automaticamente a cada mudanca no codigo fonte da lib.
---

# Skill: readme

Este projeto possui um `README.md` na raiz que deve refletir fielmente a API publica, opcoes de configuracao e comportamento da lib.

## Quando executar

Sempre que houver alteracao em arquivos `.cs` dentro de `src/AuditLib/` (excluindo testes), ao final da tarefa voce DEVE verificar e atualizar o `README.md` se necessario.

## O que verificar

### 1. Tabela de parametros (`AuditLibOptions`)

Localizada na secao "Parametros de Configuracao". Cada propriedade publica de `AuditLibOptions` deve ter uma linha na tabela com:

| Propriedade | Tipo | Default | Descricao |

Se uma propriedade foi adicionada, removida, renomeada ou teve seu tipo/default alterados, **atualize a tabela**.

### 2. Secao Fluent API

Localizada na secao "Fluent API (Registrador)". Verifique:

- `AuditConfigurationBuilder`: cada metodo fluent deve estar documentado na tabela de metodos.
- `EntityConfigurationBuilder<TEntity>`: cada metodo fluent deve estar documentado.
- Se um metodo foi adicionado/removido/renomeado, **atualize a documentacao**.

### 3. Secao "Agregados e Nomes Amigaveis"

Se `AggregateRootMappings`, `DisplayNames` ou `PropertyDisplayNames` forem alterados, verifique se os exemplos de uso ainda estao corretos.

### 4. Secao "Exemplo Completo"

Verifique se o codigo de exemplo (`Program.cs`, `AppDbContext.cs`, entidades) ainda compila e reflete a API atual. Se houve mudanca no contrato publico (metodos, parametros obrigatorios), **atualize os exemplos**.

### 5. Secao "Visao Geral" / "Como Funciona"

Se o comportamento central da lib mudou (ex: novo recurso, mudanca no fluxo do interceptor), **atualize a descricao textual**.

## Formato

Mantenha o formato existente do README:

- Tabelas com `|` pipes
- Codigo em blocos com ```csharp
- Secoes com `##` e subsecoes com `###`
- Indice no topo atualizado se novas secoes forem adicionadas ou renomeadas

## Excecoes

Nao e necessario atualizar o README quando:

- Apenas arquivos de teste foram alterados
- Apenas whitespace, comentarios ou refatoracao interna sem mudanca na API publica
- Mudancas em `AuditLog.cs` ou `AuditEntity.cs` que nao afetem a interface publica
- Mudancas em `SoftDeleteHandler.cs` ou `AuditLogRepository.cs` que nao afetem o DI ou comportamento visivel

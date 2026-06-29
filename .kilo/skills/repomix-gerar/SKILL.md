---
name: repomix-gerar
description: Gera o repomix-output.md empacotando todo o codigo fonte para leitura por IA. Executar SEMPRE antes de explorar o codigo via repomix.
---

# Skill: repomix-gerar

## Quando executar

Sempre que houver mudanca em arquivos `.cs`, `.csproj`, `.slnx`, `.json` ou `.md` dentro de `src/` ou `tests/`, execute o repomix para manter o `repomix-output.md` atualizado.

## Comando

```bash
repomix
```

O comando usa o arquivo `repomix.config.json` na raiz do projeto, que ja esta configurado com:

- **Saida**: `repomix-output.md` (markdown, melhor para leitura por IA)
- **Estilo**: `markdown`
- **Ignore**: `bin/`, `obj/`, `*.nupkg`, `.idea/`, `.kilo/`, `packages/`, `.vs/`, `repomix-output.md`, `repomix.config.json`
- **Segurança**: verificacao de credenciais/API keys ativada
- **Git**: ordenacao por alteracoes recentes

## Verificacao pos-execucao

Apos gerar, confirme que o arquivo foi criado:

```bash
ls -lh repomix-output.md
```

O arquivo deve conter o resumo dos arquivos, a estrutura de diretorios e o conteudo completo de todos os arquivos de codigo fonte.

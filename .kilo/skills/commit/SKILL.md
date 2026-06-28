---
name: commit
description: Instrucoes para criar commits semanticos em PT-BR com icone, seguindo o padrao Sindras.
---

# Skill: commit

Este projeto adota commits semanticos em PT-BR com icone.

## Formato obrigatorio

Use sempre o formato:

`<icone> Sindras - <categoria>: <descricao>`

## Categorias aceitas

- `chore` (🧹)
- `feat` (✨)
- `bugfix` (🐛)
- `docs` (📚)
- `test` (🧪)
- `build` (⚙️)
- `ci` (🔧)
- `deps` (📦)
- `perf` (🚀)
- `remove` (🔥)
- `style` (🎨)
- `refactor` (♻️)

## Exemplos

- `🧹 Sindras - chore: Criar Seeders`
- `✨ Sindras - feat: Cadastro de usuario`
- `🐛 Sindras - bugfix: Resolvendo problema de autenticacao`
- `📚 Sindras - docs: Atualizando guia de usuario`
- `🧪 Sindras - test: Criar testes de integracao para autenticacao`
- `⚙️ Sindras - build: Ajustar pipeline de build`
- `🔧 Sindras - ci: Atualizar workflow de CI`
- `📦 Sindras - deps: Atualizar dependencias do Angular`
- `🚀 Sindras - perf: Melhorar performance da listagem de usuarios`
- `🔥 Sindras - remove: Remover codigo legado de autenticacao`
- `🎨 Sindras - style: Padronizar formatacao do projeto`
- `♻️ Sindras - refactor: Refatorando o servico de localizacao`

## Boas praticas

- Escreva a descricao de forma objetiva, no infinitivo ou substantivo de acao.
- Descreva o motivo principal da mudanca, nao apenas o arquivo alterado.
- Evite mensagens genericas como "ajustes" ou "correcao" sem contexto.

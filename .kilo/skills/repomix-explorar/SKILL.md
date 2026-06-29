---
name: repomix-explorar
description: Explora o codigo fonte usando o repomix-output.md em vez de navegar nos arquivos individualmente. Leia o repomix primeiro e depois use-o como contexto unico.
---

# Skill: repomix-explorar

## Objetivo

Evitar navegar arquivo por arquivo quando se precisa de uma visao geral do codigo fonte. Em vez disso, usa o `repomix-output.md` como fonte unica de contexto.

## Fluxo

### 1. Verificar se o repomix esta atualizado

Veja se o `repomix-output.md` existe e se sua data de modificacao e mais recente que a ultima mudanca nos fontes:

```bash
ls -lh repomix-output.md
git log -1 --format="%ct %s" -- src/ tests/
```

Se o repomix estiver desatualizado (mais antigo que os ultimos commits), execute a skill `repomix-gerar` primeiro.

### 2. Ler apenas este arquivo

Nao abra arquivos `.cs` individualmente. Use apenas o `repomix-output.md` para:

- Entender a estrutura do projeto
- Buscar por classes, metodos ou padroes especificos
- Analisar fluxos de codigo
- Verificar assinaturas de metodos e interfaces

### 3. Quando precisar de detalhes alem do repomix

Se o `repomix-output.md` nao for suficiente (ex: diff nao commitado, ou precisa ver o estado atual de um arquivo especifico que foi modificado), ai sim leia o arquivo individualmente.

## Excecoes

Nao use o repomix quando:

- A tarefa envolver apenas 1-2 arquivos pequenos
- Precisar ver o estado atual de arquivos nao commitados (`git diff`)
- Estiver depurando um erro de compilacao especifico que requer o .csproj ou props atual
- O repomix-output.md nao existir ou estiver corrompido

## Boas praticas

- O `repomix-output.md` contem o codigo completo com linhas numeradas. Use a numeracao para referenciar partes especificas do codigo.
- O summary no inicio do arquivo ja lista os 10 maiores arquivos por tokens — priorize a leitura desses.
- A estrutura de diretorios no inicio ajuda a localizar rapidamente onde cada coisa esta.

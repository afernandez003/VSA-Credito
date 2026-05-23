# Workflows — Como Funciona o CI/CD

> **Por que aparecem 34 execuções se só há 3 workflows?**
> O Dependabot abriu ~10 PRs automaticamente (um por pacote desatualizado). Cada PR disparou **CI + CodeQL** em paralelo — 10 PRs × 2 workflows = 20 execuções só de Dependabot, mais as execuções do merge para `main` e da primeira execução do Dependabot Updates.

---

## Os 3 Workflows Definidos

### 1. CI — `ci.yml`

**Dispara em:** push/PR para `main`

O pipeline principal de qualidade. Executa quatro verificações em sequência:

| Step | O que faz | Falha o CI? |
|---|---|---|
| `Restore` | Restaura pacotes NuGet | Sim |
| `dotnet format --verify-no-changes` | Verifica formatação; falha se qualquer arquivo difere do padrão do `.editorconfig` | Sim |
| `dotnet list package --vulnerable` | Consulta o banco de CVEs do NuGet; falha se qualquer pacote (direto ou transitivo) tiver vulnerabilidade conhecida | Sim |
| `dotnet test + coverlet` | Roda 37 testes (27 unitários + 10 integração) e mede cobertura; falha se cobertura de linha < 60% | Sim |
| `ReportGenerator` | Gera relatório HTML + Markdown de cobertura | Não |
| `Upload artefatos` | Sobe TRX, Markdown e HTML de cobertura como artefato do run | Não |

**Onde ver o resultado:** aba **Actions → CI → run mais recente → job "Build & Test"**

---

### 2. CodeQL — `codeql.yml`

**Dispara em:** push/PR para `main` · toda segunda-feira às 06:00 UTC · disparo manual

Análise estática de segurança (SAST) feita pelo motor semântico do GitHub. Diferente de linters, o CodeQL rastreia o fluxo de dados — de onde uma entrada vem até onde é usada — para detectar vulnerabilidades reais em C#:

- SQL Injection
- Path Traversal
- XXE (XML External Entity)
- Desserialização insegura
- Algoritmos criptográficos fracos
- Dados sensíveis em logs
- Redirecionamentos não validados

**Não falha o CI** — cria alertas na aba Security.

**Onde ver o resultado:** **Security → Code scanning alerts** (lista vazia = nenhuma vulnerabilidade encontrada)

---

### 3. Dependabot Updates — automático

**Dispara em:** toda segunda-feira (verificação de versões novas)

Não é um arquivo `yml` criado por nós — é o motor interno do GitHub ativado pelo `.github/dependabot.yml`. Ele:

1. Verifica se há versões novas nos dois ecossistemas configurados:
   - **NuGet** — pacotes referenciados nos `.csproj`
   - **GitHub Actions** — `actions/checkout`, `actions/setup-dotnet`, etc.
2. Abre um PR por pacote desatualizado com: versão anterior → nova, link para changelog, verificação de CVE na nova versão
3. O PR dispara CI + CodeQL automaticamente — se passar, pode ser mergeado com confiança

**Onde ver:** aba **Pull requests** (abertos pelo bot `dependabot`)

---

## Por Que São 34 Execuções

```
Merge do PR #1 para main
  ├── CI #1             ← execução 1
  └── CodeQL #1         ← execução 2

Dependabot Updates (1ª varredura após o merge)
  └── Abre PRs #2 a #10 (9 pacotes desatualizados encontrados)

PR #2 (actions/checkout)          → CI #2 + CodeQL #2   = execuções 3, 4
PR #3 (actions/upload-artifact)   → CI #3 + CodeQL #3   = execuções 5, 6
PR #4 (actions/setup-dotnet 4→5)  → CI #4 + CodeQL #4   = execuções 7, 8
PR #5 (...)                       → CI #5 + CodeQL #5   = execuções 9, 10
PR #6 (FluentAssertions 6→8)      → CI #6 + CodeQL #6   = execuções 11, 12
PR #7 (Microsoft.NET.Test.Sdk)    → CI #7 + CodeQL #7   = execuções 13, 14
PR #8 (Serilog.AspNetCore 9→10)   → CI #8 + CodeQL #8   = execuções 15, 16
PR #9 (xunit.runner.visualstudio) → CI #9 + CodeQL #9   = execuções 17, 18
...                                                      = execuções até 34
```

**Resumo:** 1 merge + ~10 PRs do Dependabot × 2 workflows (CI + CodeQL) = ~22–34 execuções.

---

## Dependabot: PRs a Mergear ou Ignorar

Os PRs abertos pelo Dependabot estão todos com CI verde (círculo verde na lista). Isso significa que os testes passaram com as versões novas — os pacotes são compatíveis.

### Como tratar cada PR

| PR | Pacote | Ação recomendada |
|---|---|---|
| `actions/setup-dotnet 4→5` | GitHub Actions | Mergear — atualiza a action de setup |
| `actions/upload-artifact 4→7` | GitHub Actions | Mergear |
| `FluentAssertions 6→8` | Testes | Atenção — v8 mudou a API; CI passou mas revisar diff |
| `Microsoft.NET.Test.Sdk` | Testes | Mergear — patch de manutenção |
| `Serilog.AspNetCore 9→10` | API | Mergear |
| `xunit.runner.visualstudio` | Testes | Mergear |

> **Regra geral:** se CI e CodeQL passaram, o PR é seguro para mergear. Para major versions (ex: FluentAssertions 6→8), vale abrir o link do changelog incluso no PR e conferir breaking changes.

---

## Fluxo Completo

```
Push / PR para main
       │
       ├──► CI (ci.yml)
       │     ├── dotnet format
       │     ├── pacotes vulneráveis
       │     ├── dotnet test (37 testes)
       │     │    └── coverlet (gate 60%)
       │     └── artefatos (TRX + HTML de cobertura)
       │
       └──► CodeQL (codeql.yml)
             └── análise SAST → Security → Code scanning alerts

Toda segunda-feira
       │
       ├──► Dependabot verifica versões novas
       │     └── abre PRs → cada PR dispara CI + CodeQL acima
       │
       └──► CodeQL agendado (06:00 UTC)
             └── re-analisa main com banco de dados atualizado de vulnerabilidades
```

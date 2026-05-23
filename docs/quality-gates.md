# Quality Gates — Ferramentas de Qualidade de Código

> **Contexto:** Este conjunto de ferramentas foi adicionado como complemento ao desafio técnico com o objetivo de validar o comportamento das versões mais recentes das bibliotecas utilizadas no projeto — .NET 10, Confluent.Kafka, EF Core, Testcontainers, FluentValidation, Mediator e demais dependências NuGet — em um pipeline de CI real, garantindo que atualizações futuras de versão sejam detectadas antes de chegar à produção.

---

## Visão Geral

O pipeline de CI (`ci.yml`) executa cinco camadas de validação a cada push ou pull request, além de uma análise de segurança semanal agendada:

```
Restore
  └── dotnet format          ← formatação e style
  └── pacotes vulneráveis    ← CVEs nas dependências
  └── dotnet test            ← testes + cobertura (gate 60%)
       └── Coverage Report   ← HTML + comentário no PR
CodeQL (workflow separado)   ← análise estática de segurança
Dependabot (automático)      ← PRs de atualização de pacotes
```

---

## 1. `dotnet format` — Formatação e Style

**Step:** `Verificar formatação (dotnet format)`

### O que faz
Verifica se todos os arquivos `.cs` estão formatados conforme as regras definidas no `.editorconfig` da solução. Usa o formatador integrado do SDK .NET — sem dependências externas.

O flag `--verify-no-changes` faz o comando retornar código de saída `1` se qualquer arquivo precisar de ajuste, falhando o CI.

### O que cobre
- Indentação e espaçamento
- Organização de `using` statements
- Regras de whitespace (espaços extras, linhas em branco)
- Diagnósticos de style ativados no `.editorconfig` (ex: `IDE0161`, `CA2007`)

### Vantagem
Elimina discussões de formatação em code review. O estilo é aplicado mecanicamente — se passou no CI, o código está formatado. Para corrigir localmente antes do commit:

```bash
dotnet format
```

---

## 2. `dotnet list package --vulnerable` — Dependências com CVE

**Step:** `Verificar pacotes vulneráveis`

### O que faz
Consulta o banco de vulnerabilidades do NuGet (alimentado por [GitHub Advisory Database](https://github.com/advisories)) e lista todos os pacotes — diretos e transitivos — que possuem CVEs conhecidos. O step falha se alguma vulnerabilidade for encontrada.

```bash
dotnet list package --vulnerable --include-transitive
```

### O que cobre
- Pacotes NuGet referenciados diretamente no `.csproj`
- Dependências transitivas (pacotes dos pacotes)
- Severidade: `Low`, `Moderate`, `High`, `Critical`

### Vantagem
Detecta imediatamente quando um pacote adquirido — ou uma atualização automática via Dependabot — introduz uma vulnerabilidade conhecida. É a primeira linha de defesa contra ataques de supply chain.

> Se o step falhar: identifique o pacote no log e atualize para a versão que corrigiu o CVE.

---

## 3. Cobertura de Código — Gate 60%

**Ferramenta:** `coverlet` (coletor `XPlat Code Coverage`) + `ReportGenerator`  
**Configuração:** `tests/Tests/coverlet.runsettings`

### O que faz
Mede quantas linhas do código de produção são exercitadas pelos testes. O gate de **60% de cobertura de linha** faz o CI falhar se a cobertura cair abaixo desse limite.

### Exclusões (não contam para o cálculo)
| Exclusão | Motivo |
|---|---|
| `[*.Tests]*` | O próprio projeto de testes |
| `[*]*.Migrations.*` | Código gerado automaticamente pelo EF Core |
| `[*]*.Program` | Bootstrapping — difícil de testar isoladamente |
| `[GeneratedCode]` / `[CompilerGenerated]` | Código gerado pelo compilador |

### Onde ver os resultados
- **Job Summary** do CI: resumo de cobertura por namespace
- **Comentário no PR**: tabela com percentuais por assembly (atualizada automaticamente)
- **Artefato `test-results-N`**: relatório HTML navegável com cobertura por linha (`tests/TestResults/coverage/index.html`)

### Vantagem
Impede regressão de cobertura. Se um handler novo for adicionado sem testes, o CI falha antes do merge. O threshold de 60% é conservador — pode ser elevado conforme a cobertura crescer.

> Para elevar o gate: edite `<Threshold>` em `tests/Tests/coverlet.runsettings`.

---

## 4. CodeQL — Análise Estática de Segurança (SAST)

**Workflow:** `.github/workflows/codeql.yml`  
**Gratuito para:** repositórios públicos (GitHub Advanced Security incluído)

### O que faz
Analisa o código-fonte em busca de vulnerabilidades de segurança usando o motor de análise semântica da GitHub. Diferente de linters, o CodeQL entende o fluxo de dados — rastreia entradas do usuário desde o ponto de entrada até o ponto de uso para identificar vulnerabilidades reais.

### Quando executa
| Evento | Frequência |
|---|---|
| Push para `main` | A cada commit |
| Pull Request para `main` | A cada PR |
| Schedule | Toda segunda-feira às 06:00 UTC |

A execução semanal agendada garante que novas vulnerabilidades descobertas no banco de dados do CodeQL sejam detectadas mesmo sem mudanças no código.

### O que detecta em C#
- SQL Injection
- Path Traversal
- XML External Entity (XXE)
- Desserialização insegura
- Uso de algoritmos criptográficos fracos
- Dados sensíveis em logs
- Redirecionamentos não validados

### Onde ver os resultados
**GitHub → Security → Code scanning alerts**

Se nenhuma vulnerabilidade for encontrada, a aba fica vazia — esse é o resultado esperado. Alertas são priorizados por severidade (`Error`, `Warning`, `Note`) e incluem o caminho completo do fluxo de dados que originou o problema.

### Vantagem
Não depende de regras manuais — aprende com o código real. Falsos positivos são raros porque o CodeQL analisa semântica, não só padrões textuais. É a ferramenta de SAST mais adotada em projetos .NET open source.

---

## 5. Dependabot — Atualização Automática de Dependências

**Configuração:** `.github/dependabot.yml`  
**Gratuito para:** todos os repositórios públicos e privados

### O que faz
Monitora dois ecossistemas e abre pull requests automaticamente quando uma nova versão é publicada:

| Ecossistema | O que monitora |
|---|---|
| `nuget` | Pacotes NuGet referenciados nos `.csproj` da solução |
| `github-actions` | Actions usadas nos workflows (ex: `actions/checkout`, `setup-dotnet`) |

### Quando executa
Toda segunda-feira — verifica se há versões novas ou patches de segurança.

### O que cada PR entrega
- Versão anterior → versão nova
- Link para o changelog/release notes do pacote
- Compatibilidade com o `TargetFramework` declarado no `.csproj`
- Verificação de vulnerabilidades na nova versão

### Vantagem
Mantém as dependências atualizadas passivamente, sem esforço manual. PRs de Dependabot passam pelo CI normalmente — se o `dotnet format`, os testes e o CodeQL passarem, o PR pode ser mergeado com confiança. Combinado com o step de pacotes vulneráveis, a cobertura é dupla: Dependabot atualiza proativamente; o CI bloqueia retroativamente.

> Para acionar manualmente: **GitHub → Insights → Dependency graph → Dependabot → Check for updates**.

---

## Testes — Onde estão e como consultar

### Localização dos arquivos

```
tests/
├── TestResults/                        ← gerado pelo CI; gitignored
│   ├── test-results.trx                # resultado por teste (xUnit + ITestOutputHelper)
│   ├── test-results.md                 # relatório Markdown (LiquidTestReports)
│   └── coverage/
│       ├── index.html                  # cobertura por linha, navegável no browser
│       └── SummaryGithub.md            # resumo publicado no Job Summary e PR
│
└── Tests/
    ├── coverlet.runsettings            # configuração de cobertura e gate 60%
    ├── GlobalUsings.cs                 # usings globais de todos os testes
    ├── Fakers/
    │   └── CreditoFakers.cs            # dados gerados com Bogus (seeds fixos)
    ├── Unit/
    │   ├── Domain.CreditoTests.cs      # aggregate root: factory, invariantes, errors
    │   ├── IntegrarCredito.HandlerTests.cs   # publica N mensagens no bus
    │   ├── ProcessarCredito.HandlerTests.cs  # guard de duplicidade + insert
    │   ├── GetCreditosByNfse.HandlerTests.cs # filtro por NFS-e + auditoria
    │   └── GetCreditoByNumero.HandlerTests.cs # 200/404 + auditoria
    └── Integration/
        ├── ApiFactory.cs               # WebApplicationFactory + Testcontainers
        └── CreditosEndpointTests.cs    # testes HTTP end-to-end
```

### Tipos de teste

| Tipo | Quantidade | Ferramentas | Infraestrutura |
|---|---|---|---|
| **Unitário** | 27 | xUnit · NSubstitute · FluentAssertions · Bogus | Nenhuma (mocks) |
| **Integração** | 10 | xUnit · Testcontainers · FluentAssertions · Bogus | Kafka + PostgreSQL reais em Docker |

### Dados de teste

Todos os testes usam **[Bogus](https://github.com/bchavez/Bogus)** com seeds fixos para gerar dados realistas e reproduzíveis. Cada teste recebe um seed diferente — se um teste falhar, rodar novamente com o mesmo seed produz exatamente os mesmos dados.

Os testes de integração que cobrem o fluxo completo (POST → Kafka → Worker → banco → GET) usam **[Testcontainers](https://testcontainers.com/guides/getting-started-with-testcontainers-for-dotnet/)** para subir Kafka e PostgreSQL reais em Docker durante a execução — sem mocks de infraestrutura.

### Como rodar localmente

```bash
# Todos os testes
dotnet test

# Apenas unitários
dotnet test --filter "FullyQualifiedName~Unit"

# Apenas integração (requer Docker rodando)
dotnet test --filter "FullyQualifiedName~Integration"

# Com cobertura
dotnet test --collect:"XPlat Code Coverage" --settings tests/Tests/coverlet.runsettings
```

### Como consultar os resultados no GitHub Actions

#### Passo 1 — Abrir o run

```
github.com/afernandez003/VSA-Credito
  → aba Actions
    → workflow "CI" (coluna esquerda)
      → clica no run mais recente
```

O run mostra dois jobs em paralelo: **Build & Test** e **Analyze (C#)** (CodeQL).

---

#### Passo 2 — Resultados dos testes (passou/falhou)

```
Run → job "Build & Test" → step "Test + Coverage"
```

O log mostra cada teste com o resultado:
```
Aprovado  Handle: crédito encontrado → Result.Success [274 ms]
Aprovado  POST /integrar-credito-constituido + Worker → GET retorna [1 s]
```

Para ver o output interno de um teste (dados Bogus, request/response JSON, timing):
- Expanda o step e role até o teste desejado
- As linhas `── INPUT ──`, `── REQUEST ──`, `── RESPONSE ──` são do `ITestOutputHelper`

---

#### Passo 3 — Resumo de cobertura

```
Run → aba "Summary" (topo da página do run, rola até o fim)
```

Aparece a tabela gerada pelo ReportGenerator com cobertura por assembly:

| Assembly | Line | Branch | Method |
|---|---|---|---|
| Creditos.App | 85% | 78% | 91% |
| Creditos.Infra | 72% | 65% | 80% |

---

#### Passo 4 — Artefatos para download

```
Run → seção "Artifacts" (fim da página do run)
  → test-results-N  ← clica para baixar o zip
```

Após extrair o zip:

| Arquivo | Como usar |
|---|---|
| `test-results.trx` | Visual Studio → Test Explorer → Open TRX |
| `test-results.md` | Qualquer editor ou preview no GitHub |
| `coverage/index.html` | Abrir no browser — cobertura por linha, método e branch |

---

#### Passo 5 — Cobertura no PR (pull requests)

Quando o CI roda em um pull request, o bot posta automaticamente um comentário com a tabela de cobertura. Não precisa entrar em Actions — o resumo aparece direto na página do PR.

---

#### Passo 6 — Alertas de segurança (CodeQL)

```
github.com/afernandez003/VSA-Credito
  → aba Security
    → Code scanning alerts
```

Se o CodeQL não encontrar nada, a lista fica vazia — esse é o resultado esperado. Alertas são priorizados por severidade e mostram o fluxo de dados completo que originou o problema.

---

## Resumo

| Ferramenta | Falha o CI? | Quando vejo o resultado |
|---|---|---|
| `dotnet format` | Sim | Log do step, na hora |
| Pacotes vulneráveis | Sim | Log do step, na hora |
| Coverage gate (60%) | Sim | Log do step + Summary do job |
| CodeQL | Não (alerta) | Security → Code scanning alerts (~5 min) |
| Dependabot | — | PRs automáticos (segunda-feira) |

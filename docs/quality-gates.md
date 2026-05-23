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

## Resumo

| Ferramenta | Falha o CI? | Quando vejo o resultado |
|---|---|---|
| `dotnet format` | Sim | Log do step, na hora |
| Pacotes vulneráveis | Sim | Log do step, na hora |
| Coverage gate (60%) | Sim | Log do step + Summary do job |
| CodeQL | Não (alerta) | Security → Code scanning alerts (~5 min) |
| Dependabot | — | PRs automáticos (segunda-feira) |

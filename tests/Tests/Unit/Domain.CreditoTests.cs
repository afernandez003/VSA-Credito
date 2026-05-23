using Xunit.Abstractions;

namespace Creditos.Tests.Unit;

public class Domain_CreditoTests(ITestOutputHelper output)
{
    [Fact(DisplayName = "Create: todas as propriedades são atribuídas corretamente pelo factory method")]
    public void Create_DeveAtribuirPropriedadesCorretamente()
    {
        var credito = CreditoFakers.GerarDomainCredito(
            numeroCredito: "CR-000001",
            numeroNfse: "NF-9999999",
            seed: 1);

        output.WriteLine("── INPUT (Bogus seed=1) ──────────────────");
        output.WriteLine($"NumeroCredito : {credito.NumeroCredito}");
        output.WriteLine($"NumeroNfse    : {credito.NumeroNfse}");
        output.WriteLine($"ValorIssqn    : {credito.ValorIssqn}");
        output.WriteLine($"TipoCredito   : {credito.TipoCredito}");
        output.WriteLine($"SimplesNacional: {credito.SimplesNacional}");
        output.WriteLine($"Aliquota      : {credito.Aliquota}");
        output.WriteLine($"ValorFaturado : {credito.ValorFaturado}");
        output.WriteLine("── ASSERTIONS ───────────────────────────");
        output.WriteLine($"NumeroCredito == \"CR-000001\" ✅");
        output.WriteLine($"NumeroNfse    == \"NF-9999999\" ✅");
        output.WriteLine($"ValorIssqn    > 0 ✅");

        credito.NumeroCredito.Should().Be("CR-000001");
        credito.NumeroNfse.Should().Be("NF-9999999");
        credito.ValorIssqn.Should().BePositive();
        credito.Aliquota.Should().BeInRange(2m, 5m);
        credito.ValorFaturado.Should().BePositive();
    }

    [Fact(DisplayName = "Create: espaços extras em numeroCredito são removidos (Trim)")]
    public void Create_DeveFazerTrimNoNumeroCredito()
    {
        var input = "  CR-TRIM  ";
        var credito = Domain.Credito.Create(
            input, "NF-0000001", new DateOnly(2024, 1, 1),
            500m, "ISSQN", false, 5m, 10000m, 0m, 10000m);

        output.WriteLine($"── INPUT  : \"{input}\"");
        output.WriteLine($"── OUTPUT : \"{credito.NumeroCredito}\"");

        credito.NumeroCredito.Should().Be("CR-TRIM");
    }

    [Fact(DisplayName = "Create: espaços extras em numeroNfse são removidos (Trim)")]
    public void Create_DeveFazerTrimNoNumeroNfse()
    {
        var input = "  NF-TRIM  ";
        var credito = Domain.Credito.Create(
            "CR-TRIM-NF", input, new DateOnly(2024, 1, 1),
            500m, "ISSQN", false, 5m, 10000m, 0m, 10000m);

        output.WriteLine($"── INPUT  : \"{input}\"");
        output.WriteLine($"── OUTPUT : \"{credito.NumeroNfse}\"");

        credito.NumeroNfse.Should().Be("NF-TRIM");
    }

    [Fact(DisplayName = "Create: espaços extras em tipoCredito são removidos (Trim)")]
    public void Create_DeveFazerTrimNoTipoCredito()
    {
        var input = "  ISSQN  ";
        var credito = Domain.Credito.Create(
            "CR-TRIM-TC", "NF-0000002", new DateOnly(2024, 1, 1),
            500m, input, false, 5m, 10000m, 0m, 10000m);

        output.WriteLine($"── INPUT  : \"{input}\"");
        output.WriteLine($"── OUTPUT : \"{credito.TipoCredito}\"");

        credito.TipoCredito.Should().Be("ISSQN");
    }

    [Fact(DisplayName = "Create: simplesNacional=true é persistido sem conversão")]
    public void Create_ComSimplesNacionalTrue_DeveAtribuirTrue()
    {
        var credito = Domain.Credito.Create(
            "CR-SIMPLES", "NF-0000003", new DateOnly(2024, 1, 1),
            500m, "ISSQN", true, 5m, 10000m, 0m, 10000m);

        output.WriteLine($"── INPUT  : SimplesNacional = true");
        output.WriteLine($"── OUTPUT : SimplesNacional = {credito.SimplesNacional}");

        credito.SimplesNacional.Should().BeTrue();
    }

    [Fact(DisplayName = "Errors.NotFound: retorna código CREDITO_NOT_FOUND e mensagem com o número do crédito")]
    public void Errors_NotFound_DeveConterCodigoEMensagem()
    {
        var credito = CreditoFakers.GerarDomainCredito(seed: 10);
        var error = Domain.Credito.Errors.NotFound(credito.NumeroCredito);

        output.WriteLine($"── INPUT  : NumeroCredito = \"{credito.NumeroCredito}\" (Bogus seed=10)");
        output.WriteLine($"── OUTPUT : Code    = \"{error.Code}\"");
        output.WriteLine($"            Message = \"{error.Message}\"");

        error.Code.Should().Be("CREDITO_NOT_FOUND");
        error.Message.Should().Contain(credito.NumeroCredito);
    }

    [Fact(DisplayName = "Errors.AlreadyExists: retorna código CREDITO_EXISTS e mensagem com o número do crédito")]
    public void Errors_AlreadyExists_DeveConterCodigoEMensagem()
    {
        var credito = CreditoFakers.GerarDomainCredito(seed: 11);
        var error = Domain.Credito.Errors.AlreadyExists(credito.NumeroCredito);

        output.WriteLine($"── INPUT  : NumeroCredito = \"{credito.NumeroCredito}\" (Bogus seed=11)");
        output.WriteLine($"── OUTPUT : Code    = \"{error.Code}\"");
        output.WriteLine($"            Message = \"{error.Message}\"");

        error.Code.Should().Be("CREDITO_EXISTS");
        error.Message.Should().Contain(credito.NumeroCredito);
    }
}

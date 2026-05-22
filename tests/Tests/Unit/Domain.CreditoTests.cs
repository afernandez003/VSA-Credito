using FluentAssertions;

namespace Creditos.Tests.Unit;

public class Domain_CreditoTests
{
    private static Domain.Credito CriarCredito(
        string numeroCredito = "123456",
        string numeroNfse = "7891011",
        DateOnly? dataConstituicao = null,
        decimal valorIssqn = 500.00m,
        string tipoCredito = "ISSQN",
        bool simplesNacional = false,
        decimal aliquota = 5.00m,
        decimal valorFaturado = 10000.00m,
        decimal valorDeducao = 0m,
        decimal baseCalculo = 10000.00m) =>
        Domain.Credito.Create(
            numeroCredito,
            numeroNfse,
            dataConstituicao ?? new DateOnly(2024, 3, 1),
            valorIssqn,
            tipoCredito,
            simplesNacional,
            aliquota,
            valorFaturado,
            valorDeducao,
            baseCalculo);

    [Fact]
    public void Create_DeveAtribuirPropriedadesCorretamente()
    {
        var credito = CriarCredito();

        credito.NumeroCredito.Should().Be("123456");
        credito.NumeroNfse.Should().Be("7891011");
        credito.DataConstituicao.Should().Be(new DateOnly(2024, 3, 1));
        credito.ValorIssqn.Should().Be(500.00m);
        credito.TipoCredito.Should().Be("ISSQN");
        credito.SimplesNacional.Should().BeFalse();
        credito.Aliquota.Should().Be(5.00m);
        credito.ValorFaturado.Should().Be(10000.00m);
        credito.ValorDeducao.Should().Be(0m);
        credito.BaseCalculo.Should().Be(10000.00m);
    }

    [Fact]
    public void Create_DeveFazerTrimNoNumeroCredito()
    {
        var credito = CriarCredito(numeroCredito: "  123456  ");

        credito.NumeroCredito.Should().Be("123456");
    }

    [Fact]
    public void Create_DeveFazerTrimNoNumeroNfse()
    {
        var credito = CriarCredito(numeroNfse: "  7891011  ");

        credito.NumeroNfse.Should().Be("7891011");
    }

    [Fact]
    public void Create_DeveFazerTrimNoTipoCredito()
    {
        var credito = CriarCredito(tipoCredito: "  ISSQN  ");

        credito.TipoCredito.Should().Be("ISSQN");
    }

    [Fact]
    public void Create_ComSimplesNacionalTrue_DeveAtribuirTrue()
    {
        var credito = CriarCredito(simplesNacional: true);

        credito.SimplesNacional.Should().BeTrue();
    }

    [Fact]
    public void Errors_NotFound_DeveConterCodigoEMensagem()
    {
        var error = Domain.Credito.Errors.NotFound("123456");

        error.Code.Should().Be("CREDITO_NOT_FOUND");
        error.Message.Should().Contain("123456");
    }

    [Fact]
    public void Errors_AlreadyExists_DeveConterCodigoEMensagem()
    {
        var error = Domain.Credito.Errors.AlreadyExists("123456");

        error.Code.Should().Be("CREDITO_EXISTS");
        error.Message.Should().Contain("123456");
    }
}

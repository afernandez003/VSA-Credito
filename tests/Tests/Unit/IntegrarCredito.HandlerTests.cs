using Creditos.Messaging;
using Creditos.Options;
using Microsoft.Extensions.Logging.Abstractions;
using ExtOptions = Microsoft.Extensions.Options.Options;
using static Creditos.Credito.IntegrarCredito;

namespace Creditos.Tests.Unit;

public class IntegrarCredito_HandlerTests
{
    private readonly IMessagePublisher _publisher = Substitute.For<IMessagePublisher>();

    private readonly IOptions<MessagingOptions> _options = ExtOptions.Create(new MessagingOptions
    {
        TopicoIntegracao = "integrar-credito-constituido-entry",
        TopicoAuditoria = "consulta-credito-entry"
    });

    private Handler CriarHandler() => new(_publisher, _options, NullLogger<Handler>.Instance);

    private static CreditoRequest CriarRequest(string numero = "123456") => new(
        NumeroCredito: numero,
        NumeroNfse: "7891011",
        DataConstituicao: new DateOnly(2024, 3, 1),
        ValorIssqn: 500.00m,
        TipoCredito: "ISSQN",
        SimplesNacional: "Não",
        Aliquota: 5.00m,
        ValorFaturado: 10000.00m,
        ValorDeducao: 0m,
        BaseCalculo: 10000.00m);

    [Fact]
    public async Task Handle_ComUmCredito_DevePublicarUmaMensagem()
    {
        var command = new Command([CriarRequest()]);
        var handler = CriarHandler();

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        await _publisher.Received(1).PublishAsync(
            Arg.Any<CreditoMessage>(),
            "integrar-credito-constituido-entry",
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ComTresCreditos_DevePublicarTresMensagens()
    {
        var command = new Command([CriarRequest("111"), CriarRequest("222"), CriarRequest("333")]);
        var handler = CriarHandler();

        await handler.Handle(command, CancellationToken.None);

        await _publisher.Received(3).PublishAsync(
            Arg.Any<CreditoMessage>(),
            "integrar-credito-constituido-entry",
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_DeveRetornarSuccessTrue()
    {
        var command = new Command([CriarRequest()]);
        var handler = CriarHandler();

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Success.Should().BeTrue();
    }

    [Theory]
    [InlineData("Sim", true)]
    [InlineData("sim", true)]
    [InlineData("SIM", true)]
    [InlineData("Não", false)]
    [InlineData("não", false)]
    [InlineData("NAO", false)]
    public async Task Handle_ParseSimplesNacional_DeveConverterCorretamente(string input, bool esperado)
    {
        var request = CriarRequest() with { SimplesNacional = input };
        var command = new Command([request]);
        var handler = CriarHandler();

        await handler.Handle(command, CancellationToken.None);

        await _publisher.Received(1).PublishAsync(
            Arg.Is<CreditoMessage>(m => m.SimplesNacional == esperado),
            Arg.Any<string>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ListaVazia_NaoDevePublicarNada()
    {
        var command = new Command([]);
        var handler = CriarHandler();

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        await _publisher.DidNotReceive().PublishAsync(
            Arg.Any<CreditoMessage>(),
            Arg.Any<string>(),
            Arg.Any<CancellationToken>());
    }
}

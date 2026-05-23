using Creditos.Messaging;
using Creditos.Options;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit.Abstractions;
using ExtOptions = Microsoft.Extensions.Options.Options;
using static Creditos.Credito.IntegrarCredito;

namespace Creditos.Tests.Unit;

public class IntegrarCredito_HandlerTests(ITestOutputHelper output)
{
    private readonly IMessagePublisher _publisher = Substitute.For<IMessagePublisher>();

    private readonly IOptions<MessagingOptions> _options = ExtOptions.Create(new MessagingOptions
    {
        TopicoIntegracao = "integrar-credito-constituido-entry",
        TopicoAuditoria  = "consulta-credito-entry"
    });

    private Handler CriarHandler() => new(_publisher, _options, NullLogger<Handler>.Instance);

    [Fact(DisplayName = "Handle: payload com 1 crédito → PublishAsync chamado exatamente 1 vez no tópico de integração")]
    public async Task Handle_ComUmCredito_DevePublicarUmaMensagem()
    {
        var request = CreditoFakers.GerarRequest(seed: 1);
        var command = new Command([request]);

        output.WriteLine("── INPUT (Bogus seed=1) ──────────────────");
        output.WriteLine($"NumeroCredito : {request.NumeroCredito}");
        output.WriteLine($"NumeroNfse    : {request.NumeroNfse}");
        output.WriteLine($"TipoCredito   : {request.TipoCredito}");
        output.WriteLine($"SimplesNacional: {request.SimplesNacional}");
        output.WriteLine($"ValorIssqn    : {request.ValorIssqn}");

        var result = await CriarHandler().Handle(command, CancellationToken.None);

        output.WriteLine("── RESULTADO ────────────────────────────");
        output.WriteLine($"IsSuccess   : {result.IsSuccess}");
        output.WriteLine($"PublishAsync: chamado 1x no tópico \"integrar-credito-constituido-entry\"");

        result.IsSuccess.Should().BeTrue();
        await _publisher.Received(1).PublishAsync(
            Arg.Any<CreditoMessage>(),
            "integrar-credito-constituido-entry",
            Arg.Any<CancellationToken>());
    }

    [Fact(DisplayName = "Handle: payload com 3 créditos → PublishAsync chamado exatamente 3 vezes (1 por crédito)")]
    public async Task Handle_ComTresCreditos_DevePublicarTresMensagens()
    {
        var requests = CreditoFakers.GerarRequests(3, seed: 2);
        var command = new Command(requests);

        output.WriteLine("── INPUT (Bogus seed=2, 3 créditos) ──────");
        foreach (var r in requests)
            output.WriteLine($"  {r.NumeroCredito} | {r.TipoCredito} | {r.ValorIssqn:N2} | {r.SimplesNacional}");

        await CriarHandler().Handle(command, CancellationToken.None);

        output.WriteLine("── RESULTADO ────────────────────────────");
        output.WriteLine($"PublishAsync: chamado 3x no tópico \"integrar-credito-constituido-entry\"");

        await _publisher.Received(3).PublishAsync(
            Arg.Any<CreditoMessage>(),
            "integrar-credito-constituido-entry",
            Arg.Any<CancellationToken>());
    }

    [Fact(DisplayName = "Handle: sempre retorna Result.Success com success=true independente do conteúdo")]
    public async Task Handle_DeveRetornarSuccessTrue()
    {
        var request = CreditoFakers.GerarRequest(seed: 3);
        var command = new Command([request]);

        output.WriteLine($"── INPUT (Bogus seed=3): {request.NumeroCredito}");

        var result = await CriarHandler().Handle(command, CancellationToken.None);

        output.WriteLine($"── RESULTADO: IsSuccess={result.IsSuccess}, Success={result.Value.Success}");

        result.IsSuccess.Should().BeTrue();
        result.Value.Success.Should().BeTrue();
    }

    [Theory(DisplayName = "Handle: simplesNacional aceita variações de case e acento → converte para bool na mensagem")]
    [InlineData("Sim",  true)]
    [InlineData("sim",  true)]
    [InlineData("SIM",  true)]
    [InlineData("Não",  false)]
    [InlineData("não",  false)]
    [InlineData("NAO",  false)]
    public async Task Handle_ParseSimplesNacional_DeveConverterCorretamente(string input, bool esperado)
    {
        var request = CreditoFakers.GerarRequest(seed: 4) with { SimplesNacional = input };
        var command = new Command([request]);

        output.WriteLine($"── INPUT  : SimplesNacional = \"{input}\"");
        output.WriteLine($"── ESPERADO: bool = {esperado}");

        await CriarHandler().Handle(command, CancellationToken.None);

        output.WriteLine($"── RESULTADO: PublishAsync recebeu CreditoMessage.SimplesNacional = {esperado} ✅");

        await _publisher.Received(1).PublishAsync(
            Arg.Is<CreditoMessage>(m => m.SimplesNacional == esperado),
            Arg.Any<string>(),
            Arg.Any<CancellationToken>());
    }

    [Fact(DisplayName = "Handle: payload vazio → PublishAsync nunca é chamado")]
    public async Task Handle_ListaVazia_NaoDevePublicarNada()
    {
        var command = new Command([]);

        output.WriteLine("── INPUT  : lista vazia []");

        var result = await CriarHandler().Handle(command, CancellationToken.None);

        output.WriteLine($"── RESULTADO: IsSuccess={result.IsSuccess}, PublishAsync=0 chamadas");

        result.IsSuccess.Should().BeTrue();
        await _publisher.DidNotReceive().PublishAsync(
            Arg.Any<CreditoMessage>(),
            Arg.Any<string>(),
            Arg.Any<CancellationToken>());
    }
}

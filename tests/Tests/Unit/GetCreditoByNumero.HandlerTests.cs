using Creditos.Messaging;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit.Abstractions;
using static Creditos.Credito.GetCreditoByNumero;

namespace Creditos.Tests.Unit;

public class GetCreditoByNumero_HandlerTests(ITestOutputHelper output)
{
    private readonly ICreditoRepository _repository = Substitute.For<ICreditoRepository>();
    private readonly IMessagePublisher _publisher = Substitute.For<IMessagePublisher>();

    private Handler CriarHandler() => new(_repository, _publisher, NullLogger<Handler>.Instance);

    [Fact(DisplayName = "Handle: crédito encontrado → Result.Success com numeroCredito e numeroNfse corretos")]
    public async Task Handle_CreditoExistente_DeveRetornarDados()
    {
        var credito = CreditoFakers.GerarDomainCredito(seed: 1);
        _repository.GetByNumeroCreditoAsync(credito.NumeroCredito, Arg.Any<CancellationToken>())
            .Returns(credito);

        output.WriteLine("── INPUT (Bogus seed=1) ──────────────────");
        output.WriteLine($"NumeroCredito : {credito.NumeroCredito}");
        output.WriteLine($"NumeroNfse    : {credito.NumeroNfse}");
        output.WriteLine($"TipoCredito   : {credito.TipoCredito}");
        output.WriteLine($"ValorIssqn    : {credito.ValorIssqn}");
        output.WriteLine("── SETUP: repositório retorna o crédito");

        var result = await CriarHandler().Handle(new Query(credito.NumeroCredito), CancellationToken.None);

        output.WriteLine("── RESULTADO ────────────────────────────");
        output.WriteLine($"IsSuccess     : {result.IsSuccess}");
        output.WriteLine($"NumeroCredito : {result.Value.NumeroCredito} ✅");
        output.WriteLine($"NumeroNfse    : {result.Value.NumeroNfse} ✅");

        result.IsSuccess.Should().BeTrue();
        result.Value.NumeroCredito.Should().Be(credito.NumeroCredito);
        result.Value.NumeroNfse.Should().Be(credito.NumeroNfse);
    }

    [Fact(DisplayName = "Handle: crédito não encontrado → Result.Failure com código CREDITO_NOT_FOUND")]
    public async Task Handle_CreditoNaoExistente_DeveRetornarFailure()
    {
        var credito = CreditoFakers.GerarDomainCredito(seed: 2);
        _repository.GetByNumeroCreditoAsync(credito.NumeroCredito, Arg.Any<CancellationToken>())
            .Returns((Domain.Credito?)null);

        output.WriteLine($"── INPUT (Bogus seed=2): NumeroCredito = \"{credito.NumeroCredito}\"");
        output.WriteLine("── SETUP: repositório retorna null");

        var result = await CriarHandler().Handle(new Query(credito.NumeroCredito), CancellationToken.None);

        output.WriteLine("── RESULTADO ────────────────────────────");
        output.WriteLine($"IsFailure  : {result.IsFailure}");
        output.WriteLine($"Error.Code : \"{result.Error.Code}\" ✅");

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("CREDITO_NOT_FOUND");
    }

    [Fact(DisplayName = "Handle: evento de auditoria publicado com Tipo='PorNumero', independente de encontrar ou não o crédito")]
    public async Task Handle_DevePublicarEventoAuditoriaEmQualquerCaso()
    {
        var credito = CreditoFakers.GerarDomainCredito(seed: 3);
        _repository.GetByNumeroCreditoAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((Domain.Credito?)null);

        output.WriteLine($"── INPUT (Bogus seed=3): NumeroCredito = \"{credito.NumeroCredito}\"");
        output.WriteLine("── SETUP: repositório retorna null (não importa para auditoria)");

        await CriarHandler().Handle(new Query(credito.NumeroCredito), CancellationToken.None);

        output.WriteLine("── RESULTADO ────────────────────────────");
        output.WriteLine($"PublishAuditoriaAsync: chamado 1x");
        output.WriteLine($"  Tipo      = \"PorNumero\"");
        output.WriteLine($"  Parametro = \"{credito.NumeroCredito}\" ✅");

        await _publisher.Received(1).PublishAuditoriaAsync(
            Arg.Is<ConsultaRealizadaMessage>(m => m.Tipo == "PorNumero" && m.Parametro == credito.NumeroCredito),
            Arg.Any<CancellationToken>());
    }

    [Fact(DisplayName = "Handle: simplesNacional=true no banco → serializado como 'Sim' no response JSON")]
    public async Task Handle_CreditoEncontrado_DeveMapearSimplesNacionalParaSimOuNao()
    {
        var credito = Domain.Credito.Create(
            "CR-SIMPLES-NUM", "NF-SIMPLES", new DateOnly(2024, 3, 1),
            500m, "ISSQN", true, 5m, 10000m, 0m, 10000m);
        _repository.GetByNumeroCreditoAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(credito);

        output.WriteLine($"── INPUT: NumeroCredito = \"{credito.NumeroCredito}\"");
        output.WriteLine($"── SETUP: Domain.Credito.SimplesNacional = true (bool)");

        var result = await CriarHandler().Handle(new Query(credito.NumeroCredito), CancellationToken.None);

        output.WriteLine($"── RESULTADO: Response.SimplesNacional = \"{result.Value.SimplesNacional}\" ✅");

        result.Value.SimplesNacional.Should().Be("Sim");
    }
}

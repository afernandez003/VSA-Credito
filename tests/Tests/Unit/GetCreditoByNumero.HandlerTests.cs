using Creditos.Messaging;
using Microsoft.Extensions.Logging.Abstractions;
using static Creditos.Credito.GetCreditoByNumero;

namespace Creditos.Tests.Unit;

public class GetCreditoByNumero_HandlerTests
{
    private readonly ICreditoRepository _repository = Substitute.For<ICreditoRepository>();
    private readonly IMessagePublisher _publisher = Substitute.For<IMessagePublisher>();

    private Handler CriarHandler() => new(_repository, _publisher, NullLogger<Handler>.Instance);

    private static Domain.Credito CriarCredito(string numeroCredito = "123456") =>
        Domain.Credito.Create(
            numeroCredito,
            "7891011",
            new DateOnly(2024, 3, 1),
            500.00m,
            "ISSQN",
            false,
            5.00m,
            10000.00m,
            0m,
            10000.00m);

    [Fact]
    public async Task Handle_CreditoExistente_DeveRetornarDados()
    {
        _repository.GetByNumeroCreditoAsync("123456", Arg.Any<CancellationToken>())
            .Returns(CriarCredito());

        var result = await CriarHandler().Handle(new Query("123456"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.NumeroCredito.Should().Be("123456");
        result.Value.NumeroNfse.Should().Be("7891011");
    }

    [Fact]
    public async Task Handle_CreditoNaoExistente_DeveRetornarFailure()
    {
        _repository.GetByNumeroCreditoAsync("999999", Arg.Any<CancellationToken>())
            .Returns((Domain.Credito?)null);

        var result = await CriarHandler().Handle(new Query("999999"), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("CREDITO_NOT_FOUND");
    }

    [Fact]
    public async Task Handle_DevePublicarEventoAuditoriaEmQualquerCaso()
    {
        _repository.GetByNumeroCreditoAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((Domain.Credito?)null);

        await CriarHandler().Handle(new Query("123456"), CancellationToken.None);

        await _publisher.Received(1).PublishAuditoriaAsync(
            Arg.Is<ConsultaRealizadaMessage>(m => m.Tipo == "PorNumero" && m.Parametro == "123456"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_CreditoEncontrado_DeveMapearSimplesNacionalParaSimOuNao()
    {
        var credito = Domain.Credito.Create("123456", "7891011", new DateOnly(2024, 3, 1), 500m, "ISSQN", true, 5m, 10000m, 0m, 10000m);
        _repository.GetByNumeroCreditoAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(credito);

        var result = await CriarHandler().Handle(new Query("123456"), CancellationToken.None);

        result.Value.SimplesNacional.Should().Be("Sim");
    }
}

using Creditos.Messaging;
using Microsoft.Extensions.Logging.Abstractions;
using static Creditos.Credito.GetCreditosByNfse;

namespace Creditos.Tests.Unit;

public class GetCreditosByNfse_HandlerTests
{
    private readonly ICreditoRepository _repository = Substitute.For<ICreditoRepository>();
    private readonly IMessagePublisher _publisher = Substitute.For<IMessagePublisher>();

    private Handler CriarHandler() => new(_repository, _publisher, NullLogger<Handler>.Instance);

    private static Domain.Credito CriarCredito(string numeroCredito = "123456", string numeroNfse = "7891011") =>
        Domain.Credito.Create(
            numeroCredito,
            numeroNfse,
            new DateOnly(2024, 3, 1),
            500.00m,
            "ISSQN",
            false,
            5.00m,
            10000.00m,
            0m,
            10000.00m);

    [Fact]
    public async Task Handle_ComCreditosExistentes_DeveRetornarLista()
    {
        var creditos = new List<Domain.Credito> { CriarCredito("111"), CriarCredito("222") };
        _repository.GetByNumeroNfseAsync("7891011", Arg.Any<CancellationToken>())
            .Returns(creditos);

        var result = await CriarHandler().Handle(new Query("7891011"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(2);
    }

    [Fact]
    public async Task Handle_SemCreditosExistentes_DeveRetornarListaVazia()
    {
        _repository.GetByNumeroNfseAsync("7891011", Arg.Any<CancellationToken>())
            .Returns(new List<Domain.Credito>());

        var result = await CriarHandler().Handle(new Query("7891011"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_DevePublicarEventoAuditoria()
    {
        _repository.GetByNumeroNfseAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new List<Domain.Credito>());

        await CriarHandler().Handle(new Query("7891011"), CancellationToken.None);

        await _publisher.Received(1).PublishAuditoriaAsync(
            Arg.Is<ConsultaRealizadaMessage>(m => m.Tipo == "PorNfse" && m.Parametro == "7891011"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_DeveMapearSimplesNacionalParaString()
    {
        var creditos = new List<Domain.Credito>
        {
            Domain.Credito.Create("111", "7891011", new DateOnly(2024, 3, 1), 500m, "ISSQN", true, 5m, 10000m, 0m, 10000m)
        };
        _repository.GetByNumeroNfseAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(creditos);

        var result = await CriarHandler().Handle(new Query("7891011"), CancellationToken.None);

        result.Value.First().SimplesNacional.Should().Be("Sim");
    }
}

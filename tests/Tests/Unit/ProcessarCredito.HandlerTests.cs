using Creditos.Messaging;
using Microsoft.Extensions.Logging.Abstractions;
using static Creditos.Credito.ProcessarCredito;

namespace Creditos.Tests.Unit;

public class ProcessarCredito_HandlerTests
{
    private readonly ICreditoRepository _repository = Substitute.For<ICreditoRepository>();

    private Handler CriarHandler() => new(_repository, NullLogger<Handler>.Instance);

    private static CreditoMessage CriarMessage(string numeroCredito = "123456") => new(
        NumeroCredito: numeroCredito,
        NumeroNfse: "7891011",
        DataConstituicao: new DateOnly(2024, 3, 1),
        ValorIssqn: 500.00m,
        TipoCredito: "ISSQN",
        SimplesNacional: false,
        Aliquota: 5.00m,
        ValorFaturado: 10000.00m,
        ValorDeducao: 0m,
        BaseCalculo: 10000.00m);

    [Fact]
    public async Task Handle_CreditoNovo_DeveInserirNaBase()
    {
        _repository.ExistsByNumeroCreditoAsync("123456", Arg.Any<CancellationToken>())
            .Returns(false);

        var command = new Command(CriarMessage());
        var handler = CriarHandler();

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        await _repository.Received(1).AddAsync(Arg.Any<Domain.Credito>(), Arg.Any<CancellationToken>());
        await _repository.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_CreditoDuplicado_NaoDeveInserirNovamente()
    {
        _repository.ExistsByNumeroCreditoAsync("123456", Arg.Any<CancellationToken>())
            .Returns(true);

        var command = new Command(CriarMessage());
        var handler = CriarHandler();

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        await _repository.DidNotReceive().AddAsync(Arg.Any<Domain.Credito>(), Arg.Any<CancellationToken>());
        await _repository.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_CreditoDuplicado_DeveRetornarSuccess()
    {
        _repository.ExistsByNumeroCreditoAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(true);

        var result = await CriarHandler().Handle(new Command(CriarMessage()), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_CreditoNovo_DeveCriarAggregateDomainCorretamente()
    {
        Domain.Credito? creditoCapturado = null;
        _repository.ExistsByNumeroCreditoAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(false);
        await _repository.AddAsync(Arg.Do<Domain.Credito>(c => creditoCapturado = c), Arg.Any<CancellationToken>());

        var message = CriarMessage("999");
        await CriarHandler().Handle(new Command(message), CancellationToken.None);

        creditoCapturado.Should().NotBeNull();
        creditoCapturado!.NumeroCredito.Should().Be("999");
        creditoCapturado.NumeroNfse.Should().Be("7891011");
        creditoCapturado.SimplesNacional.Should().BeFalse();
    }
}

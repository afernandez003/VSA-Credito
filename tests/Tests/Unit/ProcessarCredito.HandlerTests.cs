using Creditos.Messaging;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit.Abstractions;
using static Creditos.Credito.ProcessarCredito;

namespace Creditos.Tests.Unit;

public class ProcessarCredito_HandlerTests(ITestOutputHelper output)
{
    private readonly ICreditoRepository _repository = Substitute.For<ICreditoRepository>();

    private Handler CriarHandler() => new(_repository, NullLogger<Handler>.Instance);

    [Fact(DisplayName = "Handle: crédito novo (não existe no banco) → AddAsync e SaveChangesAsync são chamados")]
    public async Task Handle_CreditoNovo_DeveInserirNaBase()
    {
        var message = CreditoFakers.GerarMessage(seed: 1);
        _repository.ExistsByNumeroCreditoAsync(message.NumeroCredito, Arg.Any<CancellationToken>())
            .Returns(false);

        output.WriteLine("── INPUT (Bogus seed=1) ──────────────────");
        output.WriteLine($"NumeroCredito : {message.NumeroCredito}");
        output.WriteLine($"NumeroNfse    : {message.NumeroNfse}");
        output.WriteLine($"TipoCredito   : {message.TipoCredito}");
        output.WriteLine($"SimplesNacional: {message.SimplesNacional}");
        output.WriteLine($"ValorIssqn    : {message.ValorIssqn}");
        output.WriteLine("── SETUP: ExistsBy... → false (crédito novo)");

        var result = await CriarHandler().Handle(new Command(message), CancellationToken.None);

        output.WriteLine("── RESULTADO ────────────────────────────");
        output.WriteLine($"IsSuccess    : {result.IsSuccess}");
        output.WriteLine($"AddAsync     : chamado 1x ✅");
        output.WriteLine($"SaveChanges  : chamado 1x ✅");

        result.IsSuccess.Should().BeTrue();
        await _repository.Received(1).AddAsync(Arg.Any<Domain.Credito>(), Arg.Any<CancellationToken>());
        await _repository.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact(DisplayName = "Handle: crédito duplicado (já existe no banco) → AddAsync NÃO é chamado (guard de idempotência)")]
    public async Task Handle_CreditoDuplicado_NaoDeveInserirNovamente()
    {
        var message = CreditoFakers.GerarMessage(seed: 2);
        _repository.ExistsByNumeroCreditoAsync(message.NumeroCredito, Arg.Any<CancellationToken>())
            .Returns(true);

        output.WriteLine("── INPUT (Bogus seed=2) ──────────────────");
        output.WriteLine($"NumeroCredito : {message.NumeroCredito}");
        output.WriteLine("── SETUP: ExistsBy... → true (duplicado)");

        var result = await CriarHandler().Handle(new Command(message), CancellationToken.None);

        output.WriteLine("── RESULTADO ────────────────────────────");
        output.WriteLine($"IsSuccess : {result.IsSuccess}");
        output.WriteLine($"AddAsync  : NÃO chamado (idempotência) ✅");
        output.WriteLine($"SaveChanges: NÃO chamado ✅");

        result.IsSuccess.Should().BeTrue();
        await _repository.DidNotReceive().AddAsync(Arg.Any<Domain.Credito>(), Arg.Any<CancellationToken>());
        await _repository.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact(DisplayName = "Handle: crédito duplicado → retorna Result.Success (operação idempotente, sem erro)")]
    public async Task Handle_CreditoDuplicado_DeveRetornarSuccess()
    {
        var message = CreditoFakers.GerarMessage(seed: 3);
        _repository.ExistsByNumeroCreditoAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(true);

        output.WriteLine($"── INPUT (Bogus seed=3): {message.NumeroCredito}");
        output.WriteLine("── SETUP: ExistsBy... → true");

        var result = await CriarHandler().Handle(new Command(message), CancellationToken.None);

        output.WriteLine($"── RESULTADO: IsSuccess={result.IsSuccess} (sem erro mesmo sendo duplicado) ✅");

        result.IsSuccess.Should().BeTrue();
    }

    [Fact(DisplayName = "Handle: crédito novo → aggregate Domain.Credito criado com os campos corretos da mensagem")]
    public async Task Handle_CreditoNovo_DeveCriarAggregateDomainCorretamente()
    {
        var message = CreditoFakers.GerarMessage(seed: 4);
        Domain.Credito? creditoCapturado = null;
        _repository.ExistsByNumeroCreditoAsync(message.NumeroCredito, Arg.Any<CancellationToken>()).Returns(false);
        await _repository.AddAsync(Arg.Do<Domain.Credito>(c => creditoCapturado = c), Arg.Any<CancellationToken>());

        output.WriteLine("── INPUT (Bogus seed=4) ──────────────────");
        output.WriteLine($"NumeroCredito  : {message.NumeroCredito}");
        output.WriteLine($"NumeroNfse     : {message.NumeroNfse}");
        output.WriteLine($"SimplesNacional: {message.SimplesNacional}");
        output.WriteLine($"ValorIssqn     : {message.ValorIssqn}");

        await CriarHandler().Handle(new Command(message), CancellationToken.None);

        output.WriteLine("── AGGREGATE CAPTURADO ──────────────────");
        output.WriteLine($"NumeroCredito  : {creditoCapturado?.NumeroCredito}");
        output.WriteLine($"NumeroNfse     : {creditoCapturado?.NumeroNfse}");
        output.WriteLine($"SimplesNacional: {creditoCapturado?.SimplesNacional}");
        output.WriteLine($"ValorIssqn     : {creditoCapturado?.ValorIssqn}");

        creditoCapturado.Should().NotBeNull();
        creditoCapturado!.NumeroCredito.Should().Be(message.NumeroCredito);
        creditoCapturado.NumeroNfse.Should().Be(message.NumeroNfse);
        creditoCapturado.SimplesNacional.Should().Be(message.SimplesNacional);
        creditoCapturado.ValorIssqn.Should().Be(message.ValorIssqn);
    }
}

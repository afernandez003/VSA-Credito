using Creditos.Messaging;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit.Abstractions;
using static Creditos.Credito.GetCreditosByNfse;

namespace Creditos.Tests.Unit;

public class GetCreditosByNfse_HandlerTests(ITestOutputHelper output)
{
    private readonly ICreditoRepository _repository = Substitute.For<ICreditoRepository>();
    private readonly IMessagePublisher _publisher = Substitute.For<IMessagePublisher>();

    private Handler CriarHandler() => new(_repository, _publisher, NullLogger<Handler>.Instance);

    [Fact(DisplayName = "Handle: NFS-e com 2 créditos no banco → retorna lista com 2 itens")]
    public async Task Handle_ComCreditosExistentes_DeveRetornarLista()
    {
        var nfse = "NF-8881234";
        var creditos = new List<Domain.Credito>
        {
            CreditoFakers.GerarDomainCredito(numeroNfse: nfse, seed: 1),
            CreditoFakers.GerarDomainCredito(numeroNfse: nfse, seed: 2),
        };
        _repository.GetByNumeroNfseAsync(nfse, Arg.Any<CancellationToken>()).Returns(creditos);

        output.WriteLine($"── INPUT: NumeroNfse = \"{nfse}\"");
        output.WriteLine("── SETUP: banco retorna 2 créditos");
        foreach (var c in creditos)
            output.WriteLine($"  {c.NumeroCredito} | {c.TipoCredito} | {c.ValorIssqn:N2}");

        var result = await CriarHandler().Handle(new Query(nfse), CancellationToken.None);

        output.WriteLine("── RESULTADO ────────────────────────────");
        output.WriteLine($"IsSuccess : {result.IsSuccess}");
        output.WriteLine($"Count     : {result.Value.Count} ✅");

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(2);
    }

    [Fact(DisplayName = "Handle: NFS-e sem créditos no banco → retorna lista vazia (não é erro)")]
    public async Task Handle_SemCreditosExistentes_DeveRetornarListaVazia()
    {
        var nfse = "NF-0000000";
        _repository.GetByNumeroNfseAsync(nfse, Arg.Any<CancellationToken>())
            .Returns(new List<Domain.Credito>());

        output.WriteLine($"── INPUT: NumeroNfse = \"{nfse}\"");
        output.WriteLine("── SETUP: banco retorna lista vazia");

        var result = await CriarHandler().Handle(new Query(nfse), CancellationToken.None);

        output.WriteLine($"── RESULTADO: IsSuccess={result.IsSuccess}, Count={result.Value.Count} ✅");

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEmpty();
    }

    [Fact(DisplayName = "Handle: toda consulta por NFS-e publica ConsultaRealizadaMessage com Tipo='PorNfse' no tópico de auditoria")]
    public async Task Handle_DevePublicarEventoAuditoria()
    {
        var nfse = "NF-AUDITORIA";
        _repository.GetByNumeroNfseAsync(nfse, Arg.Any<CancellationToken>())
            .Returns(new List<Domain.Credito>());

        output.WriteLine($"── INPUT: NumeroNfse = \"{nfse}\"");

        await CriarHandler().Handle(new Query(nfse), CancellationToken.None);

        output.WriteLine("── RESULTADO ────────────────────────────");
        output.WriteLine($"PublishAuditoriaAsync: chamado 1x");
        output.WriteLine($"  Tipo      = \"PorNfse\"");
        output.WriteLine($"  Parametro = \"{nfse}\" ✅");

        await _publisher.Received(1).PublishAuditoriaAsync(
            Arg.Is<ConsultaRealizadaMessage>(m => m.Tipo == "PorNfse" && m.Parametro == nfse),
            Arg.Any<CancellationToken>());
    }

    [Fact(DisplayName = "Handle: simplesNacional=true no banco → serializado como 'Sim' no response JSON")]
    public async Task Handle_DeveMapearSimplesNacionalParaString()
    {
        var nfse = "NF-SIMPLES";
        var creditos = new List<Domain.Credito>
        {
            Domain.Credito.Create("CR-SIMPLES-01", nfse, new DateOnly(2024, 3, 1),
                500m, "ISSQN", true, 5m, 10000m, 0m, 10000m)
        };
        _repository.GetByNumeroNfseAsync(nfse, Arg.Any<CancellationToken>()).Returns(creditos);

        output.WriteLine($"── INPUT: NumeroNfse = \"{nfse}\"");
        output.WriteLine($"── SETUP: Domain.Credito.SimplesNacional = true (bool)");

        var result = await CriarHandler().Handle(new Query(nfse), CancellationToken.None);

        output.WriteLine($"── RESULTADO: Response.SimplesNacional = \"{result.Value[0].SimplesNacional}\" ✅");

        result.Value[0].SimplesNacional.Should().Be("Sim");
    }
}

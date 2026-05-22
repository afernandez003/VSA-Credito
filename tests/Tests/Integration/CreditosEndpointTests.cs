using System.Net;
using System.Net.Http.Json;
using Creditos.Infra.Data;
using Creditos.Messaging;
using Microsoft.Extensions.DependencyInjection;

namespace Creditos.Tests.Integration;

public class CreditosEndpointTests : IClassFixture<ApiFactory>
{
    private readonly ApiFactory _factory;
    private readonly HttpClient _client;

    public CreditosEndpointTests(ApiFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    private static IReadOnlyList<Credito.IntegrarCredito.CreditoRequest> CriarPayload(params string[] numeros) =>
        numeros.Select(n => new Credito.IntegrarCredito.CreditoRequest(
            NumeroCredito: n,
            NumeroNfse: "7891011",
            DataConstituicao: new DateOnly(2024, 3, 1),
            ValorIssqn: 500.00m,
            TipoCredito: "ISSQN",
            SimplesNacional: "Não",
            Aliquota: 5.00m,
            ValorFaturado: 10000.00m,
            ValorDeducao: 0m,
            BaseCalculo: 10000.00m)).ToList();

    [Fact]
    public async Task POST_IntegrarCredito_DeveRetornar202()
    {
        var payload = CriarPayload("TEST001", "TEST002");

        var response = await _client.PostAsJsonAsync(
            "/api/creditos/integrar-credito-constituido", payload);

        response.StatusCode.Should().Be(HttpStatusCode.Accepted);
    }

    [Fact]
    public async Task POST_IntegrarCredito_DevePublicarMensagensNoPublisher()
    {
        _factory.Publisher.ClearReceivedCalls();
        var payload = CriarPayload("PUB001", "PUB002", "PUB003");

        await _client.PostAsJsonAsync("/api/creditos/integrar-credito-constituido", payload);

        await _factory.Publisher.Received(3).PublishAsync(
            Arg.Any<CreditoMessage>(),
            Arg.Any<string>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GET_CreditosByNfse_DeveRetornar200()
    {
        var response = await _client.GetAsync("/api/creditos/7891011");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GET_CreditosByNfse_ComDadosNaBase_DeveRetornarCreditos()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<CreditosDbContext>();
        db.Creditos.Add(Domain.Credito.Create(
            "GET_NFSE_001", "NFSE_888", new DateOnly(2024, 3, 1),
            500m, "ISSQN", false, 5m, 10000m, 0m, 10000m));
        await db.SaveChangesAsync();

        var response = await _client.GetAsync("/api/creditos/NFSE_888");
        var creditos = await response.Content.ReadFromJsonAsync<List<Credito.GetCreditosByNfse.Response>>();

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        creditos.Should().NotBeEmpty();
        creditos!.First().NumeroCredito.Should().Be("GET_NFSE_001");
    }

    [Fact]
    public async Task GET_CreditoByNumero_Existente_DeveRetornar200()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<CreditosDbContext>();
        db.Creditos.Add(Domain.Credito.Create(
            "GET_NUM_001", "9999999", new DateOnly(2024, 3, 1),
            500m, "ISSQN", false, 5m, 10000m, 0m, 10000m));
        await db.SaveChangesAsync();

        var response = await _client.GetAsync("/api/creditos/credito/GET_NUM_001");
        var credito = await response.Content.ReadFromJsonAsync<Credito.GetCreditoByNumero.Response>();

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        credito!.NumeroCredito.Should().Be("GET_NUM_001");
    }

    [Fact]
    public async Task GET_CreditoByNumero_NaoExistente_DeveRetornar404()
    {
        var response = await _client.GetAsync("/api/creditos/credito/CREDITO_INEXISTENTE");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GET_Self_DeveRetornar200()
    {
        var response = await _client.GetAsync("/self");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GET_Ready_DeveRetornar200()
    {
        var response = await _client.GetAsync("/ready");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}

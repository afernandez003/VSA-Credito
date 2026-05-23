using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Creditos.Infra.Data;
using Creditos.Messaging;
using Microsoft.Extensions.DependencyInjection;
using Xunit.Abstractions;

namespace Creditos.Tests.Integration;

public class CreditosEndpointTests : IClassFixture<ApiFactory>
{
    private readonly ApiFactory _factory;
    private readonly HttpClient _client;
    private readonly ITestOutputHelper _output;

    private static readonly JsonSerializerOptions JsonOpts = new() { WriteIndented = true };

    public CreditosEndpointTests(ApiFactory factory, ITestOutputHelper output)
    {
        _factory = factory;
        _client = factory.CreateClient();
        _output = output;
    }

    private async Task LogRequestResponse(string method, string url, object? body, HttpResponseMessage response)
    {
        _output.WriteLine($"── REQUEST ──────────────────────────────");
        _output.WriteLine($"{method} {url}");
        if (body is not null)
            _output.WriteLine(JsonSerializer.Serialize(body, JsonOpts));

        _output.WriteLine($"── RESPONSE ─────────────────────────────");
        _output.WriteLine($"Status: {(int)response.StatusCode} {response.StatusCode}");
        var content = await response.Content.ReadAsStringAsync();
        if (!string.IsNullOrWhiteSpace(content))
        {
            try
            {
                var formatted = JsonSerializer.Serialize(
                    JsonSerializer.Deserialize<object>(content), JsonOpts);
                _output.WriteLine(formatted);
            }
            catch
            {
                _output.WriteLine(content);
            }
        }
        _output.WriteLine($"─────────────────────────────────────────");
    }

    [Fact(DisplayName = "POST /integrar-credito-constituido: payload com 2 créditos → 202 Accepted")]
    public async Task POST_IntegrarCredito_DeveRetornar202()
    {
        var payload = CreditoFakers.GerarRequests(2, seed: 10);
        var url = "/api/creditos/integrar-credito-constituido";

        var response = await _client.PostAsJsonAsync(url, payload);

        await LogRequestResponse("POST", url, payload, response);
        response.StatusCode.Should().Be(HttpStatusCode.Accepted);
    }

    [Fact(DisplayName = "POST /integrar-credito-constituido: payload com 3 créditos → publisher recebe exatamente 3 mensagens no bus")]
    public async Task POST_IntegrarCredito_DevePublicarMensagensNoPublisher()
    {
        _factory.Publisher.ClearReceivedCalls();
        var payload = CreditoFakers.GerarRequests(3, seed: 20);
        var url = "/api/creditos/integrar-credito-constituido";

        var response = await _client.PostAsJsonAsync(url, payload);

        await LogRequestResponse("POST", url, payload, response);
        await _factory.Publisher.Received(3).PublishAsync(
            Arg.Any<CreditoMessage>(),
            Arg.Any<string>(),
            Arg.Any<CancellationToken>());
    }

    [Fact(DisplayName = "GET /api/creditos/{numeroNfse}: NFS-e sem créditos → 200 OK com lista vazia")]
    public async Task GET_CreditosByNfse_DeveRetornar200()
    {
        var url = "/api/creditos/NF-INEXISTENTE-GET";

        var response = await _client.GetAsync(url);

        await LogRequestResponse("GET", url, null, response);
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact(DisplayName = "GET /api/creditos/{numeroNfse}: NFS-e com crédito pré-existente → 200 OK com o crédito na lista")]
    public async Task GET_CreditosByNfse_ComDadosNaBase_DeveRetornarCreditos()
    {
        var credito = CreditoFakers.GerarDomainCredito(
            numeroCredito: "CR-GET-NFSE-01",
            numeroNfse: "NF-GET-NFSE-88",
            seed: 30);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<CreditosDbContext>();
        db.Creditos.Add(credito);
        await db.SaveChangesAsync();

        _output.WriteLine($"── DADO PRÉ-INSERIDO ─────────────────────");
        _output.WriteLine($"NumeroCredito : {credito.NumeroCredito}");
        _output.WriteLine($"NumeroNfse    : {credito.NumeroNfse}");
        _output.WriteLine($"ValorIssqn    : {credito.ValorIssqn}");
        _output.WriteLine($"TipoCredito   : {credito.TipoCredito}");
        _output.WriteLine($"SimplesNacional: {credito.SimplesNacional}");

        var url = $"/api/creditos/{credito.NumeroNfse}";
        var response = await _client.GetAsync(url);
        var creditos = await response.Content.ReadFromJsonAsync<List<Credito.GetCreditosByNfse.Response>>();

        await LogRequestResponse("GET", url, null, response);
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        creditos.Should().NotBeEmpty();
        creditos!.First().NumeroCredito.Should().Be(credito.NumeroCredito);
    }

    [Fact(DisplayName = "GET /api/creditos/credito/{numeroCredito}: crédito existente → 200 OK com os dados do crédito")]
    public async Task GET_CreditoByNumero_Existente_DeveRetornar200()
    {
        var credito = CreditoFakers.GerarDomainCredito(
            numeroCredito: "CR-GET-NUM-01",
            numeroNfse: "NF-GET-NUM-99",
            seed: 40);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<CreditosDbContext>();
        db.Creditos.Add(credito);
        await db.SaveChangesAsync();

        _output.WriteLine($"── DADO PRÉ-INSERIDO ─────────────────────");
        _output.WriteLine($"NumeroCredito : {credito.NumeroCredito}");
        _output.WriteLine($"NumeroNfse    : {credito.NumeroNfse}");
        _output.WriteLine($"ValorIssqn    : {credito.ValorIssqn}");
        _output.WriteLine($"TipoCredito   : {credito.TipoCredito}");
        _output.WriteLine($"SimplesNacional: {credito.SimplesNacional}");

        var url = $"/api/creditos/credito/{credito.NumeroCredito}";
        var response = await _client.GetAsync(url);
        var result = await response.Content.ReadFromJsonAsync<Credito.GetCreditoByNumero.Response>();

        await LogRequestResponse("GET", url, null, response);
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        result!.NumeroCredito.Should().Be(credito.NumeroCredito);
    }

    [Fact(DisplayName = "GET /api/creditos/credito/{numeroCredito}: crédito inexistente → 404 Not Found com ProblemDetails")]
    public async Task GET_CreditoByNumero_NaoExistente_DeveRetornar404()
    {
        var url = "/api/creditos/credito/CR-NUNCA-EXISTIU";

        var response = await _client.GetAsync(url);

        await LogRequestResponse("GET", url, null, response);
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact(DisplayName = "GET /self: health liveness → 200 OK (serviço está no ar)")]
    public async Task GET_Self_DeveRetornar200()
    {
        var url = "/self";

        var response = await _client.GetAsync(url);

        await LogRequestResponse("GET", url, null, response);
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact(DisplayName = "GET /ready: health readiness → 200 OK (DbContext acessível, banco pronto)")]
    public async Task GET_Ready_DeveRetornar200()
    {
        var url = "/ready";

        var response = await _client.GetAsync(url);

        await LogRequestResponse("GET", url, null, response);
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}

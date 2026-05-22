using Mediator;

namespace Creditos.Api.Endpoints;

internal static class CreditoEndpoints
{
    internal static IEndpointRouteBuilder MapCreditoEndpoints(this IEndpointRouteBuilder app)
    {
        var creditos = app.MapGroup("/api/creditos").WithTags("Creditos");

        creditos.MapPost("/integrar-credito-constituido", async (
            IReadOnlyList<Credito.IntegrarCredito.CreditoRequest> body,
            IMediator mediator,
            CancellationToken ct) =>
        {
            var result = await mediator.Send(new Credito.IntegrarCredito.Command(body), ct);
            return result.Match(_ => TypedResults.Json(new { success = true }, statusCode: StatusCodes.Status202Accepted));
        })
        .WithName("IntegrarCredito")
        .WithSummary("Integra uma lista de créditos constituídos publicando no tópico Kafka")
        .Produces(StatusCodes.Status202Accepted)
        .ProducesValidationProblem();

        creditos.MapGet("/{numeroNfse}", async (
            string numeroNfse,
            IMediator mediator,
            CancellationToken ct) =>
        {
            var result = await mediator.Send(new Credito.GetCreditosByNfse.Query(numeroNfse), ct);
            return result.Match();
        })
        .WithName("GetCreditosByNfse")
        .WithSummary("Retorna créditos constituídos pelo número da NFS-e")
        .Produces<IReadOnlyList<Credito.GetCreditosByNfse.Response>>();

        creditos.MapGet("/credito/{numeroCredito}", async (
            string numeroCredito,
            IMediator mediator,
            CancellationToken ct) =>
        {
            var result = await mediator.Send(new Credito.GetCreditoByNumero.Query(numeroCredito), ct);
            return result.Match();
        })
        .WithName("GetCreditoByNumero")
        .WithSummary("Retorna os detalhes de um crédito constituído pelo seu número")
        .Produces<Credito.GetCreditoByNumero.Response>()
        .ProducesProblem(StatusCodes.Status404NotFound);

        return app;
    }
}

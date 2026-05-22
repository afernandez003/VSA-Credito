using Creditos.Infra.Data;

namespace Creditos.Api.Health;

internal static class HealthEndpoints
{
    internal static IEndpointRouteBuilder MapHealthEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/self", () => TypedResults.Ok(new { status = "ok" }))
            .WithName("Self")
            .WithSummary("Liveness — verifica se o processo está ativo");

        app.MapGet("/ready", async (CreditosDbContext db, CancellationToken ct) =>
        {
            var canConnect = await db.Database.CanConnectAsync(ct);
            return canConnect
                ? TypedResults.Ok(new { status = "ready" })
                : (IResult)TypedResults.StatusCode(StatusCodes.Status503ServiceUnavailable);
        })
        .WithName("Ready")
        .WithSummary("Readiness — verifica se o banco de dados está acessível");

        return app;
    }
}

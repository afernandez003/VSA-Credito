namespace Creditos.Api.Pipeline;

internal sealed class CorrelationIdMiddleware(RequestDelegate next)
{
    private const string Header = "X-Correlation-Id";

    public async Task InvokeAsync(HttpContext context)
    {
        if (!context.Request.Headers.TryGetValue(Header, out var correlationId) ||
            string.IsNullOrWhiteSpace(correlationId))
        {
            correlationId = Guid.CreateVersion7().ToString();
        }

        context.Response.Headers[Header] = correlationId;

        using (Serilog.Context.LogContext.PushProperty("CorrelationId", correlationId.ToString()))
        {
            await next(context);
        }
    }
}

internal static class CorrelationIdExtensions
{
    public static IApplicationBuilder UseCorrelationId(this IApplicationBuilder app)
        => app.UseMiddleware<CorrelationIdMiddleware>();
}

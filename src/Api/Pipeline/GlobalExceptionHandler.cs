using System.Data.Common;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore.Storage;

namespace Creditos.Api.Pipeline;

internal sealed partial class GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger)
    : IExceptionHandler
{
    private const string CorrelationHeader = "X-Correlation-Id";

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        if (exception is OperationCanceledException && cancellationToken.IsCancellationRequested)
        {
            return true;
        }

        var correlationId = httpContext.Response.Headers[CorrelationHeader].FirstOrDefault()
            ?? httpContext.Request.Headers[CorrelationHeader].FirstOrDefault()
            ?? httpContext.TraceIdentifier;

        var traceId = System.Diagnostics.Activity.Current?.Id ?? httpContext.TraceIdentifier;

        if (exception is OperationCanceledException or DbException or RetryLimitExceededException)
        {
            LogDatabaseUnavailable(logger, correlationId, exception);
            await WriteAsync(httpContext, StatusCodes.Status503ServiceUnavailable,
                "Serviço indisponível", "Tente novamente em instantes.", correlationId, traceId, cancellationToken);
            return true;
        }

        if (exception is BadHttpRequestException badHttpEx)
        {
            await WriteAsync(httpContext, badHttpEx.StatusCode,
                "Requisição inválida", badHttpEx.InnerException?.Message ?? badHttpEx.Message,
                correlationId, traceId, cancellationToken);
            return true;
        }

        LogUnhandledException(logger, correlationId, exception);
        await WriteAsync(httpContext, StatusCodes.Status500InternalServerError,
            "Erro interno do servidor", null, correlationId, traceId, cancellationToken);
        return true;
    }

    private static async Task WriteAsync(
        HttpContext ctx, int status, string title, string? detail,
        string correlationId, string traceId, CancellationToken ct)
    {
        var problem = new ProblemDetails
        {
            Status = status,
            Title = title,
            Detail = detail,
            Type = "https://datatracker.ietf.org/doc/html/rfc9457"
        };
        problem.Extensions["correlationId"] = correlationId;
        problem.Extensions["traceId"] = traceId;
        ctx.Response.StatusCode = status;
        await ctx.Response.WriteAsJsonAsync(problem, ct);
    }

    [LoggerMessage(Level = LogLevel.Error, Message = "Exceção não tratada [CorrelationId: {CorrelationId}]")]
    private static partial void LogUnhandledException(ILogger logger, string correlationId, Exception ex);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Banco de dados inacessível [CorrelationId: {CorrelationId}]")]
    private static partial void LogDatabaseUnavailable(ILogger logger, string correlationId, Exception ex);
}

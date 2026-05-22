using Creditos.Results;
using Microsoft.AspNetCore.Mvc;

namespace Creditos.Api.Endpoints;

public static class ResultExtensions
{
    public static IResult Match(this Result result)
    {
        return result.IsSuccess
            ? TypedResults.NoContent()
            : new ProblemDetailsResult(result.Error);
    }

    public static IResult Match<TValue>(this Result<TValue> result)
    {
        return result.IsSuccess
            ? result.Value is null ? TypedResults.NoContent() : TypedResults.Ok(result.Value)
            : new ProblemDetailsResult(result.Error);
    }

    public static IResult Match<TValue>(this Result<TValue> result, Func<TValue, IResult> onSuccess)
    {
        return result.IsSuccess
            ? onSuccess(result.Value!)
            : new ProblemDetailsResult(result.Error);
    }

    private sealed class ProblemDetailsResult(Error error) : IResult
    {
        public async Task ExecuteAsync(HttpContext httpContext)
        {
            var correlationId = httpContext.Request.Headers["X-Correlation-Id"].FirstOrDefault()
                ?? httpContext.TraceIdentifier;

            if (error.ValidationErrors is { Count: > 0 })
            {
                httpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
                await httpContext.Response.WriteAsJsonAsync(
                    new ValidationProblemDetails(error.ValidationErrors)
                    {
                        Title = "Validation failed",
                        Status = StatusCodes.Status400BadRequest,
                        Extensions = { ["errorCode"] = error.Code, ["correlationId"] = correlationId }
                    });
                return;
            }

            var statusCode = error.Type switch
            {
                ErrorType.NotFound => StatusCodes.Status404NotFound,
                ErrorType.Conflict => StatusCodes.Status409Conflict,
                ErrorType.Unauthorized => StatusCodes.Status401Unauthorized,
                ErrorType.Forbidden => StatusCodes.Status403Forbidden,
                ErrorType.Internal => StatusCodes.Status500InternalServerError,
                _ => StatusCodes.Status400BadRequest
            };

            await TypedResults.Problem(
                statusCode: statusCode,
                title: error.Type.ToString(),
                detail: error.Message,
                extensions: new Dictionary<string, object?> { ["errorCode"] = error.Code, ["correlationId"] = correlationId })
                .ExecuteAsync(httpContext);
        }
    }
}

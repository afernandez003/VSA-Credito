#pragma warning disable CA1716 // 'Error' conflicts with VB.NET keyword — intentional, project is C#-only
namespace Creditos.Results;

public enum ErrorType
{
    Failure = 0,
    Validation = 1,
    NotFound = 2,
    Conflict = 3,
    Unauthorized = 4,
    Forbidden = 5,
    Internal = 6
}

public sealed record Error(string Code, string Message, ErrorType Type = ErrorType.Failure)
{
    public string? CorrelationId { get; init; }
    public Dictionary<string, string[]>? ValidationErrors { get; init; }

    public static readonly Error None = new(string.Empty, string.Empty);
    public static readonly Error NullValue = new("Error.NullValue", "O valor fornecido é nulo.", ErrorType.Failure);

    public static Error Failure(string code, string message) => new(code, message, ErrorType.Failure);
    public static Error NotFound(string code, string message) => new(code, message, ErrorType.NotFound);
    public static Error Validation(string code, string message) => new(code, message, ErrorType.Validation);
    public static Error Conflict(string code, string message) => new(code, message, ErrorType.Conflict);
    public static Error Unauthorized(string code, string message) => new(code, message, ErrorType.Unauthorized);
    public static Error Forbidden(string code, string message) => new(code, message, ErrorType.Forbidden);
    public static Error Internal(string code, string message) => new(code, message, ErrorType.Internal);

    public static implicit operator Result(Error error) => Result.Failure(error);
}
#pragma warning restore CA1716

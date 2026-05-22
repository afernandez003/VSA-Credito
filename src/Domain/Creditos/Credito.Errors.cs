using Creditos.Results;

namespace Creditos.Domain;

public sealed partial class Credito
{
    public static class Errors
    {
        public static Error NotFound(string numeroCredito) =>
            Error.NotFound("CREDITO_NOT_FOUND", $"Crédito '{numeroCredito}' não encontrado.");

        public static Error AlreadyExists(string numeroCredito) =>
            Error.Conflict("CREDITO_EXISTS", $"Crédito '{numeroCredito}' já foi integrado.");
    }
}

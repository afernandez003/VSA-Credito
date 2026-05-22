using Creditos.Results;
using Mediator;

namespace Creditos;

public static partial class Credito
{
    public static partial class GetCreditoByNumero
    {
        public sealed record Query(string NumeroCredito) : IQuery<Result<Response>>;
    }
}

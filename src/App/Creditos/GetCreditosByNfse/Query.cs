using Creditos.Results;
using Mediator;

namespace Creditos;

public static partial class Credito
{
    public static partial class GetCreditosByNfse
    {
        public sealed record Query(string NumeroNfse) : IQuery<Result<IReadOnlyList<Response>>>;
    }
}

using Creditos.Results;
using Mediator;

namespace Creditos;

public static partial class Credito
{
    public static partial class IntegrarCredito
    {
        public sealed record CreditoRequest(
            string NumeroCredito,
            string NumeroNfse,
            DateOnly DataConstituicao,
            decimal ValorIssqn,
            string TipoCredito,
            string SimplesNacional,
            decimal Aliquota,
            decimal ValorFaturado,
            decimal ValorDeducao,
            decimal BaseCalculo);

        public sealed record Command(IReadOnlyList<CreditoRequest> Creditos) : ICommand<Result<Response>>;
    }
}

namespace Creditos;

public static partial class Credito
{
    public static partial class GetCreditosByNfse
    {
        public sealed record Response(
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
    }
}

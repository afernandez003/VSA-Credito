namespace Creditos.Domain;

public sealed partial class Credito
{
    public static Credito Create(
        string numeroCredito,
        string numeroNfse,
        DateOnly dataConstituicao,
        decimal valorIssqn,
        string tipoCredito,
        bool simplesNacional,
        decimal aliquota,
        decimal valorFaturado,
        decimal valorDeducao,
        decimal baseCalculo)
    {
        return new Credito
        {
            NumeroCredito = numeroCredito.Trim(),
            NumeroNfse = numeroNfse.Trim(),
            DataConstituicao = dataConstituicao,
            ValorIssqn = valorIssqn,
            TipoCredito = tipoCredito.Trim(),
            SimplesNacional = simplesNacional,
            Aliquota = aliquota,
            ValorFaturado = valorFaturado,
            ValorDeducao = valorDeducao,
            BaseCalculo = baseCalculo
        };
    }
}

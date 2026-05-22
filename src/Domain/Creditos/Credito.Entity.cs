namespace Creditos.Domain;

public sealed partial class Credito
{
    public long Id { get; private set; }
    public string NumeroCredito { get; private set; } = default!;
    public string NumeroNfse { get; private set; } = default!;
    public DateOnly DataConstituicao { get; private set; }
    public decimal ValorIssqn { get; private set; }
    public string TipoCredito { get; private set; } = default!;
    public bool SimplesNacional { get; private set; }
    public decimal Aliquota { get; private set; }
    public decimal ValorFaturado { get; private set; }
    public decimal ValorDeducao { get; private set; }
    public decimal BaseCalculo { get; private set; }

    private Credito() { }
}

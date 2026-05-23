using Creditos.Messaging;
using static Creditos.Credito.IntegrarCredito;

namespace Creditos.Tests.Fakers;

/// <summary>
/// Geradores de dados realistas para testes usando Bogus (seed fixo → reproduzível).
/// Seed padrão: 42. Passe um seed diferente para cenários específicos.
/// </summary>
public static class CreditoFakers
{
    private static readonly string[] TiposCredito = ["ISSQN", "ISS"];
    private static readonly string[] SimplesNacionalOpcoes = ["Sim", "Não"];

    // ── CreditoRequest (POST /integrar-credito-constituido) ───────────────────

    public static Faker<CreditoRequest> RequestFaker(int seed = 42) =>
        new Faker<CreditoRequest>("pt_BR")
            .UseSeed(seed)
            .CustomInstantiator(f => new CreditoRequest(
                NumeroCredito: f.Random.Replace("CR-######"),
                NumeroNfse: f.Random.Replace("NF-#######"),
                DataConstituicao: DateOnly.FromDateTime(f.Date.Past(3)),
                ValorIssqn: Math.Round(f.Finance.Amount(10, 50_000), 2),
                TipoCredito: f.PickRandom(TiposCredito),
                SimplesNacional: f.PickRandom(SimplesNacionalOpcoes),
                Aliquota: Math.Round(f.Finance.Amount(2, 5), 2),
                ValorFaturado: Math.Round(f.Finance.Amount(1_000, 1_000_000), 2),
                ValorDeducao: Math.Round(f.Finance.Amount(0, 500), 2),
                BaseCalculo: Math.Round(f.Finance.Amount(1_000, 1_000_000), 2)));

    public static CreditoRequest GerarRequest(int seed = 42) =>
        RequestFaker(seed).Generate();

    public static IReadOnlyList<CreditoRequest> GerarRequests(int quantidade, int seed = 42) =>
        RequestFaker(seed).Generate(quantidade);

    // ── CreditoMessage (consumido pelo Worker) ────────────────────────────────

    public static Faker<CreditoMessage> MessageFaker(int seed = 42) =>
        new Faker<CreditoMessage>("pt_BR")
            .UseSeed(seed)
            .CustomInstantiator(f => new CreditoMessage(
                NumeroCredito: f.Random.Replace("CR-######"),
                NumeroNfse: f.Random.Replace("NF-#######"),
                DataConstituicao: DateOnly.FromDateTime(f.Date.Past(3)),
                ValorIssqn: Math.Round(f.Finance.Amount(10, 50_000), 2),
                TipoCredito: f.PickRandom(TiposCredito),
                SimplesNacional: f.Random.Bool(),
                Aliquota: Math.Round(f.Finance.Amount(2, 5), 2),
                ValorFaturado: Math.Round(f.Finance.Amount(1_000, 1_000_000), 2),
                ValorDeducao: Math.Round(f.Finance.Amount(0, 500), 2),
                BaseCalculo: Math.Round(f.Finance.Amount(1_000, 1_000_000), 2)));

    public static CreditoMessage GerarMessage(int seed = 42) =>
        MessageFaker(seed).Generate();

    // ── Domain.Credito (aggregate root — factory method) ─────────────────────

    public static Domain.Credito GerarDomainCredito(
        string? numeroCredito = null,
        string? numeroNfse = null,
        int seed = 42)
    {
        var f = new Faker("pt_BR") { Random = new Randomizer(seed) };
        return Domain.Credito.Create(
            numeroCredito ?? f.Random.Replace("CR-######"),
            numeroNfse ?? f.Random.Replace("NF-#######"),
            DateOnly.FromDateTime(f.Date.Past(3)),
            Math.Round(f.Finance.Amount(10, 50_000), 2),
            f.PickRandom(TiposCredito),
            f.Random.Bool(),
            Math.Round(f.Finance.Amount(2, 5), 2),
            Math.Round(f.Finance.Amount(1_000, 1_000_000), 2),
            Math.Round(f.Finance.Amount(0, 500), 2),
            Math.Round(f.Finance.Amount(1_000, 1_000_000), 2));
    }
}

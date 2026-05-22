namespace Creditos.Messaging;

public sealed record CreditoMessage(
    string NumeroCredito,
    string NumeroNfse,
    DateOnly DataConstituicao,
    decimal ValorIssqn,
    string TipoCredito,
    bool SimplesNacional,
    decimal Aliquota,
    decimal ValorFaturado,
    decimal ValorDeducao,
    decimal BaseCalculo);

public sealed record ConsultaRealizadaMessage(
    string Tipo,
    string Parametro,
    DateTimeOffset RealizadaEm);

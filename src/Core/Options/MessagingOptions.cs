namespace Creditos.Options;

public sealed class MessagingOptions
{
    public const string SectionName = "Messaging";

    public string BootstrapServers { get; init; } = "localhost:9092";
    public string TopicoIntegracao { get; init; } = "integrar-credito-constituido-entry";
    public string TopicoAuditoria { get; init; } = "consulta-credito-entry";
    public string GroupId { get; init; } = "creditos-worker";
}

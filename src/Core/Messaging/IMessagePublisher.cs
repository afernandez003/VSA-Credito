namespace Creditos.Messaging;

public interface IMessagePublisher
{
    Task PublishAsync(CreditoMessage message, string topico, CancellationToken ct = default);
    Task PublishAuditoriaAsync(ConsultaRealizadaMessage message, CancellationToken ct = default);
}

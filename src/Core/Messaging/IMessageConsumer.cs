namespace Creditos.Messaging;

public interface IMessageConsumer
{
    Task<CreditoMessage?> ConsumeAsync(CancellationToken ct = default);
}

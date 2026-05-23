using Confluent.Kafka;
using Creditos.Messaging;
using Creditos.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Text.Json;

namespace Creditos.Infra.Messaging;

public sealed partial class KafkaAdapter : IMessagePublisher, IMessageConsumer, IDisposable
{
    private readonly IProducer<string, string> _producer;
    private readonly IConsumer<string, string> _consumer;
    private readonly MessagingOptions _options;
    private readonly ILogger<KafkaAdapter> _logger;
    private bool _subscribed;

    [LoggerMessage(Level = LogLevel.Information, Message = "Mensagem publicada no tópico {Topico} — partition={Partition}, offset={Offset}")]
    private partial void LogPublicada(string topico, int partition, long offset);

    public KafkaAdapter(IOptions<MessagingOptions> options, ILogger<KafkaAdapter> logger)
    {
        _options = options.Value;
        _logger = logger;

        var producerConfig = new ProducerConfig
        {
            BootstrapServers = _options.BootstrapServers,
            Acks = Acks.All,
            EnableDeliveryReports = true
        };
        _producer = new ProducerBuilder<string, string>(producerConfig).Build();

        var consumerConfig = new ConsumerConfig
        {
            BootstrapServers = _options.BootstrapServers,
            GroupId = _options.GroupId,
            AutoOffsetReset = AutoOffsetReset.Earliest,
            EnableAutoCommit = false
        };
        _consumer = new ConsumerBuilder<string, string>(consumerConfig).Build();
    }

    public async Task PublishAsync(CreditoMessage message, string topico, CancellationToken ct = default)
    {
        var json = JsonSerializer.Serialize(message);
        var kafkaMessage = new Message<string, string>
        {
            Key = message.NumeroCredito,
            Value = json
        };

        var result = await _producer.ProduceAsync(topico, kafkaMessage, ct);
        LogPublicada(topico, result.Partition.Value, result.Offset.Value);
    }

    public async Task PublishAuditoriaAsync(ConsultaRealizadaMessage message, CancellationToken ct = default)
    {
        var json = JsonSerializer.Serialize(message);
        var kafkaMessage = new Message<string, string>
        {
            Key = message.Parametro,
            Value = json
        };

        await _producer.ProduceAsync(_options.TopicoAuditoria, kafkaMessage, ct);
    }

    public Task<CreditoMessage?> ConsumeAsync(CancellationToken ct = default)
    {
        if (!_subscribed)
        {
            _consumer.Subscribe(_options.TopicoIntegracao);
            _subscribed = true;
        }

        try
        {
            var result = _consumer.Consume(TimeSpan.FromMilliseconds(100));
            if (result is null)
            {
                return Task.FromResult<CreditoMessage?>(null);
            }

            var message = JsonSerializer.Deserialize<CreditoMessage>(result.Message.Value);
            _consumer.Commit(result);

            return Task.FromResult(message);
        }
        catch (ConsumeException ex) when (ex.Error.Code == ErrorCode.UnknownTopicOrPart)
        {
            return Task.FromResult<CreditoMessage?>(null);
        }
    }

    public void Dispose()
    {
        try { _producer.Flush(TimeSpan.FromSeconds(5)); } catch (Exception) { }
        try { _producer.Dispose(); } catch (Exception) { }
        try { _consumer.Close(); } catch (Exception) { }
        try { _consumer.Dispose(); } catch (Exception) { }
    }
}

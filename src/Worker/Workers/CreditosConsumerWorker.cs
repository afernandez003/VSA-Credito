using Creditos.Messaging;
using Mediator;
using Polly;
using Polly.CircuitBreaker;
using Polly.Retry;

namespace Creditos.Worker.Workers;

public sealed partial class CreditosConsumerWorker(
    IServiceProvider serviceProvider,
    IMessageConsumer consumer,
    ILogger<CreditosConsumerWorker> logger) : BackgroundService
{
    private static readonly TimeSpan PollingInterval = TimeSpan.FromMilliseconds(500);

    [LoggerMessage(Level = LogLevel.Information, Message = "CreditosConsumerWorker iniciado (intervalo: 500ms)")]
    private partial void LogIniciado();

    [LoggerMessage(Level = LogLevel.Information, Message = "CreditosConsumerWorker finalizado")]
    private partial void LogFinalizado();

    [LoggerMessage(Level = LogLevel.Warning, Message = "Falha transitória no ciclo de consumo ({Failures}x consecutivas)")]
    private partial void LogFalhaTransitoria(int failures);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        LogIniciado();

        var pipeline = new ResiliencePipelineBuilder()
            .AddRetry(new RetryStrategyOptions
            {
                MaxRetryAttempts = 3,
                Delay = TimeSpan.FromSeconds(1),
                BackoffType = DelayBackoffType.Exponential,
                ShouldHandle = new PredicateBuilder()
                    .Handle<Exception>(ex => ex is not OperationCanceledException)
            })
            .AddCircuitBreaker(new CircuitBreakerStrategyOptions
            {
                FailureRatio = 0.5,
                SamplingDuration = TimeSpan.FromSeconds(30),
                MinimumThroughput = 5,
                BreakDuration = TimeSpan.FromSeconds(30)
            })
            .Build();

        try
        {
            using var timer = new PeriodicTimer(PollingInterval);

            while (await timer.WaitForNextTickAsync(stoppingToken))
            {
                await pipeline.ExecuteAsync(async ct =>
                {
                    var message = await consumer.ConsumeAsync(ct);
                    if (message is null)
                    {
                        return;
                    }

                    await using var scope = serviceProvider.CreateAsyncScope();
                    var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
                    await mediator.Send(new Credito.ProcessarCredito.Command(message), ct);
                }, stoppingToken);
            }
        }
        catch (OperationCanceledException)
        {
            LogFinalizado();
        }
    }
}

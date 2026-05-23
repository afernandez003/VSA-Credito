using Creditos.Infra.Data;
using Creditos.Messaging;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Mediator;
using Testcontainers.Kafka;
using Testcontainers.PostgreSql;

namespace Creditos.Tests.Integration;

public class ApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly KafkaContainer _kafka = new KafkaBuilder()
        .WithImage("confluentinc/cp-kafka:7.6.1")
        .Build();

    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder()
        .WithImage("postgres:16-alpine")
        .Build();

    async Task IAsyncLifetime.InitializeAsync() =>
        await Task.WhenAll(_kafka.StartAsync(), _postgres.StartAsync());

    async Task IAsyncLifetime.DisposeAsync()
    {
        await base.DisposeAsync();
        await _kafka.DisposeAsync();
        await _postgres.DisposeAsync();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = _postgres.GetConnectionString(),
                ["Messaging:BootstrapServers"] = _kafka.GetBootstrapAddress()
            });
        });
    }

    public async Task<CreditosDbContext> GetDbContextAsync()
    {
        var scope = Services.CreateScope();
        return scope.ServiceProvider.GetRequiredService<CreditosDbContext>();
    }

    /// <summary>
    /// Consome e processa todas as mensagens pendentes no tópico Kafka.
    /// Para após 500ms sem receber mensagem (fila vazia).
    /// </summary>
    public async Task ProcessarMensagensPendentesAsync(int timeoutMs = 15000)
    {
        using var scope = Services.CreateScope();
        var consumer = scope.ServiceProvider.GetRequiredService<IMessageConsumer>();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

        var deadline = DateTime.UtcNow.AddMilliseconds(timeoutMs);
        var idleMs = 0;
        const int idleThresholdMs = 500;

        while (DateTime.UtcNow < deadline)
        {
            var msg = await consumer.ConsumeAsync();
            if (msg is not null)
            {
                await mediator.Send(new Credito.ProcessarCredito.Command(msg));
                idleMs = 0;
            }
            else
            {
                idleMs += 100;
                if (idleMs >= idleThresholdMs) break;
                await Task.Delay(100);
            }
        }
    }
}

using Creditos.Infra.Data;
using Creditos.Infra.Messaging;
using Creditos.Infra.Persistence;
using Creditos.Messaging;
using Creditos.Options;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Creditos.Infra.Extensions;

public static class InfrastructureExtensions
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddDbContext<CreditosDbContext>(options =>
            options.UseNpgsql(
                configuration.GetConnectionString("DefaultConnection"),
                npgsql => npgsql.EnableRetryOnFailure(3)));

        services.Configure<MessagingOptions>(configuration.GetSection(MessagingOptions.SectionName));

        // Singleton: IProducer<> do Confluent.Kafka é thread-safe e caro de instanciar
        services.AddSingleton<KafkaAdapter>();
        services.AddSingleton<IMessagePublisher>(sp => sp.GetRequiredService<KafkaAdapter>());
        services.AddSingleton<IMessageConsumer>(sp => sp.GetRequiredService<KafkaAdapter>());

        services.AddScoped<ICreditoRepository, EfCoreCreditoRepository>();

        return services;
    }
}

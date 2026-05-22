using Creditos.Infra.Data;
using Creditos.Infra.Messaging;
using Creditos.Messaging;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using NSubstitute;

namespace Creditos.Tests.Integration;

public class ApiFactory : WebApplicationFactory<Program>
{
    public IMessagePublisher Publisher { get; } = Substitute.For<IMessagePublisher>();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureServices(services =>
        {
            // Substituir PostgreSQL por InMemory
            // RemoveAll<IDbContextOptionsConfiguration<T>> remove a action que registra UseNpgsql,
            // evitando o erro "two database providers registered".
            services.RemoveAll(typeof(IDbContextOptionsConfiguration<CreditosDbContext>));
            services.RemoveAll<DbContextOptions<CreditosDbContext>>();
            services.RemoveAll<CreditosDbContext>();

            var dbName = "creditos-test-" + Guid.NewGuid().ToString("N");
            services.AddDbContext<CreditosDbContext>(options =>
                options.UseInMemoryDatabase(dbName));

            // Substituir KafkaAdapter por fakes
            services.RemoveAll<KafkaAdapter>();
            services.RemoveAll<IMessagePublisher>();
            services.RemoveAll<IMessageConsumer>();

            services.AddSingleton<IMessagePublisher>(Publisher);
            services.AddSingleton<IMessageConsumer>(Substitute.For<IMessageConsumer>());
        });
    }

    public async Task<CreditosDbContext> GetDbContextAsync()
    {
        var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<CreditosDbContext>();
        await db.Database.EnsureCreatedAsync();
        return db;
    }
}

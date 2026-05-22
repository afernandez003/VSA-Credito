using System.Globalization;
using Creditos;
using Creditos.Api.Pipeline;
using Creditos.Behaviors;
using Creditos.Infra.Data;
using Microsoft.EntityFrameworkCore;
using Creditos.Infra.Extensions;
using FluentValidation;
using Mediator;
using Serilog;

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .Enrich.FromLogContext()
    .WriteTo.Console(
        outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}",
        formatProvider: CultureInfo.InvariantCulture)
    .CreateLogger();

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((ctx, services, config) =>
    config
        .MinimumLevel.Information()
        .ReadFrom.Configuration(ctx.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext()
        .WriteTo.Console(
            outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}",
            formatProvider: CultureInfo.InvariantCulture));

builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

builder.Services.AddOpenApi(options =>
{
    options.AddDocumentTransformer((document, context, ct) =>
    {
        document.Info = new()
        {
            Title = "Creditos API — Desafio Técnico",
            Version = "v1",
            Description = "API REST para integração e consulta de créditos constituídos (ISSQN/NFS-e)."
        };
        return Task.CompletedTask;
    });
});

builder.Services.AddMediator((MediatorOptions options) =>
{
    options.ServiceLifetime = ServiceLifetime.Scoped;
    options.Assemblies = [typeof(Program).Assembly, typeof(CreditosModule).Assembly];
    options.PipelineBehaviors =
    [
        typeof(LoggingBehavior<,>),
        typeof(ValidationBehavior<,>)
    ];
});

builder.Services.AddValidatorsFromAssembly(typeof(CreditosModule).Assembly, ServiceLifetime.Scoped);
builder.Services.AddCreditosModule();

builder.Services.AddInfrastructure(builder.Configuration);

builder.Services.AddHealthChecks()
    .AddDbContextCheck<CreditosDbContext>("postgresql", tags: ["ready"]);

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    await scope.ServiceProvider.GetRequiredService<CreditosDbContext>().Database.MigrateAsync();
}

app.UseStandardPipeline();

app.Run();

public partial class Program;

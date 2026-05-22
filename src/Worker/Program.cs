using System.Globalization;
using Creditos;
using Creditos.Behaviors;
using Creditos.Infra.Extensions;
using Creditos.Worker.Workers;
using FluentValidation;
using Mediator;
using Serilog;

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .Enrich.FromLogContext()
    .WriteTo.Console(
        outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] [WORKER] {Message:lj}{NewLine}{Exception}",
        formatProvider: CultureInfo.InvariantCulture)
    .CreateBootstrapLogger();

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddSerilog((services, config) =>
    config
        .MinimumLevel.Information()
        .ReadFrom.Configuration(builder.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext()
        .WriteTo.Console(
            outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] [WORKER] {Message:lj}{NewLine}{Exception}",
            formatProvider: CultureInfo.InvariantCulture));

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

builder.Services.AddHostedService<CreditosConsumerWorker>();

var host = builder.Build();
await host.RunAsync();

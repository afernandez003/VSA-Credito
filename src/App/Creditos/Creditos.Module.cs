using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace Creditos;

public static class CreditosModule
{
    public static IServiceCollection AddCreditosModule(this IServiceCollection services)
    {
        services.AddScoped<IValidator<Credito.IntegrarCredito.Command>, Credito.IntegrarCredito.Validator>();
        services.AddScoped<IValidator<Credito.GetCreditosByNfse.Query>, Credito.GetCreditosByNfse.Validator>();
        services.AddScoped<IValidator<Credito.GetCreditoByNumero.Query>, Credito.GetCreditoByNumero.Validator>();
        return services;
    }
}

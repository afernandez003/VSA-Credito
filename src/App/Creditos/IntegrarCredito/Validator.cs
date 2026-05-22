using FluentValidation;

namespace Creditos;

public static partial class Credito
{
    public static partial class IntegrarCredito
    {
        public sealed class Validator : AbstractValidator<Command>
        {
            public Validator()
            {
                RuleFor(x => x.Creditos)
                    .NotEmpty().WithMessage("A lista de créditos não pode ser vazia.");

                RuleForEach(x => x.Creditos).ChildRules(credito =>
                {
                    credito.RuleFor(c => c.NumeroCredito).NotEmpty().MaximumLength(50);
                    credito.RuleFor(c => c.NumeroNfse).NotEmpty().MaximumLength(50);
                    credito.RuleFor(c => c.DataConstituicao)
                        .NotEmpty()
                        .LessThanOrEqualTo(DateOnly.FromDateTime(DateTime.UtcNow.Date))
                            .WithMessage("DataConstituicao não pode ser futura.");
                    credito.RuleFor(c => c.TipoCredito).NotEmpty().MaximumLength(50);
                    credito.RuleFor(c => c.SimplesNacional)
                        .NotEmpty()
                        .Must(v => v.Equals("Sim", StringComparison.OrdinalIgnoreCase)
                                || v.Equals("Não", StringComparison.OrdinalIgnoreCase)
                                || v.Equals("Nao", StringComparison.OrdinalIgnoreCase))
                            .WithMessage("SimplesNacional deve ser 'Sim' ou 'Não'.");
                    credito.RuleFor(c => c.ValorIssqn).GreaterThanOrEqualTo(0);
                    credito.RuleFor(c => c.Aliquota).InclusiveBetween(0, 100);
                    credito.RuleFor(c => c.ValorFaturado).GreaterThanOrEqualTo(0);
                    credito.RuleFor(c => c.ValorDeducao).GreaterThanOrEqualTo(0);
                    credito.RuleFor(c => c.BaseCalculo).GreaterThanOrEqualTo(0);
                });
            }
        }
    }
}

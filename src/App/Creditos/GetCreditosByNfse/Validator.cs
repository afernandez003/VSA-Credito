using FluentValidation;

namespace Creditos;

public static partial class Credito
{
    public static partial class GetCreditosByNfse
    {
        public sealed class Validator : AbstractValidator<Query>
        {
            public Validator()
            {
                RuleFor(q => q.NumeroNfse)
                    .NotEmpty().WithMessage("NumeroNfse é obrigatório.")
                    .MaximumLength(50).WithMessage("NumeroNfse deve ter no máximo 50 caracteres.");
            }
        }
    }
}

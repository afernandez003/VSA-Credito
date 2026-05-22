using FluentValidation;

namespace Creditos;

public static partial class Credito
{
    public static partial class GetCreditoByNumero
    {
        public sealed class Validator : AbstractValidator<Query>
        {
            public Validator()
            {
                RuleFor(q => q.NumeroCredito)
                    .NotEmpty().WithMessage("NumeroCredito é obrigatório.")
                    .MaximumLength(50).WithMessage("NumeroCredito deve ter no máximo 50 caracteres.");
            }
        }
    }
}

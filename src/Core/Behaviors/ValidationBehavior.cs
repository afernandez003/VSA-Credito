using FluentValidation;
using Mediator;
using Creditos.Results;

namespace Creditos.Behaviors;

public sealed class ValidationBehavior<TMessage, TResponse>(
    IEnumerable<IValidator<TMessage>> validators)
    : IPipelineBehavior<TMessage, TResponse>
    where TMessage : IMessage
    where TResponse : IResultOf<TResponse>
{
    public async ValueTask<TResponse> Handle(
        TMessage message,
        MessageHandlerDelegate<TMessage, TResponse> next,
        CancellationToken cancellationToken)
    {
        if (!validators.Any())
        {
            return await next(message, cancellationToken);
        }

        var context = new ValidationContext<TMessage>(message);

        var results = await Task.WhenAll(
            validators.Select(v => v.ValidateAsync(context, cancellationToken)));

        var failures = results
            .SelectMany(r => r.Errors)
            .Where(e => e is not null)
            .ToList();

        if (failures.Count > 0)
        {
            return CreateValidationFailure(failures);
        }

        return await next(message, cancellationToken);
    }

    private static TResponse CreateValidationFailure(List<FluentValidation.Results.ValidationFailure> failures)
    {
        var validationErrors = failures
            .GroupBy(e => e.PropertyName)
            .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray());

        var baseError = Error.Validation("VALIDATION_FAILED", "Um ou mais erros de validação ocorreram.");

        return TResponse.Failure(baseError with { ValidationErrors = validationErrors });
    }
}

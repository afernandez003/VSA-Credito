using Creditos.Messaging;
using Creditos.Results;
using Mediator;

namespace Creditos;

public static partial class Credito
{
    public static partial class ProcessarCredito
    {
        public sealed record Command(CreditoMessage Message) : ICommand<Result>;
    }
}

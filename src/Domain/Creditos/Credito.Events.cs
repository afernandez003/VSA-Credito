using Mediator;

namespace Creditos.Domain;

public sealed partial class Credito
{
    public record Integrado(string NumeroCredito, string NumeroNfse) : INotification
    {
        public const string Key = "Credito.Integrado";
    }
}

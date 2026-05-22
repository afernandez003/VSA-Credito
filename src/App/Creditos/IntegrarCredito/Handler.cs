using Creditos.Messaging;
using Creditos.Options;
using Creditos.Results;
using Mediator;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Creditos;

public static partial class Credito
{
    public static partial class IntegrarCredito
    {
        public sealed partial class Handler(
            IMessagePublisher publisher,
            IOptions<MessagingOptions> options,
            ILogger<Handler> logger)
            : ICommandHandler<Command, Result<Response>>
        {
            [LoggerMessage(Level = LogLevel.Information, Message = "Publicando {Count} crédito(s) no tópico {Topico}")]
            private partial void LogPublicando(int count, string topico);

            public async ValueTask<Result<Response>> Handle(Command cmd, CancellationToken cancellationToken)
            {
                var topico = options.Value.TopicoIntegracao;
                LogPublicando(cmd.Creditos.Count, topico);

                foreach (var req in cmd.Creditos)
                {
                    var message = new CreditoMessage(
                        req.NumeroCredito,
                        req.NumeroNfse,
                        req.DataConstituicao,
                        req.ValorIssqn,
                        req.TipoCredito,
                        ParseSimplesNacional(req.SimplesNacional),
                        req.Aliquota,
                        req.ValorFaturado,
                        req.ValorDeducao,
                        req.BaseCalculo);

                    await publisher.PublishAsync(message, topico, cancellationToken);
                }

                return Result<Response>.Success(new Response(true));
            }

            private static bool ParseSimplesNacional(string value) =>
                value.Trim().Equals("Sim", StringComparison.OrdinalIgnoreCase);
        }
    }
}

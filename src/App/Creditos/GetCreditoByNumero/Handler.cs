using Creditos.Messaging;
using Creditos.Results;
using Mediator;
using Microsoft.Extensions.Logging;

namespace Creditos;

public static partial class Credito
{
    public static partial class GetCreditoByNumero
    {
        public sealed partial class Handler(
            ICreditoRepository repository,
            IMessagePublisher publisher,
            ILogger<Handler> logger)
            : IQueryHandler<Query, Result<Response>>
        {
            [LoggerMessage(Level = LogLevel.Information, Message = "Consulta por crédito {NumeroCredito}: encontrado={Found}")]
            private partial void LogConsulta(string numeroCredito, bool found);

            public async ValueTask<Result<Response>> Handle(Query query, CancellationToken cancellationToken)
            {
                var credito = await repository.GetByNumeroCreditoAsync(query.NumeroCredito, cancellationToken);

                LogConsulta(query.NumeroCredito, credito is not null);

                await publisher.PublishAuditoriaAsync(
                    new ConsultaRealizadaMessage("PorNumero", query.NumeroCredito, DateTimeOffset.UtcNow),
                    cancellationToken);

                if (credito is null)
                {
                    return Result<Response>.Failure(Domain.Credito.Errors.NotFound(query.NumeroCredito));
                }

                return Result<Response>.Success(new Response(
                    credito.NumeroCredito,
                    credito.NumeroNfse,
                    credito.DataConstituicao,
                    credito.ValorIssqn,
                    credito.TipoCredito,
                    credito.SimplesNacional ? "Sim" : "Não",
                    credito.Aliquota,
                    credito.ValorFaturado,
                    credito.ValorDeducao,
                    credito.BaseCalculo));
            }
        }
    }
}

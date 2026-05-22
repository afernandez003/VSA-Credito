using Creditos.Messaging;
using Creditos.Results;
using Mediator;
using Microsoft.Extensions.Logging;

namespace Creditos;

public static partial class Credito
{
    public static partial class GetCreditosByNfse
    {
        public sealed partial class Handler(
            ICreditoRepository repository,
            IMessagePublisher publisher,
            ILogger<Handler> logger)
            : IQueryHandler<Query, Result<IReadOnlyList<Response>>>
        {
            [LoggerMessage(Level = LogLevel.Information, Message = "Consulta por NFS-e {NumeroNfse}: {Count} resultado(s)")]
            private partial void LogConsulta(string numeroNfse, int count);

            public async ValueTask<Result<IReadOnlyList<Response>>> Handle(Query query, CancellationToken cancellationToken)
            {
                var creditos = await repository.GetByNumeroNfseAsync(query.NumeroNfse, cancellationToken);

                LogConsulta(query.NumeroNfse, creditos.Count);

                var responses = creditos
                    .Select(c => new Response(
                        c.NumeroCredito,
                        c.NumeroNfse,
                        c.DataConstituicao,
                        c.ValorIssqn,
                        c.TipoCredito,
                        c.SimplesNacional ? "Sim" : "Não",
                        c.Aliquota,
                        c.ValorFaturado,
                        c.ValorDeducao,
                        c.BaseCalculo))
                    .ToList();

                await publisher.PublishAuditoriaAsync(
                    new ConsultaRealizadaMessage("PorNfse", query.NumeroNfse, DateTimeOffset.UtcNow),
                    cancellationToken);

                return Result<IReadOnlyList<Response>>.Success(responses);
            }
        }
    }
}

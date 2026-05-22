using Creditos.Results;
using Mediator;
using Microsoft.Extensions.Logging;

namespace Creditos;

public static partial class Credito
{
    public static partial class ProcessarCredito
    {
        public sealed partial class Handler(
            ICreditoRepository repository,
            ILogger<Handler> logger)
            : ICommandHandler<Command, Result>
        {
            [LoggerMessage(Level = LogLevel.Information, Message = "Crédito {NumeroCredito} inserido com sucesso")]
            private partial void LogInserido(string numeroCredito);

            [LoggerMessage(Level = LogLevel.Warning, Message = "Crédito {NumeroCredito} já existe — ignorado")]
            private partial void LogDuplicado(string numeroCredito);

            public async ValueTask<Result> Handle(Command cmd, CancellationToken cancellationToken)
            {
                var msg = cmd.Message;

                var exists = await repository.ExistsByNumeroCreditoAsync(msg.NumeroCredito, cancellationToken);
                if (exists)
                {
                    LogDuplicado(msg.NumeroCredito);
                    return Result.Success();
                }

                var credito = Domain.Credito.Create(
                    msg.NumeroCredito,
                    msg.NumeroNfse,
                    msg.DataConstituicao,
                    msg.ValorIssqn,
                    msg.TipoCredito,
                    msg.SimplesNacional,
                    msg.Aliquota,
                    msg.ValorFaturado,
                    msg.ValorDeducao,
                    msg.BaseCalculo);

                await repository.AddAsync(credito, cancellationToken);
                await repository.SaveChangesAsync(cancellationToken);

                LogInserido(msg.NumeroCredito);

                return Result.Success();
            }
        }
    }
}

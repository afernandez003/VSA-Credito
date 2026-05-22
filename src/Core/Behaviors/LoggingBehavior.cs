using System.Diagnostics;
using Mediator;
using Microsoft.Extensions.Logging;

namespace Creditos.Behaviors;

public sealed class LoggingBehavior<TMessage, TResponse>(
    ILogger<LoggingBehavior<TMessage, TResponse>> logger)
    : IPipelineBehavior<TMessage, TResponse>
    where TMessage : IMessage
{
    public async ValueTask<TResponse> Handle(
        TMessage message,
        MessageHandlerDelegate<TMessage, TResponse> next,
        CancellationToken cancellationToken)
    {
        var messageName = typeof(TMessage).Name;
        var sw = Stopwatch.StartNew();

        LogMessages.Handling(logger, messageName);

        try
        {
            var response = await next(message, cancellationToken);
            sw.Stop();
            LogMessages.Handled(logger, messageName, sw.ElapsedMilliseconds);
            return response;
        }
        catch (Exception ex)
        {
            sw.Stop();
            LogMessages.Failed(logger, ex, messageName, sw.ElapsedMilliseconds);
            throw;
        }
    }
}

internal static partial class LogMessages
{
    [LoggerMessage(Level = LogLevel.Information, Message = "Handling {MessageType}")]
    internal static partial void Handling(ILogger logger, string messageType);

    [LoggerMessage(Level = LogLevel.Information, Message = "Handled {MessageType} in {ElapsedMs}ms")]
    internal static partial void Handled(ILogger logger, string messageType, long elapsedMs);

    [LoggerMessage(Level = LogLevel.Error, Message = "Failed {MessageType} in {ElapsedMs}ms")]
    internal static partial void Failed(ILogger logger, Exception ex, string messageType, long elapsedMs);
}

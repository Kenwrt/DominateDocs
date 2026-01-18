using DominateDocsNotify.Models;

namespace DominateDocsNotify.Services;

public interface ITextBackgroundService
{
    event EventHandler<TextMsg> OnTextCompletedEvent;

    event EventHandler<TextMsg> OnTextErrorEvent;

    Task DoSomeCleanupAsync();

    Task DoSomeInitializationAsync();

    Task DoSomeRecoveryAsync();

    Task StartAsync(CancellationToken cancellationToken);

    Task StopAsync(CancellationToken cancellationToken);

    Task TextMsgProcessingAsync(TextMsg textMsg, CancellationToken stoppingToken);
}
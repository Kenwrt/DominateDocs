using DominateDocsNotify.Models;

namespace DominateDocsNotify.Services;

public interface IEmailBackgroundService
{
    event EventHandler<EmailMsg> OnEmailCompletedEvent;

    event EventHandler<EmailMsg> OnEmailErrorEvent;

    Task DoSomeCleanupAsync();

    Task DoSomeInitializationAsync();

    Task DoSomeRecoveryAsync();

    Task EmailSendAsync(EmailMsg email, CancellationToken stoppingToken);

    Task StartAsync(CancellationToken cancellationToken);

    Task StopAsync(CancellationToken cancellationToken);
}
using DominateDocsData.Models.Stripe;

namespace StripeBillingManager.Services;
public interface IStripeBillingManagerBackgroundService
{
    event EventHandler<LoanDocumentSetGeneratedEvent>? OnDocSetBillingCompletedEvent;
    event EventHandler<LoanDocumentSetGeneratedEvent>? OnDocSetBillingErrorEvent;
    event EventHandler<Subscription>? OnSubscriptionBillingCompletedEvent;
    event EventHandler<Subscription>? OnSubscriptionBillingErrorEvent;

    Task StartAsync(CancellationToken cancellationToken);
    Task StopAsync(CancellationToken cancellationToken);
}
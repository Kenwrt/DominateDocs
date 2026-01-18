using DominateDocsData.Models.Stripe;
using System.Collections.Concurrent;

namespace StripeBillingManager.State;
public interface IStripeBillingManagerState
{
    ConcurrentDictionary<Guid, LoanDocumentSetGeneratedEvent> DocSetEventBillingList { get; set; }
    ConcurrentQueue<LoanDocumentSetGeneratedEvent> DocSetEventBillingProcessingQueue { get; set; }
    DateTime HousekeeperLastRunTime { get; set; }
    bool IsActive { get; set; }
    bool IsHousekeeperActive { get; set; }
    bool IsReadyForProcessing { get; set; }
    bool IsRunBackgroundBillingService { get; set; }
    bool IsStartup { get; set; }
    DateTime ServiceLastRunTime { get; set; }
    ConcurrentDictionary<Guid, Subscription> SubscriptionList { get; set; }
    ConcurrentQueue<Subscription> SubscriptionProcessQueue { get; set; }

    event EventHandler<bool>? IsRunBackgroundBillingServiceChanged;
    event EventHandler<bool>? IsRunBackgroundHousekeeperServiceChanged;
    event EventHandler StateChanged;

    Task IsHousekeeperActiveHasChanged(bool val);
    Task IsRunBackgroundBillingServiceHasChanged(bool val);
    void StateHasChanged();
}
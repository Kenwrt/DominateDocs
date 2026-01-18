using DominateDocsData.Models;
using DominateDocsData.Models.Stripe;
using Microsoft.Extensions.Options;
using System.Collections.Concurrent;

namespace StripeBillingManager.State;

public class StripeBillingManagerState : IStripeBillingManagerState
{
    private IOptions<StripeBillingManagerConfigOptions> config;

    public bool IsReadyForProcessing { get; set; } = false;

    public ConcurrentDictionary<Guid, LoanDocumentSetGeneratedEvent> DocSetEventBillingList { get; set; } = new();

    public ConcurrentDictionary<Guid, Subscription> SubscriptionList { get; set; } = new();

    public ConcurrentQueue<LoanDocumentSetGeneratedEvent> DocSetEventBillingProcessingQueue { get; set; } = new();

    public ConcurrentQueue<Subscription> SubscriptionProcessQueue { get; set; } = new();


    private bool isRunBackgroundBillingService = false;

    public bool IsRunBackgroundBillingService
    {
        get
        {
            return isRunBackgroundBillingService;
        }
        set
        {
            isRunBackgroundBillingService = value;

            IsRunBackgroundBillingServiceHasChanged(value);
        }
    }


    private bool isHousekeeperActive = false;

    public bool IsHousekeeperActive
    {
        get
        {
            return isHousekeeperActive;
        }
        set
        {
            isHousekeeperActive = value;

            IsHousekeeperActiveHasChanged(value);
        }
    }

    public DateTime HousekeeperLastRunTime { get; set; } = default(DateTime);

    public DateTime ServiceLastRunTime { get; set; } = default(DateTime);

    public bool IsActive { get; set; }

    public bool IsStartup { get; set; } = false;

    public event EventHandler StateChanged;

    public event EventHandler<bool>? IsRunBackgroundBillingServiceChanged;

    public event EventHandler<bool>? IsRunBackgroundHousekeeperServiceChanged;

    public StripeBillingManagerState(IOptions<StripeBillingManagerConfigOptions> config)
    {
        this.config = config;

        IsRunBackgroundBillingService = config.Value.IsRunBackgroundBillingService;

        IsActive = config.Value.IsActive;
        IsHousekeeperActive = config.Value.IsHousekeeperActive;

        IsReadyForProcessing = true;

        StateHasChanged();
    }

    public async Task IsHousekeeperActiveHasChanged(bool val)
    {
        IsRunBackgroundHousekeeperServiceChanged?.Invoke(this, val);
    }

    public async Task IsRunBackgroundBillingServiceHasChanged(bool val)
    {
        IsRunBackgroundBillingServiceChanged?.Invoke(this, val);
    }


    public void StateHasChanged()
    {
        StateChanged?.Invoke(this, EventArgs.Empty);
    }
}
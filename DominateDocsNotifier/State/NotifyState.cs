using DominateDocsNotify.Models;
using Microsoft.Extensions.Options;
using System.Collections.Concurrent;
using static DominateDocsNotify.DominateDocsNotifyExtensions;

namespace DominateDocsNotify.State;

public class NotifyState : INotifyState
{
    private IOptions<NotifyConfigOptions> config;

    public bool IsReadyForProcessing { get; set; } = false;

    public ConcurrentDictionary<Guid, EmailMsg> EmailMsgList { get; set; } = new();

    public ConcurrentDictionary<Guid, TextMsg> TextMsgList { get; set; } = new();

    public ConcurrentQueue<EmailMsg> EmailMsgProcessingQueue { get; set; } = new();

    public ConcurrentQueue<TextMsg> TextMsgProcessingQueue { get; set; } = new();

    private bool isRunBackgroundEmailService = false;

    public bool IsRunBackgroundEmailService
    {
        get
        {
            return isRunBackgroundEmailService;
        }
        set
        {
            isRunBackgroundEmailService = value;

            IsRunBackgroundEmailServiceHasChanged(value);
        }
    }

    private bool isRunBackgroundTextService = false;

    public bool IsRunBackgroundTextService
    {
        get
        {
            return isRunBackgroundTextService;
        }
        set
        {
            isRunBackgroundTextService = value;

            IsRunBackgroundTextServiceHasChanged(value);
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

    public event EventHandler<bool> IsRunBackgroundEmailServiceChanged;

    public event EventHandler<bool> IsRunBackgroundTextServiceChanged;

    public event EventHandler<bool> IsRunBackgroundHousekeeperServiceChanged;

    public NotifyState(IOptions<NotifyConfigOptions> config)
    {
        this.config = config;

        IsRunBackgroundEmailService = config.Value.IsRunBackgroundEmailService;
        IsRunBackgroundTextService = config.Value.IsRunBackgroundTextService;
        IsActive = config.Value.IsActive;
        IsHousekeeperActive = config.Value.IsHousekeeperActive;

        IsReadyForProcessing = true;

        StateHasChanged();
    }

    private void IsHousekeeperActiveHasChanged(bool val)
    {
        IsRunBackgroundHousekeeperServiceChanged?.Invoke(this, val);
    }

    private void IsRunBackgroundEmailServiceHasChanged(bool val)
    {
        IsRunBackgroundEmailServiceChanged?.Invoke(this, val);
    }

    private void IsRunBackgroundTextServiceHasChanged(bool val)
    {
        IsRunBackgroundTextServiceChanged?.Invoke(this, val);
    }

    public void StateHasChanged()
    {
        StateChanged?.Invoke(this, EventArgs.Empty);
    }
}
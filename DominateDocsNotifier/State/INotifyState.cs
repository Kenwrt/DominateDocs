using DominateDocsNotify.Models;
using System.Collections.Concurrent;

namespace DominateDocsNotify.State;

public interface INotifyState
{
    ConcurrentDictionary<Guid, EmailMsg> EmailMsgList { get; set; }
    ConcurrentQueue<EmailMsg> EmailMsgProcessingQueue { get; set; }
    DateTime HousekeeperLastRunTime { get; set; }
    bool IsActive { get; set; }
    bool IsHousekeeperActive { get; set; }
    bool IsReadyForProcessing { get; set; }
    bool IsRunBackgroundEmailService { get; set; }
    bool IsRunBackgroundTextService { get; set; }
    bool IsStartup { get; set; }
    DateTime ServiceLastRunTime { get; set; }
    ConcurrentDictionary<Guid, TextMsg> TextMsgList { get; set; }
    ConcurrentQueue<TextMsg> TextMsgProcessingQueue { get; set; }

    event EventHandler<bool> IsRunBackgroundEmailServiceChanged;

    event EventHandler<bool> IsRunBackgroundHousekeeperServiceChanged;

    event EventHandler<bool> IsRunBackgroundTextServiceChanged;

    event EventHandler StateChanged;

    void StateHasChanged();
}
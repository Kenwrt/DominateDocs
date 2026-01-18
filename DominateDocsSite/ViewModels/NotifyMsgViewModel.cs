using CommunityToolkit.Mvvm.ComponentModel;
using DominateDocsNotify.Models;

namespace DominateDocsSite.ViewModels;

internal partial class NotifyMsgViewModel : ObservableObject
{
    [ObservableProperty]
    private long id;

    [ObservableProperty]
    private AppMsgEnums.MsgTypes msgType;

    [ObservableProperty]
    private AppMsgEnums.MsgLogLevels msgLoglevel;

    [ObservableProperty]
    private string deviceId;

    [ObservableProperty]
    private string emailTemplateName;

    [ObservableProperty]
    private string from;

    [ObservableProperty]
    private string messageBody;
}
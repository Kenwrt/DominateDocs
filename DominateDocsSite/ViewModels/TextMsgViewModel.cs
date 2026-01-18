using CommunityToolkit.Mvvm.ComponentModel;

namespace DominateDocsSite.ViewModels;

public partial class TextMsgViewModel : ObservableObject
{
    [ObservableProperty]
    private long id;

    [ObservableProperty]
    private string? textTemplateName;

    [ObservableProperty]
    private string? from;

    [ObservableProperty]
    private string? to;

    [ObservableProperty]
    private string? messageBody;
}
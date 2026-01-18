namespace DominateDocsNotify.Models;

public class NotificationConfig
{
    public bool SendLogNotifications { get; set; } = false;
    public bool SendReportNotifications { get; set; } = false;

    public bool SendSupportNotifications { get; set; } = false;

    public int AppRegId { get; set; } = -1;

    public string URL { get; set; } = string.Empty;
}
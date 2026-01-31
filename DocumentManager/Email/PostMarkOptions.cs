namespace DocumentManager.Email;

public sealed class PostmarkOptions
{
    public string ApiKey { get; set; } = "";
    public string FromEmail { get; set; } = "";
    public string? FromName { get; set; }
    public string? MessageStream { get; set; } = "outbound";
}

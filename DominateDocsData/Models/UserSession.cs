public class UserSession
{
    public Guid UserId { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string UserRole { get; set; } = string.Empty;
    public string UserPolicy { get; set; } = string.Empty;
    public string SessionId { get; set; } = string.Empty;
    public Guid DocLibId { get; set; } = Guid.Empty;
    public DateTimeOffset ExpUtc { get; set; }
}
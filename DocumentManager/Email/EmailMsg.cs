namespace DocumentManager.Email;

public sealed class EmailMsg
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string Subject { get; set; } = "";
    public int PostMarkTemplateId { get; set; }

    public string? Phone { get; set; }
    public string? Name { get; set; }

    public string? From { get; set; } = "no-reply@DominateDocs.law";
    public string? To { get; set; }
    public string? ReplyTo { get; set; }

    public string? MessageBody { get; set; }

    // ✅ NEW: Attachments
    public List<EmailAttachment> Attachments { get; set; } = new();
}


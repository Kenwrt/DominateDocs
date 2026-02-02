using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DocumentManager.Email;

public sealed class PostmarkEmailSender : IEmailSender
{
    private readonly HttpClient http;
    private readonly ILogger<PostmarkEmailSender> logger;
    private readonly DocumentManagerConfigOptions options;

    public PostmarkEmailSender(
        HttpClient http,
        IOptions<DocumentManagerConfigOptions> options,
        ILogger<PostmarkEmailSender> logger)
    {
        this.http = http;
        this.logger = logger;
        this.options = options.Value;

        if (string.IsNullOrWhiteSpace(this.options.PostMarkApiKey))
            throw new InvalidOperationException("Postmark ApiKey missing (DocumentManagerConfigOptions.PostMarkApiKey).");

        if (string.IsNullOrWhiteSpace(this.options.FromEmail))
            throw new InvalidOperationException("FromEmail missing (DocumentManagerConfigOptions.FromEmail).");

        this.http.BaseAddress = new Uri("https://api.postmarkapp.com/");
        this.http.DefaultRequestHeaders.Accept.Clear();
        this.http.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        if (this.http.DefaultRequestHeaders.Contains("X-Postmark-Server-Token"))
            this.http.DefaultRequestHeaders.Remove("X-Postmark-Server-Token");

        this.http.DefaultRequestHeaders.Add("X-Postmark-Server-Token", this.options.PostMarkApiKey);
    }

    public async Task SendAsync(EmailMsg msg, CancellationToken ct)
    {
        if (msg is null) throw new ArgumentNullException(nameof(msg));
        if (string.IsNullOrWhiteSpace(msg.To)) throw new ArgumentException("EmailMsg.To is required.");
        if (string.IsNullOrWhiteSpace(msg.Subject)) msg.Subject = "(no subject)";
        if (string.IsNullOrWhiteSpace(msg.MessageBody)) msg.MessageBody = "";

        var from = string.IsNullOrWhiteSpace(options.FromName)
            ? options.FromEmail
            : $"{options.FromName} <{options.FromEmail}>";

        var attachments = BuildPostmarkAttachments(msg);

        var payload = new PostmarkSendEmailRequest
        {
            From = from,
            To = msg.To!,
            Subject = msg.Subject,
            TextBody = msg.MessageBody,
            ReplyTo = msg.ReplyTo,
            MessageStream = string.IsNullOrWhiteSpace(options.MessageStream) ? "outbound" : options.MessageStream,
            Attachments = attachments.Count > 0 ? attachments : null
        };

        logger.LogInformation("➡️ Postmark send: To={To} Subject={Subject} Attachments={Count}",
            payload.To, payload.Subject, payload.Attachments?.Count ?? 0);

        using var resp = await http.PostAsJsonAsync("email", payload, ct);
        var body = await resp.Content.ReadAsStringAsync(ct);

        if (!resp.IsSuccessStatusCode)
        {
            logger.LogError("❌ Postmark failed: Status={Status} Body={Body}", (int)resp.StatusCode, body);
            throw new InvalidOperationException($"Postmark send failed ({(int)resp.StatusCode}). Body: {body}");
        }

        logger.LogInformation("✅ Postmark OK: {Body}", body);
    }

    private List<PostmarkAttachment> BuildPostmarkAttachments(EmailMsg msg)
    {
        var list = new List<PostmarkAttachment>();

        if (msg.Attachments is null || msg.Attachments.Count == 0)
            return list;

        foreach (var a in msg.Attachments)
        {
            if (a is null) continue;

            byte[] bytes;
            try
            {
                bytes = a.ToBytes();
            }
            catch
            {
                // Don’t throw the whole email away for one busted attachment.
                logger.LogWarning("Skipping attachment '{Name}' because it has no usable data.", a.FileName);
                continue;
            }

            if (bytes is null || bytes.Length == 0)
                continue;

            var ct = string.IsNullOrWhiteSpace(a.ContentType) ? "application/octet-stream" : a.ContentType;

            var pa = new PostmarkAttachment
            {
                Name = string.IsNullOrWhiteSpace(a.FileName) ? "attachment.bin" : a.FileName,
                ContentType = ct,
                Content = Convert.ToBase64String(bytes),
                ContentId = a.IsInline ? (a.ContentId ?? a.FileName) : null
            };

            list.Add(pa);
        }

        return list;
    }

    private sealed class PostmarkSendEmailRequest
    {
        public string From { get; set; } = "";
        public string To { get; set; } = "";
        public string? ReplyTo { get; set; }
        public string Subject { get; set; } = "";
        public string TextBody { get; set; } = "";
        public string MessageStream { get; set; } = "outbound";
        public List<PostmarkAttachment>? Attachments { get; set; }
    }

    private sealed class PostmarkAttachment
    {
        public string Name { get; set; } = "";
        public string Content { get; set; } = "";      // base64
        public string ContentType { get; set; } = "";
        public string? ContentId { get; set; }         // Postmark supports ContentID for inline
    }
}

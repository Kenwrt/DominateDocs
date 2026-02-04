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
            throw new InvalidOperationException("Postmark ApiKey is missing. Set Postmark:ApiKey in appsettings.");

        if (string.IsNullOrWhiteSpace(this.options.FromEmail))
            throw new InvalidOperationException("Postmark FromEmail is missing. Set Postmark:FromEmail in appsettings.");

        // Configure HttpClient once
        this.http.BaseAddress = new Uri("https://api.postmarkapp.com/");
        this.http.DefaultRequestHeaders.Accept.Clear();
        this.http.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        // Postmark auth header (server token)
        if (this.http.DefaultRequestHeaders.Contains("X-Postmark-Server-Token"))
            this.http.DefaultRequestHeaders.Remove("X-Postmark-Server-Token");

        this.http.DefaultRequestHeaders.Add("X-Postmark-Server-Token", this.options.PostMarkApiKey);
    }

    public async Task SendAsync(EmailMsg msg, CancellationToken ct)
    {
        if (msg is null) throw new ArgumentNullException(nameof(msg));
        if (string.IsNullOrWhiteSpace(msg.To)) throw new ArgumentException("EmailMsg.To is required.", nameof(msg));
        if (string.IsNullOrWhiteSpace(msg.Subject)) throw new ArgumentException("EmailMsg.Subject is required.", nameof(msg));
        if (string.IsNullOrWhiteSpace(msg.MessageBody)) throw new ArgumentException("EmailMsg.MessageBody is required.", nameof(msg));

        var from = string.IsNullOrWhiteSpace(options.FromName)
            ? options.FromEmail
            : $"{options.FromName} <{options.FromEmail}>";

        var payload = new PostmarkSendEmailRequest
        {
            From = from,
            To = msg.To,
            Subject = msg.Subject,
            TextBody = msg.MessageBody,
            // If you later add HtmlBody support, put it here too:
            // HtmlBody = msg.HtmlBody,
            MessageStream = string.IsNullOrWhiteSpace(options.MessageStream) ? "outbound" : options.MessageStream
        };

        logger.LogInformation("➡️ Postmark sending: To={To} Subject={Subject} Stream={Stream}", payload.To, payload.Subject, payload.MessageStream);

        using var resp = await http.PostAsJsonAsync("email", payload, ct);
        var body = await resp.Content.ReadAsStringAsync(ct);

        if (!resp.IsSuccessStatusCode)
        {
            logger.LogError(
                "❌ Postmark send failed: Status={StatusCode} Body={Body}",
                (int)resp.StatusCode,
                body);

            throw new InvalidOperationException($"Postmark send failed ({(int)resp.StatusCode}). Body: {body}");
        }

        logger.LogInformation("✅ Postmark send OK: {Body}", body);
    }

    // Postmark request DTO
    private sealed class PostmarkSendEmailRequest
    {
        public string From { get; set; } = "";
        public string To { get; set; } = "";
        public string Subject { get; set; } = "";
        public string TextBody { get; set; } = "";
        public string? HtmlBody { get; set; }
        public string MessageStream { get; set; } = "outbound";
    }
}

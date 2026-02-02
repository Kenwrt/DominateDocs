
using Microsoft.Extensions.Logging;

namespace DocumentManager.Email;

public sealed class NoopEmailSender : IEmailSender
{
    private readonly ILogger<NoopEmailSender> _logger;

    public NoopEmailSender(ILogger<NoopEmailSender> logger)
    {
        _logger = logger;
    }

    public Task SendAsync(EmailMsg msg, CancellationToken ct = default)
    {
        _logger.LogInformation(
            "NOOP email sender: To={To} Subject={Subject}",
            msg.To,
            msg.Subject);

        return Task.CompletedTask;
    }
}

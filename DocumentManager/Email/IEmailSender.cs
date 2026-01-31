using DominateDocsNotify.Models;

namespace DocumentManager.Email;

public interface IEmailSender
{
    Task SendAsync(EmailMsg msg, CancellationToken ct);
}

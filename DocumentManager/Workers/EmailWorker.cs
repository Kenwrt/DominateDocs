using DocumentManager.Email;
using DocumentManager.Infrastructure;
using DocumentManager.Jobs;
using DominateDocsNotify.Models;
using Microsoft.Extensions.Logging;

namespace DocumentManager.Workers;

public sealed class EmailWorker : WorkerPoolBackgroundService<EmailJob>
{
    private readonly ILogger<EmailWorker> _logger;
    private readonly IEmailSender _sender;

    public EmailWorker(
        IJobQueue<EmailJob> queue,
        ILogger<EmailWorker> logger,
        IEmailSender sender)
        : base(queue, logger, workers: 2)
    {
        _logger = logger;
        _sender = sender;
    }

    protected override async Task HandleAsync(EmailJob job, CancellationToken ct)
    {
        var msg = new EmailMsg
        {
            To = job.ToEmail,
            Subject = job.Subject,
            MessageBody = $"Your documents for loan {job.LoanId} are ready."
        };

        _logger.LogInformation(
            "EmailWorker sending email for LoanId={LoanId} To={To}",
            job.LoanId,
            job.ToEmail);

        await _sender.SendAsync(msg, ct);
    }
}

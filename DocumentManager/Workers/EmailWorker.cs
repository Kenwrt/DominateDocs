using DocumentManager.Email;
using DocumentManager.Infrastructure;
using DocumentManager.Jobs;
using DominateDocsNotify.Models;
using Microsoft.Extensions.Logging;

namespace DocumentManager.Workers;

public sealed class EmailWorker : WorkerPoolBackgroundService<EmailJob>
{
    private readonly ILogger<EmailWorker> logger;
    private readonly IEmailSender sender;

    public EmailWorker(
        IJobQueue<EmailJob> queue,
        ILogger<EmailWorker> logger,
        IEmailSender sender)
        : base(queue, logger, workers: 2)
    {
        this.logger = logger;
        this.sender = sender;
    }

    protected override async Task HandleAsync(EmailJob job, CancellationToken ct)
    {
        // You can expand this later to include template IDs, HTML bodies, attachments, etc.
        var msg = new EmailMsg
        {
            To = job.ToEmail,
            Subject = job.Subject,
            MessageBody = $"""
                Your documents for loan {job.LoanId} are ready.

                If you didn't request this, someone is doing something weird.
                """
        };

        logger.LogInformation(
            "📧 EmailWorker sending email for LoanId={LoanId} To={To} Subject={Subject}",
            job.LoanId,
            job.ToEmail,
            job.Subject);

        await sender.SendAsync(msg, ct);
    }

    public override Task StartAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("✅ EmailWorker STARTED (Workers={Workers})", 2);
        return base.StartAsync(cancellationToken);
    }
}

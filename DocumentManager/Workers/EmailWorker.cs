using System.IO.Compression;
using DocumentManager.Email;
using DocumentManager.Infrastructure;
using DocumentManager.Jobs;
using DocumentManager.State;
using DominateDocsData.Enums;
using Microsoft.Extensions.Logging;

namespace DocumentManager.Workers;

public sealed class EmailWorker : WorkerPoolBackgroundService<EmailJob>
{
    private readonly ILogger<EmailWorker> logger;
    private readonly IEmailSender sender;
    private readonly IDocumentManagerState docState;

    private const int MaxDocs = 100;
    private const int MaxTotalBytes = 25 * 1024 * 1024; // 25 MB safety cap (zip or attachments)

    public EmailWorker(
        IJobQueue<EmailJob> queue,
        ILogger<EmailWorker> logger,
        IEmailSender sender,
        IDocumentManagerState docState)
        : base(queue, logger, workers: 2)
    {
        this.logger = logger;
        this.sender = sender;
        this.docState = docState;
    }

    protected override async Task HandleAsync(EmailJob job, CancellationToken ct)
    {
        if (job.LoanId == Guid.Empty)
        {
            logger.LogWarning("EmailWorker: LoanId was empty. Skipping.");
            return;
        }

        var to = (job.ToEmail ?? "").Trim();
        if (string.IsNullOrWhiteSpace(to))
        {
            logger.LogWarning("EmailWorker: ToEmail empty for LoanId={LoanId}. Skipping.", job.LoanId);
            return;
        }

        // ✅ IMPORTANT:
        // Always wait for a merge quiet period even for IndividualDocument mode.
        // This is what makes "1 email with N attachments" reliable.
        await WaitForMergeQuietPeriodAsync(job.LoanId, job.ZipMaxWaitSeconds, ct).ConfigureAwait(false);

        var docs = BuildDocumentAttachments(job.LoanId);

        var msg = new EmailMsg
        {
            To = to,
            Subject = string.IsNullOrWhiteSpace(job.Subject) ? "Documents Ready" : job.Subject,
            MessageBody = BuildBody(job.LoanId, docs.Count, job.AttachmentOutput),
        };

        if (job.AttachmentOutput == EmailEnums.AttachmentOutput.ZipFile)
        {
            var zipAttachment = BuildZipAttachment(job.LoanId, docs);

            if (zipAttachment != null)
            {
                msg.Attachments.Add(zipAttachment);
            }
            else
            {
                // Fall back to individual documents if zip fails
                foreach (var a in docs)
                    msg.Attachments.Add(a);

                msg.MessageBody += "\n\n(Zip was requested but could not be produced. Sent individual documents instead.)";
            }
        }
        else
        {
            foreach (var a in docs)
                msg.Attachments.Add(a);
        }

        var totalBytes = msg.Attachments.Sum(a =>
        {
            try { return a.ToBytes().Length; }
            catch { return 0; }
        });

        logger.LogInformation(
            "📧 EmailWorker send: LoanId={LoanId} To={To} Mode={Mode} Attachments={Count} TotalBytes={Bytes}",
            job.LoanId, to, job.AttachmentOutput, msg.Attachments.Count, totalBytes);

        if (msg.Attachments.Count == 0)
        {
            msg.MessageBody += "\n\n(No completed merge outputs were found in memory. Either merges are not complete, or the in-memory merge list was cleared.)";
        }

        try
        {
            logger.LogInformation("EmailWorker: Sending email LoanId={LoanId} To={To} Mode={Mode} Attachments={Count}",
                job.LoanId, to, job.AttachmentOutput, msg.Attachments.Count);

            await sender.SendAsync(msg, ct).ConfigureAwait(false);

            logger.LogInformation("EmailWorker: Sent email LoanId={LoanId} To={To} Mode={Mode} Attachments={Count}",
                job.LoanId, to, job.AttachmentOutput, msg.Attachments.Count);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "EmailWorker: Send failed LoanId={LoanId} To={To} Mode={Mode}", job.LoanId, to, job.AttachmentOutput);
        }
    }

    private static string BuildBody(Guid loanId, int docCount, EmailEnums.AttachmentOutput mode)
    {
        if (mode == EmailEnums.AttachmentOutput.ZipFile)
            return $"Attached is a ZIP containing {docCount} generated document(s) for loan {loanId:N}.";

        return $"Attached are {docCount} generated document(s) for loan {loanId:N}.";
    }

    private async Task WaitForMergeQuietPeriodAsync(Guid loanId, int maxWaitSeconds, CancellationToken ct)
    {
        if (maxWaitSeconds <= 0) return;

        var start = DateTime.UtcNow;
        var lastCount = CountCompletedMerges(loanId);
        var stableSince = DateTime.UtcNow;

        while (!ct.IsCancellationRequested && (DateTime.UtcNow - start).TotalSeconds < maxWaitSeconds)
        {
            await Task.Delay(500, ct).ConfigureAwait(false);

            var nowCount = CountCompletedMerges(loanId);
            if (nowCount != lastCount)
            {
                lastCount = nowCount;
                stableSince = DateTime.UtcNow;
                continue;
            }

            // Quiet for 1.5 seconds => treat as settled
            if ((DateTime.UtcNow - stableSince).TotalMilliseconds >= 1500)
                return;
        }
    }

    private int CountCompletedMerges(Guid loanId)
    {
        try
        {
            return docState.DocumentList.Values.Count(m =>
                m != null &&
                m.LoanAgreement != null &&
                m.LoanAgreement.Id == loanId &&
                m.Status == DocumentMergeState.Status.Complete &&
                m.MergedDocumentBytes != null &&
                m.MergedDocumentBytes.Length > 0);
        }
        catch
        {
            return 0;
        }
    }

    private List<EmailAttachment> BuildDocumentAttachments(Guid loanId)
    {
        var list = new List<EmailAttachment>();

        try
        {
            var merges = docState.DocumentList.Values
                .Where(m =>
                    m != null &&
                    m.LoanAgreement != null &&
                    m.LoanAgreement.Id == loanId &&
                    m.Status == DocumentMergeState.Status.Complete &&
                    m.MergedDocumentBytes != null &&
                    m.MergedDocumentBytes.Length > 0)
                .ToList();

            foreach (var m in merges)
            {
                if (list.Count >= MaxDocs) break;

                var name = (m.Document?.Name ?? "Document").Trim();
                if (string.IsNullOrWhiteSpace(name)) name = "Document";

                // Determine file extension
                var ext = (m.Document?.OutputType ?? DocumentTypes.OutputTypes.PDF) == DocumentTypes.OutputTypes.DOCX ? "docx" : "pdf";
                var fileName = $"{SanitizeFileName(name)}.{ext}";

                var contentType = ext == "docx"
                    ? "application/vnd.openxmlformats-officedocument.wordprocessingml.document"
                    : "application/pdf";

                list.Add(new EmailAttachment
                {
                    FileName = fileName,
                    ContentType = contentType,
                    Data = m.MergedDocumentBytes!
                });
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "EmailWorker: BuildDocumentAttachments failed LoanId={LoanId}", loanId);
        }

        // Cap total bytes
        var total = list.Sum(a => a.Data?.Length ?? 0);
        if (total > MaxTotalBytes)
        {
            logger.LogWarning("EmailWorker: Total attachment bytes exceeded cap. LoanId={LoanId} Bytes={Bytes}", loanId, total);
            // keep only as many as we can under the cap
            var trimmed = new List<EmailAttachment>();
            var running = 0;
            foreach (var a in list)
            {
                var sz = a.Data?.Length ?? 0;
                if (running + sz > MaxTotalBytes) break;
                trimmed.Add(a);
                running += sz;
            }
            list = trimmed;
        }

        return list;
    }

    private EmailAttachment? BuildZipAttachment(Guid loanId, List<EmailAttachment> docs)
    {
        if (docs.Count == 0) return null;

        try
        {
            using var ms = new MemoryStream();
            using (var zip = new ZipArchive(ms, ZipArchiveMode.Create, leaveOpen: true))
            {
                foreach (var a in docs)
                {
                    var entry = zip.CreateEntry(a.FileName, CompressionLevel.Optimal);
                    using var entryStream = entry.Open();
                    entryStream.Write(a.Data, 0, a.Data.Length);
                }
            }

            var zipBytes = ms.ToArray();
            if (zipBytes.Length == 0) return null;

            if (zipBytes.Length > MaxTotalBytes)
            {
                logger.LogWarning("EmailWorker: Zip bytes exceeded cap. LoanId={LoanId} Bytes={Bytes}", loanId, zipBytes.Length);
                return null;
            }

            return new EmailAttachment
            {
                FileName = $"Loan_{loanId:N}_Documents.zip",
                ContentType = "application/zip",
                Data = zipBytes
            };
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "EmailWorker: BuildZipAttachment failed LoanId={LoanId}", loanId);
            return null;
        }
    }

    private static string SanitizeFileName(string name)
    {
        foreach (var c in Path.GetInvalidFileNameChars())
            name = name.Replace(c, '_');

        return name.Trim();
    }
}

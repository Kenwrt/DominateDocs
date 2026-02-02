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

        // If ZIP requested, give merges a moment to settle.
        if (job.AttachmentOutput == EmailEnums.AttachmentOutput.ZipFile)
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
        return mode == EmailEnums.AttachmentOutput.ZipFile
            ? $"Attached is a ZIP containing {docCount} generated document(s) for loan {loanId}."
            : $"Attached are {docCount} generated document(s) for loan {loanId}.";
    }

    private async Task WaitForMergeQuietPeriodAsync(Guid loanId, int maxWaitSeconds, CancellationToken ct)
    {
        if (maxWaitSeconds <= 0) return;

        var end = DateTime.UtcNow.AddSeconds(maxWaitSeconds);

        var lastCompletedCount = -1;
        var stableTicks = 0;

        while (DateTime.UtcNow < end && !ct.IsCancellationRequested)
        {
            var completed = CountCompletedMerges(loanId);

            if (completed == lastCompletedCount)
                stableTicks++;
            else
                stableTicks = 0;

            lastCompletedCount = completed;

            // “Quiet” for ~1 second (5 * 200ms)
            if (stableTicks >= 5)
                return;

            await Task.Delay(200, ct).ConfigureAwait(false);
        }
    }

    private int CountCompletedMerges(Guid loanId)
    {
        return docState.DocumentList.Values.Count(m =>
            m is not null
            && m.LoanAgreement is not null
            && m.LoanAgreement.Id == loanId
            && m.Status == DocumentMergeState.Status.Complete
            && m.MergedDocumentBytes is not null
            && m.MergedDocumentBytes.Length > 0);
    }

    private List<EmailAttachment> BuildDocumentAttachments(Guid loanId)
    {
        var results = new List<EmailAttachment>();

        var merges = docState.DocumentList.Values
            .Where(m => m is not null
                        && m.LoanAgreement is not null
                        && m.LoanAgreement.Id == loanId
                        && m.Status == DocumentMergeState.Status.Complete
                        && m.MergedDocumentBytes is not null
                        && m.MergedDocumentBytes.Length > 0)
            .OrderBy(m => m.Document?.Name)
            .ToList();

        var total = 0;

        foreach (var m in merges)
        {
            if (results.Count >= MaxDocs)
                break;

            var bytes = m.MergedDocumentBytes!;
            if (bytes.Length == 0) continue;

            // OutputType comes from the Document
            var outType = m.Document?.OutputType ?? DocumentTypes.OutputTypes.PDF;

            var ext = outType == DocumentTypes.OutputTypes.DOCX ? "docx" : "pdf";
            var contentType = outType == DocumentTypes.OutputTypes.DOCX
                ? "application/vnd.openxmlformats-officedocument.wordprocessingml.document"
                : "application/pdf";

            var baseName = string.IsNullOrWhiteSpace(m.Document?.Name) ? "document" : m.Document!.Name!;
            var fileName = NormalizeFileName(baseName, ext);

            if (total + bytes.Length > MaxTotalBytes)
                break;

            total += bytes.Length;

            results.Add(new EmailAttachment
            {
                FileName = fileName,
                ContentType = contentType,
                SourceType = EmailEnums.AttachmentSourceType.ByteArray,
                OutputType = EmailEnums.AttachmentOutput.IndividualDocument,
                Data = bytes
            });
        }

        return results;
    }

    private EmailAttachment? BuildZipAttachment(Guid loanId, List<EmailAttachment> docs)
    {
        if (docs.Count == 0) return null;

        try
        {
            using var ms = new MemoryStream();

            using (var zip = new ZipArchive(ms, ZipArchiveMode.Create, leaveOpen: true))
            {
                foreach (var doc in docs)
                {
                    var bytes = doc.ToBytes();
                    if (bytes.Length == 0) continue;

                    var safeName = MakeZipSafeFileName(doc.FileName);
                    var entry = zip.CreateEntry(safeName, CompressionLevel.Optimal);

                    using var entryStream = entry.Open();
                    entryStream.Write(bytes, 0, bytes.Length);
                }
            }

            var zipBytes = ms.ToArray();

            if (zipBytes.Length == 0 || zipBytes.Length > MaxTotalBytes)
                return null;

            return new EmailAttachment
            {
                FileName = $"Loan_{loanId:N}_Documents.zip",
                ContentType = "application/zip",
                SourceType = EmailEnums.AttachmentSourceType.ByteArray,
                OutputType = EmailEnums.AttachmentOutput.ZipFile,
                Data = zipBytes
            };
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "❌ EmailWorker: failed to build ZIP for LoanId={LoanId}", loanId);
            return null;
        }
    }

    private static string NormalizeFileName(string baseName, string ext)
    {
        var name = (baseName ?? "document").Trim();
        var dot = name.LastIndexOf('.');
        if (dot > 0) name = name.Substring(0, dot);

        foreach (var c in Path.GetInvalidFileNameChars())
            name = name.Replace(c, '_');

        return $"{name}.{ext}";
    }

    private static string MakeZipSafeFileName(string fileName)
    {
        var name = (fileName ?? "document.bin").Replace("\\", "_").Replace("/", "_");
        foreach (var c in Path.GetInvalidFileNameChars())
            name = name.Replace(c, '_');

        return string.IsNullOrWhiteSpace(name) ? "document.bin" : name;
    }

    public override Task StartAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("✅ EmailWorker STARTED (Workers={Workers})", 2);
        return base.StartAsync(cancellationToken);
    }
}

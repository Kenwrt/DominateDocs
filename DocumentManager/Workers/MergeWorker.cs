using DocumentManager.Email;
using DocumentManager.Infrastructure;
using DocumentManager.Jobs;
using DocumentManager.Services;
using DocumentManager.State;
using DominateDocsData.Enums;
using DominateDocsData.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DocumentManager.Workers;

/// <summary>
/// Optional extension point: register one or more hooks that run after a merge completes successfully.
/// This is the clean place to enqueue email, write audit logs, push notifications, etc.
/// </summary>
public interface IMergeCompleteHook
{
    Task OnMergeCompleteAsync(DocumentMerge merge, CancellationToken ct);
}

public sealed class MergeWorker : WorkerPoolBackgroundService<MergeJob>
{
    private readonly ILogger<MergeWorker> logger;
    private readonly IOptions<DocumentManagerConfigOptions> options;
    private readonly IDocumentManagerState docState;
    private readonly IRazorLiteService razorLiteService;
    private readonly IWordServices wordServices;
    private readonly IEnumerable<IMergeCompleteHook> completionHooks;

    // NEW: direct email sender (Postmark implementation)
    private readonly IEmailSender emailSender;

    public MergeWorker(
        IJobQueue<MergeJob> queue,
        ILogger<MergeWorker> logger,
        IOptions<DocumentManagerConfigOptions> options,
        IDocumentManagerState docState,
        IRazorLiteService razorLiteService,
        IWordServices wordServices,
        IEnumerable<IMergeCompleteHook> completionHooks,
        IEmailSender emailSender)
        : base(queue, logger, options.Value.MaxDocumentMergeThreads)
    {
        this.logger = logger;
        this.options = options;
        this.docState = docState;
        this.razorLiteService = razorLiteService;
        this.wordServices = wordServices;
        this.completionHooks = completionHooks;
        this.emailSender = emailSender;
    }

    protected override async Task HandleAsync(MergeJob job, CancellationToken ct)
    {
        if (!options.Value.IsActive || !docState.IsRunBackgroundDocumentMergeService)
            return;

        var documentMerge = job.Merge;

        try
        {
            logger.LogInformation("📥 MergeWorker got MergeJob for MergeId={MergeId}, Doc={Doc}",
                documentMerge?.Id, documentMerge?.Document?.Name);

            docState.DocumentList.TryAdd(documentMerge.Id, documentMerge);

            using var ms = new MemoryStream(
                capacity: documentMerge.Document.TemplateDocumentBytes.Length + 4096);

            ms.Write(documentMerge.Document.TemplateDocumentBytes, 0, documentMerge.Document.TemplateDocumentBytes.Length);
            ms.Position = 0;

            var msResult = await razorLiteService.ProcessAsync(ms, documentMerge.LoanAgreement).ConfigureAwait(false);

            if (msResult is not null)
            {
                msResult.Position = 0;

                // We do NOT mutate Document.Name here.
                // Generate a filename for emailing only.
                string attachmentFileName;
                string attachmentContentType;

                if (documentMerge.Document.OutputType == DocumentTypes.OutputTypes.DOCX)
                {
                    documentMerge.MergedDocumentBytes = msResult.ToArray();
                    attachmentFileName = BuildFileName(documentMerge.Document?.Name, "docx");
                    attachmentContentType = "application/vnd.openxmlformats-officedocument.wordprocessingml.document";
                }
                else
                {
                    var pdfStream = await wordServices.ConvertWordToPdfAsync(msResult).ConfigureAwait(false);
                    pdfStream.Position = 0;

                    documentMerge.MergedDocumentBytes = pdfStream.ToArray();
                    attachmentFileName = BuildFileName(documentMerge.Document?.Name, "pdf");
                    attachmentContentType = "application/pdf";
                }

                documentMerge.Status = DocumentMergeState.Status.Complete;
                documentMerge.MergeCompleteAt = DateTime.UtcNow;

                // 1) Optional hook(s)
                foreach (var hook in completionHooks)
                {
                    try
                    {
                        await hook.OnMergeCompleteAsync(documentMerge, ct).ConfigureAwait(false);
                    }
                    catch (Exception hookEx)
                    {
                        logger.LogError(hookEx, "Merge completion hook failed for MergeId={MergeId}", documentMerge.Id);
                    }
                }

                // 2) NEW: Email the merged bytes as an attachment if we have an address
                await TryEmailMergedDocumentAsync(
                    documentMerge,
                    attachmentFileName,
                    attachmentContentType,
                    ct).ConfigureAwait(false);
            }
            else
            {
                documentMerge.MergedDocumentBytes = null;
                documentMerge.Status = DocumentMergeState.Status.Error;
            }

            docState.StateHasChanged();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error processing document {MergeId}", documentMerge.Id);
            documentMerge.Status = DocumentMergeState.Status.Error;
            throw;
        }
        finally
        {
            // keep it in docState.DocumentList if you want UI to show it; otherwise remove it here
            // docState.DocumentList.TryRemove(documentMerge.Id, out _);
        }
    }

    private async Task TryEmailMergedDocumentAsync(
        DocumentMerge merge,
        string attachmentFileName,
        string attachmentContentType,
        CancellationToken ct)
    {
        try
        {
            // Primary: LoanAgreement.EmailTo (what your Admin Bench already uses)
            var to = merge.LoanAgreement?.EmailTo;

            if (string.IsNullOrWhiteSpace(to))
            {
                // No email, no problem. (Actually yes, it's a problem, but not ours.)
                logger.LogInformation("📭 Merge complete but no EmailTo on LoanAgreement. MergeId={MergeId}", merge.Id);
                return;
            }

            var bytes = merge.MergedDocumentBytes;
            if (bytes is null || bytes.Length == 0)
            {
                logger.LogWarning("📭 Merge complete but no bytes to email. MergeId={MergeId}", merge.Id);
                return;
            }

            var loanId = merge.LoanAgreement?.Id;
            var docName = merge.Document?.Name ?? "Document";

            var subject = $"Documents Ready: {docName}";
            if (loanId.HasValue && loanId.Value != Guid.Empty)
                subject += $" (Loan {loanId.Value})";

            var msg = new EmailMsg
            {
                To = to.Trim(),
                Subject = subject,
                MessageBody = $"Attached is your generated document: {attachmentFileName}",
                Attachments =
                {
                    new EmailAttachment
                    {
                        FileName = attachmentFileName,
                        ContentType = attachmentContentType,
                        Data = bytes
                    }
                }
            };

            logger.LogInformation("📧 Sending merged document email: MergeId={MergeId} To={To} File={File} Bytes={Bytes}",
                merge.Id, msg.To, attachmentFileName, bytes.Length);

            await emailSender.SendAsync(msg, ct).ConfigureAwait(false);

            logger.LogInformation("✅ Email sent: MergeId={MergeId} To={To}", merge.Id, msg.To);
        }
        catch (Exception ex)
        {
            // Email failure should NOT poison merge completion
            logger.LogError(ex, "❌ Email failed after merge complete. MergeId={MergeId}", merge.Id);
        }
    }

    private static string BuildFileName(string? baseName, string ext)
    {
        var safe = string.IsNullOrWhiteSpace(baseName) ? "document" : baseName.Trim();

        // Strip any existing extension to avoid "file.pdf.pdf"
        var dot = safe.LastIndexOf('.');
        if (dot > 0 && dot < safe.Length - 1)
            safe = safe.Substring(0, dot);

        return $"{safe}.{ext}";
    }

    public override Task StartAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("✅ MergeWorker STARTED (MaxThreads={MaxThreads})", options.Value.MaxDocumentMergeThreads);
        return base.StartAsync(cancellationToken);
    }
}

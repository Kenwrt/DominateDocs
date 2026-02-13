using DocumentManager.Email;
using DocumentManager.Infrastructure;
using DocumentManager.Jobs;
using DocumentManager.Services;
using DocumentManager.State;
using DominateDocsData.Database;
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

    // Kept to avoid breaking DI in your project (even though we no longer email here)
    private readonly IEmailSender emailSender;

    // DB access for DocumentStore
    private readonly IMongoDatabaseRepo dbApp;

    public MergeWorker(
        IJobQueue<MergeJob> queue,
        ILogger<MergeWorker> logger,
        IOptions<DocumentManagerConfigOptions> options,
        IDocumentManagerState docState,
        IRazorLiteService razorLiteService,
        IWordServices wordServices,
        IEnumerable<IMergeCompleteHook> completionHooks,
        IEmailSender emailSender,
        IMongoDatabaseRepo dbApp)
        : base(queue, logger, options.Value.MaxDocumentMergeThreads)
    {
        this.logger = logger;
        this.options = options;
        this.docState = docState;
        this.razorLiteService = razorLiteService;
        this.wordServices = wordServices;
        this.completionHooks = completionHooks;
        this.emailSender = emailSender; // intentionally unused now
        this.dbApp = dbApp;
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

            var templateBytes = ResolveTemplateBytes(documentMerge.Document);

            if (templateBytes is null || templateBytes.Length == 0)
            {
                logger.LogError("❌ MergeWorker: No template bytes resolved. MergeId={MergeId} DocId={DocId} DocStoreId={DocStoreId}",
                    documentMerge.Id,
                    documentMerge.Document?.Id,
                    documentMerge.Document?.DocStoreId);

                documentMerge.MergedDocumentBytes = null;
                documentMerge.Status = DocumentMergeState.Status.Error;
                docState.StateHasChanged();
                return;
            }

            using var ms = new MemoryStream(capacity: templateBytes.Length + 4096);

            ms.Write(templateBytes, 0, templateBytes.Length);
            ms.Position = 0;

            var msResult = await razorLiteService.ProcessAsync(ms, documentMerge.LoanAgreement).ConfigureAwait(false);

            if (msResult is not null)
            {
                msResult.Position = 0;

                if (documentMerge.Document.OutputType == DocumentTypes.OutputTypes.DOCX)
                {
                    documentMerge.MergedDocumentBytes = msResult.ToArray();
                }
                else
                {
                    var pdfStream = await wordServices.ConvertWordToPdfAsync(msResult).ConfigureAwait(false);
                    pdfStream.Position = 0;
                    documentMerge.MergedDocumentBytes = pdfStream.ToArray();
                }

                documentMerge.Status = DocumentMergeState.Status.Complete;
                documentMerge.MergeCompleteAt = DateTime.UtcNow;

                // Optional hooks
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

                // ✅ NO EMAIL HERE.
                // Email is sent once per loan by EmailWorker, triggered by LoanWorker.
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
    }

    private byte[]? ResolveTemplateBytes(Document? doc)
    {
        if (doc is null) return null;

        // Preferred: DocumentStore bytes
        if (doc.DocStoreId != Guid.Empty)
        {
            try
            {
                var store = dbApp.GetRecordById<DocumentStore>(doc.DocStoreId);
                if (store?.DocumentBytes is not null && store.DocumentBytes.Length > 0)
                    return store.DocumentBytes;

                logger.LogWarning("MergeWorker: DocumentStore bytes missing/empty. DocId={DocId} DocStoreId={DocStoreId}",
                    doc.Id, doc.DocStoreId);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "MergeWorker: failed to load DocumentStore. DocId={DocId} DocStoreId={DocStoreId}",
                    doc.Id, doc.DocStoreId);
            }
        }

        // Back-compat fallback
        if (doc.TemplateDocumentBytes is not null && doc.TemplateDocumentBytes.Length > 0)
        {
            logger.LogWarning("MergeWorker: using fallback TemplateDocumentBytes (DocStoreId missing or invalid). DocId={DocId} Name={Name}",
                doc.Id, doc.Name);

            return doc.TemplateDocumentBytes;
        }

        return null;
    }
}

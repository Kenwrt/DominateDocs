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

    public MergeWorker(
        IJobQueue<MergeJob> queue,
        ILogger<MergeWorker> logger,
        IOptions<DocumentManagerConfigOptions> options,
        IDocumentManagerState docState,
        IRazorLiteService razorLiteService,
        IWordServices wordServices,
        IEnumerable<IMergeCompleteHook> completionHooks)
        : base(queue, logger, options.Value.MaxDocumentMergeThreads)
    {
        this.logger = logger;
        this.options = options;
        this.docState = docState;
        this.razorLiteService = razorLiteService;
        this.wordServices = wordServices;
        this.completionHooks = completionHooks;
    }

    protected override async Task HandleAsync(MergeJob job, CancellationToken ct)
    {
        if (!options.Value.IsActive || !docState.IsRunBackgroundDocumentMergeService)
            return;

        var documentMerge = job.Merge;

        try
        {
            docState.DocumentList.TryAdd(documentMerge.Id, documentMerge);

            using var ms = new MemoryStream(
                capacity: documentMerge.Document.TemplateDocumentBytes.Length + 4096);

            ms.Write(documentMerge.Document.TemplateDocumentBytes, 0, documentMerge.Document.TemplateDocumentBytes.Length);
            ms.Position = 0;

            var msResult = await razorLiteService.ProcessAsync(ms, documentMerge.LoanAgreement).ConfigureAwait(false);

            if (msResult is not null)
            {
                msResult.Position = 0;

                if (documentMerge.Document.OutputType == DocumentTypes.OutputTypes.DOCX)
                {
                    // FIX: previously you marked it "complete" but didn't store bytes.
                    documentMerge.MergedDocumentBytes = msResult.ToArray();

                    // Keep your existing behavior, but now it actually has content.
                    // If you truly mean DOCX, use ".docx"; if you need macros, ".docm".
                    documentMerge.Document.Name = $"{documentMerge.Document.Name}.docm";
                }
                else
                {
                    var pdfStream = await wordServices.ConvertWordToPdfAsync(msResult).ConfigureAwait(false);
                    pdfStream.Position = 0;

                    documentMerge.MergedDocumentBytes = pdfStream.ToArray();
                    documentMerge.Document.Name = $"{documentMerge.Document.Name}.pdf";
                }

                documentMerge.Status = DocumentMergeState.Status.Complete;
                documentMerge.MergeCompleteAt = DateTime.UtcNow;

                // Merge completion -> hook(s) (email enqueue belongs here)
                foreach (var hook in completionHooks)
                {
                    try
                    {
                        await hook.OnMergeCompleteAsync(documentMerge, ct).ConfigureAwait(false);
                    }
                    catch (Exception hookEx)
                    {
                        // Hook failures should not poison the merge result
                        logger.LogError(hookEx, "Merge completion hook failed for MergeId {MergeId}", documentMerge.Id);
                    }
                }
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
}

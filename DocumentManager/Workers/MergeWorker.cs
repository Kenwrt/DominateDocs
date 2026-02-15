using DocumentManager.Email;
using DocumentManager.Infrastructure;
using DocumentManager.Jobs;
using DocumentManager.Services;
using DocumentManager.State;
using DominateDocsData.Database;
using DominateDocsData.Enums;
using DominateDocsData.Models;
using GemBox.Document;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.IO.Compression;

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
                documentMerge.WordDocumentBytes = null;
                documentMerge.PdfDocumentBytes = null;
                documentMerge.Status = DocumentMergeState.Status.Error;
                docState.StateHasChanged();
                return;
            }

            var outputType = documentMerge.Document?.OutputType ?? DocumentTypes.OutputTypes.PDF;

            var wantsDocx = outputType == DocumentTypes.OutputTypes.DOCX || IsDualOutput(outputType);
            var wantsPdf = outputType == DocumentTypes.OutputTypes.PDF || IsDualOutput(outputType);

            // Requirement: if DOCX is selected (including dual), convert DOCM -> DOCX via GemBox before producing output.
            if (wantsDocx && IsDocmByContentType(templateBytes))
            {
                try
                {
                    using var docmStream = new MemoryStream(templateBytes, writable: false);
                    var docm = DocumentModel.Load(docmStream);

                    using var docxOut = new MemoryStream();
                    docm.Save(docxOut, SaveOptions.DocxDefault);

                    templateBytes = docxOut.ToArray();
                }
                catch (Exception ex)
                {
                    // Best-effort: continue with original bytes rather than failing.
                    logger.LogError(ex, "MergeWorker: DOCM->DOCX conversion failed. MergeId={MergeId} Doc={Doc}",
                        documentMerge.Id, documentMerge.Document?.Name);

                    logger.LogError("MergeWorker: DOCM->DOCX failure details (ToString): {Error}", ex.ToString());
                }
            }

            // Merge template with RazorLite
            using var msTemplate = new MemoryStream(capacity: templateBytes.Length + 4096);
            msTemplate.Write(templateBytes, 0, templateBytes.Length);
            msTemplate.Position = 0;

            var msResult = await razorLiteService.ProcessAsync(msTemplate, documentMerge.LoanAgreement).ConfigureAwait(false);

            if (msResult is null)
            {
                documentMerge.MergedDocumentBytes = null;
                documentMerge.WordDocumentBytes = null;
                documentMerge.PdfDocumentBytes = null;
                documentMerge.Status = DocumentMergeState.Status.Error;
                docState.StateHasChanged();
                return;
            }

            if (msResult.CanSeek)
                msResult.Position = 0;

            Exception? docxError = null;
            Exception? pdfError = null;

            // ======================
            // Produce DOCX
            // ======================
            if (wantsDocx)
            {
                try
                {
                    // Preserve merged DOCX bytes.
                    documentMerge.WordDocumentBytes = msResult.ToArray();
                }
                catch (Exception ex)
                {
                    docxError = ex;
                    documentMerge.WordDocumentBytes = null;

                    logger.LogError(ex, "MergeWorker: DOCX output failed. MergeId={MergeId} Doc={Doc}",
                        documentMerge.Id, documentMerge.Document?.Name);
                    logger.LogError("MergeWorker: DOCX failure details (ToString): {Error}", ex.ToString());
                }
            }

            // ======================
            // Produce PDF (your ConvertWordToPdfAsync requires MemoryStream)
            // ======================
            if (wantsPdf)
            {
                try
                {
                    MemoryStream pdfSource;

                    // Prefer converting from produced DOCX bytes (deterministic).
                    if (documentMerge.WordDocumentBytes is { Length: > 0 })
                    {
                        pdfSource = new MemoryStream(documentMerge.WordDocumentBytes, writable: false);
                    }
                    else
                    {
                        // Fallback: clone msResult into a MemoryStream (because your signature wants MemoryStream).
                        if (msResult.CanSeek)
                            msResult.Position = 0;

                        using var tmp = new MemoryStream();
                        msResult.CopyTo(tmp);
                        pdfSource = new MemoryStream(tmp.ToArray(), writable: false);
                    }

                    if (pdfSource.CanSeek)
                        pdfSource.Position = 0;

                    var pdfStream = await wordServices.ConvertWordToPdfAsync(pdfSource).ConfigureAwait(false);
                    if (pdfStream.CanSeek)
                        pdfStream.Position = 0;

                    documentMerge.PdfDocumentBytes = pdfStream.ToArray();
                }
                catch (Exception ex)
                {
                    pdfError = ex;
                    documentMerge.PdfDocumentBytes = null;

                    // Best-effort: do NOT fail the merge if DOCX succeeded
                    logger.LogError(ex,
                        "MergeWorker: PDF output failed. MergeId={MergeId} Doc={Doc}. Will still complete if any output exists.",
                        documentMerge.Id, documentMerge.Document?.Name);

                    logger.LogError("MergeWorker: PDF failure details (ToString): {Error}", ex.ToString());
                }
            }

            // Back-compat: pick a primary merged bytes so older consumers still work
            if (documentMerge.PdfDocumentBytes is { Length: > 0 })
                documentMerge.MergedDocumentBytes = documentMerge.PdfDocumentBytes;
            else if (documentMerge.WordDocumentBytes is { Length: > 0 })
                documentMerge.MergedDocumentBytes = documentMerge.WordDocumentBytes;
            else
                documentMerge.MergedDocumentBytes = null;

            // Status: Complete if ANY output exists; Error only if NONE exist
            if (HasAnyOutputBytes(documentMerge))
            {
                documentMerge.Status = DocumentMergeState.Status.Complete;
                documentMerge.MergeCompleteAt = DateTime.UtcNow;

                if (docxError != null || pdfError != null)
                {
                    logger.LogWarning(
                        "MergeWorker completed with warnings. MergeId={MergeId} Doc={Doc} OutputType={OutputType} DocxOk={DocxOk} PdfOk={PdfOk}",
                        documentMerge.Id,
                        documentMerge.Document?.Name,
                        outputType,
                        documentMerge.WordDocumentBytes is { Length: > 0 },
                        documentMerge.PdfDocumentBytes is { Length: > 0 });
                }

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
            }
            else
            {
                documentMerge.Status = DocumentMergeState.Status.Error;
            }

            // ✅ NO EMAIL HERE.
            // Email is sent once per loan by EmailWorker, triggered by LoanWorker.

            docState.StateHasChanged();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error processing document {MergeId}", documentMerge.Id);
            documentMerge.Status = DocumentMergeState.Status.Error;
            throw;
        }
    }

    private static bool HasAnyOutputBytes(DocumentMerge m)
    {
        if (m.MergedDocumentBytes is { Length: > 0 }) return true;
        if (m.WordDocumentBytes is { Length: > 0 }) return true;
        if (m.PdfDocumentBytes is { Length: > 0 }) return true;
        return false;
    }

    private static bool IsDualOutput(DocumentTypes.OutputTypes outputType)
    {
        // Your enum includes DOCXPDF; treat that as the dual selection.
        return outputType == DocumentTypes.OutputTypes.DOCXPDF;
    }

    private static bool IsDocmByContentType(byte[] templateBytes)
    {
        // DOCM is a ZIP package. Detect the macro-enabled main content type.
        try
        {
            using var ms = new MemoryStream(templateBytes, writable: false);
            using var zip = new ZipArchive(ms, ZipArchiveMode.Read, leaveOpen: false);

            var entry = zip.GetEntry("[Content_Types].xml");
            if (entry is null) return false;

            using var es = entry.Open();
            using var sr = new StreamReader(es);
            var xml = sr.ReadToEnd();

            return xml.Contains("application/vnd.ms-word.document.macroEnabled.main+xml", StringComparison.OrdinalIgnoreCase);
        }
        catch
        {
            return false;
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

using DocumentManager.Email;
using DocumentManager.Infrastructure;
using DocumentManager.Jobs;
using DocumentManager.Services;
using DocumentManager.State;
using DominateDocsData.Enums;
using DominateDocsData.Models;
using Microsoft.Extensions.Logging;
using System.Reflection;

namespace DominateDocsSite.ViewModels;

public sealed class AdminBenchViewModel
{
    public bool IsBusy { get; private set; }
    public string? Status { get; private set; }

    public List<Guid> DocLibIds { get; private set; } = new();
    public Guid SelectedDocLibId { get; set; }

    // UI says optional, so it is optional.
    public string EmailTo { get; set; } = "";

    public EmailEnums.AttachmentOutput EmailAttachmentOutput { get; set; } =
        EmailEnums.AttachmentOutput.IndividualDocument;

    public DocumentTypes.OutputTypes OutputType { get; set; } = DocumentTypes.OutputTypes.PDF;

    public List<Document> Documents { get; private set; } = new();
    public List<LoanType> LoanTypes { get; private set; } = new();
    public List<LoanAgreement> LoanAgreements { get; private set; } = new();

    public LoanAgreement? SelectedLoanAgreement { get; set; }
    public LoanType? SelectedLoanType { get; set; }

    // What the UI renders as “Persisted Deliveries”.
    // In bench mode we repurpose it as “Preview Deliveries” until you actually Run and persist.
    public IReadOnlyList<DocumentDelivery> SelectedLoanDeliveries
        => (SelectedLoanAgreement?.DocumentDeliverys as IReadOnlyList<DocumentDelivery>)
           ?? Array.Empty<DocumentDelivery>();

    public List<MergeRow> LiveMergeRows { get; private set; } = new();

    public sealed class MergeRow
    {
        public string Status { get; set; } = "";
        public string DocumentName { get; set; } = "";
        public string CompletedLocal { get; set; } = "";
        public int Bytes { get; set; }
    }

    private readonly IDocumentOutputService outputService;
    private readonly IJobQueue<LoanJob> loanQueue;
    private readonly IJobQueue<MergeJob> mergeQueue;
    private readonly IJobQueue<EmailJob> emailQueue;
    private readonly IDocumentManagerState docState;
    private readonly ILogger<AdminBenchViewModel> logger;

    private readonly Dictionary<Guid, string> docNameCache = new();

    public AdminBenchViewModel(
        IDocumentOutputService outputService,
        IJobQueue<LoanJob> loanQueue,
        IJobQueue<MergeJob> mergeQueue,
        IJobQueue<EmailJob> emailQueue,
        IDocumentManagerState docState,
        ILogger<AdminBenchViewModel> logger)
    {
        this.outputService = outputService;
        this.loanQueue = loanQueue;
        this.mergeQueue = mergeQueue;
        this.emailQueue = emailQueue;
        this.docState = docState;
        this.logger = logger;
    }

    public bool CanRunOneButton
        => SelectedDocLibId != Guid.Empty
           && SelectedLoanAgreement is not null
           && SelectedLoanType is not null;

    public async Task InitializeAsync()
    {
        try
        {
            Status = "Loading admin bench…";

            DocLibIds = outputService.GetDocLibIds();
            if (SelectedDocLibId == Guid.Empty && DocLibIds.Count > 0)
                SelectedDocLibId = DocLibIds[0];

            LoanAgreements = outputService.GetLoanAgreements();

            await OnDocLibChangedAsync().ConfigureAwait(false);

            // If nothing selected, pick first loan so the bench shows something.
            if (SelectedLoanAgreement is null)
                SelectedLoanAgreement = LoanAgreements.FirstOrDefault();

            if (SelectedLoanAgreement is not null)
                await OnLoanAgreementChangedAsync().ConfigureAwait(false);

            Status ??= "Ready.";
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "InitializeAsync failed");
            Status = "Init failed. Check logs.";
        }
    }

    public async Task OnDocLibChangedAsync()
    {
        try
        {
            Status = "Loading Doc Library assets…";

            Documents = outputService.GetDocuments(SelectedDocLibId);
            LoanTypes = outputService.GetLoanTypes(SelectedDocLibId);

            if (SelectedLoanType is null || LoanTypes.All(x => x.Id != SelectedLoanType.Id))
                SelectedLoanType = LoanTypes.FirstOrDefault();

            RebuildDocNameCache(Documents);

            // If a loan is already selected, keep its DocLibId aligned and refresh preview.
            if (SelectedLoanAgreement is not null)
                await OnLoanAgreementChangedAsync().ConfigureAwait(false);

            RebuildLiveMergeRows();

            Status = $"Loaded: {Documents.Count} docs, {LoanTypes.Count} loan types.";
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "OnDocLibChangedAsync failed");
            Status = "Doc Library reload failed. Check logs.";
        }
    }

    public async Task OnLoanAgreementChangedAsync()
    {
        try
        {
            if (SelectedLoanAgreement is null)
                return;

            // Keep bench DocLib aligned to the selected loan (this matters for doc pool).
            if (SelectedLoanAgreement.DocLibId != Guid.Empty && SelectedLoanAgreement.DocLibId != SelectedDocLibId)
            {
                SelectedDocLibId = SelectedLoanAgreement.DocLibId;

                Documents = outputService.GetDocuments(SelectedDocLibId);
                LoanTypes = outputService.GetLoanTypes(SelectedDocLibId);

                RebuildDocNameCache(Documents);
            }

            // Align LoanType selection to what the loan currently stores (if set).
            await EnsureLoanTypeSelectionFromLoanAsync().ConfigureAwait(false);

            // Optional: prefill EmailTo from the loan (but still optional).
            if (string.IsNullOrWhiteSpace(EmailTo))
                EmailTo = SelectedLoanAgreement.EmailTo ?? "";

            // ✅ Restore classic Admin Bench behavior: compute preview deliveries immediately.
            RebuildPreviewDeliveries();

            RebuildLiveMergeRows();

            var deliveries = SelectedLoanAgreement.DocumentDeliverys?.Count ?? 0;
            Status = deliveries > 0
                ? $"Preview deliveries: {deliveries} (select LoanType/Loan to refresh)."
                : "No documents matched (preview). Check rules, keys, and DocLibId/doc pool.";
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "OnLoanAgreementChangedAsync failed");
            Status = "Loan select failed. Check logs.";
        }
    }

    public Task OnLoanTypeChangedAsync()
    {
        try
        {
            // ✅ Preview refresh on loan type change (no Run button required)
            RebuildPreviewDeliveries();

            var deliveries = SelectedLoanAgreement?.DocumentDeliverys?.Count ?? 0;
            Status = deliveries > 0
                ? $"Preview deliveries: {deliveries}"
                : "No documents matched (preview). Check rules/keys/doc pool.";
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "OnLoanTypeChangedAsync failed");
            Status = "LoanType change failed. Check logs.";
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// ONE BUTTON:
    /// - If deliveries don't exist in DB, queue LoanJob to persist them
    /// - Queue merges
    /// - Queue email only if EmailTo is provided
    /// </summary>
    public async Task RunMergeAndEmailAsync()
    {
        if (!CanRunOneButton)
        {
            Status = "Select Doc Library, Loan Agreement, and Loan Type.";
            return;
        }

        IsBusy = true;

        try
        {
            var loanId = SelectedLoanAgreement!.Id;

            // Refresh selected loan from DB so we see persisted deliveries if they exist
            SelectedLoanAgreement = outputService.GetLoanAgreements()
                .FirstOrDefault(l => l.Id == loanId) ?? SelectedLoanAgreement;

            // If still no deliveries persisted, queue pipeline to generate + persist them
            if ((SelectedLoanAgreement!.DocumentDeliverys?.Count ?? 0) == 0)
            {
                Status = "No persisted deliveries found. Queueing Loan pipeline to generate deliveries…";

                await QueueThenGeneratePipelineAsync().ConfigureAwait(false);

                var gotDeliveries = await WaitForDeliveriesAsync(loanId, timeoutSeconds: 30).ConfigureAwait(false);
                if (!gotDeliveries)
                {
                    // Keep preview available even if persistence fails
                    RebuildPreviewDeliveries();
                    Status = "Timed out waiting for persisted deliveries. Preview still shown. Check LoanWorker persistence/logs.";
                    return;
                }

                SelectedLoanAgreement = outputService.GetLoanAgreements()
                    .FirstOrDefault(l => l.Id == loanId) ?? SelectedLoanAgreement;
            }

            var deliveryCount = SelectedLoanAgreement!.DocumentDeliverys?.Count ?? 0;
            Status = $"Persisted deliveries: {deliveryCount}. Queueing merges…";

            // Queue merges based on persisted deliveries
            var queued = await QueueMergesFromDeliveriesAsync(loanId).ConfigureAwait(false);
            if (queued == 0)
            {
                Status = "No merge jobs queued. Likely missing documents in DocumentLibrary.Documents for this DocLibId.";
                return;
            }

            Status = $"Queued {queued} merge job(s). Waiting for completion…";
            var completed = await WaitForMergesCompleteAsync(loanId, expectedCount: queued, timeoutSeconds: 90)
                .ConfigureAwait(false);

            RebuildLiveMergeRows();

            if (!completed)
            {
                Status = "Timed out waiting for merges. Check MergeWorker logs / service toggles.";
                return;
            }

            var to = ResolveEmailTo().Trim();
            if (string.IsNullOrWhiteSpace(to))
            {
                Status = "✅ Merges complete. No email sent (Email To was blank).";
                return;
            }

            var traceCount = SelectedLoanAgreement?.AdminBench?.Trace?.Count ?? 0;
            var subject = $"Admin Bench Results: {SelectedLoanAgreement?.LoanTypeName ?? "Loan"} | Deliveries={deliveryCount} | Trace={traceCount}";

            var emailJob = new EmailJob(
                loanId,
                to,
                subject,
                EmailAttachmentOutput,
                ZipMaxWaitSeconds: 10
            );

            await emailQueue.EnqueueAsync(emailJob, CancellationToken.None).ConfigureAwait(false);

            Status = EmailAttachmentOutput == EmailEnums.AttachmentOutput.ZipFile
                ? $"✅ Done. Merges complete. Queued ZIP email to {to}."
                : $"✅ Done. Merges complete. Queued email to {to}.";
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "RunMergeAndEmailAsync failed");
            Status = "Run failed. Check logs.";
        }
        finally
        {
            IsBusy = false;
        }
    }

    // =========================================================
    // Preview evaluation (RESTORES OLD ADMIN BENCH BEHAVIOR)
    // =========================================================

    private void RebuildPreviewDeliveries()
    {
        if (SelectedLoanAgreement is null || SelectedLoanType is null)
            return;

        // Always evaluate against the correct doc pool for THIS loan/doclib
        var docLibIdForLoan = SelectedLoanAgreement.DocLibId != Guid.Empty
            ? SelectedLoanAgreement.DocLibId
            : SelectedDocLibId;

        var docPool = outputService.GetDocuments(docLibIdForLoan);

        // Keep local lists in sync for name resolution
        Documents = docPool;
        RebuildDocNameCache(docPool);

        // Evaluate final document list (defaults + rule generated depends on your evaluator)
        var results = outputService.EvaluateDocuments(SelectedLoanType, SelectedLoanAgreement, docPool);

        SelectedLoanAgreement.DocumentDeliverys ??= new List<DocumentDelivery>();
        SelectedLoanAgreement.DocumentDeliverys.Clear();

        foreach (var doc in results)
        {
            SelectedLoanAgreement.DocumentDeliverys.Add(new DocumentDelivery
            {
                DocId = doc.Id,
                OutputType = OutputType, // bench override for preview
                Copies = doc.Copies <= 0 ? 1 : doc.Copies,
                DelieveryTypes = DocumentTypes.DelieveryTypes.Email,
                DeliveryLoaction = string.Empty
            });
        }
    }

    // =========================================================
    // Queue/Wait helpers
    // =========================================================

    private async Task QueueThenGeneratePipelineAsync()
    {
        var loan = outputService.GetLoanAgreements()
            .FirstOrDefault(l => l.Id == SelectedLoanAgreement!.Id);

        if (loan is null)
            throw new InvalidOperationException("Loan not found.");

        var lt = SelectedLoanType!;
        loan.LoanTypeId = lt.Id;
        loan.LoanTypeName = lt.Name;

        SetIfExists(loan, "OutputType", OutputType);

        // Do NOT set loan.EmailTo here.
        if (loan.AdminBench is not null)
        {
            loan.AdminBench.Enabled = true;
            loan.AdminBench.OutputTypeOverride = OutputType;
            loan.AdminBench.SuppressMerge = true; // bench queues merges itself
        }

        await loanQueue.EnqueueAsync(new LoanJob(loan), CancellationToken.None).ConfigureAwait(false);
    }

    private async Task<bool> WaitForDeliveriesAsync(Guid loanId, int timeoutSeconds)
    {
        var deadline = DateTime.UtcNow.AddSeconds(timeoutSeconds);

        while (DateTime.UtcNow < deadline)
        {
            var loan = outputService.GetLoanAgreements().FirstOrDefault(l => l.Id == loanId);
            var count = loan?.DocumentDeliverys?.Count ?? 0;

            if (count > 0)
                return true;

            await Task.Delay(300).ConfigureAwait(false);
        }

        return false;
    }

    private async Task<bool> WaitForMergesCompleteAsync(Guid loanId, int expectedCount, int timeoutSeconds)
    {
        var deadline = DateTime.UtcNow.AddSeconds(timeoutSeconds);

        while (DateTime.UtcNow < deadline)
        {
            var merges = docState.DocumentList.Values
                .Where(x => x?.LoanAgreement?.Id == loanId)
                .ToList();

            var done = merges.Count(x => x.Status == DocumentMergeState.Status.Complete);
            var err = merges.Count(x => x.Status == DocumentMergeState.Status.Error);

            if (done >= expectedCount)
                return true;

            if (err > 0 && (done + err) >= expectedCount)
                return true;

            await Task.Delay(350).ConfigureAwait(false);
        }

        return false;
    }

    private async Task<int> QueueMergesFromDeliveriesAsync(Guid loanId)
    {
        var loanFromDb = outputService.GetLoanAgreements().FirstOrDefault(l => l.Id == loanId);
        var loan = loanFromDb ?? SelectedLoanAgreement!;
        var deliveries = loan.DocumentDeliverys ?? new List<DocumentDelivery>();

        var docLibIdForLoan = loan.DocLibId != Guid.Empty ? loan.DocLibId : SelectedDocLibId;

        var docPool = outputService.GetDocuments(docLibIdForLoan);
        Documents = docPool;
        var docsById = docPool.GroupBy(d => d.Id).ToDictionary(g => g.Key, g => g.First());

        RebuildDocNameCache(docPool);

        var loanForMerges = CloneLoanForMerge(loan);
        loanForMerges.EmailTo = null;

        var queued = 0;

        foreach (var delivery in deliveries)
        {
            if (!docsById.TryGetValue(delivery.DocId, out var doc))
            {
                logger.LogWarning("QueueMergesFromDeliveriesAsync: DocId={DocId} not found for DocLibId={DocLibId}",
                    delivery.DocId, docLibIdForLoan);
                continue;
            }

            var mergeDoc = CloneDocumentForMerge(doc);
            mergeDoc.OutputType = delivery.OutputType;

            var merge = new DocumentMerge
            {
                LoanAgreement = loanForMerges,
                Document = mergeDoc,
                Status = DocumentMergeState.Status.Queued
            };

            await mergeQueue.EnqueueAsync(new MergeJob(merge), CancellationToken.None).ConfigureAwait(false);
            queued++;
        }

        return queued;
    }

    // =========================================================
    // UI helpers
    // =========================================================

    public string GetLoanLabel(LoanAgreement loan) => outputService.GetLoanLabel(loan);

    public string GetDocName(Guid docId)
    {
        if (docId == Guid.Empty) return "";

        if (docNameCache.TryGetValue(docId, out var name) && !string.IsNullOrWhiteSpace(name))
            return name;

        var local = Documents.FirstOrDefault(d => d.Id == docId)?.Name;
        if (!string.IsNullOrWhiteSpace(local))
        {
            docNameCache[docId] = local!;
            return local!;
        }

        return docId.ToString();
    }

    private string ResolveEmailTo()
    {
        if (!string.IsNullOrWhiteSpace(EmailTo))
            return EmailTo;

        return SelectedLoanAgreement?.EmailTo ?? "";
    }

    private Task EnsureLoanTypeSelectionFromLoanAsync()
    {
        if (SelectedLoanAgreement is null)
            return Task.CompletedTask;

        var storedId = SelectedLoanAgreement.LoanTypeId;

        if (storedId != Guid.Empty)
            SelectedLoanType = LoanTypes.FirstOrDefault(x => x.Id == storedId) ?? SelectedLoanType;

        if (SelectedLoanType is null)
            SelectedLoanType = LoanTypes.FirstOrDefault();

        return Task.CompletedTask;
    }

    private void RebuildLiveMergeRows()
    {
        LiveMergeRows.Clear();

        if (SelectedLoanAgreement is null)
            return;

        var loanId = SelectedLoanAgreement.Id;

        var rows = docState.DocumentList.Values
            .Where(x => x?.LoanAgreement?.Id == loanId)
            .OrderByDescending(x => x.MergeCompleteAt ?? DateTime.MinValue)
            .Select(x => new MergeRow
            {
                Status = x.Status.ToString(),
                DocumentName = x.Document?.Name ?? "(no name)",
                CompletedLocal = x.MergeCompleteAt?.ToLocalTime().ToString("g") ?? "",
                Bytes = x.MergedDocumentBytes?.Length ?? 0
            })
            .ToList();

        LiveMergeRows.AddRange(rows);
    }

    private void RebuildDocNameCache(IEnumerable<Document> docs)
    {
        docNameCache.Clear();

        foreach (var d in docs)
        {
            if (d is null) continue;
            if (d.Id == Guid.Empty) continue;
            if (string.IsNullOrWhiteSpace(d.Name)) continue;

            if (!docNameCache.ContainsKey(d.Id))
                docNameCache[d.Id] = d.Name!;
        }
    }

    private static Document CloneDocumentForMerge(Document source)
    {
        return new Document
        {
            Id = source.Id,
            DocLibId = source.DocLibId,
            Name = source.Name,
            DocStoreId = source.DocStoreId,

            TemplateRef = source.TemplateRef,
            MergedRef = source.MergedRef,
            MasterTemplateDocumentUsedName = source.MasterTemplateDocumentUsedName,

            TemplateDocumentBytes = source.TemplateDocumentBytes,
            MergedDocumentBytes = source.MergedDocumentBytes,

            HiddenTagName = source.HiddenTagName,
            HiddenTagValue = source.HiddenTagValue,
            UpdatedAt = source.UpdatedAt,

            GenerateMultipleFor = source.GenerateMultipleFor is null
                ? new List<DocumentTypes.GenerateMultipleFor>()
                : new List<DocumentTypes.GenerateMultipleFor>(source.GenerateMultipleFor),

            OutputType = source.OutputType,
            Copies = source.Copies
        };
    }

    private static LoanAgreement CloneLoanForMerge(LoanAgreement source)
    {
        return new LoanAgreement
        {
            Id = source.Id,
            DocLibId = source.DocLibId,
            LoanTypeId = source.LoanTypeId,
            LoanTypeName = source.LoanTypeName,

            LenderCode = source.LenderCode,
            BrokerCode = source.BrokerCode,
            BorrowerCode = source.BorrowerCode,
            PropertyState = source.PropertyState,

            LenderNames = source.LenderNames,
            BorrowerNames = source.BorrowerNames,
            BrokerNames = source.BrokerNames,

            EmailTo = source.EmailTo,
            OutputType = source.OutputType,

            DocumentDeliverys = source.DocumentDeliverys,
            AdminBench = source.AdminBench,
        };
    }

    private static void SetIfExists(object target, string propertyName, object? value)
    {
        if (target is null) return;

        try
        {
            var prop = target.GetType().GetProperty(propertyName,
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.IgnoreCase);

            if (prop is null || !prop.CanWrite) return;

            if (value is null)
            {
                prop.SetValue(target, null);
                return;
            }

            var propType = Nullable.GetUnderlyingType(prop.PropertyType) ?? prop.PropertyType;

            if (propType.IsInstanceOfType(value))
            {
                prop.SetValue(target, value);
                return;
            }

            var converted = Convert.ChangeType(value, propType);
            prop.SetValue(target, converted);
        }
        catch
        {
            // intentionally swallow
        }
    }
}

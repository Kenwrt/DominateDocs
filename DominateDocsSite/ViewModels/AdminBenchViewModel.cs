using DocumentManager.Email;
using DocumentManager.Infrastructure;
using DocumentManager.Jobs;
using DocumentManager.Services;
using DocumentManager.State;
using DominateDocsData.Enums;
using DominateDocsData.Models;
using Microsoft.Extensions.Logging;

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
    private readonly IJobQueue<EmailJob> emailQueue;
    private readonly IDocumentManagerState docState;
    private readonly ILogger<AdminBenchViewModel> logger;

    private readonly Dictionary<Guid, string> docNameCache = new();

    public AdminBenchViewModel(
        IDocumentOutputService outputService,
        IJobQueue<LoanJob> loanQueue,
        IJobQueue<EmailJob> emailQueue,
        IDocumentManagerState docState,
        ILogger<AdminBenchViewModel> logger)
    {
        this.outputService = outputService;
        this.loanQueue = loanQueue;
        this.emailQueue = emailQueue;
        this.docState = docState;
        this.logger = logger;
    }

    public bool CanRunOneButton
        => SelectedDocLibId != Guid.Empty
           && SelectedLoanAgreement is not null
           && SelectedLoanType is not null;

    // AdminBench.razor calls this.
    public Task InitializeAsync() => LoadInternalAsync();

    // Keep compatibility if anything else still calls LoadAsync().
    public Task LoadAsync() => LoadInternalAsync();

    private Task LoadInternalAsync()
    {
        try
        {
            DocLibIds = outputService.GetDocLibIds();

            // Default DocLib first, because LoanTypes require it.
            if (SelectedDocLibId == Guid.Empty && DocLibIds.Count > 0)
                SelectedDocLibId = DocLibIds[0];

            LoanTypes = SelectedDocLibId != Guid.Empty
                ? outputService.GetLoanTypes(SelectedDocLibId)
                : new List<LoanType>();

            LoanAgreements = outputService.GetLoanAgreements();

            if (SelectedLoanType is null && LoanTypes.Count > 0)
                SelectedLoanType = LoanTypes[0];

            if (SelectedLoanAgreement is null && LoanAgreements.Count > 0)
                SelectedLoanAgreement = LoanAgreements[0];

            // preload docs if possible
            if (SelectedDocLibId != Guid.Empty)
            {
                Documents = outputService.GetDocuments(SelectedDocLibId);
                RebuildDocNameCache(Documents);
            }

            RebuildPreviewDeliveries();
            RebuildLiveMergeRows();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "AdminBench InitializeAsync/LoadAsync failed");
            Status = "Failed to load Admin Bench data. Check logs.";
        }

        return Task.CompletedTask;
    }

    public Task OnDocLibChangedAsync()
    {
        try
        {
            if (SelectedDocLibId == Guid.Empty)
                return Task.CompletedTask;

            Documents = outputService.GetDocuments(SelectedDocLibId);
            RebuildDocNameCache(Documents);

            // LoanTypes are DocLib scoped
            LoanTypes = outputService.GetLoanTypes(SelectedDocLibId);

            if (SelectedLoanType is null || (SelectedLoanType.DocLibId != SelectedDocLibId))
                SelectedLoanType = LoanTypes.FirstOrDefault();

            RebuildPreviewDeliveries();

            var deliveries = SelectedLoanAgreement?.DocumentDeliverys?.Count ?? 0;
            Status = deliveries > 0
                ? $"Preview deliveries: {deliveries}"
                : "No documents matched (preview). Check rules/keys/doc pool.";
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "OnDocLibChangedAsync failed");
            Status = "Doc library change failed. Check logs.";
        }

        return Task.CompletedTask;
    }

    public Task OnLoanAgreementChangedAsync()
    {
        try
        {
            RebuildPreviewDeliveries();

            var deliveries = SelectedLoanAgreement?.DocumentDeliverys?.Count ?? 0;
            Status = deliveries > 0
                ? $"Preview deliveries: {deliveries}"
                : "No documents matched (preview). Check rules/keys/doc pool.";
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "OnLoanAgreementChangedAsync failed");
            Status = "Loan selection change failed. Check logs.";
        }

        return Task.CompletedTask;
    }

    public Task OnLoanTypeChangedAsync()
    {
        try
        {
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

    public string GetLoanLabel(LoanAgreement loan) => outputService.GetLoanLabel(loan);

    public string GetDocName(Guid docId)
    {
        if (docNameCache.TryGetValue(docId, out var name))
            return name;

        var doc = outputService.TryResolveDocumentById(docId);
        if (doc?.Name is not null)
        {
            docNameCache[docId] = doc.Name;
            return doc.Name;
        }

        return docId.ToString();
    }

    /// <summary>
    /// ONE BUTTON:
    /// - Queue LoanJob (LoanWorker populates formatted names + persists loan + deliveries)
    /// - LoanWorker also queues merges (we do NOT clone/queue merges here)
    /// - Email behavior:
    ///   - IndividualDocument: MergeWorker emails each merged doc (NO EmailJob here)
    ///   - ZipFile: enqueue EmailJob here (single ZIP email)
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

            Status = "Queueing LoanWorker (this is where formatted names get persisted)…";

            await QueueLoanPipelineAsync(runMerges: true).ConfigureAwait(false);

            // Wait for formatted names to exist in DB (LoanWorker persists them via UpSertRecordAsync)
            var ensuredNames = await WaitForFormattedNamesAsync(loanId, timeoutSeconds: 45).ConfigureAwait(false);
            if (!ensuredNames)
            {
                Status = "Timed out waiting for LoanWorker to persist formatted name fields. Check LoanWorker logs.";
                return;
            }

            // Refresh loan from DB (so UI shows persisted fields)
            SelectedLoanAgreement = outputService.GetLoanAgreements().FirstOrDefault(l => l.Id == loanId) ?? SelectedLoanAgreement;

            // If there are deliveries, LoanWorker will have queued merges. If none, there is nothing to merge.
            var deliveryCount = SelectedLoanAgreement?.DocumentDeliverys?.Count ?? 0;
            if (deliveryCount == 0)
            {
                Status = "Formatted names persisted, but 0 deliveries matched. No merges to run.";
                return;
            }

            Status = $"Deliveries persisted: {deliveryCount}. Waiting for merges to complete…";

            var completed = await WaitForMergesCompleteAsync(loanId, timeoutSeconds: 120).ConfigureAwait(false);

            RebuildLiveMergeRows();

            if (!completed)
            {
                Status = "Timed out waiting for merges. Check MergeWorker logs / toggles.";
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

            if (EmailAttachmentOutput == EmailEnums.AttachmentOutput.ZipFile)
            {
                var emailJob = new EmailJob(
                    loanId,
                    to,
                    subject,
                    EmailAttachmentOutput,
                    ZipMaxWaitSeconds: 10);

                await emailQueue.EnqueueAsync(emailJob, CancellationToken.None).ConfigureAwait(false);

                Status = $"✅ Done. Merges complete. Queued ZIP email to {to}.";
            }
            else
            {
                // IndividualDocument mode: MergeWorker emails each merged document.
                Status = $"✅ Done. Merges complete. Emails sent per document to {to}.";
            }
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

    private void RebuildPreviewDeliveries()
    {
        if (SelectedLoanAgreement is null || SelectedLoanType is null || SelectedDocLibId == Guid.Empty)
            return;

        // Pull doc pool
        Documents = outputService.GetDocuments(SelectedDocLibId);
        RebuildDocNameCache(Documents);

        // Evaluate (preview)
        var matched = outputService.EvaluateDocuments(SelectedLoanType, SelectedLoanAgreement, Documents);

        SelectedLoanAgreement.DocumentDeliverys ??= new List<DocumentDelivery>();
        SelectedLoanAgreement.DocumentDeliverys.Clear();

        foreach (var doc in matched)
        {
            SelectedLoanAgreement.DocumentDeliverys.Add(new DocumentDelivery
            {
                DocId = doc.Id,
                OutputType = OutputType,
                Copies = doc.Copies <= 0 ? 1 : doc.Copies,
                DelieveryTypes = DocumentTypes.DelieveryTypes.Email,
                DeliveryLoaction = ""
            });
        }
    }

    private async Task QueueLoanPipelineAsync(bool runMerges)
    {
        var loanId = SelectedLoanAgreement!.Id;

        // Pull from DB (LoanWorker should work on persisted object)
        var loan = outputService.GetLoanAgreements().FirstOrDefault(l => l.Id == loanId);
        if (loan is null) throw new InvalidOperationException("Loan not found in DB.");

        // Apply AdminBench overrides using the real model type (LoanAgreement.AdminBenchOverrides)
        loan.AdminBench ??= new LoanAgreement.AdminBenchOverrides();
        loan.AdminBench.Enabled = true;
        loan.AdminBench.OutputTypeOverride = OutputType;

        // ✅ CRITICAL:
        // - IndividualDocument: allow MergeWorker emails by providing EmailToOverride
        // - ZipFile: suppress per-doc emails by clearing EmailToOverride (LoanWorker won't copy to LoanAgreement.EmailTo)
        loan.AdminBench.EmailToOverride = EmailAttachmentOutput == EmailEnums.AttachmentOutput.ZipFile ? string.Empty : ResolveEmailTo();

        loan.AdminBench.SuppressMerge = !runMerges;

        // Context overrides for rule evaluation
        loan.DocLibId = SelectedDocLibId;
        loan.OutputType = OutputType;

        loan.LoanTypeId = SelectedLoanType!.Id;
        loan.LoanTypeName = SelectedLoanType!.Name;

        await loanQueue.EnqueueAsync(new LoanJob(loan), CancellationToken.None).ConfigureAwait(false);
    }

    private async Task<bool> WaitForFormattedNamesAsync(Guid loanId, int timeoutSeconds)
    {
        var deadline = DateTime.UtcNow.AddSeconds(timeoutSeconds);

        while (DateTime.UtcNow < deadline)
        {
            var refreshed = outputService.GetLoanAgreements().FirstOrDefault(l => l.Id == loanId);

            if (refreshed is not null && HasFormattedNames(refreshed))
            {
                SelectedLoanAgreement = refreshed;
                return true;
            }

            await Task.Delay(300).ConfigureAwait(false);
        }

        return false;
    }

    private static bool HasFormattedNames(LoanAgreement loan)
    {
        // Loan-level fields used in templates
        if (string.IsNullOrWhiteSpace(loan.LenderNames)) return false;
        if (string.IsNullOrWhiteSpace(loan.BorrowerNames)) return false;
        if (string.IsNullOrWhiteSpace(loan.BrokerNames)) return false;

        // Optional but commonly used
        if (loan.Guarantors is not null && loan.Guarantors.Count > 0 && string.IsNullOrWhiteSpace(loan.GuarantorNames))
            return false;

        // If parties exist, at least one should have FormattedName
        if (loan.Lenders is not null && loan.Lenders.Count > 0)
            return loan.Lenders.Any(x => !string.IsNullOrWhiteSpace(x.FormattedName));

        return true;
    }

    private async Task<bool> WaitForMergesCompleteAsync(Guid loanId, int timeoutSeconds)
    {
        var deadline = DateTime.UtcNow.AddSeconds(timeoutSeconds);

        while (DateTime.UtcNow < deadline)
        {
            var merges = docState.DocumentList.Values
                .Where(m => m?.LoanAgreement?.Id == loanId)
                .ToList();

            if (merges.Count == 0)
            {
                await Task.Delay(300).ConfigureAwait(false);
                continue;
            }

            var done = merges.Count(x => x.Status == DocumentMergeState.Status.Complete);
            var err = merges.Count(x => x.Status == DocumentMergeState.Status.Error);

            if (done + err >= merges.Count)
                return true;

            await Task.Delay(300).ConfigureAwait(false);
        }

        return false;
    }

    private void RebuildLiveMergeRows()
    {
        try
        {
            if (SelectedLoanAgreement is null)
            {
                LiveMergeRows = new List<MergeRow>();
                return;
            }

            var loanId = SelectedLoanAgreement.Id;

            var merges = docState.DocumentList.Values
                .Where(m => m?.LoanAgreement?.Id == loanId)
                .OrderByDescending(m => m.UpdatedAt)
                .ToList();

            LiveMergeRows = merges.Select(m => new MergeRow
            {
                Status = m.Status.ToString(),
                DocumentName = m.Document?.Name ?? GetDocName(m.Document?.Id ?? Guid.Empty),
                CompletedLocal = m.UpdatedAt.ToLocalTime().ToString("g"),
                Bytes = m.Document?.MergedDocumentBytes?.Length ?? 0
            }).ToList();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "RebuildLiveMergeRows failed");
            LiveMergeRows = new List<MergeRow>();
        }
    }

    private void RebuildDocNameCache(IReadOnlyList<Document> docs)
    {
        docNameCache.Clear();

        foreach (var d in docs)
        {
            if (d is null) continue;
            if (d.Id == Guid.Empty) continue;

            docNameCache[d.Id] = d.Name ?? d.Id.ToString();
        }
    }

    private string ResolveEmailTo()
    {
        // Bench default is user input; LoanWorker can also read AdminBench.EmailToOverride
        return EmailTo ?? string.Empty;
    }
}

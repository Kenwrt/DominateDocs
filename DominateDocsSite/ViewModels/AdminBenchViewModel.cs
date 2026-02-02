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

    public string EmailTo { get; set; } = "";

    public EmailEnums.AttachmentOutput EmailAttachmentOutput { get; set; } =
        EmailEnums.AttachmentOutput.IndividualDocument;

    // Bench override (global for this run)
    public DocumentTypes.OutputTypes OutputType { get; set; } = DocumentTypes.OutputTypes.PDF;

    public List<Document> Documents { get; private set; } = new();
    public List<LoanType> LoanTypes { get; private set; } = new();
    public List<LoanAgreement> LoanAgreements { get; private set; } = new();

    public LoanAgreement? SelectedLoanAgreement { get; set; }
    public LoanType? SelectedLoanType { get; set; }

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
           && SelectedLoanType is not null
           && !string.IsNullOrWhiteSpace(ResolveEmailTo());

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

            Status = "Ready.";
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

            await EnsureLoanTypeSelectionFromLoanAsync().ConfigureAwait(false);

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
            await EnsureLoanTypeSelectionFromLoanAsync().ConfigureAwait(false);

            if (SelectedLoanAgreement != null)
            {
                if (string.IsNullOrWhiteSpace(EmailTo))
                    EmailTo = SelectedLoanAgreement.EmailTo ?? "";

                if (SelectedLoanAgreement.OutputType != default)
                    OutputType = SelectedLoanAgreement.OutputType;
            }

            RebuildLiveMergeRows();

            var deliveries = SelectedLoanAgreement?.DocumentDeliverys?.Count ?? 0;
            Status = deliveries > 0
                ? $"Loan selected. Persisted deliveries: {deliveries}."
                : "Loan selected. No persisted deliveries yet.";
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "OnLoanAgreementChangedAsync failed");
            Status = "Loan select failed. Check logs.";
        }
    }

    public Task OnLoanTypeChangedAsync()
    {
        // Single button does the work. Selection changes should be harmless.
        return Task.CompletedTask;
    }

    /// <summary>
    /// ONE BUTTON:
    /// 1) Ensure deliveries exist (queue ThenGenerate pipeline if needed)
    /// 2) Queue merges from deliveries (but prevent any auto-email-per-doc behavior)
    /// 3) Wait for merges
    /// 4) Queue ONE EmailJob (zip/individual)
    /// </summary>
    public async Task RunMergeAndEmailAsync()
    {
        if (!CanRunOneButton)
        {
            Status = "Select Doc Library, Loan Agreement, Loan Type, and provide an email address.";
            return;
        }

        IsBusy = true;

        try
        {
            var loanId = SelectedLoanAgreement!.Id;
            var to = ResolveEmailTo().Trim();

            // Refresh selected loan
            SelectedLoanAgreement = outputService.GetLoanAgreements()
                .FirstOrDefault(l => l.Id == loanId) ?? SelectedLoanAgreement;

            // 1) Ensure deliveries exist (persisted on loan)
            if ((SelectedLoanAgreement!.DocumentDeliverys?.Count ?? 0) == 0)
            {
                Status = "No deliveries found. Running ThenGenerate evaluation to create deliveries…";

                await QueueThenGeneratePipelineAsync(to).ConfigureAwait(false);

                var gotDeliveries = await WaitForDeliveriesAsync(loanId, timeoutSeconds: 30).ConfigureAwait(false);
                if (!gotDeliveries)
                {
                    Status = "Timed out waiting for deliveries. Check LoanWorker logs and ThenGenerate rules.";
                    return;
                }

                SelectedLoanAgreement = outputService.GetLoanAgreements()
                    .FirstOrDefault(l => l.Id == loanId) ?? SelectedLoanAgreement;
            }

            var deliveryCount = SelectedLoanAgreement!.DocumentDeliverys?.Count ?? 0;
            Status = $"Proposed deliveries: {deliveryCount}. Queueing merges…";

            // 2) Queue merges from deliveries, but DO NOT allow auto-email-per-doc
            var queued = await QueueMergesFromDeliveriesAsync(loanId).ConfigureAwait(false);
            if (queued == 0)
            {
                Status = "No merge jobs queued. Likely missing documents in the selected Doc Library.";
                return;
            }

            // 3) Wait for merges
            Status = $"Queued {queued} merge job(s). Waiting for completion…";
            var completed = await WaitForMergesCompleteAsync(loanId, expectedCount: queued, timeoutSeconds: 90)
                .ConfigureAwait(false);

            RebuildLiveMergeRows();

            if (!completed)
            {
                Status = "Timed out waiting for merges to complete. Check MergeWorker logs / background service status.";
                return;
            }

            // 4) Queue ONE email job (this is the ONLY intended email path)
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

    private async Task QueueThenGeneratePipelineAsync(string emailTo)
    {
        var loan = outputService.GetLoanAgreements()
            .FirstOrDefault(l => l.Id == SelectedLoanAgreement!.Id);

        if (loan is null)
            throw new InvalidOperationException("Loan not found.");

        var lt = SelectedLoanType!;
        loan.LoanTypeId = lt.Id;
        loan.LoanTypeName = lt.Name;

        // Bench selections written to the loan for evaluation/trace
        SetIfExists(loan, "OutputType", OutputType);

        // IMPORTANT:
        // Do NOT set loan.EmailTo here.
        // Setting loan.EmailTo triggers other parts of your pipeline to auto-email per merged doc.
        // We only want the single EmailJob at the end.
        //
        // If your LoanWorker uses AdminBench overrides, set those instead (safe).
        if (loan.AdminBench is not null)
        {
            loan.AdminBench.Enabled = true;
            loan.AdminBench.SuppressMerge = true; // We control merges from the bench
            loan.AdminBench.OutputTypeOverride = OutputType;
            loan.AdminBench.EmailToOverride = emailTo;
        }

        await loanQueue.EnqueueAsync(new LoanJob(loan), CancellationToken.None).ConfigureAwait(false);
    }

    private async Task<bool> WaitForDeliveriesAsync(Guid loanId, int timeoutSeconds)
    {
        var stopAt = DateTime.UtcNow.AddSeconds(timeoutSeconds);

        while (DateTime.UtcNow < stopAt)
        {
            var fresh = outputService.GetLoanAgreements().FirstOrDefault(l => l.Id == loanId);
            var count = fresh?.DocumentDeliverys?.Count ?? 0;
            if (count > 0)
            {
                SelectedLoanAgreement = fresh;
                return true;
            }

            await Task.Delay(250).ConfigureAwait(false);
        }

        return false;
    }

    private async Task<int> QueueMergesFromDeliveriesAsync(Guid loanId)
    {
        Documents = outputService.GetDocuments(SelectedDocLibId);
        var docsById = Documents.GroupBy(d => d.Id).ToDictionary(g => g.Key, g => g.First());

        var loanFromDb = outputService.GetLoanAgreements().FirstOrDefault(l => l.Id == loanId);
        var loan = loanFromDb ?? SelectedLoanAgreement!;
        var deliveries = loan.DocumentDeliverys ?? new List<DocumentDelivery>();

        // CRITICAL:
        // Create a loan object for merge jobs that has EmailTo cleared.
        // This prevents any “auto-email each doc when merge completes” behavior.
        var loanForMerges = CloneLoanForMerge(loan);
        loanForMerges.EmailTo = null;

        var queued = 0;

        foreach (var delivery in deliveries)
        {
            if (!docsById.TryGetValue(delivery.DocId, out var doc))
                continue;

            // Bench override applies to output type for this run
            doc.OutputType = OutputType;

            var merge = new DocumentMerge
            {
                LoanAgreement = loanForMerges,
                Document = doc,
                Status = DocumentMergeState.Status.Queued
            };

            await mergeQueue.EnqueueAsync(new MergeJob(merge), CancellationToken.None).ConfigureAwait(false);
            queued++;
        }

        return queued;
    }

    private static LoanAgreement CloneLoanForMerge(LoanAgreement source)
    {
        // Shallow clone is enough: we want same IDs & metadata, but we control EmailTo.
        // Keep AdminBench + Delivery list references intact.
        return new LoanAgreement
        {
            Id = source.Id,
            UserId = source.UserId,
            UserType = source.UserType,
            UserProfile = source.UserProfile,
            ReferenceName = source.ReferenceName,
            LoanNumber = source.LoanNumber,
            LoanTypeId = source.LoanTypeId,
            LoanTypeName = source.LoanTypeName,
            DocLibId = source.DocLibId,
            OutputType = source.OutputType,
            EmailTo = source.EmailTo,
            AdminBench = source.AdminBench,
            DocumentDeliverys = source.DocumentDeliverys,
            LenderNames = source.LenderNames,
            BorrowerNames = source.BorrowerNames,
            BrokerNames = source.BrokerNames,
            LenderCode = source.LenderCode,
            BrokerCode = source.BrokerCode,
            BorrowerCode = source.BorrowerCode,
            PropertyState = source.PropertyState
        };
    }

    private async Task<bool> WaitForMergesCompleteAsync(Guid loanId, int expectedCount, int timeoutSeconds)
    {
        var stopAt = DateTime.UtcNow.AddSeconds(timeoutSeconds);

        while (DateTime.UtcNow < stopAt)
        {
            var completed = docState.DocumentList.Values.Count(m =>
                m is not null
                && m.LoanAgreement is not null
                && m.LoanAgreement.Id == loanId
                && m.Status == DocumentMergeState.Status.Complete
                && m.MergedDocumentBytes is not null
                && m.MergedDocumentBytes.Length > 0);

            if (completed >= expectedCount)
                return true;

            RebuildLiveMergeRows();
            await Task.Delay(300).ConfigureAwait(false);
        }

        return false;
    }

    public string GetLoanLabel(LoanAgreement loan) => outputService.GetLoanLabel(loan);

    public string GetDocName(Guid docId)
        => Documents.FirstOrDefault(d => d.Id == docId)?.Name ?? docId.ToString();

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

            if (propType.IsEnum && value is Enum e)
            {
                prop.SetValue(target, e);
                return;
            }

            if (propType == typeof(string))
            {
                prop.SetValue(target, value.ToString());
                return;
            }

            var converted = Convert.ChangeType(value, propType);
            prop.SetValue(target, converted);
        }
        catch
        {
            // bench tool should never crash the UI due to optional properties
        }
    }
}

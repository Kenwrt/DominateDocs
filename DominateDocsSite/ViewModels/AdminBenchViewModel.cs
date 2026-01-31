using DocumentManager.Infrastructure;
using DocumentManager.Jobs;
using DocumentManager.Services;
using DominateDocsData.Enums;
using DominateDocsData.Models;
using Microsoft.Extensions.Logging;
using System.Reflection;

namespace DominateDocsSite.ViewModels;

/// <summary>
/// Admin Bench driver for queuing worker-based jobs + displaying non-content results.
/// The UI should never render document bytes; only decisions (deliveries) + status.
/// </summary>
public sealed class AdminBenchViewModel
{
    public bool IsBusy { get; private set; }

    public string? Status { get; private set; }

    public List<Guid> DocLibIds { get; private set; } = new();

    public Guid SelectedDocLibId { get; set; }

    public string EmailTo { get; set; } = "";

    public DocumentTypes.OutputTypes OutputType { get; set; } = DocumentTypes.OutputTypes.PDF;

    public List<Document> Documents { get; private set; } = new();

    public List<LoanType> LoanTypes { get; private set; } = new();

    public List<LoanAgreement> LoanAgreements { get; private set; } = new();

    public LoanAgreement? SelectedLoanAgreement { get; set; }

    public LoanType? SelectedLoanType { get; set; }

    /// <summary>
    /// Snapshot of persisted ThenGenerate decisions (non-content) for the selected loan.
    /// </summary>
    public IReadOnlyList<DocumentDelivery> SelectedLoanDeliveries
        => (SelectedLoanAgreement?.DocumentDeliverys as IReadOnlyList<DocumentDelivery>)
           ?? Array.Empty<DocumentDelivery>();

    private readonly IDocumentOutputService outputService;
    private readonly IJobQueue<LoanJob> loanQueue;
    private readonly IJobQueue<MergeJob> mergeQueue;
    private readonly ILogger<AdminBenchViewModel> logger;

    public AdminBenchViewModel(
        IDocumentOutputService outputService,
        IJobQueue<LoanJob> loanQueue,
        IJobQueue<MergeJob> mergeQueue,
        ILogger<AdminBenchViewModel> logger)
    {
        this.outputService = outputService;
        this.loanQueue = loanQueue;
        this.mergeQueue = mergeQueue;
        this.logger = logger;
    }

    public async Task InitializeAsync()
    {
        try
        {
            Status = "Loading admin bench…";

            DocLibIds = outputService.GetDocLibIds();

            if (SelectedDocLibId == Guid.Empty && DocLibIds.Count > 0)
                SelectedDocLibId = DocLibIds[0];

            await ReloadAsync().ConfigureAwait(false);

            Status = "Ready.";
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "InitializeAsync failed");
            Status = "Init failed. Check logs.";
        }
    }

    public bool CanMergeFromDeliveries =>
        SelectedDocLibId != Guid.Empty &&
        SelectedLoanAgreement != null &&
        (SelectedLoanAgreement?.DocumentDeliverys?.Count ?? 0) > 0;

    public async Task ReloadAsync()
    {
        try
        {
            Status = "Reloading lists…";

            Documents = outputService.GetDocuments(SelectedDocLibId);
            LoanTypes = outputService.GetLoanTypes(SelectedDocLibId);
            LoanAgreements = outputService.GetLoanAgreements();

            // Rebind selected loan to the fresh instance (by Id)
            if (SelectedLoanAgreement != null)
            {
                var id = SelectedLoanAgreement.Id;
                SelectedLoanAgreement = LoanAgreements.FirstOrDefault(l => l.Id == id) ?? SelectedLoanAgreement;
            }

            // If we have a loan selected but no LoanType selected, default to the loan's stored type.
            await EnsureLoanTypeSelectionFromLoanAsync().ConfigureAwait(false);

            Status = $"Loaded: {Documents.Count} docs, {LoanTypes.Count} loan types. Agreements: {LoanAgreements.Count}.";
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "ReloadAsync failed");
            Status = "Reload failed. Check logs.";
        }
    }

    /// <summary>
    /// Called by the UI when the user picks a different LoanAgreement.
    /// Defaults LoanType to what is stored on the loan and refreshes persisted results.
    /// </summary>
    public async Task OnLoanAgreementChangedAsync()
    {
        try
        {
            await EnsureLoanTypeSelectionFromLoanAsync().ConfigureAwait(false);

            // Reasonable UX defaults
            if (SelectedLoanAgreement != null)
            {
                if (string.IsNullOrWhiteSpace(EmailTo))
                    EmailTo = SelectedLoanAgreement.EmailTo ?? "";

                if (SelectedLoanAgreement.OutputType != default)
                    OutputType = SelectedLoanAgreement.OutputType;
            }

            await RefreshSelectedLoanAsync().ConfigureAwait(false);

            Status = "Loan selected.";
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "OnLoanAgreementChangedAsync failed");
            Status = "Loan select failed. Check logs.";
        }
    }

    /// <summary>
    /// Called by the UI when LoanType changes.
    /// Automatically queues ThenGenerate (evaluation only) so results update without a button click.
    /// </summary>
    public async Task OnLoanTypeChangedAsync()
    {
        if (SelectedLoanAgreement is null || SelectedLoanType is null)
            return;

        await QueueThenGeneratePipelineAutoAsync().ConfigureAwait(false);
    }

    /// <summary>
    /// Reload ONLY the selected loan agreement from DB (via GetLoanAgreements) so the Admin Bench can
    /// show persisted ThenGenerate results without needing document content.
    /// </summary>
    public Task RefreshSelectedLoanAsync()
    {
        try
        {
            if (SelectedLoanAgreement is null)
                return Task.CompletedTask;

            var id = SelectedLoanAgreement.Id;

            LoanAgreements = outputService.GetLoanAgreements();

            var fresh = LoanAgreements.FirstOrDefault(l => l.Id == id);
            if (fresh != null)
            {
                SelectedLoanAgreement = fresh;
                Status = $"Refreshed loan. Deliveries: {fresh.DocumentDeliverys?.Count ?? 0}.";
            }
            else
            {
                Status = "Refresh did not find selected loan. Check filters / DB.";
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "RefreshSelectedLoanAsync failed");
            Status = "Refresh failed. Check logs.";
        }

        return Task.CompletedTask;
    }

    public void Clear()
    {
        SelectedLoanAgreement = null;
        SelectedLoanType = null;
        Status = null;
    }

    /// <summary>
    /// Auto pipeline: persist loan context + enqueue LoanJob.
    /// We force AdminBench.Enabled=true and AdminBench.SuppressMerge=true so the worker ONLY evaluates
    /// ThenGenerate, persists DocumentDeliverys + Trace, and stops before merging.
    /// </summary>
    private async Task QueueThenGeneratePipelineAutoAsync()
    {
        IsBusy = true;
        try
        {
            Status = "Queueing ThenGenerate pipeline (auto)…";

            // Fresh loan instance to avoid stale persisted deliveries confusing the UI
            var loan = outputService.GetLoanAgreements()
                .FirstOrDefault(l => l.Id == SelectedLoanAgreement!.Id);

            if (loan is null)
            {
                Status = "Loan not found.";
                return;
            }

            // Keep UI selection in sync
            var loanType = SelectedLoanType!;
            loan.LoanTypeId = loanType.Id;
            loan.LoanTypeName = loanType.Name;

            // Keep output + email in sync
            SetIfExists(loan, "OutputType", OutputType);
            SetIfExists(loan, "EmailTo", EmailTo);

            // Configure AdminBench flags WITHOUT compile-time dependency on the AdminBenchConfig type.
            EnsureAdminBenchConfig(loan);
            SetNestedIfExists(loan, "AdminBench", "Enabled", true);
            SetNestedIfExists(loan, "AdminBench", "SuppressMerge", true);
            SetNestedIfExists(loan, "AdminBench", "OutputTypeOverride", OutputType);
            SetNestedIfExists(loan, "AdminBench", "EmailToOverride", EmailTo);

            // Helpful when reading logs
            SetNestedIfExists(loan, "AdminBench", "LastRunReason", $"Auto run for LoanType '{loanType.Name}'");

            await loanQueue.EnqueueAsync(new LoanJob(loan), CancellationToken.None).ConfigureAwait(false);

            // Poll refresh so the UI updates itself without the user smashing buttons like a lab rat.
            await PollRefreshResultsAsync(expectedMinCount: 0).ConfigureAwait(false);

            Status = "Auto ThenGenerate queued. Results refreshed.";
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "QueueThenGeneratePipelineAutoAsync failed");
            Status = "Auto queue failed. Check logs.";
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task PollRefreshResultsAsync(int expectedMinCount)
    {
        // Workers run async. We poll a few times so the Admin Bench looks "live".
        const int attempts = 12;
        const int delayMs = 250;

        for (int i = 0; i < attempts; i++)
        {
            await Task.Delay(delayMs).ConfigureAwait(false);
            await RefreshSelectedLoanAsync().ConfigureAwait(false);

            var count = SelectedLoanAgreement?.DocumentDeliverys?.Count ?? 0;
            if (count >= expectedMinCount)
                return;
        }
    }

    /// <summary>
    /// Debug-only: queue merges from the persisted ThenGenerate delivery list.
    /// This does NOT rerun rules. It consumes the saved decisions.
    /// </summary>
    public async Task QueueMergeFromDeliveriesAsync()
    {
        if (!CanMergeFromDeliveries) return;

        IsBusy = true;
        try
        {
            await RefreshSelectedLoanAsync().ConfigureAwait(false);

            var loan = SelectedLoanAgreement!;
            var deliveries = loan.DocumentDeliverys ?? new List<DocumentDelivery>();

            // Refresh docs list for the doc library (so name lookup works)
            Documents = outputService.GetDocuments(SelectedDocLibId);
            var docsById = Documents.GroupBy(d => d.Id).ToDictionary(g => g.Key, g => g.First());

            var queued = 0;
            foreach (var d in deliveries)
            {
                if (!docsById.TryGetValue(d.DocId, out var doc))
                    continue;

                doc.OutputType = d.OutputType;

                var merge = new DocumentMerge
                {
                    LoanAgreement = loan,
                    Document = doc,
                    Status = DocumentMergeState.Status.Queued
                };

                SetIfExists(merge, "EmailTo", d.DeliveryLoaction ?? EmailTo);
                SetIfExists(merge, "EmailSubject", $"Bench Merge From Deliveries ({doc.Name})");
                SetIfExists(merge, "Subject", $"Bench Merge From Deliveries ({doc.Name})");
                SetIfExists(merge, "OutputType", d.OutputType);

                await mergeQueue.EnqueueAsync(new MergeJob(merge), CancellationToken.None).ConfigureAwait(false);
                queued++;
            }

            Status = $"Queued {queued} merge job(s) from persisted deliveries.";
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "QueueMergeFromDeliveriesAsync failed");
            Status = "Queue merge-from-deliveries failed. Check logs.";
        }
        finally
        {
            IsBusy = false;
        }
    }

    public string GetLoanLabel(LoanAgreement loan) => outputService.GetLoanLabel(loan);

    public string GetDocName(Guid docId)
        => Documents.FirstOrDefault(d => d.Id == docId)?.Name ?? docId.ToString();

    private Task EnsureLoanTypeSelectionFromLoanAsync()
    {
        if (SelectedLoanAgreement is null)
            return Task.CompletedTask;

        // If the loan has a stored LoanTypeId, default to it
        var storedId = SelectedLoanAgreement.LoanTypeId;

        if (storedId != Guid.Empty)
        {
            SelectedLoanType = LoanTypes.FirstOrDefault(x => x.Id == storedId) ?? SelectedLoanType;
        }

        // If still null, pick the first available type as a sane default
        if (SelectedLoanType is null && LoanTypes.Count > 0)
            SelectedLoanType = LoanTypes[0];

        return Task.CompletedTask;
    }

    // =========================
    // Reflection helpers (keep UI decoupled from model churn)
    // =========================

    private static void EnsureAdminBenchConfig(LoanAgreement loan)
    {
        try
        {
            var prop = loan.GetType().GetProperty("AdminBench",
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.IgnoreCase);

            if (prop is null || !prop.CanWrite)
                return;

            var existing = prop.GetValue(loan);
            if (existing != null)
                return;

            var propType = prop.PropertyType;
            if (propType.IsInterface || propType.IsAbstract)
                return;

            var created = Activator.CreateInstance(propType);
            prop.SetValue(loan, created);
        }
        catch
        {
            // optional: don't fail the bench if this can't be created
        }
    }

    private static void SetNestedIfExists(object target, string parentProperty, string childProperty, object? value)
    {
        var parent = GetPropertyValue(target, parentProperty);
        if (parent is null)
            return;

        SetIfExists(parent, childProperty, value);
    }

    private static object? GetPropertyValue(object target, string propertyName)
    {
        try
        {
            var prop = target.GetType().GetProperty(propertyName,
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.IgnoreCase);

            return prop?.GetValue(target);
        }
        catch { return null; }
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

            if (propType == typeof(Guid) && value is Guid g)
            {
                prop.SetValue(target, g);
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
            // intentionally swallow: optional metadata, not critical path
        }
    }
}

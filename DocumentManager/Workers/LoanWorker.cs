using DocumentManager.Infrastructure;
using DocumentManager.Jobs;
using DocumentManager.Services;
using DocumentManager.State;
using DominateDocsData.Database;
using DominateDocsData.Enums;
using DominateDocsData.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Linq;

namespace DocumentManager.Workers;

public sealed class LoanWorker : WorkerPoolBackgroundService<LoanJob>
{
    private readonly ILogger<LoanWorker> logger;
    private readonly IOptions<DocumentManagerConfigOptions> options;
    private readonly IDocumentManagerState docState;
    private readonly IJobQueue<MergeJob> mergeQueue;
    private readonly IMongoDatabaseRepo dbApp;
    private readonly IServiceScopeFactory scopeFactory;

    public LoanWorker(
        IJobQueue<LoanJob> queue,
        IJobQueue<MergeJob> mergeQueue,
        ILogger<LoanWorker> logger,
        IOptions<DocumentManagerConfigOptions> options,
        IDocumentManagerState docState,
        IMongoDatabaseRepo dbApp,
        IServiceScopeFactory scopeFactory)
        : base(queue, logger, options.Value.MaxLoanApplicationThreads)
    {
        this.logger = logger;
        this.options = options;
        this.docState = docState;
        this.mergeQueue = mergeQueue;
        this.dbApp = dbApp;
        this.scopeFactory = scopeFactory;
    }

    public override Task StartAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("✅ LoanWorker STARTED (MaxThreads={MaxThreads})", options.Value.MaxLoanApplicationThreads);
        return base.StartAsync(cancellationToken);
    }

    protected override async Task HandleAsync(LoanJob job, CancellationToken ct)
    {
        if (!options.Value.IsActive || !docState.IsRunBackgroundLoanApplicationService)
            return;

        if (job?.Loan is null)
        {
            logger.LogWarning("LoanWorker got a null LoanJob.Loan");
            return;
        }

        logger.LogInformation("📥 LoanWorker got LoanJob for LoanId={LoanId}", job.Loan.Id);

        await ProcessLoanAsync(job.Loan, ct).ConfigureAwait(false);
    }

    private async Task ProcessLoanAsync(LoanAgreement loan, CancellationToken ct)
    {
        LoanAgreement? loanState = null;

        try
        {
            docState.LoanList.TryAdd(loan.Id, loan);

            docState.StateHasChanged();

            // Precompute template-friendly strings (keep your existing behavior)
            loan.LenderNames = await GetLenderNamesAsync(loan).ConfigureAwait(false);
            loan.BorrowerNames = await GetBorrowerNamesAsync(loan).ConfigureAwait(false);
            loan.BrokerNames = await GetBrokerNamesAsync(loan).ConfigureAwait(false);

            loanState = loan;

            // Run then-generate evaluation and persist deliveries
            var docs = await EvaluateThenGenerateDocsAsync(loan, ct).ConfigureAwait(false);

            // Persisted deliveries / merging logic continues (keep your current flow)
            await SaveDeliveriesAsync(loan, docs, ct).ConfigureAwait(false);

            // Queue merge work if you do that here
            await QueueMergeFromDeliveriesAsync(loan, ct).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "LoanWorker: exception processing LoanId={LoanId}", loan.Id);
        }
        finally
        {
            docState.StateHasChanged();
        }
    }

    private Task<IReadOnlyList<Document>> EvaluateThenGenerateDocsAsync(LoanAgreement loan, CancellationToken ct)
    {
        LoanType? loanType = null;

        try
        {
            loanType = dbApp.GetRecordById<LoanType>(loan.LoanTypeId);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "ThenGenerate(DB): failed to load LoanTypeId={LoanTypeId}", loan.LoanTypeId);
        }

        if (loanType is null)
        {
            logger.LogWarning("ThenGenerate(DB): LoanType could not be loaded for LoanTypeId={LoanTypeId}", loan.LoanTypeId);
            return Task.FromResult<IReadOnlyList<Document>>(Array.Empty<Document>());
        }

        var data = BuildRuleDataBag(loan);

        IReadOnlyList<Guid> docIds;

        if (loan.AdminBench?.Enabled == true)
        {
            docIds = DocumentOutputEvaluator.BuildFinalDocumentIdsWithTrace(loanType, data, out var trace);

            // Make it painfully obvious what run this trace belongs to.
            loan.AdminBench.Trace.Clear();
            loan.AdminBench.Trace.Add($"=== LoanWorker Context @ {DateTime.UtcNow:O} UTC ===");
            loan.AdminBench.Trace.Add($"LoanId={loan.Id} | LoanTypeId={loan.LoanTypeId} | DocLibId={loan.DocLibId}");
            loan.AdminBench.Trace.Add($"DataSnapshot: LenderState={(data.TryGetValue("LenderState", out var ls) ? (ls?.ToString() ?? "<null>") : "<missing>")} | BorrowerState={(data.TryGetValue("BorrowerState", out var bs) ? (bs?.ToString() ?? "<null>") : "<missing>")} | BrokerState={(data.TryGetValue("BrokerState", out var brs) ? (brs?.ToString() ?? "<null>") : "<missing>")}");
            loan.AdminBench.Trace.Add($"DataKeys: {string.Join(", ", data.Keys.OrderBy(k => k))}");
            loan.AdminBench.Trace.Add("");
            loan.AdminBench.Trace.AddRange(trace);
        }
        else
        {
            docIds = DocumentOutputEvaluator.BuildFinalDocumentIds(loanType, data);
        }

        logger.LogInformation("ThenGenerate(DB): LoanId={LoanId} LoanTypeId={LoanTypeId} -> {Count} doc id(s)",
            loan.Id, loan.LoanTypeId, docIds.Count);

        if (docIds.Count == 0)
            return Task.FromResult<IReadOnlyList<Document>>(Array.Empty<Document>());

        var docs = new List<Document>(docIds.Count);

        foreach (var id in docIds)
        {
            try
            {
                var d = dbApp.GetRecordById<Document>(id);
                if (d != null) docs.Add(d);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "ThenGenerate(DB): failed to load DocumentId={DocumentId}", id);
            }
        }

        return Task.FromResult<IReadOnlyList<Document>>(docs);
    }

    // -------------------------
    // Existing helpers (your file already has these; keep them as-is below)
    // -------------------------

    private Dictionary<string, object?> BuildRuleDataBag(LoanAgreement loan)
    {
        // Your existing implementation remains exactly as in your current file.
        // (Not rewritten here since your bug is evaluation/trace, not bag building.)
        var data = new Dictionary<string, object?>();

        // ... your existing population logic ...

        return data;
    }

    private Task<string> GetLenderNamesAsync(LoanAgreement loan) => Task.FromResult(loan.LenderNames ?? "");
    private Task<string> GetBorrowerNamesAsync(LoanAgreement loan) => Task.FromResult(loan.BorrowerNames ?? "");
    private Task<string> GetBrokerNamesAsync(LoanAgreement loan) => Task.FromResult(loan.BrokerNames ?? "");

    private Task SaveDeliveriesAsync(LoanAgreement loan, IReadOnlyList<Document> docs, CancellationToken ct)
        => Task.CompletedTask;

    private Task QueueMergeFromDeliveriesAsync(LoanAgreement loan, CancellationToken ct)
        => Task.CompletedTask;
}

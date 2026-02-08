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
using System.Reflection;

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
        try
        {
            docState.LoanList.TryAdd(loan.Id, loan);
            docState.StateHasChanged();

            // Precompute template-friendly strings (keep your existing behavior)
            loan.LenderNames = await GetLenderNamesAsync(loan).ConfigureAwait(false);
            loan.BorrowerNames = await GetBorrowerNamesAsync(loan).ConfigureAwait(false);
            loan.BrokerNames = await GetBrokerNamesAsync(loan).ConfigureAwait(false);

            // Run then-generate evaluation and produce the Document list
            var docs = await EvaluateThenGenerateDocsAsync(loan, ct).ConfigureAwait(false);

            // ✅ Build persisted delivery plan (includes Copies + OutputType per document)
            await SaveDeliveriesAsync(loan, docs, ct).ConfigureAwait(false);

            // ✅ Queue merges from the persisted delivery plan unless suppressed
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

            loan.AdminBench.Trace.Clear();
            loan.AdminBench.Trace.Add($"=== LoanWorker Context @ {DateTime.UtcNow:O} UTC ===");
            loan.AdminBench.Trace.Add($"LoanId={loan.Id} | LoanTypeId={loan.LoanTypeId} | DocLibId={loan.DocLibId}");
            loan.AdminBench.Trace.Add($"DataSnapshot: " +
                                     $"LenderState={(data.TryGetValue("LenderState", out var ls) ? (ls?.ToString() ?? "<null>") : "<missing>")} | " +
                                     $"BorrowerState={(data.TryGetValue("BorrowerState", out var bs) ? (bs?.ToString() ?? "<null>") : "<missing>")} | " +
                                     $"BrokerState={(data.TryGetValue("BrokerState", out var brs) ? (brs?.ToString() ?? "<null>") : "<missing>")}");
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

        // ✅ NEW: Resolve documents from DocumentLibrary.Documents (NOT the old top-level Documents collection)
        var docPool = GetAllLibraryDocuments();
        var docsById = docPool.ToDictionary(d => d.Id, d => d);

        var resolved = new List<Document>(docIds.Count);

        foreach (var id in docIds)
        {
            if (docsById.TryGetValue(id, out var doc))
            {
                resolved.Add(doc);
            }
            else
            {
                logger.LogWarning("ThenGenerate(DB): DocumentId={DocumentId} not found in DocumentLibrary.Documents (old Documents collection likely still referenced elsewhere)",
                    id);
            }
        }

        return Task.FromResult<IReadOnlyList<Document>>(resolved);
    }

    // -------------------------
    // Existing helpers (your file already has these; keep them as-is below)
    // -------------------------

    public Dictionary<string, object?> BuildEvalData(DominateDocsData.Models.LoanAgreement loan)
    {
        // Keep this intentionally dumb/simple: add keys as your rules expand.
        // This is the rule "key bag". If the UI lets you pick a field name,
        // then this method is the contract that provides it.
        var data = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);


        //State
        var lenderState = TryGetNestedString(loan, "Lenders", 0, "State")
                       ?? TryGetNestedString(loan, "Lender", "State")
                       ?? TryGetNestedString(loan, "LenderState");

        var brokerState = TryGetNestedString(loan, "Brokers", 0, "State")
                      ?? TryGetNestedString(loan, "Broker", "State")
                      ?? TryGetNestedString(loan, "BrokerState");

        var borrowerState = TryGetNestedString(loan, "Borrowers", 0, "State")
                        ?? TryGetNestedString(loan, "Borrower", "State")
                        ?? TryGetNestedString(loan, "BorrowerState");

        var propertyState = TryGetNestedString(loan, "Properties", 0, "State")
                       ?? TryGetNestedString(loan, "PropertyRecord", "State")
                       ?? TryGetNestedString(loan, "PropertyState");


        //Codes
        var lenderCode = TryGetNestedString(loan, "Lenders", 0, "Code")
                     ?? TryGetNestedString(loan, "Lender", "Code")
                     ?? TryGetNestedString(loan, "LenderCode");

        var brokerCode = TryGetNestedString(loan, "Brokers", 0, "Code")
                     ?? TryGetNestedString(loan, "Broker", "Code")
                     ?? TryGetNestedString(loan, "BrokerCode");


        return data;

        //static string? GetState(object? party)
        //{
        //    if (party is null) return null;

        //    // Try common names across your models (you change these, because humans)
        //    var raw =
        //        GetString(party, "State") ??
        //        GetString(party, "PreferredStateVenue") ??
        //        GetString(party, "StateOfIncorporation");

        //    return string.IsNullOrWhiteSpace(raw) ? null : raw.Trim();
        //}

        //static string? GetString(object? obj, string propName)
        //{
        //    if (obj is null) return null;
        //    try
        //    {
        //        var pi = obj.GetType().GetProperty(propName, System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
        //        return pi?.GetValue(obj)?.ToString();
        //    }
        //    catch { return null; }
        //}
    }

    private static IReadOnlyDictionary<string, object?> BuildRuleDataBag(LoanAgreement loan)
    {
        var data = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
        {
            ["LenderCode"] = loan.LenderCode,
            ["BrokerCode"] = loan.BrokerCode,
            ["BorrowerCode"] = loan.BorrowerCode,
            ["PropertyState"] = loan.PropertyState,
            ["LoanTypeName"] = loan.LoanTypeName,

            // Handy identifiers (safe to include; many rules ignore them).
            ["LoanId"] = loan.Id,
            ["LoanTypeId"] = loan.LoanTypeId,
            ["DocLibId"] = loan.DocLibId
        };

        // =========================
        // Party State Extraction
        // =========================
        // Your LoanType rule uses: Field=LenderState
        // If this key is missing, IF conditions like "LenderState IN CA,TN" will always fail.
        var lenderState =
            TryGetNestedString(loan, "Lenders", 0, "State") ??
            TryGetNestedString(loan, "Lenders", 0, "StateChoiceOfLaw") ??
            TryGetNestedString(loan, "Lenders", 0, "StateCode") ??
            TryGetNestedString(loan, "Lender", "State") ??
            TryGetNestedString(loan, "Lender", "StateChoiceOfLaw") ??
            TryGetNestedString(loan, "Lender", "StateCode") ??
            TryGetNestedString(loan, "Lenders", 0, "Address", "State") ??
            TryGetNestedString(loan, "Lenders", 0, "MailingAddress", "State") ??
            TryGetNestedString(loan, "Lenders", 0, "MailingAddress", "State") ??
            TryGetNestedString(loan, "Lenders", 0, "PhysicalAddress", "State") ??
            TryGetNestedString(loan, "Lender", "Address", "State") ??
            TryGetNestedString(loan, "Lender", "MailingAddress", "State") ??
            TryGetNestedString(loan, "Lender", "PhysicalAddress", "State") ??
            TryGetNestedString(loan, "Lenders", 0, "StateLendingLicenses", 0, "State") ??
            TryGetNestedString(loan, "Lenders", 0, "LendingLicenses", 0, "State") ??
            TryGetNestedString(loan, "Lenders", 0, "Licenses", 0, "State") ??
            TryGetNestedString(loan, "Lender", "StateLendingLicenses", 0, "State") ??
            TryGetNestedString(loan, "Lender", "LendingLicenses", 0, "State") ??
            TryGetNestedString(loan, "Lender", "Licenses", 0, "State");

        if (!string.IsNullOrWhiteSpace(lenderState))
            data["LenderState"] = lenderState!.Trim();

        var borrowerState =
            TryGetNestedString(loan, "Borrowers", 0, "State") ??
            TryGetNestedString(loan, "Borrowers", 0, "StateChoiceOfLaw") ??
            TryGetNestedString(loan, "Borrowers", 0, "StateCode") ??
            TryGetNestedString(loan, "Borrower", "State") ??
            TryGetNestedString(loan, "Borrower", "StateChoiceOfLaw") ??
            TryGetNestedString(loan, "Borrower", "StateCode") ??
            TryGetNestedString(loan, "Borrowers", 0, "Address", "State") ??
            TryGetNestedString(loan, "Borrowers", 0, "MailingAddress", "State") ??
            TryGetNestedString(loan, "Borrowers", 0, "PhysicalAddress", "State") ??
            TryGetNestedString(loan, "Borrower", "Address", "State") ??
            TryGetNestedString(loan, "Borrower", "MailingAddress", "State") ??
            TryGetNestedString(loan, "Borrower", "PhysicalAddress", "State");

        if (!string.IsNullOrWhiteSpace(borrowerState))
            data["BorrowerState"] = borrowerState!.Trim();

        var brokerState =
            TryGetNestedString(loan, "Brokers", 0, "State") ??
            TryGetNestedString(loan, "Brokers", 0, "StateChoiceOfLaw") ??
            TryGetNestedString(loan, "Brokers", 0, "StateCode") ??
            TryGetNestedString(loan, "Broker", "State") ??
            TryGetNestedString(loan, "Broker", "StateChoiceOfLaw") ??
            TryGetNestedString(loan, "Broker", "StateCode") ??
            TryGetNestedString(loan, "Brokers", 0, "Address", "State") ??
            TryGetNestedString(loan, "Brokers", 0, "MailingAddress", "State") ??
            TryGetNestedString(loan, "Brokers", 0, "PhysicalAddress", "State") ??
            TryGetNestedString(loan, "Broker", "Address", "State") ??
            TryGetNestedString(loan, "Broker", "MailingAddress", "State") ??
            TryGetNestedString(loan, "Broker", "PhysicalAddress", "State");

        if (!string.IsNullOrWhiteSpace(brokerState))
            data["BrokerState"] = brokerState!.Trim();

        // Allow Admin Bench overrides to win last.
        if (loan.AdminBench?.KeyOverrides is not null)
        {
            foreach (var kvp in loan.AdminBench.KeyOverrides)
                data[kvp.Key] = kvp.Value;
        }

        // Remove nulls (no LINQ dependency surprises).
        if (data.Count > 0)
        {
            var keysToRemove = new System.Collections.Generic.List<string>();
            foreach (var kvp in data)
            {
                if (kvp.Value is null)
                    keysToRemove.Add(kvp.Key);
            }
            foreach (var k in keysToRemove)
                data.Remove(k);
        }

        return data;
    }

    private Task<string> GetLenderNamesAsync(LoanAgreement loan) => Task.FromResult(loan.LenderNames ?? "");
    private Task<string> GetBorrowerNamesAsync(LoanAgreement loan) => Task.FromResult(loan.BorrowerNames ?? "");
    private Task<string> GetBrokerNamesAsync(LoanAgreement loan) => Task.FromResult(loan.BrokerNames ?? "");

    private static string? TryGetNestedString(object root, params object[] path)
    {
        try
        {
            object? cur = root;

            foreach (var seg in path)
            {
                if (cur == null) return null;

                if (seg is string propName)
                {
                    var pi = cur.GetType().GetProperty(propName, BindingFlags.Public | BindingFlags.Instance);
                    cur = pi?.GetValue(cur);
                }
                else if (seg is int index)
                {
                    if (cur is System.Collections.IList list && list.Count > index)
                        cur = list[index];
                    else
                        return null;
                }
            }

            return cur?.ToString();
        }
        catch { return null; }
    }

    /// <summary>
    /// Creates the persisted delivery plan on the LoanAgreement.
    /// This is what Admin Bench shows, and what Merge queue uses.
    /// </summary>
    private async Task SaveDeliveriesAsync(
    LoanAgreement loan,
    IReadOnlyList<Document> docs,
    CancellationToken ct)
    {
        loan.DocumentDeliverys ??= new List<DocumentDelivery>();
        loan.DocumentDeliverys.Clear();

        var forcedOutput =
            loan.AdminBench?.Enabled == true
                ? loan.AdminBench.OutputTypeOverride
                : (DocumentTypes.OutputTypes?)null;

        foreach (var doc in docs)
        {
            loan.DocumentDeliverys.Add(new DocumentDelivery
            {
                DocId = doc.Id,
                OutputType = forcedOutput ?? doc.OutputType,
                Copies = doc.Copies <= 0 ? 1 : doc.Copies,
                DelieveryTypes = DocumentTypes.DelieveryTypes.Email,
                DeliveryLoaction = string.Empty
            });
        }

        // ============================
        // 🔑 THIS WAS MISSING
        // Persist the updated loan
        // ============================
        await dbApp.UpSertRecordAsync(loan).ConfigureAwait(false);

        logger.LogInformation(
            "✅ Deliveries persisted. LoanId={LoanId} DeliveryCount={Count}",
            loan.Id,
            loan.DocumentDeliverys.Count);
    }


    /// <summary>
    /// Queues merge jobs based on the persisted delivery plan.
    /// </summary>
    private async Task QueueMergeFromDeliveriesAsync(LoanAgreement loan, CancellationToken ct)
    {
        if (loan.AdminBench?.Enabled == true && loan.AdminBench.SuppressMerge)
        {
            logger.LogInformation("⛔ Merge suppressed by AdminBench for LoanId={LoanId}", loan.Id);
            return;
        }

        if (loan.DocumentDeliverys.Count == 0)
        {
            logger.LogInformation("QueueMergeFromDeliveriesAsync: No deliveries to merge for LoanId={LoanId}", loan.Id);
            return;
        }

        var queued = 0;

        foreach (var delivery in loan.DocumentDeliverys)
        {
            var doc = TryResolveLibraryDocumentById(delivery.DocId);

            if (doc is null)
            {
                logger.LogWarning("QueueMergeFromDeliveriesAsync: DocumentId={DocId} not found in DocumentLibrary.Documents", delivery.DocId);
                continue;
            }

            // Delivery output type controls what is produced for THIS document.
            // IMPORTANT: do NOT mutate the library metadata document instance.
            var mergeDoc = CloneDocument(doc);
            mergeDoc.OutputType = delivery.OutputType;

            var merge = new DocumentMerge
            {
                LoanAgreement = loan,
                Document = mergeDoc,
                Status = DocumentMergeState.Status.Queued
            };

            await mergeQueue.EnqueueAsync(new MergeJob(merge), ct).ConfigureAwait(false);
            queued++;
        }

        logger.LogInformation("✅ QueueMergeFromDeliveriesAsync: LoanId={LoanId} MergeJobsQueued={Count}",
            loan.Id, queued);
    }

    // -------------------------
    // NEW: Library-based resolution helpers
    // -------------------------

    private List<Document> GetAllLibraryDocuments()
    {
        try
        {
            var libs = dbApp.GetRecords<DocumentLibrary>().ToList();
            return libs
                .Where(l => l is not null)
                .SelectMany(l => l.Documents ?? new List<Document>())
                .ToList();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "LoanWorker: failed to load DocumentLibrary records");
            return new List<Document>();
        }
    }

    private Document? TryResolveLibraryDocumentById(Guid docId)
    {
        if (docId == Guid.Empty) return null;

        var docs = GetAllLibraryDocuments();
        return docs.FirstOrDefault(d => d.Id == docId);
    }

    private static Document CloneDocument(Document source)
    {
        // Shallow clone, enough to prevent OutputType (and other per-merge overrides) from mutating library metadata.
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

            GenerateMultipleFor = source.GenerateMultipleFor is null ? new List<DocumentTypes.GenerateMultipleFor>() : new List<DocumentTypes.GenerateMultipleFor>(source.GenerateMultipleFor),
            OutputType = source.OutputType,
            Copies = source.Copies
        };
    }
}

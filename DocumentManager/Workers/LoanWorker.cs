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
using System.Collections.Generic;
using System.Linq;
using System.Text;

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

            // =========================================================
            // ✅ AdminBench overrides MUST be copied onto the Loan itself
            // so MergeWorker/EmailWorker sees EmailTo + OutputType.
            // =========================================================
            if (loan.AdminBench?.Enabled == true)
            {
                if (loan.AdminBench.OutputTypeOverride.HasValue)
                    loan.OutputType = loan.AdminBench.OutputTypeOverride.Value;

                if (!string.IsNullOrWhiteSpace(loan.AdminBench.EmailToOverride))
                    loan.EmailTo = loan.AdminBench.EmailToOverride;
            }

            // =========================================================
            // ✅ Build formatted name fields (Loan + Party records)
            // =========================================================
            PopulateLoanAndPartyFormattedFields(loan);

            // Persist formatted fields + AdminBench overrides so RazorLite merge always sees them.
            await dbApp.UpSertRecordAsync(loan).ConfigureAwait(false);

            // ThenGenerate -> Documents
            var docs = await EvaluateThenGenerateDocsAsync(loan, ct).ConfigureAwait(false);

            // Persist delivery plan (also persists the loan)
            await SaveDeliveriesAsync(loan, docs, ct).ConfigureAwait(false);

            // AdminBench owns merge queueing when SuppressMerge is true
            if (loan.AdminBench?.Enabled == true && loan.AdminBench.SuppressMerge)
            {
                logger.LogInformation("LoanWorker: AdminBench.SuppressMerge=true, skipping merge enqueue. LoanId={LoanId}", loan.Id);
                return;
            }

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

    // ==========================================================
    // FORMATTING (NO REFLECTION)
    // ==========================================================

    private static void PopulateLoanAndPartyFormattedFields(LoanAgreement loan)
    {
        // Lenders
        if (loan.Lenders is not null && loan.Lenders.Count > 0)
        {
            foreach (var l in loan.Lenders)
            {
                l.FormattedName = BuildLenderFormattedName(l);
            }

            loan.LenderNames = BuildLenderNames(loan.Lenders);
        }
        else
        {
            loan.LenderNames = string.Empty;
        }

        // Borrowers
        if (loan.Borrowers is not null && loan.Borrowers.Count > 0)
        {
            foreach (var b in loan.Borrowers)
            {
                b.FormattedName = BuildPartyFormattedName(b);
                b.SigningAuthoritiesFormatted = BuildSigningAuthorities(b.SigningAuthorities);
                b.AliasNamesFormatted = BuildAliasNames(b.AliasNames);
                b.EntityOwnersFormatted = BuildEntityOwners(b.EntityOwners);
                // SignatureLinesFormatted intentionally not generated (you said you do this in-doc)
            }

            loan.BorrowerNames = BuildPartyNames(loan.Borrowers);
        }
        else
        {
            loan.BorrowerNames = string.Empty;
        }

        // Brokers
        if (loan.Brokers is not null && loan.Brokers.Count > 0)
        {
            foreach (var b in loan.Brokers)
            {
                b.FormattedName = BuildPartyFormattedName(b);
                b.SigningAuthoritiesFormatted = BuildSigningAuthorities(b.SigningAuthorities);
                b.AliasNamesFormatted = BuildAliasNames(b.AliasNames);
                b.EntityOwnersFormatted = BuildEntityOwners(b.EntityOwners);
            }

            loan.BrokerNames = BuildPartyNames(loan.Brokers);
        }
        else
        {
            loan.BrokerNames = string.Empty;
        }

        // Guarantors
        if (loan.Guarantors is not null && loan.Guarantors.Count > 0)
        {
            foreach (var g in loan.Guarantors)
            {
                g.FormattedName = BuildPartyFormattedName(g);
                g.SigningAuthoritiesFormatted = BuildSigningAuthorities(g.SigningAuthorities);
                g.AliasNamesFormatted = BuildAliasNames(g.AliasNames);
                g.EntityOwnersFormatted = BuildEntityOwners(g.EntityOwners);
            }

            loan.GuarantorNames = BuildPartyNames(loan.Guarantors);
        }
        else
        {
            loan.GuarantorNames = string.Empty;
        }

        // Properties
        if (loan.Properties is not null && loan.Properties.Count > 0)
        {
            foreach (var p in loan.Properties)
            {
                // Property owners are parties too
                if (p.PropertyOwners is not null && p.PropertyOwners.Count > 0)
                {
                    foreach (var owner in p.PropertyOwners)
                    {
                        owner.FormattedName = BuildPartyFormattedName(owner);
                        owner.SigningAuthoritiesFormatted = BuildSigningAuthorities(owner.SigningAuthorities);
                        owner.AliasNamesFormatted = BuildAliasNames(owner.AliasNames);
                        owner.EntityOwnersFormatted = BuildEntityOwners(owner.EntityOwners);
                    }

                    p.PropertyOwnersFormatted = BuildPartyNames(p.PropertyOwners);
                }
                else
                {
                    p.PropertyOwnersFormatted = string.Empty;
                }

                p.EntityOwnersFormatted = BuildEntityOwners(p.EntityOwners);
            }

            loan.PropertyAddresses = BuildPropertyAddresses(loan.Properties);
        }
        else
        {
            loan.PropertyAddresses = string.Empty;
        }
    }

    private static string BuildPartyFormattedName(IPartyNames p)
    {
        var isIndividual = p.EntityType == Entity.Types.Individual;

        if (isIndividual)
            return $"{p.EntityName} a {p.EntityType}".Trim();

        return $"{p.EntityName} a {p.StateOfIncorporationDescription} {p.EntityStructureDescription}".Trim();
    }

    private static string BuildLenderFormattedName(Lender p)
    {
        var baseLine = BuildPartyFormattedName(p);

        if (!string.IsNullOrWhiteSpace(p.NmlsLicenseNumber))
            return $"{baseLine} (CFL License No.{p.NmlsLicenseNumber})";

        return baseLine;
    }

    private static string BuildPartyNames<T>(IEnumerable<T> parties) where T : IPartyNames
    {
        if (parties is null) return string.Empty;

        var sb = new StringBuilder();
        var first = true;

        foreach (var p in parties)
        {
            var line = BuildPartyFormattedName(p);

            if (string.IsNullOrWhiteSpace(line))
                continue;

            if (first)
            {
                sb.AppendLine(line);
                first = false;
            }
            else
            {
                sb.AppendLine($", {line}");
            }
        }

        return sb.ToString();
    }

    private static string BuildLenderNames(IEnumerable<Lender> lenders)
    {
        if (lenders is null) return string.Empty;

        var sb = new StringBuilder();
        var first = true;

        foreach (var l in lenders)
        {
            var line = BuildLenderFormattedName(l);

            if (string.IsNullOrWhiteSpace(line))
                continue;

            if (first)
            {
                sb.AppendLine(line);
                first = false;
            }
            else
            {
                sb.AppendLine($", {line}");
            }
        }

        return sb.ToString();
    }

    private static string BuildPropertyAddresses(IEnumerable<PropertyRecord> properties)
    {
        if (properties is null) return string.Empty;

        var sb = new StringBuilder();
        var first = true;

        foreach (var p in properties)
        {
            if (string.IsNullOrWhiteSpace(p.FullAddress))
                continue;

            if (first)
            {
                sb.AppendLine(p.FullAddress);
                first = false;
            }
            else
            {
                sb.AppendLine($", {p.FullAddress}");
            }
        }

        return sb.ToString();
    }

    private static string BuildSigningAuthorities(IEnumerable<SigningAuthority> parties)
    {
        if (parties is null) return string.Empty;

        var sb = new StringBuilder();
        var first = true;

        foreach (var p in parties)
        {
            if (string.IsNullOrWhiteSpace(p?.Name) && string.IsNullOrWhiteSpace(p?.Title))
                continue;

            var line = $"{p.Name} as {p.Title}".Trim();

            if (first)
            {
                sb.AppendLine(line);
                first = false;
            }
            else
            {
                sb.AppendLine($", {line}");
            }
        }

        return sb.ToString();
    }

    private static string BuildAliasNames(IEnumerable<AkaName> parties)
    {
        if (parties is null) return string.Empty;

        var sb = new StringBuilder();
        var first = true;

        foreach (var p in parties)
        {
            if (string.IsNullOrWhiteSpace(p?.Name) && string.IsNullOrWhiteSpace(p?.AlsoKnownAs))
                continue;

            var line = $"{p.Name} as {p.AlsoKnownAs}".Trim();

            if (first)
            {
                sb.AppendLine(line);
                first = false;
            }
            else
            {
                sb.AppendLine($", {line}");
            }
        }

        return sb.ToString();
    }

    private static string BuildEntityOwners(IEnumerable<EntityOwner> parties)
    {
        if (parties is null) return string.Empty;

        var sb = new StringBuilder();
        var first = true;

        foreach (var p in parties)
        {
            if (string.IsNullOrWhiteSpace(p?.Name))
                continue;

            var line = $"{p.Name} a {p.PercentOfOwnership}% owner".Trim();

            if (first)
            {
                sb.AppendLine(line);
                first = false;
            }
            else
            {
                sb.AppendLine($", {line}");
            }
        }

        return sb.ToString();
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

            if (!string.IsNullOrWhiteSpace(trace))
            {
                foreach (var line in trace.Split(new[] { "\r\n", "\n", "\r" }, StringSplitOptions.None))
                    loan.AdminBench.Trace.Add(item: line);
            }
        }
        else
        {
            docIds = DocumentOutputEvaluator.BuildFinalDocumentIds(loanType, data);
        }

        if (docIds.Count == 0)
            return Task.FromResult<IReadOnlyList<Document>>(Array.Empty<Document>());

        var resolved = new List<Document>();

        foreach (var id in docIds)
        {
            var doc = TryResolveLibraryDocumentById(id);
            if (doc is null)
            {
                logger.LogWarning("ThenGenerate: DocumentId not found in DocumentLibrary.Documents. DocId={DocId}", id);
                continue;
            }

            resolved.Add(CloneDocument(doc));
        }

        return Task.FromResult<IReadOnlyList<Document>>(resolved);
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

            ["LoanId"] = loan.Id,
            ["LoanTypeId"] = loan.LoanTypeId,
            ["DocLibId"] = loan.DocLibId
        };

        if (loan.AdminBench?.KeyOverrides is not null)
        {
            foreach (var kvp in loan.AdminBench.KeyOverrides)
                data[kvp.Key] = kvp.Value;
        }

        if (data.Count > 0)
        {
            var keysToRemove = new List<string>();
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

    private async Task SaveDeliveriesAsync(LoanAgreement loan, IReadOnlyList<Document> docs, CancellationToken ct)
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

        await dbApp.UpSertRecordAsync(loan).ConfigureAwait(false);

        logger.LogInformation("✅ Deliveries persisted. LoanId={LoanId} DeliveryCount={Count}", loan.Id, loan.DocumentDeliverys.Count);
    }

    private async Task QueueMergeFromDeliveriesAsync(LoanAgreement loan, CancellationToken ct)
    {
        if (loan.DocumentDeliverys is null || loan.DocumentDeliverys.Count == 0)
        {
            logger.LogInformation("QueueMergeFromDeliveriesAsync: No deliveries to merge for LoanId={LoanId}", loan.Id);
            return;
        }

        foreach (var del in loan.DocumentDeliverys)
        {
            if (ct.IsCancellationRequested) break;

            var doc = TryResolveLibraryDocumentById(del.DocId);

            if (doc is null)
            {
                logger.LogWarning("QueueMergeFromDeliveriesAsync: DocumentId={DocId} not found in DocumentLibrary.Documents", del.DocId);
                continue;
            }

            var copyCount = del.Copies <= 0 ? 1 : del.Copies;

            for (int i = 0; i < copyCount; i++)
            {
                var clone = CloneDocument(source: doc);
                clone.OutputType = del.OutputType;
                clone.Copies = 1;

                var merge = new DocumentMerge
                {
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    LoanAgreement = loan,
                    Document = clone,
                    Status = DocumentMergeState.Status.Queued
                };

                await mergeQueue.EnqueueAsync(new MergeJob(merge), ct).ConfigureAwait(false);
            }
        }
    }

    private Document? TryResolveLibraryDocumentById(Guid docId)
    {
        try
        {
            var libs = dbApp.GetRecords<DocumentLibrary>().ToList();

            foreach (var lib in libs)
            {
                var match = lib.Documents?.FirstOrDefault(d => d.Id == docId);
                if (match is not null) return match;
            }

            return null;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "TryResolveLibraryDocumentById failed. DocId={DocId}", docId);
            return null;
        }
    }

    private static Document CloneDocument(Document source)
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
            GenerateMultipleFor = source.GenerateMultipleFor,
            OutputType = source.OutputType,
            Copies = source.Copies
        };
    }
}

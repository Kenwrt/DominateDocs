using DocumentManager.Infrastructure;
using DocumentManager.Jobs;
using DocumentManager.State;
using DominateDocsData.Enums;
using DominateDocsData.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Collections;
using System.Reflection;
using System.Text;

namespace DocumentManager.Workers;

/// <summary>
/// Optional extension point: implement this in any project (Admin Bench, Loan Processing, etc.)
/// and register with DI to produce the "Then Generate" documents for a loan.
/// </summary>
public interface ILoanThenGenerateEvaluator
{
    Task<IReadOnlyList<Document>> EvaluateAsync(LoanAgreement loan, CancellationToken ct);
}

public sealed class LoanWorker : WorkerPoolBackgroundService<LoanJob>
{
    private readonly ILogger<LoanWorker> logger;
    private readonly IOptions<DocumentManagerConfigOptions> options;
    private readonly IDocumentManagerState docState;
    private readonly IJobQueue<MergeJob> mergeQueue;
    private readonly IServiceProvider services;
    private readonly IEnumerable<ILoanThenGenerateEvaluator> evaluators;

    public LoanWorker(
        IJobQueue<LoanJob> queue,
        IJobQueue<MergeJob> mergeQueue,
        ILogger<LoanWorker> logger,
        IOptions<DocumentManagerConfigOptions> options,
        IDocumentManagerState docState,
        IServiceProvider services,
        IEnumerable<ILoanThenGenerateEvaluator> evaluators)
        : base(queue, logger, options.Value.MaxLoanApplicationThreads)
    {
        this.logger = logger;
        this.options = options;
        this.docState = docState;
        this.mergeQueue = mergeQueue;
        this.services = services;
        this.evaluators = evaluators;
    }

    protected override async Task HandleAsync(LoanJob job, CancellationToken ct)
    {
        if (!options.Value.IsActive || !docState.IsRunBackgroundLoanApplicationService)
            return;

        await ProcessLoanAsync(job.Loan, ct).ConfigureAwait(false);
    }

    private async Task ProcessLoanAsync(LoanAgreement loan, CancellationToken ct)
    {
        LoanAgreement? loanState = null;

        try
        {
            docState.LoanList.TryAdd(loan.Id, loan);
            docState.StateHasChanged();

            // Build strings used by templates
            loan.LenderNames = await BuildLenderNamesAsync(loan.Lenders, ct).ConfigureAwait(false);
            loan.BorrowerNames = await BuildPartyNamesAsync(loan.Borrowers, ct).ConfigureAwait(false);
            loan.BrokerNames = await BuildPartyNamesAsync(loan.Brokers, ct).ConfigureAwait(false);
            loan.GuarantorNames = await BuildPartyNamesAsync(loan.Guarantors, ct).ConfigureAwait(false);
            loan.PropertyAddresses = await BuildPropertyAddressAsync(loan.Properties, ct).ConfigureAwait(false);

            // Evaluate ThenGenerate docs (no more TODO handwaving)
            var docs = await EvaluateThenGenerateDocumentsAsync(loan, ct).ConfigureAwait(false);

            if (docs.Count == 0)
            {
                logger.LogWarning("ThenGenerate evaluation returned 0 documents for Loan {LoanId}", loan.Id);
                return;
            }

            foreach (var doc in docs)
            {
                ct.ThrowIfCancellationRequested();

                var documentMerge = new DocumentMerge
                {
                    LoanAgreement = loan,
                    Document = doc,
                    Status = DocumentMergeState.Status.Queued
                };

                await mergeQueue.EnqueueAsync(new MergeJob(documentMerge), ct).ConfigureAwait(false);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Loan processing failed for {LoanId}", loan.Id);
            throw;
        }
        finally
        {
            docState.LoanList.Remove(loan.Id, out loanState);
            docState.StateHasChanged();
        }
    }

    private async Task<IReadOnlyList<Document>> EvaluateThenGenerateDocumentsAsync(LoanAgreement loan, CancellationToken ct)
    {
        // 1) Preferred path: explicit evaluator(s) registered in DI
        // This is how you avoid "guessing" forever.
        foreach (var eval in evaluators)
        {
            var docs = await eval.EvaluateAsync(loan, ct).ConfigureAwait(false);
            if (docs is { Count: > 0 })
                return docs;
        }

        // 2) Fallback path: reflection adapter to existing evaluator types without hard references.
        // This lets you keep moving if the evaluator lives in another assembly/shared project.
        var fallback = await TryEvaluateViaReflectionAsync(loan, ct).ConfigureAwait(false);
        return fallback;
    }

    private static readonly Lazy<Type?> EvaluatorTypeCache = new(() =>
    {
        // Try common names (including the typo you mentioned before: DocumentOuptEvaluator)
        var targetNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "DocumentOutputEvaluator",
            "DocumentOuptEvaluator"
        };

        foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
        {
            Type[] types;
            try { types = asm.GetTypes(); }
            catch { continue; }

            foreach (var t in types)
            {
                if (t is null) continue;
                if (targetNames.Contains(t.Name))
                    return t;
            }
        }

        return null;
    });

    private async Task<IReadOnlyList<Document>> TryEvaluateViaReflectionAsync(LoanAgreement loan, CancellationToken ct)
    {
        var t = EvaluatorTypeCache.Value;
        if (t is null)
            return Array.Empty<Document>();

        // Resolve from DI by Type
        object? svc = null;
        try { svc = services.GetService(t); }
        catch { /* ignore */ }

        if (svc is null)
            return Array.Empty<Document>();

        // Try likely method names in order.
        // We accept sync or async, and accept IEnumerable<Document> or IReadOnlyList<Document> etc.
        var methodNames = new[]
        {
            "EvaluateThenGenerateDocumentsAsync",
            "EvaluateThenGenerateAsync",
            "EvaluateAsync",
            "EvaluateThenGenerateDocuments",
            "EvaluateThenGenerate",
            "Evaluate"
        };

        MethodInfo? mi = null;
        foreach (var name in methodNames)
        {
            mi = t.GetMethods(BindingFlags.Public | BindingFlags.Instance)
                  .FirstOrDefault(m =>
                      string.Equals(m.Name, name, StringComparison.OrdinalIgnoreCase) &&
                      m.GetParameters().Length is >= 1 and <= 2);

            if (mi is not null) break;
        }

        if (mi is null)
            return Array.Empty<Document>();

        object? result;
        var parms = mi.GetParameters();

        try
        {
            if (parms.Length == 1)
            {
                // (LoanAgreement loan)
                result = mi.Invoke(svc, new object?[] { loan });
            }
            else
            {
                // (LoanAgreement loan, CancellationToken ct)
                result = mi.Invoke(svc, new object?[] { loan, ct });
            }
        }
        catch
        {
            return Array.Empty<Document>();
        }

        // Await Task if needed
        if (result is Task task)
        {
            await task.ConfigureAwait(false);

            var taskType = task.GetType();
            if (taskType.IsGenericType)
            {
                var prop = taskType.GetProperty("Result");
                result = prop?.GetValue(task);
            }
            else
            {
                result = null;
            }
        }

        if (result is null)
            return Array.Empty<Document>();

        // Normalize return value into IReadOnlyList<Document>
        if (result is IReadOnlyList<Document> ro)
            return ro;

        if (result is IEnumerable<Document> ie)
            return ie.ToList();

        if (result is IEnumerable anyEnum)
        {
            var list = new List<Document>();
            foreach (var item in anyEnum)
            {
                if (item is Document d) list.Add(d);
            }
            return list;
        }

        return Array.Empty<Document>();
    }

    // ----- your existing helpers (kept intact) -----

    private async Task<string> BuildPartyNamesAsync<T>(IEnumerable<T> parties, CancellationToken cancellationToken = default)
        where T : IPartyNames
    {
        if (parties is null) return string.Empty;

        var sb = new StringBuilder();
        var first = true;

        foreach (var p in parties)
        {
            if (cancellationToken.IsCancellationRequested) break;

            var isIndividual = p.EntityType == Entity.Types.Individual;
            string line = isIndividual
                ? $"{p.EntityName} a {p.EntityType}"
                : $"{p.EntityName} a {p.StateOfIncorporationDescription} {p.EntityStructureDescription}";

            if (first) { sb.AppendLine(line); first = false; }
            else { sb.AppendLine($", {line}"); }
        }

        return sb.ToString();
    }

    private async Task<string> BuildLenderNamesAsync(IEnumerable<Lender> lender, CancellationToken cancellationToken = default)
    {
        if (lender is null) return string.Empty;

        var sb = new StringBuilder();
        var first = true;

        foreach (var p in lender)
        {
            if (cancellationToken.IsCancellationRequested) break;

            var isIndividual = p.EntityType == Entity.Types.Individual;

            string line;
            if (p.NmlsLicenseNumber is not null)
            {
                line = isIndividual
                    ? $"{p.EntityName} a {p.EntityType}"
                    : $"{p.EntityName} a {p.StateOfIncorporationDescription} {p.EntityStructureDescription} (CFL License No.{p.NmlsLicenseNumber})";
            }
            else
            {
                line = isIndividual
                    ? $"{p.EntityName} a {p.EntityType}"
                    : $"{p.EntityName} a {p.StateOfIncorporationDescription} {p.EntityStructureDescription}";
            }

            if (first) { sb.AppendLine(line); first = false; }
            else { sb.AppendLine($", {line}"); }
        }

        return sb.ToString();
    }

    private async Task<string> BuildPropertyAddressAsync<T>(IEnumerable<T> properties, CancellationToken cancellationToken = default)
        where T : IPropertyAddresses
    {
        if (properties is null) return string.Empty;

        var sb = new StringBuilder();
        var first = true;

        foreach (var p in properties)
        {
            if (cancellationToken.IsCancellationRequested) break;

            if (first) { sb.AppendLine(p.FullAddress); first = false; }
            else { sb.AppendLine($", {p.FullAddress}"); }
        }

        return sb.ToString();
    }
}

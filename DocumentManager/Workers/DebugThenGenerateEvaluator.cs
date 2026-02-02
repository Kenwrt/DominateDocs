using DocumentManager.Services;
using DominateDocsData.Database;
using DominateDocsData.Models;
using Microsoft.Extensions.Logging;
using System.Reflection;

namespace DocumentManager.Workers;

public sealed class DebugThenGenerateEvaluator : ILoanThenGenerateEvaluator
{
    private readonly IDocumentOutputService outputService; // kept for DI compatibility
    private readonly IServiceProvider services;             // kept for DI compatibility
    private readonly ILogger<DebugThenGenerateEvaluator> logger;
    private readonly IMongoDatabaseRepo db;

    public DebugThenGenerateEvaluator(
        IDocumentOutputService outputService,
        IServiceProvider services,
        IMongoDatabaseRepo db,
        ILogger<DebugThenGenerateEvaluator> logger)
    {
        this.outputService = outputService;
        this.services = services;
        this.logger = logger;
        this.db = db;
    }

    public Task<IReadOnlyList<Document>> EvaluateAsync(LoanAgreement loan, CancellationToken ct)
    {
        // Only act when explicitly enabled (so this stays AdminBench-only behavior)
        if (loan.AdminBench?.Enabled != true)
            return Task.FromResult<IReadOnlyList<Document>>(Array.Empty<Document>());

        var loanType = db.GetRecordById<LoanType>(loan.LoanTypeId);
        if (loanType is null)
        {
            logger.LogWarning("ThenGenerate(Debug): LoanType could not be resolved. LoanId={LoanId}", loan.Id);
            
            return Task.FromResult<IReadOnlyList<Document>>(Array.Empty<Document>());
        }

        var data = BuildRuleDataBag(loan);

        var ids = DocumentOutputEvaluator.BuildFinalDocumentIdsWithTrace(loanType, data, out var trace);

        


        loan.AdminBench.Trace.Clear();

        loan.AdminBench.Trace.AddRange(trace);

        if (ids.Count == 0)
        {
            loan.AdminBench.Trace.Add("No documents returned from ThenGenerate.");
            loan.DocumentDeliverys.Clear();
            return Task.FromResult<IReadOnlyList<Document>>(Array.Empty<Document>());
        }

        var docs = new List<Document>(ids.Count);
        foreach (var id in ids)
        {
            ct.ThrowIfCancellationRequested();

            var doc = db.GetRecordById<Document>(id);
            if (doc != null)
                docs.Add(doc);
            else
                loan.AdminBench.Trace.Add($"! missing Document id={id}");
        }

        // Build delivery list for the bench to show results without generating the files
        loan.DocumentDeliverys.Clear();

        foreach (var d in docs)
        {
            loan.DocumentDeliverys.Add(new DocumentDelivery
            {
                DocId = d.Id,
                OutputType = loan.AdminBench.OutputTypeOverride ?? loan.OutputType,
                DelieveryTypes = DominateDocsData.Enums.DocumentTypes.DelieveryTypes.Email,
                DeliveryLoaction = loan.AdminBench.EmailToOverride ?? loan.EmailTo,
                Copies = 1
            });
        }

        logger.LogInformation("ThenGenerate(Debug): resolved {Count} documents for LoanId={LoanId}", docs.Count, loan.Id);
        
        return Task.FromResult<IReadOnlyList<Document>>(docs);
    }

    private static IReadOnlyDictionary<string, object?> BuildRuleDataBag(LoanAgreement loan)
    {
        var data = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
        {
            ["LenderCode"] = loan.LenderCode,
            ["BrokerCode"] = loan.BrokerCode,
            ["BorrowerCode"] = loan.BorrowerCode,
            ["PropertyState"] = loan.PropertyState,
            ["LoanTypeName"] = loan.LoanTypeName
        };

        if (loan.AdminBench?.KeyOverrides is not null)
        {
            foreach (var kvp in loan.AdminBench.KeyOverrides)
                data[kvp.Key] = kvp.Value;
        }

        foreach (var k in data.Where(kvp => kvp.Value is null).Select(kvp => kvp.Key).ToList())
            data.Remove(k);

        return data;
    }
}

using DocumentManager.Services;
using DocumentManager.State;
using DominateDocsData.Enums;
using DominateDocsData.Models;
using DominateDocsData.Database;
using Microsoft.Extensions.Logging;
using System.Reflection;

namespace DocumentManager.Services;

public sealed class DocumentOutputService : IDocumentOutputService
{
    private readonly IMongoDatabaseRepo db;
    private readonly IDocumentManagerState docState;
    private readonly ILogger<DocumentOutputService> logger;

    public DocumentOutputService(
        IMongoDatabaseRepo db,
        IDocumentManagerState docState,
        ILogger<DocumentOutputService> logger)
    {
        this.db = db;
        this.docState = docState;
        this.logger = logger;
    }

    // =========================================================
    // DocLibId semantics
    // =========================================================
    // SelectedDocLibId is the *logical* DocLibId stored on:
    //   - Document.DocLibId (inside DocumentLibrary.Documents)
    //   - LoanType.DocLibId
    //
    // It is NOT the DocumentLibrary record Id.
    // =========================================================

    public List<Guid> GetDocLibIds()
    {
        try
        {
            var libs = db.GetRecords<DocumentLibrary>().ToList();
            var docs = libs
                .Where(l => l is not null)
                .SelectMany(l => l.Documents ?? new List<Document>())
                .ToList();

            var loanTypes = db.GetRecords<LoanType>().ToList();

            return docs.Select(d => d.DocLibId)
                .Concat(loanTypes.Select(t => t.DocLibId))
                .Where(x => x != Guid.Empty)
                .Distinct()
                .OrderBy(x => x)
                .ToList();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "GetDocLibIds failed");
            return new List<Guid>();
        }
    }

    public List<Document> GetDocuments(Guid docLibId)
    {
        // docLibId = Document.DocLibId (logical), NOT DocumentLibrary.Id
        try
        {
            var libs = db.GetRecords<DocumentLibrary>().ToList();

            return libs
                .Where(l => l is not null)
                .SelectMany(l => l.Documents ?? new List<Document>())
                .Where(d => d.DocLibId == docLibId)
                .OrderBy(d => d.Name)
                .ToList();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "GetDocuments failed for DocLibId={DocLibId}", docLibId);
            return new List<Document>();
        }
    }

    public List<LoanType> GetLoanTypes(Guid docLibId)
    {
        try
        {
            return db.GetRecords<LoanType>()
                .Where(t => t.DocLibId == docLibId)
                .OrderBy(t => t.Name)
                .ToList();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "GetLoanTypes failed for DocLibId={DocLibId}", docLibId);
            return new List<LoanType>();
        }
    }

    public List<LoanAgreement> GetLoanAgreements()
    {
        try
        {
            return db.GetRecords<LoanAgreement>().ToList();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "GetLoanAgreements failed");
            return new List<LoanAgreement>();
        }
    }

    public string GetLoanLabel(LoanAgreement loan)
    {
        // Prefer common fields if present; keep this safe for model churn.
        var rn = TryGetPropString(loan, "ReferenceName");
        if (!string.IsNullOrWhiteSpace(rn)) return rn;

        var name = TryGetPropString(loan, "Name");
        if (!string.IsNullOrWhiteSpace(name)) return name;

        return loan.ToString() ?? "Loan";
    }

    public List<Document> EvaluateDocuments(LoanType loanType, LoanAgreement loanAgreement, IReadOnlyList<Document> docPool)
    {
        var data = BuildEvalData(loanAgreement);

        var ids = DocumentOutputEvaluator.BuildFinalDocumentIds(loanType, data);

        var byId = docPool.GroupBy(d => d.Id).ToDictionary(g => g.Key, g => g.First());

        var results = new List<Document>();
        foreach (var id in ids)
        {
            if (byId.TryGetValue(id, out var doc))
                results.Add(doc);
        }

        return results;
    }

    // =========================================================
    // DocumentStore support (templates live here)
    // =========================================================

    public byte[]? TryGetTemplateBytes(Document doc)
    {
        if (doc is null) return null;

        if (doc.DocStoreId != Guid.Empty)
        {
            try
            {
                var store = db.GetRecordById<DocumentStore>(doc.DocStoreId);
                if (store?.DocumentBytes is not null && store.DocumentBytes.Length > 0)
                    return store.DocumentBytes;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "TryGetTemplateBytes failed. DocId={DocId} DocStoreId={DocStoreId}", doc.Id, doc.DocStoreId);
            }
        }

        // Back-compat fallback during migration (older docs)
        if (doc.TemplateDocumentBytes is not null && doc.TemplateDocumentBytes.Length > 0)
            return doc.TemplateDocumentBytes;

        return null;
    }

    public Document? TryResolveDocumentById(Guid docId)
    {
        if (docId == Guid.Empty) return null;

        try
        {
            var libs = db.GetRecords<DocumentLibrary>().ToList();

            return libs
                .Where(l => l is not null)
                .SelectMany(l => l.Documents ?? new List<Document>())
                .FirstOrDefault(d => d.Id == docId);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "TryResolveDocumentById failed. DocId={DocId}", docId);
            return null;
        }
    }

    // =========================================================
    // Rule evaluation keys
    // =========================================================

    private IReadOnlyDictionary<string, object?> BuildEvalData(LoanAgreement loan)
    {
        var data = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);

        data["LenderCode"] = loan.LenderCode;
        data["BrokerCode"] = loan.BrokerCode;
        data["BorrowerCode"] = loan.BorrowerCode;
        data["PropertyState"] = loan.PropertyState;
        data["LoanTypeName"] = loan.LoanTypeName;

        data["LoanId"] = loan.Id;
        data["LoanTypeId"] = loan.LoanTypeId;
        data["DocLibId"] = loan.DocLibId;

        var lenderState =
            TryGetNestedString(loan, "Lenders", 0, "State") ??
            TryGetNestedString(loan, "Lenders", 0, "StateChoiceOfLaw") ??
            TryGetNestedString(loan, "Lender", "State") ??
            TryGetNestedString(loan, "Lender", "StateChoiceOfLaw") ??
            TryGetNestedString(loan, "Lenders", 0, "Address", "State") ??
            TryGetNestedString(loan, "Lenders", 0, "MailingAddress", "State") ??
            TryGetNestedString(loan, "Lenders", 0, "PhysicalAddress", "State");

        if (!string.IsNullOrWhiteSpace(lenderState))
            data["LenderState"] = lenderState!.Trim();

        var borrowerState =
            TryGetNestedString(loan, "Borrowers", 0, "State") ??
            TryGetNestedString(loan, "Borrowers", 0, "StateChoiceOfLaw") ??
            TryGetNestedString(loan, "Borrower", "State") ??
            TryGetNestedString(loan, "Borrower", "StateChoiceOfLaw") ??
            TryGetNestedString(loan, "Borrowers", 0, "Address", "State") ??
            TryGetNestedString(loan, "Borrowers", 0, "MailingAddress", "State") ??
            TryGetNestedString(loan, "Borrowers", 0, "PhysicalAddress", "State");

        if (!string.IsNullOrWhiteSpace(borrowerState))
            data["BorrowerState"] = borrowerState!.Trim();

        var brokerState =
            TryGetNestedString(loan, "Brokers", 0, "State") ??
            TryGetNestedString(loan, "Brokers", 0, "StateChoiceOfLaw") ??
            TryGetNestedString(loan, "Broker", "State") ??
            TryGetNestedString(loan, "Broker", "StateChoiceOfLaw") ??
            TryGetNestedString(loan, "Brokers", 0, "Address", "State") ??
            TryGetNestedString(loan, "Brokers", 0, "MailingAddress", "State") ??
            TryGetNestedString(loan, "Brokers", 0, "PhysicalAddress", "State");

        if (!string.IsNullOrWhiteSpace(brokerState))
            data["BrokerState"] = brokerState!.Trim();

        if (loan.AdminBench?.KeyOverrides is not null)
        {
            foreach (var kvp in loan.AdminBench.KeyOverrides)
                data[kvp.Key] = kvp.Value;
        }

        // prune nulls
        if (data.Count > 0)
        {
            var rm = new List<string>();
            foreach (var kv in data)
                if (kv.Value is null) rm.Add(kv.Key);
            foreach (var k in rm)
                data.Remove(k);
        }

        return data;
    }

    private static string? TryGetPropString(object obj, string propName)
    {
        var prop = obj.GetType().GetProperty(propName, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
        if (prop == null) return null;
        return prop.GetValue(obj)?.ToString();
    }

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
                    var pi = cur.GetType().GetProperty(propName, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
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
}

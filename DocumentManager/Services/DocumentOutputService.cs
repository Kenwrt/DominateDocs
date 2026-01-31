using DocumentManager.Services;
using DocumentManager.State;
using DominateDocsData.Enums;
using DominateDocsData.Models;
using DominateDocsNotify.State;
using DominateDocsData.Database;
using Microsoft.Extensions.Logging;
using System.Reflection;

namespace DocumentManager.Services;

public sealed class DocumentOutputService : IDocumentOutputService
{
    private readonly IMongoDatabaseRepo db;
    private readonly IDocumentManagerState docState;
    private readonly INotifyState notifyState;
    private readonly ILogger<DocumentOutputService> logger;

    public DocumentOutputService(
        IMongoDatabaseRepo db,
        IDocumentManagerState docState,
        INotifyState notifyState,
        ILogger<DocumentOutputService> logger)
    {
        this.db = db;
        this.docState = docState;
        this.notifyState = notifyState;
        this.logger = logger;
    }

    // -------------------------
    // Library + list loading
    // -------------------------

    public List<Guid> GetDocLibIds()
    {
        // Build libraries from things that actually have DocLibId (NO LoanAgreement.DocLibId)
        var allDocs = db.GetRecords<DominateDocsData.Models.Document>().ToList();
        var allLoanTypes = db.GetRecords<DominateDocsData.Models.LoanType>().ToList();

        return allDocs.Select(d => d.DocLibId)
            .Concat(allLoanTypes.Select(t => t.DocLibId))
            .Where(id => id != Guid.Empty)
            .Distinct()
            .OrderBy(id => id)
            .ToList();
    }

    public List<DominateDocsData.Models.Document> GetDocuments(Guid docLibId)
    {
        return db.GetRecords<DominateDocsData.Models.Document>()
            .Where(d => d.DocLibId == docLibId)
            .OrderBy(d => d.Name)
            .ToList();
    }

    public List<DominateDocsData.Models.LoanType> GetLoanTypes(Guid docLibId)
    {
        return db.GetRecords<DominateDocsData.Models.LoanType>()
            .Where(t => t.DocLibId == docLibId)
            .OrderBy(t => t.Name)
            .ToList();
    }

    public List<DominateDocsData.Models.LoanAgreement> GetLoanAgreements()
    {
        // Agreements are NOT keyed by DocLibId in your model
        return db.GetRecords<DominateDocsData.Models.LoanAgreement>().ToList();
    }

    public string GetLoanLabel(DominateDocsData.Models.LoanAgreement loan)
    {
        // Prefer ReferenceName if present; else fall back to Name, Id, ToString
        var rn = TryGetPropString(loan, "ReferenceName");
        if (!string.IsNullOrWhiteSpace(rn)) return rn;

        var name = TryGetPropString(loan, "Name");
        if (!string.IsNullOrWhiteSpace(name)) return name;

        var id = TryGetPropString(loan, "Id");
        if (!string.IsNullOrWhiteSpace(id)) return id;

        return loan.ToString() ?? "LoanAgreement";
    }

    // -------------------------
    // Evaluation (rules)
    // -------------------------

    public List<Document> EvaluateDocuments(LoanType loanType, LoanAgreement loanAgreement, IReadOnlyList<Document> docPool)
    {
        var data = BuildEvalData(loanAgreement);

        // Evaluate IDs from rules
        var ids = DocumentOutputEvaluator.BuildFinalDocumentIds(loanType, data);

        var docsById = docPool.ToDictionary(d => d.Id, d => d);

        return ids.Where(id => docsById.ContainsKey(id))
                  .Select(id => docsById[id])
                  .ToList();
    }

    /// <summary>
    /// This is your "key bag" for rule evaluation. Add keys here (or make this pluggable later).
    /// </summary>
    public Dictionary<string, object?> BuildEvalData(DominateDocsData.Models.LoanAgreement loan)
    {
        // This is the rule "key bag". If the UI lets you pick a field name,
        // then this method is the contract that provides it.
        var data = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);

        var lender0 = loan.Lenders?.FirstOrDefault();
        var borrower0 = loan.Borrowers?.FirstOrDefault();
        var broker0 = loan.Brokers?.FirstOrDefault();

        // States
        var lenderState = GetState(lender0);
        if (!string.IsNullOrWhiteSpace(lenderState))
            data["LenderState"] = lenderState;

        var borrowerState = GetState(borrower0);
        if (!string.IsNullOrWhiteSpace(borrowerState))
            data["BorrowerState"] = borrowerState;

        var brokerState = GetState(broker0);
        if (!string.IsNullOrWhiteSpace(brokerState))
            data["BrokerState"] = brokerState;

        // Codes
        var lenderCode = loan.LenderCode ?? GetString(lender0, "LenderCode") ?? GetString(lender0, "Code");
        if (!string.IsNullOrWhiteSpace(lenderCode))
            data["LenderCode"] = lenderCode;

        var borrowerCode = loan.BorrowerCode ?? GetString(borrower0, "BorrowerCode") ?? GetString(borrower0, "Code");
        if (!string.IsNullOrWhiteSpace(borrowerCode))
            data["BorrowerCode"] = borrowerCode;

        var brokerCode = loan.BrokerCode ?? GetString(broker0, "BrokerCode") ?? GetString(broker0, "Code");
        if (!string.IsNullOrWhiteSpace(brokerCode))
            data["BrokerCode"] = brokerCode;

        // Property
        if (!string.IsNullOrWhiteSpace(loan.PropertyState))
            data["PropertyState"] = loan.PropertyState;

        return data;

        static string? GetState(object? party)
        {
            if (party is null) return null;

            // Try common names across your models (you change these, because humans)
            var raw =
                GetString(party, "State") ??
                GetString(party, "PreferredStateVenue") ??
                GetString(party, "StateOfIncorporation");

            return string.IsNullOrWhiteSpace(raw) ? null : raw.Trim();
        }

        static string? GetString(object? obj, string propName)
        {
            if (obj is null) return null;
            try
            {
                var pi = obj.GetType().GetProperty(propName, System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
                return pi?.GetValue(obj)?.ToString();
            }
            catch { return null; }
        }
    }


    // -------------------------
    // Merge + Email
    // -------------------------

    public async Task MergeAndEmailAsync(
        IReadOnlyList<DominateDocsData.Models.Document> docs,
        DominateDocsData.Models.LoanAgreement loanAgreement,
        DocumentTypes.OutputTypes outputType,
        string emailTo,
        string subject)
    {
        if (docs.Count == 0) return;

        docState.IsRunBackgroundDocumentMergeService = true;
        notifyState.IsRunBackgroundEmailService = true;

        var merges = new List<DocumentMerge>();

        foreach (var doc in docs)
        {
            doc.OutputType = outputType;

            var merge = new DocumentMerge
            {
                Id = Guid.NewGuid(),
                LoanAgreement = loanAgreement,
                Document = doc,
                Status = DocumentMergeState.Status.Pending
            };

            merges.Add(merge);
            docState.DocumentProcessingQueue.Enqueue(merge);
        }

        // Wait for completion (simple polling like your VM did)
        while (merges.Any(m => m.Status == DocumentMergeState.Status.Pending))
            await Task.Delay(200);

        var completed = merges
            .Where(m => m.Status == DocumentMergeState.Status.Complete && m.MergedDocumentBytes != null)
            .ToList();

        QueueEmailBestEffort(
            to: emailTo,
            subject: subject,
            completed: completed);
    }

    // -------------------------
    // Reflection helpers
    // -------------------------

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

    private static string? TryGetPropString(object obj, string propName)
    {
        try
        {
            var pi = obj.GetType().GetProperty(propName, BindingFlags.Public | BindingFlags.Instance);
            var v = pi?.GetValue(obj);
            return v?.ToString();
        }
        catch { return null; }
    }

    // -------------------------
    // Email enqueue (best effort)
    // -------------------------

    private void QueueEmailBestEffort(string to, string subject, List<DocumentMerge> completed)
    {
        // Same approach as your VM: reflection so we don't guess your EmailMsg shape.
        try
        {
            var emailMsgType = FindType("DominateDocsNotify.Models.EmailMsg");
            if (emailMsgType == null)
            {
                logger.LogWarning("EmailMsg type not found. Skipping email queue.");
                return;
            }

            var msg = Activator.CreateInstance(emailMsgType);
            if (msg == null)
            {
                logger.LogWarning("Could not create EmailMsg instance. Skipping email queue.");
                return;
            }

            SetIfExists(msg, "Id", Guid.NewGuid());
            SetIfExists(msg, "To", to);
            SetIfExists(msg, "Subject", subject);
            SetIfExists(msg, "MessageBody", $"Admin bench merge completed. Attachments: {completed.Count}.");

            // ProviderType (optional)
            var providerProp = emailMsgType.GetProperty("ProviderType");
            if (providerProp != null && providerProp.CanWrite)
            {
                var pt = providerProp.PropertyType;
                if (pt.IsEnum)
                {
                    object? value = Enum.GetNames(pt).Contains("Fluent")
                        ? Enum.Parse(pt, "Fluent")
                        : Enum.GetValues(pt).GetValue(0);

                    if (value != null) providerProp.SetValue(msg, value);
                }
            }

            // Attachments (best effort)
            TryAddAttachments(msg, completed);

            // Queue it
            var q = notifyState.EmailMsgProcessingQueue;
            var enqueue = q.GetType().GetMethod("Enqueue");
            enqueue?.Invoke(q, new[] { msg });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "QueueEmailBestEffort failed (email skipped).");
        }
    }

    private void TryAddAttachments(object msg, List<DocumentMerge> completed)
    {
        try
        {
            var t = msg.GetType();
            var attachmentsProp = t.GetProperty("Attachments", BindingFlags.Public | BindingFlags.Instance);
            if (attachmentsProp == null) return;

            var attachmentsObj = attachmentsProp.GetValue(msg);
            if (attachmentsObj == null) return;

            var addMethod = attachmentsObj.GetType().GetMethod("Add");
            if (addMethod == null) return;

            var elementType = attachmentsObj.GetType().IsGenericType
                ? attachmentsObj.GetType().GetGenericArguments()[0]
                : null;

            if (elementType == null) return;

            foreach (var m in completed)
            {
                if (m.MergedDocumentBytes == null) continue;

                var att = Activator.CreateInstance(elementType);
                if (att == null) continue;

                SetIfExists(att, "FileName", $"{SafeName(m.Document?.Name) ?? "MergedDoc"}.pdf");
                SetIfExists(att, "ContentType", "application/pdf");
                SetIfExists(att, "Bytes", m.MergedDocumentBytes);
                SetIfExists(att, "Data", m.MergedDocumentBytes);

                addMethod.Invoke(attachmentsObj, new[] { att });
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Attachment reflection add failed (email will queue without attachments).");
        }
    }

    private static void SetIfExists(object target, string propName, object? value)
    {
        var pi = target.GetType().GetProperty(propName, BindingFlags.Public | BindingFlags.Instance);
        if (pi == null || !pi.CanWrite) return;

        if (value == null)
        {
            pi.SetValue(target, null);
            return;
        }

        if (pi.PropertyType.IsAssignableFrom(value.GetType()))
        {
            pi.SetValue(target, value);
            return;
        }

        if (pi.PropertyType.IsEnum && value is string s)
            pi.SetValue(target, Enum.Parse(pi.PropertyType, s));
    }

    private static Type? FindType(string fullName)
    {
        foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
        {
            var t = asm.GetType(fullName, throwOnError: false);
            if (t != null) return t;
        }
        return null;
    }

    private static string? SafeName(string? name)
    {
        if (string.IsNullOrWhiteSpace(name)) return null;

        foreach (var c in Path.GetInvalidFileNameChars())
            name = name.Replace(c, '_');

        return name;
    }
}

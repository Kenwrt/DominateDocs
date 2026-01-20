using DominateDocsSite.Database;
using DominateDocsSite.State;
using DominateDocsData.Models;
using DominateDocsData.Models.DTOs;
using DominateDocsData.Models.RulesEngine;
using DominateDocsData.Models.RulesEngine.Enums;
using DominateDocsData.Models.RulesEngine.Fields;
using Microsoft.Extensions.Logging;
using System.Collections.ObjectModel;
using System.Reflection;
using static DominateDocsData.Models.RulesEngine.Enums.RulesEnums;

namespace DominateDocsSite.ViewModels;

public sealed class LoanTypeViewModel
{
    private readonly ILogger<LoanTypeViewModel> logger;
    private readonly IMongoDatabaseRepo db;
    private readonly UserSession userSession;

    public const int MaxDepth = 4;

    // -----------------------------
    // Backing model (newer name)
    // -----------------------------
    public LoanType Editing { get; private set; } = new();

    // -----------------------------
    // Compatibility layer (older UI expects these)
    // -----------------------------

    /// <summary>
    /// Old pages expect EditingLoanType. Provide it as an alias.
    /// </summary>
    public LoanType EditingLoanType
    {
        get => Editing;
        set => Editing = value ?? new LoanType();
    }

    /// <summary>
    /// Old pages expect MasterDocuments as documents (not DTOs).
    /// </summary>
    public IReadOnlyList<Document> MasterDocuments { get; private set; } = Array.Empty<Document>();

    /// <summary>
    /// Optional: still useful for chip labels, etc.
    /// </summary>
    public IReadOnlyList<DocumentListDTO> MasterDocumentDtos { get; private set; } = Array.Empty<DocumentListDTO>();

    /// <summary>
    /// Old pages expect Fields in (Key, Label) shape.
    /// </summary>
    public IReadOnlyList<(string Key, string Label)> Fields { get; private set; }
        = Array.Empty<(string, string)>();

    /// <summary>
    /// Old pages expect FieldValues in dictionary form:
    /// FieldKey -> list of (Display, Value)
    /// </summary>
    public IReadOnlyDictionary<string, IReadOnlyList<(string Display, string Value)>> FieldValues { get; private set; }
        = new Dictionary<string, IReadOnlyList<(string, string)>>();

    /// <summary>
    /// Old pages expect OutputRules to be a mutable UI collection.
    /// </summary>
    public ObservableCollection<OutputRule> OutputRulesUi { get; private set; } = new();

    // -----------------------------
    // ctor
    // -----------------------------
    public LoanTypeViewModel(
        ILogger<LoanTypeViewModel> logger,
        IMongoDatabaseRepo db,
        UserSession userSession)
    {
        this.logger = logger;
        this.db = db;
        this.userSession = userSession;

        LoadMasterDocuments(userSession.DocLibId);
        BuildRuleFieldMetadata();

        LoadForCreate();
    }

    // -----------------------------
    // Load/Create/Edit
    // -----------------------------
    public void LoadForCreate()
    {
        Editing = new LoanType
        {
            Name = "",
            Description = "",
            IconKey = "",
            DefaultDocumentIds = new(),
            OutputRules = new(),
            ProductionDocumentIds = new()
        };

        EnsureLogic();
        RebuildUiCollections();
    }

    public void LoadForEdit(Guid loanTypeId)
    {
        var existing = db.GetRecordById<LoanType>(loanTypeId);
        Editing = existing ?? new LoanType();

        EnsureLogic();
        RebuildUiCollections();
    }

    public void EnsureLogic()
    {
        Editing.OutputRules ??= new List<OutputRule>();
        Editing.DefaultDocumentIds ??= new List<Guid>();
        Editing.ProductionDocumentIds ??= new List<Guid>();
    }

    private void RebuildUiCollections()
    {
        EnsureLogic();
        OutputRulesUi = new ObservableCollection<OutputRule>(Editing.OutputRules ?? new List<OutputRule>());
    }

    public void CommitUiToModel()
    {
        EnsureLogic();

        Editing.OutputRules = OutputRulesUi.ToList();

        db.UpSertRecord(Editing);

        LoadForCreate();
    }

    // -----------------------------
    // Master Documents
    // -----------------------------
    public void LoadMasterDocuments(Guid docLibId)
    {
        try
        {
            var docs = db.GetRecords<Document>() ?? Enumerable.Empty<Document>();
            var filtered = docs.Where(x => x.DocLibId == docLibId).ToList();

            MasterDocuments = filtered;

            // Keep a DTO projection for display purposes, but don't break the UI expectations.
            MasterDocumentDtos = filtered
                .Select(d => new DocumentListDTO(
                    GetDocumentId(d),
                    d.DocLibId,
                    GetDocumentDisplayName(d),
                    GetDocumentUpdatedAt(d),
                    GetDocumentDocStoreId(d)))
                .Where(x => x.Id != Guid.Empty)
                .ToList();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to load MasterDocuments for DocLibId {DocLibId}", docLibId);
            MasterDocuments = Array.Empty<Document>();
            MasterDocumentDtos = Array.Empty<DocumentListDTO>();
        }
    }

    // -----------------------------
    // Old method names expected by LoanTypeCreate.razor
    // -----------------------------
    public OutputRule AddOutputRule() => AddRule();

    public void RemoveOutputRule(OutputRule rule) => RemoveRule(rule);

    // -----------------------------
    // Newer internal names
    // -----------------------------
    public OutputRule AddRule()
    {
        EnsureLogic();

        var rule = new OutputRule
        {
            Name = $"Rule {OutputRulesUi.Count + 1}",
            If = NewGroupWithOneLeaf(),
            ThenGenerateDocumentIds = new List<Guid>()
        };

        OutputRulesUi.Add(rule);
        return rule;
    }

    public void RemoveRule(OutputRule rule)
    {
        if (rule is null) return;
        OutputRulesUi.Remove(rule);
    }

    public void SetDefaultDocuments(List<Guid> docIds)
    {
        EnsureLogic();
        Editing.DefaultDocumentIds = (docIds ?? new()).Distinct().ToList();
    }

    public void RemoveDefaultDocument(Guid id)
    {
        EnsureLogic();

        var idx = Editing.DefaultDocumentIds.FindIndex(d => d == id);
        if (idx < 0) return;

        Editing.DefaultDocumentIds.RemoveAt(idx);
    }

    public void ClearAllLogic()
    {
        EnsureLogic();
        OutputRulesUi.Clear();
        Editing.DefaultDocumentIds.Clear();
        Editing.ProductionDocumentIds.Clear();
    }

    // -----------------------------
    // Rules: fields + values metadata from RuleFieldRegistry
    // -----------------------------
    private void BuildRuleFieldMetadata()
    {
        try
        {
            var defs = RuleFieldRegistry.All();

            Fields = defs
                .Select(d => (d.Key, d.Label))
                .ToList();

            // Build values dictionary (only if OptionsProvider exists)
            var dict = new Dictionary<string, IReadOnlyList<(string Display, string Value)>>(StringComparer.OrdinalIgnoreCase);

            foreach (var def in defs)
            {
                if (def.OptionsProvider is null)
                {
                    dict[def.Key] = Array.Empty<(string, string)>();
                    continue;
                }

                var options = def.OptionsProvider.Invoke() ?? new List<(string Display, string Value)>();
                dict[def.Key] = options.ToList();
            }

            FieldValues = dict;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to build RuleFieldRegistry metadata.");
            Fields = Array.Empty<(string, string)>();
            FieldValues = new Dictionary<string, IReadOnlyList<(string, string)>>();
        }
    }

    // -----------------------------
    // Rules helpers
    // -----------------------------
    public static ConditionGroup NewGroupWithOneLeaf()
        => new ConditionGroup
        {
            Terms = new List<ConditionTerm>
            {
                new ConditionTerm
                {
                    Node = NewLeaf(),
                    JoinToNext = LogicalOperator.And
                }
            }
        };

    public static ConditionLeaf NewLeaf()
    {
        var firstFieldKey = RuleFieldRegistry.All().FirstOrDefault()?.Key ?? "BorrowerState";

        return new ConditionLeaf
        {
            Condition = new Condition
            {
                FieldKey = firstFieldKey,
                Operator = ConditionalOperator.Equals,
                Values = new List<string>()
            }
        };
    }

    public static void AddLeafTerm(ConditionGroup group, LogicalOperator joinFromPrevious = LogicalOperator.And)
    {
        if (group.Terms.Count > 0)
            group.Terms[^1].JoinToNext = joinFromPrevious;

        group.Terms.Add(new ConditionTerm { Node = NewLeaf(), JoinToNext = LogicalOperator.And });
    }

    public static void RemoveTermAt(ConditionGroup group, int index)
    {
        if (index < 0 || index >= group.Terms.Count) return;

        group.Terms.RemoveAt(index);

        if (group.Terms.Count > 0)
            group.Terms[^1].JoinToNext = LogicalOperator.And;
    }

    // -----------------------------
    // Document model drift helpers
    // -----------------------------
    private static Guid GetDocumentId(Document doc)
    {
        foreach (var name in new[] { "Id", "DocumentId", "_id" })
        {
            var prop = doc.GetType().GetProperty(name, BindingFlags.Public | BindingFlags.Instance);
            if (prop is null) continue;

            var val = prop.GetValue(doc);
            if (val is Guid g && g != Guid.Empty) return g;

            if (val is string s && Guid.TryParse(s, out var parsed) && parsed != Guid.Empty)
                return parsed;
        }

        return Guid.Empty;
    }

    private static string GetDocumentDisplayName(Document doc)
    {
        var candidates = new[] { "Name", "Title", "DisplayName", "DocumentName", "FileName" };

        foreach (var propName in candidates)
        {
            var prop = doc.GetType().GetProperty(propName, BindingFlags.Public | BindingFlags.Instance);
            if (prop is null) continue;

            var val = prop.GetValue(doc);
            if (val is string s && !string.IsNullOrWhiteSpace(s))
                return s;
        }

        var id = GetDocumentId(doc);
        return id == Guid.Empty ? "(Unnamed Document)" : id.ToString();
    }

    private static DateTime GetDocumentUpdatedAt(Document doc)
    {
        var prop = doc.GetType().GetProperty("UpdatedAt", BindingFlags.Public | BindingFlags.Instance);
        return prop?.GetValue(doc) is DateTime dt ? dt : default;
    }

    private static Guid GetDocumentDocStoreId(Document doc)
    {
        var prop = doc.GetType().GetProperty("DocStoreId", BindingFlags.Public | BindingFlags.Instance);
        if (prop?.GetValue(doc) is Guid g) return g;
        if (prop?.GetValue(doc) is string s && Guid.TryParse(s, out var parsed)) return parsed;
        return Guid.Empty;
    }
}

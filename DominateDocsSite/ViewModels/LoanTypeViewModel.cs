using DominateDocsData.Database;
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

   
    public LoanType Editing { get; private set; } = new();

   
    public LoanType EditingLoanType
    {
        get => Editing;
        set => Editing = value ?? new LoanType();
    }

    public IReadOnlyList<DocumentListDTO> MasterDocumentDtos { get; private set; } = Array.Empty<DocumentListDTO>();

    public IReadOnlyList<(string Key, string Label)> Fields { get; private set; } = Array.Empty<(string, string)>();

    public IReadOnlyDictionary<string, IReadOnlyList<(string Display, string Value)>> FieldValues { get; private set; } = new Dictionary<string, IReadOnlyList<(string, string)>>();

    public ObservableCollection<OutputRule> OutputRulesUi { get; private set; } = new();

    public LoanTypeViewModel(ILogger<LoanTypeViewModel> logger,IMongoDatabaseRepo db, UserSession userSession)
    {
        this.logger = logger;
        this.db = db;
        this.userSession = userSession;

        LoadMasterDocuments(userSession.DocLibId);
        BuildRuleFieldMetadata();

        LoadForCreate();
    }

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
            var filtered = db.GetRecords<Document>().Where(x => x.DocLibId == docLibId).Select(d => new DocumentListDTO(d.Id, d.DocLibId, d.Name, d.UpdatedAt, d.DocStoreId)).ToList();    

            MasterDocumentDtos = filtered;

        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to load MasterDocuments for DocLibId {DocLibId}", docLibId);
            MasterDocumentDtos = Array.Empty<DocumentListDTO>();
          
        }
    }

    public OutputRule AddOutputRule() => AddRule();

    public void RemoveOutputRule(OutputRule rule) => RemoveRule(rule);

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

    
}

using DominateDocsData.Models;
using DominateDocsData.Models.DTOs;
using DominateDocsData.Models.RulesEngine;
using DominateDocsSite.Database;
using System.Collections.ObjectModel;
using static DominateDocsData.Models.RulesEngine.Enums.RulesEnums;

namespace DominateDocsSite.ViewModels;

public sealed class LoanTypeViewModel
{
    private readonly ILogger<LoanTypeViewModel> logger;
    private readonly IMongoDatabaseRepo db;
    private readonly UserSession userSession;

    public const int MaxDepth = 4;

    public LoanType Editing { get; set; } = new();

    public List<DocumentListDTO> MasterDocuments { get; private set; } = new();

    public ObservableCollection<OutputRule> OutputRulesUi { get; set; } = new();
     
    public LoanTypeViewModel(ILogger<LoanTypeViewModel> logger, IMongoDatabaseRepo db, UserSession userSession)
    {
        this.logger = logger;
        this.db = db;
        this.userSession = userSession;

        MasterDocuments = db.GetRecords<Document>().Where(x => x.DocLibId == userSession.DocLibId).Select(x => new DocumentListDTO(x.Id,x.DocLibId,x.Name,x.UpdatedAt,x.DocStoreId)).ToList();

       // MasterDocuments = db.GetRecords<Document>().ToList();
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

        RebuildUiCollections();
    }

    // NEW: load for edit by Id (this is what your Razor is calling)
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
    }

    private void RebuildUiCollections()
    {
        EnsureLogic();

        OutputRulesUi = new ObservableCollection<OutputRule>(Editing.OutputRules);
    }

    public void CommitUiToModel()
    {
        Editing.OutputRules = OutputRulesUi.ToList();

        db.UpSertRecord(Editing);

        // reset editor state after save
        LoadForCreate();
    }

    public void SetDefaultDocuments(List<Guid> docIds)
    {
        EnsureLogic();

        var ids = (docIds ?? new()).Distinct().ToList();

        Editing.DefaultDocumentIds.Clear();


        Editing.DefaultDocumentIds = ids;

        //foreach (var id in ids)
        //{
        //    if (!Editing.DefaultDocumentIds.Any(d => d.Id == id))
        //    {
        //        var doc = MasterDocuments.FirstOrDefault(d => d.Id == id);
        //        if (doc != null)
        //        {
        //            Editing.DefaultDocumentIds.Add(new DocumentListDTO
        //            {
        //                Id = doc.Id,
        //                DocLibId = doc.DocLibId,
        //                Name = doc.Name,
        //                UpdatedAt = doc.UpdatedAt,
        //                DocStoreId = doc.DocStoreId
        //            });
        //        }
        //    }
        //}
                
    }

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
    }

    public static ConditionGroup NewGroupWithOneLeaf()
        => new ConditionGroup
        {
            Terms = new List<ConditionTerm>
            {
                new ConditionTerm
                {
                    Node = new ConditionLeaf { Condition = new Condition() },
                    JoinToNext = LogicalOperator.And
                }
            }
        };

    public static bool CanAddChildGroup(int depth) => false;

    public static ConditionLeaf NewLeaf() => new ConditionLeaf { Condition = new Condition() };

    public static void AddLeafTerm(ConditionGroup group, LogicalOperator joinFromPrevious = LogicalOperator.And)
    {
        if (group.Terms.Count > 0)
            group.Terms[^1].JoinToNext = joinFromPrevious;

        group.Terms.Add(new ConditionTerm { Node = NewLeaf(), JoinToNext = LogicalOperator.And });
    }

    public static void AddGroupTerm(ConditionGroup group, int depth, LogicalOperator joinFromPrevious = LogicalOperator.And)
    {
        // Groups disabled by design (UI + evaluator)
    }

    public static void RemoveTermAt(ConditionGroup group, int index)
    {
        if (index < 0 || index >= group.Terms.Count) return;

        group.Terms.RemoveAt(index);

        if (group.Terms.Count > 0) group.Terms[^1].JoinToNext = LogicalOperator.And;
    }
}

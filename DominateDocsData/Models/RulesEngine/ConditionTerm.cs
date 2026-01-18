using static DominateDocsData.Models.RulesEngine.Enums.RulesEnums;

namespace DominateDocsData.Models.RulesEngine;

public sealed class ConditionTerm
{
    public ConditionNode Node { get; set; } = new ConditionLeaf();
    public LogicalOperator JoinToNext { get; set; } = LogicalOperator.And;
}
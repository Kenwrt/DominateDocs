namespace DominateDocsData.Models.RulesEngine;

public sealed class ConditionGroup
{
    // Ordered terms, each term can be a leaf or nested group node.
    // AND/OR is stored on the *previous* term (JoinToNext).
    public List<ConditionTerm> Terms { get; set; } = new();
}
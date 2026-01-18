namespace DominateDocsData.Models.RulesEngine;

public sealed class OutputRule
{
    public Guid Id { get; set; } = Guid.NewGuid();


    public string Name { get; set; } = "New Rule";

    // THEN: generate these (subset of AvailableDocuments)
    public List<Guid> ThenGenerateDocumentIds { get; set; } = new();

    // IF: nested group tree
    public ConditionGroup If { get; set; } = new();
}
using DominateDocsData.Models.RulesEngine;
using MongoDB.Bson.Serialization.Attributes;

namespace DominateDocsData.Models;

[BsonIgnoreExtraElements]
public class LoanType
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid DocLibId { get; set; } = Guid.Parse("533fb231-20f3-4819-8d83-64ede387bd02");

    public string Name { get; set; } = "";

    public string Description { get; set; } = "";

    public string  IconKey { get; set; } = "";
    
    public List<Guid> DefaultDocumentIds { get; set; } = new();

    public List<OutputRule> OutputRules { get; set; } = new();

    public List<Guid> ProductionDocumentIds { get; set; } = new();

}
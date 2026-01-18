using MongoDB.Bson.Serialization.Attributes;

namespace DominateDocsData.Models.RulesEngine;

[BsonDiscriminator("condition")]
public sealed class ConditionLeaf : ConditionNode
{
    // Stable identity for UI + persistence (do NOT regenerate after creation)
    [BsonElement("id")]
    public Guid Id { get; set; } = Guid.NewGuid();

    public Condition Condition { get; set; } = new();
}

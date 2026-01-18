using MongoDB.Bson.Serialization.Attributes;

namespace DominateDocsData.Models.RulesEngine;

[BsonDiscriminator("group")]
public sealed class ConditionGroupNode : ConditionNode
{
    public ConditionGroup Group { get; set; } = new();
}
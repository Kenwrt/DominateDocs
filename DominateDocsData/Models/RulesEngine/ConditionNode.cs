using MongoDB.Bson.Serialization.Attributes;
using System.Text.Json.Serialization;

namespace DominateDocsData.Models.RulesEngine;

[BsonDiscriminator(RootClass = true)]
[BsonKnownTypes(typeof(ConditionLeaf), typeof(ConditionGroupNode))]
[JsonPolymorphic(TypeDiscriminatorPropertyName = "nodeType")]
[JsonDerivedType(typeof(ConditionLeaf), "condition")]
[JsonDerivedType(typeof(ConditionGroupNode), "group")]
public abstract class ConditionNode
{ }
using static DominateDocsData.Models.RulesEngine.Enums.RulesEnums;

namespace DominateDocsData.Models.RulesEngine;

public sealed class Condition
{
    public string FieldKey { get; set; } = ""; // "@State_Generated"

    public ConditionalOperator Operator { get; set; } = ConditionalOperator.Equals;

    // Null/empty allowed depending on operator
    public List<string>? Values { get; set; }

    public FieldValueType? FieldTypeHint { get; set; }
}
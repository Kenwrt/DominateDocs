using static DominateDocsData.Models.RulesEngine.Enums.RulesEnums;

namespace DominateDocsData.Models.RulesEngine;

public sealed class RuleFieldDefinition
{
    public string Key { get; init; } = "";
    public string Label { get; init; } = "";

    public Type DataType { get; init; } = typeof(string);

    public RuleFieldUiType UiType { get; init; } = RuleFieldUiType.Text;

    public IReadOnlySet<ConditionalOperator> AllowedOperators { get; init; } = new HashSet<ConditionalOperator>
    {
        ConditionalOperator.Equals,
        ConditionalOperator.NotEquals,
        ConditionalOperator.In,
        ConditionalOperator.NotIn,
        ConditionalOperator.IsAnswered,
        ConditionalOperator.IsUnanswered
    };

    // For Select/MultiSelect (including enums)
    public Func<IEnumerable<(string Display, string Value)>>? OptionsProvider { get; init; }

    // Optional validation hook (UI-side)
    public Func<string, bool>? ValueValidator { get; init; }

    public FieldValueType FieldValueType => UiType switch
    {
        RuleFieldUiType.Boolean => FieldValueType.Boolean,
        RuleFieldUiType.Date => FieldValueType.Date,
        RuleFieldUiType.Integer or RuleFieldUiType.Decimal => FieldValueType.Number,
        RuleFieldUiType.MultiSelect => FieldValueType.MultiSelect,
        _ => FieldValueType.String
    };
}
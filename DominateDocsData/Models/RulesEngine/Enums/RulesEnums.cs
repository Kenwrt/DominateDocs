namespace DominateDocsData.Models.RulesEngine.Enums;

public class RulesEnums
{
    public enum LogicalOperator
    { And, Or }

    // public enum ConditionOperator { AnyOf, NoneOf, AllOf, IsAnswered, IsUnanswered }
    public enum FieldValueType
    { String, Number, Boolean, Date, MultiSelect }

    public enum RuleFieldUiType
    {
        Text,
        Select,
        MultiSelect,
        Integer,
        Decimal,
        Boolean,
        Date
    }

    public enum ConditionalOperator
    {
        // Legacy (keep so existing saved rules don't explode)

        [System.ComponentModel.Description(" AnyOf")]
        AnyOf,

        [System.ComponentModel.Description("NoneOf")]
        NoneOf,

        [System.ComponentModel.Description("AllOf")]
        AllOf,

        [System.ComponentModel.Description("IsAnswered")]
        IsAnswered,

        [System.ComponentModel.Description("IsUnanswered")]
        IsUnanswered,

        [System.ComponentModel.Description("Equals")]
        Equals,

        [System.ComponentModel.Description("NotEquals")]
        NotEquals,

        [System.ComponentModel.Description("GreaterThan")]
        GreaterThan,

        [System.ComponentModel.Description("GreaterThanOrEqual")]
        GreaterThanOrEqual,

        [System.ComponentModel.Description("LessThan")]
        LessThan,

        [System.ComponentModel.Description("LessThanOrEqual")]
        LessThanOrEqual,

        [System.ComponentModel.Description("In")]
        In,

        [System.ComponentModel.Description("NotIn")]
        NotIn,

        [System.ComponentModel.Description("IsTrue")]
        IsTrue,

        [System.ComponentModel.Description("IsFalse")]
        IsFalse
    }
}
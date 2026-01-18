using DominateDocsData.Enums;
using DominateDocsData.Models.RulesEngine.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using static DominateDocsData.Models.RulesEngine.Enums.RulesEnums;

namespace DominateDocsData.Models.RulesEngine.Fields
{
    public static class RuleFieldRegistry
    {
        private static readonly Dictionary<string, RuleFieldDefinition> _defs =
            new(StringComparer.OrdinalIgnoreCase);

        static RuleFieldRegistry()
        {
            // ----------------------------------------------------
            // Register your non-State fields here (examples only)
            // ----------------------------------------------------
            Register(new RuleFieldDefinition
            {
                Key = "LenderName",
                Label = "Lender Name",
                UiType = RuleFieldUiType.Text,
                DataType = typeof(string),
                AllowedOperators = new HashSet<ConditionalOperator>
                {
                    ConditionalOperator.Equals,
                    ConditionalOperator.NotEquals,
                    ConditionalOperator.In,
                    ConditionalOperator.NotIn
                }
            });

            Register(new RuleFieldDefinition
            {
                Key = "LoanAmount",
                Label = "Loan Amount",
                UiType = RulesEnums.RuleFieldUiType.Decimal,
                DataType = typeof(decimal),
                AllowedOperators = new HashSet<ConditionalOperator>
                {
                    ConditionalOperator.Equals,
                    ConditionalOperator.NotEquals,
                    ConditionalOperator.GreaterThan,
                    ConditionalOperator.GreaterThanOrEqual,
                    ConditionalOperator.LessThan,
                    ConditionalOperator.LessThanOrEqual,
                    ConditionalOperator.In,
                    ConditionalOperator.NotIn
                }
            });

            // ----------------------------------------------------
            // You can explicitly register state fields if you want,
            // but you don't have to because Get() auto-detects *State
            // ----------------------------------------------------
            Register(BuildStateField("LenderState", "Lender State"));
            Register(BuildStateField("PropertyState", "Property State"));
            Register(BuildStateField("BorrowerState", "Borrower State"));
            Register(BuildStateField("GuarantorState", "Guarantor State"));
            Register(BuildStateField("BrokerState", "Broker State"));
        }

        public static void Register(RuleFieldDefinition def)
        {
            if (def is null) throw new ArgumentNullException(nameof(def));
            if (string.IsNullOrWhiteSpace(def.Key)) throw new ArgumentException("RuleFieldDefinition.Key is required.");

            _defs[def.Key] = def;
        }

        public static RuleFieldDefinition Get(string? key)
        {
            if (string.IsNullOrWhiteSpace(key))
                return Unknown("(none)");

            if (_defs.TryGetValue(key, out var def))
                return def;

            // Auto: anything ending in "State" becomes a State multi-option field.
            if (IsStateKey(key))
                return BuildStateField(key, ToLabel(key));

            return Unknown(key);
        }

        public static IReadOnlyList<RuleFieldDefinition> All()
            => _defs.Values.OrderBy(x => x.Label).ToList();

        private static bool IsStateKey(string key)
            => key.EndsWith("State", StringComparison.OrdinalIgnoreCase);

        private static RuleFieldDefinition Unknown(string key) => new RuleFieldDefinition
        {
            Key = key,
            Label = $"Unknown Field ({key})",
            UiType = RuleFieldUiType.Text,
            DataType = typeof(string),
            AllowedOperators = new HashSet<ConditionalOperator>
            {
                ConditionalOperator.Equals,
                ConditionalOperator.NotEquals,
                ConditionalOperator.In,
                ConditionalOperator.NotIn
            }
        };

        private static RuleFieldDefinition BuildStateField(string key, string label) => new RuleFieldDefinition
        {
            Key = key,
            Label = label,
            UiType = RuleFieldUiType.Select,
            DataType = typeof(UsStates.UsState),
            AllowedOperators = new HashSet<ConditionalOperator>
            {
                ConditionalOperator.Equals,
                ConditionalOperator.NotEquals,
                ConditionalOperator.In,
                ConditionalOperator.NotIn
            },
            OptionsProvider = () =>
                Enum.GetValues(typeof(UsStates.UsState))
                    .Cast<UsStates.UsState>()
                    .Where(s => s != UsStates.UsState.None)
                    .Select(s => (Display: GetEnumDescription(s), Value: s.ToString()))
                    .ToList()
        };

        private static string ToLabel(string key)
        {
            // crude but readable: BorrowerState -> Borrower State
            return string.Concat(key.Select((ch, i) =>
                i > 0 && char.IsUpper(ch) ? " " + ch : ch.ToString()));
        }

        private static string GetEnumDescription(Enum value)
        {
            var fi = value.GetType().GetField(value.ToString());
            if (fi == null) return value.ToString();

            var attr = (System.ComponentModel.DescriptionAttribute?)
                Attribute.GetCustomAttribute(fi, typeof(System.ComponentModel.DescriptionAttribute));

            return attr?.Description ?? value.ToString();
        }
    }
}

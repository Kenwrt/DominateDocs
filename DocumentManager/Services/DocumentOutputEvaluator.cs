using DominateDocsData.Models;
using DominateDocsData.Models.RulesEngine;
using DominateDocsData.Models.RulesEngine.Fields;
using System.Globalization;
using static DominateDocsData.Models.RulesEngine.Enums.RulesEnums;

namespace DocumentManager.Services;

public static class DocumentOutputEvaluator
{
    /// <summary>
    /// Fastest: returns only the ordered, de-duped document IDs to generate.
    /// No DTOs, no DB calls, no lookups. Caller decides how to fetch docs.
    /// </summary>
    public static IReadOnlyList<Guid> BuildFinalDocumentIds(
        LoanType loanType,
        IReadOnlyDictionary<string, object?> data)
    {
        var orderedIds = new List<Guid>();
        var seen = new HashSet<Guid>();

        void addId(Guid id)
        {
            if (id == Guid.Empty) return;
            if (!seen.Add(id)) return;
            orderedIds.Add(id);
        }

        // 1) Defaults first
        if (loanType.DefaultDocumentIds is not null)
        {
            foreach (var id in loanType.DefaultDocumentIds)
                addId(id);
        }

        // 2) Then rule-driven docs
        if (loanType.OutputRules is not null)
        {
            foreach (var rule in loanType.OutputRules)
            {
                if (!MatchesGroup(rule.If, data))
                    continue;

                if (rule.ThenGenerateDocumentIds is null)
                    continue;

                foreach (var id in rule.ThenGenerateDocumentIds)
                    addId(id);
            }
        }

        return orderedIds;
    }

    /// <summary>
    /// Returns ordered DTOs by resolving IDs through the provided resolver.
    /// This keeps the evaluator independent from Mongo/DB concerns.
    /// </summary>
    public static IReadOnlyList<DominateDocsData.Models.DTOs.DocumentListDTO> BuildFinalDocumentSet(
        LoanType loanType,
        IReadOnlyDictionary<string, object?> data,
        Func<Guid, DominateDocsData.Models.DTOs.DocumentListDTO?> resolveDtoById)
    {
        if (resolveDtoById is null)
            throw new ArgumentNullException(nameof(resolveDtoById));

        var ids = BuildFinalDocumentIds(loanType, data);

        var result = new List<DominateDocsData.Models.DTOs.DocumentListDTO>(ids.Count);
        foreach (var id in ids)
        {
            var dto = resolveDtoById(id);
            if (dto is not null)
                result.Add(dto);
        }

        return result;
    }

    // ---------------------------
    // Your existing rule evaluator
    // ---------------------------

    private static bool MatchesGroup(ConditionGroup group, IReadOnlyDictionary<string, object?> data)
    {
        if (group.Terms.Count == 0) return true;

        bool evalNode(ConditionNode node) => node switch
        {
            ConditionLeaf leaf => EvaluateCondition(leaf.Condition, data),
            ConditionGroupNode gn => MatchesGroup(gn.Group, data),
            _ => false
        };

        bool acc = evalNode(group.Terms[0].Node);

        for (int i = 1; i < group.Terms.Count; i++)
        {
            var prevJoin = group.Terms[i - 1].JoinToNext;
            var nextVal = evalNode(group.Terms[i].Node);

            acc = prevJoin switch
            {
                LogicalOperator.And => acc && nextVal,
                LogicalOperator.Or => acc || nextVal,
                _ => acc
            };
        }

        return acc;
    }

    private static bool EvaluateCondition(Condition c, IReadOnlyDictionary<string, object?> data)
    {
        data.TryGetValue(c.FieldKey, out var raw);

        var field = RuleFieldRegistry.Get(c.FieldKey);

        // Operator validity (fail closed)
        if (!field.AllowedOperators.Contains(c.Operator))
            return false;

        // Presence checks
        if (c.Operator == ConditionalOperator.IsAnswered)
            return raw is not null && !string.IsNullOrWhiteSpace(raw.ToString());

        if (c.Operator == ConditionalOperator.IsUnanswered)
            return raw is null || string.IsNullOrWhiteSpace(raw.ToString());

        // Boolean checks
        if (c.Operator == ConditionalOperator.IsTrue || c.Operator == ConditionalOperator.IsFalse)
        {
            if (!TryCoerceBool(raw, out var b))
                return false;

            return c.Operator == ConditionalOperator.IsTrue ? b : !b;
        }

        var values = (c.Values ?? new List<string>())
            .Where(v => !string.IsNullOrWhiteSpace(v))
            .Select(v => v.Trim())
            .ToList();

        // Map legacy operators to modern equivalents
        var op = c.Operator switch
        {
            ConditionalOperator.AnyOf => ConditionalOperator.In,
            ConditionalOperator.NoneOf => ConditionalOperator.NotIn,
            _ => c.Operator
        };

        // Multi-value runtime
        if (raw is IEnumerable<string> list)
        {
            var set = new HashSet<string>(list.Where(s => !string.IsNullOrWhiteSpace(s)), StringComparer.OrdinalIgnoreCase);

            return op switch
            {
                ConditionalOperator.In => values.Any(set.Contains),
                ConditionalOperator.NotIn => !values.Any(set.Contains),
                ConditionalOperator.AllOf => values.All(set.Contains),
                ConditionalOperator.Equals => values.Count == 1 && set.Contains(values[0]),
                ConditionalOperator.NotEquals => values.Count == 1 && !set.Contains(values[0]),
                _ => false
            };
        }

        // Enum comparison (ex: UsState)
        if (field.DataType.IsEnum)
            return EvaluateEnum(field.DataType, raw, op, values);

        // Scalar runtime
        var rawStr = raw?.ToString();

        // IN / NOT IN (string membership)
        if (op is ConditionalOperator.In or ConditionalOperator.NotIn)
        {
            if (rawStr is null) return op == ConditionalOperator.NotIn;
            var hit = values.Any(v => string.Equals(v, rawStr, StringComparison.OrdinalIgnoreCase));
            return op == ConditionalOperator.In ? hit : !hit;
        }

        // Equals / NotEquals
        if (op is ConditionalOperator.Equals or ConditionalOperator.NotEquals)
        {
            var equals = EqualsTyped(raw, values);
            return op == ConditionalOperator.Equals ? equals : !equals;
        }

        // Comparisons (<, >, <=, >=)
        if (op is ConditionalOperator.GreaterThan or ConditionalOperator.GreaterThanOrEqual
            or ConditionalOperator.LessThan or ConditionalOperator.LessThanOrEqual)
        {
            var rhs = values.FirstOrDefault();
            if (rhs is null) return false;

            if (TryCoerceDecimal(raw, out var leftDec) && TryCoerceDecimal(rhs, out var rightDec))
            {
                return op switch
                {
                    ConditionalOperator.GreaterThan => leftDec > rightDec,
                    ConditionalOperator.GreaterThanOrEqual => leftDec >= rightDec,
                    ConditionalOperator.LessThan => leftDec < rightDec,
                    ConditionalOperator.LessThanOrEqual => leftDec <= rightDec,
                    _ => false
                };
            }

            if (TryCoerceDateTime(raw, out var leftDt) && TryCoerceDateTime(rhs, out var rightDt))
            {
                return op switch
                {
                    ConditionalOperator.GreaterThan => leftDt > rightDt,
                    ConditionalOperator.GreaterThanOrEqual => leftDt >= rightDt,
                    ConditionalOperator.LessThan => leftDt < rightDt,
                    ConditionalOperator.LessThanOrEqual => leftDt <= rightDt,
                    _ => false
                };
            }

            return false;
        }

        // Legacy fallback
        if (op == ConditionalOperator.AllOf)
            return rawStr != null && values.All(v => string.Equals(v, rawStr, StringComparison.OrdinalIgnoreCase));

        return false;
    }

    private static bool EvaluateEnum(Type enumType, object? raw, ConditionalOperator op, List<string> values)
    {
        if (raw is null)
            return op == ConditionalOperator.NotIn || op == ConditionalOperator.IsUnanswered;

        var rawStr = raw.ToString();
        if (string.IsNullOrWhiteSpace(rawStr))
            return op == ConditionalOperator.NotIn || op == ConditionalOperator.IsUnanswered;

        if (!TryParseEnum(enumType, rawStr!, out var rawEnum))
            return false;

        var rhsEnums = new List<object>();
        foreach (var v in values)
        {
            if (!TryParseEnum(enumType, v, out var rhs))
                return false;
            rhsEnums.Add(rhs!);
        }

        return op switch
        {
            ConditionalOperator.Equals => rhsEnums.Count == 1 && rawEnum!.Equals(rhsEnums[0]),
            ConditionalOperator.NotEquals => rhsEnums.Count == 1 && !rawEnum!.Equals(rhsEnums[0]),
            ConditionalOperator.In => rhsEnums.Any(x => rawEnum!.Equals(x)),
            ConditionalOperator.NotIn => !rhsEnums.Any(x => rawEnum!.Equals(x)),
            _ => false
        };
    }

    private static bool TryParseEnum(Type enumType, string value, out object? parsed)
    {
        parsed = null;
        try
        {
            parsed = Enum.Parse(enumType, value, ignoreCase: true);
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static bool EqualsTyped(object? raw, List<string> values)
    {
        if (values.Count == 0) return raw is null;

        if (values.Count > 1)
        {
            var rawStr = raw?.ToString();
            if (rawStr is null) return false;
            return values.Any(v => string.Equals(v, rawStr, StringComparison.OrdinalIgnoreCase));
        }

        var rhs = values[0];

        if (TryCoerceBool(raw, out var lb) && TryCoerceBool(rhs, out var rb))
            return lb == rb;

        if (TryCoerceDecimal(raw, out var ld) && TryCoerceDecimal(rhs, out var rd))
            return ld == rd;

        if (TryCoerceDateTime(raw, out var ldt) && TryCoerceDateTime(rhs, out var rdt))
            return ldt == rdt;

        if (TryCoerceGuid(raw, out var lg) && TryCoerceGuid(rhs, out var rg))
            return lg == rg;

        var rawStr2 = raw?.ToString();
        return string.Equals(rawStr2, rhs, StringComparison.OrdinalIgnoreCase);
    }

    private static bool TryCoerceBool(object? v, out bool b)
    {
        b = default;

        if (v is bool bb) { b = bb; return true; }

        var s = v?.ToString();
        if (string.IsNullOrWhiteSpace(s)) return false;

        s = s.Trim();
        if (bool.TryParse(s, out b)) return true;

        if (string.Equals(s, "1", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(s, "yes", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(s, "y", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(s, "on", StringComparison.OrdinalIgnoreCase))
        { b = true; return true; }

        if (string.Equals(s, "0", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(s, "no", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(s, "n", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(s, "off", StringComparison.OrdinalIgnoreCase))
        { b = false; return true; }

        return false;
    }

    private static bool TryCoerceDecimal(object? v, out decimal d)
    {
        d = default;

        if (v is decimal dd) { d = dd; return true; }
        if (v is int i) { d = i; return true; }
        if (v is long l) { d = l; return true; }
        if (v is double db) { d = (decimal)db; return true; }
        if (v is float f) { d = (decimal)f; return true; }

        var s = v?.ToString();
        if (string.IsNullOrWhiteSpace(s)) return false;

        return decimal.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out d)
               || decimal.TryParse(s, NumberStyles.Any, CultureInfo.CurrentCulture, out d);
    }

    private static bool TryCoerceDateTime(object? v, out DateTime dt)
    {
        dt = default;

        if (v is DateTime dtt) { dt = dtt; return true; }

        var s = v?.ToString();
        if (string.IsNullOrWhiteSpace(s)) return false;

        return DateTime.TryParse(s, CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out dt)
               || DateTime.TryParse(s, CultureInfo.CurrentCulture, DateTimeStyles.AssumeLocal, out dt);
    }

    private static bool TryCoerceGuid(object? v, out Guid g)
    {
        g = default;

        if (v is Guid gg) { g = gg; return true; }

        var s = v?.ToString();
        if (string.IsNullOrWhiteSpace(s)) return false;

        return Guid.TryParse(s, out g);
    }
}

using DominateDocsData.Models;
using DominateDocsData.Models.RulesEngine;
using System;
using Microsoft.Extensions.Logging;
using System.Reflection;
using static DominateDocsData.Models.RulesEngine.Enums.RulesEnums;

namespace DocumentManager.Services;

/// <summary>
/// Evaluates LoanType DefaultDocuments + OutputRules (ThenGenerate) into a final ordered, de-duped list of DocumentIds.
/// Supports the strongly-typed RulesEngine model (ConditionGroup/ConditionTerm/ConditionLeaf) and a small
/// reflection-based fallback for older shapes.
/// </summary>
public static class DocumentOutputEvaluator
{
    // =========================
    // Public API
    // =========================

    public static IReadOnlyList<Guid> BuildFinalDocumentIdsWithTrace(
        LoanType loanType,
        IReadOnlyDictionary<string, object?> data,
        out string traceText)
    {
        var trace = new System.Collections.Generic.List<string>();
        var result = BuildFinalDocumentIdsInternal(loanType, data, trace);
        traceText = string.Join(Environment.NewLine, trace);
        return result;
    }

    public static IReadOnlyList<Guid> BuildFinalDocumentIds(
        LoanType loanType,
        IReadOnlyDictionary<string, object?> data)
    {
        return BuildFinalDocumentIdsInternal(loanType, data, trace: null);
    }

    // =========================
    // Core evaluation
    // =========================

    private static IReadOnlyList<Guid> BuildFinalDocumentIdsInternal(
        LoanType loanType,
        IReadOnlyDictionary<string, object?> data,
        System.Collections.Generic.List<string>? trace)
    {
        var final = new System.Collections.Generic.List<Guid>();

        // Default docs
        if (loanType.DefaultDocumentIds is not null)
        {
            foreach (var id in loanType.DefaultDocumentIds)
                if (id != Guid.Empty) final.Add(id);
        }

        // ThenGenerate rules
        if (loanType.OutputRules is not null)
        {
            foreach (var r in loanType.OutputRules)
            {
                if (r is null) continue;

                var ok = EvaluateConditionGroup(r.If, data, out var why, trace);
                trace?.Add($"Rule '{r.Name}': IF => {ok} | {why}");

                if (!ok) continue;

                if (r.ThenGenerateDocumentIds is null) continue;

                foreach (var id in r.ThenGenerateDocumentIds)
                    if (id != Guid.Empty) final.Add(id);
            }
        }

        // De-dupe preserve order
        var seen = new System.Collections.Generic.HashSet<Guid>();
        var deduped = new System.Collections.Generic.List<Guid>();
        foreach (var id in final)
        {
            if (seen.Add(id))
                deduped.Add(id);
        }

        return deduped;
    }

    private static bool EvaluateConditionGroup(
        ConditionGroup group,
        IReadOnlyDictionary<string, object?> data,
        out string why,
        System.Collections.Generic.List<string>? trace)
    {
        if (group is null)
        {
            why = "null group";
            return true;
        }

        if (group.Terms is null || group.Terms.Count == 0)
        {
            why = "empty group";
            return true;
        }

        bool acc = EvaluateNode(group.Terms[0].Node, data, out var why0, trace);
        var reasons = new System.Collections.Generic.List<string> { why0 };

        for (int i = 1; i < group.Terms.Count; i++)
        {
            var prevJoin = group.Terms[i - 1].JoinToNext;
            bool next = EvaluateNode(group.Terms[i].Node, data, out var whyn, trace);
            reasons.Add($"{prevJoin}: {whyn}");

            acc = prevJoin switch
            {
                LogicalOperator.Or => acc || next,
                LogicalOperator.And => acc && next,
                _ => acc && next
            };
        }

        why = string.Join(" | ", reasons);
        return acc;
    }

    private static bool EvaluateNode(
        object? node,
        IReadOnlyDictionary<string, object?> data,
        out string why,
        System.Collections.Generic.List<string>? trace)
    {
        if (node is null)
        {
            why = "null node";
            return true;
        }

        if (node is ConditionLeaf leaf)
            return EvaluateLeafCondition(leaf.Condition, data, out why);

        if (node is ConditionGroup group)
            return EvaluateConditionGroup(group, data, out why, trace);

        if (node is ConditionGroupNode gn)
            return EvaluateConditionGroupNode(gn, data, out why, trace);

        why = $"unknown node type {node.GetType().Name}";
        return false;
    }

    private static bool EvaluateConditionGroupNode(
        ConditionGroupNode group,
        IReadOnlyDictionary<string, object?> data,
        out string why,
        System.Collections.Generic.List<string>? trace)
    {
        var termsObj = GetObject(group, "Terms");
        if (termsObj is not System.Collections.IEnumerable ie || termsObj is string)
        {
            why = "group node missing Terms";
            return false;
        }

        var terms = new System.Collections.Generic.List<ConditionTerm>();
        foreach (var t in ie)
        {
            if (t is ConditionTerm ct)
                terms.Add(ct);
        }

        if (terms.Count == 0)
        {
            why = "empty group node";
            return true;
        }

        bool acc = EvaluateNode(terms[0].Node, data, out var why0, trace);
        var reasons = new System.Collections.Generic.List<string> { why0 };

        for (int i = 1; i < terms.Count; i++)
        {
            var prevJoin = terms[i - 1].JoinToNext;
            bool next = EvaluateNode(terms[i].Node, data, out var whyn, trace);
            reasons.Add($"{prevJoin}: {whyn}");

            acc = prevJoin switch
            {
                LogicalOperator.Or => acc || next,
                LogicalOperator.And => acc && next,
                _ => acc && next
            };
        }

        why = string.Join(" | ", reasons);
        return acc;
    }

    private static bool EvaluateLeafCondition(
        object condition,
        IReadOnlyDictionary<string, object?> data,
        out string why)
    {
        var field = GetString(condition, "Field") ?? GetString(condition, "FieldKey") ?? GetString(condition, "Key") ?? GetString(condition, "Name");
        field = field?.Trim();

        if (string.IsNullOrWhiteSpace(field))
        {
            why = "missing field";
            return false;
        }

        var op = (GetString(condition, "Operator") ?? GetString(condition, "Op") ?? "Equals").Trim();
        op = NormalizeOperator(op);

        var values = ExtractStringList(condition, "Values", "Value", "AllowedValues");

        TryGetValueLoose(data, field, out var actualObj);
        var actual = actualObj?.ToString();

        return Compare(actual, values, op, out why, field);
    }

    // =========================
    // Comparison
    // =========================

    private static bool Compare(
        string? actual,
        System.Collections.Generic.List<string> values,
        string opRaw,
        out string why,
        string field)
    {
        var op = (opRaw ?? "Equals").Trim();

        var normalizedValues = NormalizeValues(values);
        var actualTrim = actual?.Trim();

        switch (op.ToLowerInvariant())
        {
            case "equals":
            case "eq":
            case "==":
                {
                    var target = normalizedValues.Count > 0 ? normalizedValues[0] : null;
                    var ok = string.Equals(actualTrim, target, StringComparison.OrdinalIgnoreCase);
                    why = $"{field} == {target} (actual='{actualTrim}')";
                    return ok;
                }

            case "notequals":
            case "neq":
            case "!=":
                {
                    var target = normalizedValues.Count > 0 ? normalizedValues[0] : null;
                    var ok = !string.Equals(actualTrim, target, StringComparison.OrdinalIgnoreCase);
                    why = $"{field} != {target} (actual='{actualTrim}')";
                    return ok;
                }

            case "in":
                {
                    var set = normalizedValues.ToHashSet(StringComparer.OrdinalIgnoreCase);
                    var ok = actualTrim != null && set.Contains(actualTrim);
                    why = $"{field} in [{string.Join(", ", normalizedValues)}] (actual='{actualTrim}')";
                    return ok;
                }

            case "notin":
                {
                    var set = normalizedValues.ToHashSet(StringComparer.OrdinalIgnoreCase);
                    var ok = actualTrim == null || !set.Contains(actualTrim);
                    why = $"{field} not in [{string.Join(", ", normalizedValues)}] (actual='{actualTrim}')";
                    return ok;
                }

            case "istrue":
                {
                    var ok = TryParseBool(actualTrim) == true;
                    why = $"{field} is true (actual='{actualTrim}')";
                    return ok;
                }

            case "isfalse":
                {
                    var ok = TryParseBool(actualTrim) == false;
                    why = $"{field} is false (actual='{actualTrim}')";
                    return ok;
                }

            case "contains":
                {
                    var target = normalizedValues.FirstOrDefault() ?? "";
                    var ok = actualTrim != null && actualTrim.Contains(target, StringComparison.OrdinalIgnoreCase);
                    why = $"{field} contains '{target}' (actual='{actualTrim}')";
                    return ok;
                }

            case "startswith":
                {
                    var target = normalizedValues.FirstOrDefault() ?? "";
                    var ok = actualTrim != null && actualTrim.StartsWith(target, StringComparison.OrdinalIgnoreCase);
                    why = $"{field} startsWith '{target}' (actual='{actualTrim}')";
                    return ok;
                }

            case "endswith":
                {
                    var target = normalizedValues.FirstOrDefault() ?? "";
                    var ok = actualTrim != null && actualTrim.EndsWith(target, StringComparison.OrdinalIgnoreCase);
                    why = $"{field} endsWith '{target}' (actual='{actualTrim}')";
                    return ok;
                }

            default:
                {
                    var target = normalizedValues.FirstOrDefault();
                    var ok = string.Equals(actualTrim, target, StringComparison.OrdinalIgnoreCase);
                    why = $"{field} == {target} (fallback op='{op}', actual='{actualTrim}')";
                    return ok;
                }
        }
    }

    private static bool? TryParseBool(string? s)
    {
        if (string.IsNullOrWhiteSpace(s))
            return null;

        var t = s.Trim();

        if (bool.TryParse(t, out var b))
            return b;

        if (int.TryParse(t, out var i))
            return i != 0;

        if (string.Equals(t, "yes", StringComparison.OrdinalIgnoreCase))
            return true;
        if (string.Equals(t, "no", StringComparison.OrdinalIgnoreCase))
            return false;

        return null;
    }

    private static System.Collections.Generic.List<string> NormalizeValues(System.Collections.Generic.List<string> values)
    {
        return values
            .SelectMany(v => (v ?? "").Split(',', StringSplitOptions.RemoveEmptyEntries))
            .Select(v => v.Trim())
            .Where(v => !string.IsNullOrWhiteSpace(v))
            .ToList();
    }

    // =========================
    // Legacy/Reflection helpers
    // =========================

    private static bool TryGetValueLoose(IReadOnlyDictionary<string, object?> data, string key, out object? value)
    {
        if (data.TryGetValue(key, out value))
            return true;

        var trimmed = key.TrimStart('@');

        if (data.TryGetValue(trimmed, out value))
            return true;

        foreach (var k in data.Keys)
        {
            if (string.Equals(k?.TrimStart('@'), trimmed, StringComparison.OrdinalIgnoreCase))
            {
                value = data[k];
                return true;
            }
        }

        value = null;
        return false;
    }

    private static string NormalizeOperator(string opRaw)
    {
        var raw = (opRaw ?? string.Empty).Trim();

        if (int.TryParse(raw, out var code))
        {
            return code switch
            {
                0 => "AnyOf",
                1 => "NoneOf",
                2 => "AllOf",
                3 => "IsAnswered",
                4 => "IsUnanswered",
                5 => "Equals",
                6 => "NotEquals",
                7 => "GreaterThan",
                8 => "GreaterThanOrEqual",
                9 => "LessThan",
                10 => "LessThanOrEqual",
                11 => "In",
                12 => "NotIn",
                13 => "IsTrue",
                14 => "IsFalse",
                _ => raw
            };
        }

        return raw;
    }

    private static System.Collections.Generic.List<string> ExtractStringList(object obj, params string[] propNames)
    {
        var list = new System.Collections.Generic.List<string>();

        foreach (var prop in propNames)
        {
            var v = GetObject(obj, prop);
            if (v is null) continue;

            if (v is string s)
            {
                list.Add(s);
            }
            else if (v is System.Collections.IEnumerable ie && v is not string)
            {
                foreach (var item in ie)
                {
                    if (item is null) continue;
                    list.Add(item.ToString() ?? "");
                }
            }
        }

        return list;
    }

    private static object? GetObject(object obj, string prop)
    {
        var pi = obj.GetType().GetProperty(prop, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
        return pi?.GetValue(obj);
    }

    private static string? GetString(object obj, string prop)
    {
        return GetObject(obj, prop)?.ToString();
    }
}

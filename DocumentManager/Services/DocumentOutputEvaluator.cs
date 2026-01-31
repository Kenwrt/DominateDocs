using DominateDocsData.Models;
using DominateDocsData.Models.RulesEngine;
using FluentEmail.Core.Models;
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
        out List<string> trace)
    {
        var traceLocal = new List<string>();
        var orderedIds = new List<Guid>();
        var seen = new HashSet<Guid>();

        var startedUtc = DateTime.UtcNow;
        traceLocal.Add($"=== ThenGenerate Trace @ {startedUtc:O} UTC ===");
        traceLocal.Add($"LoanType: {(string.IsNullOrWhiteSpace(loanType?.Name) ? loanType?.Id.ToString() : loanType.Name)}");
        traceLocal.Add($"EvalData: keyCount={(data?.Count ?? 0)} | keys={FormatAvailableKeys(data ?? new Dictionary<string, object?>(), maxKeys: 50)}");
        traceLocal.Add($"KeyCheck: LenderState={(TryGetValueLoose(data, "LenderState", out var ls) ? (ls?.ToString() ?? "<null>") : "<missing>")} (len={(TryGetValueLoose(data, "LenderState", out var ls2) ? (ls2?.ToString() ?? "").Trim().Length : -1)})");
        traceLocal.Add($"KeyCheck: BorrowerState={(TryGetValueLoose(data, "BorrowerState", out var bs) ? (bs?.ToString() ?? "<null>") : "<missing>")}");
        traceLocal.Add($"KeyCheck: BrokerState={(TryGetValueLoose(data, "BrokerState", out var brs) ? (brs?.ToString() ?? "<null>") : "<missing>")}");
        traceLocal.Add("");

        void addId(Guid id, string reason)
        {
            if (id == Guid.Empty) return;

            if (!seen.Add(id))
            {
                traceLocal.Add($"+ skip duplicate docId={id} ({reason})");
                return;
            }

            orderedIds.Add(id);
            traceLocal.Add($"+ add docId={id} ({reason})");
        }

        EvaluateInternal(loanType, data, addId, traceLocal);

        trace = traceLocal;
        return orderedIds;
    }

    public static IReadOnlyList<Guid> BuildFinalDocumentIds(
        LoanType loanType,
        IReadOnlyDictionary<string, object?> evalData)
    {
        var orderedIds = new List<Guid>();
        var seen = new HashSet<Guid>();

        void addId(Guid id, string _)
        {
            if (id == Guid.Empty) return;
            if (seen.Add(id)) orderedIds.Add(id);
        }

        EvaluateInternal(loanType, evalData, addId, trace: null);
        return orderedIds;
    }

    // Convenience overload (your call sites often use Dictionary)
    public static IReadOnlyList<Guid> BuildFinalDocumentIds(
        LoanType loanType,
        Dictionary<string, object?> evalData)
        => BuildFinalDocumentIds(loanType, (IReadOnlyDictionary<string, object?>)evalData);

    // =========================
    // Internal evaluation
    // =========================

    private static void EvaluateInternal(
        LoanType loanType,
        IReadOnlyDictionary<string, object?> data,
        Action<Guid, string> addId,
        List<string>? trace)
    {
        if (loanType == null)
        {
            trace?.Add("! loanType is null");
            return;
        }

        // 1) Default documents
        foreach (var id in ExtractGuidList(loanType, "DefaultDocuments", "DefaultDocumentIds", "DefaultDocIds"))
            addId(id, "default");

        // 2) Output rules (ThenGenerate)
        foreach (var ruleObj in ExtractRuleList(loanType))
        {
            // Strongly-typed path (your current model)
            if (ruleObj is OutputRule rule)
            {
                var ruleName = !string.IsNullOrWhiteSpace(rule.Name) ? rule.Name : rule.Id.ToString();

                trace?.Add($"--- Evaluate Rule: {ruleName} ---");

                var matched = EvaluateRule(rule, data, out var why, trace);
                if (matched)
                {
                    trace?.Add($"= rule matched: {ruleName} ({why})");

                    if (rule.ThenGenerateDocumentIds != null)
                    {
                        foreach (var id in rule.ThenGenerateDocumentIds)
                            addId(id, $"thenGenerate:{ruleName}");
                    }
                }
                else
                {
                    trace?.Add($"- rule did not match: {ruleName} ({why})");
                }

                continue;
            }

            // Reflection fallback (older shapes)
            var name = GetString(ruleObj, "Name") ?? GetString(ruleObj, "Title") ?? GetString(ruleObj, "Id") ?? "rule";

            if (EvaluateRuleReflection(ruleObj, data, out var why2))
            {
                trace?.Add($"= rule matched: {name} ({why2})");

                foreach (var id in ExtractGuidList(ruleObj, "ThenGenerateDocumentIds", "ThenGenerateDocIds", "DocumentIds", "ThenGenerateIds"))
                    addId(id, $"thenGenerate:{name}");
            }
            else
            {
                trace?.Add($"- rule did not match: {name} ({why2})");
            }
        }
    }

    private static IEnumerable<object> ExtractRuleList(LoanType loanType)
    {
        // Prefer strongly typed property if present
        if (loanType.OutputRules != null)
        {
            foreach (var r in loanType.OutputRules)
                if (r != null) yield return r;
            yield break;
        }

        // Fallback to reflection for older shapes
        var rulesObj =
            GetObject(loanType, "OutputRules") ??
            GetObject(loanType, "Rules") ??
            GetObject(loanType, "ThenGenerateRules");

        if (rulesObj is System.Collections.IEnumerable ie && rulesObj is not string)
        {
            foreach (var r in ie)
                if (r != null) yield return r;
        }
    }

    // =========================
    // Strongly typed rules evaluation
    // =========================

    private static bool EvaluateRule(
        OutputRule rule,
        IReadOnlyDictionary<string, object?> data,
        out string why,
        List<string>? trace)
    {
        if (rule.If == null)
        {
            why = "no conditions";
            return true;
        }

        var ok = EvaluateConditionGroup(rule.If, data, out why, trace);
        return ok;
    }

    private static bool EvaluateConditionGroup(
        ConditionGroup group,
        IReadOnlyDictionary<string, object?> data,
        out string why,
        List<string>? trace)
    {
        if (group.Terms == null || group.Terms.Count == 0)
        {
            why = "empty group";
            return true; // empty group matches
        }

        // Each ConditionTerm stores JoinToNext (LogicalOperator) which indicates how THIS term joins to the NEXT term.
        // Fold:
        //  - acc = term[0]
        //  - for i=1..n-1 apply term[i-1].JoinToNext between acc and term[i]
        bool acc = EvaluateNode(group.Terms[0].Node, data, out var why0, trace);
        var reasons = new List<string> { why0 };

        for (int i = 1; i < group.Terms.Count; i++)
        {
            var prevJoin = group.Terms[i - 1].JoinToNext;
            bool next = EvaluateNode(group.Terms[i].Node, data, out var whyn, trace);
            reasons.Add($"{prevJoin}: {whyn}");

            // prevJoin is a VALUE (LogicalOperator), not a type.
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

    private static bool EvaluateConditionGroupNode(
        ConditionGroupNode group,
        IReadOnlyDictionary<string, object?> data,
        out string why,
        List<string>? trace)
    {
        // ConditionGroupNode should have Terms like ConditionGroup.
        // Use reflection access to avoid coupling to internal implementation details.
        var termsObj = GetObject(group, "Terms");
        if (termsObj is not System.Collections.IEnumerable ie || termsObj is string)
        {
            why = "group node missing Terms";
            return false;
        }

        var terms = new List<ConditionTerm>();
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
        var reasons = new List<string> { why0 };

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

    private static bool EvaluateNode(
        object? node,
        IReadOnlyDictionary<string, object?> data,
        out string why,
        List<string>? trace)
    {
        if (node is null)
        {
            why = "null node";
            return false;
        }

        if (node is ConditionLeaf leaf)
            return EvaluateLeaf(leaf, data, out why, trace);

        if (node is ConditionGroup group)
            return EvaluateConditionGroup(group, data, out why, trace);

        // Some persisted shapes use a polymorphic node type ConditionGroupNode.
        if (node is ConditionGroupNode groupNode)
            return EvaluateConditionGroupNode(groupNode, data, out why, trace);

        // Defensive: unknown node shape -> reflection fallback
        return EvaluateConditionReflection(node, data, out why);
    }

    private static bool EvaluateLeaf(
        ConditionLeaf leaf,
        IReadOnlyDictionary<string, object?> data,
        out string why,
        List<string>? trace)
    {
        var cond = leaf.Condition;
        if (cond == null)
        {
            why = "leaf missing Condition";
            return false;
        }

        var field = (cond.FieldKey ?? "").Trim();
        if (string.IsNullOrWhiteSpace(field))
        {
            why = $"missing field key (Condition.FieldKey is empty). Available data keys: {FormatAvailableKeys(data)}";
            trace?.Add($"! missing field key on leaf. Available data keys: {FormatAvailableKeys(data)}");
            return false;
        }

        // Operator can come through as named enum value or numeric code.
        var opRaw = cond.Operator.ToString();
        if (string.IsNullOrWhiteSpace(opRaw)) opRaw = "Equals";
        var op = NormalizeOperator(opRaw);

        var values = new List<string>();
        if (cond.Values != null)
        {
            foreach (var v in cond.Values)
                values.Add(v?.ToString() ?? "");
        }

        if (!TryGetValueLoose(data, field, out var actualObj))
        {
            why = $"missing data key '{field}' (Available data keys: {FormatAvailableKeys(data)})";
            trace?.Add($"! missing data key '{field}'. Available data keys: {FormatAvailableKeys(data)}");
            trace?.Add($"! debug fieldKey=[{field}] len={field.Length}");
            return false;
        }

        var actual = actualObj?.ToString();

        var ok = Compare(actual, values, op, out why, field);

        trace?.Add($"  • fieldKey=[{field}] len={field.Length} opRaw='{opRaw}' opNorm='{op}' values=[{string.Join(", ", NormalizeValues(values))}] actual='{actual?.Trim()}' => {ok} | {why}");

        return ok;
    }

    // =========================
    // Reflection fallback evaluation
    // =========================

    private static bool EvaluateRuleReflection(
        object rule,
        IReadOnlyDictionary<string, object?> data,
        out string why)
    {
        var conditionsObj =
            GetObject(rule, "Conditions") ??
            GetObject(rule, "FieldConditions") ??
            GetObject(rule, "If") ??
            GetObject(rule, "When");

        if (conditionsObj is null)
        {
            why = "no conditions";
            return true;
        }

        if (conditionsObj is ConditionGroup cg)
            return EvaluateConditionGroup(cg, data, out why, trace: null);

        if (conditionsObj is not System.Collections.IEnumerable ie || conditionsObj is string)
            return EvaluateConditionReflection(conditionsObj, data, out why);

        var allOk = true;
        var reasons = new List<string>();

        foreach (var cond in ie)
        {
            if (cond is null) continue;

            var ok = EvaluateConditionReflection(cond, data, out var r);
            reasons.Add(r);

            if (!ok)
                allOk = false;
        }

        why = string.Join("; ", reasons);
        return allOk;
    }

    private static bool EvaluateConditionReflection(
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
        List<string> values,
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
                    var target = normalizedValues.FirstOrDefault();
                    var ok = string.Equals(actualTrim, target, StringComparison.OrdinalIgnoreCase);
                    why = $"{field} == {target} (actual='{actualTrim}')";
                    return ok;
                }

            case "notequals":
            case "neq":
            case "!=":
                {
                    var target = normalizedValues.FirstOrDefault();
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

    private static List<string> NormalizeValues(List<string> values)
    {
        return values
            .SelectMany(v => (v ?? "").Split(',', StringSplitOptions.RemoveEmptyEntries))
            .Select(v => v.Trim())
            .Where(v => !string.IsNullOrWhiteSpace(v))
            .ToList();
    }

    private static string FormatAvailableKeys(IReadOnlyDictionary<string, object?> data, int maxKeys = 25)
    {
        try
        {
            var keys = data.Keys
                .Where(k => !string.IsNullOrWhiteSpace(k))
                .OrderBy(k => k, StringComparer.OrdinalIgnoreCase)
                .Take(maxKeys)
                .ToList();

            var total = data.Keys.Count();
            var suffix = total > keys.Count ? $"…(+{total - keys.Count} more)" : string.Empty;

            return keys.Count == 0 ? "<none>" : string.Join(", ", keys) + suffix;
        }
        catch
        {
            return "<unavailable>";
        }
    }

    // =========================
    // Debug helpers (Admin Bench)
    // =========================

    private static bool TryGetValueLoose(IReadOnlyDictionary<string, object?> data, string key, out object? value)
    {
        value = null;

        if (data is null) return false;

        key = (key ?? string.Empty).Trim();
        if (key.Length == 0) return false;

        // Exact first
        if (data.TryGetValue(key, out value))
            return true;

        // Loose: trim + ignore case
        foreach (var k in data.Keys)
        {
            if (string.Equals((k ?? string.Empty).Trim(), key, StringComparison.OrdinalIgnoreCase))
            {
                value = data[k];
                return true;
            }
        }

        return false;
    }

    private static string NormalizeOperator(string opRaw)
    {
        var raw = (opRaw ?? string.Empty).Trim();

        // If it comes through numeric, map the known ConditionalOperator codes
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

    // =========================
    // Extraction helpers (reflection)
    // =========================

    private static IEnumerable<Guid> ExtractGuidList(object obj, params string[] propNames)
    {
        foreach (var prop in propNames)
        {
            var v = GetObject(obj, prop);
            if (v is null) continue;

            if (v is Guid g)
                yield return g;
            else if (v is System.Collections.IEnumerable ie && v is not string)
            {
                foreach (var item in ie)
                {
                    if (item is null) continue;
                    if (item is Guid ig) yield return ig;

                    var gid =
                        GetGuid(item, "DocumentId") ??
                        GetGuid(item, "DocId") ??
                        GetGuid(item, "Id");

                    if (gid.HasValue) yield return gid.Value;
                }
            }
        }
    }

    private static List<string> ExtractStringList(object obj, params string[] propNames)
    {
        foreach (var prop in propNames)
        {
            var v = GetObject(obj, prop);

            if (v is null) continue;

            if (v is string s)
                return new List<string> { s };

            if (v is System.Collections.IEnumerable ie && v is not string)
            {
                var list = new List<string>();
                foreach (var item in ie)
                    list.Add(item?.ToString() ?? "");
                return list;
            }
        }

        return new List<string>();
    }

    private static object? GetObject(object obj, string propName)
    {
        try
        {
            var pi = obj.GetType().GetProperty(propName);
            return pi?.GetValue(obj);
        }
        catch { return null; }
    }

    private static string? GetString(object obj, string propName)
        => GetObject(obj, propName)?.ToString();

    private static Guid? GetGuid(object obj, string propName)
    {
        var v = GetObject(obj, propName);
        if (v is null) return null;

        if (v is Guid g) return g;
        if (Guid.TryParse(v.ToString(), out var parsed)) return parsed;

        return null;
    }
}

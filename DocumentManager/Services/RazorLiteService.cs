using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using Microsoft.Extensions.Logging;
using RazorLight;
using RazorLight.Compilation;
using System.Collections;
using System.Collections.Concurrent;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Dynamic;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;

//using System.Collections;
//using System.Collections.Concurrent;
//using System.ComponentModel;
//using System.ComponentModel.DataAnnotations;
//using System.Dynamic;
//using System.Reflection;
//using System.Text;
//using System.Text.RegularExpressions;
//using DocumentFormat.OpenXml;
//using DocumentFormat.OpenXml.Packaging;
//using DocumentFormat.OpenXml.Wordprocessing;
//using Microsoft.Extensions.Logging;
//using RazorLight;
//using RazorLight.Compilation;

public class RazorLiteService : IRazorLiteService
{
    // [[IF <expr>]] ... [[ENDIF]]
    private readonly Regex IfStart =
        new(@"^\s*\[\[IF\s+(.+?)\]\]\s*$", RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private const string EndIfToken = "[[ENDIF]]";

    // Skip paragraphs that contain Word page-number fields
    private readonly Regex PageFieldRx =
        new(@"\b(PAGE|NUMPAGES|SECTIONPAGES)\b", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    // [[FOREACH <var> in <Root.Path>]] ... [[ENDFOREACH]]
    private readonly Regex ForeachStart =
        new(@"^\s*\[\[FOREACH\s+([A-Za-z_][A-Za-z0-9_]*)\s+in\s+(.+?)\]\]\s*$",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private const string EndForeachToken = "[[ENDFOREACH]]";

    private readonly ILogger<RazorLiteService> logger;

    private readonly RazorLightEngine _engine =
        new RazorLightEngineBuilder().UseMemoryCachingProvider().Build();

    public RazorLiteService(ILogger<RazorLiteService> logger)
    {
        this.logger = logger;
    }

    public async Task ProcessAsync(string inputPath, string outputPath, object model)
    {
        try
        {
            var rootName = model.GetType().Name;
            File.Copy(inputPath, outputPath, true);
            using var doc = WordprocessingDocument.Open(outputPath, true);
            await ProcessAllStoriesAsync(doc, model, rootName);
            doc.MainDocumentPart!.Document.Save();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "ProcessAsync file failed.");
            throw;
        }
    }

    public async Task<MemoryStream> ProcessAsync(MemoryStream ms, object model)
    {
        try
        {
            var rootName = model.GetType().Name;
            ms.Position = 0;
            using (var doc = WordprocessingDocument.Open(ms, true))
            {
                await ProcessAllStoriesAsync(doc, model, rootName);
                doc.MainDocumentPart!.Document.Save();
            }
            ms.Position = 0;
            return ms;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "ProcessAsync stream failed.");
            throw;
        }
    }

    // ---------- Orchestrate across body/headers/footers/text boxes ----------

    private async Task ProcessAllStoriesAsync(WordprocessingDocument doc, object model, string rootName)
    {
        var mdp = doc.MainDocumentPart!;
        logger.LogDebug("Starting merge. Root model: {Root}", rootName);

        await ProcessContainerAsync(mdp.Document.Body!, model, rootName, "Body");

        foreach (var (part, idx) in mdp.HeaderParts.Select((p, i) => (p, i)))
        {
            logger.LogDebug("Processing HeaderPart[{Idx}]", idx);
            await ProcessContainerAsync(part.RootElement!, model, rootName, $"Header[{idx}]");
        }

        foreach (var (part, idx) in mdp.FooterParts.Select((p, i) => (p, i)))
        {
            logger.LogDebug("Processing FooterPart[{Idx}]", idx);
            await ProcessContainerAsync(part.RootElement!, model, rootName, $"Footer[{idx}]");
        }

        foreach (var (root, i) in EnumerateTextBoxRoots(mdp).Select((r, i) => (r, i)))
        {
            logger.LogDebug("Processing TextBox[{Idx}]", i);
            await ProcessContainerAsync(root, model, rootName, $"TextBox[{i}]");
        }

        logger.LogDebug("Merge complete.");
    }

    // ---------- Extractors ----------

    // For readable logs: show \t and \n tokens so you can spot layout junk
    private string GetParagraphDebugText(Paragraph p)
    {
        var sb = new StringBuilder();
        foreach (var e in p.Descendants())
        {
            switch (e)
            {
                case Text t:
                    sb.Append(t.Text);
                    break;

                case FieldCode fc:       // w:instrText
                    sb.Append(fc.Text);
                    break;

                case Break:
                    sb.Append("\\n");
                    break;

                case TabChar:
                    sb.Append("\\t");
                    break;
            }
        }
        return sb.ToString()
                 .Replace('\u00A0', ' ')
                 .Replace("\u200B", "");
    }

    // For matching tokens: tabs/breaks become whitespace, collapse it, trim
    private string GetParagraphMatchText(Paragraph p)
    {
        var sb = new StringBuilder();
        foreach (var e in p.Descendants())
        {
            switch (e)
            {
                case Text t:
                    sb.Append(t.Text);
                    break;

                case FieldCode fc:
                    sb.Append(fc.Text);
                    break;

                case Break:
                case TabChar:
                    sb.Append(' ');
                    break;
            }
        }

        var s = sb.ToString()
                  .Replace('\u00A0', ' ')
                  .Replace("\u200B", "")
                  .Replace("\u2009", " ");

        s = Regex.Replace(s, @"\s+", " ").Trim();
        return s;
    }

    private bool IsPageFieldParagraph(Paragraph p)
    {
        // Simple fields: <w:fldSimple w:instr="PAGE \* MERGEFORMAT">
        if (p.Descendants<SimpleField>().Any(sf => PageFieldRx.IsMatch(sf.Instruction?.Value ?? string.Empty)))
            return true;

        // Complex fields: runs contain <w:instrText> (SDK: FieldCode) with "PAGE ..." etc.
        if (p.Descendants<FieldCode>().Any(fc => PageFieldRx.IsMatch(fc.Text ?? string.Empty)))
            return true;

        return false;
    }

    private IEnumerable<OpenXmlElement> EnumerateTextBoxRoots(OpenXmlPartContainer container)
    {
        foreach (var partRef in container.Parts)
        {
            var part = partRef.OpenXmlPart;
            if (part?.RootElement is null) continue;

            foreach (var tb in part.RootElement.Descendants()
                         .Where(e => e.LocalName == "txbxContent")) // w:txbxContent
            {
                yield return tb;
            }

            foreach (var nested in EnumerateTextBoxRoots(part)) yield return nested;
        }
    }

    // ---------- Core processing for a single container ----------

    private async Task ProcessContainerAsync(OpenXmlElement containerRoot, object model, string rootName, string scopeName)
    {
        var paragraphs = containerRoot.Descendants<Paragraph>().ToList();
        if (paragraphs.Count == 0)
        {
            logger.LogDebug("[{Scope}] No paragraphs found.", scopeName);
            return;
        }

        logger.LogDebug("[{Scope}] Paragraphs discovered: {Count}", scopeName, paragraphs.Count);

        var toRemove = new HashSet<OpenXmlElement>();
        int i = 0;

        while (i < paragraphs.Count)
        {
            var p = paragraphs[i];
            var rawLog = GetParagraphDebugText(p);
            var norm = NormalizeForToken(GetParagraphMatchText(p));

            logger.LogDebug("[{Scope}] Raw p#{I}: {Raw}", scopeName, i, rawLog);

            // IF block
            var ifMatch = IfStart.Match(norm);
            if (ifMatch.Success)
            {
                logger.LogDebug("[{Scope}] IF start at p#{I}: '{Norm}'", scopeName, i, norm);

                var cond = ifMatch.Groups[1].Value.Trim();
                var inner = new List<Paragraph>();
                var innerInfo = new List<(int idx, string raw, string norm)>();
                int j = i + 1;

                for (; j < paragraphs.Count; j++)
                {
                    var rawInner = GetParagraphDebugText(paragraphs[j]);
                    var normInner = NormalizeForToken(GetParagraphMatchText(paragraphs[j]));
                    if (TokenEquals(normInner, EndIfToken)) break;
                    inner.Add(paragraphs[j]);
                    innerInfo.Add((j, rawInner, normInner));
                }

                if (j >= paragraphs.Count)
                {
                    logger.LogWarning("[{Scope}] Malformed IF: missing [[ENDIF]]. Aborting IF at p#{I}.", scopeName, i);
                    break;
                }

                var keep = await EvalIfAsync(cond, model, rootName, null, null);
                logger.LogDebug("[{Scope}] IF evaluated '{Cond}' => {Keep}. Inner count = {Cnt}.",
                    scopeName, cond, keep, inner.Count);

                foreach (var info in innerInfo)
                    logger.LogDebug("[{Scope}]   inner p#{Idx} raw: {Raw}", scopeName, info.idx, info.raw);

                if (!keep)
                {
                    foreach (var para in inner) toRemove.Add(para);
                    logger.LogDebug("[{Scope}] IF false: queued {Count} inner paragraphs for removal.", scopeName, inner.Count);
                }

                toRemove.Add(p);
                toRemove.Add(paragraphs[j]);
                i = j + 1;
                continue;
            }

            // FOREACH block
            var feMatch = ForeachStart.Match(norm);
            if (feMatch.Success)
            {
                var loopVar = feMatch.Groups[1].Value.Trim();
                var enumerableExpr = feMatch.Groups[2].Value.Trim();
                logger.LogDebug("[{Scope}] FOREACH start at p#{I}: var={Var}, expr={Expr}", scopeName, i, loopVar, enumerableExpr);

                var stencil = new List<Paragraph>();
                int j = i + 1;

                for (; j < paragraphs.Count; j++)
                {
                    var t = NormalizeForToken(GetParagraphMatchText(paragraphs[j]));
                    if (TokenEquals(t, EndForeachToken)) break;
                    stencil.Add(paragraphs[j]);
                }

                if (j >= paragraphs.Count)
                {
                    logger.LogWarning("[{Scope}] Malformed FOREACH: missing [[ENDFOREACH]]. Aborting at p#{I}.", scopeName, i);
                    break;
                }

                var sequence = ResolveEnumerable(model, enumerableExpr, rootName) ?? Array.Empty<object>();
                logger.LogDebug("[{Scope}] FOREACH sequence resolved to {Count} items.", scopeName, sequence.Count());

                var insertAfter = paragraphs[j]; // append clones after ENDFOREACH
                foreach (var item in sequence)
                {
                    var clones = stencil.Select(s => (Paragraph)s.CloneNode(true)).ToList();

                    // Handle nested blocks inside the clones first
                    await ProcessNestedBlocksInMemoryAsync(clones, model, rootName, loopVar, item, scopeName);

                    // Scalar replacement in each cloned paragraph (skip page fields)
                    foreach (var clone in clones)
                    {
                        if (!IsPageFieldParagraph(clone))
                        {
                            PlaceholderReplacer.ReplaceInParagraph(clone,
                                key => ResolveKey(key, model, rootName, loopVar, item));
                            SimpleTabReplacer.ReplaceInParagraph(clone);
                        }

                        insertAfter.InsertAfterSelf(clone);
                        insertAfter = clone;
                    }
                }

                // Remove markers and stencil after expansion
                toRemove.Add(paragraphs[i]);
                toRemove.Add(paragraphs[j]);
                foreach (var s in stencil) toRemove.Add(s);

                i = j + 1;
                continue;
            }

            // Plain paragraph: root-scoped replacements (skip page field paragraphs)
            if (!IsPageFieldParagraph(p))
            {
                PlaceholderReplacer.ReplaceInParagraph(p, key => ResolveKey(key, model, rootName, null, null));
                SimpleTabReplacer.ReplaceInParagraph(p);
            }
            else
            {
                logger.LogDebug("[{Scope}] Skipping paragraph with page field.", scopeName);
            }

            i++;
        }

        foreach (var rem in toRemove) rem.Remove();

        // ---------- bounded extra passes ----------
        const int MaxExtraPasses = 2;
        for (int pass = 1; pass <= MaxExtraPasses; pass++)
        {
            var before = containerRoot.OuterXml;

            var paragraphs2 = containerRoot.Descendants<Paragraph>().ToList();
            if (paragraphs2.Count == 0) break;

            var toRemove2 = new HashSet<OpenXmlElement>();
            int i2 = 0;

            while (i2 < paragraphs2.Count)
            {
                var p2 = paragraphs2[i2];
                var norm2 = NormalizeForToken(GetParagraphMatchText(p2));

                // IF
                var ifMatch2 = IfStart.Match(norm2);
                if (ifMatch2.Success)
                {
                    var cond2 = ifMatch2.Groups[1].Value.Trim();
                    var inner2 = new List<Paragraph>();
                    int j2 = i2 + 1;

                    for (; j2 < paragraphs2.Count; j2++)
                    {
                        var t2 = NormalizeForToken(GetParagraphMatchText(paragraphs2[j2]));
                        if (TokenEquals(t2, EndIfToken)) break;
                        inner2.Add(paragraphs2[j2]);
                    }
                    if (j2 >= paragraphs2.Count) { logger.LogWarning("[{Scope}] Malformed IF in extra pass.", scopeName); break; }

                    var keep2 = await EvalIfAsync(cond2, model, rootName, null, null);
                    if (!keep2) foreach (var ip in inner2) toRemove2.Add(ip);
                    toRemove2.Add(paragraphs2[i2]);
                    toRemove2.Add(paragraphs2[j2]);

                    i2 = j2 + 1;
                    continue;
                }

                // FOREACH
                var feMatch2 = ForeachStart.Match(norm2);
                if (feMatch2.Success)
                {
                    var loopVar2 = feMatch2.Groups[1].Value.Trim();
                    var expr2 = feMatch2.Groups[2].Value.Trim();

                    var stencil2 = new List<Paragraph>();
                    int j2 = i2 + 1;
                    for (; j2 < paragraphs2.Count; j2++)
                    {
                        var t2 = NormalizeForToken(GetParagraphMatchText(paragraphs2[j2]));
                        if (TokenEquals(t2, EndForeachToken)) break;
                        stencil2.Add(paragraphs2[j2]);
                    }
                    if (j2 >= paragraphs2.Count) { logger.LogWarning("[{Scope}] Malformed FOREACH in extra pass.", scopeName); break; }

                    var seq2 = ResolveEnumerable(model, expr2, rootName) ?? Array.Empty<object>();
                    var insertAfter2 = paragraphs2[j2];

                    foreach (var item2 in seq2)
                    {
                        var clones2 = stencil2.Select(s => (Paragraph)s.CloneNode(true)).ToList();
                        await ProcessNestedBlocksInMemoryAsync(clones2, model, rootName, loopVar2, item2, "ExtraPass");

                        foreach (var clone2 in clones2)
                        {
                            if (!IsPageFieldParagraph(clone2))
                            {
                                PlaceholderReplacer.ReplaceInParagraph(clone2,
                                    key => ResolveKey(key, model, rootName, loopVar2, item2));
                                SimpleTabReplacer.ReplaceInParagraph(clone2);
                            }

                            insertAfter2.InsertAfterSelf(clone2);
                            insertAfter2 = clone2;
                        }
                    }

                    toRemove2.Add(paragraphs2[i2]);
                    toRemove2.Add(paragraphs2[j2]);
                    foreach (var s2 in stencil2) toRemove2.Add(s2);

                    i2 = j2 + 1;
                    continue;
                }

                // Plain (skip page-field paragraphs)
                if (!IsPageFieldParagraph(p2))
                {
                    PlaceholderReplacer.ReplaceInParagraph(p2, key => ResolveKey(key, model, rootName, null, null));
                    SimpleTabReplacer.ReplaceInParagraph(p2);
                }

                i2++;
            }

            foreach (var rem2 in toRemove2) rem2.Remove();

            var after = containerRoot.OuterXml;
            if (string.Equals(before, after, StringComparison.Ordinal))
            {
                logger.LogDebug("[{Scope}] No structural changes in extra pass #{Pass}; stopping.", scopeName, pass);
                break;
            }

            bool markersRemain = containerRoot.Descendants<Paragraph>().Any(pp =>
            {
                var s = NormalizeForToken(GetParagraphMatchText(pp));
                return IfStart.IsMatch(s) || ForeachStart.IsMatch(s)
                       || TokenEquals(s, EndIfToken) || TokenEquals(s, EndForeachToken);
            });

            logger.LogDebug("[{Scope}] Extra pass #{Pass} changed XML. Markers remain: {Remain}",
                scopeName, pass, markersRemain);

            if (!markersRemain) break;
        }

        // Log if markers still remain
        bool leftover = containerRoot.Descendants<Paragraph>().Any(p =>
        {
            var s = NormalizeForToken(GetParagraphMatchText(p));
            return IfStart.IsMatch(s) || ForeachStart.IsMatch(s)
                   || TokenEquals(s, EndIfToken) || TokenEquals(s, EndForeachToken);
        });
        if (leftover)
        {
            logger.LogWarning("[{Scope}] Markers remain after bounded passes. Likely malformed blocks or tokens split across objects. Continuing.", scopeName);
        }
    }

    private async Task ProcessNestedBlocksInMemoryAsync(
        List<Paragraph> paras, object model, string rootName, string loopVar, object loopItem, string scopeName)
    {
        var toRemove = new HashSet<OpenXmlElement>();
        int k = 0;

        while (k < paras.Count)
        {
            var txt = NormalizeForToken(GetParagraphMatchText(paras[k]));

            // Nested IF
            var ifMatch = IfStart.Match(txt);
            if (ifMatch.Success)
            {
                var cond = ifMatch.Groups[1].Value.Trim();
                var inner = new List<Paragraph>();
                int m = k + 1;

                for (; m < paras.Count; m++)
                {
                    var t = NormalizeForToken(GetParagraphMatchText(paras[m]));
                    if (TokenEquals(t, EndIfToken)) break;
                    inner.Add(paras[m]);
                }
                if (m >= paras.Count)
                {
                    logger.LogWarning("[{Scope}] Nested IF missing [[ENDIF]] within loop block.", scopeName);
                    break;
                }

                var keep = await EvalIfAsync(cond, model, rootName, loopVar, loopItem);
                logger.LogDebug("[{Scope}] Nested IF '{Cond}' => {Keep}. Inner count = {Cnt}", scopeName, cond, keep, inner.Count);

                if (!keep) foreach (var rp in inner) toRemove.Add(rp);

                toRemove.Add(paras[k]);  // [[IF...]]
                toRemove.Add(paras[m]);  // [[ENDIF]]

                k = m + 1;
                continue;
            }

            // Nested FOREACH
            var feMatch = ForeachStart.Match(txt);
            if (feMatch.Success)
            {
                var childVar = feMatch.Groups[1].Value.Trim();
                var childExpr = feMatch.Groups[2].Value.Trim();

                var stencil = new List<Paragraph>();
                int m = k + 1;

                for (; m < paras.Count; m++)
                {
                    var t = NormalizeForToken(GetParagraphMatchText(paras[m]));
                    if (TokenEquals(t, EndForeachToken)) break;
                    stencil.Add(paras[m]);
                }
                if (m >= paras.Count)
                {
                    logger.LogWarning("[{Scope}] Nested FOREACH missing [[ENDFOREACH]] within loop block.", scopeName);
                    break;
                }

                var seq = ResolveEnumerableForLoopScope(model, loopVar, loopItem, childExpr, rootName) ?? Array.Empty<object>();

                var insertionIndex = m + 1;
                var newParas = new List<Paragraph>();

                foreach (var itm in seq)
                {
                    var clones = stencil.Select(s => (Paragraph)s.CloneNode(true)).ToList();
                    await ProcessNestedBlocksInMemoryAsync(clones, model, rootName, childVar, itm, scopeName);

                    foreach (var clone in clones)
                    {
                        if (!IsPageFieldParagraph(clone))
                        {
                            PlaceholderReplacer.ReplaceInParagraph(clone,
                                key => ResolveKey(key, model, rootName, childVar, itm));
                            SimpleTabReplacer.ReplaceInParagraph(clone);
                        }
                        newParas.Add(clone);
                    }
                }

                var range = paras.GetRange(k, (m - k) + 1);
                foreach (var r in range) toRemove.Add(r);
                paras.InsertRange(insertionIndex, newParas);

                k = insertionIndex + newParas.Count;
                continue;
            }

            // Plain paragraph inside nested list: scalars at loop scope (skip page fields)
            if (!IsPageFieldParagraph(paras[k]))
            {
                PlaceholderReplacer.ReplaceInParagraph(paras[k],
                    key => ResolveKey(key, model, rootName, loopVar, loopItem));
                SimpleTabReplacer.ReplaceInParagraph(paras[k]);
            }

            k++;
        }

        foreach (var rem in toRemove) paras.Remove(rem as Paragraph);
    }

    // ---------- Utilities ----------

    private static string NormalizeForToken(string s) =>
        (s ?? string.Empty)
            .Replace('\u00A0', ' ')
            .Replace("\u200B", "")
            .Replace("\u2009", " ")
            .Trim();

    private static bool TokenEquals(string s, string token) =>
        string.Equals(NormalizeForToken(s), token, StringComparison.Ordinal);

    // ---------- Placeholder & path resolution ----------

    private string? ResolveKey(string key, object rootModel, string rootName, string? loopVar, object? loopItem)
    {
        try
        {
            if (string.Equals(key, "TAB", StringComparison.OrdinalIgnoreCase))
                return "\t";

            if (!string.IsNullOrWhiteSpace(loopVar) &&
                key.StartsWith(loopVar + ".", StringComparison.OrdinalIgnoreCase) &&
                loopItem is not null)
            {
                return ResolvePath(loopItem, key[(loopVar.Length + 1)..]);
            }

            if (key.StartsWith(rootName + ".", StringComparison.Ordinal))
            {
                var sub = key[(rootName.Length + 1)..];
                return ResolvePath(rootModel, sub);
            }

            logger.LogWarning("Placeholder '{Key}' ignored; must start with '{RootName}.' or '{LoopVar}.'",
                key, rootName, loopVar);
            return null;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "ResolveKey failed for {Key}", key);
            return null;
        }
    }

    private IEnumerable<object>? ResolveEnumerable(object model, string expr, string rootName)
    {
        try
        {
            var path = expr.Trim();
            if (!path.StartsWith(rootName + ".", StringComparison.OrdinalIgnoreCase))
            {
                logger.LogWarning("FOREACH source '{Expr}' ignored; must start with '{RootName}.'", expr, rootName);
                return null;
            }

            var sub = path.Substring(rootName.Length + 1);
            var value = ResolvePathRaw(model, sub);
            return (value as IEnumerable)?.Cast<object>();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "ResolveEnumerable failed for: {Expr}", expr);
            return null;
        }
    }

    private IEnumerable<object>? ResolveEnumerableForLoopScope(object model, string loopVar, object loopItem, string expr, string rootName)
    {
        var path = expr.Trim();

        if (path.StartsWith(loopVar + ".", StringComparison.OrdinalIgnoreCase))
        {
            var sub = path[(loopVar.Length + 1)..];
            var value = ResolvePathRaw(loopItem, sub);
            return (value as IEnumerable)?.Cast<object>();
        }

        return ResolveEnumerable(model, expr, rootName);
    }

    private string? ResolvePath(object? obj, string path)
    {
        var value = ResolvePathRaw(obj, path);
        return value?.ToString();
    }

    // ----- Enum description support (.Description / .Name / .Value) -----

    private static readonly ConcurrentDictionary<Type, ConcurrentDictionary<object, string>> _enumDescCache
        = new();

    private static string GetEnumDescriptionString(object enumObj)
    {
        var type = enumObj.GetType();
        var map = _enumDescCache.GetOrAdd(type, _ => new ConcurrentDictionary<object, string>());

        return map.GetOrAdd(enumObj, key =>
        {
            var name = Enum.GetName(type, key);
            if (name is null) return key.ToString() ?? string.Empty;

            var mem = type.GetMember(name);
            if (mem.Length > 0)
            {
                var desc = mem[0].GetCustomAttribute<DescriptionAttribute>()?.Description;
                if (!string.IsNullOrWhiteSpace(desc)) return desc!;
                var display = mem[0].GetCustomAttribute<DisplayAttribute>()?.GetName();
                if (!string.IsNullOrWhiteSpace(display)) return display!;
            }
            return name;
        });
    }

    private object? ResolvePathRaw(object? obj, string path)
    {
        try
        {
            if (obj == null) return null;
            object? cur = obj;

            foreach (var seg in path.Split('.'))
            {
                if (cur == null) return null;

                // ---------- Enum smart members ----------
                var curType = cur.GetType();
                var underlyingNullable = Nullable.GetUnderlyingType(curType);
                var isEnum = cur is Enum;
                var isNullableEnum = underlyingNullable?.IsEnum == true;

                if (isEnum || isNullableEnum)
                {
                    if (seg.Equals("Description", StringComparison.OrdinalIgnoreCase))
                    {
                        if (isNullableEnum)
                        {
                            var hasValue = (bool)(curType.GetProperty("HasValue")?.GetValue(cur) ?? false);
                            if (!hasValue) { cur = null; continue; }
                            var val = curType.GetProperty("Value")!.GetValue(cur)!;
                            cur = GetEnumDescriptionString(val);
                            continue;
                        }
                        cur = GetEnumDescriptionString((Enum)cur);
                        continue;
                    }
                    if (seg.Equals("Name", StringComparison.OrdinalIgnoreCase))
                    {
                        if (isNullableEnum)
                        {
                            var hasValue = (bool)(curType.GetProperty("HasValue")?.GetValue(cur) ?? false);
                            if (!hasValue) { cur = null; continue; }
                            var val = curType.GetProperty("Value")!.GetValue(cur)!;
                            cur = Enum.GetName(underlyingNullable!, val) ?? val.ToString();
                            continue;
                        }
                        cur = Enum.GetName(curType, cur) ?? cur.ToString();
                        continue;
                    }
                    if (seg.Equals("Value", StringComparison.OrdinalIgnoreCase))
                    {
                        if (isNullableEnum)
                        {
                            var hasValue = (bool)(curType.GetProperty("HasValue")?.GetValue(cur) ?? false);
                            if (!hasValue) { cur = null; continue; }
                            var val = curType.GetProperty("Value")!.GetValue(cur)!;
                            cur = Convert.ChangeType(val, Enum.GetUnderlyingType(underlyingNullable!));
                            continue;
                        }
                        cur = Convert.ChangeType(cur, Enum.GetUnderlyingType(curType));
                        continue;
                    }
                }
                // ---------- /Enum smart members ----------

                // IDictionary<string, object> support
                if (cur is IDictionary<string, object> dict)
                {
                    dict.TryGetValue(seg, out cur);
                    continue;
                }

                // IDictionary non-generic best effort (case-insensitive)
                if (cur is System.Collections.IDictionary any)
                {
                    object? found = null;
                    foreach (var k in any.Keys)
                    {
                        if (string.Equals(Convert.ToString(k), seg, StringComparison.OrdinalIgnoreCase))
                        { found = any[k]; break; }
                    }
                    cur = found;
                    continue;
                }

                // Regular property/field lookup (case-insensitive)
                var t = cur.GetType();
                var prop = t.GetProperty(seg,
                    BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
                if (prop != null) { cur = prop.GetValue(cur); continue; }

                var field = t.GetField(seg,
                    BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
                if (field != null) { cur = field.GetValue(cur); continue; }

                // Miss
                return null;
            }
            return cur;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "ResolvePath failed for {Path}", path);
            return null;
        }
    }

    // ---------- IF evaluation via RazorLight ----------

    private string NormalizeCondition(string condition)
    {
        var c = CoerceWordQuotes(condition).Trim();
        c = Regex.Replace(c, @"(?<![!<>=])=(?![=])", "=="); // single '=' to '=='
        return c;
    }

    private async Task<bool> EvalIfAsync(string condition, object model, string rootName, string? loopVar, object? loopItem)
    {
        try
        {
            var normalized = NormalizeCondition(condition);
            var qualified = QualifyIdentifiers(normalized, rootName, loopVar);

            dynamic bag = new ExpandoObject();
            var dict = (IDictionary<string, object>)bag;
            dict[rootName] = model;
            if (!string.IsNullOrWhiteSpace(loopVar) && loopItem is not null)
                dict[loopVar!] = loopItem;

            var tpl = $@"@({qualified} ? ""True"" : ""False"")";
            var result = await _engine.CompileRenderStringAsync(Guid.NewGuid().ToString(), tpl, (object)bag);
            var verdict = result.Trim().Equals("True", StringComparison.OrdinalIgnoreCase);

            logger.LogDebug("EvalIf: '{Cond}' => {Verdict} | normalized='{Norm}' qualified='{Qual}'",
                condition, verdict, normalized, qualified);

            return verdict;
        }
        catch (TemplateCompilationException tce)
        {
            logger.LogError("EvalIf compilation error: {Msg} | Errors: {Errors}",
                tce.Message, string.Join(" | ", tce.CompilationErrors ?? Array.Empty<string>()));
            return false;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "EvalIf failed for: {Condition}", condition);
            return false;
        }
    }

    private string CoerceWordQuotes(string s) =>
        s.Replace('“', '"').Replace('”', '"')
         .Replace('‘', '\'').Replace('’', '\'');

    private string QualifyIdentifiers(string expr, string rootName, string? loopVar)
    {
        var e = expr;

        string Qualify(string source, string name) =>
            Regex.Replace(source,
                $@"\b{Regex.Escape(name)}\b(?=(?:\s*[\.\(\)\[\],]|$))",
                $"Model.{name}");

        e = Qualify(e, rootName);
        if (!string.IsNullOrWhiteSpace(loopVar))
            e = Qualify(e, loopVar!);

        return e;
    }

    // ---------- Placeholder replacement (DFA across leaves) ----------

    private static class PlaceholderReplacer
    {
        private static readonly Regex IdRx =
            new(@"^[A-Za-z_][A-Za-z0-9_.]*$", RegexOptions.Compiled);

        private static List<OpenXmlLeafTextElement> Leaves(Paragraph p) =>
            p.Descendants<OpenXmlLeafTextElement>().ToList();

        public static void ReplaceInParagraph(Paragraph para, Func<string, string?> resolver)
        {
            var leaves = Leaves(para);
            if (leaves.Count == 0) return;

            int i = 0;
            while (i < leaves.Count)
            {
                if (!TryFindCharForward(leaves, ref i, '{')) break;

                int startLeaf = i;
                int startOffset = IndexOfCharInLeaf(leaves[i], '{');

                var idBuilder = new StringBuilder();
                int j = i;
                int jOffsetAfter = startOffset + 1;
                bool closed = false;

                while (j < leaves.Count)
                {
                    var txt = leaves[j].Text ?? string.Empty;
                    int k = j == i ? jOffsetAfter : 0;

                    for (; k < txt.Length; k++)
                    {
                        char c = txt[k];
                        if (c == '}')
                        {
                            var key = idBuilder.ToString();
                            if (IdRx.IsMatch(key))
                            {
                                var replacement = resolver(key) ?? string.Empty;
                                ReplaceSpan(para, leaves, startLeaf, startOffset, j, k + 1, replacement);
                                leaves = Leaves(para);       // refresh view after mutation
                                i = Math.Min(startLeaf + 1, leaves.Count);
                            }
                            else
                            {
                                i = Math.Min(startLeaf + 1, leaves.Count);
                            }
                            closed = true;
                            break;
                        }
                        else
                        {
                            idBuilder.Append(c);
                        }
                    }
                    if (closed) break;
                    j++;
                }

                if (!closed) break; // unmatched '{' -> bail
            }

            // Cleanup: remove empty runs and disable proofing
            foreach (var pe in para.Descendants<ProofError>().ToList()) pe.Remove();
            foreach (var r in para.Descendants<Run>().ToList())
            {
                bool hasText = r.Descendants<OpenXmlLeafTextElement>()
                                .Any(l => !string.IsNullOrEmpty(l.Text));

                if (!hasText && !r.Descendants<Drawing>().Any())
                    r.Remove();
                else
                {
                    r.RunProperties ??= new RunProperties();
                    r.RunProperties.NoProof = new NoProof();
                }
            }
        }

        private static bool TryFindCharForward(List<OpenXmlLeafTextElement> leaves, ref int i, char ch)
        {
            for (; i < leaves.Count; i++)
            {
                var t = leaves[i].Text ?? string.Empty;
                if (t.IndexOf(ch) >= 0) return true;
            }
            return false;
        }

        private static int IndexOfCharInLeaf(OpenXmlLeafTextElement leaf, char ch) =>
            (leaf.Text ?? string.Empty).IndexOf(ch);

        // Replace [startLeaf:startOffset, endLeaf:endOffset) with replacement
        private static void ReplaceSpan(
            Paragraph para,
            List<OpenXmlLeafTextElement> leaves,
            int startLeaf, int startOffset,
            int endLeaf, int endOffset,
            string replacement)
        {
            for (int idx = startLeaf; idx <= endLeaf; idx++)
            {
                var leaf = leaves[idx];
                var s = leaf.Text ?? string.Empty;

                int leftKeep = idx == startLeaf ? startOffset : 0;
                int rightStart = idx == endLeaf ? endOffset : s.Length;

                var left = leftKeep > 0 ? s.Substring(0, leftKeep) : string.Empty;
                var right = rightStart < s.Length ? s.Substring(rightStart) : string.Empty;

                if (idx == startLeaf)
                    leaf.Text = left + replacement + right;
                else
                    leaf.Text = left + right;
            }
        }
    }

    // ---------- Simple Tab Replacement ----------

    private static class SimpleTabReplacer
    {
        public static void ReplaceInParagraph(Paragraph para)
        {
            foreach (var t in para.Descendants<Text>())
            {
                if (t.Text is null) continue;
                if (t.Text.IndexOf("{TAB}", StringComparison.OrdinalIgnoreCase) >= 0)
                    t.Text = Regex.Replace(t.Text, @"\{TAB\}", "\t", RegexOptions.IgnoreCase);
            }
        }
    }
}

//public class RazorLiteService : IRazorLiteService
//{
//    // [[IF <expr>]] ... [[ENDIF]]
//    private readonly Regex IfStart =
//        new(@"^\s*\[\[IF\s+(.+?)\]\]\s*$", RegexOptions.Compiled | RegexOptions.IgnoreCase);
//    private const string EndIfToken = "[[ENDIF]]";

//    // Skip paragraphs that contain Word page-number fields
//    private readonly Regex PageFieldRx =
//        new(@"\b(PAGE|NUMPAGES|SECTIONPAGES)\b", RegexOptions.IgnoreCase | RegexOptions.Compiled);

//    // [[FOREACH <var> in <Root.Path>]] ... [[ENDFOREACH]]
//    private readonly Regex ForeachStart =
//        new(@"^\s*\[\[FOREACH\s+([A-Za-z_][A-Za-z0-9_]*)\s+in\s+(.+?)\]\]\s*$",
//            RegexOptions.Compiled | RegexOptions.IgnoreCase);
//    private const string EndForeachToken = "[[ENDFOREACH]]";

//    private readonly ILogger<RazorLiteService> logger;
//    private readonly RazorLightEngine _engine =
//        new RazorLightEngineBuilder().UseMemoryCachingProvider().Build();

//    public RazorLiteService(ILogger<RazorLiteService> logger)
//    {
//        this.logger = logger;
//    }

//    public async Task ProcessAsync(string inputPath, string outputPath, object model)
//    {
//        try
//        {
//            var rootName = model.GetType().Name;
//            File.Copy(inputPath, outputPath, true);
//            using var doc = WordprocessingDocument.Open(outputPath, true);
//            await ProcessAllStoriesAsync(doc, model, rootName);
//            doc.MainDocumentPart!.Document.Save();
//        }
//        catch (Exception ex)
//        {
//            logger.LogError(ex, "ProcessAsync file failed.");
//            throw;
//        }
//    }

//    public async Task<MemoryStream> ProcessAsync(MemoryStream ms, object model)
//    {
//        try
//        {
//            var rootName = model.GetType().Name;
//            ms.Position = 0;
//            using (var doc = WordprocessingDocument.Open(ms, true))
//            {
//                await ProcessAllStoriesAsync(doc, model, rootName);
//                doc.MainDocumentPart!.Document.Save();
//            }
//            ms.Position = 0;
//            return ms;
//        }
//        catch (Exception ex)
//        {
//            logger.LogError(ex, "ProcessAsync stream failed.");
//            throw;
//        }
//    }

//    // ---------- Orchestrate across body/headers/footers/text boxes ----------

//    private async Task ProcessAllStoriesAsync(WordprocessingDocument doc, object model, string rootName)
//    {
//        var mdp = doc.MainDocumentPart!;
//        logger.LogDebug("Starting merge. Root model: {Root}", rootName);

//        await ProcessContainerAsync(mdp.Document.Body!, model, rootName, "Body");

//        foreach (var (part, idx) in mdp.HeaderParts.Select((p, i) => (p, i)))
//        {
//            logger.LogDebug("Processing HeaderPart[{Idx}]", idx);
//            await ProcessContainerAsync(part.RootElement!, model, rootName, $"Header[{idx}]");
//        }

//        foreach (var (part, idx) in mdp.FooterParts.Select((p, i) => (p, i)))
//        {
//            logger.LogDebug("Processing FooterPart[{Idx}]", idx);
//            await ProcessContainerAsync(part.RootElement!, model, rootName, $"Footer[{idx}]");
//        }

//        foreach (var (root, i) in EnumerateTextBoxRoots(mdp).Select((r, i) => (r, i)))
//        {
//            logger.LogDebug("Processing TextBox[{Idx}]", i);
//            await ProcessContainerAsync(root, model, rootName, $"TextBox[{i}]");
//        }

//        logger.LogDebug("Merge complete.");
//    }

//    // ---------- Extractors ----------

//    // For readable logs: show \t and \n tokens so you can spot layout junk
//    private string GetParagraphDebugText(Paragraph p)
//    {
//        var sb = new StringBuilder();
//        foreach (var e in p.Descendants())
//        {
//            switch (e)
//            {
//                case Text t:
//                    sb.Append(t.Text);
//                    break;
//                case FieldCode fc:       // w:instrText
//                    sb.Append(fc.Text);
//                    break;
//                case Break:
//                    sb.Append("\\n");
//                    break;
//                case TabChar:
//                    sb.Append("\\t");
//                    break;
//            }
//        }
//        return sb.ToString()
//                 .Replace('\u00A0', ' ')
//                 .Replace("\u200B", "");
//    }

//    // For matching tokens: tabs/breaks become whitespace, collapse it, trim
//    private string GetParagraphMatchText(Paragraph p)
//    {
//        var sb = new StringBuilder();
//        foreach (var e in p.Descendants())
//        {
//            switch (e)
//            {
//                case Text t:
//                    sb.Append(t.Text);
//                    break;
//                case FieldCode fc:
//                    sb.Append(fc.Text);
//                    break;
//                case Break:
//                case TabChar:
//                    sb.Append(' ');
//                    break;
//            }
//        }

//        var s = sb.ToString()
//                  .Replace('\u00A0', ' ')
//                  .Replace("\u200B", "")
//                  .Replace("\u2009", " ");

//        s = Regex.Replace(s, @"\s+", " ").Trim();
//        return s;
//    }

//    private bool IsPageFieldParagraph(Paragraph p)
//    {
//        // Simple fields: <w:fldSimple w:instr="PAGE \* MERGEFORMAT">
//        if (p.Descendants<SimpleField>().Any(sf => PageFieldRx.IsMatch(sf.Instruction?.Value ?? string.Empty)))
//            return true;

//        // Complex fields: runs contain <w:instrText> (SDK: FieldCode) with "PAGE ..." etc.
//        if (p.Descendants<FieldCode>().Any(fc => PageFieldRx.IsMatch(fc.Text ?? string.Empty)))
//            return true;

//        return false;
//    }

//    private IEnumerable<OpenXmlElement> EnumerateTextBoxRoots(OpenXmlPartContainer container)
//    {
//        foreach (var partRef in container.Parts)
//        {
//            var part = partRef.OpenXmlPart;
//            if (part?.RootElement is null) continue;

//            foreach (var tb in part.RootElement.Descendants()
//                         .Where(e => e.LocalName == "txbxContent")) // w:txbxContent
//            {
//                yield return tb;
//            }

//            foreach (var nested in EnumerateTextBoxRoots(part)) yield return nested;
//        }
//    }

//    // ---------- Core processing for a single container ----------

//    private async Task ProcessContainerAsync(OpenXmlElement containerRoot, object model, string rootName, string scopeName)
//    {
//        var paragraphs = containerRoot.Descendants<Paragraph>().ToList();
//        if (paragraphs.Count == 0)
//        {
//            logger.LogDebug("[{Scope}] No paragraphs found.", scopeName);
//            return;
//        }

//        logger.LogDebug("[{Scope}] Paragraphs discovered: {Count}", scopeName, paragraphs.Count);

//        var toRemove = new HashSet<OpenXmlElement>();
//        int i = 0;

//        while (i < paragraphs.Count)
//        {
//            var p = paragraphs[i];
//            var rawLog = GetParagraphDebugText(p);
//            var norm = NormalizeForToken(GetParagraphMatchText(p));

//            logger.LogDebug("[{Scope}] Raw p#{I}: {Raw}", scopeName, i, rawLog);

//            // IF block
//            var ifMatch = IfStart.Match(norm);
//            if (ifMatch.Success)
//            {
//                logger.LogDebug("[{Scope}] IF start at p#{I}: '{Norm}'", scopeName, i, norm);

//                var cond = ifMatch.Groups[1].Value.Trim();
//                var inner = new List<Paragraph>();
//                var innerInfo = new List<(int idx, string raw, string norm)>();
//                int j = i + 1;

//                for (; j < paragraphs.Count; j++)
//                {
//                    var rawInner = GetParagraphDebugText(paragraphs[j]);
//                    var normInner = NormalizeForToken(GetParagraphMatchText(paragraphs[j]));
//                    if (TokenEquals(normInner, EndIfToken)) break;
//                    inner.Add(paragraphs[j]);
//                    innerInfo.Add((j, rawInner, normInner));
//                }

//                if (j >= paragraphs.Count)
//                {
//                    logger.LogWarning("[{Scope}] Malformed IF: missing [[ENDIF]]. Aborting IF at p#{I}.", scopeName, i);
//                    break;
//                }

//                var keep = await EvalIfAsync(cond, model, rootName, null, null);
//                logger.LogDebug("[{Scope}] IF evaluated '{Cond}' => {Keep}. Inner count = {Cnt}.",
//                    scopeName, cond, keep, inner.Count);

//                foreach (var info in innerInfo)
//                    logger.LogDebug("[{Scope}]   inner p#{Idx} raw: {Raw}", scopeName, info.idx, info.raw);

//                if (!keep)
//                {
//                    foreach (var para in inner) toRemove.Add(para);
//                    logger.LogDebug("[{Scope}] IF false: queued {Count} inner paragraphs for removal.", scopeName, inner.Count);
//                }

//                toRemove.Add(p);
//                toRemove.Add(paragraphs[j]);
//                i = j + 1;
//                continue;
//            }

//            // FOREACH block
//            var feMatch = ForeachStart.Match(norm);
//            if (feMatch.Success)
//            {
//                var loopVar = feMatch.Groups[1].Value.Trim();
//                var enumerableExpr = feMatch.Groups[2].Value.Trim();
//                logger.LogDebug("[{Scope}] FOREACH start at p#{I}: var={Var}, expr={Expr}", scopeName, i, loopVar, enumerableExpr);

//                var stencil = new List<Paragraph>();
//                int j = i + 1;

//                for (; j < paragraphs.Count; j++)
//                {
//                    var t = NormalizeForToken(GetParagraphMatchText(paragraphs[j]));
//                    if (TokenEquals(t, EndForeachToken)) break;
//                    stencil.Add(paragraphs[j]);
//                }

//                if (j >= paragraphs.Count)
//                {
//                    logger.LogWarning("[{Scope}] Malformed FOREACH: missing [[ENDFOREACH]]. Aborting at p#{I}.", scopeName, i);
//                    break;
//                }

//                var sequence = ResolveEnumerable(model, enumerableExpr, rootName) ?? Array.Empty<object>();
//                logger.LogDebug("[{Scope}] FOREACH sequence resolved to {Count} items.", scopeName, sequence.Count());

//                var insertAfter = paragraphs[j]; // append clones after ENDFOREACH
//                foreach (var item in sequence)
//                {
//                    var clones = stencil.Select(s => (Paragraph)s.CloneNode(true)).ToList();

//                    // Handle nested blocks inside the clones first
//                    await ProcessNestedBlocksInMemoryAsync(clones, model, rootName, loopVar, item, scopeName);

//                    // Scalar replacement in each cloned paragraph (skip page fields)
//                    foreach (var clone in clones)
//                    {
//                        if (!IsPageFieldParagraph(clone))
//                        {
//                            PlaceholderReplacer.ReplaceInParagraph(clone,
//                                key => ResolveKey(key, model, rootName, loopVar, item));
//                            SimpleTabReplacer.ReplaceInParagraph(clone);
//                        }

//                        insertAfter.InsertAfterSelf(clone);
//                        insertAfter = clone;
//                    }
//                }

//                // Remove markers and stencil after expansion
//                toRemove.Add(paragraphs[i]);
//                toRemove.Add(paragraphs[j]);
//                foreach (var s in stencil) toRemove.Add(s);

//                i = j + 1;
//                continue;
//            }

//            // Plain paragraph: root-scoped replacements (skip page field paragraphs)
//            if (!IsPageFieldParagraph(p))
//            {
//                PlaceholderReplacer.ReplaceInParagraph(p, key => ResolveKey(key, model, rootName, null, null));
//                SimpleTabReplacer.ReplaceInParagraph(p);
//            }
//            else
//            {
//                logger.LogDebug("[{Scope}] Skipping paragraph with page field.", scopeName);
//            }

//            i++;
//        }

//        foreach (var rem in toRemove) rem.Remove();

//        // ---------- bounded extra passes ----------
//        const int MaxExtraPasses = 2;
//        for (int pass = 1; pass <= MaxExtraPasses; pass++)
//        {
//            var before = containerRoot.OuterXml;

//            var paragraphs2 = containerRoot.Descendants<Paragraph>().ToList();
//            if (paragraphs2.Count == 0) break;

//            var toRemove2 = new HashSet<OpenXmlElement>();
//            int i2 = 0;

//            while (i2 < paragraphs2.Count)
//            {
//                var p2 = paragraphs2[i2];
//                var norm2 = NormalizeForToken(GetParagraphMatchText(p2));

//                // IF
//                var ifMatch2 = IfStart.Match(norm2);
//                if (ifMatch2.Success)
//                {
//                    var cond2 = ifMatch2.Groups[1].Value.Trim();
//                    var inner2 = new List<Paragraph>();
//                    int j2 = i2 + 1;

//                    for (; j2 < paragraphs2.Count; j2++)
//                    {
//                        var t2 = NormalizeForToken(GetParagraphMatchText(paragraphs2[j2]));
//                        if (TokenEquals(t2, EndIfToken)) break;
//                        inner2.Add(paragraphs2[j2]);
//                    }
//                    if (j2 >= paragraphs2.Count) { logger.LogWarning("[{Scope}] Malformed IF in extra pass.", scopeName); break; }

//                    var keep2 = await EvalIfAsync(cond2, model, rootName, null, null);
//                    if (!keep2) foreach (var ip in inner2) toRemove2.Add(ip);
//                    toRemove2.Add(paragraphs2[i2]);
//                    toRemove2.Add(paragraphs2[j2]);

//                    i2 = j2 + 1;
//                    continue;
//                }

//                // FOREACH
//                var feMatch2 = ForeachStart.Match(norm2);
//                if (feMatch2.Success)
//                {
//                    var loopVar2 = feMatch2.Groups[1].Value.Trim();
//                    var expr2 = feMatch2.Groups[2].Value.Trim();

//                    var stencil2 = new List<Paragraph>();
//                    int j2 = i2 + 1;
//                    for (; j2 < paragraphs2.Count; j2++)
//                    {
//                        var t2 = NormalizeForToken(GetParagraphMatchText(paragraphs2[j2]));
//                        if (TokenEquals(t2, EndForeachToken)) break;
//                        stencil2.Add(paragraphs2[j2]);
//                    }
//                    if (j2 >= paragraphs2.Count) { logger.LogWarning("[{Scope}] Malformed FOREACH in extra pass.", scopeName); break; }

//                    var seq2 = ResolveEnumerable(model, expr2, rootName) ?? Array.Empty<object>();
//                    var insertAfter2 = paragraphs2[j2];

//                    foreach (var item2 in seq2)
//                    {
//                        var clones2 = stencil2.Select(s => (Paragraph)s.CloneNode(true)).ToList();
//                        await ProcessNestedBlocksInMemoryAsync(clones2, model, rootName, loopVar2, item2, "ExtraPass");

//                        foreach (var clone2 in clones2)
//                        {
//                            if (!IsPageFieldParagraph(clone2))
//                            {
//                                PlaceholderReplacer.ReplaceInParagraph(clone2,
//                                    key => ResolveKey(key, model, rootName, loopVar2, item2));
//                                SimpleTabReplacer.ReplaceInParagraph(clone2);
//                            }

//                            insertAfter2.InsertAfterSelf(clone2);
//                            insertAfter2 = clone2;
//                        }
//                    }

//                    toRemove2.Add(paragraphs2[i2]);
//                    toRemove2.Add(paragraphs2[j2]);
//                    foreach (var s2 in stencil2) toRemove2.Add(s2);

//                    i2 = j2 + 1;
//                    continue;
//                }

//                // Plain (skip page-field paragraphs)
//                if (!IsPageFieldParagraph(p2))
//                {
//                    PlaceholderReplacer.ReplaceInParagraph(p2, key => ResolveKey(key, model, rootName, null, null));
//                    SimpleTabReplacer.ReplaceInParagraph(p2);
//                }

//                i2++;
//            }

//            foreach (var rem2 in toRemove2) rem2.Remove();

//            var after = containerRoot.OuterXml;
//            if (string.Equals(before, after, StringComparison.Ordinal))
//            {
//                logger.LogDebug("[{Scope}] No structural changes in extra pass #{Pass}; stopping.", scopeName, pass);
//                break;
//            }

//            bool markersRemain = containerRoot.Descendants<Paragraph>().Any(pp =>
//            {
//                var s = NormalizeForToken(GetParagraphMatchText(pp));
//                return IfStart.IsMatch(s) || ForeachStart.IsMatch(s)
//                       || TokenEquals(s, EndIfToken) || TokenEquals(s, EndForeachToken);
//            });

//            logger.LogDebug("[{Scope}] Extra pass #{Pass} changed XML. Markers remain: {Remain}",
//                scopeName, pass, markersRemain);

//            if (!markersRemain) break;
//        }

//        // Log if markers still remain
//        bool leftover = containerRoot.Descendants<Paragraph>().Any(p =>
//        {
//            var s = NormalizeForToken(GetParagraphMatchText(p));
//            return IfStart.IsMatch(s) || ForeachStart.IsMatch(s)
//                   || TokenEquals(s, EndIfToken) || TokenEquals(s, EndForeachToken);
//        });
//        if (leftover)
//        {
//            logger.LogWarning("[{Scope}] Markers remain after bounded passes. Likely malformed blocks or tokens split across objects. Continuing.", scopeName);
//        }
//    }

//    private async Task ProcessNestedBlocksInMemoryAsync(
//        List<Paragraph> paras, object model, string rootName, string loopVar, object loopItem, string scopeName)
//    {
//        var toRemove = new HashSet<OpenXmlElement>();
//        int k = 0;

//        while (k < paras.Count)
//        {
//            var txt = NormalizeForToken(GetParagraphMatchText(paras[k]));

//            // Nested IF
//            var ifMatch = IfStart.Match(txt);
//            if (ifMatch.Success)
//            {
//                var cond = ifMatch.Groups[1].Value.Trim();
//                var inner = new List<Paragraph>();
//                int m = k + 1;

//                for (; m < paras.Count; m++)
//                {
//                    var t = NormalizeForToken(GetParagraphMatchText(paras[m]));
//                    if (TokenEquals(t, EndIfToken)) break;
//                    inner.Add(paras[m]);
//                }
//                if (m >= paras.Count)
//                {
//                    logger.LogWarning("[{Scope}] Nested IF missing [[ENDIF]] within loop block.", scopeName);
//                    break;
//                }

//                var keep = await EvalIfAsync(cond, model, rootName, loopVar, loopItem);
//                logger.LogDebug("[{Scope}] Nested IF '{Cond}' => {Keep}. Inner count = {Cnt}", scopeName, cond, keep, inner.Count);

//                if (!keep) foreach (var rp in inner) toRemove.Add(rp);

//                toRemove.Add(paras[k]);  // [[IF...]]
//                toRemove.Add(paras[m]);  // [[ENDIF]]

//                k = m + 1;
//                continue;
//            }

//            // Nested FOREACH
//            var feMatch = ForeachStart.Match(txt);
//            if (feMatch.Success)
//            {
//                var childVar = feMatch.Groups[1].Value.Trim();
//                var childExpr = feMatch.Groups[2].Value.Trim();

//                var stencil = new List<Paragraph>();
//                int m = k + 1;

//                for (; m < paras.Count; m++)
//                {
//                    var t = NormalizeForToken(GetParagraphMatchText(paras[m]));
//                    if (TokenEquals(t, EndForeachToken)) break;
//                    stencil.Add(paras[m]);
//                }
//                if (m >= paras.Count)
//                {
//                    logger.LogWarning("[{Scope}] Nested FOREACH missing [[ENDFOREACH]] within loop block.", scopeName);
//                    break;
//                }

//                var seq = ResolveEnumerableForLoopScope(model, loopVar, loopItem, childExpr, rootName) ?? Array.Empty<object>();

//                var insertionIndex = m + 1;
//                var newParas = new List<Paragraph>();

//                foreach (var itm in seq)
//                {
//                    var clones = stencil.Select(s => (Paragraph)s.CloneNode(true)).ToList();
//                    await ProcessNestedBlocksInMemoryAsync(clones, model, rootName, childVar, itm, scopeName);

//                    foreach (var clone in clones)
//                    {
//                        if (!IsPageFieldParagraph(clone))
//                        {
//                            PlaceholderReplacer.ReplaceInParagraph(clone,
//                                key => ResolveKey(key, model, rootName, childVar, itm));
//                            SimpleTabReplacer.ReplaceInParagraph(clone);
//                        }
//                        newParas.Add(clone);
//                    }
//                }

//                var range = paras.GetRange(k, (m - k) + 1);
//                foreach (var r in range) toRemove.Add(r);
//                paras.InsertRange(insertionIndex, newParas);

//                k = insertionIndex + newParas.Count;
//                continue;
//            }

//            // Plain paragraph inside nested list: scalars at loop scope (skip page fields)
//            if (!IsPageFieldParagraph(paras[k]))
//            {
//                PlaceholderReplacer.ReplaceInParagraph(paras[k],
//                    key => ResolveKey(key, model, rootName, loopVar, loopItem));
//                SimpleTabReplacer.ReplaceInParagraph(paras[k]);
//            }

//            k++;
//        }

//        foreach (var rem in toRemove) paras.Remove(rem as Paragraph);
//    }

//    // ---------- Utilities ----------

//    private static string NormalizeForToken(string s) =>
//        (s ?? string.Empty)
//            .Replace('\u00A0', ' ')
//            .Replace("\u200B", "")
//            .Replace("\u2009", " ")
//            .Trim();

//    private static bool TokenEquals(string s, string token) =>
//        string.Equals(NormalizeForToken(s), token, StringComparison.Ordinal);

//    // ---------- Placeholder & path resolution ----------

//    private string? ResolveKey(string key, object rootModel, string rootName, string? loopVar, object? loopItem)
//    {
//        try
//        {
//            if (string.Equals(key, "TAB", StringComparison.OrdinalIgnoreCase))
//                return "\t";

//            if (!string.IsNullOrWhiteSpace(loopVar) &&
//                key.StartsWith(loopVar + ".", StringComparison.OrdinalIgnoreCase) &&
//                loopItem is not null)
//            {
//                return ResolvePath(loopItem, key[(loopVar.Length + 1)..]);
//            }

//            if (key.StartsWith(rootName + ".", StringComparison.Ordinal))
//            {
//                var sub = key[(rootName.Length + 1)..];
//                return ResolvePath(rootModel, sub);
//            }

//            logger.LogWarning("Placeholder '{Key}' ignored; must start with '{RootName}.' or '{LoopVar}.'",
//                key, rootName, loopVar);
//            return null;
//        }
//        catch (Exception ex)
//        {
//            logger.LogError(ex, "ResolveKey failed for {Key}", key);
//            return null;
//        }
//    }

//    private IEnumerable<object>? ResolveEnumerable(object model, string expr, string rootName)
//    {
//        try
//        {
//            var path = expr.Trim();
//            if (!path.StartsWith(rootName + ".", StringComparison.OrdinalIgnoreCase))
//            {
//                logger.LogWarning("FOREACH source '{Expr}' ignored; must start with '{RootName}.'", expr, rootName);
//                return null;
//            }

//            var sub = path.Substring(rootName.Length + 1);
//            var value = ResolvePathRaw(model, sub);
//            return (value as IEnumerable)?.Cast<object>();
//        }
//        catch (Exception ex)
//        {
//            logger.LogError(ex, "ResolveEnumerable failed for: {Expr}", expr);
//            return null;
//        }
//    }

//    private IEnumerable<object>? ResolveEnumerableForLoopScope(object model, string loopVar, object loopItem, string expr, string rootName)
//    {
//        var path = expr.Trim();

//        if (path.StartsWith(loopVar + ".", StringComparison.OrdinalIgnoreCase))
//        {
//            var sub = path[(loopVar.Length + 1)..];
//            var value = ResolvePathRaw(loopItem, sub);
//            return (value as IEnumerable)?.Cast<object>();
//        }

//        return ResolveEnumerable(model, expr, rootName);
//    }

//    private string? ResolvePath(object? obj, string path)
//    {
//        var value = ResolvePathRaw(obj, path);
//        return value?.ToString();
//    }

//    private object? ResolvePathRaw(object? obj, string path)
//    {
//        try
//        {
//            if (obj == null) return null;
//            object? cur = obj;

//            foreach (var seg in path.Split('.'))
//            {
//                if (cur == null) return null;
//                var t = cur.GetType();

//                if (cur is IDictionary<string, object> dict)
//                {
//                    dict.TryGetValue(seg, out cur);
//                    continue;
//                }
//                if (cur is System.Collections.IDictionary any)
//                {
//                    object? found = null;
//                    foreach (var k in any.Keys)
//                    {
//                        if (string.Equals(Convert.ToString(k), seg, StringComparison.OrdinalIgnoreCase))
//                        { found = any[k]; break; }
//                    }
//                    cur = found;
//                    continue;
//                }

//                var prop = t.GetProperty(seg,
//                    System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.IgnoreCase);
//                if (prop != null) { cur = prop.GetValue(cur); continue; }

//                var field = t.GetField(seg,
//                    System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.IgnoreCase);
//                if (field != null) { cur = field.GetValue(cur); continue; }

//                return null;
//            }
//            return cur;
//        }
//        catch (Exception ex)
//        {
//            logger.LogError(ex, "ResolvePath failed for {Path}", path);
//            return null;
//        }
//    }

//    // ---------- IF evaluation via RazorLight ----------

//    private string NormalizeCondition(string condition)
//    {
//        var c = CoerceWordQuotes(condition).Trim();
//        c = Regex.Replace(c, @"(?<![!<>=])=(?![=])", "=="); // single '=' to '=='
//        return c;
//    }

//    private async Task<bool> EvalIfAsync(string condition, object model, string rootName, string? loopVar, object? loopItem)
//    {
//        try
//        {
//            var normalized = NormalizeCondition(condition);
//            var qualified = QualifyIdentifiers(normalized, rootName, loopVar);

//            dynamic bag = new ExpandoObject();
//            var dict = (IDictionary<string, object>)bag;
//            dict[rootName] = model;
//            if (!string.IsNullOrWhiteSpace(loopVar) && loopItem is not null)
//                dict[loopVar!] = loopItem;

//            var tpl = $@"@({qualified} ? ""True"" : ""False"")";
//            var result = await _engine.CompileRenderStringAsync(Guid.NewGuid().ToString(), tpl, (object)bag);
//            var verdict = result.Trim().Equals("True", StringComparison.OrdinalIgnoreCase);

//            logger.LogDebug("EvalIf: '{Cond}' => {Verdict} | normalized='{Norm}' qualified='{Qual}'",
//                condition, verdict, normalized, qualified);

//            return verdict;
//        }
//        catch (TemplateCompilationException tce)
//        {
//            logger.LogError("EvalIf compilation error: {Msg} | Errors: {Errors}",
//                tce.Message, string.Join(" | ", tce.CompilationErrors ?? Array.Empty<string>()));
//            return false;
//        }
//        catch (Exception ex)
//        {
//            logger.LogError(ex, "EvalIf failed for: {Condition}", condition);
//            return false;
//        }
//    }

//    private string CoerceWordQuotes(string s) =>
//        s.Replace('“', '"').Replace('”', '"')
//         .Replace('‘', '\'').Replace('’', '\'');

//    private string QualifyIdentifiers(string expr, string rootName, string? loopVar)
//    {
//        var e = expr;

//        string Qualify(string source, string name) =>
//            Regex.Replace(source,
//                $@"\b{Regex.Escape(name)}\b(?=(?:\s*[\.\(\)\[\],]|$))",
//                $"Model.{name}");

//        e = Qualify(e, rootName);
//        if (!string.IsNullOrWhiteSpace(loopVar))
//            e = Qualify(e, loopVar!);

//        return e;
//    }

//    // ---------- Placeholder replacement (DFA across leaves) ----------

//    private static class PlaceholderReplacer
//    {
//        private static readonly Regex IdRx =
//            new(@"^[A-Za-z_][A-Za-z0-9_.]*$", RegexOptions.Compiled);

//        private static List<OpenXmlLeafTextElement> Leaves(Paragraph p) =>
//            p.Descendants<OpenXmlLeafTextElement>().ToList();

//        public static void ReplaceInParagraph(Paragraph para, Func<string, string?> resolver)
//        {
//            var leaves = Leaves(para);
//            if (leaves.Count == 0) return;

//            int i = 0;
//            while (i < leaves.Count)
//            {
//                if (!TryFindCharForward(leaves, ref i, '{')) break;

//                int startLeaf = i;
//                int startOffset = IndexOfCharInLeaf(leaves[i], '{');

//                var idBuilder = new StringBuilder();
//                int j = i;
//                int jOffsetAfter = startOffset + 1;
//                bool closed = false;

//                while (j < leaves.Count)
//                {
//                    var txt = leaves[j].Text ?? string.Empty;
//                    int k = j == i ? jOffsetAfter : 0;

//                    for (; k < txt.Length; k++)
//                    {
//                        char c = txt[k];
//                        if (c == '}')
//                        {
//                            var key = idBuilder.ToString();
//                            if (IdRx.IsMatch(key))
//                            {
//                                var replacement = resolver(key) ?? string.Empty;
//                                ReplaceSpan(para, leaves, startLeaf, startOffset, j, k + 1, replacement);
//                                leaves = Leaves(para);       // refresh view after mutation
//                                i = Math.Min(startLeaf + 1, leaves.Count);
//                            }
//                            else
//                            {
//                                i = Math.Min(startLeaf + 1, leaves.Count);
//                            }
//                            closed = true;
//                            break;
//                        }
//                        else
//                        {
//                            idBuilder.Append(c);
//                        }
//                    }
//                    if (closed) break;
//                    j++;
//                }

//                if (!closed) break; // unmatched '{' -> bail
//            }

//            // Cleanup: remove empty runs and disable proofing
//            foreach (var pe in para.Descendants<ProofError>().ToList()) pe.Remove();
//            foreach (var r in para.Descendants<Run>().ToList())
//            {
//                bool hasText = r.Descendants<OpenXmlLeafTextElement>()
//                                .Any(l => !string.IsNullOrEmpty(l.Text));

//                if (!hasText && !r.Descendants<Drawing>().Any())
//                    r.Remove();
//                else
//                {
//                    r.RunProperties ??= new RunProperties();
//                    r.RunProperties.NoProof = new NoProof();
//                }
//            }
//        }

//        private static bool TryFindCharForward(List<OpenXmlLeafTextElement> leaves, ref int i, char ch)
//        {
//            for (; i < leaves.Count; i++)
//            {
//                var t = leaves[i].Text ?? string.Empty;
//                if (t.IndexOf(ch) >= 0) return true;
//            }
//            return false;
//        }

//        private static int IndexOfCharInLeaf(OpenXmlLeafTextElement leaf, char ch) =>
//            (leaf.Text ?? string.Empty).IndexOf(ch);

//        // Replace [startLeaf:startOffset, endLeaf:endOffset) with replacement
//        private static void ReplaceSpan(
//            Paragraph para,
//            List<OpenXmlLeafTextElement> leaves,
//            int startLeaf, int startOffset,
//            int endLeaf, int endOffset,
//            string replacement)
//        {
//            for (int idx = startLeaf; idx <= endLeaf; idx++)
//            {
//                var leaf = leaves[idx];
//                var s = leaf.Text ?? string.Empty;

//                int leftKeep = idx == startLeaf ? startOffset : 0;
//                int rightStart = idx == endLeaf ? endOffset : s.Length;

//                var left = leftKeep > 0 ? s.Substring(0, leftKeep) : string.Empty;
//                var right = rightStart < s.Length ? s.Substring(rightStart) : string.Empty;

//                if (idx == startLeaf)
//                    leaf.Text = left + replacement + right;
//                else
//                    leaf.Text = left + right;
//            }
//        }
//    }

//    // ---------- Simple Tab Replacement ----------

//    private static class SimpleTabReplacer
//    {
//        public static void ReplaceInParagraph(Paragraph para)
//        {
//            foreach (var t in para.Descendants<Text>())
//            {
//                if (t.Text is null) continue;
//                if (t.Text.IndexOf("{TAB}", StringComparison.OrdinalIgnoreCase) >= 0)
//                    t.Text = Regex.Replace(t.Text, @"\{TAB\}", "\t", RegexOptions.IgnoreCase);
//            }
//        }
//    }
//}
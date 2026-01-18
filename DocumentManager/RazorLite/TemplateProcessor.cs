using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using RazorLight;
using System.Collections;
using System.Dynamic;
using System.Text;
using System.Text.RegularExpressions;

namespace DocumentManager.RazorLite;

public class TemplateProcessor : ITemplateProcessor
{
    private static readonly Regex IfStart = new(@"\A\[\[IF\s+(.*)\]\]\z", RegexOptions.Compiled);
    private static readonly Regex ForeachStart = new(@"\A\[\[FOREACH\s+(\w+)\s+in\s+(.+)\]\]\z", RegexOptions.Compiled);
    private const string EndToken = "[[END]]";
    private const string EndForeachToken = "[[ENDFOREACH]]";

    private readonly RazorLightEngine _engine = new RazorLightEngineBuilder().UseMemoryCachingProvider().Build();

    public async Task ProcessAsync(string inputPath, string outputPath, object model)
    {
        File.Copy(inputPath, outputPath, true);
        using var doc = WordprocessingDocument.Open(outputPath, true);
        var body = doc.MainDocumentPart!.Document.Body!;

        // Linear pass with stack to handle IF/FOREACH blocks
        var paragraphs = body.Elements<Paragraph>().ToList();
        var toRemove = new HashSet<OpenXmlElement>();
        var i = 0;

        while (i < paragraphs.Count)
        {
            var p = paragraphs[i];
            var text = GetParagraphText(p).Trim();

            // IF block
            var ifMatch = IfStart.Match(text);
            var feMatch = ForeachStart.Match(text);

            if (ifMatch.Success)
            {
                var cond = ifMatch.Groups[1].Value.Trim();
                // gather inner block until [[END]]
                var inner = new List<Paragraph>();
                int j = i + 1;
                for (; j < paragraphs.Count; j++)
                {
                    var t = GetParagraphText(paragraphs[j]).Trim();
                    if (t == EndToken)
                        break;
                    inner.Add(paragraphs[j]);
                }
                if (j >= paragraphs.Count) break; // malformed, bail gracefully

                var keep = await EvalIfAsync(cond, model);
                if (!keep)
                {
                    foreach (var para in inner) toRemove.Add(para);
                }
                // remove the IF and END markers
                toRemove.Add(p);
                toRemove.Add(paragraphs[j]);
                i = j + 1;
                continue;
            }
            else if (feMatch.Success)
            {
                var varName = feMatch.Groups[1].Value.Trim();
                var enumerableExpr = feMatch.Groups[2].Value.Trim();
                // collect inner
                var stencil = new List<Paragraph>();
                int j = i + 1;
                for (; j < paragraphs.Count; j++)
                {
                    var t = GetParagraphText(paragraphs[j]).Trim();
                    if (t == EndForeachToken)
                        break;
                    stencil.Add(paragraphs[j]);
                }
                if (j >= paragraphs.Count) break;

                // Resolve enumerable from model (supports "Model.Sections" only in this demo)
                var sequence = ResolveEnumerable(model, enumerableExpr) ?? Array.Empty<object>();

                // Insert clones after the foreach end marker, then remove stencil
                var insertAfter = paragraphs[j];
                foreach (var item in sequence)
                {
                    foreach (var sPara in stencil)
                    {
                        var clone = (Paragraph)sPara.CloneNode(true);
                        PlaceholderReplacer.ReplaceInParagraph(clone, key => ResolveKey(key, model, varName, item));
                        insertAfter.InsertAfterSelf(clone);
                        insertAfter = clone;
                    }
                }
                // Mark markers and stencil for removal
                toRemove.Add(p);
                toRemove.Add(paragraphs[j]);
                foreach (var s in stencil) toRemove.Add(s);

                i = j + 1;
                continue;
            }
            else
            {
                // plain paragraph: do global placeholder replacement against root model
                PlaceholderReplacer.ReplaceInParagraph(p, key => ResolveKey(key, model, null, null));
                i++;
            }
        }

        foreach (var rem in toRemove) rem.Remove();
        doc.MainDocumentPart!.Document.Save();
    }

    private static string GetParagraphText(Paragraph p) =>
        string.Concat(p.Descendants<Text>().Select(t => t.Text));

    private static string NormalizeCondition(string condition, object model)
    {
        // Prefix any bare top-level model property names with 'Model.' so Razor can resolve them.
        // e.g. 'Ken == "Good Guy"' -> 'Model.Ken == "Good Guy"'
        try
        {
            var names = model.GetType().GetProperties()
                .Select(p => p.Name)
                .OrderByDescending(n => n.Length) // prevent partial replacements
                .ToList();

            foreach (var name in names)
            {
                var pattern = $@"\b{name}\b";
                condition = System.Text.RegularExpressions.Regex.Replace(
                    condition, pattern, $"Model.{name}");
            }
            return condition;
        }
        catch
        {
            return condition;
        }
    }

    private async Task<bool> EvalIfAsync(string condition, object model)
    {
        // Build a dynamic root that contains both Model and its top-level props at root.
        dynamic bag = new ExpandoObject();
        var dict = (IDictionary<string, object>)bag;
        dict["Model"] = model;
        foreach (var p in model.GetType().GetProperties())
            dict[p.Name] = p.GetValue(model);

        // Prelude: create local aliases so bare names compile
        // e.g. @{ var Ken = Model.Ken; var Name = Model.Name; ... }
        var prelude = BuildPrelude(model);

        // Full template: prelude + boolean expression
        var tpl = $@"@{{{prelude}}} @({condition} ? ""True"" : ""False"")";

        var result = await _engine.CompileRenderStringAsync(
            Guid.NewGuid().ToString(), tpl, (object)bag);

        return result.Trim().Equals("True", StringComparison.OrdinalIgnoreCase);
    }

    private static string BuildPrelude(object model)
    {
        var sb = new StringBuilder();
        foreach (var p in model.GetType().GetProperties())
        {
            var name = p.Name; // safe: C# identifiers from your POCO
            sb.Append(" var ").Append(name).Append(" = Model.")
              .Append(name).Append("; ");
        }
        return sb.ToString();
    }

    private static IEnumerable<object>? ResolveEnumerable(object model, string expr)
    {
        // Very small resolver: supports "Model.Sections"
        if (expr.Trim() == "Model.Sections")
        {
            var pi = model.GetType().GetProperty("Sections");
            var val = pi?.GetValue(model) as IEnumerable;
            return val?.Cast<object>();
        }
        return null;
    }

    private static string? ResolveKey(string key, object rootModel, string? loopVar, object? loopItem)
    {
        // Supports {Name}, {LoanAmount}, {item.Title}, {item.Body}
        if (key.StartsWith("item.") && loopItem != null)
        {
            return ResolvePath(loopItem, key.Substring("item.".Length));
        }
        // allow Borrower.Address.City style from root
        return ResolvePath(rootModel, key);
    }

    private static string? ResolvePath(object? obj, string path)
    {
        if (obj == null) return null;
        var cur = obj;
        foreach (var part in path.Split('.'))
        {
            var pi = cur.GetType().GetProperty(part);
            if (pi == null) return null;
            cur = pi.GetValue(cur);
            if (cur == null) return null;
        }
        if (cur is IFormattable fmt && path.Equals("LoanAmount", StringComparison.OrdinalIgnoreCase))
            return fmt.ToString("C0", System.Globalization.CultureInfo.GetCultureInfo("en-US"));
        return cur.ToString();
    }
}
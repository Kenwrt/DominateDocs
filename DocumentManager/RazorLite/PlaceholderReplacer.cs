using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Wordprocessing;
using System.Text;
using System.Text.RegularExpressions;

namespace DocumentManager.RazorLite;

public static class PlaceholderReplacer
{
    // Use a non-compiled, culture-invariant regex with an explicit timeout to avoid TypeInitializationException on some environments.
    private static readonly Regex Placeholder = new(@"\{[A-Za-z0-9._]+\}", RegexOptions.CultureInvariant, TimeSpan.FromSeconds(2));

    public static void ReplaceInParagraph(Paragraph para, Func<string, string?> resolver)
    {
        try
        {
            var runs = para.Descendants<Run>().ToList();
            if (runs.Count == 0) return;

            var sb = new StringBuilder();
            var segments = new List<(Run run, int start, int length, Text textEl)>();

            foreach (var run in runs)
            {
                foreach (var t in run.Descendants<Text>())
                {
                    var start = sb.Length;
                    sb.Append(t.Text);
                    var len = sb.Length - start;
                    if (len > 0) segments.Add((run, start, len, t));
                }
            }

            var full = sb.ToString();
            var matches = Placeholder.Matches(full).Cast<Match>().OrderByDescending(m => m.Index);
            foreach (var m in matches)
            {
                var key = m.Value.Trim('{', '}');
                var val = resolver(key) ?? "";
                ReplaceSpanWithText(para, segments, m.Index, m.Length, val);
            }
        }
        catch (Exception ex)
        {
            // Surface better diagnostics to the caller rather than crashing in static init
            throw new InvalidOperationException("PlaceholderReplacer failed while replacing placeholders in a paragraph.", ex);
        }
    }

    public static void ReplaceSpanWithText(Paragraph para,
        List<(Run run, int start, int length, Text textEl)> segments,
        int spanStart, int spanLen, string replacement)
    {
        int remainingEnd = spanStart + spanLen;
        var affected = segments.Where(s => !(s.start + s.length <= spanStart || s.start >= remainingEnd))
                               .OrderBy(s => s.start)
                               .ToList();
        if (affected.Count == 0) return;

        var first = affected.First();
        var last = affected.Last();

        var fullBefore = first.textEl.Text;
        var firstOffset = spanStart - first.start;
        var before = fullBefore.Substring(0, Math.Max(0, firstOffset));

        var fullLast = last.textEl.Text;
        var lastEndOffsetInLast = (spanStart + spanLen) - last.start;
        var after = fullLast.Substring(Math.Max(0, lastEndOffsetInLast));
        var firstRun = first.run;
        var firstText = first.textEl;

        foreach (var a in affected.Skip(1))
            a.textEl.Remove();

        firstText.Text = before;

        var replacementRun = new Run();
        var rp = firstRun.GetFirstChild<RunProperties>();
        replacementRun.Append(rp?.CloneNode(true) ?? new RunProperties());
        replacementRun.Append(new Text(replacement) { Space = SpaceProcessingModeValues.Preserve });
        firstRun.InsertAfterSelf(replacementRun);

        var afterRun = new Run();
        afterRun.Append(rp?.CloneNode(true) ?? new RunProperties());
        afterRun.Append(new Text(after) { Space = SpaceProcessingModeValues.Preserve });
        replacementRun.InsertAfterSelf(afterRun);
    }
}
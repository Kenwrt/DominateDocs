using System.Globalization;
using System.Text;

namespace LiquidDocsSite.Components.Pages.FluentValidation;

public static class ValidationHelper
{
    // Canonicalizer for duplicate-name checks (case/space/punctuation/diacritics-insensitive)
    public static string NameNormalizerToKey(string? input)
    {
        if (string.IsNullOrWhiteSpace(input)) return string.Empty;

        string formD = input.Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder(formD.Length);

        foreach (char ch in formD)
        {
            var uc = CharUnicodeInfo.GetUnicodeCategory(ch);
            if (uc == UnicodeCategory.NonSpacingMark) continue; // strip diacritics
            if (char.IsLetterOrDigit(ch))
                sb.Append(char.ToUpperInvariant(ch));
        }
        return sb.ToString();
    }

    public static bool NamesAreUniqueByNormalizedKey(List<LiquidDocsData.Models.Document>? docs)
    {
        if (docs is null || docs.Count <= 1) return true;
        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (var d in docs)
        {
            var key = NameNormalizerToKey(d?.Name);
            if (!seen.Add(key)) return false;
        }
        return true;
    }
}
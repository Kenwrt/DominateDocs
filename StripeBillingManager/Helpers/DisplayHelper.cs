using System.Globalization;
using System.Text;

namespace StripeBillingManager.Helpers;

public static class DisplayHelper
{
    public static string ToCurrencyString(this decimal amount)
    {
        return string.Format(CultureInfo.CurrentCulture, "{0:C}", amount);
    }

    public static string ToPercentageString(this decimal amount)
    {
        return string.Format(CultureInfo.CurrentCulture, "{0:P2}", amount);
    }

    public static string ToDateString(this DateTime date)
    {
        return date.ToString("d", CultureInfo.CurrentCulture);
    }

    public static string ToDateString(this DateTime? date)
    {
        if (date.HasValue)
        {
            return date.Value.ToString("d", CultureInfo.CurrentCulture);
        }
        return string.Empty;
    }

    public static string ToDateTimeString(this DateTime date)
    {
        return date.ToString("g", CultureInfo.CurrentCulture);
    }

    public static string ToDateTimeString(this DateTime? date)
    {
        if (date.HasValue)
        {
            return date.Value.ToString("g", CultureInfo.CurrentCulture);
        }
        return string.Empty;
    }

    public static string ToShortDateString(this DateTime date)
    {
        return date.ToString("d", CultureInfo.CurrentCulture);
    }

    public static string ToShortDateString(this DateTime? date)
    {
        if (date.HasValue)
        {
            return date.Value.ToString("d", CultureInfo.CurrentCulture);
        }
        return string.Empty;
    }

    public static string ToShortTimeString(this DateTime date)
    {
        return date.ToString("t", CultureInfo.CurrentCulture);
    }

    public static string ToShortTimeString(this DateTime? date)
    {
        if (date.HasValue)
        {
            return date.Value.ToString("t", CultureInfo.CurrentCulture);
        }
        return string.Empty;
    }

    public static string ToYesNoString(this bool value)
    {
        return value ? "Yes" : "No";
    }

    public static string ToYesNoString(this bool? value)
    {
        if (value.HasValue)
        {
            return value.Value ? "Yes" : "No";
        }
        return "No";
    }

    public static string ToTitleCase(this string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return text;
        }
        TextInfo textInfo = CultureInfo.CurrentCulture.TextInfo;
        return textInfo.ToTitleCase(text.ToLower());
    }

    public static string Truncate(this string text, int maxLength)
    {
        if (string.IsNullOrEmpty(text) || maxLength <= 0)
        {
            return string.Empty;
        }
        if (text.Length <= maxLength)
        {
            return text;
        }
        return text.Substring(0, maxLength) + "...";
    }

    public static string ToInitials(this string fullName)
    {
        if (string.IsNullOrWhiteSpace(fullName))
        {
            return string.Empty;
        }
        var names = fullName.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (names.Length == 0)
        {
            return string.Empty;
        }
        var initials = new StringBuilder();
        foreach (var name in names)
        {
            initials.Append(char.ToUpper(name[0]));
        }
        return initials.ToString();
    }

    public static string ToFileSizeString(this long byteCount)
    {
        string[] suf = { "B", "KB", "MB", "GB", "TB", "PB", "EB" }; // Longs run out around EB
        if (byteCount == 0)
            return "0" + suf[0];
        long bytes = Math.Abs(byteCount);
        int place = Convert.ToInt32(Math.Floor(Math.Log(bytes, 1024)));
        double num = Math.Round(bytes / Math.Pow(1024, place), 1);
        return (Math.Sign(byteCount) * num).ToString() + " " + suf[place];
    }

    public static string ToCamelCase(this string text)
    {
        if (string.IsNullOrWhiteSpace(text) || text.Length < 2)
        {
            return text;
        }
        return char.ToLower(text[0]) + text.Substring(1);
    }

    public static string ToPascalCase(this string text)
    {
        if (string.IsNullOrWhiteSpace(text) || text.Length < 2)
        {
            return text;
        }
        return char.ToUpper(text[0]) + text.Substring(1);
    }

    public static string ToSentenceCase(this string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return text;
        }
        return char.ToUpper(text[0]) + text.Substring(1).ToLower();
    }

    public static string ToEnumString<T>(this T enumValue) where T : Enum
    {
        return enumValue.ToString().Replace("_", " ");
    }

    public static string ToEnumString(this int enumValue, Type enumType)
    {
        if (Enum.IsDefined(enumType, enumValue))
        {
            var value = (Enum)Enum.ToObject(enumType, enumValue);
            return value.ToString().Replace("_", " ");
        }
        return enumValue.ToString();
    }

    public static string ToEnumString(this string enumValue, Type enumType)
    {
        if (Enum.TryParse(enumType, enumValue, out var value))
        {
            return value.ToString().Replace("_", " ");
        }
        return enumValue;
    }

    public static string ToPlural(this string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return text;
        }
        if (text.EndsWith("y", StringComparison.OrdinalIgnoreCase) && text.Length > 1 && !IsVowel(text[text.Length - 2]))
        {
            return text.Substring(0, text.Length - 1) + "ies";
        }
        else if (text.EndsWith("s", StringComparison.OrdinalIgnoreCase) || text.EndsWith("x", StringComparison.OrdinalIgnoreCase) || text.EndsWith("z", StringComparison.OrdinalIgnoreCase) || text.EndsWith("ch", StringComparison.OrdinalIgnoreCase) || text.EndsWith("sh", StringComparison.OrdinalIgnoreCase))
        {
            return text + "es";
        }
        else
        {
            return text + "s";
        }
    }

    private static bool IsVowel(char c)
    {
        return "aeiouAEIOU".IndexOf(c) >= 0;
    }

    public static string FormatAsMillions(this decimal amount)
    {
        if (amount >= 1_000_000)
        {
            return (amount / 1_000_000M).ToString("0.##") + "M";
        }
        else if (amount >= 1_000)
        {
            return (amount / 1_000M).ToString("0.##") + "K";
        }
        else
        {
            return amount.ToString("0.##");
        }
    }

    public static string FormatAsMillions(this double amount)
    {
        if (amount >= 1_000_000)
        {
            return (amount / 1_000_000D).ToString("0.##") + "M";
        }
        else if (amount >= 1_000)
        {
            return (amount / 1_000D).ToString("0.##") + "K";
        }
        else
        {
            return amount.ToString("0.##");
        }
    }

    public static string CapitalizeWordsNoSpaces(string input)
    {
        if (string.IsNullOrWhiteSpace(input)) return string.Empty;

        var words = input.Split((char[])null, StringSplitOptions.RemoveEmptyEntries);
        return string.Concat(words.Select(w =>
        {
            var first = char.ToUpperInvariant(w[0]);
            return w.Length == 1 ? first.ToString() : first + w.Substring(1);
        }));
    }

    public static string FormatDollarsCompact(decimal amount)
    {
        if (amount == 0) return "$0";

        var sign = amount < 0 ? "-" : "";
        var abs = Math.Abs(amount);

        // Compact for ≥ $1,000,000 -> $1.2M (1 decimal, rounded)
        if (abs >= 1_000_000m)
            return $"{sign}${abs / 1_000_000m:0.#}M";

        // Otherwise $ with thousands separators, no cents
        return $"{sign}${abs:0,0}";
    }

    public static string FormatDollarsCompact(string amountString)
    {
        decimal amount;

        if (!string.IsNullOrEmpty(amountString)) return "$0";

        if (!decimal.TryParse(amountString, out var Amount))
        {
            return amountString; // Return original string if parsing fails
        }

        amount = Amount;

        var sign = amount < 0 ? "-" : "";
        var abs = Math.Abs(amount);

        // Compact for ≥ $1,000,000 -> $1.2M (1 decimal, rounded)
        if (abs >= 1_000_000m)
            return $"{sign}${abs / 1_000_000m:0.#}M";

        // Otherwise $ with thousands separators, no cents
        return $"{sign}${abs:0,0}";
    }

    // Nullable-friendly wrapper if needed
    public static string FormatDollarsCompact(decimal? amount) =>
        amount is null ? "" : FormatDollarsCompact(amount.Value);
}
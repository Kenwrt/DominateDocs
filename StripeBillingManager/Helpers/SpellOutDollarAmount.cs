namespace StripeBillingManager.Helpers;

using System;
using System.Text;

public static class ToWords
{
    private static readonly string[] Units = { "Zero", "One", "Two", "Three", "Four", "Five", "Six", "Seven", "Eight", "Nine", "Ten", "Eleven", "Twelve", "Thirteen", "Fourteen", "Fifteen", "Sixteen", "Seventeen", "Eighteen", "Nineteen" };
    private static readonly string[] Tens = { "", "", "Twenty", "Thirty", "Forty", "Fifty", "Sixty", "Seventy", "Eighty", "Ninety" };
    private static readonly string[] Thousands = { "", "Thousand", "Million", "Billion", "Trillion" };

    public static string DollarsToWords(decimal amount)
    {
        if (amount < 0)
            throw new ArgumentOutOfRangeException(nameof(amount), "Amount cannot be negative.");

        long dollars = (long)Math.Floor(amount);
        int cents = (int)((amount - dollars) * 100);

        var sb = new StringBuilder();

        if (dollars == 0)
        {
            sb.Append("Zero Dollars");
        }
        else
        {
            sb.Append(NumberToWords(dollars));
            sb.Append(dollars == 1 ? " Dollar" : " Dollars");
        }

        if (cents > 0)
        {
            sb.Append(" and ");
            sb.Append(NumberToWords(cents));
            sb.Append(cents == 1 ? " Cent" : " Cents");
        }

        return sb.ToString();
    }

    private static string NumberToWords(long number)
    {
        if (number == 0)
            return Units[0];

        int thousandGroup = 0;
        var sb = new StringBuilder();

        while (number > 0)
        {
            int threeDigits = (int)(number % 1000);
            if (threeDigits != 0)
            {
                var groupText = ThreeDigitToWords(threeDigits);
                if (thousandGroup > 0)
                    groupText += " " + Thousands[thousandGroup];
                if (sb.Length > 0)
                    groupText += " ";
                sb.Insert(0, groupText);
            }
            number /= 1000;
            thousandGroup++;
        }

        return sb.ToString().Trim();
    }

    private static string ThreeDigitToWords(int number)
    {
        var sb = new StringBuilder();

        if (number >= 100)
        {
            sb.Append(Units[number / 100]);
            sb.Append(" Hundred");
            number %= 100;
            if (number > 0)
                sb.Append(" ");
        }

        if (number >= 20)
        {
            sb.Append(Tens[number / 10]);
            number %= 10;
            if (number > 0)
                sb.Append(" ");
        }

        if (number > 0 && number < 20)
        {
            sb.Append(Units[number]);
        }

        return sb.ToString();
    }

    public static string InterestRateToWords(decimal rate)
    {
        // Separate whole and decimal parts
        int wholePart = (int)Math.Floor(rate);
        int fractionalPart = (int)((rate - wholePart) * 100); // 2 decimal places

        string wholeWord = NumberToWords(wholePart);
        string fractionalWord = fractionalPart > 0
            ? $"and {NumberToWords(fractionalPart)} hundredths"
            : string.Empty;

        return $"{wholeWord} {fractionalWord} percent".Trim();
    }

    private static string NumberToWords(int number)
    {
        if (number == 0)
            return "zero";

        if (number < 0)
            return "minus " + NumberToWords(Math.Abs(number));

        string[] units =
        {
            "zero", "one", "two", "three", "four", "five", "six", "seven", "eight", "nine", "ten",
            "eleven", "twelve", "thirteen", "fourteen", "fifteen", "sixteen", "seventeen", "eighteen", "nineteen"
        };

        string[] tens =
        {
            "zero", "ten", "twenty", "thirty", "forty", "fifty", "sixty", "seventy", "eighty", "ninety"
        };

        if (number < 20)
            return units[number];

        if (number < 100)
            return tens[number / 10] + (number % 10 > 0 ? "-" + units[number % 10] : "");

        if (number < 1000)
            return units[number / 100] + " hundred" + (number % 100 > 0 ? " and " + NumberToWords(number % 100) : "");

        if (number < 1000000)
            return NumberToWords(number / 1000) + " thousand" + (number % 1000 > 0 ? " " + NumberToWords(number % 1000) : "");

        return number.ToString(); // fallback for large numbers
    }
}
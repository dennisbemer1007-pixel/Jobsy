using System.Text.RegularExpressions;

namespace Jobsy.Web.Services;

/// <summary>Hides shop prices from test accounts that unlock the extended test for free.</summary>
public static partial class TestAccountCopy
{
    public static string StripPrice(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return text ?? "";
        }

        var stripped = PriceParen().Replace(text, "");
        stripped = Mollie().Replace(stripped, "");
        return Regex.Replace(stripped, @"\s{2,}", " ").Trim();
    }

    [GeneratedRegex(@"\s*\([^)]*(?:€|EUR|Mollie|\d+[.,]\d{2})[^)]*\)", RegexOptions.IgnoreCase)]
    private static partial Regex PriceParen();

    [GeneratedRegex(@"€\s*\d+[.,]\d{2}|Mollie", RegexOptions.IgnoreCase)]
    private static partial Regex Mollie();
}

using System.Text.RegularExpressions;

namespace Jobsy.Core.Rules;

/// <summary>
/// City only from a Dutch home address. The street and postcode stay off the CV.
/// </summary>
public static partial class HomeAddressCity
{
    public static string? From(string? homeAddress)
    {
        if (string.IsNullOrWhiteSpace(homeAddress))
        {
            return null;
        }

        var trimmed = homeAddress.Trim();
        var match = PostcodeThenCity().Match(trimmed);
        if (match.Success)
        {
            var city = match.Groups[2].Value.Trim().TrimEnd(',');
            return string.IsNullOrWhiteSpace(city) ? null : city;
        }

        if (trimmed.Any(char.IsDigit))
        {
            return null;
        }

        return trimmed;
    }

    [GeneratedRegex(@"(\d{4}\s?[A-Za-z]{2})\s+([^,]+)$", RegexOptions.CultureInvariant)]
    private static partial Regex PostcodeThenCity();
}

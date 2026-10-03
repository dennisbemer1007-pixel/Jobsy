using System.Text.RegularExpressions;

namespace Jobsy.Web.Services;

/// <summary>
/// Dutch postcode matching for onboarding. PDOK labels a postcode area as
/// "Stationsplein, 1012AB Amsterdam", which does not start with the code.
/// </summary>
public static partial class PostcodeMatch
{
    [GeneratedRegex("^[1-9][0-9]{3} [A-Z]{2}$", RegexOptions.CultureInvariant)]
    private static partial Regex NlPostcode();

    public static string Normalize(string? value)
    {
        var raw = (value ?? string.Empty).Replace(" ", "").ToUpperInvariant();
        return raw.Length == 6 && NlCompact().IsMatch(raw)
            ? raw[..4] + " " + raw[4..]
            : (value ?? string.Empty).Trim().ToUpperInvariant();
    }

    public static bool IsNlPostcode(string? normalized)
        => !string.IsNullOrWhiteSpace(normalized) && NlPostcode().IsMatch(normalized);

    public static bool HasLeadingMatch(IEnumerable<AddressSuggestion> items, string normalizedPostcode)
    {
        var code = Compact(normalizedPostcode);
        if (code is null)
        {
            return false;
        }

        foreach (var item in items)
        {
            if (CompactLabel(item.Label).StartsWith(code, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>Prefer a label that starts with the postcode, then one that contains it.</summary>
    public static AddressSuggestion? Pick(IEnumerable<AddressSuggestion> items, string normalizedPostcode)
    {
        var code = Compact(normalizedPostcode);
        if (code is null)
        {
            return null;
        }

        AddressSuggestion? contains = null;
        foreach (var item in items)
        {
            var compact = CompactLabel(item.Label);
            if (compact.StartsWith(code, StringComparison.OrdinalIgnoreCase))
            {
                return item;
            }

            if (contains is null && compact.Contains(code, StringComparison.OrdinalIgnoreCase))
            {
                contains = item;
            }
        }

        return contains;
    }

    /// <summary>City after a leading postcode. Returns null when the label is a street.</summary>
    public static string? CityFromLabel(string? label, string normalizedPostcode)
    {
        if (string.IsNullOrWhiteSpace(label) || !IsNlPostcode(normalizedPostcode))
        {
            return null;
        }

        var trimmed = label.Trim();
        var spaced = normalizedPostcode;
        var compact = spaced.Replace(" ", "");
        string? rest = null;
        if (trimmed.StartsWith(spaced + " ", StringComparison.OrdinalIgnoreCase)
            || trimmed.StartsWith(spaced + ",", StringComparison.OrdinalIgnoreCase))
        {
            rest = trimmed[spaced.Length..];
        }
        else if (trimmed.StartsWith(compact + " ", StringComparison.OrdinalIgnoreCase)
                 || trimmed.StartsWith(compact + ",", StringComparison.OrdinalIgnoreCase))
        {
            rest = trimmed[compact.Length..];
        }

        if (string.IsNullOrWhiteSpace(rest))
        {
            return null;
        }

        rest = rest.Trim().TrimStart(',', ' ').Trim();
        var city = rest.Split(',', 2)[0].Trim();
        if (city.Length == 0
            || city.Equals("Nederland", StringComparison.OrdinalIgnoreCase)
            || city.Equals("Netherlands", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        return city;
    }

    public static string? Compact(string? value)
    {
        var raw = (value ?? string.Empty).Replace(" ", "").ToUpperInvariant();
        return NlCompact().IsMatch(raw) ? raw : null;
    }

    [GeneratedRegex("^[1-9][0-9]{3}[A-Z]{2}$", RegexOptions.CultureInvariant)]
    private static partial Regex NlCompact();

    private static string CompactLabel(string? label)
        => (label ?? string.Empty).Replace(" ", "").Replace(",", "");
}

using System.Text.RegularExpressions;

namespace Jobsy.Core.Privacy;

/// <summary>Default masking for admin personal-data surfaces (prompt 05).</summary>
public static partial class PersonalDataMasker
{
    public static string MaskEmail(string? email)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            return string.Empty;
        }

        var trimmed = email.Trim();
        var at = trimmed.IndexOf('@');
        if (at <= 0 || at == trimmed.Length - 1)
        {
            return "***";
        }

        var local = trimmed[..at];
        var domain = trimmed[(at + 1)..];
        var localMasked = local.Length == 1
            ? "*"
            : local[0] + "***";
        return $"{localMasked}@{domain}";
    }

    public static string MaskName(string? fullName)
    {
        if (string.IsNullOrWhiteSpace(fullName))
        {
            return string.Empty;
        }

        var parts = fullName.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (parts.Length == 0)
        {
            return string.Empty;
        }

        if (parts.Length == 1)
        {
            return parts[0];
        }

        var last = parts[^1];
        var initial = last.Length == 0 ? "?" : char.ToUpperInvariant(last[0]).ToString();
        return $"{parts[0]} {initial}.";
    }

    public static string MaskPhone(string? phone)
    {
        if (string.IsNullOrWhiteSpace(phone))
        {
            return string.Empty;
        }

        var digits = DigitsOnly().Replace(phone, "");
        if (digits.Length < 2)
        {
            return "•••";
        }

        var suffix = digits[^Math.Min(2, digits.Length)..];
        return $"••• ••• {suffix}";
    }

    /// <summary>NL•• •••• •••• 1234 style (last 4 visible).</summary>
    public static string MaskIban(string? iban)
    {
        if (string.IsNullOrWhiteSpace(iban))
        {
            return string.Empty;
        }

        var compact = iban.Replace(" ", "", StringComparison.Ordinal).ToUpperInvariant();
        if (compact.Length < 4)
        {
            return "••••";
        }

        var country = compact.Length >= 2 ? compact[..2] : "XX";
        var last4 = compact[^4..];
        return $"{country}•• •••• •••• {last4}";
    }

    /// <summary>Address → city only (drop street / number).</summary>
    public static string? MaskAddressToCity(string? address, string? city = null)
    {
        if (!string.IsNullOrWhiteSpace(city))
        {
            return city.Trim();
        }

        if (string.IsNullOrWhiteSpace(address))
        {
            return null;
        }

        // Heuristic: last comma-separated segment often holds city.
        var parts = address.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        return parts.Length == 0 ? null : parts[^1];
    }

    public static string? MaskAgeBand(int? ageYears)
    {
        if (ageYears is null || ageYears < 0)
        {
            return null;
        }

        var age = ageYears.Value;
        if (age < 18)
        {
            return "<18";
        }

        if (age < 25)
        {
            return "18-24";
        }

        if (age < 35)
        {
            return "25-34";
        }

        if (age < 45)
        {
            return "35-44";
        }

        if (age < 55)
        {
            return "45-54";
        }

        if (age < 65)
        {
            return "55-64";
        }

        return "65+";
    }

    public static string FormatAgeBandLabel(int? ageYears)
        => MaskAgeBand(ageYears) ?? string.Empty;

    [GeneratedRegex(@"\D")]
    private static partial Regex DigitsOnly();
}

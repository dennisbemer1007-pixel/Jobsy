using System.Text;
using System.Text.RegularExpressions;

namespace Jobsy.Core.Rules;

public static class CandidateNameRules
{
    public static string ComposeFullName(string? firstName, string? lastName, string? fallback = null)
    {
        var parts = new[] { firstName?.Trim(), lastName?.Trim() }
            .Where(p => !string.IsNullOrWhiteSpace(p));
        var composed = string.Join(' ', parts);
        if (!string.IsNullOrWhiteSpace(composed))
        {
            return composed;
        }

        return string.IsNullOrWhiteSpace(fallback) ? string.Empty : fallback.Trim();
    }

    public static (string? FirstName, string? LastName) SplitFullName(string? fullName)
    {
        if (string.IsNullOrWhiteSpace(fullName))
        {
            return (null, null);
        }

        var parts = fullName.Trim().Split(' ', 2, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (parts.Length == 1)
        {
            return (parts[0], null);
        }

        return (parts[0], parts[1]);
    }

    public static string DisplayFirstName(string? firstName, string? fullName)
    {
        if (!string.IsNullOrWhiteSpace(firstName))
        {
            return firstName.Trim();
        }

        return SplitFullName(fullName).FirstName ?? string.Empty;
    }

    public static string DisplayLastName(string? lastName, string? fullName)
    {
        if (!string.IsNullOrWhiteSpace(lastName))
        {
            return lastName.Trim();
        }

        return SplitFullName(fullName).LastName ?? string.Empty;
    }
}

public static partial class CandidatePhoneRules
{
    [GeneratedRegex(@"^[+0-9][0-9\s\-()]{5,24}$")]
    private static partial Regex PhonePattern();

    public static string? Normalize(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return null;
        }

        var trimmed = raw.Trim();
        if (trimmed.Length > 32)
        {
            trimmed = trimmed[..32];
        }

        return trimmed;
    }

    public static bool IsValid(string? phone)
    {
        if (string.IsNullOrWhiteSpace(phone))
        {
            return true; // optional
        }

        var normalized = Normalize(phone);
        return normalized is not null && PhonePattern().IsMatch(normalized);
    }

    /// <summary>Digits-only form for WhatsApp deep links (best-effort NL).</summary>
    public static string? ToWhatsAppE164Digits(string? phone)
    {
        var normalized = Normalize(phone);
        if (normalized is null)
        {
            return null;
        }

        var sb = new StringBuilder();
        foreach (var ch in normalized)
        {
            if (char.IsDigit(ch) || (ch == '+' && sb.Length == 0))
            {
                sb.Append(ch);
            }
        }

        var digits = sb.ToString().TrimStart('+');
        if (digits.StartsWith('0') && digits.Length >= 9)
        {
            digits = "31" + digits[1..];
        }

        return digits.Length >= 8 ? digits : null;
    }
}

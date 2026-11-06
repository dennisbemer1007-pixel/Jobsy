using System.Net.Mail;
using System.Text.RegularExpressions;

namespace Jobsy.Core.Rules;

/// <summary>Checks for the admin Bedrijfsgegevens form. Messages are B1 Dutch.</summary>
public static partial class PlatformCompanyFieldRules
{
    public const string KvkMessage = "Het KvK-nummer heeft 8 cijfers.";
    public const string EmailMessage = "Dit e-mailadres klopt niet.";
    public const string VatMessage = "Het btw-nummer ziet er niet goed uit. Gebruik de vorm NL123456789B01.";

    public static void EnsureValid(string? kvk, string? vat, params string?[] emails)
    {
        if (!IsValidKvk(kvk))
        {
            throw new ArgumentException(KvkMessage);
        }

        if (!IsValidDutchVat(vat))
        {
            throw new ArgumentException(VatMessage);
        }

        foreach (var email in emails)
        {
            if (!IsValidEmail(email))
            {
                throw new ArgumentException(EmailMessage);
            }
        }
    }

    public static bool IsValidKvk(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return true;
        }

        var digits = new string(value.Where(char.IsDigit).ToArray());
        return digits.Length == 8 && value.Count(char.IsDigit) == value.Count(c => !char.IsWhiteSpace(c));
    }

    public static bool IsValidEmail(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return true;
        }

        var trimmed = value.Trim();
        return trimmed.Length <= 200 && MailAddress.TryCreate(trimmed, out var parsed)
               && parsed.Address.Equals(trimmed, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>NL + 9 digits + B + 2 digits, with the 11-proef on the first 9 digits.</summary>
    public static bool IsValidDutchVat(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return true;
        }

        var compact = new string(value.Where(c => !char.IsWhiteSpace(c)).ToArray()).ToUpperInvariant();
        if (!DutchVatPattern().IsMatch(compact))
        {
            return false;
        }

        var digits = compact.Substring(2, 9);
        var sum = 0;
        for (var i = 0; i < 8; i++)
        {
            sum += (digits[i] - '0') * (9 - i);
        }

        var check = sum % 11;
        return check < 10 && check == digits[8] - '0';
    }

    [GeneratedRegex("^NL[0-9]{9}B[0-9]{2}$", RegexOptions.CultureInvariant)]
    private static partial Regex DutchVatPattern();
}

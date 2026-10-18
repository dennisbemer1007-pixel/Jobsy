using System.Text.RegularExpressions;
using Jobsy.Core.Enums;
using Jobsy.Core.Sales;

namespace Jobsy.Core.Rules;

/// <summary>Shared phone/IBAN detection for after-match messaging (client and server).</summary>
public static partial class SensitiveMessageContentRules
{
    public enum SensitiveKind
    {
        None = 0,
        Phone = 1,
        Iban = 2
    }

    public static SensitiveKind Detect(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return SensitiveKind.None;
        }

        if (ContainsIban(text))
        {
            return SensitiveKind.Iban;
        }

        return ContainsPhone(text) ? SensitiveKind.Phone : SensitiveKind.None;
    }

    public static ApplicationMessageBlockReason BlockReasonForEmployer(
        SensitiveKind kind,
        AfterMatchContactShareState share)
    {
        return kind switch
        {
            SensitiveKind.Iban when !share.IbanAllowed => ApplicationMessageBlockReason.IbanBeforeOffer,
            SensitiveKind.Phone when !share.PhoneAllowed => ApplicationMessageBlockReason.EmployerPhoneBeforeInterview,
            _ => ApplicationMessageBlockReason.None
        };
    }

    public static bool CandidateMaySendDespiteWarning(SensitiveKind kind) =>
        kind is SensitiveKind.Phone;

    private static bool ContainsPhone(string text)
    {
        foreach (Match match in PhonePattern().Matches(text))
        {
            var digits = DigitsOnly(match.Value);
            if (digits.Length is >= 9 and <= 15 && CandidatePhoneRules.IsValid(NormalizePhoneDigits(digits)))
            {
                return true;
            }
        }

        return false;
    }

    private static bool ContainsIban(string text)
    {
        foreach (Match match in IbanPattern().Matches(text))
        {
            if (Iban.IsValid(Iban.Normalize(match.Value)))
            {
                return true;
            }
        }

        return false;
    }

    private static string NormalizePhoneDigits(string digits)
    {
        if (digits.StartsWith("31", StringComparison.Ordinal) && digits.Length >= 11)
        {
            return "+" + digits;
        }

        if (digits.StartsWith('0'))
        {
            return "+31" + digits[1..];
        }

        return digits;
    }

    private static string DigitsOnly(string value)
    {
        var chars = value.Where(char.IsDigit).ToArray();
        return new string(chars);
    }

    [GeneratedRegex(@"\b(?:\+?\d[\d\s\-().]{7,}\d)\b", RegexOptions.CultureInvariant)]
    private static partial Regex PhonePattern();

    [GeneratedRegex(@"\b[A-Z]{2}\s?[0-9]{2}(?:\s?[A-Z0-9]{4}){2,7}\b", RegexOptions.CultureInvariant | RegexOptions.IgnoreCase)]
    private static partial Regex IbanPattern();
}

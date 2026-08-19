using System.Net.Mail;
using System.Text.RegularExpressions;

namespace Jobsy.Core.Rules;

public static partial class CandidateReferenceRules
{
    public const int MaxPerCandidate = 8;
    public const int MaxMinimumOnVacancy = 5;

    public static bool IsComplete(string? employerName, string? contactName, string? email, string? phone)
        => !string.IsNullOrWhiteSpace(NormalizeName(employerName))
           && !string.IsNullOrWhiteSpace(NormalizeName(contactName))
           && IsValidEmail(email)
           && CandidatePhoneRules.IsValid(phone)
           && !string.IsNullOrWhiteSpace(CandidatePhoneRules.Normalize(phone));

    public static string? ValidateEntry(string? employerName, string? contactName, string? email, string? phone)
    {
        if (string.IsNullOrWhiteSpace(NormalizeName(employerName)))
        {
            return "Vul de werkgever in bij elke recensie.";
        }

        if (string.IsNullOrWhiteSpace(NormalizeName(contactName)))
        {
            return "Vul de naam van de contactpersoon in bij elke recensie.";
        }

        if (!IsValidEmail(email))
        {
            return "Vul een geldig e-mailadres in bij elke recensie.";
        }

        var phoneNormalized = CandidatePhoneRules.Normalize(phone);
        if (string.IsNullOrWhiteSpace(phoneNormalized) || !CandidatePhoneRules.IsValid(phoneNormalized))
        {
            return "Vul een geldig telefoonnummer in bij elke recensie.";
        }

        return null;
    }

    public static string? NormalizeName(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var trimmed = value.Trim();
        return trimmed.Length > 200 ? trimmed[..200] : trimmed;
    }

    public static string? NormalizeEmail(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var trimmed = value.Trim();
        return trimmed.Length > 256 ? trimmed[..256] : trimmed;
    }

    public static bool IsValidEmail(string? email)
    {
        var normalized = NormalizeEmail(email);
        if (normalized is null || !EmailShape().IsMatch(normalized))
        {
            return false;
        }

        try
        {
            _ = new MailAddress(normalized);
            return true;
        }
        catch (FormatException)
        {
            return false;
        }
    }

    public static int CountComplete(
        IEnumerable<(string EmployerName, string ContactName, string Email, string Phone)> entries)
        => entries.Count(e => IsComplete(e.EmployerName, e.ContactName, e.Email, e.Phone));

    [GeneratedRegex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex EmailShape();
}

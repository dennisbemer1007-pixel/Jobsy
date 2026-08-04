using System.Text.RegularExpressions;
using Jobsy.Core.Entities;

namespace Jobsy.Core.Rules;

/// <summary>Validatie van stage-exclusiviteit (masterdata-gedreven).</summary>
public static class ExclusivityRules
{
    public const string DefaultOpenName = "Open voor alle studenten";

    /// <summary>Stable seed id for the default open option.</summary>
    public static readonly Guid DefaultOpenOptionId = Guid.Parse("a0000000-0000-4000-8000-000000000001");

    public static bool RequiresApplicantExtras(ExclusivitySetting? setting)
        => setting is { IsOpenOption: false };

    public static string BadgeText(ExclusivitySetting? setting)
        => setting is null || setting.IsOpenOption
            ? DefaultOpenName
            : setting.Name;

    public static string? ValidateSchoolEmail(ExclusivitySetting setting, string? schoolEmail)
    {
        if (setting.IsOpenOption)
        {
            return null;
        }

        if (string.IsNullOrWhiteSpace(schoolEmail))
        {
            return $"Deze stageplek is exclusief voor studenten van {setting.Name}. Gebruik je school‑e-mailadres.";
        }

        var email = schoolEmail.Trim().ToLowerInvariant();
        var at = email.LastIndexOf('@');
        if (at <= 0 || at == email.Length - 1 || email.Count(c => c == '@') != 1)
        {
            return $"Deze stageplek is exclusief voor studenten van {setting.Name}. Gebruik je school‑e-mailadres.";
        }

        if (string.IsNullOrWhiteSpace(setting.SchoolDomain))
        {
            return null;
        }

        // Exact domain match only (no evil.{domain} / parent-suffix tricks).
        var domain = setting.SchoolDomain.Trim().TrimStart('@').ToLowerInvariant();
        var mailDomain = email[(at + 1)..];
        if (!string.Equals(mailDomain, domain, StringComparison.Ordinal))
        {
            return $"Deze stageplek is exclusief voor studenten van {setting.Name}. Gebruik je school‑e-mailadres.";
        }

        return null;
    }

    public static string? ValidateStudentNumber(ExclusivitySetting setting, string? studentNumber)
    {
        if (setting.IsOpenOption)
        {
            return null;
        }

        if (string.IsNullOrWhiteSpace(studentNumber))
        {
            return $"Het ingevoerde studentnummer voldoet niet aan het formaat van {setting.Name}.";
        }

        if (string.IsNullOrWhiteSpace(setting.StudentNumberPattern))
        {
            return null;
        }

        try
        {
            if (!Regex.IsMatch(studentNumber.Trim(), setting.StudentNumberPattern, RegexOptions.CultureInvariant, TimeSpan.FromMilliseconds(250)))
            {
                return $"Het ingevoerde studentnummer voldoet niet aan het formaat van {setting.Name}.";
            }
        }
        catch (RegexParseException)
        {
            return $"Het ingevoerde studentnummer voldoet niet aan het formaat van {setting.Name}.";
        }
        catch (RegexMatchTimeoutException)
        {
            return $"Het ingevoerde studentnummer voldoet niet aan het formaat van {setting.Name}.";
        }

        return null;
    }

    public static string? ValidateStudyProgram(ExclusivitySetting setting, string? studyProgram)
    {
        if (setting.IsOpenOption)
        {
            return null;
        }

        var allowed = setting.Educations?
            .Where(e => e.IsActive)
            .Select(e => e.Name)
            .ToList() ?? [];

        if (allowed.Count == 0)
        {
            // No list configured → any non-empty opleiding accepted.
            return string.IsNullOrWhiteSpace(studyProgram)
                ? "Kies je opleiding."
                : null;
        }

        if (string.IsNullOrWhiteSpace(studyProgram))
        {
            return "Kies je opleiding.";
        }

        if (!allowed.Any(a => string.Equals(a, studyProgram.Trim(), StringComparison.OrdinalIgnoreCase)))
        {
            return $"Kies een opleiding die hoort bij {setting.Name}.";
        }

        return null;
    }

    public static string? ValidateApplicantExtras(
        ExclusivitySetting setting,
        string? studentNumber,
        string? schoolEmail,
        string? studyProgram)
    {
        if (!RequiresApplicantExtras(setting))
        {
            return null;
        }

        return ValidateSchoolEmail(setting, schoolEmail)
               ?? ValidateStudentNumber(setting, studentNumber)
               ?? ValidateStudyProgram(setting, studyProgram);
    }

    public static string? ValidatePatternSyntax(string? pattern)
    {
        if (string.IsNullOrWhiteSpace(pattern))
        {
            return null;
        }

        try
        {
            _ = new Regex(pattern, RegexOptions.CultureInvariant, TimeSpan.FromMilliseconds(250));
            return null;
        }
        catch (Exception)
        {
            return "Studentnummerpatroon is geen geldige reguliere expressie.";
        }
    }

    public static string? NormalizeDomain(string? domain)
    {
        if (string.IsNullOrWhiteSpace(domain))
        {
            return null;
        }

        var trimmed = domain.Trim().TrimStart('@').ToLowerInvariant();
        return string.IsNullOrWhiteSpace(trimmed) ? null : trimmed;
    }

    /// <summary>
    /// Soft eligibility for UI (profile e-mail). Does not replace apply-time school-email validation.
    /// </summary>
    public static bool ProfileEmailLooksEligible(ExclusivitySetting? setting, string? profileEmail)
    {
        if (setting is null || setting.IsOpenOption || string.IsNullOrWhiteSpace(setting.SchoolDomain))
        {
            return true;
        }

        if (string.IsNullOrWhiteSpace(profileEmail))
        {
            return false;
        }

        return ValidateSchoolEmail(setting, profileEmail) is null;
    }
}

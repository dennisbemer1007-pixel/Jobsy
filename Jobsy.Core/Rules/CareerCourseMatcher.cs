using System.Globalization;
using System.Text;
using Jobsy.Core.Contracts;
using Jobsy.Core.Enums;

namespace Jobsy.Core.Rules;

/// <summary>
/// Matches career-plan course names to profile certificates (name only; not education levels).
/// </summary>
public static class CareerCourseMatcher
{
    public const int MinMatchLength = 4;

    private static readonly HashSet<string> FillerWords = new(StringComparer.Ordinal)
    {
        "cursus", "training", "opleiding", "basis", "module", "workshop"
    };

    /// <summary>
    /// Normalize for matching: VacancyTextSearch.Normalize + strip diacritics + drop filler words.
    /// </summary>
    public static string Normalize(string? value)
    {
        var baseNormalized = VacancyTextSearch.Normalize(StripDiacritics(value));
        if (baseNormalized.Length == 0)
        {
            return string.Empty;
        }

        var tokens = baseNormalized
            .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(t => t.Length > 0 && !FillerWords.Contains(t))
            .ToList();
        return string.Join(' ', tokens);
    }

    public static bool IsMatch(string? courseName, string? certificateName)
    {
        var course = Normalize(courseName);
        var cert = Normalize(certificateName);
        if (course.Length < MinMatchLength || cert.Length < MinMatchLength)
        {
            return false;
        }

        return course == cert
               || course.Contains(cert, StringComparison.Ordinal)
               || cert.Contains(course, StringComparison.Ordinal);
    }

    public static bool IsOnProfile(string? courseName, IEnumerable<CandidateCertificateDto>? certificates)
    {
        if (certificates is null)
        {
            return false;
        }

        foreach (var cert in certificates)
        {
            if (IsMatch(courseName, cert.Name))
            {
                return true;
            }
        }

        return false;
    }

    public static int MatchedCount(IReadOnlyList<string> courses, IEnumerable<CandidateCertificateDto>? certificates)
    {
        if (courses.Count == 0)
        {
            return 0;
        }

        var count = 0;
        foreach (var course in courses)
        {
            if (IsOnProfile(course, certificates))
            {
                count++;
            }
        }

        return count;
    }

    /// <summary>Internal planning metric only — never show this to candidates or employers.</summary>
    public static int StepMatchPercent(IReadOnlyList<string> courses, IEnumerable<CandidateCertificateDto>? certificates)
    {
        if (courses.Count == 0)
        {
            return 0;
        }

        var matched = MatchedCount(courses, certificates);
        return (int)Math.Round(100.0 * matched / courses.Count, MidpointRounding.AwayFromZero);
    }

    public static bool AllCoursesMatched(IReadOnlyList<string> courses, IEnumerable<CandidateCertificateDto>? certificates)
        => courses.Count > 0 && MatchedCount(courses, certificates) == courses.Count;

    /// <summary>Localization key for why an offer fits (no percentages).</summary>
    public static string RelevanceReasonKey(
        TrainingOfferSection section,
        string? competencyCategory,
        string? stepTitle)
    {
        if (section == TrainingOfferSection.NextStep && !string.IsNullOrWhiteSpace(stepTitle))
        {
            return "Training.Reason.NextStep";
        }

        if (section == TrainingOfferSection.Growth && !string.IsNullOrWhiteSpace(competencyCategory))
        {
            return $"Training.Reason.Growth.{competencyCategory}";
        }

        return section switch
        {
            TrainingOfferSection.General => "Training.Reason.General",
            _ => "Training.Reason.Fallback"
        };
    }

    public static string FormatRelevanceReason(
        bool english,
        string? reasonNl,
        string? reasonEn,
        string fallbackNl,
        string fallbackEn)
    {
        var custom = english ? reasonEn : reasonNl;
        if (!string.IsNullOrWhiteSpace(custom))
        {
            return custom.Trim();
        }

        return english ? fallbackEn : fallbackNl;
    }

    private static string StripDiacritics(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var normalized = value.Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder(normalized.Length);
        foreach (var ch in normalized)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(ch) != UnicodeCategory.NonSpacingMark)
            {
                sb.Append(ch);
            }
        }

        return sb.ToString().Normalize(NormalizationForm.FormC);
    }
}

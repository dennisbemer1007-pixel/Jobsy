using System.Globalization;
using System.Text;
using Jobsy.Core.Contracts;

namespace Jobsy.Core.Rules;

/// <summary>
/// Drops CV-extraction fields that are not literally present in the source CV text (no invented employers, etc.).
/// </summary>
public static class CvExtractionGrounding
{
    public static CvExtractedProfile Filter(CvExtractedProfile extracted, string cvText)
    {
        var haystack = Normalize(cvText);
        if (haystack.Length == 0)
        {
            return extracted with
            {
                AboutMe = null,
                FirstName = null,
                LastName = null,
                PhoneNumber = null,
                Employers = [],
                Certificates = [],
                Educations = [],
                Roles = [],
                DrivingLicenses = []
            };
        }

        var employers = extracted.Employers?
            .Where(e => ContainsPhrase(haystack, e.EmployerName))
            .Where(e => string.IsNullOrWhiteSpace(e.Role) || ContainsPhrase(haystack, e.Role))
            .Take(12)
            .ToList();

        var certificates = extracted.Certificates?
            .Where(c => ContainsPhrase(haystack, c.Name))
            .Take(20)
            .ToList();

        var educations = FilterList(extracted.Educations, haystack);
        var roles = FilterList(extracted.Roles, haystack);
        var licenses = FilterList(extracted.DrivingLicenses, haystack);

        return extracted with
        {
            AboutMe = null,
            FirstName = null,
            LastName = null,
            PhoneNumber = null,
            Employers = employers,
            Certificates = certificates,
            Educations = educations,
            Roles = roles,
            DrivingLicenses = licenses
        };
    }

    private static List<string>? FilterList(IReadOnlyList<string>? items, string haystack)
    {
        if (items is null || items.Count == 0)
        {
            return null;
        }

        var list = items.Where(i => ContainsPhrase(haystack, i)).Distinct(StringComparer.OrdinalIgnoreCase).Take(12).ToList();
        return list.Count == 0 ? null : list;
    }

    internal static bool ContainsPhrase(string haystack, string? phrase)
    {
        var needle = Normalize(phrase);
        return needle.Length > 0 && haystack.Contains(needle, StringComparison.Ordinal);
    }

    internal static string Normalize(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return string.Empty;
        }

        var sb = new StringBuilder(text.Length);
        foreach (var ch in text.Trim().ToLowerInvariant())
        {
            if (char.IsLetterOrDigit(ch))
            {
                sb.Append(ch);
            }
            else if (char.IsWhiteSpace(ch) || ch is '-' or '\'' or '.' or '/' or '\\')
            {
                sb.Append(' ');
            }
        }

        var collapsed = CollapseSpaces(sb.ToString());
        return RemoveDiacritics(collapsed);
    }

    private static string CollapseSpaces(string value)
    {
        var parts = value.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        return parts.Length == 0 ? string.Empty : string.Join(' ', parts);
    }

    private static string RemoveDiacritics(string text)
    {
        var normalized = text.Normalize(NormalizationForm.FormD);
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

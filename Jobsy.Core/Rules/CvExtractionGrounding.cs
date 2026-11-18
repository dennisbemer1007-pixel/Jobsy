using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using Jobsy.Core.Contracts;

namespace Jobsy.Core.Rules;

/// <summary>
/// Drops CV-extraction fields that are not literally present in the source CV text (no invented employers, etc.).
/// </summary>
public static class CvExtractionGrounding
{
    private static readonly string[] EducationTokens = ["vmbo", "mbo", "havo", "vwo", "hbo", "wo"];

    private static readonly Regex DrivingLicenseHarvestRegex = new(
        @"(?:rijbewijs|rij\s*bewijs|driving\s*licen[cs]e)[^a-z0-9]{0,24}([a-z0-9,\s+/]+)",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);

    private static readonly string[] DrivingLicenseCodes = ["AM", "A1", "A2", "A", "B", "BE", "C", "CE", "D", "DE"];

    private static readonly Regex WorkExperienceSectionRegex = new(
        @"werkervaring\s+(.+?)\s+opleiding",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);

    private static readonly Regex CertificateTokenRegex = new(
        @"\b(bhv|vca(?:\s*basis)?|haccp|vvo|ehbo)\b",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);

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

        var employers = MergeEmployers(extracted.Employers, cvText, haystack);

        var certificates = MergeCertificates(extracted.Certificates, haystack);

        var educations = MergeEducations(extracted.Educations, haystack);
        var roles = FilterList(extracted.Roles, haystack);
        var licenses = MergeDrivingLicenses(extracted.DrivingLicenses, haystack);

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

    private static List<CandidateEmployerHistoryDto>? MergeEmployers(
        IReadOnlyList<CandidateEmployerHistoryDto>? fromAi,
        string cvText,
        string haystack)
    {
        var merged = new List<CandidateEmployerHistoryDto>();
        foreach (var employer in fromAi ?? [])
        {
            if (CvPdfMetadataGuard.IsPdfMetadataNoise(employer.EmployerName))
            {
                continue;
            }

            if (ContainsPhrase(haystack, employer.EmployerName))
            {
                merged.Add(employer);
            }
        }

        var harvestHaystack = Normalize(cvText);
        var section = WorkExperienceSectionRegex.Match(harvestHaystack);
        if (section.Success)
        {
            foreach (var fragment in section.Groups[1].Value.Split([',', '–', '-', ';'], StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries))
            {
                if (fragment.Length < 3 || CvPdfMetadataGuard.IsPdfMetadataNoise(fragment))
                {
                    continue;
                }

                if (!ContainsPhrase(haystack, fragment))
                {
                    continue;
                }

                if (merged.Any(e => ContainsPhrase(haystack, e.EmployerName) && ContainsPhrase(haystack, fragment)))
                {
                    continue;
                }

                merged.Add(new CandidateEmployerHistoryDto(ToDisplayPhrase(fragment)));
            }
        }

        return merged.Count == 0 ? null : merged.Take(12).ToList();
    }

    private static List<CandidateCertificateDto>? MergeCertificates(
        IReadOnlyList<CandidateCertificateDto>? fromAi,
        string haystack)
    {
        var merged = new List<CandidateCertificateDto>();
        foreach (var cert in fromAi ?? [])
        {
            if (CvPdfMetadataGuard.IsPdfMetadataNoise(cert.Name))
            {
                continue;
            }

            if (ContainsPhrase(haystack, cert.Name))
            {
                merged.Add(cert);
            }
        }

        foreach (Match match in CertificateTokenRegex.Matches(haystack))
        {
            var token = match.Groups[1].Value;
            if (CvPdfMetadataGuard.IsPdfMetadataNoise(token))
            {
                continue;
            }

            var label = CertificateDisplayLabel(token);
            if (merged.Any(c => string.Equals(c.Name, label, StringComparison.OrdinalIgnoreCase)))
            {
                continue;
            }

            if (ContainsPhrase(haystack, token))
            {
                merged.Add(new CandidateCertificateDto(label, null));
            }
        }

        return merged.Count == 0 ? null : merged.Take(20).ToList();
    }

    private static string CertificateDisplayLabel(string token)
        => token.ToUpperInvariant() switch
        {
            "VCA" or "VCA BASIS" => "VCA",
            _ => token.ToUpperInvariant()
        };

    private static string ToDisplayPhrase(string normalizedFragment)
    {
        var words = normalizedFragment.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        return string.Join(' ', words.Select(static w =>
            w.Length switch
            {
                0 => w,
                1 => w.ToUpperInvariant(),
                _ => char.ToUpperInvariant(w[0]) + w[1..]
            }));
    }

    private static List<string>? MergeEducations(IReadOnlyList<string>? fromAi, string haystack)
    {
        var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var item in fromAi ?? [])
        {
            if (ContainsPhrase(haystack, item))
            {
                _ = set.Add(CanonicalEducationLabel(item));
            }
        }

        foreach (var token in EducationTokens)
        {
            if (haystack.Contains(token, StringComparison.Ordinal))
            {
                _ = set.Add(CanonicalEducationLabel(token));
            }
        }

        var list = set.Take(12).ToList();
        return list.Count == 0 ? null : list;
    }

    private static List<string>? MergeDrivingLicenses(IReadOnlyList<string>? fromAi, string haystack)
    {
        var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var code in fromAi ?? [])
        {
            if (DrivingLicensePresent(haystack, code))
            {
                _ = set.Add(code.Trim().ToUpperInvariant());
            }
        }

        foreach (Match match in DrivingLicenseHarvestRegex.Matches(haystack))
        {
            var chunk = match.Groups[1].Value;
            foreach (var code in DrivingLicenseCodes)
            {
                if (DrivingLicensePresent(chunk, code))
                {
                    _ = set.Add(code);
                }
            }
        }

        var list = set.OrderBy(c => c, StringComparer.Ordinal).Take(12).ToList();
        return list.Count == 0 ? null : list;
    }

    private static bool DrivingLicensePresent(string haystack, string? code)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            return false;
        }

        var normalized = code.Trim().ToUpperInvariant();
        if (normalized.Length == 1)
        {
            return haystack.Contains("rijbewijs " + normalized.ToLowerInvariant(), StringComparison.Ordinal)
                   || haystack.Contains("categorie " + normalized.ToLowerInvariant(), StringComparison.Ordinal)
                   || Regex.IsMatch(
                       haystack,
                       $@"(?:^|[^a-z]){normalized.ToLowerInvariant()}(?:[^a-z]|$)",
                       RegexOptions.CultureInvariant);
        }

        return Regex.IsMatch(
            haystack,
            $@"(?:^|[^a-z]){Regex.Escape(normalized.ToLowerInvariant())}(?:[^a-z]|$)",
            RegexOptions.CultureInvariant);
    }

    private static string CanonicalEducationLabel(string value)
    {
        var token = Normalize(value);
        foreach (var known in EducationTokens)
        {
            if (token.Contains(known, StringComparison.Ordinal))
            {
                return known.ToUpperInvariant();
            }
        }

        return value.Trim();
    }

    private static List<string>? FilterList(IReadOnlyList<string>? items, string haystack)
    {
        if (items is null || items.Count == 0)
        {
            return null;
        }

        var list = items
            .Where(i => !CvPdfMetadataGuard.IsPdfMetadataNoise(i))
            .Where(i => !CvExtractionFactsGuard.ContainsForbiddenProposalFragment(i))
            .Where(i => ContainsPhrase(haystack, i))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(12)
            .ToList();
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

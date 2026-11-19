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

    private static readonly Regex EducationYearLinePrefix = new(
        @"^\s*\d{4}\s*[-–]\s*\d{4}\s+",
        RegexOptions.CultureInvariant | RegexOptions.Compiled);

    private static readonly Regex EducationSectionRegex = new(
        @"opleiding\s+(.+?)\s+(?:rijbewijs|certificaten)\b",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);

    private static readonly Regex CertificateYearRegex = new(
        @"\b(bhv|vca(?:\s*basis)?|haccp|vvo|ehbo)[\s\-]*(certificaat)?[^0-9]{0,12}(\d{4})\b",
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

        var (educations, educationSchool) = MergeEducations(extracted.Educations, extracted.EducationSchool, cvText, haystack);
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
            EducationSchool = educationSchool,
            Roles = roles,
            DrivingLicenses = licenses
        };
    }

    private static List<CandidateEmployerHistoryDto>? MergeEmployers(
        IReadOnlyList<CandidateEmployerHistoryDto>? fromAi,
        string cvText,
        string haystack)
    {
        var merged = CvWorkExperienceHarvest.Parse(cvText, haystack);

        foreach (var employer in fromAi ?? [])
        {
            if (CvPdfMetadataGuard.IsPdfMetadataNoise(employer.EmployerName)
                || !ContainsPhrase(haystack, employer.EmployerName))
            {
                continue;
            }

            var line = CvWorkExperienceHarvest.FindWorkLineContaining(cvText, employer.EmployerName);
            if (line is null)
            {
                continue;
            }

            var idx = FindEmployerIndex(merged, employer);
            if (idx < 0)
            {
                continue;
            }

            var existing = merged[idx];
            var role = employer.Role;
            if (RoleContainsYear(role)
                || (!string.IsNullOrWhiteSpace(role) && !ContainsPhrase(haystack, role)))
            {
                role = existing.Role;
            }

            merged[idx] = existing with
            {
                Role = role ?? existing.Role,
                EmployerName = existing.EmployerName.Length >= employer.EmployerName.Trim().Length
                    ? existing.EmployerName
                    : employer.EmployerName.Trim(),
            };
        }

        merged = CvWorkExperienceHarvest.DedupeEmployers(merged);
        return merged.Count == 0 ? null : merged.Take(12).ToList();
    }

    private static bool RoleContainsYear(string? role)
        => !string.IsNullOrWhiteSpace(role) && Regex.IsMatch(role, @"\d{4}", RegexOptions.CultureInvariant);

    private static int FindEmployerIndex(List<CandidateEmployerHistoryDto> list, CandidateEmployerHistoryDto candidate)
    {
        for (var i = 0; i < list.Count; i++)
        {
            if (CvWorkExperienceHarvest.EmployerRoleKey(list[i]) == CvWorkExperienceHarvest.EmployerRoleKey(candidate))
            {
                return i;
            }

            var left = list[i];
            var empA = Normalize(left.EmployerName);
            var empB = Normalize(candidate.EmployerName);
            if (empA.Contains(empB, StringComparison.Ordinal) || empB.Contains(empA, StringComparison.Ordinal))
            {
                return i;
            }
        }

        return -1;
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
                merged.Add(cert with
                {
                    Name = CvCertificateNormalize.DisplayName(cert.Name),
                    Year = cert.Year is >= 1950 and <= 2100 ? cert.Year : TryCertificateYear(haystack, cert.Name)
                });
            }
        }

        foreach (Match match in CertificateTokenRegex.Matches(haystack))
        {
            var token = match.Groups[1].Value;
            if (CvPdfMetadataGuard.IsPdfMetadataNoise(token))
            {
                continue;
            }

            var label = CvCertificateNormalize.DisplayName(token);
            if (merged.Any(c => CvCertificateNormalize.DedupKey(c.Name) == CvCertificateNormalize.DedupKey(label)))
            {
                continue;
            }

            if (ContainsPhrase(haystack, token))
            {
                merged.Add(new CandidateCertificateDto(label, TryCertificateYear(haystack, token)));
            }
        }

        var deduped = CvCertificateNormalize.Dedupe(merged);
        return deduped.Count == 0 ? null : deduped.Take(20).ToList();
    }

    private static int? TryCertificateYear(string haystack, string name)
    {
        foreach (Match match in CertificateYearRegex.Matches(haystack))
        {
            var token = match.Groups[1].Value;
            if (!ContainsPhrase(haystack, token) && !ContainsPhrase(haystack, name))
            {
                continue;
            }

            if (int.TryParse(match.Groups[3].Value, out var year) && year is >= 1950 and <= 2100)
            {
                var label = CvCertificateNormalize.DisplayName(token);
                if (CvCertificateNormalize.DedupKey(label) == CvCertificateNormalize.DedupKey(name)
                    || ContainsPhrase(haystack, name))
                {
                    return year;
                }
            }
        }

        return null;
    }

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

    private static (List<string>? Educations, string? School) MergeEducations(
        IReadOnlyList<string>? fromAi,
        string? schoolFromAi,
        string cvText,
        string haystack)
    {
        var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        string? school = string.IsNullOrWhiteSpace(schoolFromAi) ? null : schoolFromAi.Trim();

        foreach (var item in fromAi ?? [])
        {
            if (ContainsPhrase(haystack, item))
            {
                _ = set.Add(item.Trim());
            }
        }

        var section = EducationSectionRegex.Match(cvText);
        if (section.Success)
        {
            var rawLine = section.Groups[1].Value.Trim();
            var titleRaw = EducationYearLinePrefix.Replace(rawLine, string.Empty).Trim();
            var lineNorm = CollapseSpaces(Normalize(titleRaw));
            if (lineNorm.Length > 2)
            {
                var (title, institute) = SplitEducationLine(lineNorm);
                if (!string.IsNullOrWhiteSpace(title) && ContainsPhrase(haystack, title))
                {
                    _ = set.Add(ExtractEducationTitleFromRaw(titleRaw, title));
                }

                if (!string.IsNullOrWhiteSpace(institute) && ContainsPhrase(haystack, institute))
                {
                    school ??= ExtractSchoolFromRaw(rawLine, institute);
                }
            }
        }

        foreach (var token in EducationTokens)
        {
            if (haystack.Contains(token, StringComparison.Ordinal) && set.Count == 0)
            {
                _ = set.Add(CanonicalEducationLabel(token));
            }
        }

        var list = set.Take(12).ToList();
        return list.Count == 0 ? (null, school) : (list, school);
    }

    private static (string Title, string? School) SplitEducationLine(string normalizedLine)
    {
        var rocIdx = normalizedLine.IndexOf(" roc ", StringComparison.Ordinal);
        if (rocIdx > 0)
        {
            return (normalizedLine[..rocIdx].Trim(), normalizedLine[rocIdx..].Trim());
        }

        return (normalizedLine.Trim(), null);
    }

    private static string ExtractEducationTitleFromRaw(string rawLine, string normalizedTitle)
    {
        var withoutYears = EducationYearLinePrefix.Replace(rawLine, string.Empty).Trim();
        var comma = withoutYears.IndexOf(',', StringComparison.Ordinal);
        var slice = comma > 0 ? withoutYears[..comma].Trim() : withoutYears;
        var idx = slice.IndexOf("ROC", StringComparison.OrdinalIgnoreCase);
        if (idx > 0)
        {
            slice = slice[..idx].Trim().TrimEnd(',');
        }

        return slice.Length > 0 ? slice : ToDisplayPhrase(normalizedTitle);
    }

    private static string ExtractSchoolFromRaw(string rawLine, string normalizedSchool)
    {
        var idx = rawLine.IndexOf("ROC", StringComparison.OrdinalIgnoreCase);
        if (idx >= 0)
        {
            var school = rawLine[idx..].Trim();
            var comma = school.IndexOf(',', StringComparison.Ordinal);
            return comma > 0 ? school[..comma].Trim() : school;
        }

        return ToDisplayPhrase(normalizedSchool);
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

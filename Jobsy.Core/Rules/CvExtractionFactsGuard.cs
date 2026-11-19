using Jobsy.Core.Contracts;

namespace Jobsy.Core.Rules;

/// <summary>
/// Final fail-closed pass: proposals may only contain literals present in visible CV text, never PDF metadata.
/// </summary>
public static class CvExtractionFactsGuard
{
    private static readonly string[] ForbiddenInSavedValue =
    [
        "reportlab",
        "opensource",
        "pdf library",
        "adobe pdf",
        "microsoft word",
        "skia/pdf",
        "wkhtmltopdf",
        "itext",
        "fpdf",
        "ghostscript",
        "libreoffice",
        "openoffice",
    ];

    public static CvExtractedProfile Apply(CvExtractedProfile extracted, string visibleCvText)
    {
        var grounded = CvExtractionGrounding.Filter(extracted, visibleCvText);
        var haystack = CvExtractionGrounding.Normalize(visibleCvText);
        return grounded with
        {
            Roles = PruneStringList(grounded.Roles, haystack),
            Educations = PruneStringList(grounded.Educations, haystack),
            DrivingLicenses = PruneStringList(grounded.DrivingLicenses, haystack),
            Employers = PruneEmployers(grounded.Employers, haystack, visibleCvText),
            Certificates = PruneCertificates(grounded.Certificates, haystack),
            EducationSchool = GroundEducationSchool(grounded.EducationSchool, haystack),
        };
    }

    private static string? GroundEducationSchool(string? school, string haystack)
    {
        if (string.IsNullOrWhiteSpace(school) || ContainsForbiddenProposalFragment(school))
        {
            return null;
        }

        return CvExtractionGrounding.ContainsPhrase(haystack, school) ? school.Trim() : null;
    }

    public static bool ContainsForbiddenProposalFragment(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        var normalized = CvExtractionGrounding.Normalize(value);
        foreach (var denied in ForbiddenInSavedValue)
        {
            if (normalized.Contains(denied, StringComparison.Ordinal))
            {
                return true;
            }
        }

        return CvPdfMetadataGuard.IsPdfMetadataNoise(value);
    }

    private static List<string>? PruneStringList(IReadOnlyList<string>? items, string haystack)
    {
        if (items is null || items.Count == 0)
        {
            return null;
        }

        var list = items
            .Where(i => !ContainsForbiddenProposalFragment(i))
            .Where(i => CvExtractionGrounding.ContainsPhrase(haystack, i))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(12)
            .ToList();
        return list.Count == 0 ? null : list;
    }

    private static List<CandidateEmployerHistoryDto>? PruneEmployers(
        IReadOnlyList<CandidateEmployerHistoryDto>? employers,
        string haystack,
        string rawCvText)
    {
        if (employers is null || employers.Count == 0)
        {
            return null;
        }

        var list = employers
            .Where(e => !ContainsForbiddenProposalFragment(e.EmployerName))
            .Where(e => CvExtractionGrounding.ContainsPhrase(haystack, e.EmployerName))
            .Select(e => SanitizeEmployer(e, haystack, rawCvText))
            .Take(12)
            .ToList();
        return list.Count == 0 ? null : list;
    }

    private static CandidateEmployerHistoryDto SanitizeEmployer(
        CandidateEmployerHistoryDto employer,
        string haystack,
        string rawCvText)
    {
        var withDates = CvEmployerDateRules.Apply(employer, haystack, rawCvText);
        if (!string.IsNullOrWhiteSpace(withDates.StartMonth)
            && !CvEmployerDateRules.YearAllowedInHaystack(haystack, withDates.StartMonth))
        {
            withDates = withDates with { StartMonth = null };
        }

        if (!string.IsNullOrWhiteSpace(withDates.EndMonth)
            && !CvEmployerDateRules.YearAllowedInHaystack(haystack, withDates.EndMonth))
        {
            withDates = withDates with { EndMonth = null, IsCurrent = false };
        }

        if (withDates.IsCurrent == true && !CvEmployerDateRules.MayShowAsCurrent(haystack, withDates))
        {
            withDates = withDates with { IsCurrent = false };
        }

        if (withDates.EndMonth is null && withDates.IsCurrent is not true)
        {
            withDates = withDates with { IsCurrent = false };
        }

        return withDates;
    }

    private static List<CandidateCertificateDto>? PruneCertificates(
        IReadOnlyList<CandidateCertificateDto>? certificates,
        string haystack)
    {
        if (certificates is null || certificates.Count == 0)
        {
            return null;
        }

        var list = certificates
            .Where(c => !ContainsForbiddenProposalFragment(c.Name))
            .Where(c => CvExtractionGrounding.ContainsPhrase(haystack, c.Name))
            .Select(c => c with { Name = CvCertificateNormalize.DisplayName(c.Name) })
            .ToList();
        var deduped = CvCertificateNormalize.Dedupe(list);
        return deduped.Count == 0 ? null : deduped.Take(20).ToList();
    }
}

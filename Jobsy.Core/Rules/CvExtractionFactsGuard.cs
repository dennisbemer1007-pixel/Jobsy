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
            Employers = PruneEmployers(grounded.Employers, haystack),
            Certificates = PruneCertificates(grounded.Certificates, haystack),
        };
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
        string haystack)
    {
        if (employers is null || employers.Count == 0)
        {
            return null;
        }

        var list = employers
            .Where(e => !ContainsForbiddenProposalFragment(e.EmployerName))
            .Where(e => CvExtractionGrounding.ContainsPhrase(haystack, e.EmployerName))
            .Take(12)
            .ToList();
        return list.Count == 0 ? null : list;
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
            .Take(20)
            .ToList();
        return list.Count == 0 ? null : list;
    }
}

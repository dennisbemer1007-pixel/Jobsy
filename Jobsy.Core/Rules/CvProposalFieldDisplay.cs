using System.Text;
using Jobsy.Core.Contracts;

namespace Jobsy.Core.Rules;

/// <summary>Plain B1 Dutch labels for CV confirm checkboxes (field name + values that will be saved).</summary>
public static class CvProposalFieldDisplay
{
    public static string Format(string fieldKey, CvExtractedProfile? suggested)
    {
        if (suggested is null)
        {
            return fieldKey;
        }

        return fieldKey switch
        {
            CvExtractionFieldKeys.DrivingLicenses => FormatDrivingLicenses(suggested),
            CvExtractionFieldKeys.Educations => FormatEducation(suggested),
            CvExtractionFieldKeys.Roles => FormatRoles(suggested),
            CvExtractionFieldKeys.Employers => FormatEmployers(suggested),
            CvExtractionFieldKeys.Certificates => FormatCertificates(suggested),
            _ => fieldKey,
        };
    }

    private static string FormatDrivingLicenses(CvExtractedProfile profile)
    {
        var codes = profile.DrivingLicenses?
            .Where(c => !string.IsNullOrWhiteSpace(c))
            .Select(c => c.Trim().ToUpperInvariant())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        if (codes is not { Count: > 0 })
        {
            return "Rijbewijs";
        }

        return codes.Count == 1
            ? $"Rijbewijs {codes[0]}"
            : $"Rijbewijs: {string.Join(", ", codes)}";
    }

    private static string FormatEducation(CvExtractedProfile profile)
    {
        var parts = new List<string>();
        if (profile.Educations is { Count: > 0 })
        {
            parts.Add(string.Join("; ", profile.Educations.Where(e => !string.IsNullOrWhiteSpace(e)).Select(e => e.Trim())));
        }

        if (!string.IsNullOrWhiteSpace(profile.EducationSchool))
        {
            parts.Add(profile.EducationSchool.Trim());
        }

        return parts.Count == 0 ? "Opleiding" : string.Join(" · ", parts);
    }

    private static string FormatRoles(CvExtractedProfile profile)
    {
        var roles = profile.Roles?
            .Where(r => !string.IsNullOrWhiteSpace(r))
            .Select(r => r.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        return roles is not { Count: > 0 }
            ? "Gewenste rollen"
            : $"Gewenste rollen: {string.Join(", ", roles)}";
    }

    private static string FormatEmployers(CvExtractedProfile profile)
    {
        var items = profile.Employers?
            .Where(e => !string.IsNullOrWhiteSpace(e.EmployerName))
            .Select(FormatEmployerLine)
            .Where(s => s.Length > 0)
            .ToList();
        return items is not { Count: > 0 }
            ? "Werkervaring"
            : string.Join("; ", items);
    }

    private static string FormatEmployerLine(CandidateEmployerHistoryDto emp)
    {
        var sb = new StringBuilder(emp.EmployerName.Trim());
        if (!string.IsNullOrWhiteSpace(emp.Role))
        {
            sb.Append(", ");
            sb.Append(emp.Role.Trim());
        }

        var range = FormatYearRange(emp);
        if (range.Length > 0)
        {
            sb.Append(", ");
            sb.Append(range);
        }

        return sb.ToString();
    }

    private static string FormatYearRange(CandidateEmployerHistoryDto emp)
    {
        var startYear = YearFromMonth(emp.StartMonth);
        var endYear = YearFromMonth(emp.EndMonth);
        if (startYear is not null && endYear is not null)
        {
            return $"{startYear}–{endYear}";
        }

        if (startYear is not null && emp.IsCurrent == true)
        {
            return $"{startYear}–heden";
        }

        if (startYear is not null)
        {
            return startYear;
        }

        if (endYear is not null)
        {
            return endYear;
        }

        return string.Empty;
    }

    private static string? YearFromMonth(string? yyyyMm)
    {
        if (string.IsNullOrWhiteSpace(yyyyMm) || yyyyMm.Length < 4)
        {
            return null;
        }

        return yyyyMm[..4];
    }

    private static string FormatCertificates(CvExtractedProfile profile)
    {
        var items = profile.Certificates?
            .Where(c => !string.IsNullOrWhiteSpace(c.Name))
            .Select(c => c.Year is int y ? $"{c.Name.Trim()} ({y})" : c.Name.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        return items is not { Count: > 0 }
            ? "Certificaten"
            : string.Join("; ", items);
    }
}

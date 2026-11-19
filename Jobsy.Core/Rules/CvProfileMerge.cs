using Jobsy.Core.Contracts;

namespace Jobsy.Core.Rules;

/// <summary>
/// Fills empty candidate-profile fields from a CV extraction. Never overwrites values the candidate already set.
/// </summary>
public static class CvProfileMerge
{
    public static CvProfileMergeResult Apply(
        string? existingFirstName,
        string? existingLastName,
        string? existingPhone,
        CandidatePreferencesDto existing,
        CvExtractedProfile extracted)
    {
        var filled = new List<string>();

        var first = existingFirstName;
        var last = existingLastName;
        var phone = existingPhone;

        // AI never writes AboutMe; the candidate writes that field themselves.
        var about = existing.AboutMe;

        var licenses = MergeDistinct(existing.DrivingLicenses, extracted.DrivingLicenses);
        if (licenses.Count > (existing.DrivingLicenses?.Count ?? 0))
        {
            filled.Add("rijbewijs");
        }

        var educations = MergeDistinct(existing.Educations, extracted.Educations);
        if (educations.Count > (existing.Educations?.Count ?? 0))
        {
            filled.Add("opleiding");
        }

        var roles = MergeDistinct(existing.Roles, extracted.Roles);
        if (roles.Count > existing.Roles.Count)
        {
            filled.Add("gewenste rollen");
        }

        var employers = MergeEmployers(existing.Employers, extracted.Employers);
        if (employers.Count > (existing.Employers?.Count ?? 0))
        {
            filled.Add("werkervaring");
        }

        var certificates = MergeCertificates(existing.Certificates, extracted.Certificates);
        if (certificates.Count > (existing.Certificates?.Count ?? 0))
        {
            filled.Add("certificaten");
        }

        var educationDirection = existing.EducationDirection;
        if (string.IsNullOrWhiteSpace(educationDirection)
            && !string.IsNullOrWhiteSpace(extracted.EducationSchool))
        {
            educationDirection = extracted.EducationSchool.Trim();
        }

        var prefs = existing with
        {
            AboutMe = about,
            DrivingLicenses = licenses,
            Educations = educations,
            EducationDirection = educationDirection,
            Roles = roles,
            Employers = employers,
            Certificates = certificates
        };

        return new CvProfileMergeResult(first, last, phone, prefs, filled);
    }

    /// <summary>Apply only confirmed CV-suggestion field keys (see <see cref="CvExtractionFieldKeys"/>).</summary>
    public static CvProfileMergeResult ApplyConfirmed(
        string? existingFirstName,
        string? existingLastName,
        string? existingPhone,
        CandidatePreferencesDto existing,
        CvExtractedProfile extracted,
        IReadOnlyCollection<string> confirmedKeys)
    {
        var keys = new HashSet<string>(confirmedKeys, StringComparer.OrdinalIgnoreCase);
        var slice = new CvExtractedProfile(
            DrivingLicenses: keys.Contains(CvExtractionFieldKeys.DrivingLicenses) ? extracted.DrivingLicenses : null,
            Educations: keys.Contains(CvExtractionFieldKeys.Educations) ? extracted.Educations : null,
            EducationSchool: keys.Contains(CvExtractionFieldKeys.Educations) ? extracted.EducationSchool : null,
            Roles: keys.Contains(CvExtractionFieldKeys.Roles) ? extracted.Roles : null,
            Employers: keys.Contains(CvExtractionFieldKeys.Employers) ? extracted.Employers : null,
            Certificates: keys.Contains(CvExtractionFieldKeys.Certificates) ? extracted.Certificates : null);
        return Apply(existingFirstName, existingLastName, existingPhone, existing, slice);
    }

    private static List<string> MergeDistinct(IReadOnlyList<string>? existing, IReadOnlyList<string>? incoming)
    {
        var list = new List<string>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var item in existing ?? [])
        {
            if (string.IsNullOrWhiteSpace(item) || !seen.Add(item.Trim()))
            {
                continue;
            }

            list.Add(item.Trim());
        }

        foreach (var item in incoming ?? [])
        {
            if (string.IsNullOrWhiteSpace(item) || !seen.Add(item.Trim()))
            {
                continue;
            }

            list.Add(item.Trim());
        }

        return list;
    }

    private static List<CandidateEmployerHistoryDto> MergeEmployers(
        IReadOnlyList<CandidateEmployerHistoryDto>? existing,
        IReadOnlyList<CandidateEmployerHistoryDto>? incoming)
    {
        var list = (existing ?? [])
            .Where(e => !string.IsNullOrWhiteSpace(e.EmployerName))
            .ToList();
        var seen = list
            .Select(e => e.EmployerName.Trim())
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (var item in incoming ?? [])
        {
            if (string.IsNullOrWhiteSpace(item.EmployerName) || !seen.Add(item.EmployerName.Trim()))
            {
                continue;
            }

            var trimmed = item with { EmployerName = item.EmployerName.Trim() };
            if (!string.IsNullOrWhiteSpace(trimmed.EndMonth))
            {
                trimmed = trimmed with { IsCurrent = false };
            }
            else if (trimmed.IsCurrent is not true)
            {
                trimmed = trimmed with { IsCurrent = false };
            }

            list.Add(trimmed);
        }

        return list;
    }

    private static List<CandidateCertificateDto> MergeCertificates(
        IReadOnlyList<CandidateCertificateDto>? existing,
        IReadOnlyList<CandidateCertificateDto>? incoming)
    {
        var list = (existing ?? [])
            .Where(c => !string.IsNullOrWhiteSpace(c.Name))
            .ToList();
        foreach (var item in incoming ?? [])
        {
            if (string.IsNullOrWhiteSpace(item.Name))
            {
                continue;
            }

            list.Add(item with { Name = CvCertificateNormalize.DisplayName(item.Name) });
        }

        return CvCertificateNormalize.Dedupe(list);
    }
}

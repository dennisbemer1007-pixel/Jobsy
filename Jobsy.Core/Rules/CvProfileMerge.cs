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

        foreach (var item in incoming ?? [])
        {
            if (string.IsNullOrWhiteSpace(item.EmployerName))
            {
                continue;
            }

            var trimmed = item with { EmployerName = item.EmployerName.Trim() };
            var dupIdx = FindIncomingDuplicateIndex(list, trimmed);
            if (dupIdx >= 0)
            {
                list[dupIdx] = CvWorkExperienceHarvest.PreferRicher(list[dupIdx], trimmed);
                continue;
            }
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

        return CvWorkExperienceHarvest.DedupeEmployers(list);
    }

    private static int FindIncomingDuplicateIndex(
        List<CandidateEmployerHistoryDto> list,
        CandidateEmployerHistoryDto candidate)
    {
        for (var i = 0; i < list.Count; i++)
        {
            if (CvWorkExperienceHarvest.EmployerRoleKey(list[i]) == CvWorkExperienceHarvest.EmployerRoleKey(candidate))
            {
                return i;
            }

            var empA = CvExtractionGrounding.Normalize(list[i].EmployerName);
            var empB = CvExtractionGrounding.Normalize(candidate.EmployerName);
            var roleA = CvExtractionGrounding.Normalize(list[i].Role);
            var roleB = CvExtractionGrounding.Normalize(candidate.Role);
            if ((roleA.Length == 0 || roleB.Length == 0 || string.Equals(roleA, roleB, StringComparison.Ordinal))
                && (empA.Contains(empB, StringComparison.Ordinal) || empB.Contains(empA, StringComparison.Ordinal)))
            {
                return i;
            }
        }

        return -1;
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

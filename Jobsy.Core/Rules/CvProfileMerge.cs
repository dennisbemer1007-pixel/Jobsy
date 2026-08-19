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
        if (string.IsNullOrWhiteSpace(first) && !string.IsNullOrWhiteSpace(extracted.FirstName))
        {
            first = extracted.FirstName.Trim();
            filled.Add("voornaam");
        }

        var last = existingLastName;
        if (string.IsNullOrWhiteSpace(last) && !string.IsNullOrWhiteSpace(extracted.LastName))
        {
            last = extracted.LastName.Trim();
            filled.Add("achternaam");
        }

        var phone = existingPhone;
        var extractedPhone = CandidatePhoneRules.Normalize(extracted.PhoneNumber);
        if (string.IsNullOrWhiteSpace(phone)
            && !string.IsNullOrWhiteSpace(extractedPhone)
            && CandidatePhoneRules.IsValid(extractedPhone))
        {
            phone = extractedPhone;
            filled.Add("telefoon");
        }

        var about = existing.AboutMe;
        if (string.IsNullOrWhiteSpace(about) && !string.IsNullOrWhiteSpace(extracted.AboutMe))
        {
            about = extracted.AboutMe.Trim();
            if (about.Length > 2000)
            {
                about = about[..2000];
            }

            filled.Add("over mij");
        }

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

        var prefs = existing with
        {
            AboutMe = about,
            DrivingLicenses = licenses,
            Educations = educations,
            Roles = roles,
            Employers = employers,
            Certificates = certificates
        };

        return new CvProfileMergeResult(first, last, phone, prefs, filled);
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

            list.Add(item with { EmployerName = item.EmployerName.Trim() });
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
        var seen = list
            .Select(c => c.Name.Trim())
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (var item in incoming ?? [])
        {
            if (string.IsNullOrWhiteSpace(item.Name) || !seen.Add(item.Name.Trim()))
            {
                continue;
            }

            list.Add(item with { Name = item.Name.Trim() });
        }

        return list;
    }
}

using Jobsy.Core.Contracts;

namespace Jobsy.Core.Rules;

/// <summary>Dutch labels for CV extraction fields (profile UI + API).</summary>
public static class CvExtractionFieldKeys
{
    public const string DrivingLicenses = "rijbewijs";
    public const string Educations = "opleiding";
    public const string Roles = "gewenste rollen";
    public const string Employers = "werkervaring";
    public const string Certificates = "certificaten";

    public static IReadOnlyList<string> ProposalKeys { get; } =
    [
        DrivingLicenses,
        Educations,
        Roles,
        Employers,
        Certificates
    ];

    public static IReadOnlyList<string> ListProposalKeys(CvExtractedProfile profile)
    {
        var keys = new List<string>(5);
        if (profile.DrivingLicenses is { Count: > 0 })
        {
            keys.Add(DrivingLicenses);
        }

        if (profile.Educations is { Count: > 0 })
        {
            keys.Add(Educations);
        }

        if (profile.Roles is { Count: > 0 })
        {
            keys.Add(Roles);
        }

        if (profile.Employers is { Count: > 0 })
        {
            keys.Add(Employers);
        }

        if (profile.Certificates is { Count: > 0 })
        {
            keys.Add(Certificates);
        }

        return keys;
    }
}
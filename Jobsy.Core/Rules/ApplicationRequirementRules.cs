namespace Jobsy.Core.Rules;

/// <summary>Hard gates for applying to a vacancy (license, education, employers).</summary>
public static class ApplicationRequirementRules
{
    public static string? ValidateHardRequirements(
        string? requiredDrivingLicense,
        string? requiredEducation,
        int? minimumEmployers,
        IReadOnlyList<string>? candidateLicenses,
        IReadOnlyList<string>? candidateEducations,
        int candidateEmployerCount)
    {
        if (!DrivingLicenseLabels.CandidateMeetsRequirement(candidateLicenses, requiredDrivingLicense))
        {
            return $"Deze vacature vereist rijbewijs {requiredDrivingLicense}.";
        }

        if (!EducationLevelLabels.CandidateMeetsRequirement(candidateEducations, requiredEducation))
        {
            return $"Deze vacature vereist opleidingsniveau: {requiredEducation}.";
        }

        if (minimumEmployers is > 0 && candidateEmployerCount < minimumEmployers.Value)
        {
            return $"Deze vacature vereist minimaal {minimumEmployers.Value} eerdere werkgever(s).";
        }

        return null;
    }
}

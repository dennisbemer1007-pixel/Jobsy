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
        int candidateEmployerCount,
        int? minimumReferences = null,
        int candidateReferenceCount = 0)
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

        if (minimumReferences is > 0 && candidateReferenceCount < minimumReferences.Value)
        {
            return $"Deze vacature vereist minimaal {minimumReferences.Value} recensie(s) (werkgever, contactpersoon, e-mail en telefoon). Vul ze eerst aan in je profiel.";
        }

        return null;
    }
}

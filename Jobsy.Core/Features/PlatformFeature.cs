namespace Jobsy.Core.Features;

/// <summary>Platform feature toggles controlled via admin settings.</summary>
public enum PlatformFeature
{
    /// <summary>Employer/vacancy surface (banenkaart, sollicitaties, portals). Default ON.</summary>
    Employers,

    /// <summary>Candidate passport profile UI. Default OFF until enabled in a later release.</summary>
    CandidatePassport
}

namespace Jobsy.Core.Features;

/// <summary>Platform feature toggles controlled via admin settings.</summary>
public enum PlatformFeature
{
    /// <summary>Employer/vacancy surface (banenkaart, sollicitaties, portals). Default ON.</summary>
    Employers,

    /// <summary>Candidate passport profile UI. Default OFF until enabled in a later release.</summary>
    CandidatePassport,

    /// <summary>Partner portal, partner codes and consent. Default false.</summary>
    PassportPartners,

    /// <summary>DNA-paspoort PDF v2 and the shareable-preferences section. Default false.</summary>
    PassportPdfV2,

    /// <summary>School, teacher and pupil surfaces. Default false until enabled in platform settings.</summary>
    Schools
}

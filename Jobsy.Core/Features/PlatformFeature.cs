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
    Schools,

    /// <summary>Ambassador programme. Default false. Sales-manager admin stays a separate surface.</summary>
    Ambassadors,

    /// <summary>WhatsApp come-back reminders. Default false. Needs env config and a candidate opt-in.</summary>
    WhatsAppReminders,

    /// <summary>Compact ~4-page personal deep-test PDFs. Default false keeps the longer layout.</summary>
    CompactTestPdf,

    /// <summary>Stored honest career advice on job detail. Default false. No live generation.</summary>
    HonestAdvice,

    /// <summary>
    /// "Werk met toekomst dat bij jou past" on Functiefit and Carrière.
    /// Default false. Uses sourced outlook and the candidate's own test only.
    /// </summary>
    FutureJobsForYou,

    /// <summary>
    /// Phase-2 employer flows: ½-token accept, Maqqie placement, agency map radius, uren doorgeven.
    /// Default false. Requires <see cref="Employers"/> when enabled.
    /// </summary>
    EmployerPhase2,

    /// <summary>Import/share external vacancy URLs and e-mail employers. Default false.</summary>
    CandidateExternalVacancies
}

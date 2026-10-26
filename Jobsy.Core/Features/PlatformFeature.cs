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

    /// <summary>Leerling DNA-paspoort v2 (groep 7/8 + VO). Default false.</summary>
    PupilPassportV2,

    /// <summary>Student/stagiair DNA-paspoort with internships/vacancies. Default false.</summary>
    StudentPassport,

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
    CandidateExternalVacancies,

    /// <summary>
    /// Proactive matching: Lobsy zoekt voor je, werkgever-shortlist en kennismakingsverzoeken. Default false.
    /// </summary>
    ProactiveMatching,

    /// <summary>Quick-Scan v2 question sets for new candidate test rows. Default false.</summary>
    TestsV2,

    /// <summary>Per-item practice examples on Quick-Scan question flow. Default false.</summary>
    TestItemExamples,

    /// <summary>Motivation cues and “Jouw beeld tot nu toe” on Quick-Scan. Default false.</summary>
    TestProgressInsight,

    /// <summary>Optional employer team/vacancy fit questionnaires and work-context matching. Default false.</summary>
    EmployerFitQuestionnaires,

    /// <summary>
    /// Paid employer kandidatenkaart (reverse job map). Default false.
    /// Requires <see cref="Employers"/> when enabled.
    /// </summary>
    Kandidatenkaart,

    /// <summary>
    /// Bedrijvenkaart and "Ik wil hier werken" (candidate + employer). Default true on acceptatie.
    /// Requires <see cref="Employers"/> when enabled.
    /// </summary>
    CompanyMapInitiative,

    /// <summary>Na de match: gesprek plannen, berichten en uitkomst. Default false.</summary>
    AfterMatch,

    /// <summary>Geverifieerde referenties on the candidate passport. Default false.</summary>
    ReferenceVerification,

    /// <summary>Privacy controls for sensitive work history. Default false.</summary>
    SensitiveExperience,

    /// <summary>Partner workshops/opleidingen feed, Carrière leren &amp; Paspoort groeien. Default false.</summary>
    TrainingPartners,

    /// <summary>
    /// Mockup UX: mini-spiegel at 5/10, unlock cards, words-only deep report. Default on (admin can disable).
    /// </summary>
    TestDepthValueMockup,

    /// <summary>UWV werkmap: kansberoepen, ondernemerstest, drijfveren, adviseur-deel. Default on (admin can disable).</summary>
    UwvFeatures,

    /// <summary>Mijn activiteiten (UWV werkmap). Default off; on on acceptatie via config.</summary>
    ActivityLog
}

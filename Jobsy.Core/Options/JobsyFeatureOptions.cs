namespace Jobsy.Core.Options;

/// <summary>
/// Feature toggles. Database platform settings override these when present.
/// </summary>
public sealed class JobsyFeatureOptions
{
    public const string SectionName = "JobsyFeatures";

    /// <summary>
    /// When true, users can enrol a TOTP authenticator.
    /// </summary>
    public bool AuthenticatorEnabled { get; set; } = true;

    /// <summary>
    /// When true, registration API may return the activation URL in the submit response (local demo only).
    /// </summary>
    public bool ExposeRegistrationActivationLinks { get; set; }

    /// <summary>
    /// When false, vacancy create skips content moderation (AI and heuristics).
    /// </summary>
    public bool VacancyContentModerationEnabled { get; set; } = true;

    /// <summary>
    /// When true, "Antwoorden wijzigen" is available on completed tests.
    /// Requires the questionnaire pending-edit save PR. Default off until that lands on Acc.
    /// </summary>
    public bool TestEditMode { get; set; }

    /// <summary>
    /// When true, "Bekijk voorbeeld-PDF" is shown. Enabled in PR B when sample PDFs exist.
    /// </summary>
    public bool SamplePdf { get; set; }
}

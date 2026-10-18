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
    public bool AuthenticatorEnabled { get; set; }

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

    /// <summary>
    /// When true, candidates may opt in to anonymized research exports (COTAN validation). Default off.
    /// Overridden by <see cref="Entities.PlatformFeatureSettings.ResearchConsentEnabled"/> when set in admin.
    /// </summary>
    public bool ResearchConsentEnabled { get; set; }

    /// <summary>When true, new Quick-Scan rows use v2 catalogs. Overridden by platform settings when present.</summary>
    public bool TestsV2Enabled { get; set; }

    /// <summary>When true, Quick-Scan questions show per-item practice examples. Overridden by platform settings.</summary>
    public bool TestItemExamplesEnabled { get; set; }

    /// <summary>When true, Quick-Scan shows motivation + progress insight card. Overridden by platform settings.</summary>
    public bool TestProgressInsightEnabled { get; set; }

    /// <summary>
    /// Personal test mirror after verdieping/volledig free depth. Default on; disable only as emergency kill switch.
    /// </summary>
    public bool TestReflectionEnabled { get; set; } = true;
}

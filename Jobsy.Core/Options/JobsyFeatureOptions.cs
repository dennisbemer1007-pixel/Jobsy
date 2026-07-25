namespace Jobsy.Core.Options;

/// <summary>
/// Feature toggles. Database platform settings override these when present.
/// </summary>
public sealed class JobsyFeatureOptions
{
    public const string SectionName = "JobsyFeatures";

    /// <summary>
    /// When true, the apply flow accepts an optional Authenticator stub flag (no real MFA).
    /// </summary>
    public bool AuthenticatorEnabled { get; set; }

    /// <summary>
    /// When true, registration API may return the activation URL in the submit response (local demo only).
    /// </summary>
    public bool ExposeRegistrationActivationLinks { get; set; }

    /// <summary>
    /// When false, vacancy create skips content moderation (AI and heuristics).
    /// </summary>
    public bool VacancyContentModerationEnabled { get; set; } = true;
}

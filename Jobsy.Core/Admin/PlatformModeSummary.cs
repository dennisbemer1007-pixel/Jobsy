namespace Jobsy.Core.Admin;

/// <summary>
/// Tiny read-model for the dashboard Platform-modus card.
/// File 05 replaces this with <c>PlatformSettingsCatalog</c> entries marked ShowOnDashboard.
/// </summary>
public sealed record PlatformModeRow(
    string Key,
    string LabelKey,
    string ValueKey,
    bool IsOn,
    bool IsPolicyReadonly = false);

public static class PlatformModeSummary
{
    /// <summary>
    /// Builds dashboard rows from the live platform-features payload.
    /// EmployersEnabled / CandidatePassportEnabled stay commented slots until those fields exist (D7).
    /// </summary>
    public static IReadOnlyList<PlatformModeRow> Build(
        bool vacancyContentModerationEnabled)
    {
        var rows = new List<PlatformModeRow>
        {
            // Slot: Werkgevers actief — only when EmployersEnabled exists on PlatformFeatureSettings.
            // Slot: Mijn Paspoort — only when CandidatePassportEnabled exists.
            new(
                "ai-moderation",
                "AdminDash.Mode.AiModeration",
                vacancyContentModerationEnabled ? "AdminDash.Mode.On" : "AdminDash.Mode.Off",
                vacancyContentModerationEnabled),
            new(
                "mfa-policy",
                "AdminDash.Mode.Mfa",
                "AdminDash.Mode.Required",
                IsOn: true,
                IsPolicyReadonly: true)
        };
        return rows;
    }
}

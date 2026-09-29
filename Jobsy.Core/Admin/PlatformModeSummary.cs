namespace Jobsy.Core.Admin;

/// <summary>
/// Tiny read-model for the dashboard Platform-modus card.
/// Built from <c>PlatformSettingsCatalog</c> ShowOnDashboard entries (file 05).
/// </summary>
public sealed record PlatformModeRow(
    string Key,
    string LabelKey,
    string ValueKey,
    bool IsOn,
    bool IsPolicyReadonly = false);

/// <summary>
/// Compatibility shim — prefer <c>Jobsy.Web.Admin.PlatformSettingsCatalog.DashboardRows</c>.
/// Kept so Core tests can assert absent employer/passport flags without referencing Web.
/// </summary>
public static class PlatformModeSummary
{
    public static IReadOnlyList<PlatformModeRow> Build(bool vacancyContentModerationEnabled)
    {
        var rows = new List<PlatformModeRow>
        {
            // EmployersEnabled / CandidatePassportEnabled stay out until those fields exist (D7).
            new(
                "VacancyContentModerationEnabled",
                "AdminSettings.AiModeration.Title",
                vacancyContentModerationEnabled ? "AdminDash.Mode.On" : "AdminDash.Mode.Off",
                vacancyContentModerationEnabled),
            new(
                "MfaPolicy",
                "AdminSettings.Mfa.Title",
                "AdminDash.Mode.Required",
                IsOn: true,
                IsPolicyReadonly: true)
        };
        return rows;
    }
}

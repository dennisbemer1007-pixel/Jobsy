using Jobsy.Core.Features;
using Jobsy.Web.Services;

namespace Jobsy.Web.Settings;

internal static class SettingsSsrData
{
    internal sealed record MailPanelData(
        bool LoadError,
        bool SavedBanner,
        bool ReminderEmails,
        bool EmployersOn,
        bool WhatsAppAvailable,
        bool WhatsAppOptedIn,
        string? WhatsAppPhone,
        IReadOnlyList<JobsyApiClient.EmailPreferenceItemDto> Optional,
        IReadOnlyList<string> AlwaysCultureKeys);

    internal sealed record PageData(
        string Pane,
        bool ProactiveOn,
        bool KandidatenkaartOn,
        MailPanelData Mail);

    internal static async Task<PageData> LoadAsync(
        JobsyApiClient api,
        IFeatureFlags featureFlags,
        string? paneRaw,
        bool savedQuery,
        CancellationToken cancellationToken = default)
    {
        var flags = await featureFlags.GetAsync(cancellationToken);
        var pane = string.IsNullOrWhiteSpace(paneRaw) ? "mail" : paneRaw.Trim().ToLowerInvariant();
        var mail = await LoadMailPanelAsync(api, featureFlags, savedQuery, cancellationToken);

        return new PageData(
            pane,
            flags.IsEnabled(PlatformFeature.ProactiveMatching),
            flags.IsEnabled(PlatformFeature.Kandidatenkaart),
            mail);
    }

    private static async Task<MailPanelData> LoadMailPanelAsync(
        JobsyApiClient api,
        IFeatureFlags featureFlags,
        bool savedQuery,
        CancellationToken cancellationToken)
    {
        try
        {
            var dto = await api.GetMyEmailPreferencesAsync(cancellationToken);
            if (dto is null)
            {
                return new MailPanelData(true, savedQuery, true, true, false, false, null, [], []);
            }

            var employersOn = await featureFlags.IsEnabledAsync(PlatformFeature.Employers, cancellationToken);
            var optional = (dto.Optional ?? [])
                .Where(item => item.Key is not ("VacancyEngagementReminder" or "CompanyReEngagement"))
                .ToList();
            if (!employersOn)
            {
                optional.RemoveAll(item => item.Key is "PushBom");
            }

            var alwaysKeys = new List<string> { "MailSettings.Always.Codes" };
            if (employersOn)
            {
                alwaysKeys.Add("MailSettings.Always.Applications");
            }

            alwaysKeys.Add("MailSettings.Always.Security");

            var whatsApp = await api.GetReminderWhatsAppAsync(cancellationToken);
            return new MailPanelData(
                false,
                savedQuery,
                dto.ReminderEmailsEnabled,
                employersOn,
                whatsApp?.Available == true,
                whatsApp?.OptedIn == true,
                whatsApp?.Phone,
                optional,
                alwaysKeys);
        }
        catch
        {
            return new MailPanelData(true, savedQuery, true, true, false, false, null, [], []);
        }
    }
}

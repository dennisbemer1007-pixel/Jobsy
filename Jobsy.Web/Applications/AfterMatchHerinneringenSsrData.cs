using Jobsy.Core.Contracts;
using Jobsy.Core.Enums;
using Jobsy.Web.KandidaatBanen;
using Jobsy.Web.Localization;
using Jobsy.Web.Services;

namespace Jobsy.Web.Applications;

internal static class AfterMatchHerinneringenSsrData
{
    internal sealed record PageData(
        AfterMatchThreadDto Thread,
        string SollicitatiesHref,
        string RescheduleHref,
        string RouteHref,
        string TipsHref,
        string PracticeHref,
        string AssistantHref,
        AfterMatchHerinneringenFields Fields);

    internal static async Task<PageData?> LoadAsync(
        JobsyApiClient api,
        CultureState culture,
        Guid applicationId,
        string? returnUrl,
        CancellationToken cancellationToken = default)
    {
        var thread = await api.GetAfterMatchForCandidateAsync(applicationId, cancellationToken);
        if (thread is null
            || thread.InterviewStatus != InterviewStatus.Scheduled
            || thread.Outcome == AfterMatchOutcomeKind.NotNow)
        {
            return null;
        }

        return Build(culture, thread, applicationId, returnUrl);
    }

    internal static PageData Demo(CultureState culture) =>
        Build(culture, AfterMatchMailSsrDemoData.ScheduledThread(), AfterMatchMailSsrDemoData.ScheduledApplicationId, null);

    private static PageData Build(
        CultureState culture,
        AfterMatchThreadDto thread,
        Guid applicationId,
        string? returnUrl)
    {
        var backHref = ApplicationsSsrQuery.TryGetSafeApplicationsReturn(returnUrl, out var safeReturn)
            ? safeReturn
            : KbRoutes.Applications;

        const string candidateHow = "/candidate/hoe-werkt-lobsy";
        var routeHref = "https://www.google.com/maps/search/?api=1&query="
            + Uri.EscapeDataString(thread.LocationOrLink ?? thread.CompanyName);

        return new PageData(
            thread,
            backHref,
            AfterMatchSollicitatiesUrls.Reschedule(applicationId, backHref),
            routeHref,
            candidateHow,
            candidateHow,
            candidateHow,
            AfterMatchHerinneringenFieldsBuilder.Build(culture, thread, applicationId));
    }
}

using Jobsy.Core.Contracts;
using Jobsy.Core.Enums;
using Jobsy.Web.KandidaatBanen;
using Jobsy.Web.Localization;
using Jobsy.Web.Services;

namespace Jobsy.Web.Applications;

internal static class AfterMatchBevestigingSsrData
{
    internal sealed record PageData(
        AfterMatchThreadDto Thread,
        string SollicitatiesHref,
        string RescheduleHref,
        string TipsHref,
        string OpenInterviewHref,
        string AssistantHref,
        AfterMatchMailConfirmationFields MailFields);

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

        return new PageData(
            thread,
            backHref,
            AfterMatchSollicitatiesUrls.Reschedule(applicationId, backHref),
            candidateHow,
            $"/candidate/applications/{applicationId:D}/after-match",
            candidateHow,
            AfterMatchMailConfirmationFieldsBuilder.Build(culture, thread, applicationId));
    }
}

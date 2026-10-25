using Jobsy.Core.Contracts;
using Jobsy.Core.Enums;
using Jobsy.Web.KandidaatBanen;
using Jobsy.Web.Services;

namespace Jobsy.Web.Applications;

internal static class AfterMatchHerinneringenSsrData
{
    internal sealed record PageData(AfterMatchThreadDto Thread, string SollicitatiesHref);

    internal static async Task<PageData?> LoadAsync(
        JobsyApiClient api,
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

        var backHref = ApplicationsSsrQuery.TryGetSafeApplicationsReturn(returnUrl, out var safeReturn)
            ? safeReturn
            : KbRoutes.Applications;

        return new PageData(thread, backHref);
    }

    internal static PageData Demo() =>
        new(AfterMatchMailSsrDemoData.ScheduledThread(), KbRoutes.Applications);
}

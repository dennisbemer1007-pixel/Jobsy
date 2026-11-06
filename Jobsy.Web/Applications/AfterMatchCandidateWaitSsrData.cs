using Jobsy.Core.Contracts;
using Jobsy.Core.Enums;
using Jobsy.Web.KandidaatBanen;
using Jobsy.Web.Localization;
using Jobsy.Web.Services;

namespace Jobsy.Web.Applications;

internal static class AfterMatchCandidateWaitSsrData
{
    internal sealed record PageData(
        AfterMatchThreadDto Thread,
        string SollicitatiesHref,
        string MessagesHref,
        string HeroLead,
        string YesDateLabel,
        string PlanHint);

    internal static async Task<PageData?> LoadAsync(
        JobsyApiClient api,
        CultureState culture,
        Guid applicationId,
        string? returnUrl,
        CancellationToken cancellationToken = default)
    {
        var thread = await api.GetAfterMatchForCandidateAsync(applicationId, cancellationToken);
        if (thread is null || thread.InterviewStatus != InterviewStatus.RescheduleRequested)
        {
            return null;
        }

        var backHref = ApplicationsSsrQuery.TryGetSafeApplicationsReturn(returnUrl, out var safeReturn)
            ? safeReturn
            : KbRoutes.Applications;

        var nl = System.Globalization.CultureInfo.GetCultureInfo("nl-NL");
        var yesDate = thread.AcceptedAtUtc is DateTime acceptedAt
            ? AfterMatchMailSsrTime.ToAmsterdam(acceptedAt).ToString("d MMM", nl)
            : "—";

        var planHint = culture["AfterMatch.Candidate.ProposalPending.PlanHint"];
        var heroLead = string.Format(
            culture["AfterMatch.Candidate.ProposalPending.Lead"],
            thread.CompanyName);

        return new PageData(
            thread,
            backHref,
            AfterMatchSollicitatiesUrls.Messages(applicationId, backHref),
            heroLead,
            yesDate,
            planHint);
    }
}

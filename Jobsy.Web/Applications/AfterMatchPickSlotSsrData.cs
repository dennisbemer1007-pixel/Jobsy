using Jobsy.Core.Contracts;
using Jobsy.Core.Enums;
using Jobsy.Web.KandidaatBanen;
using Jobsy.Web.Localization;
using Jobsy.Web.Services;

namespace Jobsy.Web.Applications;

internal static class AfterMatchPickSlotSsrData
{
    internal sealed record PageData(
        AfterMatchThreadDto Thread,
        string ApplicationsBackHref,
        string BackLabel,
        string SollicitatiesHref,
        IReadOnlyList<AfterMatchSlotDto> SlotsOrdered,
        bool CandidateProposalPending,
        string? CandidateProposalSummary);

    internal static async Task<PageData?> LoadAsync(
        JobsyApiClient api,
        CultureState culture,
        Guid applicationId,
        string? returnUrl,
        CancellationToken cancellationToken = default)
    {
        var thread = await api.GetAfterMatchForCandidateAsync(applicationId, cancellationToken);
        if (thread is null)
        {
            return null;
        }

        var slots = thread.OpenSlots.OrderBy(s => s.StartUtc).ToList();
        var candidateProposalPending = thread.InterviewStatus == InterviewStatus.RescheduleRequested && slots.Count == 0;
        if (slots.Count == 0 && !candidateProposalPending)
        {
            return null;
        }

        var backHref = ApplicationsSsrQuery.TryGetSafeApplicationsReturn(returnUrl, out var safeReturn)
            ? safeReturn
            : KbRoutes.Applications;

        var backLabel = backHref.Contains("tab=berichten", StringComparison.OrdinalIgnoreCase)
            ? culture["AfterMatch.Link.BackBerichten"]
            : culture["AfterMatch.Link.BackPickSlot"];

        var proposalSummary = candidateProposalPending
            ? FormatCandidateProposalSummary(thread)
            : null;

        return new PageData(
            thread,
            backHref,
            backLabel,
            backHref,
            slots,
            candidateProposalPending,
            proposalSummary);
    }

    private static string? FormatCandidateProposalSummary(AfterMatchThreadDto thread)
    {
        var change = thread.RecentChanges.FirstOrDefault(c =>
            c.Kind == InterviewChangeKind.Reschedule
            && string.Equals(c.ActorRole, "Candidate", StringComparison.OrdinalIgnoreCase));
        if (change is null || string.IsNullOrWhiteSpace(change.Reason))
        {
            return null;
        }

        var reason = change.Reason.Trim();
        var dot = reason.IndexOf('.', StringComparison.Ordinal);
        return dot > 0 ? reason[..dot].Trim() : reason;
    }
}

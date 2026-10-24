using Jobsy.Core.Contracts;
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
        IReadOnlyList<AfterMatchSlotDto> SlotsOrdered);

    internal static async Task<PageData?> LoadAsync(
        JobsyApiClient api,
        CultureState culture,
        Guid applicationId,
        string? returnUrl,
        CancellationToken cancellationToken = default)
    {
        var thread = await api.GetAfterMatchForCandidateAsync(applicationId, cancellationToken);
        if (thread is null || thread.OpenSlots.Count == 0)
        {
            return null;
        }

        var backHref = ApplicationsSsrQuery.TryGetSafeApplicationsReturn(returnUrl, out var safeReturn)
            ? safeReturn
            : KbRoutes.Applications;

        var backLabel = backHref.Contains("tab=berichten", StringComparison.OrdinalIgnoreCase)
            ? culture["AfterMatch.Link.BackBerichten"]
            : culture["AfterMatch.Link.BackPickSlot"];

        var slots = thread.OpenSlots.OrderBy(s => s.StartUtc).ToList();
        return new PageData(
            thread,
            backHref,
            backLabel,
            KbRoutes.Applications,
            slots);
    }
}

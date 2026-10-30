using Jobsy.Core.Contracts;
using Jobsy.Core.Enums;
using Jobsy.Web.KandidaatBanen;
using Jobsy.Web.Localization;

namespace Jobsy.Web.Applications;

internal static class AfterMatchCandidateCancelledSsrData
{
    internal sealed record PageData(
        AfterMatchThreadDto Thread,
        string BackHref,
        string Reason,
        bool CancelledByEmployer);

    internal static PageData? Load(CultureState culture, AfterMatchThreadDto? thread, Guid applicationId, string? returnUrl)
    {
        ArgumentNullException.ThrowIfNull(culture);
        if (thread is null || thread.ApplicationId != applicationId)
        {
            return null;
        }

        var cancel = thread.RecentChanges
            .Where(c => c.Kind == InterviewChangeKind.Cancel && !string.IsNullOrWhiteSpace(c.Reason))
            .OrderByDescending(c => c.CreatedAtUtc)
            .FirstOrDefault();
        if (cancel is null)
        {
            return null;
        }

        var backHref = ApplicationsSsrQuery.TryGetSafeApplicationsReturn(returnUrl, out var safeReturn)
            ? safeReturn
            : KbRoutes.Applications;

        var byEmployer = string.Equals(cancel.ActorRole, "Employer", StringComparison.OrdinalIgnoreCase);
        return new PageData(
            thread,
            backHref,
            cancel.Reason.Trim(),
            byEmployer);
    }
}

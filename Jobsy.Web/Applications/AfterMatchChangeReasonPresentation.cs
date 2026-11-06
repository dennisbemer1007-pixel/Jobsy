using Jobsy.Core.Contracts;
using Jobsy.Core.Enums;

namespace Jobsy.Web.Applications;

/// <summary>Which interview-change reasons are still relevant for the current UI state.</summary>
internal static class AfterMatchChangeReasonPresentation
{
    internal static string? LatestReasonForEmployerReschedule(AfterMatchThreadDto thread)
    {
        if (thread.InterviewStatus != InterviewStatus.RescheduleRequested)
        {
            return null;
        }

        return LatestMatchingReason(thread, InterviewChangeKind.Reschedule);
    }

    internal static string? LatestReasonForEmployerPanelNote(AfterMatchThreadDto thread)
    {
        if (thread.InterviewStatus != InterviewStatus.Proposing)
        {
            return null;
        }

        if (thread.OpenSlots.Count > 0)
        {
            return null;
        }

        return LatestMatchingReason(thread, InterviewChangeKind.Cancel);
    }

    private static string? LatestMatchingReason(AfterMatchThreadDto thread, InterviewChangeKind kind)
    {
        foreach (var change in thread.RecentChanges)
        {
            if (change.Kind != kind || string.IsNullOrWhiteSpace(change.Reason))
            {
                continue;
            }

            return change.Reason.Trim();
        }

        return null;
    }
}

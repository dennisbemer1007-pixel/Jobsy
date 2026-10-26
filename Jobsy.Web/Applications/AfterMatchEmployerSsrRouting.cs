using Jobsy.Core.Contracts;
using Jobsy.Core.Enums;

namespace Jobsy.Web.Applications;

internal static class AfterMatchEmployerSsrRouting
{
    /// <summary>Screen 02 (wat nu) SSR — employer still proposing interview times.</summary>
    internal static bool ShouldUseWatNuScreen(AfterMatchThreadDto? thread) =>
        thread is not null
        && thread.InterviewStatus == InterviewStatus.Proposing;

    /// <summary>Employer home when interview is scheduled (gesprek gepland).</summary>
    internal static bool ShouldUseScheduledHomeScreen(AfterMatchThreadDto? thread) =>
        thread is not null
        && thread.InterviewStatus == InterviewStatus.Scheduled;

    /// <summary>Screen 03 (momenten voorstellen) SSR.</summary>
    internal static bool ShouldUseProposeScreen(AfterMatchThreadDto? thread) =>
        thread is not null
        && thread.InterviewStatus == InterviewStatus.Proposing;

    /// <summary>Screen 09 (verzetten) SSR — candidate asked to reschedule or employer moves a confirmed slot.</summary>
    internal static bool ShouldUseRescheduleScreen(AfterMatchThreadDto? thread) =>
        thread is not null
        && thread.InterviewStatus is InterviewStatus.RescheduleRequested or InterviewStatus.Scheduled;

    /// <summary>Employer cancel interview SSR (from gesprek / verzetten flows).</summary>
    internal static bool ShouldUseCancelScreen(AfterMatchThreadDto? thread) =>
        thread is not null
        && thread.InterviewStatus is InterviewStatus.Scheduled or InterviewStatus.RescheduleRequested;

    /// <summary>Screen 11 (berichten) SSR — at least one after-match thread hub.</summary>
    internal static bool ShouldUseMessagesScreen(AfterMatchThreadDto? contextThread) =>
        contextThread is not null
        && AfterMatchEmployerBerichtenPresentation.ThreadEligible(contextThread);

    internal static bool ShouldUseOutcomeScreen(AfterMatchThreadDto? thread) =>
        thread is not null
        && thread.Outcome is null
        && thread.InterviewStatus is InterviewStatus.Scheduled or InterviewStatus.Done;

    internal static bool ShouldUseDoneSummaryScreen(AfterMatchThreadDto? thread) =>
        thread is not null
        && thread.Outcome is not null
        && thread.InterviewStatus == InterviewStatus.Done;

    internal static bool ShouldUsePanelSsrDefault(AfterMatchThreadDto thread, string panel)
    {
        panel = string.IsNullOrWhiteSpace(panel) ? "" : panel.Trim().ToLowerInvariant();
        if (panel is "slots")
        {
            panel = "propose";
        }

        return panel switch
        {
            "propose" => ShouldUseProposeScreen(thread),
            "messages" => true,
            "cancel" => ShouldUseCancelScreen(thread),
            "outcome" => ShouldUseOutcomeScreen(thread) || ShouldUseDoneSummaryScreen(thread),
            _ => ShouldUseWatNuScreen(thread)
                || ShouldUseScheduledHomeScreen(thread)
                || ShouldUseDoneSummaryScreen(thread),
        };
    }

    internal static bool ShouldRedirectToRescheduleSsr(AfterMatchThreadDto thread, string panel)
    {
        panel = string.IsNullOrWhiteSpace(panel) ? "" : panel.Trim().ToLowerInvariant();
        if (panel is "slots")
        {
            panel = "propose";
        }

        return thread.InterviewStatus == InterviewStatus.RescheduleRequested
               && panel is not ("messages" or "cancel" or "propose");
    }
}

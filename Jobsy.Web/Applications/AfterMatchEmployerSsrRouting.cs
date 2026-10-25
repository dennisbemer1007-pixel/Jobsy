using Jobsy.Core.Contracts;
using Jobsy.Core.Enums;

namespace Jobsy.Web.Applications;

internal static class AfterMatchEmployerSsrRouting
{
    /// <summary>Screen 02 (wat nu) SSR — employer still proposing interview times.</summary>
    internal static bool ShouldUseWatNuScreen(AfterMatchThreadDto? thread) =>
        thread is not null
        && thread.InterviewStatus == InterviewStatus.Proposing;

    /// <summary>Screen 03 (momenten voorstellen) SSR.</summary>
    internal static bool ShouldUseProposeScreen(AfterMatchThreadDto? thread) =>
        thread is not null
        && thread.InterviewStatus == InterviewStatus.Proposing;

    /// <summary>Screen 09 (verzetten) SSR — candidate asked to reschedule.</summary>
    internal static bool ShouldUseRescheduleScreen(AfterMatchThreadDto? thread) =>
        thread is not null
        && thread.InterviewStatus == InterviewStatus.RescheduleRequested;

    /// <summary>Employer cancel interview SSR (from gesprek / verzetten flows).</summary>
    internal static bool ShouldUseCancelScreen(AfterMatchThreadDto? thread) =>
        thread is not null
        && thread.InterviewStatus is InterviewStatus.Scheduled or InterviewStatus.RescheduleRequested;

    /// <summary>Screen 11 (berichten) SSR — at least one after-match thread hub.</summary>
    internal static bool ShouldUseMessagesScreen(AfterMatchThreadDto? contextThread) =>
        contextThread is not null
        && AfterMatchEmployerBerichtenPresentation.ThreadEligible(contextThread);
}

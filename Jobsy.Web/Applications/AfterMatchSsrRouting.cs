using Jobsy.Core.Contracts;
using Jobsy.Core.Enums;

namespace Jobsy.Web.Applications;

internal static class AfterMatchSsrRouting
{
    internal static bool ShouldUseOutcomeScreen(AfterMatchThreadDto? thread) =>
        thread is not null
        && (thread.Outcome == AfterMatchOutcomeKind.NotNow
            || thread.InterviewStatus == InterviewStatus.Done);

    internal static bool ShouldUseConfirmedScreen(AfterMatchThreadDto thread) =>
        thread.InterviewStatus == InterviewStatus.Scheduled
        && thread.Outcome != AfterMatchOutcomeKind.NotNow;
}

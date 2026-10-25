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
}

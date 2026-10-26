using Jobsy.Core.Enums;

namespace Jobsy.Core.Rules;

/// <summary>
/// When the candidate may pick from employer-proposed open slots (SSR routing and list CTAs).
/// After a candidate reschedule request the interview stays <see cref="InterviewStatus.RescheduleRequested"/>
/// until the employer responds; new employer times move the flow back to <see cref="InterviewStatus.Proposing"/>.
/// </summary>
public static class AfterMatchCandidateSlotRules
{
    public static bool MayPickOpenSlots(InterviewStatus status) =>
        status == InterviewStatus.Proposing;
}

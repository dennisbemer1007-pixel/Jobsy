using Jobsy.Core.Enums;

namespace Jobsy.Core.Rules;

public sealed record AfterMatchContactShareState(
    bool PhoneAllowed,
    bool IbanAllowed,
    AfterMatchOutcomeKind? Outcome,
    InterviewStatus InterviewStatus)
{
    public static AfterMatchContactShareState From(InterviewStatus status, AfterMatchOutcomeKind? outcome) =>
        new(
            PhoneAllowed: status is InterviewStatus.Done,
            IbanAllowed: outcome is AfterMatchOutcomeKind.Offer,
            Outcome: outcome,
            InterviewStatus: status);
}

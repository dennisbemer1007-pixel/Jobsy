using Jobsy.Core.Contracts;
using Jobsy.Web.Localization;

namespace Jobsy.Web.Applications;

internal static class AfterMatchEmployerOutcomeSsrData
{
    internal sealed record PageData(
        AfterMatchThreadDto Thread,
        string TalentPoolHref,
        string ApplicationDetailHref,
        string RejectionDemoReason,
        string? YesStepHint,
        string? PlanStepHint,
        string? InterviewStepHint,
        IReadOnlyList<string> OfferReasonChips,
        IReadOnlyList<string> RejectionReasonChips);

    internal static PageData FromThread(CultureState culture, AfterMatchThreadDto thread, Guid applicationId)
    {
        var yesHint = thread.AcceptedAtUtc is DateTime accepted
            ? AfterMatchTimelineLabels.ShortDate(accepted)
            : null;
        var interviewHint = AfterMatchTimelineLabels.InterviewStep(thread);
        var planHint = AfterMatchTimelineLabels.PlanStep(thread);
        if (planHint == "—")
        {
            planHint = yesHint;
        }

        return new PageData(
            thread,
            "/werkgever/talentpool",
            $"/werkgever/sollicitaties/{applicationId:D}",
            culture["AfterMatch.Screen13.DemoReason"],
            yesHint,
            planHint,
            interviewHint,
            [
                culture["AfterMatch.Screen12.ReasonChip.OfferStart"],
                culture["AfterMatch.Screen12.ReasonChip.OfferHours"],
                culture["AfterMatch.Screen12.ReasonChip.OfferWelcome"],
                culture["AfterMatch.Screen12.ReasonChip.OfferOther"]
            ],
            [
                culture["AfterMatch.Screen12.ReasonChip.Experience"],
                culture["AfterMatch.Screen12.ReasonChip.Hours"],
                culture["AfterMatch.Screen12.ReasonChip.Filled"],
                culture["AfterMatch.Screen12.ReasonChip.Other"]
            ]);
    }
}

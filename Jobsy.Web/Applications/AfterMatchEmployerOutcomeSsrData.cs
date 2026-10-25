using System.Globalization;
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
        var nl = CultureInfo.GetCultureInfo("nl-NL");
        var yesHint = thread.AcceptedAtUtc is DateTime accepted
            ? AfterMatchMailSsrTime.ToAmsterdam(accepted).ToString("d MMM", nl)
            : null;
        var interviewHint = thread.ScheduledStartUtc is DateTime scheduled
            ? AfterMatchMailSsrTime.ToAmsterdam(scheduled).ToString("d MMM", nl)
            : null;
        var planHint = thread.ScheduledStartUtc is DateTime planBase
            ? AfterMatchMailSsrTime.ToAmsterdam(planBase).AddDays(-7).ToString("d MMM", nl)
            : yesHint;

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

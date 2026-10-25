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
        string DefaultReason,
        string? YesStepHint,
        string? PlanStepHint,
        string? InterviewStepHint,
        IReadOnlyList<string> ReasonChips);

    internal static PageData FromThread(CultureState culture, AfterMatchThreadDto thread, Guid applicationId)
    {
        var nl = CultureInfo.GetCultureInfo("nl-NL");
        var yesHint = thread.AcceptedAtUtc?.ToLocalTime().ToString("d MMM", nl);
        var interviewHint = thread.ScheduledStartUtc?.ToLocalTime().ToString("d MMM", nl);
        var planHint = thread.ScheduledStartUtc?.ToLocalTime().AddDays(-7).ToString("d MMM", nl)
            ?? yesHint;

        return new PageData(
            thread,
            "/werkgever/talentpool",
            $"/werkgever/sollicitaties/{applicationId:D}",
            culture["AfterMatch.Screen13.DemoReason"],
            yesHint,
            planHint,
            interviewHint,
            [
                culture["AfterMatch.Screen12.ReasonChip.Experience"],
                culture["AfterMatch.Screen12.ReasonChip.Hours"],
                culture["AfterMatch.Screen12.ReasonChip.Filled"],
                culture["AfterMatch.Screen12.ReasonChip.Other"]
            ]);
    }
}

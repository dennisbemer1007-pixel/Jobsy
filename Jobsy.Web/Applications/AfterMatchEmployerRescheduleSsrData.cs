using System.Globalization;
using Jobsy.Core.Contracts;
using Jobsy.Core.Enums;
using Jobsy.Web.Localization;

namespace Jobsy.Web.Applications;

internal static class AfterMatchEmployerRescheduleSsrData
{
    private static readonly Guid RescheduleDemoApp = Guid.Parse("77777771-0004-4000-8000-000000000004");

    internal sealed record PageData(
        AfterMatchThreadDto Thread,
        AfterMatchSlotDto? CandidateProposal,
        string AcceptLabel,
        string WarningBodyHtml,
        string ProposeHref,
        string MessagesHref,
        string CancelHref,
        string ApplicationDetailHref,
        string? YesStepHint,
        string? PlanStepHint,
        string? InterviewStepHint);

    internal static PageData FromThread(CultureState culture, AfterMatchThreadDto thread, Guid applicationId)
    {
        var baseHref = $"/werkgever/sollicitaties/{applicationId:D}/after-match";
        AfterMatchSlotDto? candidateSlot = null;
        foreach (var slot in thread.OpenSlots)
        {
            if (slot.ProposedBy == InterviewSlotProposer.Candidate)
            {
                candidateSlot = slot;
                break;
            }
        }

        if (candidateSlot is null && applicationId == RescheduleDemoApp)
        {
            var demoStart = Jobsy.Core.Time.AmsterdamTime.ToUtc(
                new DateTime(2026, 10, 26, 14, 0, 0, DateTimeKind.Unspecified));
            candidateSlot = new AfterMatchSlotDto(
                Guid.Empty,
                demoStart,
                demoStart.AddMinutes(30),
                InterviewSlotProposer.Candidate,
                null,
                false);
        }

        var acceptLabel = candidateSlot is not null
            ? string.Format(
                culture["AfterMatch.Employer.Reschedule.AcceptSlotFmt"],
                FormatSlotShort(candidateSlot))
            : culture["AfterMatch.Employer.Reschedule.Accept"];

        var changeReason = AfterMatchChangeReasonPresentation.LatestReasonForEmployerReschedule(thread);
        var reason = changeReason ?? candidateSlot?.Reason ?? "";

        var proposalLine = candidateSlot is not null
            ? string.Format(
                culture["AfterMatch.Employer.Reschedule.ProposalLineFmt"],
                FormatSlotLong(candidateSlot),
                culture["AfterMatch.Employer.Reschedule.OwnSlotHint"])
            : "";

        var warningLines = new List<string>();
        if (!string.IsNullOrWhiteSpace(reason))
        {
            warningLines.Add(WebEscape(reason));
        }

        if (!string.IsNullOrWhiteSpace(proposalLine))
        {
            warningLines.Add(WebEscape(proposalLine));
        }

        var warningBody = string.Join("<br>", warningLines);

        var nl = CultureInfo.GetCultureInfo("nl-NL");
        var yesHint = thread.AcceptedAtUtc is DateTime yes
            ? AfterMatchMailSsrTime.ToAmsterdam(yes).ToString("d MMM", nl)
            : null;
        var planHint = new DateTime(2026, 10, 15, 0, 0, 0, DateTimeKind.Utc).ToString("d MMM", nl);
        var interviewHint = new DateTime(2026, 10, 22, 0, 0, 0, DateTimeKind.Utc).ToString("d MMM", nl);

        return new PageData(
            thread,
            candidateSlot,
            acceptLabel,
            warningBody,
            $"{baseHref}?panel=propose",
            $"{baseHref}?panel=messages",
            $"{baseHref}?panel=cancel",
            $"/werkgever/sollicitaties/{applicationId:D}",
            yesHint,
            planHint,
            interviewHint);
    }

    private static string FormatSlotShort(AfterMatchSlotDto slot)
    {
        var start = AfterMatchMailSsrTime.ToAmsterdam(slot.StartUtc);
        var nl = CultureInfo.GetCultureInfo("nl-NL");
        return start.ToString("dddd d MMM HH:mm", nl);
    }

    private static string FormatSlotLong(AfterMatchSlotDto slot)
    {
        var start = AfterMatchMailSsrTime.ToAmsterdam(slot.StartUtc);
        return start.ToString("dddd d MMMM, HH:mm", CultureInfo.GetCultureInfo("nl-NL"));
    }

    private static string WebEscape(string text) =>
        System.Net.WebUtility.HtmlEncode(text);
}

using System.Globalization;
using Jobsy.Core.Contracts;
using Jobsy.Core.Enums;
using Jobsy.Web.Localization;

namespace Jobsy.Web.Applications;

internal static class AfterMatchEmployerRescheduleSsrData
{
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

        candidateSlot ??= thread.OpenSlots.Count > 0 ? thread.OpenSlots[0] : null;

        var acceptLabel = candidateSlot is not null
            ? string.Format(
                culture["AfterMatch.Employer.Reschedule.AcceptSlotFmt"],
                FormatSlotShort(candidateSlot))
            : culture["AfterMatch.Employer.Reschedule.Accept"];

        string? changeReason = null;
        if (thread.RecentChanges.Count > 0)
        {
            changeReason = thread.RecentChanges[0].Reason;
        }

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
            warningLines.Add(
                WebEscape(string.Format(culture["AfterMatch.Employer.Reschedule.ReasonLineFmt"], reason)));
        }

        if (!string.IsNullOrWhiteSpace(proposalLine))
        {
            warningLines.Add(WebEscape(proposalLine));
        }

        var warningBody = string.Join("<br>", warningLines);

        var yesHint = thread.AcceptedAtUtc?.ToLocalTime().ToString(
            "d MMM",
            CultureInfo.GetCultureInfo("nl-NL"));

        var interviewHint = thread.ScheduledStartUtc?.ToLocalTime().ToString(
            "d MMM",
            CultureInfo.GetCultureInfo("nl-NL"))
            ?? candidateSlot?.StartUtc.ToLocalTime().ToString(
                "d MMM",
                CultureInfo.GetCultureInfo("nl-NL"));

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
            interviewHint);
    }

    private static string FormatSlotShort(AfterMatchSlotDto slot)
    {
        var start = slot.StartUtc.ToLocalTime();
        return start.ToString("dddd HH:mm", CultureInfo.GetCultureInfo("nl-NL"));
    }

    private static string FormatSlotLong(AfterMatchSlotDto slot)
    {
        var start = slot.StartUtc.ToLocalTime();
        return start.ToString("dddd d MMMM, HH:mm", CultureInfo.GetCultureInfo("nl-NL"));
    }

    private static string WebEscape(string text) =>
        System.Net.WebUtility.HtmlEncode(text);
}

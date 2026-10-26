using System.Globalization;
using Jobsy.Core.Contracts;
using Jobsy.Core.Enums;

namespace Jobsy.Web.Applications;

/// <summary>SSOT for timeline / week-aside interview moment labels (no fixed mock dates).</summary>
internal static class AfterMatchTimelineLabels
{
    private static readonly CultureInfo Nl = CultureInfo.GetCultureInfo("nl-NL");

    internal static string ShortDate(DateTime? utc)
        => utc is null
            ? "—"
            : AfterMatchMailSsrTime.ToAmsterdam(utc.Value).ToString("d MMM", Nl);

    internal static string InterviewStep(AfterMatchThreadDto thread)
    {
        if (thread.ScheduledStartUtc is DateTime scheduled)
        {
            return ShortDate(scheduled);
        }

        if (thread.InterviewStatus is InterviewStatus.Proposing or InterviewStatus.RescheduleRequested)
        {
            var open = thread.OpenSlots.OrderBy(s => s.StartUtc).FirstOrDefault();
            if (open is not null)
            {
                return ShortDate(open.StartUtc);
            }
        }

        return "—";
    }

    internal static string WeekAsideWhen(AfterMatchThreadDto thread)
    {
        if (thread.InterviewStatus == InterviewStatus.Scheduled && thread.ScheduledStartUtc is DateTime scheduled)
        {
            return FormatWeekSlot(scheduled);
        }

        var candidateProposal = thread.OpenSlots
            .Where(s => s.ProposedBy == InterviewSlotProposer.Candidate)
            .OrderBy(s => s.StartUtc)
            .FirstOrDefault();
        if (candidateProposal is not null)
        {
            return FormatWeekSlot(candidateProposal.StartUtc);
        }

        var employerProposal = thread.OpenSlots.OrderBy(s => s.StartUtc).FirstOrDefault();
        return employerProposal is null ? "—" : FormatWeekSlot(employerProposal.StartUtc);
    }

    internal static string FormatWeekSlot(DateTime utc)
    {
        var local = AfterMatchMailSsrTime.ToAmsterdam(utc);
        var text = local.ToString("dddd d MMM, HH:mm", Nl);
        if (text.Length > 0 && char.IsLower(text[0]))
        {
            text = char.ToUpper(text[0], Nl) + text[1..];
        }

        return text;
    }
}

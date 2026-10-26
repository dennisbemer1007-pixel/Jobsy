using Jobsy.Core.Contracts;
using Jobsy.Core.Enums;
using Jobsy.Core.Rules;
using Jobsy.Core.Time;
using Jobsy.Web.Localization;

namespace Jobsy.Web.Applications;

internal static class AfterMatchEmployerWeekAsideRows
{
    private static readonly Guid Verkoop4790App = Guid.Parse("77777771-0006-4000-8000-000000000006");

    internal sealed record Row(string Title, string Subtitle, string PillClass, string PillLabel);

    internal static IReadOnlyList<Row> Build(CultureState culture, AfterMatchThreadDto current)
    {
        var rows = new List<Row> { RowForThread(culture, current) };

        if (!AfterMatchEmployerMessagesDemoHub.IsDemoApplication(current.ApplicationId))
        {
            return rows;
        }

        var slots = AfterMatchEmployerDemoSlots.InitialUtc(DateTime.UtcNow);
        if (slots.Length > 1 && current.ApplicationId != Verkoop4790App)
        {
            var scheduledUtc = slots[1];
            if (IsInCurrentAmsterdamWeek(scheduledUtc))
            {
                rows.Add(new Row(
                    culture["AfterMatch.Employer.Reschedule.WeekRow4790Name"],
                    AfterMatchTimelineLabels.FormatWeekSlot(scheduledUtc),
                    "ok",
                    culture["AfterMatch.Employer.Reschedule.WeekRow4790Status"]));
            }
        }

        return rows;
    }

    private static Row RowForThread(CultureState culture, AfterMatchThreadDto thread)
    {
        var title = AfterMatchEmployerEmployerPresentation.CandidateDisplayName(thread);
        var isScheduled = thread.InterviewStatus == InterviewStatus.Scheduled && thread.ScheduledStartUtc is not null;
        if (thread.InterviewStatus == InterviewStatus.RescheduleRequested)
        {
            return new Row(
                title,
                culture["AfterMatch.Employer.Reschedule.RequestLabel"],
                "warm",
                culture["AfterMatch.Employer.Reschedule.ActionPill"]);
        }

        var pillClass = isScheduled ? "ok" : "warm";
        var pillLabel = isScheduled
            ? culture["AfterMatch.Employer.Reschedule.WeekRow4790Status"]
            : culture["AfterMatch.Employer.Reschedule.ActionPill"];
        return new Row(
            title,
            AfterMatchTimelineLabels.WeekAsideWhen(thread),
            pillClass,
            pillLabel);
    }

    private static bool IsInCurrentAmsterdamWeek(DateTime utc)
    {
        var start = InitiativeWeekLimitRules.WeekStartAmsterdam(DateTime.UtcNow);
        var end = InitiativeWeekLimitRules.NextWeekStartAmsterdam(DateTime.UtcNow);
        return utc >= start && utc < end;
    }
}

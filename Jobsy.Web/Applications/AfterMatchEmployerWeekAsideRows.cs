using Jobsy.Core.Contracts;
using Jobsy.Core.Enums;
using Jobsy.Core.Rules;
using Jobsy.Web.Localization;

namespace Jobsy.Web.Applications;

internal static class AfterMatchEmployerWeekAsideRows
{
    internal sealed record Row(string Title, string Subtitle, string PillClass, string PillLabel);

    internal static IReadOnlyList<Row> Build(
        CultureState culture,
        AfterMatchThreadDto current,
        IReadOnlyList<AfterMatchThreadDto> scopeThreads)
    {
        var weekStart = InitiativeWeekLimitRules.WeekStartAmsterdam(DateTime.UtcNow);
        var weekEnd = InitiativeWeekLimitRules.NextWeekStartAmsterdam(DateTime.UtcNow);

        var byApp = new Dictionary<Guid, AfterMatchThreadDto>();
        foreach (var thread in scopeThreads)
        {
            if (!HasInterviewMomentThisWeek(thread, weekStart, weekEnd))
            {
                continue;
            }

            byApp[thread.ApplicationId] = thread;
        }

        if (!byApp.ContainsKey(current.ApplicationId)
            && (HasInterviewMomentThisWeek(current, weekStart, weekEnd)
                || current.InterviewStatus == InterviewStatus.RescheduleRequested))
        {
            byApp[current.ApplicationId] = current;
        }

        return byApp.Values
            .OrderBy(SortKeyUtc)
            .ThenBy(t => AfterMatchEmployerEmployerPresentation.CandidateDisplayName(t), StringComparer.Ordinal)
            .Select(t => RowForThread(culture, t))
            .ToList();
    }

    private static DateTime SortKeyUtc(AfterMatchThreadDto thread)
    {
        if (thread.ScheduledStartUtc is DateTime scheduled)
        {
            return scheduled;
        }

        var slot = thread.OpenSlots.OrderBy(s => s.StartUtc).FirstOrDefault();
        return slot?.StartUtc ?? DateTime.MaxValue;
    }

    private static bool HasInterviewMomentThisWeek(AfterMatchThreadDto thread, DateTime weekStart, DateTime weekEnd)
    {
        if (thread.InterviewStatus == InterviewStatus.Scheduled && thread.ScheduledStartUtc is DateTime scheduled)
        {
            return scheduled >= weekStart && scheduled < weekEnd;
        }

        if (thread.InterviewStatus == InterviewStatus.RescheduleRequested)
        {
            return true;
        }

        foreach (var slot in thread.OpenSlots)
        {
            if (slot.StartUtc >= weekStart && slot.StartUtc < weekEnd)
            {
                return true;
            }
        }

        return false;
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
}

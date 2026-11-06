using Jobsy.Core.Enums;

namespace Jobsy.Core.Rules.KandidaatBanen;

public enum KbSavedJobStateKind
{
    Open = 0,
    ClosingSoon = 1,
    Fulfilled = 2,
    Closed = 3,
    Hidden = 4
}

public sealed record KbSavedJobState(
    KbSavedJobStateKind Kind,
    int? DaysUntilEnd,
    string LabelKey);

/// <summary>
/// Pure vacancy → saved-job state pill (Bewaard). Days use Europe/Amsterdam calendar dates.
/// </summary>
public static class KbSavedJobStateResolver
{
    public const string LabelOpen = "Kb.Saved.Open";
    public const string LabelClosingSoon = "Kb.Saved.ClosingSoon";
    public const string LabelFulfilled = "Kb.Saved.Fulfilled";
    public const string LabelClosed = "Kb.Saved.Closed";

    private static readonly TimeZoneInfo Amsterdam = ResolveAmsterdam();

    public static KbSavedJobState Resolve(
        VacancyStatus status,
        DateOnly endDate,
        DateTime? closedAtUtc,
        DateTime utcNow)
    {
        if (status is VacancyStatus.Draft or VacancyStatus.PendingApproval)
        {
            return new KbSavedJobState(KbSavedJobStateKind.Hidden, null, LabelClosed);
        }

        if (status == VacancyStatus.Fulfilled)
        {
            return new KbSavedJobState(KbSavedJobStateKind.Fulfilled, null, LabelFulfilled);
        }

        var today = DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(
            DateTime.SpecifyKind(utcNow, DateTimeKind.Utc),
            Amsterdam));

        if (status == VacancyStatus.Archived || closedAtUtc.HasValue || endDate < today)
        {
            return new KbSavedJobState(KbSavedJobStateKind.Closed, null, LabelClosed);
        }

        // Active and EndDate >= today
        var days = endDate.DayNumber - today.DayNumber;
        if (days <= 7)
        {
            return new KbSavedJobState(KbSavedJobStateKind.ClosingSoon, days, LabelClosingSoon);
        }

        return new KbSavedJobState(KbSavedJobStateKind.Open, days, LabelOpen);
    }

    public static bool IsOpenLike(KbSavedJobStateKind kind)
        => kind is KbSavedJobStateKind.Open or KbSavedJobStateKind.ClosingSoon;

    public static bool IsClosedLike(KbSavedJobStateKind kind)
        => kind is KbSavedJobStateKind.Closed or KbSavedJobStateKind.Fulfilled;

    private static TimeZoneInfo ResolveAmsterdam()
    {
        foreach (var id in new[] { "Europe/Amsterdam", "W. Europe Standard Time" })
        {
            try
            {
                return TimeZoneInfo.FindSystemTimeZoneById(id);
            }
            catch (TimeZoneNotFoundException)
            {
            }
            catch (InvalidTimeZoneException)
            {
            }
        }

        return TimeZoneInfo.Utc;
    }
}

using Jobsy.Core.Time;

namespace Jobsy.Web.Applications;

internal static class AfterMatchEmployerDemoSlots
{
    private static readonly (int Hour, int Minute)[] SlotTimes = [(10, 30), (9, 0), (14, 0)];

    /// <summary>Demo slots for werkgever screen 03 (next Amsterdam workdays).</summary>
    internal static DateTime[] InitialUtc(DateTime? notBeforeUtc = null)
    {
        var anchor = notBeforeUtc is { } nb
            ? AmsterdamTime.ToLocal(nb)
            : AmsterdamTime.ToLocal(DateTime.UtcNow);

        var day = DateOnly.FromDateTime(anchor.Date);
        if (anchor.TimeOfDay >= TimeSpan.FromHours(16))
        {
            day = day.AddDays(1);
        }

        var slots = new List<DateTime>(3);
        var timeIndex = 0;
        while (slots.Count < 3)
        {
            if (day.DayOfWeek is not DayOfWeek.Saturday and not DayOfWeek.Sunday)
            {
                var (hour, minute) = SlotTimes[timeIndex % SlotTimes.Length];
                var local = new DateTime(day.Year, day.Month, day.Day, hour, minute, 0, DateTimeKind.Unspecified);
                var utc = AmsterdamTime.ToUtc(local);
                if (notBeforeUtc is null || utc > notBeforeUtc.Value)
                {
                    slots.Add(utc);
                    timeIndex++;
                }
            }

            day = day.AddDays(1);
            if (day.Year > anchor.Year + 1)
            {
                break;
            }
        }

        return slots.Count == 3
            ? [slots[0], slots[1], slots[2]]
            :
            [
                AmsterdamTime.ToUtc(anchor.Date.AddDays(1).AddHours(10).AddMinutes(30)),
                AmsterdamTime.ToUtc(anchor.Date.AddDays(2).AddHours(9)),
                AmsterdamTime.ToUtc(anchor.Date.AddDays(3).AddHours(14)),
            ];
    }
}

using System.Globalization;

namespace Jobsy.Core.Time;

/// <summary>Closed vacancies show the real close date, not the planned end date.</summary>
public static class ClosedVacancyDeadline
{
    public static bool IsClosed(string? status)
        => status is "Archived" or "Fulfilled";

    public static string Label(
        string? status,
        DateOnly endDate,
        DateTime? closedAtUtc,
        string closedOnTemplate,
        string closedWithoutDate)
    {
        if (!IsClosed(status))
        {
            return endDate.ToString("dd-MM-yyyy", CultureInfo.InvariantCulture);
        }

        return closedAtUtc is DateTime closed
            ? string.Format(
                CultureInfo.InvariantCulture,
                closedOnTemplate,
                AmsterdamTime.ToLocal(closed).ToString("dd-MM-yyyy", CultureInfo.InvariantCulture))
            : closedWithoutDate;
    }
}

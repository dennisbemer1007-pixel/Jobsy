namespace Jobsy.Core.Rules;

/// <summary>Canonical day / day-part codes for schedule matrices (candidate + vacancy).</summary>
public static class DayPartMatrix
{
    public static readonly string[] DayCodes = ["Ma", "Di", "Wo", "Do", "Vr", "Za", "Zo"];
    public static readonly string[] DayPartCodes = ["Ochtend", "Middag", "Avond", "Nacht"];

    public static readonly IReadOnlyDictionary<string, string> DayPartWindows =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["Ochtend"] = "06:00 – 12:00",
            ["Middag"] = "12:00 – 18:00",
            ["Avond"] = "18:00 – 23:00",
            ["Nacht"] = "23:00 – 06:00"
        };

    public static bool IsValidDayCode(string? code)
        => !string.IsNullOrWhiteSpace(code)
           && DayCodes.Contains(code.Trim(), StringComparer.OrdinalIgnoreCase);

    public static bool IsValidDayPartCode(string? code)
        => !string.IsNullOrWhiteSpace(code)
           && DayPartCodes.Contains(code.Trim(), StringComparer.OrdinalIgnoreCase);

    public static string NormalizeDayCode(string code)
    {
        var match = DayCodes.FirstOrDefault(d => d.Equals(code.Trim(), StringComparison.OrdinalIgnoreCase));
        return match ?? code.Trim();
    }

    public static string NormalizeDayPartCode(string code)
    {
        var match = DayPartCodes.FirstOrDefault(d => d.Equals(code.Trim(), StringComparison.OrdinalIgnoreCase));
        return match ?? code.Trim();
    }
}

/// <summary>How flexible/empty schedules were produced.</summary>
public enum FlexibleScheduleSource
{
    Manual = 0,
    ImportEmpty = 1,
    ApiEmpty = 2,
    AtsEmpty = 3
}

/// <summary>Shared schedule payload for vacancy + candidate preferences.</summary>
public sealed class SchedulePayload
{
    public bool FlexibleTimes { get; set; }
    public FlexibleScheduleSource? FlexibleSource { get; set; }

    /// <summary>Day code → selected day-part codes.</summary>
    public Dictionary<string, List<string>> Slots { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    public static SchedulePayload Flexible(FlexibleScheduleSource source)
        => new() { FlexibleTimes = true, FlexibleSource = source };

    public bool HasAnySlot()
        => Slots.Values.Any(parts => parts is { Count: > 0 });

    public string? Validate()
    {
        if (FlexibleTimes)
        {
            return null;
        }

        if (!HasAnySlot())
        {
            return "Selecteer dagdelen of kies ‘Tijden in overleg’.";
        }

        foreach (var (day, parts) in Slots)
        {
            if (!DayPartMatrix.IsValidDayCode(day))
            {
                return $"Ongeldige dagcode: {day}.";
            }

            foreach (var part in parts)
            {
                if (!DayPartMatrix.IsValidDayPartCode(part))
                {
                    return $"Ongeldig dagdeel: {part}.";
                }
            }
        }

        return null;
    }

    /// <summary>Normalize keys/codes; when flexible, clear concrete slots.</summary>
    public SchedulePayload Normalize()
    {
        if (FlexibleTimes)
        {
            Slots = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
            return this;
        }

        var normalized = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
        foreach (var (day, parts) in Slots)
        {
            if (!DayPartMatrix.IsValidDayCode(day) || parts is null || parts.Count == 0)
            {
                continue;
            }

            var dayKey = DayPartMatrix.NormalizeDayCode(day);
            var unique = parts
                .Where(DayPartMatrix.IsValidDayPartCode)
                .Select(DayPartMatrix.NormalizeDayPartCode)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
            if (unique.Count > 0)
            {
                normalized[dayKey] = unique;
            }
        }

        Slots = normalized;
        return this;
    }
}

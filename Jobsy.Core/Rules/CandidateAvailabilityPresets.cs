namespace Jobsy.Core.Rules;

/// <summary>
/// Maps the three candidate “harde criteria” availability presets onto existing
/// hours / flexible / day-part preference fields (no extra table).
/// </summary>
public static class CandidateAvailabilityPresets
{
    public const string Immediate = "immediate";
    public const string PartTime = "parttime";
    public const string Seasonal = "seasonal";

    public static readonly string[] All = [Immediate, PartTime, Seasonal];

    public static CandidateAvailabilityPatch Apply(string kind)
        => kind.Trim().ToLowerInvariant() switch
        {
            "immediate" or "per direct" or "direct" => new(
                Immediate,
                8,
                40,
                FlexibleTimes: true,
                Availability: new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)),
            "parttime" or "part-time" => new(
                PartTime,
                8,
                24,
                FlexibleTimes: false,
                Availability: WeekdayOfficeSlots()),
            "seasonal" or "seizoen" or "seizoenswerk" => new(
                Seasonal,
                16,
                40,
                FlexibleTimes: true,
                Availability: new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)),
            _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, "Onbekende beschikbaarheidspreset.")
        };

    public static string? Detect(decimal? minHours, decimal? maxHours, bool? flexibleTimes)
    {
        var mid = minHours is decimal min && maxHours is decimal max
            ? (min + max) / 2m
            : (decimal?)null;

        if (flexibleTimes == true && minHours is >= 16 && maxHours is >= 32)
        {
            return Seasonal;
        }

        if (mid is < 32 && flexibleTimes != true)
        {
            return PartTime;
        }

        if (flexibleTimes == true)
        {
            return Immediate;
        }

        if (mid is < 32)
        {
            return PartTime;
        }

        return null;
    }

    private static Dictionary<string, string[]> WeekdayOfficeSlots()
    {
        var slots = new[] { "Ochtend", "Middag" };
        var map = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase);
        foreach (var day in new[] { "Ma", "Di", "Wo", "Do", "Vr" })
        {
            map[day] = slots;
        }

        return map;
    }
}

public sealed record CandidateAvailabilityPatch(
    string Kind,
    decimal MinHoursPerWeek,
    decimal MaxHoursPerWeek,
    bool FlexibleTimes,
    IReadOnlyDictionary<string, string[]> Availability);

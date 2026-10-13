namespace Jobsy.Core.Rules;

/// <summary>
/// Deterministic availability presets for onboarding wizard v2 step 2.
/// Combinations are order-independent; day-parts are the union of selected presets.
/// </summary>
public static class AvailabilityPresetRules
{
    public const string Direct = "direct";
    public const string School = "school";
    public const string Weekend = "weekend";
    public const string Evening = "evening";
    public const string Office = "office";
    public const string Holiday = "holiday";
    public const string Parttime = "parttime";
    public const string Fulltime = "fulltime";

    public static readonly string[] AllCodes =
    [
        Direct, School, Weekend, Evening, Office, Holiday, Parttime, Fulltime
    ];

    /// <summary>Presets that contribute specific day-parts (not only hours).</summary>
    public static readonly HashSet<string> DaySpecificCodes = new(StringComparer.OrdinalIgnoreCase)
    {
        School, Weekend, Evening, Office, Holiday
    };

    public sealed record PresetDefinition(
        string Code,
        decimal? MinHours,
        decimal? MaxHours,
        IReadOnlyList<string> DayParts);

    public sealed record ComputeResult(
        decimal? MinHours,
        decimal? MaxHours,
        IReadOnlySet<string> DayParts,
        bool Immediate);

    private static readonly Dictionary<string, PresetDefinition> Definitions =
        new(StringComparer.OrdinalIgnoreCase)
        {
            [Direct] = new(Direct, null, null, []),
            [School] = new(School, 6, 16,
            [
                "Ma:Middag", "Di:Middag", "Wo:Middag", "Do:Middag", "Vr:Middag",
                "Za:Ochtend", "Za:Middag"
            ]),
            [Weekend] = new(Weekend, 8, 16,
            [
                "Za:Ochtend", "Za:Middag", "Zo:Ochtend", "Zo:Middag"
            ]),
            [Evening] = new(Evening, 8, 20,
            [
                "Ma:Avond", "Di:Avond", "Wo:Avond", "Do:Avond", "Vr:Avond"
            ]),
            [Office] = new(Office, 32, 40,
            [
                "Ma:Ochtend", "Ma:Middag", "Di:Ochtend", "Di:Middag",
                "Wo:Ochtend", "Wo:Middag", "Do:Ochtend", "Do:Middag",
                "Vr:Ochtend", "Vr:Middag"
            ]),
            [Holiday] = new(Holiday, 16, 40,
            [
                "Ma:Ochtend", "Ma:Middag", "Di:Ochtend", "Di:Middag",
                "Wo:Ochtend", "Wo:Middag", "Do:Ochtend", "Do:Middag",
                "Vr:Ochtend", "Vr:Middag"
            ]),
            [Parttime] = new(Parttime, 12, 32,
            [
                "Ma:Ochtend", "Ma:Middag", "Di:Ochtend", "Di:Middag",
                "Wo:Ochtend", "Wo:Middag", "Do:Ochtend", "Do:Middag",
                "Vr:Ochtend", "Vr:Middag"
            ]),
            [Fulltime] = new(Fulltime, 36, 40,
            [
                "Ma:Ochtend", "Ma:Middag", "Di:Ochtend", "Di:Middag",
                "Wo:Ochtend", "Wo:Middag", "Do:Ochtend", "Do:Middag",
                "Vr:Ochtend", "Vr:Middag"
            ])
        };

    public static bool IsKnown(string? code)
        => !string.IsNullOrWhiteSpace(code) && Definitions.ContainsKey(code.Trim());

    public static PresetDefinition? TryGet(string code)
        => Definitions.TryGetValue(code, out var def) ? def : null;

    /// <summary>
    /// Combine selected presets. Hours: min of mins, max of maxes (clamped 1–60).
    /// Day-parts: union of day-specific presets; parttime/fulltime only add days
    /// when no day-specific preset is selected. Immediate when <c>direct</c> is selected.
    /// </summary>
    public static ComputeResult Compute(IReadOnlySet<string> presets)
    {
        var selected = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var raw in presets)
        {
            if (string.IsNullOrWhiteSpace(raw))
            {
                continue;
            }

            var code = raw.Trim();
            if (Definitions.TryGetValue(code, out var value))
            {
                selected.Add(value.Code);
            }
        }

        var immediate = selected.Contains(Direct);
        var hasDaySpecific = selected.Any(c => DaySpecificCodes.Contains(c));

        decimal? min = null;
        decimal? max = null;
        var dayParts = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var code in selected)
        {
            var def = Definitions[code];
            if (def.MinHours is { } dMin)
            {
                min = min is null ? dMin : Math.Min(min.Value, dMin);
            }

            if (def.MaxHours is { } dMax)
            {
                max = max is null ? dMax : Math.Max(max.Value, dMax);
            }

            var contributesDays = DaySpecificCodes.Contains(code)
                                  || (!hasDaySpecific && (code is Parttime or Fulltime));
            if (contributesDays)
            {
                foreach (var part in def.DayParts)
                {
                    dayParts.Add(part);
                }
            }
        }

        if (min is { } m)
        {
            min = Math.Clamp(m, 1m, 60m);
        }

        if (max is { } x)
        {
            max = Math.Clamp(x, 1m, 60m);
        }

        if (min is { } mi && max is { } ma && mi > ma)
        {
            (min, max) = (ma, mi);
        }

        return new ComputeResult(min, max, dayParts, immediate);
    }

    /// <summary>
    /// Apply compute onto existing hour values when presets contribute no hours.
    /// </summary>
    public static ComputeResult Compute(
        IReadOnlySet<string> presets,
        decimal? currentMin,
        decimal? currentMax)
    {
        var result = Compute(presets);
        var min = result.MinHours ?? currentMin;
        var max = result.MaxHours ?? currentMax;
        return result with { MinHours = min, MaxHours = max };
    }
}

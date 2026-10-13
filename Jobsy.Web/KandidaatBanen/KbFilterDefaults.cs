namespace Jobsy.Web.KandidaatBanen;

/// <summary>
/// Candidate banenkaart filter defaults and badge counting (file 03 / D13).
/// Badge = deviations from the candidate's own defaults; a fresh page is 0.
/// </summary>
public sealed record KbFilterDefaults(
    string Transport = KbMapStart.DefaultTransport,
    int MaxTravelMinutes = KbMapStart.DefaultMaxTravelMinutes,
    int RadiusKm = 15,
    int? AgeYears = null,
    int MinHoursPerWeek = 0,
    int MaxHoursPerWeek = 40);

public sealed record KbFilterState(
    string Transport,
    int MaxTravelMinutes,
    int RadiusKm,
    int? AgeYears,
    int MinHoursPerWeek,
    int MaxHoursPerWeek,
    string? SearchQuery,
    int WorkTypeCount,
    int CategoryCount,
    bool HasMinWage,
    bool HasMaxWage,
    bool MyVacanciesOnly,
    int MinMatchPercent,
    bool HasOrigin);

public static class KbFilterBadge
{
    /// <summary>Count filter deviations from <paramref name="defaults"/>. Each axis counts once.</summary>
    public static int Count(KbFilterState state, KbFilterDefaults defaults)
    {
        var n = 0;
        if (state.WorkTypeCount > 0) n++;
        if (state.CategoryCount > 0) n++;
        if (!string.IsNullOrWhiteSpace(state.SearchQuery)) n++;
        if (!AgesEqual(state.AgeYears, defaults.AgeYears)) n++;
        if (state.HasMinWage) n++;
        if (state.HasMaxWage) n++;
        if (state.MinHoursPerWeek != defaults.MinHoursPerWeek
            || state.MaxHoursPerWeek != defaults.MaxHoursPerWeek)
        {
            n++;
        }

        if (state.MyVacanciesOnly) n++;
        // Travel preset (mode + minutes) is shown on its own chip — exclude from Filters (n).
        if (state.HasOrigin && state.RadiusKm != defaults.RadiusKm) n++;
        if (state.MinMatchPercent > 0) n++;
        return n;
    }

    private static bool AgesEqual(int? a, int? b) => a == b;
}

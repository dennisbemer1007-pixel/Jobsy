using System.Globalization;
using Jobsy.Core.Rules;
using Jobsy.Web.KandidaatBanen;
using Jobsy.Web.Models;
using Microsoft.AspNetCore.WebUtilities;

namespace Jobsy.Web.Banenkaart;

internal static class BanenkaartSsrQuery
{
    internal const string ApplyPath = "/api/banenkaart-client/apply-filters";
    internal const string ResetPath = "/api/banenkaart-client/reset-filters";
    internal const string SavePath = "/api/banenkaart-client/save";

    internal sealed record State(
        string? SearchQuery,
        IReadOnlyList<string> WorkTypes,
        int? MaxTravelMinutes,
        string Transport,
        int RadiusKm,
        int MinHoursPerWeek,
        int MaxHoursPerWeek,
        int? MinHourlyWageCents,
        int? MaxHourlyWageCents,
        int? AgeYears,
        int MinMatchPercent,
        IReadOnlyList<Guid> CategoryIds,
        IReadOnlyList<Guid> CompanyIds,
        string SortMode,
        bool DesktopListMode,
        double? OriginLat,
        double? OriginLng,
        string? OriginLabel,
        string? Error,
        bool Saved,
        int Page = 1,
        bool UrlOriginAbsentByQuery = false);

    /// <summary>
    /// URL carries filter params but no origin — origin must not be restored from cookie/profile (URL SSOT).
    /// </summary>
    internal static int DiscoverMaxMinutes(int? maxTravelMinutes)
        => maxTravelMinutes ?? 90;

    internal static bool UrlOmitsOriginWhileFiltered(IQueryCollection query)
    {
        if (query.ContainsKey("originLat") || query.ContainsKey("originLng"))
        {
            return false;
        }

        return query.ContainsKey("maxMinutes")
               || query.ContainsKey("transport")
               || query.ContainsKey("minHours")
               || query.ContainsKey("maxHours")
               || query.ContainsKey("q")
               || query.ContainsKey("categoryId")
               || query.ContainsKey("workType")
               || query.ContainsKey("sort")
               || query.ContainsKey("minWage")
               || query.ContainsKey("maxWage")
               || query.ContainsKey("age")
               || query.ContainsKey("minMatchPercent")
               || query.ContainsKey("companyId")
               || query.ContainsKey("radiusKm")
               || query.ContainsKey("weergave");
    }

    internal static State Parse(IQueryCollection query)
    {
        query.TryGetValue("q", out var qValues);
        var search = qValues.FirstOrDefault()?.Trim();
        if (string.IsNullOrWhiteSpace(search))
        {
            search = null;
        }

        query.TryGetValue("workType", out var workTypeValues);
        var workTypes = WorkTypeLabels.NormalizeFilterLabels(workTypeValues).ToList();

        var hasOriginInUrl = query.ContainsKey("originLat") || query.ContainsKey("originLng");
        int? maxMinutes = hasOriginInUrl ? null : KbMapStart.DefaultMaxTravelMinutes;
        if (query.TryGetValue("maxMinutes", out var minValues)
            && int.TryParse(minValues.FirstOrDefault(), out var parsedMinutes))
        {
            maxMinutes = Math.Clamp(parsedMinutes, 5, 90);
        }

        var transport = KbMapStart.DefaultTransport;
        if (query.TryGetValue("transport", out var transportValues))
        {
            transport = NormalizeTransport(transportValues.FirstOrDefault()) ?? transport;
        }

        var radiusKm = 15;
        if (query.TryGetValue("radiusKm", out var radiusValues)
            && int.TryParse(radiusValues.FirstOrDefault(), out var parsedRadius))
        {
            radiusKm = Math.Clamp(parsedRadius, 1, 50);
        }

        var minHours = 0;
        var maxHours = 40;
        if (query.TryGetValue("minHours", out var minH)
            && int.TryParse(minH.FirstOrDefault(), out var parsedMinH))
        {
            minHours = Math.Clamp(parsedMinH, 0, 40);
        }

        if (query.TryGetValue("maxHours", out var maxH)
            && int.TryParse(maxH.FirstOrDefault(), out var parsedMaxH))
        {
            maxHours = Math.Clamp(parsedMaxH, 0, 40);
        }

        int? minWage = null;
        int? maxWage = null;
        if (query.TryGetValue("minWage", out var minWageValues)
            && decimal.TryParse(minWageValues.FirstOrDefault(), NumberStyles.Any, CultureInfo.InvariantCulture, out var minWageDec))
        {
            minWage = (int)Math.Round(minWageDec * 100);
        }

        if (query.TryGetValue("maxWage", out var maxWageValues)
            && decimal.TryParse(maxWageValues.FirstOrDefault(), NumberStyles.Any, CultureInfo.InvariantCulture, out var maxWageDec))
        {
            maxWage = (int)Math.Round(maxWageDec * 100);
        }

        int? age = null;
        if (query.TryGetValue("age", out var ageValues)
            && int.TryParse(ageValues.FirstOrDefault(), out var parsedAge)
            && parsedAge >= 15)
        {
            age = parsedAge;
        }

        var minMatch = 0;
        if (query.TryGetValue("minMatchPercent", out var matchValues)
            && int.TryParse(matchValues.FirstOrDefault(), out var parsedMatch))
        {
            minMatch = Math.Clamp(parsedMatch, 0, 100);
        }

        var categoryIds = new List<Guid>();
        if (query.TryGetValue("categoryId", out var catValues))
        {
            foreach (var raw in catValues)
            {
                if (Guid.TryParse(raw, out var id))
                {
                    categoryIds.Add(id);
                }
            }
        }

        var companyIds = new List<Guid>();
        if (query.TryGetValue("companyId", out var companyValues))
        {
            foreach (var raw in companyValues)
            {
                if (Guid.TryParse(raw, out var id))
                {
                    companyIds.Add(id);
                }
            }
        }

        var sort = "distance";
        if (query.TryGetValue("sort", out var sortValues))
        {
            var rawSort = sortValues.FirstOrDefault()?.Trim();
            if (!string.IsNullOrWhiteSpace(rawSort))
            {
                sort = rawSort!;
            }
        }

        var listMode = false;
        if (query.TryGetValue("weergave", out var weergaveValues))
        {
            listMode = string.Equals(weergaveValues.FirstOrDefault(), "lijst", StringComparison.OrdinalIgnoreCase);
        }

        double? originLat = null;
        double? originLng = null;
        if (query.TryGetValue("originLat", out var latValues)
            && double.TryParse(latValues.FirstOrDefault(), NumberStyles.Any, CultureInfo.InvariantCulture, out var lat)
            && query.TryGetValue("originLng", out var lngValues)
            && double.TryParse(lngValues.FirstOrDefault(), NumberStyles.Any, CultureInfo.InvariantCulture, out var lng))
        {
            originLat = lat;
            originLng = lng;
        }

        query.TryGetValue("originLabel", out var labelValues);
        var originLabel = labelValues.FirstOrDefault()?.Trim();
        if (string.IsNullOrWhiteSpace(originLabel))
        {
            originLabel = null;
        }

        query.TryGetValue("error", out var errorValues);
        var error = errorValues.FirstOrDefault();
        if (string.IsNullOrWhiteSpace(error))
        {
            error = null;
        }

        var saved = string.Equals(query["saved"].FirstOrDefault(), "1", StringComparison.Ordinal);

        var page = 1;
        if (query.TryGetValue("page", out var pageValues)
            && int.TryParse(pageValues.FirstOrDefault(), out var parsedPage))
        {
            page = Math.Max(1, parsedPage);
        }

        return new State(
            search,
            workTypes,
            maxMinutes,
            transport,
            radiusKm,
            minHours,
            maxHours,
            minWage,
            maxWage,
            age,
            minMatch,
            categoryIds,
            companyIds,
            sort,
            listMode,
            originLat,
            originLng,
            originLabel,
            error,
            saved,
            page);
    }

    internal static string BuildUrl(State state, string? error = null, bool? saved = null)
    {
        var q = new List<string>();
        if (!string.IsNullOrWhiteSpace(state.SearchQuery))
        {
            q.Add($"q={Uri.EscapeDataString(state.SearchQuery)}");
        }

        foreach (var wt in state.WorkTypes)
        {
            q.Add($"workType={Uri.EscapeDataString(wt)}");
        }

        if (state.MaxTravelMinutes is int travelMinutes)
        {
            q.Add($"maxMinutes={travelMinutes}");
        }

        if (!string.Equals(state.Transport, KbMapStart.DefaultTransport, StringComparison.Ordinal))
        {
            q.Add($"transport={Uri.EscapeDataString(state.Transport)}");
        }

        if (state.RadiusKm != 15)
        {
            q.Add($"radiusKm={state.RadiusKm}");
        }

        if (state.MinHoursPerWeek != 0)
        {
            q.Add($"minHours={state.MinHoursPerWeek}");
        }

        if (state.MaxHoursPerWeek != 40)
        {
            q.Add($"maxHours={state.MaxHoursPerWeek}");
        }

        if (state.MinHourlyWageCents is int minCents)
        {
            q.Add($"minWage={(minCents / 100m).ToString(CultureInfo.InvariantCulture)}");
        }

        if (state.MaxHourlyWageCents is int maxCents)
        {
            q.Add($"maxWage={(maxCents / 100m).ToString(CultureInfo.InvariantCulture)}");
        }

        if (state.AgeYears is int age)
        {
            q.Add($"age={age}");
        }

        if (state.MinMatchPercent > 0)
        {
            q.Add($"minMatchPercent={state.MinMatchPercent}");
        }

        foreach (var id in state.CategoryIds)
        {
            q.Add($"categoryId={id:D}");
        }

        foreach (var id in state.CompanyIds)
        {
            q.Add($"companyId={id:D}");
        }

        if (!string.Equals(state.SortMode, "distance", StringComparison.Ordinal))
        {
            q.Add($"sort={Uri.EscapeDataString(state.SortMode)}");
        }

        if (state.DesktopListMode)
        {
            q.Add("weergave=lijst");
        }

        if (state.OriginLat is double lat && state.OriginLng is double lng)
        {
            q.Add($"originLat={lat.ToString(CultureInfo.InvariantCulture)}");
            q.Add($"originLng={lng.ToString(CultureInfo.InvariantCulture)}");
            if (!string.IsNullOrWhiteSpace(state.OriginLabel))
            {
                q.Add($"originLabel={Uri.EscapeDataString(state.OriginLabel!)}");
            }
        }

        var err = error ?? state.Error;
        if (!string.IsNullOrWhiteSpace(err))
        {
            q.Add($"error={Uri.EscapeDataString(err)}");
        }

        if (saved == true || (saved is null && state.Saved))
        {
            q.Add("saved=1");
        }

        if (state.Page > 1)
        {
            q.Add($"page={state.Page}");
        }

        return q.Count == 0 ? "/banenkaart" : "/banenkaart?" + string.Join("&", q);
    }

    internal static State FromForm(IFormCollection form, State? fallback = null)
    {
        fallback ??= new State(null, [], KbMapStart.DefaultMaxTravelMinutes, KbMapStart.DefaultTransport, 15, 0, 40, null, null, null, 0, [], [], "distance", false, null, null, null, null, false, 1);

        var search = form["q"].ToString().Trim();
        if (string.IsNullOrWhiteSpace(search))
        {
            search = null;
        }

        var workTypes = form["workType"].Select(v => v?.Trim()).Where(v => !string.IsNullOrWhiteSpace(v)).Cast<string>().ToList();

        int? maxMinutes = int.TryParse(form["maxMinutes"], out var mm) ? Math.Clamp(mm, 5, 90) : fallback.MaxTravelMinutes;
        var transport = NormalizeTransport(form["transport"]) ?? fallback.Transport;
        var radiusKm = int.TryParse(form["radiusKm"], out var rk) ? Math.Clamp(rk, 1, 50) : fallback.RadiusKm;
        var minHours = int.TryParse(form["minHours"], out var minH) ? Math.Clamp(minH, 0, 40) : fallback.MinHoursPerWeek;
        var maxHours = int.TryParse(form["maxHours"], out var maxH) ? Math.Clamp(maxH, 0, 40) : fallback.MaxHoursPerWeek;

        int? minWage = null;
        if (decimal.TryParse(form["minWage"], NumberStyles.Any, CultureInfo.InvariantCulture, out var minDec))
        {
            minWage = (int)Math.Round(minDec * 100);
        }

        int? maxWage = null;
        if (decimal.TryParse(form["maxWage"], NumberStyles.Any, CultureInfo.InvariantCulture, out var maxDec))
        {
            maxWage = (int)Math.Round(maxDec * 100);
        }

        int? age = null;
        if (int.TryParse(form["age"], out var parsedAge) && parsedAge >= 15)
        {
            age = parsedAge;
        }

        var minMatch = int.TryParse(form["minMatchPercent"], out var mp) ? Math.Clamp(mp, 0, 100) : fallback.MinMatchPercent;

        var categoryIds = form["categoryId"]
            .Select(v => Guid.TryParse(v, out var id) ? id : Guid.Empty)
            .Where(id => id != Guid.Empty)
            .ToList();

        var sort = form["sort"].ToString();
        if (string.IsNullOrWhiteSpace(sort))
        {
            sort = fallback.SortMode;
        }

        var listMode = string.Equals(form["weergave"], "lijst", StringComparison.OrdinalIgnoreCase);

        double? originLat = fallback.OriginLat;
        double? originLng = fallback.OriginLng;
        var originLabel = fallback.OriginLabel;
        if (double.TryParse(form["originLat"], NumberStyles.Any, CultureInfo.InvariantCulture, out var lat)
            && double.TryParse(form["originLng"], NumberStyles.Any, CultureInfo.InvariantCulture, out var lng))
        {
            originLat = lat;
            originLng = lng;
            var label = form["originLabel"].ToString().Trim();
            originLabel = string.IsNullOrWhiteSpace(label) ? originLabel : label;
        }

        return new State(
            search,
            workTypes,
            maxMinutes,
            transport,
            radiusKm,
            minHours,
            maxHours,
            minWage,
            maxWage,
            age,
            minMatch,
            categoryIds,
            fallback.CompanyIds,
            sort,
            listMode,
            originLat,
            originLng,
            originLabel,
            null,
            false,
            1);
    }

    /// <summary>Default list sort for banenkaart SSR + client catalog (logged-in candidate with fit).</summary>
    internal static string ResolveEffectiveSort(
        string sortFromState,
        bool sortExplicitInUrl,
        bool isCandidate,
        bool showFitFilters,
        bool hasOrigin)
    {
        if (sortExplicitInUrl && !string.IsNullOrWhiteSpace(sortFromState))
        {
            return sortFromState.Trim().ToLowerInvariant();
        }

        if (isCandidate && showFitFilters)
        {
            return "match";
        }

        if (hasOrigin)
        {
            return "distance";
        }

        return string.IsNullOrWhiteSpace(sortFromState) || string.Equals(sortFromState, "distance", StringComparison.OrdinalIgnoreCase)
            ? "title"
            : sortFromState.Trim().ToLowerInvariant();
    }

    internal static IReadOnlyList<VacancyListItem> OrderVacancies(
        IReadOnlyList<VacancyListItem> items,
        string sortMode,
        bool showFitFilters,
        bool hasOrigin)
    {
        if (items.Count <= 1)
        {
            return items;
        }

        var mode = (sortMode ?? "").Trim().ToLowerInvariant();
        if (mode == "match" && showFitFilters)
        {
            return items
                .OrderByDescending(v => v.FitPercent ?? v.MatchPercent ?? -1)
                .ThenBy(v => v.TravelMinutes ?? int.MaxValue)
                .ThenBy(v => v.Title, StringComparer.CurrentCulture)
                .ToList();
        }

        if (mode == "distance" && hasOrigin)
        {
            return items
                .OrderBy(v => v.TravelMinutes ?? int.MaxValue)
                .ThenBy(v => v.Title, StringComparer.CurrentCulture)
                .ToList();
        }

        if (mode == "title")
        {
            return items
                .OrderBy(v => v.Title, StringComparer.CurrentCulture)
                .ToList();
        }

        return items;
    }

    private static string? NormalizeTransport(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return null;
        }

        return raw switch
        {
            var t when t.Equals("Lopend", StringComparison.OrdinalIgnoreCase) => "Lopend",
            var t when t.Equals("Fiets", StringComparison.OrdinalIgnoreCase) => "Fiets",
            var t when t.Equals("E-bike", StringComparison.OrdinalIgnoreCase) => "E-bike",
            var t when t.Equals("Auto", StringComparison.OrdinalIgnoreCase) => "Auto",
            var t when t.Equals("OV", StringComparison.OrdinalIgnoreCase) => "OV",
            _ => TransportLabels.IsEBike(raw) ? "E-bike" : null
        };
    }
}

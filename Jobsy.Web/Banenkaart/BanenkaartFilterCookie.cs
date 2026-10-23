using System.Text;
using System.Text.Json;
using Jobsy.Web.KandidaatBanen;
using Microsoft.AspNetCore.Http;

namespace Jobsy.Web.Banenkaart;

/// <summary>Persists banenkaart filter state for SSR first paint (guest + signed-in candidate).</summary>
internal static class BanenkaartFilterCookie
{
    internal const string CookieName = "Jobsy.BanenkaartFilters";
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    internal static BanenkaartSsrQuery.State Merge(HttpContext http, BanenkaartSsrQuery.State fromUrl)
    {
        if (!TryRead(http.Request, out var stored))
        {
            return fromUrl;
        }

        return fromUrl with
        {
            SearchQuery = fromUrl.SearchQuery ?? stored.SearchQuery,
            WorkTypes = fromUrl.WorkTypes.Count > 0 ? fromUrl.WorkTypes : stored.WorkTypes,
            MaxTravelMinutes = HasExplicitTravel(http.Request) ? fromUrl.MaxTravelMinutes : stored.MaxTravelMinutes,
            Transport = HasExplicitTransport(http.Request) ? fromUrl.Transport : stored.Transport,
            RadiusKm = http.Request.Query.ContainsKey("radiusKm") ? fromUrl.RadiusKm : stored.RadiusKm,
            MinHoursPerWeek = http.Request.Query.ContainsKey("minHours") ? fromUrl.MinHoursPerWeek : stored.MinHoursPerWeek,
            MaxHoursPerWeek = http.Request.Query.ContainsKey("maxHours") ? fromUrl.MaxHoursPerWeek : stored.MaxHoursPerWeek,
            MinHourlyWageCents = http.Request.Query.ContainsKey("minWage") ? fromUrl.MinHourlyWageCents : stored.MinHourlyWageCents,
            MaxHourlyWageCents = http.Request.Query.ContainsKey("maxWage") ? fromUrl.MaxHourlyWageCents : stored.MaxHourlyWageCents,
            AgeYears = http.Request.Query.ContainsKey("age") ? fromUrl.AgeYears : stored.AgeYears,
            MinMatchPercent = http.Request.Query.ContainsKey("minMatchPercent") ? fromUrl.MinMatchPercent : stored.MinMatchPercent,
            CategoryIds = fromUrl.CategoryIds.Count > 0 ? fromUrl.CategoryIds : stored.CategoryIds,
            CompanyIds = fromUrl.CompanyIds.Count > 0 ? fromUrl.CompanyIds : stored.CompanyIds,
            SortMode = http.Request.Query.ContainsKey("sort") ? fromUrl.SortMode : stored.SortMode,
            OriginLat = fromUrl.OriginLat ?? stored.OriginLat,
            OriginLng = fromUrl.OriginLng ?? stored.OriginLng,
            OriginLabel = fromUrl.OriginLabel ?? stored.OriginLabel,
        };
    }

    internal static void Persist(HttpResponse response, BanenkaartSsrQuery.State state)
    {
        var dto = FilterDto.FromState(state);
        var json = JsonSerializer.Serialize(dto, JsonOptions);
        var encoded = Convert.ToBase64String(Encoding.UTF8.GetBytes(json));
        response.Cookies.Append(
            CookieName,
            encoded,
            new CookieOptions
            {
                Path = "/",
                MaxAge = TimeSpan.FromDays(180),
                SameSite = SameSiteMode.Lax,
                HttpOnly = true,
                Secure = response.HttpContext.Request.IsHttps,
                IsEssential = true,
            });
    }

    internal static void Clear(HttpResponse response)
    {
        response.Cookies.Delete(CookieName, new CookieOptions { Path = "/" });
    }

    internal static bool TryRead(HttpRequest request, out BanenkaartSsrQuery.State state)
    {
        state = default!;
        if (!request.Cookies.TryGetValue(CookieName, out var raw) || string.IsNullOrWhiteSpace(raw))
        {
            return false;
        }

        try
        {
            var json = Encoding.UTF8.GetString(Convert.FromBase64String(raw));
            var dto = JsonSerializer.Deserialize<FilterDto>(json, JsonOptions);
            if (dto is null)
            {
                return false;
            }

            state = dto.ToState();
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static bool HasExplicitTravel(HttpRequest request)
        => request.Query.ContainsKey("maxMinutes");

    private static bool HasExplicitTransport(HttpRequest request)
        => request.Query.ContainsKey("transport");

    private sealed class FilterDto
    {
        public string? SearchQuery { get; set; }
        public List<string> WorkTypes { get; set; } = [];
        public int MaxTravelMinutes { get; set; }
        public string Transport { get; set; } = KbMapStart.DefaultTransport;
        public int RadiusKm { get; set; } = 15;
        public int MinHoursPerWeek { get; set; }
        public int MaxHoursPerWeek { get; set; } = 40;
        public int? MinHourlyWageCents { get; set; }
        public int? MaxHourlyWageCents { get; set; }
        public int? AgeYears { get; set; }
        public int MinMatchPercent { get; set; }
        public List<Guid> CategoryIds { get; set; } = [];
        public List<Guid> CompanyIds { get; set; } = [];
        public string SortMode { get; set; } = "distance";
        public double? OriginLat { get; set; }
        public double? OriginLng { get; set; }
        public string? OriginLabel { get; set; }

        public static FilterDto FromState(BanenkaartSsrQuery.State state) => new()
        {
            SearchQuery = state.SearchQuery,
            WorkTypes = state.WorkTypes.ToList(),
            MaxTravelMinutes = state.MaxTravelMinutes,
            Transport = state.Transport,
            RadiusKm = state.RadiusKm,
            MinHoursPerWeek = state.MinHoursPerWeek,
            MaxHoursPerWeek = state.MaxHoursPerWeek,
            MinHourlyWageCents = state.MinHourlyWageCents,
            MaxHourlyWageCents = state.MaxHourlyWageCents,
            AgeYears = state.AgeYears,
            MinMatchPercent = state.MinMatchPercent,
            CategoryIds = state.CategoryIds.ToList(),
            CompanyIds = state.CompanyIds.ToList(),
            SortMode = state.SortMode,
            OriginLat = state.OriginLat,
            OriginLng = state.OriginLng,
            OriginLabel = state.OriginLabel,
        };

        public BanenkaartSsrQuery.State ToState() => new(
            SearchQuery,
            WorkTypes,
            MaxTravelMinutes,
            Transport,
            RadiusKm,
            MinHoursPerWeek,
            MaxHoursPerWeek,
            MinHourlyWageCents,
            MaxHourlyWageCents,
            AgeYears,
            MinMatchPercent,
            CategoryIds,
            CompanyIds,
            SortMode,
            false,
            OriginLat,
            OriginLng,
            OriginLabel,
            null,
            false,
            1);
    }
}

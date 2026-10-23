using System.Security.Cryptography;
using System.Text.Json;
using Jobsy.Web.Models;
using Jobsy.Web.Services;
using Microsoft.Extensions.Caching.Memory;

namespace Jobsy.Web.Banenkaart;

internal static class BanenkaartClientPins
{
    internal const string Path = "/api/banenkaart-client/pins";
    private static readonly TimeSpan CacheTtl = TimeSpan.FromSeconds(60);

    internal static async Task<IResult> GetAsync(
        HttpContext http,
        JobsyApiClient api,
        IMemoryCache cache,
        CancellationToken cancellationToken)
    {
        var query = BanenkaartSsrQuery.Parse(http.Request.Query);
        var scope = http.Request.Query["scope"].FirstOrDefault();
        var mapScope = !string.Equals(scope, "list", StringComparison.OrdinalIgnoreCase);

        var cacheKey = BuildCacheKey(http.Request.QueryString.Value, mapScope);
        if (cache.TryGetValue(cacheKey, out CachedPins? hit) && hit is not null)
        {
            if (http.Request.Headers.IfNoneMatch.Contains(hit.ETag))
            {
                http.Response.Headers.ETag = hit.ETag;
                http.Response.Headers.CacheControl = "private, max-age=60, stale-while-revalidate=120";
                return Results.StatusCode(StatusCodes.Status304NotModified);
            }

            return Write(http, hit.Body, hit.ETag);
        }

        decimal? minWage = query.MinHourlyWageCents is int minC ? minC / 100m : null;
        decimal? maxWage = query.MaxHourlyWageCents is int maxC ? maxC / 100m : null;
        var companyFocus = query.CompanyIds.Count > 0;

        double? pinOriginLat = mapScope ? null : query.OriginLat;
        double? pinOriginLng = mapScope ? null : query.OriginLng;
        double? pinRadius = mapScope || pinOriginLat is null ? null : query.RadiusKm;

        IReadOnlyList<VacancyPinBootItem> pins;
        try
        {
            pins = await api.GetVacancyPinsAsync(
                query.Transport,
                query.MaxTravelMinutes,
                pinOriginLat,
                pinOriginLng,
                pinRadius,
                companyFocus || query.WorkTypes.Count == 0 ? null : query.WorkTypes,
                companyFocus ? null : query.SearchQuery,
                companyFocus || query.CategoryIds.Count == 0 ? null : query.CategoryIds,
                companyFocus ? null : query.CompanyIds,
                http.User.IsInRole("Candidate") && query.MinMatchPercent > 0 ? query.MinMatchPercent : null,
                ct: cancellationToken);
        }
        catch
        {
            return Results.StatusCode(StatusCodes.Status502BadGateway);
        }

        var compact = pins.Select(p => new PinDto(
            p.Id,
            p.Lat,
            p.Lng,
            p.Colour,
            p.WorkType,
            p.PublicMapRadiusKm)).ToList();

        var body = JsonSerializer.SerializeToUtf8Bytes(compact, JsonSerializerOptions);
        var etag = ComputeEtag(body);
        cache.Set(cacheKey, new CachedPins(body, etag), CacheTtl);
        return Write(http, body, etag);
    }

    private static readonly JsonSerializerOptions JsonSerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
    };

    private sealed record PinDto(
        Guid Id,
        double Lat,
        double Lng,
        string? Colour,
        string? WorkType,
        int? PublicMapRadiusKm);

    private static string BuildCacheKey(string? queryString, bool mapScope)
    {
        var raw = queryString?.TrimStart('?') ?? "";
        return $"bk-pins-v1:{(mapScope ? "map" : "list")}:{raw}";
    }

    private static string ComputeEtag(byte[] body)
    {
        var hash = SHA256.HashData(body);
        return Convert.ToHexString(hash.AsSpan(0, 16)).ToLowerInvariant();
    }

    private static IResult Write(HttpContext http, byte[] body, string etag)
    {
        http.Response.Headers.ETag = etag;
        http.Response.Headers.CacheControl = "private, max-age=60, stale-while-revalidate=120";
        return Results.Bytes(body, "application/json");
    }

    private sealed record CachedPins(byte[] Body, string ETag);
}

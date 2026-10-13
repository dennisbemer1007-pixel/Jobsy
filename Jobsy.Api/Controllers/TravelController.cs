using System.Globalization;
using System.Text;
using Jobsy.Core.Interfaces;
using Jobsy.Core.Rules;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Caching.Memory;

namespace Jobsy.Api.Controllers;

[ApiController]
[Route("api/travel")]
public sealed class TravelController : ControllerBase
{
    private static readonly TimeSpan ResponseCacheTtl = TimeSpan.FromDays(7);

    private readonly IIsochroneService _isochrones;
    private readonly IMemoryCache _cache;

    public TravelController(IIsochroneService isochrones, IMemoryCache cache)
    {
        _isochrones = isochrones;
        _cache = cache;
    }

    /// <summary>
    /// GeoJSON FeatureCollection of travel-time polygons (largest first).
    /// Falls back to 404 when the provider has no data (client draws circles).
    /// </summary>
    [HttpGet("isochrones")]
    [AllowAnonymous]
    [EnableRateLimiting("public-read")]
    public async Task<IActionResult> GetIsochrones(
        [FromQuery] double lat,
        [FromQuery] double lng,
        [FromQuery] string mode = TransportLabels.Bike,
        [FromQuery] string minutes = "10,20,30",
        CancellationToken cancellationToken = default)
    {
        if (double.IsNaN(lat) || double.IsNaN(lng)
            || lat is < -90 or > 90 || lng is < -180 or > 180)
        {
            return BadRequest();
        }

        var parsedMinutes = ParseMinutes(minutes);
        if (parsedMinutes.Count == 0)
        {
            return BadRequest();
        }

        var transport = TransportLabels.Parse(mode);
        var lat3 = Math.Round(lat, 3, MidpointRounding.AwayFromZero);
        var lng3 = Math.Round(lng, 3, MidpointRounding.AwayFromZero);
        var cacheKey = "api-iso:" + lat3.ToString(CultureInfo.InvariantCulture) + ":" +
                       lng3.ToString(CultureInfo.InvariantCulture) + ":" +
                       transport + ":" + string.Join(',', parsedMinutes);

        if (_cache.TryGetValue(cacheKey, out string? cached) && !string.IsNullOrWhiteSpace(cached))
        {
            return Content(cached, "application/geo+json", Encoding.UTF8);
        }

        var geojson = await _isochrones
            .TryGetIsochronesAsync(lat3, lng3, transport, parsedMinutes, cancellationToken)
            .ConfigureAwait(false);
        if (string.IsNullOrWhiteSpace(geojson))
        {
            return NotFound();
        }

        _cache.Set(cacheKey, geojson, ResponseCacheTtl);
        Response.Headers.CacheControl = "public,max-age=604800";
        return Content(geojson, "application/geo+json", Encoding.UTF8);
    }

    private static List<int> ParseMinutes(string? raw)
    {
        var list = new List<int>();
        if (string.IsNullOrWhiteSpace(raw))
        {
            return list;
        }

        foreach (var part in raw.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            if (int.TryParse(part, NumberStyles.Integer, CultureInfo.InvariantCulture, out var m)
                && m is > 0 and <= 120)
            {
                list.Add(m);
            }
        }

        return list.Distinct().OrderByDescending(m => m).ToList();
    }
}

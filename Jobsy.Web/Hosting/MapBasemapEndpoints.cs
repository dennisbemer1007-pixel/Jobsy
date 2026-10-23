using Microsoft.Extensions.Caching.Memory;

namespace Jobsy.Web.Hosting;

/// <summary>Same-origin proxy for the OpenFreeMap liberty style JSON (faster TLS + cache).</summary>
public static class MapBasemapEndpoints
{
    private const string UpstreamStyleUrl = "https://tiles.openfreemap.org/styles/liberty";
    internal const string LibertyStylePath = "/map/basemap/liberty/style.json";
    private static readonly TimeSpan CacheTtl = TimeSpan.FromHours(24);

    public static IEndpointRouteBuilder MapMapBasemapEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet(LibertyStylePath, GetLibertyStyleAsync).AllowAnonymous();
        return app;
    }

    private static async Task<IResult> GetLibertyStyleAsync(
        IHttpClientFactory httpClientFactory,
        IMemoryCache cache,
        CancellationToken cancellationToken)
    {
        const string cacheKey = "map-style-liberty-v1";
        if (cache.TryGetValue(cacheKey, out byte[]? body) && body is { Length: > 0 })
        {
            return Results.Bytes(body, "application/json");
        }

        var client = httpClientFactory.CreateClient();
        client.Timeout = TimeSpan.FromSeconds(20);
        byte[] payload;
        try
        {
            payload = await client.GetByteArrayAsync(UpstreamStyleUrl, cancellationToken);
        }
        catch
        {
            return Results.StatusCode(StatusCodes.Status502BadGateway);
        }

        if (payload.Length == 0)
        {
            return Results.StatusCode(StatusCodes.Status502BadGateway);
        }

        cache.Set(cacheKey, payload, CacheTtl);
        return Results.Bytes(payload, "application/json");
    }
}

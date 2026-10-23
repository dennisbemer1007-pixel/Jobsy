using System.Security.Cryptography;
using Jobsy.Web.Services;
using Microsoft.Extensions.Caching.Memory;

namespace Jobsy.Web.Banenkaart;

internal static class BanenkaartClientCatalog
{
    internal const string AllPath = "/api/banenkaart-client/all";
    private static readonly TimeSpan CacheTtl = TimeSpan.FromMinutes(15);

    internal static async Task<IResult> GetAllAsync(
        HttpContext http,
        JobsyApiClient api,
        IMemoryCache cache,
        CancellationToken cancellationToken)
    {
        const string cacheKey = "bk-catalog-v1";
        if (cache.TryGetValue(cacheKey, out CachedCatalog? cached) && cached is not null)
        {
            if (http.Request.Headers.IfNoneMatch.Contains(cached.ETag))
            {
                http.Response.Headers.ETag = cached.ETag;
                http.Response.Headers.CacheControl = "private, max-age=900, stale-while-revalidate=3600";
                return Results.StatusCode(StatusCodes.Status304NotModified);
            }

            return Write(http, cached.Body, cached.ETag);
        }

        JobsyApiClient.BanenkaartCatalogHttpResult? upstream;
        try
        {
            upstream = await api.GetBanenkaartCatalogAsync(cancellationToken);
        }
        catch
        {
            upstream = null;
        }

        if (upstream is null)
        {
            return Results.StatusCode(StatusCodes.Status502BadGateway);
        }

        if (upstream.Value.NotModified && cache.TryGetValue(cacheKey, out cached) && cached is not null)
        {
            http.Response.Headers.ETag = cached.ETag;
            http.Response.Headers.CacheControl = "private, max-age=900, stale-while-revalidate=3600";
            return Results.StatusCode(StatusCodes.Status304NotModified);
        }

        var body = upstream.Value.Body;
        if (body is null || body.Length == 0)
        {
            return Results.StatusCode(StatusCodes.Status502BadGateway);
        }

        var etag = NormalizeEtag(upstream.Value.ETag) ?? ComputeEtag(body);
        cache.Set(cacheKey, new CachedCatalog(body, etag), CacheTtl);
        return Write(http, body, etag);
    }

    private static string? NormalizeEtag(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return null;
        }

        var tag = raw.Trim();
        if (!tag.StartsWith('"'))
        {
            tag = "\"" + tag.Trim('"') + "\"";
        }

        return tag;
    }

    private static string ComputeEtag(byte[] body)
    {
        var hash = SHA256.HashData(body);
        return "\"" + Convert.ToHexString(hash.AsSpan(0, 16)).ToLowerInvariant() + "\"";
    }

    private static IResult Write(HttpContext http, byte[] body, string etag)
    {
        http.Response.Headers.ETag = etag;
        http.Response.Headers.CacheControl = "private, max-age=900, stale-while-revalidate=3600";
        return Results.Bytes(body, "application/json");
    }

    private sealed record CachedCatalog(byte[] Body, string ETag);
}

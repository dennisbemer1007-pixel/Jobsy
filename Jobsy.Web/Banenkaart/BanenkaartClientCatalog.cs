using Microsoft.Extensions.Caching.Memory;

namespace Jobsy.Web.Banenkaart;

internal static class BanenkaartClientCatalog
{
    internal const string AllPath = "/api/banenkaart-client/all";
    private static readonly TimeSpan CacheTtl = TimeSpan.FromMinutes(15);

    internal static async Task<IResult> GetAllAsync(
        HttpContext http,
        IHttpClientFactory httpClientFactory,
        IMemoryCache cache,
        CancellationToken cancellationToken)
    {
        var client = httpClientFactory.CreateClient("JobsyApi");
        var cacheKey = "bk-catalog-v1";
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

        using var request = new HttpRequestMessage(HttpMethod.Get, "api/vacancies/banenkaart-catalog");
        if (http.Request.Headers.TryGetValue("If-None-Match", out var inm))
        {
            request.Headers.TryAddWithoutValidation("If-None-Match", inm.ToString());
        }

        HttpResponseMessage response;
        try
        {
            response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        }
        catch
        {
            return Results.StatusCode(StatusCodes.Status502BadGateway);
        }

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var ms = new MemoryStream();
        await stream.CopyToAsync(ms, cancellationToken);
        var body = ms.ToArray();
        var etag = response.Headers.ETag?.Tag ?? response.Headers.ETag?.ToString() ?? "";

        if (response.StatusCode == System.Net.HttpStatusCode.NotModified && cache.TryGetValue(cacheKey, out cached) && cached is not null)
        {
            http.Response.Headers.ETag = cached.ETag;
            http.Response.Headers.CacheControl = "private, max-age=900, stale-while-revalidate=3600";
            return Results.StatusCode(StatusCodes.Status304NotModified);
        }

        if (response.IsSuccessStatusCode && body.Length > 0 && !string.IsNullOrWhiteSpace(etag))
        {
            cache.Set(cacheKey, new CachedCatalog(body, etag), CacheTtl);
        }

        if (!response.IsSuccessStatusCode)
        {
            return Results.StatusCode((int)response.StatusCode);
        }

        return Write(http, body, etag);
    }

    private static IResult Write(HttpContext http, byte[] body, string etag)
    {
        if (!string.IsNullOrWhiteSpace(etag))
        {
            http.Response.Headers.ETag = etag;
        }

        http.Response.Headers.CacheControl = "private, max-age=900, stale-while-revalidate=3600";
        return Results.Bytes(body, "application/json");
    }

    private sealed record CachedCatalog(byte[] Body, string ETag);
}

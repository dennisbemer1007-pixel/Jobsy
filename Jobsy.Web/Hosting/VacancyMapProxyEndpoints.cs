using System.Collections.Concurrent;
using System.Net;
using System.Net.Http.Headers;
using Jobsy.Core.Security;
using Jobsy.Web.Services;
using Microsoft.Extensions.Caching.Memory;

namespace Jobsy.Web.Hosting;

/// <summary>
/// Same-origin proxies so the banenkaart (jobMap.js) can call /api/vacancies/* on the web host.
/// Those routes only exist on the API service; without this proxy the browser gets 404 on lobsy.nl.
/// Forwards visitor IP + internal secret so API rate limits partition per visitor, not the Web hop.
/// </summary>
public static class VacancyMapProxyEndpoints
{
    public static readonly TimeSpan AnonymousCacheTtl = TimeSpan.FromSeconds(60);

    public static void MapVacancyMapProxyEndpoints(this WebApplication app)
    {
        app.MapGet("/api/vacancies/pins", (HttpContext http, IVacancyMapApiForwarder forwarder, CancellationToken ct) =>
            forwarder.ForwardAsync(http, "api/vacancies/pins", ct));

        app.MapGet("/api/vacancies/cards", (HttpContext http, IVacancyMapApiForwarder forwarder, CancellationToken ct) =>
            forwarder.ForwardAsync(http, "api/vacancies/cards", ct));

        app.MapGet("/api/vacancies/{id:guid}/card", (HttpContext http, Guid id, IVacancyMapApiForwarder forwarder, CancellationToken ct) =>
            forwarder.ForwardAsync(http, $"api/vacancies/{id:D}/card", ct));

        // Legacy detail path still used by older cached jobMap bundles.
        app.MapGet("/api/vacancies/{id:guid}", (HttpContext http, Guid id, IVacancyMapApiForwarder forwarder, CancellationToken ct) =>
            forwarder.ForwardAsync(http, $"api/vacancies/{id:D}", ct));
    }
}

public interface IVacancyMapApiForwarder
{
    Task ForwardAsync(HttpContext http, string apiPath, CancellationToken ct);
}

/// <summary>Forwards map API GETs to the Jobsy API with visitor IP + caller identity when present.</summary>
public sealed class VacancyMapApiForwarder : IVacancyMapApiForwarder
{
    private static readonly ConcurrentDictionary<string, byte> CacheKeys = new(StringComparer.Ordinal);

    private readonly IConfiguration _configuration;
    private readonly IServiceProvider _services;
    private readonly IMemoryCache _cache;

    public VacancyMapApiForwarder(
        IConfiguration configuration,
        IServiceProvider services,
        IMemoryCache cache)
    {
        _configuration = configuration;
        _services = services;
        _cache = cache;
    }

    public async Task ForwardAsync(HttpContext http, string apiPath, CancellationToken ct)
    {
        var query = http.Request.QueryString.HasValue ? http.Request.QueryString.Value : "";
        var target = apiPath + query;
        var cacheable = IsAnonymousCacheable(http, apiPath);
        var cacheKey = "map-proxy:" + target;

        if (cacheable && _cache.TryGetValue(cacheKey, out CachedProxyResponse? cached) && cached is not null)
        {
            http.Response.StatusCode = cached.StatusCode;
            if (!string.IsNullOrWhiteSpace(cached.ETag))
            {
                http.Response.Headers.ETag = cached.ETag;
            }

            if (!string.IsNullOrWhiteSpace(cached.CacheControl))
            {
                http.Response.Headers.CacheControl = cached.CacheControl;
            }

            http.Response.ContentType = cached.ContentType ?? "application/json; charset=utf-8";
            await http.Response.Body.WriteAsync(cached.Body, ct);
            return;
        }

        using var client = JobsyApiClientFactory.Create(_services, _configuration);
        using var request = new HttpRequestMessage(HttpMethod.Get, target);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        if (http.Request.Headers.TryGetValue("If-None-Match", out var etag))
        {
            request.Headers.TryAddWithoutValidation("If-None-Match", etag.ToString());
        }

        ApplyVisitorIdentity(request, http);

        using var response = await client.SendAsync(
            request,
            HttpCompletionOption.ResponseHeadersRead,
            ct);

        http.Response.StatusCode = (int)response.StatusCode;

        if (response.Headers.TryGetValues("Retry-After", out var retryAfterValues))
        {
            http.Response.Headers.RetryAfter = retryAfterValues.FirstOrDefault();
        }
        else if (response.Headers.RetryAfter is { } retryAfter)
        {
            http.Response.Headers.RetryAfter = retryAfter.ToString();
        }

        if (response.Headers.ETag is { } responseEtag)
        {
            http.Response.Headers.ETag = responseEtag.ToString();
        }

        if (response.Headers.CacheControl is { } cache)
        {
            http.Response.Headers.CacheControl = cache.ToString();
        }
        else if (response.Headers.TryGetValues("Cache-Control", out var cacheValues))
        {
            http.Response.Headers.CacheControl = string.Join(", ", cacheValues);
        }

        if (response.Content.Headers.ContentType is { } contentType)
        {
            http.Response.ContentType = contentType.ToString();
        }

        var body = await response.Content.ReadAsByteArrayAsync(ct);

        // Never cache failures (429/5xx) — retries must hit the API again.
        if (cacheable
            && response.StatusCode is HttpStatusCode.OK or HttpStatusCode.NotModified)
        {
            var entry = new CachedProxyResponse(
                (int)response.StatusCode,
                body,
                http.Response.Headers.ETag.ToString(),
                http.Response.Headers.CacheControl.ToString(),
                http.Response.ContentType);
            _cache.Set(cacheKey, entry, VacancyMapProxyEndpoints.AnonymousCacheTtl);
            CacheKeys.TryAdd(cacheKey, 0);
        }

        await http.Response.Body.WriteAsync(body, ct);
    }

    /// <summary>
    /// Attaches visitor IP + internal secret. <see cref="JobsyApiAuthHandler"/> also
    /// applies these; setting them here keeps the proxy correct if the handler chain changes.
    /// </summary>
    private void ApplyVisitorIdentity(HttpRequestMessage request, HttpContext http)
    {
        request.Headers.Remove(InternalClientIpHeaders.ClientIpHeader);
        request.Headers.Remove(InternalClientIpHeaders.InternalSecretHeader);

        var secret = _configuration[InternalClientIpHeaders.ConfigKey];
        if (string.IsNullOrWhiteSpace(secret))
        {
            return;
        }

        var visitorIp = ResolveVisitorIp(http);
        if (string.IsNullOrWhiteSpace(visitorIp))
        {
            return;
        }

        request.Headers.TryAddWithoutValidation(InternalClientIpHeaders.ClientIpHeader, visitorIp);
        request.Headers.TryAddWithoutValidation(InternalClientIpHeaders.InternalSecretHeader, secret.Trim());
    }

    /// <summary>Prefer Cloudflare visitor IP, then connection remote IP.</summary>
    public static string? ResolveVisitorIp(HttpContext? http)
    {
        if (http is null)
        {
            return null;
        }

        if (http.Request.Headers.TryGetValue("CF-Connecting-IP", out var cf)
            && !string.IsNullOrWhiteSpace(cf.ToString())
            && IPAddress.TryParse(cf.ToString().Trim(), out _))
        {
            return cf.ToString().Trim();
        }

        return http.Connection.RemoteIpAddress?.ToString();
    }

    private static bool IsAnonymousCacheable(HttpContext http, string apiPath)
    {
        if (http.User.Identity?.IsAuthenticated == true)
        {
            return false;
        }

        // Cache pins + card(s) only — not legacy full vacancy detail.
        return apiPath.Equals("api/vacancies/pins", StringComparison.OrdinalIgnoreCase)
               || apiPath.Equals("api/vacancies/cards", StringComparison.OrdinalIgnoreCase)
               || (apiPath.StartsWith("api/vacancies/", StringComparison.OrdinalIgnoreCase)
                   && apiPath.EndsWith("/card", StringComparison.OrdinalIgnoreCase));
    }

    private sealed record CachedProxyResponse(
        int StatusCode,
        byte[] Body,
        string? ETag,
        string? CacheControl,
        string? ContentType);
}

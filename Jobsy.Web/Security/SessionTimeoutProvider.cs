using System.Net.Http.Json;
using Jobsy.Core;
using Jobsy.Core.Options;
using Jobsy.Core.Rules;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace Jobsy.Web.Security;

public interface ISessionTimeoutProvider
{
    Task<int> GetInactivityTimeoutMinutesAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Reads the admin-configured inactivity timeout from the API (short-lived cache for immediate roll-out).
/// </summary>
public sealed class SessionTimeoutProvider : ISessionTimeoutProvider
{
    public const string CacheKey = "jobsy.session.inactivity-timeout-minutes";
    private static readonly TimeSpan CacheTtl = TimeSpan.FromSeconds(30);

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _configuration;
    private readonly IMemoryCache _cache;
    private readonly ILogger<SessionTimeoutProvider> _logger;
    private readonly SessionAuthOptions _sessionOptions;

    public SessionTimeoutProvider(
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration,
        IMemoryCache cache,
        ILogger<SessionTimeoutProvider> logger,
        IOptions<SessionAuthOptions> sessionOptions)
    {
        _httpClientFactory = httpClientFactory;
        _configuration = configuration;
        _cache = cache;
        _logger = logger;
        _sessionOptions = sessionOptions.Value;
    }

    public async Task<int> GetInactivityTimeoutMinutesAsync(CancellationToken cancellationToken = default)
    {
        if (_cache.TryGetValue(CacheKey, out int cached)
            && cached >= SessionSecurityRules.MinInactivityTimeoutMinutes)
        {
            return SessionSecurityRules.ClampTimeoutMinutes(cached);
        }

        try
        {
            var apiBase = JobsyPublicUrl.NormalizeBaseUrl(
                _configuration["ApiBaseUrl"],
                "http://localhost:5200/");
            var client = _httpClientFactory.CreateClient("JobsySessionSecurity");
            client.BaseAddress ??= new Uri(apiBase);
            client.Timeout = TimeSpan.FromSeconds(5);

            var dto = await client.GetFromJsonAsync<SessionSecurityResponse>(
                "api/settings/session-security",
                cancellationToken);
            var minutes = SessionTimeoutResolver.ResolveInactivityMinutes(
                _sessionOptions,
                dto?.InactivityTimeoutMinutes ?? 0);
            _cache.Set(CacheKey, minutes, CacheTtl);
            return minutes;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to load session inactivity timeout; using default {Minutes}m.",
                SessionSecurityRules.DefaultInactivityTimeoutMinutes);
            var fallback = SessionTimeoutResolver.ResolveInactivityMinutes(_sessionOptions, 0);
            _cache.Set(CacheKey, fallback, TimeSpan.FromSeconds(10));
            return fallback;
        }
    }

    private sealed record SessionSecurityResponse(int InactivityTimeoutMinutes);
}

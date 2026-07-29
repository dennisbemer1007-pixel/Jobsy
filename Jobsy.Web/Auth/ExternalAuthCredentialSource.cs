using System.Net.Http.Json;
using Jobsy.Core;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace Jobsy.Web.Auth;

public interface IExternalAuthCredentialSource
{
    Task<bool> IsEntraConfiguredAsync(CancellationToken cancellationToken = default);
    Task<bool> IsGoogleConfiguredAsync(CancellationToken cancellationToken = default);
    Task<ExternalOAuthCredentials?> GetEntraAsync(CancellationToken cancellationToken = default);
    Task<ExternalOAuthCredentials?> GetGoogleAsync(CancellationToken cancellationToken = default);
}

public sealed record ExternalOAuthCredentials(
    string ClientId,
    string ClientSecret,
    string? TenantId);

/// <summary>
/// Resolves Entra/Google credentials from appsettings, falling back to Admin → Integraties via API.
/// </summary>
public sealed class ExternalAuthCredentialSource : IExternalAuthCredentialSource
{
    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(2);

    private readonly IConfiguration _configuration;
    private readonly IOptions<AuthOptions> _authOptions;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IMemoryCache _cache;

    public ExternalAuthCredentialSource(
        IConfiguration configuration,
        IOptions<AuthOptions> authOptions,
        IHttpClientFactory httpClientFactory,
        IMemoryCache cache)
    {
        _configuration = configuration;
        _authOptions = authOptions;
        _httpClientFactory = httpClientFactory;
        _cache = cache;
    }

    public async Task<bool> IsEntraConfiguredAsync(CancellationToken cancellationToken = default)
        => (await GetEntraAsync(cancellationToken)) is not null;

    public async Task<bool> IsGoogleConfiguredAsync(CancellationToken cancellationToken = default)
        => (await GetGoogleAsync(cancellationToken)) is not null;

    public Task<ExternalOAuthCredentials?> GetEntraAsync(CancellationToken cancellationToken = default)
    {
        var local = _authOptions.Value.Entra;
        if (local.IsConfigured)
        {
            return Task.FromResult<ExternalOAuthCredentials?>(new ExternalOAuthCredentials(
                local.ClientId!.Trim(),
                local.ClientSecret!.Trim(),
                string.IsNullOrWhiteSpace(local.TenantId) ? "common" : local.TenantId.Trim()));
        }

        return GetFromIntegrationsAsync("entra", cancellationToken);
    }

    public Task<ExternalOAuthCredentials?> GetGoogleAsync(CancellationToken cancellationToken = default)
    {
        var local = _authOptions.Value.Google;
        if (local.IsConfigured)
        {
            return Task.FromResult<ExternalOAuthCredentials?>(new ExternalOAuthCredentials(
                local.ClientId!.Trim(),
                local.ClientSecret!.Trim(),
                null));
        }

        return GetFromIntegrationsAsync("google", cancellationToken);
    }

    private async Task<ExternalOAuthCredentials?> GetFromIntegrationsAsync(
        string provider,
        CancellationToken cancellationToken)
    {
        var cacheKey = $"external-oauth:{provider}";
        if (_cache.TryGetValue(cacheKey, out ExternalOAuthCredentials? cached))
        {
            return cached;
        }

        try
        {
            var apiBase = JobsyPublicUrl.NormalizeBaseUrl(
                _configuration["ApiBaseUrl"],
                "http://localhost:5200/");
            var client = _httpClientFactory.CreateClient("JobsyAuthProvision");
            client.BaseAddress = new Uri(apiBase);
            client.Timeout = TimeSpan.FromSeconds(8);

            using var request = new HttpRequestMessage(
                HttpMethod.Get,
                $"api/auth/external-provider-config/{provider}");
            var secret = _configuration["JobsyAuth:ExternalProvisionSecret"];
            if (!string.IsNullOrWhiteSpace(secret))
            {
                request.Headers.TryAddWithoutValidation("X-Jobsy-Provision-Secret", secret);
            }

            using var response = await client.SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                _cache.Set(cacheKey, (ExternalOAuthCredentials?)null, TimeSpan.FromSeconds(30));
                return null;
            }

            var body = await response.Content.ReadFromJsonAsync<ConfigDto>(cancellationToken);
            if (body is null
                || string.IsNullOrWhiteSpace(body.ClientId)
                || string.IsNullOrWhiteSpace(body.ClientSecret))
            {
                _cache.Set(cacheKey, (ExternalOAuthCredentials?)null, TimeSpan.FromSeconds(30));
                return null;
            }

            var creds = new ExternalOAuthCredentials(
                body.ClientId.Trim(),
                body.ClientSecret.Trim(),
                string.IsNullOrWhiteSpace(body.TenantId) ? null : body.TenantId.Trim());
            _cache.Set(cacheKey, creds, CacheDuration);
            return creds;
        }
        catch
        {
            _cache.Set(cacheKey, (ExternalOAuthCredentials?)null, TimeSpan.FromSeconds(30));
            return null;
        }
    }

    private sealed class ConfigDto
    {
        public string ClientId { get; set; } = string.Empty;
        public string ClientSecret { get; set; } = string.Empty;
        public string? TenantId { get; set; }
    }
}

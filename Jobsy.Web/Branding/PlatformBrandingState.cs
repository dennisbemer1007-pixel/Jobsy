using System.Net.Http.Json;
using Jobsy.Web.Services;

namespace Jobsy.Web.Branding;

/// <summary>
/// Loads the platform slogan from Bedrijfsgegevens for the site header.
/// Regional CNAME slogans still win when they are set.
/// Uses the anonymous <c>JobsyPublic</c> client so the header never shares
/// the circuit <see cref="JobsyApiClient"/> (not thread-safe, auth-bound).
/// </summary>
public sealed class PlatformBrandingState
{
    public const string HttpClientName = "JobsyPublic";

    private readonly IHttpClientFactory _httpFactory;
    private Task? _initializeTask;

    public PlatformBrandingState(IHttpClientFactory httpFactory)
    {
        _httpFactory = httpFactory;
    }

    public string? CompanyName { get; private set; }

    public string? Slogan { get; private set; }

    public event Action? Changed;

    public Task EnsureInitializedAsync()
        => _initializeTask ??= LoadAsync();

    public Task RefreshAsync()
    {
        _initializeTask = LoadAsync();
        return _initializeTask;
    }

    public string ResolveTagline(string? regionSlogan, string fallback)
        => ResolveTagline(regionSlogan, Slogan, fallback);

    public static string ResolveTagline(string? regionSlogan, string? companySlogan, string fallback)
    {
        if (!string.IsNullOrWhiteSpace(regionSlogan))
        {
            return regionSlogan.Trim();
        }

        if (!string.IsNullOrWhiteSpace(companySlogan))
        {
            return companySlogan.Trim();
        }

        return fallback;
    }

    private async Task LoadAsync()
    {
        try
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(2));
            var http = _httpFactory.CreateClient(HttpClientName);
            var branding = await http.GetFromJsonAsync<SiteBrandingItem>("api/site/branding", cts.Token);
            CompanyName = string.IsNullOrWhiteSpace(branding?.CompanyName) ? null : branding.CompanyName.Trim();
            Slogan = string.IsNullOrWhiteSpace(branding?.Slogan) ? null : branding.Slogan.Trim();
        }
        catch
        {
            // Header must render even if the public branding call fails.
        }

        Changed?.Invoke();
    }
}

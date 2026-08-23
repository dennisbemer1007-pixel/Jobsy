using Jobsy.Web.Services;

namespace Jobsy.Web.Branding;

/// <summary>
/// Loads the platform slogan from Bedrijfsgegevens for the site header.
/// Regional CNAME slogans still win when they are set.
/// </summary>
public sealed class PlatformBrandingState
{
    private readonly JobsyApiClient _api;
    private Task? _initializeTask;

    public PlatformBrandingState(JobsyApiClient api)
    {
        _api = api;
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
            var branding = await _api.GetPublicBrandingAsync(cts.Token);
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

using System.Net;

namespace Jobsy.Tests;

/// <summary>
/// Verifies acceptatie serves real map-popup WebP bytes (not category colour tiles).
/// Skips when acceptatie is unreachable (local dev without network).
/// </summary>
public sealed class MapPopupPhotoAcceptatieLiveTests
{
    private static readonly Guid FlexSeedVacancyId = Guid.Parse("a3000000-0000-4000-8000-000000000002");

    [Theory]
    [InlineData("desktop")]
    [InlineData("mobile")]
    public async Task Live_acc_map_popup_photo_is_real_webp_not_category_tile(string variant)
    {
        var baseUrl = ResolveLiveBaseUrl();
        if (baseUrl is null || !await IsReachableAsync(baseUrl))
        {
            return;
        }

        using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
        var url = $"{baseUrl}/api/vacancies/{FlexSeedVacancyId:D}/map-popup-photo?variant={variant}";
        using var response = await http.GetAsync(url);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        response.Headers.TryGetValues("X-Jobsy-Map-Popup-Photo", out var sourceHeaders);
        var source = sourceHeaders?.FirstOrDefault();
        Assert.Equal("photo", source);

        var bytes = await response.Content.ReadAsByteArrayAsync();
        Assert.True(bytes.Length > 3000,
            $"Expected multikbyte WebP from public flex.webp chain, got {bytes.Length} bytes (variant={variant}).");

        var cache = response.Headers.CacheControl?.ToString() ?? "";
        Assert.Contains("immutable", cache, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Live_acc_flex_webp_static_asset_is_available()
    {
        var baseUrl = ResolveLiveBaseUrl();
        if (baseUrl is null || !await IsReachableAsync(baseUrl))
        {
            return;
        }

        using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
        using var response = await http.GetAsync(baseUrl + "/images/vacancies/flex.webp");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var bytes = await response.Content.ReadAsByteArrayAsync();
        Assert.True(bytes.Length > 30_000, "flex.webp on acceptatie should be a full photo asset.");
    }

    /// <summary>
    /// Set <c>JOBSY_E2E_BASE_URL=https://acceptatie.lobsy.nl</c> to assert live map-popup photos after deploy.
    /// </summary>
    private static string? ResolveLiveBaseUrl()
    {
        var fromEnv = (Environment.GetEnvironmentVariable("JOBSY_E2E_BASE_URL") ?? "").Trim().TrimEnd('/');
        if (string.IsNullOrWhiteSpace(fromEnv))
        {
            return null;
        }

        return fromEnv;
    }

    private static async Task<bool> IsReachableAsync(string baseUrl)
    {
        try
        {
            using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(12) };
            using var res = await http.GetAsync(baseUrl + "/health");
            return res.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }
}

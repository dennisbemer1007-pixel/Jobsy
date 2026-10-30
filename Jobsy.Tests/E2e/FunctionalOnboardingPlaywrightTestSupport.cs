using Microsoft.Playwright;

namespace Jobsy.Tests.E2e;

internal static class FunctionalOnboardingPlaywrightTestSupport
{
    internal static BrowserNewContextOptions CreateContextOptions(string baseUrl, int width, int height, bool mobile)
        => new()
        {
            ViewportSize = new() { Width = width, Height = height },
            IsMobile = mobile,
            HasTouch = mobile,
            Locale = "nl-NL",
            Geolocation = new() { Latitude = 52.066f, Longitude = 4.235f, Accuracy = 25 },
            Permissions = ["geolocation"],
            IgnoreHTTPSErrors = true,
            BaseURL = baseUrl,
        };

    internal static async Task<(bool Run, string BaseUrl)> TryBeginAsync()
    {
        var baseUrl = FunctionalE2eGate.TryBaseUrl() ?? "";
        if (FunctionalE2eGate.IsRequired)
        {
            baseUrl = await FunctionalE2eGate.RequireReachableBaseUrlAsync();
            return (true, baseUrl);
        }

        if (string.IsNullOrWhiteSpace(baseUrl) || !await SmokeReachability.IsReachableAsync(baseUrl))
        {
            return (false, baseUrl);
        }

        return (true, baseUrl);
    }
}

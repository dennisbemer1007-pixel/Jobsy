using System.Net;
using System.Text;
using System.Text.Json;
using Jobsy.Core.Rules;
using Microsoft.Playwright;

namespace Jobsy.Tests;

[Collection("PlaywrightSmoke")]
public sealed class OnboardingCompletionStickyPlaywrightTests : IAsyncLifetime
{
    private IPlaywright? _playwright;
    private IBrowser? _browser;

    public async ValueTask InitializeAsync()
    {
        var exit = Microsoft.Playwright.Program.Main(["install", "chromium"]);
        Assert.Equal(0, exit);
        _playwright = await Playwright.CreateAsync();
        _browser = await _playwright.Chromium.LaunchAsync(new() { Headless = true });
    }

    public async ValueTask DisposeAsync()
    {
        if (_browser is not null)
        {
            await _browser.DisposeAsync();
        }

        _playwright?.Dispose();
    }

    [Theory]
    [InlineData(false, 1280, 720)]
    [InlineData(true, 390, 844)]
    public async Task Completed_revisit_step2_save_then_discovery_overview(bool mobile, int width, int height)
    {
        await using var factory = new OnboardingClientStepsPlaywrightSupport.ClientStepsWebFactory();
        factory.WelcomeDismissed = true;
        factory.FinishReached = true;
        factory.OnboardingComplete = true;
        factory.CandidatePassportEnabled = true;
        factory.SavedStep = 10;
        factory.StoredProfile.TestAiConsentAt = DateTime.UtcNow;
        factory.StoredProfile.Preferences ??= new();
        factory.StoredProfile.AvailableFromDate = new DateOnly(2027, 4, 20);
        factory.StoredProfile.Preferences.MaxTravelMinutes = 30;
        factory.DiscoveryOverviewCompetencyAnswered = OnboardingWizardCatalog.MiniLevelCount;
        factory.DiscoveryOverviewCareerAnswered = OnboardingWizardCatalog.MiniLevelCount;
        factory.DiscoveryOverviewCultureAnswered = OnboardingWizardCatalog.MiniLevelCount;
        factory.DiscoveryOverviewValuesAnswered = OnboardingWizardCatalog.MiniLevelCount;

        var root = factory.EnsureServerAddress();
        await using var context = await _browser!.NewContextAsync(new BrowserNewContextOptions
        {
            ViewportSize = new() { Width = width, Height = height },
            HasTouch = mobile,
            IsMobile = mobile,
            Locale = "nl-NL",
        });
        await PlaywrightCookieConsent.AcceptAsync(context);
        var page = await context.NewPageAsync();
        page.SetDefaultTimeout(120_000);

        await page.GotoAsync(root + "/__test/sign-in?return=" + Uri.EscapeDataString("/candidate/start?step=2"));
        await page.Locator("[data-ob-client-step=\"2\"]").WaitForAsync(new() { Timeout = 60_000 });

        var token = await page.Locator("input[name=\"__RequestVerificationToken\"]").First.GetAttributeAsync("value");
        Assert.False(string.IsNullOrWhiteSpace(token));

        var saveBody = JsonSerializer.Serialize(new
        {
            minHours = 8,
            maxHours = 16,
            immediate = true,
            presets = new[] { "school" },
        });
        var saveResponse = await page.APIRequest.PutAsync(
            root + "/api/onboarding-client/steps/2",
            new()
            {
                Headers = new Dictionary<string, string>
                {
                    ["Content-Type"] = "application/json",
                    ["RequestVerificationToken"] = token!,
                },
                Data = saveBody,
            });
        Assert.True(saveResponse.Ok, await saveResponse.TextAsync());

        factory.SavedStep = 2;

        var discoveryResponse = await page.GotoAsync(
            root + "/candidate/ontdekkingsreis",
            new() { WaitUntil = WaitUntilState.DOMContentLoaded });
        Assert.NotNull(discoveryResponse);
        await page.WaitForURLAsync(
            u => u.Contains("stap=overview", StringComparison.OrdinalIgnoreCase),
            new() { Timeout = 45_000 });
        Assert.DoesNotContain("/candidate/start", page.Url, StringComparison.OrdinalIgnoreCase);
        await page.Locator("[data-testid=\"discovery-overview-route\"]").WaitForAsync(new() { Timeout = 45_000 });
    }

    [Fact]
    public async Task Http_completed_revisit_step2_save_then_discovery_overview()
    {
        await using var factory = new OnboardingClientStepsPlaywrightSupport.ClientStepsWebFactory();
        factory.WelcomeDismissed = true;
        factory.FinishReached = true;
        factory.OnboardingComplete = true;
        factory.CandidatePassportEnabled = true;
        factory.SavedStep = 10;
        factory.StoredProfile.TestAiConsentAt = DateTime.UtcNow;
        factory.StoredProfile.Preferences ??= new();
        factory.StoredProfile.AvailableFromDate = new DateOnly(2027, 4, 20);
        factory.StoredProfile.Preferences.MaxTravelMinutes = 30;
        factory.DiscoveryOverviewCompetencyAnswered = OnboardingWizardCatalog.MiniLevelCount;
        factory.DiscoveryOverviewCareerAnswered = OnboardingWizardCatalog.MiniLevelCount;
        factory.DiscoveryOverviewCultureAnswered = OnboardingWizardCatalog.MiniLevelCount;
        factory.DiscoveryOverviewValuesAnswered = OnboardingWizardCatalog.MiniLevelCount;

        var client = factory.CreateClient(new() { AllowAutoRedirect = false, HandleCookies = true });
        _ = await client.GetAsync("/__test/sign-in?return=" + Uri.EscapeDataString("/candidate/start?step=2"));

        var getStep = await client.GetAsync("/candidate/start?step=2");
        var html = await getStep.Content.ReadAsStringAsync();
        var token = ExtractAntiforgery(html);
        Assert.False(string.IsNullOrWhiteSpace(token));

        var savePayload = JsonSerializer.Serialize(new
        {
            minHours = 8,
            maxHours = 16,
            immediate = true,
            presets = new[] { "school" },
        });
        using var saveRequest = new HttpRequestMessage(HttpMethod.Put, "/api/onboarding-client/steps/2");
        saveRequest.Headers.TryAddWithoutValidation("RequestVerificationToken", token);
        saveRequest.Content = new StringContent(savePayload, Encoding.UTF8, "application/json");
        var save = await client.SendAsync(saveRequest);
        Assert.Equal(HttpStatusCode.OK, save.StatusCode);

        var discovery = await client.GetAsync("/candidate/ontdekkingsreis");
        Assert.Equal(HttpStatusCode.SeeOther, discovery.StatusCode);
        var location = discovery.Headers.Location?.ToString() ?? "";
        Assert.Contains("stap=overview", location, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("/candidate/start", location, StringComparison.OrdinalIgnoreCase);
    }

    private static string? ExtractAntiforgery(string html)
    {
        const string marker = "name=\"__RequestVerificationToken\" value=\"";
        var i = html.IndexOf(marker, StringComparison.Ordinal);
        if (i < 0)
        {
            return null;
        }

        i += marker.Length;
        var j = html.IndexOf('"', i);
        return j < 0 ? null : html[i..j];
    }
}

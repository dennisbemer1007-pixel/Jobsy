using Bunit;
using Jobsy.Core.Rules;
using Jobsy.Web.Components.Candidate.Onboarding;
using Jobsy.Web.Localization;
using Jobsy.Web.Services;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;
using Microsoft.Playwright;
using System.Security.Claims;

namespace Jobsy.Tests;

/// <summary>Renders PersonalStep geolocation states and captures PNGs when JOBSY_CAPTURE_GEO=1.</summary>
public sealed class OnboardingGeolocationScreenshotTests : BunitContext
{
    public OnboardingGeolocationScreenshotTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddSingleton<AuthenticationStateProvider>(new FakeAuth());
        Services.AddSingleton(sp => new CultureState(
            sp.GetRequiredService<IJSRuntime>(),
            sp,
            sp.GetRequiredService<AuthenticationStateProvider>()));
        Services.AddSingleton<IGeocodingClient, FakeGeocoder>();
    }

    [Theory]
    [InlineData(1440, 900, false, "onboarding-geolocation-low-accuracy-desktop-1440.png")]
    [InlineData(390, 844, true, "onboarding-geolocation-precise-mobile-390.png")]
    public async Task Capture_geolocation_hint_states(int width, int height, bool precise, string fileName)
    {
        if ((Environment.GetEnvironmentVariable("JOBSY_CAPTURE_GEO") ?? "").Trim() != "1")
        {
            return;
        }

        var outDir = (Environment.GetEnvironmentVariable("JOBSY_GEO_ARTIFACTS") ?? "/cursor/stores/self/blockers").Trim();
        Directory.CreateDirectory(outDir);
        var staticBase = (Environment.GetEnvironmentVariable("PERSONAL_STEP_STATIC_BASE") ?? "http://127.0.0.1:5201").Trim().TrimEnd('/');

        var draft = new OnboardingProfileDraft { FirstName = "Geo", LastName = "Test" };
        var label = "Westmadeweg 120, 2553 EM Den Haag";
        var accuracy = precise ? 25d : 800d;
        CandidateHomeAddressLookup.ApplyGeolocationPrefill(draft, 52.066, 4.235, accuracy, label);

        var html = await RenderDocumentAsync(draft, staticBase);
        var htmlPath = Path.Combine(outDir, fileName.Replace(".png", ".html", StringComparison.Ordinal));
        await File.WriteAllTextAsync(htmlPath, html);

        Microsoft.Playwright.Program.Main(["install", "chromium"]);
        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new() { Headless = true });
        await using var page = await browser.NewPageAsync(new() { ViewportSize = new() { Width = width, Height = height } });
        await page.GotoAsync(new Uri(htmlPath).AbsoluteUri, new() { WaitUntil = WaitUntilState.NetworkIdle, Timeout = 60_000 });
        await page.ScreenshotAsync(new() { Path = Path.Combine(outDir, fileName), FullPage = false });
    }

    private async Task<string> RenderDocumentAsync(OnboardingProfileDraft draft, string staticBase)
    {
        var cut = Render<PersonalStep>(ps => ps
            .Add(p => p.Draft, draft)
            .Add(p => p.ShowValidation, false)
            .Add(p => p.UseDiscoveryPostcodeHint, true));
        await cut.InvokeAsync(() => Task.CompletedTask);
        return PersonalStepScreenshotExporter.BuildDocument(cut.Find(".ob-fields").InnerHtml, staticBase);
    }

    private sealed class FakeAuth : AuthenticationStateProvider
    {
        public override Task<AuthenticationState> GetAuthenticationStateAsync()
            => Task.FromResult(new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity())));
    }

    private sealed class FakeGeocoder : IGeocodingClient
    {
        public Task<IReadOnlyList<AddressSuggestion>> SuggestAsync(string query, CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<AddressSuggestion>>([]);

        public Task<string?> ReverseAsync(double latitude, double longitude, CancellationToken cancellationToken = default)
            => Task.FromResult<string?>("Westmadeweg 120, 2553 EM Den Haag");
    }
}

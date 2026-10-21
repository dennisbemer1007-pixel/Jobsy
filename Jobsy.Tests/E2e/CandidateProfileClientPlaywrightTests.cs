using Jobsy.Core.Features;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Playwright;

namespace Jobsy.Tests.E2e;

[Collection("PlaywrightSmoke")]
public sealed class CandidateProfileClientPlaywrightTests : IAsyncLifetime
{
    private IllustratedPagesWebFactory? _web;
    private IPlaywright? _playwright;
    private IBrowser? _browser;

    public async ValueTask InitializeAsync()
    {
        _ = Microsoft.Playwright.Program.Main(["install", "chromium"]);
        _playwright = await Playwright.CreateAsync();
        _browser = await _playwright.Chromium.LaunchAsync(new() { Headless = true });
        _web = new ProfileStaticWebFactory();
        _ = _web.ServerAddress;
    }

    public async ValueTask DisposeAsync()
    {
        if (_browser is not null)
        {
            await _browser.DisposeAsync();
        }

        _playwright?.Dispose();
        _web?.Dispose();
    }

    [Theory]
    [InlineData(PlaywrightViewportContexts.DesktopWidth, PlaywrightViewportContexts.DesktopHeight)]
    [InlineData(PlaywrightViewportContexts.MobileWidth, PlaywrightViewportContexts.MobileHeight)]
    public async Task Personal_section_types_client_side_until_one_post(int width, int height)
    {
        var baseUrl = _web!.ServerAddress.TrimEnd('/');
        await using var context = await _browser!.NewContextAsync(PlaywrightViewportContexts.ForSize(width, height));
        var page = await context.NewPageAsync();
        await PlaywrightCookieConsent.AcceptAsync(context);

        var posts = 0;
        await page.RouteAsync("**/candidate/profile/sections/**", route =>
        {
            if (route.Request.Method.Equals("POST", StringComparison.OrdinalIgnoreCase))
            {
                posts++;
            }

            return route.ContinueAsync();
        });

        var signIn = $"{baseUrl}/__test/sign-in?role=candidate&return={Uri.EscapeDataString("/candidate/profile?tab=profile&profileSection=personal")}";
        await page.GotoAsync(signIn, new() { WaitUntil = WaitUntilState.DOMContentLoaded });
        await page.GotoAsync($"{baseUrl}/candidate/profile?tab=profile&profileSection=personal", new() { WaitUntil = WaitUntilState.Load, Timeout = 90_000 });

        var firstName = page.Locator("[data-testid='profile-first-name']").First;
        await firstName.WaitForAsync(new() { Timeout = 15_000 });
        for (var i = 0; i < 7; i++)
        {
            await firstName.click();
            await firstName.press("End");
            await firstName.type("x", new() { Delay = 20 });
            await page.WaitForTimeoutAsync(40);
        }

        Assert.Equal(0, posts);

        posts = 0;
        await page.Locator("form[data-profile-section-form='personal'] button[type='submit']").ClickAsync();
        await page.WaitForURLAsync(u => u.Contains("profileSaved=1", StringComparison.OrdinalIgnoreCase), new() { Timeout = 30_000 });
        Assert.Equal(1, posts);
    }

    private sealed class ProfileStaticWebFactory : IllustratedPagesWebFactory
    {
        protected override void ConfigureWebHost(Microsoft.AspNetCore.Hosting.IWebHostBuilder builder)
        {
            base.ConfigureWebHost(builder);
            builder.ConfigureTestServices(services =>
            {
                services.RemoveAll<IFeatureFlags>();
                services.AddSingleton<IFeatureFlags>(new PassportOffFlags());
            });
        }

        private sealed class PassportOffFlags : IFeatureFlags
        {
            private readonly FeatureFlagSnapshot _snap = new(EmployersEnabled: true, CandidatePassportEnabled: false);

            public ValueTask<FeatureFlagSnapshot> GetAsync(CancellationToken cancellationToken = default)
                => ValueTask.FromResult(_snap);

            public ValueTask<bool> IsEnabledAsync(PlatformFeature feature, CancellationToken cancellationToken = default)
                => ValueTask.FromResult(_snap.IsEnabled(feature));

            public void Invalidate()
            {
            }
        }
    }
}

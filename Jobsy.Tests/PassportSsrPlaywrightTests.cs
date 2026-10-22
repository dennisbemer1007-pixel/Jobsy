using Jobsy.Tests.E2e;
using Microsoft.Playwright;

namespace Jobsy.Tests;

[Collection("PlaywrightSmoke")]
public sealed class PassportSsrPlaywrightTests : IAsyncLifetime
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
    [InlineData(false)]
    [InlineData(true)]
    public async Task Passport_ssr_tabs_save_and_kompas_link(bool mobile)
    {
        await using var factory = new OnboardingClientStepsPlaywrightSupport.ClientStepsWebFactory();
        factory.WelcomeDismissed = true;
        factory.FinishReached = true;
        factory.CandidatePassportEnabled = true;
        factory.StoredProfile.FirstName = "Paspoort";
        factory.StoredProfile.LastName = "Tester";
        factory.StoredProfile.PhoneNumber = "0612345678";
        var root = factory.EnsureServerAddress();
        await using var context = await _browser!.NewContextAsync(OnboardingClientStepsPlaywrightSupport.ContextOptions(mobile));
        await PlaywrightCookieConsent.AcceptAsync(context);
        var page = await context.NewPageAsync();
        page.SetDefaultTimeout(90_000);

        if (!mobile)
        {
            await AssertOnboardingAccDesktopFixesAsync(page, root);
        }

        await page.GotoAsync(root + "/__test/sign-in?return=" + Uri.EscapeDataString("/candidate/paspoort?tab=dna"));
        await PlaywrightCookieConsent.AcceptOnPageAsync(page);
        await page.GetByTestId("passport-page-ssr").WaitForAsync(new() { Timeout = 60_000 });

        foreach (var tab in new[] { "dna", "tests", "career", "proof", "data" })
        {
            await page.Locator($"a.passport-tab[href*=\"tab={tab}\"]").First.ClickAsync();
            await page.WaitForURLAsync($"**/candidate/paspoort?tab={tab}**");
            await page.GetByTestId("passport-page-ssr").WaitForAsync();
        }

        await page.Locator("[data-testid=passport-data-phone]").WaitForAsync();
        await page.Locator("[data-testid=passport-data-first-name]").FillAsync("Paspoort");
        await page.Locator("[data-testid=passport-data-last-name]").FillAsync("Tester");
        var newPhone = mobile ? "0687654321" : "0698765432";
        await page.Locator("[data-testid=passport-data-phone]").FillAsync(newPhone);
        await page.GetByTestId("passport-data-save").ClickAsync();
        await page.WaitForURLAsync(u => u.Contains("/candidate/paspoort", StringComparison.OrdinalIgnoreCase)
                                        && u.Contains("tab=data", StringComparison.OrdinalIgnoreCase)
                                        && (u.Contains("saved=1", StringComparison.OrdinalIgnoreCase)
                                            || u.Contains("error=", StringComparison.OrdinalIgnoreCase)),
            new() { Timeout = 30_000 });
        Assert.True(
            page.Url.Contains("saved=1", StringComparison.OrdinalIgnoreCase),
            "Expected saved=1 after POST; got: " + page.Url);

        await page.ReloadAsync();
        await page.GetByTestId("passport-page-ssr").WaitForAsync();
        await Assertions.Expect(page.Locator("[data-testid=passport-data-phone]")).ToHaveValueAsync(newPhone);

        await page.Locator("a[href=\"/candidate/kompas\"]").First.ClickAsync();
        await page.WaitForURLAsync("**/candidate/kompas**");
        await page.GetByTestId("candidate-kompas-ssr").WaitForAsync(new() { Timeout = 60_000 });
    }

    private static async Task AssertOnboardingAccDesktopFixesAsync(IPage page, string root)
    {
        await page.GotoAsync(root + "/__test/sign-in?return=" + Uri.EscapeDataString("/candidate/start"));
        await PlaywrightCookieConsent.AcceptOnPageAsync(page);
        await page.WaitForURLAsync("**/candidate/start/kompas**", new() { Timeout = 30_000 });
        Assert.DoesNotContain("ontdekkingsreis", page.Url, StringComparison.OrdinalIgnoreCase);

        await page.GotoAsync(root + "/__test/sign-in?return=" + Uri.EscapeDataString("/candidate/start?step=6"));
        await PlaywrightCookieConsent.AcceptOnPageAsync(page);
        await page.WaitForURLAsync("**/candidate/start?step=6**");
        await page.Locator("form[data-ob-ssr-step=\"6\"]").WaitForAsync(new() { Timeout = 60_000 });
        var heroSrc = await page.GetByTestId("ob-rustig-hero-desk").GetAttributeAsync("src") ?? "";
        Assert.Contains("step-06-droom-professions", heroSrc, StringComparison.OrdinalIgnoreCase);
        var heroOk = await page.GetByTestId("ob-rustig-hero-desk").EvaluateAsync<bool>(
            "img => img.complete && img.naturalWidth > 0");
        Assert.True(heroOk, "Step 6 desktop hero should load.");

        await page.GotoAsync(root + "/__test/sign-in?return=" + Uri.EscapeDataString("/candidate/start?step=4"));
        await PlaywrightCookieConsent.AcceptOnPageAsync(page);
        await page.WaitForURLAsync("**/candidate/start?step=4**");
        await page.Locator("form[data-ob-ssr-step=\"4\"]").WaitForAsync(new() { Timeout = 60_000 });
        var layout = await page.EvaluateAsync<(double SidebarRight, double HeroLeft)>(@"() => {
            const sidebar = document.querySelector('.ob-rustig__sidebar');
            const hero = document.querySelector('[data-testid=ob-rustig-hero]');
            if (!sidebar || !hero) return { sidebarRight: 0, heroLeft: 0 };
            const sb = sidebar.getBoundingClientRect();
            const hb = hero.getBoundingClientRect();
            return { sidebarRight: sb.right, heroLeft: hb.left };
        }");
        Assert.True(
            layout.HeroLeft >= layout.SidebarRight - 2,
            $"Step 4 hero should stay in main column (heroLeft={layout.HeroLeft}, sidebarRight={layout.SidebarRight}).");
    }
}

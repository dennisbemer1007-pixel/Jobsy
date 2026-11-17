using Jobsy.Tests.E2e;
using Microsoft.Playwright;

namespace Jobsy.Tests;

/// <summary>Acceptatie retest guards after PR #1477 (kompas hub CTAs, map copy, signup #email, help icon).</summary>
[Collection("PlaywrightSmoke")]
public sealed class AcceptatieRetestPost1477PlaywrightTests : IAsyncLifetime
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
    [InlineData(390)]
    [InlineData(375)]
    public async Task Kompas_hub_ctas_share_left_edge_on_mobile(int width)
    {
        await using var factory = CreateCompleteCandidateFactory();
        var root = factory.EnsureServerAddress();
        await using var context = await _browser!.NewContextAsync(ViewportContext(width, mobile: true));
        await PlaywrightCookieConsent.AcceptAsync(context);
        var page = await context.NewPageAsync();

        await SignInAsync(page, root, "/candidate/kompas");
        await page.GotoAsync(root + "/candidate/kompas");
        await page.Locator(".kompas__ssr-actions").WaitForAsync(new() { Timeout = 90_000 });

        var aligned = await page.EvaluateAsync<bool>(
            """
            () => {
              const row = document.querySelector('.kompas__ssr-actions');
              const buttons = row ? [...row.querySelectorAll('.ob-rustig__btn')] : [];
              if (buttons.length < 2) return false;
              const lefts = buttons.map(b => Math.round(b.getBoundingClientRect().left));
              const widths = buttons.map(b => Math.round(b.getBoundingClientRect().width));
              const sameLeft = Math.max(...lefts) - Math.min(...lefts) <= 2;
              const sameWidth = Math.max(...widths) - Math.min(...widths) <= 2;
              const gaps = [];
              for (let i = 1; i < buttons.length; i++) {
                gaps.push(Math.round(buttons[i].getBoundingClientRect().top - buttons[i - 1].getBoundingClientRect().bottom));
              }
              const evenGap = gaps.length === 0 || (Math.max(...gaps) - Math.min(...gaps) <= 3 && gaps.every(g => g >= 0));
              return sameLeft && sameWidth && evenGap;
            }
            """);
        Assert.True(aligned, $"Kompas hub CTAs must align at {width}px");
    }

    [Fact]
    public async Task Kompas_hub_banenkaart_card_has_no_literal_placeholder()
    {
        await using var factory = CreateCompleteCandidateFactory();
        factory.StoredProfile.Preferences ??= new();
        factory.StoredProfile.Preferences.HomeAddress = string.Empty;
        var root = factory.EnsureServerAddress();
        await using var context = await _browser!.NewContextAsync(ViewportContext(1280, mobile: false));
        await PlaywrightCookieConsent.AcceptAsync(context);
        var page = await context.NewPageAsync();

        await SignInAsync(page, root, "/candidate/kompas");
        await page.GotoAsync(root + "/candidate/kompas");
        await page.Locator("[data-testid=onboarding-finale-now-cards]").WaitForAsync(new() { Timeout = 90_000 });

        var copy = await page.Locator("[data-testid=onboarding-finale-now-cards]").InnerTextAsync();
        Assert.DoesNotContain("{0}", copy, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Account_maken_email_label_visible_below_sticky_header_at_1280()
    {
        await using var factory = new IllustratedPagesWebFactory();
        var root = factory.EnsureServerAddress();
        await using var context = await _browser!.NewContextAsync(new()
        {
            ViewportSize = new() { Width = 1280, Height = 900 },
            IgnoreHTTPSErrors = true,
            Locale = "nl-NL",
        });
        await PlaywrightCookieConsent.AcceptAsync(context);
        var page = await context.NewPageAsync();

        await page.GotoAsync(root + "/account-maken#email", new() { WaitUntil = WaitUntilState.DOMContentLoaded });
        await PlaywrightCookieConsent.AcceptOnPageAsync(page);
        await page.Locator("#email").WaitForAsync(new() { Timeout = 15_000 });

        var labelVisible = await page.EvaluateAsync<bool>(
            """
            () => {
              const input = document.getElementById('email');
              const label = input ? input.closest('label') : null;
              const header = document.querySelector('.pub-header');
              if (!input || !label || !header) return false;
              const hr = header.getBoundingClientRect();
              const lr = label.getBoundingClientRect();
              return lr.top >= hr.bottom - 1 && lr.height > 4 && lr.width > 4;
            }
            """);
        Assert.True(labelVisible, "E-mail label must sit fully below the sticky public header at 1280px");
    }

    [Theory]
    [InlineData("/candidate/kompas", ".app-header .page-help__btn")]
    [InlineData("/candidate/start/kompas", ".admin-topbar--candidate-onboarding .page-help__btn")]
    public async Task Help_button_shows_question_mark(string path, string selector)
    {
        await using var factory = CreateCompleteCandidateFactory();
        factory.FinishReached = true;
        var root = factory.EnsureServerAddress();
        await using var context = await _browser!.NewContextAsync(ViewportContext(1280, mobile: false));
        await PlaywrightCookieConsent.AcceptAsync(context);
        var page = await context.NewPageAsync();

        if (path.StartsWith("/candidate/start", StringComparison.Ordinal))
        {
            await page.GotoAsync(root + "/__test/sign-in?return=" + Uri.EscapeDataString(path));
            await page.Locator("[data-ob-rustig-screen=\"kompas\"]").WaitForAsync(new() { Timeout = 90_000 });
        }
        else
        {
            await SignInAsync(page, root, path);
            await page.GotoAsync(root + path);
            await page.Locator(selector).First.WaitForAsync(new() { Timeout = 90_000 });
        }

        var helpOk = await page.EvaluateAsync<bool>(
            """
            (sel) => {
              const btn = document.querySelector(sel);
              if (!btn) return false;
              const text = (btn.textContent || '').trim();
              const aria = (btn.getAttribute('aria-label') || '').trim();
              const showsQuestion = text === '?' || getComputedStyle(btn, '::before').content.replaceAll('"', '') === '?';
              const dutchAria = aria.toLowerCase().includes('info over deze pagina')
                || aria.toLowerCase().includes('about this page');
              return showsQuestion && aria.length > 8 && dutchAria;
            }
            """,
            selector);
        Assert.True(helpOk, $"Help control at {path} must show ? with a descriptive aria-label");
    }

    private static OnboardingClientStepsPlaywrightSupport.ClientStepsWebFactory CreateCompleteCandidateFactory()
    {
        var factory = new OnboardingClientStepsPlaywrightSupport.ClientStepsWebFactory
        {
            WelcomeDismissed = true,
            FinishReached = true,
            OnboardingComplete = true,
            CandidatePassportEnabled = true,
            SavedStep = 10,
        };
        factory.StoredProfile.PhoneNumber = "+31612345678";
        factory.StoredProfile.Preferences ??= new();
        factory.StoredProfile.Preferences.HomeAddress = "Zaandam";
        factory.DiscoveryOverviewValuesAnswered = Jobsy.Core.Rules.OnboardingWizardCatalog.MiniLevelCount;
        return factory;
    }

    private static async Task SignInAsync(IPage page, string root, string returnPath)
    {
        await page.GotoAsync(root + "/__test/sign-in?return=" + Uri.EscapeDataString(returnPath));
        await page.WaitForLoadStateAsync(LoadState.DOMContentLoaded);
    }

    private static BrowserNewContextOptions ViewportContext(int width, bool mobile)
        => new()
        {
            ViewportSize = new() { Width = width, Height = mobile ? 844 : 900 },
            IsMobile = mobile,
            HasTouch = mobile,
            IgnoreHTTPSErrors = true,
            Locale = "nl-NL",
        };
}

using Jobsy.Tests.E2e;
using Microsoft.Playwright;

namespace Jobsy.Tests;

/// <summary>
/// Post–PR #1476 acceptatie retest guards (375/390/1280/1440 + signup + banenkaart copy).
/// </summary>
[Collection("PlaywrightSmoke")]
public sealed class AcceptatieRetestPost1476PlaywrightTests : IAsyncLifetime
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

    public static TheoryData<int> MobileWidths => new() { 375, 390 };

    public static TheoryData<int, bool> KompasHubWidths => new()
    {
        { 375, true },
        { 390, true },
        { 1280, false },
        { 1440, false },
    };

    [Theory]
    [MemberData(nameof(KompasHubWidths))]
    public async Task Kompas_hub_header_brand_and_help_visible(int width, bool mobile)
    {
        await using var factory = CreateCompleteCandidateFactory();
        var root = factory.EnsureServerAddress();
        await using var context = await _browser!.NewContextAsync(ViewportContext(width, mobile));
        await PlaywrightCookieConsent.AcceptAsync(context);
        var page = await context.NewPageAsync();
        page.SetDefaultTimeout(120_000);

        await SignInAsync(page, root, "/candidate/kompas");
        await page.GotoAsync(root + "/candidate/kompas", new() { WaitUntil = WaitUntilState.DOMContentLoaded });
        await PlaywrightCookieConsent.AcceptOnPageAsync(page);
        await page.Locator("[data-testid=candidate-kompas-ssr]").WaitForAsync(new() { Timeout = 90_000 });

        var headerOk = await page.EvaluateAsync<bool>(
            """
            (vpW) => {
              const name = document.querySelector('.app-header .brand__name');
              const helpBtn = document.querySelector('.app-header .page-help__btn');
              if (!name || !helpBtn) return false;
              const nr = name.getBoundingClientRect();
              const hr = helpBtn.getBoundingClientRect();
              const nameVisible = nr.width > 8 && nr.height > 8 && nr.right > 0 && nr.left < vpW;
              const helpVisible = hr.width >= 40 && hr.height >= 40
                && hr.left >= -1 && hr.right <= vpW + 1
                && hr.top >= -1 && hr.bottom <= window.innerHeight + 1;
              return nameVisible && helpVisible;
            }
            """,
            width);
        Assert.True(headerOk, $"Kompas hub header brand + ? must be visible at {width}px");
    }

    [Theory]
    [MemberData(nameof(KompasHubWidths))]
    public async Task Kompas_hub_ctas_align_horizontally_on_desktop(int width, bool mobile)
    {
        await using var factory = CreateCompleteCandidateFactory();
        var root = factory.EnsureServerAddress();
        await using var context = await _browser!.NewContextAsync(ViewportContext(width, mobile));
        await PlaywrightCookieConsent.AcceptAsync(context);
        var page = await context.NewPageAsync();

        await SignInAsync(page, root, "/candidate/kompas");
        await page.GotoAsync(root + "/candidate/kompas");
        await page.Locator(".kompas__ssr-actions").WaitForAsync(new() { Timeout = 90_000 });

        var ctaOk = await page.EvaluateAsync<bool>(
            """
            (mobile) => {
              const row = document.querySelector('.kompas__ssr-actions');
              const buttons = row ? [...row.querySelectorAll('.ob-rustig__btn')] : [];
              if (buttons.length < 2) return false;
              if (mobile) return true;
              const tops = buttons.map(b => Math.round(b.getBoundingClientRect().top));
              const sameRow = Math.max(...tops) - Math.min(...tops) <= 8;
              const rowBox = row.getBoundingClientRect();
              const centered = Math.abs((rowBox.left + rowBox.right) / 2 - window.innerWidth / 2) < 80;
              return sameRow && centered;
            }
            """,
            mobile);
        Assert.True(ctaOk, $"Kompas CTA row layout at {width}px");
    }

    [Theory]
    [MemberData(nameof(KompasHubWidths))]
    public async Task Kompas_hub_cards_do_not_overflow_at_desktop(int width, bool mobile)
    {
        if (mobile)
        {
            return;
        }

        await using var factory = CreateCompleteCandidateFactory();
        var root = factory.EnsureServerAddress();
        await using var context = await _browser!.NewContextAsync(ViewportContext(width, mobile));
        await PlaywrightCookieConsent.AcceptAsync(context);
        var page = await context.NewPageAsync();

        await SignInAsync(page, root, "/candidate/kompas");
        await page.GotoAsync(root + "/candidate/kompas");
        await page.Locator(".ob-rustig__kompas-test-card").First.WaitForAsync(new() { Timeout = 90_000 });

        var ok = await page.EvaluateAsync<bool>(
            """
            () => {
              const cards = [...document.querySelectorAll('.kompas-page-wrap .ob-rustig__kompas-test-card, .kompas-page-wrap .ob-rustig__kompas-dir-card')];
              if (!cards.length) return true;
              return cards.every(card => {
                const cardRect = card.getBoundingClientRect();
                const texts = [...card.querySelectorAll('h3, p')];
                return texts.every(t => {
                  const tr = t.getBoundingClientRect();
                  return tr.right <= cardRect.right + 2 && tr.left >= cardRect.left - 2;
                });
              });
            }
            """);
        Assert.True(ok, $"Kompas card text must stay inside cards at {width}px");
    }

    [Theory]
    [MemberData(nameof(MobileWidths))]
    public async Task Banenkaart_kaart_toggle_in_viewport(int width)
    {
        await using var factory = CreateCompleteCandidateFactory();
        var root = factory.EnsureServerAddress();
        await using var context = await _browser!.NewContextAsync(ViewportContext(width, mobile: true));
        await PlaywrightCookieConsent.AcceptAsync(context);
        var page = await context.NewPageAsync();

        await SignInAsync(page, root, "/banenkaart");
        await page.GotoAsync(root + "/banenkaart");
        await page.Locator("[data-testid=kb-view-kaart]").First.WaitForAsync(new() { Timeout = 90_000 });

        var toggleOk = await page.EvaluateAsync<bool>(
            """
            (vpW) => {
              const btn = document.querySelector('[data-testid=kb-view-kaart]');
              if (!btn) return false;
              const r = btn.getBoundingClientRect();
              return r.left >= -1 && r.right <= vpW + 1 && r.width >= 40;
            }
            """,
            width);
        Assert.True(toggleOk, $"Kaart toggle on-screen at {width}px");
    }

    [Fact]
    public async Task Account_maken_van_ontdek_hash_email_reveals_form()
    {
        await using var factory = new IllustratedPagesWebFactory();
        var root = factory.EnsureServerAddress();
        await using var context = await _browser!.NewContextAsync(ViewportContext(390, mobile: true));
        await PlaywrightCookieConsent.AcceptAsync(context);
        var page = await context.NewPageAsync();

        await page.GotoAsync(root + "/account-maken?van=ontdek#email", new() { WaitUntil = WaitUntilState.DOMContentLoaded });
        await PlaywrightCookieConsent.AcceptOnPageAsync(page);

        await page.Locator("#email").WaitForAsync(new() { Timeout = 15_000 });
        var visible = await page.Locator("#email").IsVisibleAsync();
        Assert.True(visible);

        var providersHidden = await page.EvaluateAsync<bool>(
            """
            () => {
              const p = document.querySelector('.pub-signup__providers');
              return !p || p.hasAttribute('hidden') || getComputedStyle(p).display === 'none';
            }
            """);
        Assert.True(providersHidden, "OAuth provider row should be hidden when #email is active");
    }

    [Fact]
    public async Task Job_search_activity_plural_guard_unit()
    {
        Assert.Equal("1 activiteit", Jobsy.Core.Rules.JobSearchActivityPeriodRules.FormatActiviteitCountNl(1));
        Assert.Equal("2 activiteiten", Jobsy.Core.Rules.JobSearchActivityPeriodRules.FormatActiviteitCountNl(2));
    }

    [Theory]
    [MemberData(nameof(KompasHubWidths))]
    public async Task Onboarding_start_kompas_help_icon_visible(int width, bool mobile)
    {
        await using var factory = CreateCompleteCandidateFactory();
        factory.FinishReached = true;
        var root = factory.EnsureServerAddress();
        await using var context = await _browser!.NewContextAsync(ViewportContext(width, mobile));
        await PlaywrightCookieConsent.AcceptAsync(context);
        var page = await context.NewPageAsync();

        await page.GotoAsync(root + "/__test/sign-in?return=" + Uri.EscapeDataString("/candidate/start/kompas"));
        await page.Locator("[data-ob-rustig-screen=\"kompas\"]").WaitForAsync(new() { Timeout = 90_000 });

        var headerIconOk = await page.EvaluateAsync<bool>(
            """
            (vpW) => {
              const helpBtn = document.querySelector('.admin-topbar--candidate-onboarding .page-help__btn');
              const brand = document.querySelector('.admin-topbar--candidate-onboarding .admin-topbar__brand-text');
              if (!helpBtn) return false;
              const hr = helpBtn.getBoundingClientRect();
              const helpOk = hr.width >= 40 && hr.left >= 0 && hr.right <= vpW + 1;
              if (!brand) return helpOk;
              const br = brand.getBoundingClientRect();
              const brandOk = br.width > 8 && br.left >= 0 && br.right <= vpW;
              return helpOk && brandOk;
            }
            """,
            width);
        Assert.True(headerIconOk, $"Onboarding kompas header at {width}px");
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
using Jobsy.Tests.E2e;
using Microsoft.Playwright;

namespace Jobsy.Tests;

/// <summary>
/// Mobile 390px layout: full-width content, no horizontal overflow, kompas coach bubble clearance.
/// </summary>
[Collection("PlaywrightSmoke")]
public sealed class CandidateMobileLayout390PlaywrightTests : IAsyncLifetime
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

    public static TheoryData<string, string, string> FullWidthRoutes => new()
    {
        { "/candidate/kompas", "kompas-hub", ".kompas-page-wrap .kompas-hub-layout__main, .kompas-page-wrap .kompas--ssr-hub" },
        { "/candidate/ontdekkingsreis?stap=overview", "ontdekkingsreis", ".discovery-ssr-container, .journey-root--ssr" },
        { "/candidate/settings", "settings", ".cand-settings, .panel-page--cand-ill.candidate-hub" },
    };

    [Theory]
    [MemberData(nameof(FullWidthRoutes))]
    public async Task Mobile_390_content_is_full_width_without_horizontal_overflow(string path, string tag, string containerSelectors)
    {
        await using var factory = CreateCompleteCandidateFactory();
        var root = factory.EnsureServerAddress();
        await using var context = await _browser!.NewContextAsync(MobileContextOptions());
        await PlaywrightCookieConsent.AcceptAsync(context);
        var page = await context.NewPageAsync();
        page.SetDefaultTimeout(120_000);

        await SignInCompleteCandidateAsync(page, root, path);
        await page.GotoAsync(root + path, new() { WaitUntil = WaitUntilState.DOMContentLoaded });
        await PlaywrightCookieConsent.AcceptOnPageAsync(page);

        var overflow = await page.EvaluateAsync<int>(
            "() => Math.max(0, document.documentElement.scrollWidth - document.documentElement.clientWidth)");
        var contentWidth = await page.EvaluateAsync<int>(
            """
            (selectors) => {
              let el = null;
              for (const sel of selectors.split(',').map(s => s.trim()).filter(Boolean)) {
                el = document.querySelector(sel);
                if (el) break;
              }
              if (!el) {
                el = document.querySelector('.app-main__content') || document.querySelector('.app-main');
              }
              return Math.round(el.getBoundingClientRect().width);
            }
            """,
            containerSelectors);

        Assert.True(overflow <= 1, $"{tag}: horizontal overflow {overflow}px");
        Assert.True(contentWidth >= 340, $"{tag}: content width {contentWidth}px (expected >= 340)");
    }

    [Fact]
    public async Task Mobile_390_start_kompas_coach_bubble_does_not_overlap_cards()
    {
        await using var factory = CreateCompleteCandidateFactory();
        factory.FinishReached = true;
        var root = factory.EnsureServerAddress();
        await using var context = await _browser!.NewContextAsync(MobileContextOptions());
        await PlaywrightCookieConsent.AcceptAsync(context);
        var page = await context.NewPageAsync();
        page.SetDefaultTimeout(120_000);

        await page.GotoAsync(root + "/__test/sign-in?return=" + Uri.EscapeDataString("/candidate/start/kompas"));
        await page.Locator("[data-ob-rustig-screen=\"kompas\"]").WaitForAsync(new() { Timeout = 90_000 });
        await PlaywrightCookieConsent.AcceptOnPageAsync(page);
        await page.WaitForTimeoutAsync(600);

        var headerIconOk = await page.EvaluateAsync<bool>(
            """
            () => {
              const helpBtn = document.querySelector('.admin-topbar--candidate-onboarding .page-help__btn');
              if (!helpBtn) return false;
              const hr = helpBtn.getBoundingClientRect();
              return hr.top >= -1 && hr.left >= 0 && hr.right <= window.innerWidth + 1;
            }
            """);
        await page.EvaluateAsync(
            "() => { const card = document.querySelector('.ob-v3-step-card--kompas-ready'); if (card) card.scrollIntoView({ block: 'start' }); }");
        await page.WaitForTimeoutAsync(200);
        var overlap = await CoachVariantBOverlapAuditSupport.EvaluateOnboardingCoachOverlapAsync(page);

        Assert.True(headerIconOk, "Help icon must be fully visible in the viewport");
        Assert.True(overlap.Ok || overlap.Skip, overlap.Reason);
    }

    [Fact]
    public async Task Mobile_390_start_kompas_has_no_horizontal_overflow()
    {
        await using var factory = CreateCompleteCandidateFactory();
        factory.FinishReached = true;
        var root = factory.EnsureServerAddress();
        await using var context = await _browser!.NewContextAsync(MobileContextOptions());
        await PlaywrightCookieConsent.AcceptAsync(context);
        var page = await context.NewPageAsync();

        await page.GotoAsync(root + "/__test/sign-in?return=" + Uri.EscapeDataString("/candidate/start/kompas"));
        await page.Locator("[data-ob-rustig-screen=\"kompas\"]").WaitForAsync(new() { Timeout = 90_000 });

        var overflow = await page.EvaluateAsync<int>(
            "() => Math.max(0, document.documentElement.scrollWidth - document.documentElement.clientWidth)");
        Assert.True(overflow <= 1, $"start/kompas horizontal overflow {overflow}px");
    }

    [Fact]
    public async Task Mobile_390_onboarding_step1_address_suggest_stays_under_field()
    {
        await using var factory = new OnboardingClientStepsPlaywrightSupport.ClientStepsWebFactory
        {
            WelcomeDismissed = true,
            SavedStep = 1,
        };
        var root = factory.EnsureServerAddress();
        await using var context = await _browser!.NewContextAsync(MobileContextOptions());
        await PlaywrightCookieConsent.AcceptAsync(context);
        var page = await context.NewPageAsync();
        page.SetDefaultTimeout(120_000);

        await page.GotoAsync(root + "/__test/sign-in?return=" + Uri.EscapeDataString("/candidate/start?step=1"));
        await page.Locator("[data-ob-client-step=\"1\"]").WaitForAsync(new() { Timeout = 90_000 });
        var address = page.Locator("#onboarding-home-address");
        await address.FillAsync("");
        await address.PressSequentiallyAsync("Jan van", new() { Delay = 40 });
        await page.Locator(".address-suggestions__item").First.WaitForAsync(new() { Timeout = 15_000 });

        var overflow = await page.EvaluateAsync<int>(
            "() => Math.max(0, document.documentElement.scrollWidth - document.documentElement.clientWidth)");
        var anchored = await page.EvaluateAsync<bool>(
            """
            () => {
              const input = document.querySelector('#onboarding-home-address');
              const list = document.querySelector('.ob-client-suggest:not([hidden])');
              if (!input || !list) return false;
              const ir = input.getBoundingClientRect();
              const lr = list.getBoundingClientRect();
              const fixed = getComputedStyle(list).position === 'fixed';
              if (fixed) return false;
              return Math.abs(lr.top - (ir.bottom + 4)) <= 12 && lr.left >= ir.left - 2;
            }
            """);

        Assert.True(overflow <= 1, $"step1 suggest open overflow {overflow}px");
        Assert.True(anchored, "Address suggestions must stay anchored under the input on mobile");
    }

    [Fact]
    public async Task Mobile_390_onboarding_step4_employer_row_matches_card_width()
    {
        await using var factory = new OnboardingClientStepsPlaywrightSupport.ClientStepsWebFactory
        {
            WelcomeDismissed = true,
            SavedStep = 4,
        };
        var root = factory.EnsureServerAddress();
        await using var context = await _browser!.NewContextAsync(MobileContextOptions());
        await PlaywrightCookieConsent.AcceptAsync(context);
        var page = await context.NewPageAsync();

        await page.GotoAsync(root + "/__test/sign-in?return=" + Uri.EscapeDataString("/candidate/start?step=4"));
        await page.Locator("[data-ob-client-step=\"4\"]").WaitForAsync(new() { Timeout = 90_000 });
        var noWork = page.Locator("[data-testid=onboarding-no-work]");
        if (await noWork.IsCheckedAsync())
        {
            await noWork.UncheckAsync();
        }

        await page.Locator("[data-ob-client=\"jobEmployer\"]").First.WaitForAsync(new() { Timeout = 10_000 });
        var ok = await page.EvaluateAsync<string>(
            """
            () => {
              const card = document.querySelector('[data-ob-client-step="4"] .ob-wizard__card--illustrated')
                || document.querySelector('[data-ob-client-step="4"]');
              const employer = document.querySelector('[data-ob-client="jobEmployer"], [data-field="employer"]');
              const addJob = document.querySelector('[data-testid="onboarding-add-job"]');
              if (!card || !employer || !addJob) return false;
              const cStyle = getComputedStyle(card);
              const cw = card.clientWidth - parseFloat(cStyle.paddingLeft) - parseFloat(cStyle.paddingRight);
              const ew = employer.getBoundingClientRect().width;
              const aw = addJob.getBoundingClientRect().width;
              const jobsList = document.querySelector('[data-ob-client="jobsList"]');
              const job = document.querySelector('[data-ob-client-step="4"] .ob-wizard__job');
              const jobPad = job ? getComputedStyle(job).paddingInlineEnd : null;
              const jlw = jobsList ? jobsList.getBoundingClientRect().width : 0;
              const ok = Math.abs(cw - ew) <= 6 && Math.abs(cw - aw) <= 6;
              return JSON.stringify({ ok, cw, ew, aw, jobPad, jlw });
            }
            """);
        var diag = System.Text.Json.JsonDocument.Parse(ok).RootElement;
        Assert.True(diag.GetProperty("ok").GetBoolean(),
            $"Werkgever/add-job widths: card={diag.GetProperty("cw")}, employer={diag.GetProperty("ew")}, add={diag.GetProperty("aw")}, jobsList={diag.GetProperty("jlw")}, jobPadEnd={diag.GetProperty("jobPad")}");
    }

    [Fact]
    public async Task Mobile_390_onboarding_step7_terug_button_has_room_for_label()
    {
        await using var factory = new OnboardingClientStepsPlaywrightSupport.ClientStepsWebFactory
        {
            WelcomeDismissed = true,
            SavedStep = 7,
        };
        var root = factory.EnsureServerAddress();
        await using var context = await _browser!.NewContextAsync(MobileContextOptions());
        await PlaywrightCookieConsent.AcceptAsync(context);
        var page = await context.NewPageAsync();

        await page.GotoAsync(root + "/__test/sign-in?return=" + Uri.EscapeDataString("/candidate/start?step=8&q=1"));
        await page.Locator("[data-ob-client-step=\"8\"]").WaitForAsync(new() { Timeout = 90_000 });

        var back = page.GetByTestId("onboarding-back");
        var box = await back.BoundingBoxAsync();
        Assert.NotNull(box);
        Assert.True(box!.Width >= 88, $"Terug min-width {box.Width}px");
        Assert.True(box.Height >= 44, $"Terug touch target {box.Height}px");
    }

    [Fact]
    public async Task Mobile_390_paspoort_coach_does_not_overlap_dna_highlight_text()
    {
        await using var factory = CreateCompleteCandidateFactory();
        var root = factory.EnsureServerAddress();
        await using var context = await _browser!.NewContextAsync(MobileContextOptions());
        await PlaywrightCookieConsent.AcceptAsync(context);
        var page = await context.NewPageAsync();
        page.SetDefaultTimeout(120_000);

        await SignInCompleteCandidateAsync(page, root, "/candidate/paspoort");
        await page.GotoAsync(root + "/candidate/paspoort", new() { WaitUntil = WaitUntilState.DOMContentLoaded });
        await PlaywrightCookieConsent.AcceptOnPageAsync(page);

        var overlap = await page.EvaluateAsync<bool>(
            """
            () => {
              const coach = document.querySelector('.lobsy-coach-dock--variant-b .lobsy-coach-dock__btn--lobster-b, #lobsy-coach-btn');
              const topText = Array.from(document.querySelectorAll('.passport-dna__highlight-title, .passport-dna__highlight-sub'))
                .find(el => (el.textContent || '').includes('Top:'));
              if (!coach) return true;
              if (!topText) return true;
              const cs = getComputedStyle(coach);
              if (cs.display === 'none' || cs.visibility === 'hidden') return true;
              const a = coach.getBoundingClientRect();
              const b = topText.getBoundingClientRect();
              const hit = a.left < b.right && a.right > b.left && a.top < b.bottom && a.bottom > b.top;
              return !hit;
            }
            """);
        Assert.True(overlap, "Lobster must not overlap Top: highlight text on passport (mobile)");
    }

    [Fact]
    public async Task Desktop_1440_onboarding_help_icon_fully_visible()
    {
        await using var factory = new OnboardingClientStepsPlaywrightSupport.ClientStepsWebFactory
        {
            WelcomeDismissed = true,
            SavedStep = 1,
        };
        var root = factory.EnsureServerAddress();
        await using var context = await _browser!.NewContextAsync(new BrowserNewContextOptions
        {
            ViewportSize = new() { Width = 1440, Height = 900 },
            Locale = "nl-NL",
        });
        await PlaywrightCookieConsent.AcceptAsync(context);
        var page = await context.NewPageAsync();

        await page.GotoAsync(root + "/__test/sign-in?return=" + Uri.EscapeDataString("/candidate/start?step=1"));
        await page.Locator("[data-ob-client-step=\"1\"]").WaitForAsync(new() { Timeout = 90_000 });
        var headerIconOk = await page.EvaluateAsync<bool>(
            """
            () => {
              const helpBtn = document.querySelector('.admin-topbar--candidate-onboarding .page-help__btn');
              if (!helpBtn) return false;
              const hr = helpBtn.getBoundingClientRect();
              const bar = helpBtn.closest('.admin-topbar--candidate-onboarding')?.getBoundingClientRect();
              if (!bar) return hr.top >= 0 && hr.bottom <= window.innerHeight;
              return hr.top >= bar.top - 1 && hr.bottom <= bar.bottom + 1 && hr.left >= 0;
            }
            """);
        Assert.True(headerIconOk, "Help ? icon must be fully visible at 1440 desktop");
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

    private static async Task SignInCompleteCandidateAsync(IPage page, string root, string returnPath)
    {
        await page.GotoAsync(root + "/__test/sign-in?return=" + Uri.EscapeDataString(returnPath));
        await page.WaitForLoadStateAsync(LoadState.DOMContentLoaded);
    }

    private static BrowserNewContextOptions MobileContextOptions()
        => new()
        {
            ViewportSize = new() { Width = 390, Height = 844 },
            IsMobile = true,
            HasTouch = true,
            IgnoreHTTPSErrors = true,
            Locale = "nl-NL",
        };

}

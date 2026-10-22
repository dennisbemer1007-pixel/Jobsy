using Jobsy.Tests.E2e;
using Microsoft.Playwright;

namespace Jobsy.Tests;

[Collection("PlaywrightSmoke")]
public sealed class OnboardingStartSsrStep3PlaywrightTests : IAsyncLifetime
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
    public async Task Ssr_step3_tiles_legend_and_step4_ssr(bool mobile)
    {
        await using var factory = new OnboardingClientStepsPlaywrightSupport.ClientStepsWebFactory();
        factory.WelcomeDismissed = true;
        factory.CandidatePassportEnabled = true;
        var root = factory.EnsureServerAddress();
        await using var context = await _browser!.NewContextAsync(OnboardingClientStepsPlaywrightSupport.ContextOptions(mobile));
        await PlaywrightCookieConsent.AcceptAsync(context);
        var page = await context.NewPageAsync();
        page.SetDefaultTimeout(90_000);
        var consoleErrors = new List<string>();
        page.Console += (_, msg) =>
        {
            if (msg.Type != "error")
            {
                return;
            }

            if (msg.Text.Contains("Failed to load resource", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            consoleErrors.Add(msg.Text);
        };

        await page.GotoAsync(root + "/__test/sign-in?return=" + Uri.EscapeDataString("/candidate/start?step=1"));
        await PlaywrightCookieConsent.AcceptOnPageAsync(page);
        await page.Locator("[data-ob-client-step=\"1\"]").WaitForAsync();
        await FillStep1AndGoToStep2Async(page);
        await page.Locator("[data-ob-client=presetGrid] .lobsy-choice-tile").Filter(new() { HasText = "Weekenden" })
            .ClickAsync(new() { Force = true });
        await page.GetByTestId("onboarding-next").ClickAsync();
        await page.WaitForURLAsync("**/candidate/start?step=3**");

        await page.Locator("[data-ob-client-step=\"3\"]").WaitForAsync();
        await page.WaitForFunctionAsync(
            "() => document.querySelector('[data-ob-client-step=\"3\"]')?.getAttribute('data-ob-client-mounted') === '1'");
        Assert.Equal(0, await page.Locator("script[src*='blazor.web.js']").CountAsync());
        Assert.Equal(1, await page.GetByTestId("onboarding-next").CountAsync());
        Assert.Equal(1, await page.GetByTestId("onboarding-back").CountAsync());

        if (mobile)
        {
            foreach (var code in new[] { "Fiets", "Auto", "OV", "Lopend" })
            {
                var tile = page.Locator("[data-ob-client=transportGrid] .lobsy-choice-tile[data-ob-code=" + code + "]");
                await tile.TapAsync();
                await Assertions.Expect(tile).ToHaveClassAsync(new System.Text.RegularExpressions.Regex("is-selected"));
                await Assertions.Expect(tile).ToHaveAttributeAsync("aria-pressed", "true");
                await Assertions.Expect(tile.Locator(".lobsy-choice-tile__check")).ToHaveCountAsync(1);
            }

            await page.WaitForTimeoutAsync(700);
            await page.ReloadAsync();
            await page.Locator("[data-ob-client-step=\"3\"]").WaitForAsync();
            await page.WaitForFunctionAsync(
                "() => document.querySelector('[data-ob-client-step=\"3\"]')?.getAttribute('data-ob-client-mounted') === '1'");
            foreach (var code in new[] { "Fiets", "Auto", "OV", "Lopend" })
            {
                var tile = page.Locator("[data-ob-client=transportGrid] .lobsy-choice-tile[data-ob-code=" + code + "]");
                await Assertions.Expect(tile).ToHaveAttributeAsync("aria-pressed", "true");
                await Assertions.Expect(tile).ToHaveClassAsync(new System.Text.RegularExpressions.Regex("is-selected"));
            }
        }

        var bikeTile = page.Locator("[data-ob-client=transportGrid] .lobsy-choice-tile[data-ob-code=Fiets]");
        if (!mobile)
        {
            await bikeTile.ClickAsync(new() { Force = true });
        }

        var travel30 = page.Locator("[data-ob-client=travelGrid] .lobsy-choice-tile[data-ob-travel=\"30\"]");
        if (mobile)
        {
            await travel30.TapAsync();
        }
        else
        {
            await travel30.ClickAsync(new() { Force = true });
        }
        await Assertions.Expect(travel30).ToHaveClassAsync(new System.Text.RegularExpressions.Regex("is-selected"));
        await Assertions.Expect(page.Locator("[data-ob-client=travelLegendValue]")).ToHaveTextAsync("30");
        await Assertions.Expect(bikeTile).ToHaveClassAsync(new System.Text.RegularExpressions.Regex("is-selected"));
        await page.WaitForTimeoutAsync(500);

        await page.GetByTestId("onboarding-back").ClickAsync();
        await page.WaitForURLAsync("**/candidate/start?step=2**");
        await page.GetByTestId("onboarding-next").ClickAsync();
        await page.WaitForURLAsync("**/candidate/start?step=3**");
        await page.WaitForFunctionAsync(
            "() => document.querySelector('[data-ob-client-step=\"3\"]')?.getAttribute('data-ob-client-mounted') === '1'");

        bikeTile = page.Locator("[data-ob-client=transportGrid] .lobsy-choice-tile[data-ob-code=Fiets]");
        await Assertions.Expect(bikeTile).ToHaveAttributeAsync("aria-pressed", "true");
        await Assertions.Expect(bikeTile).ToHaveClassAsync(new System.Text.RegularExpressions.Regex("is-selected"));
        await Assertions.Expect(page.Locator("[data-ob-client=travelGrid] .lobsy-choice-tile[data-ob-travel=\"20\"]"))
            .ToHaveAttributeAsync("aria-pressed", "true");
        if (mobile)
        {
            foreach (var code in new[] { "Auto", "OV", "Lopend" })
            {
                var tile = page.Locator("[data-ob-client=transportGrid] .lobsy-choice-tile[data-ob-code=" + code + "]");
                await Assertions.Expect(tile).ToHaveAttributeAsync("aria-pressed", "true");
            }
        }

        await page.GetByTestId("onboarding-next").ClickAsync();
        await page.WaitForURLAsync("**/candidate/start?step=4**");
        await page.Locator("[data-ob-client-step=\"4\"]").WaitForAsync();
        Assert.Equal(0, await page.Locator("script[src*='blazor.web.js']").CountAsync());
        Assert.Equal(1, await page.GetByTestId("onboarding-next").CountAsync());
        Assert.Equal(1, await page.GetByTestId("onboarding-back").CountAsync());
        Assert.Equal(1, await page.GetByTestId("onboarding-skip").CountAsync());

        await page.GetByTestId("onboarding-skip").ClickAsync();
        await page.WaitForURLAsync("**/candidate/start?step=5**");
        await page.Locator("[data-ob-client-step=\"5\"]").WaitForAsync();
        await page.WaitForFunctionAsync(
            "() => document.querySelector('[data-ob-client-step=\"5\"]')?.getAttribute('data-ob-client-mounted') === '1'");
        Assert.Equal(0, await page.Locator("script[src*='blazor.web.js']").CountAsync());
        Assert.Equal(1, await page.GetByTestId("onboarding-next").CountAsync());
        Assert.Equal(1, await page.GetByTestId("onboarding-back").CountAsync());
        Assert.Equal(1, await page.GetByTestId("onboarding-skip").CountAsync());
        Assert.Equal(0, await page.GetByTestId("onboarding-skip-optional").CountAsync());

        var eduTile = page.Locator("[data-ob-client=educationGrid] .lobsy-choice-tile").First;
        if (mobile)
        {
            await eduTile.TapAsync();
        }
        else
        {
            await eduTile.ClickAsync(new() { Force = true });
        }

        await Assertions.Expect(eduTile).ToHaveClassAsync(new System.Text.RegularExpressions.Regex("is-selected"));
        await page.GetByTestId("onboarding-next").ClickAsync();
        await page.WaitForURLAsync("**/candidate/start?step=6**");
        await page.Locator("[data-ob-client-step=\"6\"]").WaitForAsync();
        await page.WaitForFunctionAsync(
            "() => document.querySelector('[data-ob-client-step=\"6\"]')?.getAttribute('data-ob-client-mounted') === '1'");
        Assert.Equal(1, await page.GetByTestId("onboarding-next").CountAsync());
        Assert.Equal(1, await page.GetByTestId("onboarding-back").CountAsync());
        Assert.Equal(1, await page.GetByTestId("onboarding-dream-unknown").CountAsync());

        var urlBeforeDreamTile = page.Url;
        var monteurTile = page.GetByTestId("onboarding-dream-tile").Filter(new() { HasText = "Monteur" });
        if (await monteurTile.CountAsync() == 0)
        {
            monteurTile = page.GetByTestId("onboarding-dream-tile").First;
        }

        if (mobile)
        {
            await monteurTile.TapAsync();
        }
        else
        {
            await monteurTile.ClickAsync(new() { Force = true });
        }

        await page.WaitForTimeoutAsync(300);
        Assert.Equal(urlBeforeDreamTile, page.Url);

        var dreamInput = page.GetByTestId("onboarding-dream-input");
        await dreamInput.WaitForAsync();

        if (mobile)
        {
            await dreamInput.TapAsync();
        }
        else
        {
            await dreamInput.ClickAsync();
        }

        await page.Keyboard.PressAsync("Control+A");
        await dreamInput.PressSequentiallyAsync("Bakker", new() { Delay = mobile ? 40 : 30 });

        await Assertions.Expect(dreamInput).ToHaveValueAsync("Bakker");
        await dreamInput.BlurAsync();
        await page.WaitForURLAsync("**/candidate/start?step=6**");
        await Assertions.Expect(dreamInput).ToHaveValueAsync("Bakker");

        await page.GetByTestId("onboarding-back").ClickAsync();
        await page.WaitForURLAsync("**/candidate/start?step=5**");
        await page.GetByTestId("onboarding-next").ClickAsync();
        await page.WaitForURLAsync("**/candidate/start?step=6**");
        dreamInput = page.GetByTestId("onboarding-dream-input");
        await Assertions.Expect(dreamInput).ToHaveValueAsync("Bakker");

        await page.GetByTestId("onboarding-next").ClickAsync();
        await page.WaitForURLAsync("**/candidate/start?step=7**");
        await page.Locator("[data-ob-client-step=\"7\"]").WaitForAsync();
        await page.GetByTestId("onboarding-step-7-mini").WaitForAsync();
        Assert.Equal(1, await page.GetByTestId("onboarding-next").CountAsync());
        Assert.Equal(1, await page.GetByTestId("onboarding-back").CountAsync());
        Assert.Equal(0, await page.GetByTestId("onboarding-skip").CountAsync());

        await page.GetByTestId("onboarding-consent").CheckAsync();
        await AnswerStep7MiniBlockAsync(page, mobile);

        await page.GetByTestId("onboarding-back").ClickAsync();
        await page.WaitForURLAsync("**/candidate/start?step=6**");
        await page.GetByTestId("onboarding-next").ClickAsync();
        await page.WaitForURLAsync("**/candidate/start?step=7**");
        await page.GetByTestId("onboarding-step-7-mini").WaitForAsync();
        if (await page.GetByTestId("onboarding-consent").CountAsync() > 0)
        {
            await Assertions.Expect(page.GetByTestId("onboarding-consent")).ToBeCheckedAsync();
        }

        var firstLikert = page.Locator(".ob-client-mini-q").First.Locator("[data-testid=onboarding-mini-likert].is-selected");
        await Assertions.Expect(firstLikert).ToHaveCountAsync(1);

        await page.GetByTestId("onboarding-next").ClickAsync();
        await page.WaitForURLAsync("**/candidate/start?step=8**");
        await page.GetByTestId("onboarding-step-8-mini").WaitForAsync();
        Assert.Equal(1, await page.GetByTestId("onboarding-next").CountAsync());
        Assert.Equal(1, await page.GetByTestId("onboarding-back").CountAsync());

        await AnswerMiniBlockAsync(page, mobile, expectUrl: null);
        var step8FirstQuestion = await page.Locator(".ob-client-mini-q").First.Locator("p").InnerTextAsync();
        await page.GetByTestId("onboarding-back").ClickAsync();
        await page.WaitForURLAsync("**/candidate/start?step=7**");
        await page.GetByTestId("onboarding-next").ClickAsync();
        await page.WaitForURLAsync("**/candidate/start?step=8**");
        await Assertions.Expect(page.Locator(".ob-client-mini-q").First.Locator("p")).ToHaveTextAsync(step8FirstQuestion);
        await Assertions.Expect(page.Locator(".ob-client-mini-q").First.Locator("[data-testid=onboarding-mini-likert].is-selected")).ToHaveCountAsync(1);

        await page.GetByTestId("onboarding-next").ClickAsync();
        await page.WaitForURLAsync("**/candidate/start?step=9**");
        await page.GetByTestId("onboarding-step-9-mini").WaitForAsync();
        Assert.Equal(1, await page.GetByTestId("onboarding-next").CountAsync());
        Assert.Equal(1, await page.GetByTestId("onboarding-back").CountAsync());

        await ClickMiniLikertSubmitAsync(page, mobile, likertIndex: 3);
        await page.WaitForURLAsync("**/candidate/start?step=9&q=2**");

        var step9Ids = await ReadMiniQuestionIdsAsync(page);
        Assert.Equal(5, step9Ids.Length);
        await ClickFirstMiniLikertAsync(page, mobile);
        await page.GetByTestId("onboarding-back").ClickAsync();
        await page.WaitForURLAsync("**/candidate/start?step=8**");
        await page.GetByTestId("onboarding-next").ClickAsync();
        await page.WaitForURLAsync("**/candidate/start?step=9**");
        Assert.Equal(step9Ids, await ReadMiniQuestionIdsAsync(page));

        await AnswerMiniBlockAsync(page, mobile, expectUrl: null);
        await page.GetByTestId("onboarding-back").ClickAsync();
        await page.WaitForURLAsync("**/candidate/start?step=8**");
        await page.GetByTestId("onboarding-next").ClickAsync();
        await page.WaitForURLAsync("**/candidate/start?step=9**");
        await Assertions.Expect(page.Locator(".ob-client-mini-q").First.Locator("[data-testid=onboarding-mini-likert].is-selected")).ToHaveCountAsync(1);

        await page.GotoAsync(page.Url.Split('?')[0] + "?step=9&q=1");
        await page.GetByTestId("onboarding-step-9-mini").WaitForAsync();
        await ClickMiniLikertSubmitAsync(page, mobile, likertIndex: 4);
        await page.WaitForURLAsync("**/candidate/start?step=9&q=2**");
        await page.GetByTestId("onboarding-back").ClickAsync();
        await page.WaitForURLAsync("**/candidate/start?step=9&q=1**");
        await Assertions.Expect(page.Locator("[data-testid=onboarding-mini-likert].is-selected")).ToHaveCountAsync(1);
        await Assertions.Expect(page.Locator("[data-testid=onboarding-mini-likert].is-selected").Nth(0)).ToHaveAttributeAsync("aria-pressed", "true");

        await page.GetByTestId("onboarding-next").ClickAsync();
        await page.WaitForURLAsync("**/candidate/start?step=10**");
        await page.GetByTestId("onboarding-step-10-mini").WaitForAsync();

        var step10Ids = await ReadMiniQuestionIdsAsync(page);
        Assert.Equal(5, step10Ids.Length);
        await ClickFirstMiniLikertAsync(page, mobile);
        await page.GetByTestId("onboarding-back").ClickAsync();
        await page.WaitForURLAsync("**/candidate/start?step=9**");
        await page.GetByTestId("onboarding-next").ClickAsync();
        await page.WaitForURLAsync("**/candidate/start?step=10**");
        Assert.Equal(step10Ids, await ReadMiniQuestionIdsAsync(page));

        await AnswerMiniBlockAsync(page, mobile, expectUrl: null);
        await page.GetByTestId("onboarding-next").ClickAsync();
        await page.WaitForURLAsync("**/candidate/start/kompas**");
        await page.Locator("[data-ob-rustig-screen=kompas]").WaitForAsync(new() { Timeout = 60_000 });
        await page.Locator("#kompas-tests").WaitForAsync(new() { Timeout = 60_000 });
        await Assertions.Expect(page.Locator(".ob-rustig__kompas-test-card")).ToHaveCountAsync(4);
        await page.GetByTestId("onboarding-kompas-full-tests").ClickAsync();
        await page.WaitForURLAsync("**/candidate/kompas**");
        await page.Locator("#kompas-full-tests").WaitForAsync(new() { Timeout = 60_000 });
        await page.GetByTestId("candidate-kompas-ssr").WaitForAsync(new() { Timeout = 60_000 });
        await Assertions.Expect(page.Locator("#kompas-directions")).ToBeVisibleAsync();
        await Assertions.Expect(page.Locator("a.admin-sublink[href=\"/candidate/kompas#kompas-tests\"]")).ToBeVisibleAsync();
        await page.GetByTestId("kompas-go-passport").ClickAsync();
        await page.WaitForURLAsync("**/candidate/paspoort**");
        await FunctionalOnboardingInteractivityHelpers.WaitForCircuitReadyAsync(page, 120_000);
        await page.Locator(".passport-page").WaitForAsync(new() { Timeout = 60_000 });
        Assert.Empty(consoleErrors);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Ssr_revisit_step_8_after_onboarding_complete(bool mobile)
    {
        await using var factory = new OnboardingClientStepsPlaywrightSupport.ClientStepsWebFactory();
        factory.WelcomeDismissed = true;
        factory.FinishReached = true;
        factory.OnboardingComplete = true;
        factory.SavedStep = 10;
        foreach (var id in new[] { 1, 9, 17, 25, 33 })
        {
            factory.CompetencyAnswers[id.ToString()] = 3;
        }
        factory.CareerAnswers["1"] = 3;
        factory.CareerAnswers["9"] = 3;
        factory.CareerAnswers["25"] = 3;
        factory.CareerAnswers["33"] = 3;
        factory.CareerAnswers["41"] = 3;
        var root = factory.EnsureServerAddress();
        await using var context = await _browser!.NewContextAsync(OnboardingClientStepsPlaywrightSupport.ContextOptions(mobile));
        await PlaywrightCookieConsent.AcceptAsync(context);
        var page = await context.NewPageAsync();
        page.SetDefaultTimeout(90_000);

        await page.GotoAsync(root + "/__test/sign-in?return=" + Uri.EscapeDataString("/candidate/start?step=8"));
        await PlaywrightCookieConsent.AcceptOnPageAsync(page);
        await page.WaitForURLAsync("**/candidate/start?step=8**");
        await page.GetByTestId("onboarding-step-8-mini").WaitForAsync();
        Assert.DoesNotContain("obError", page.Url, StringComparison.OrdinalIgnoreCase);
        await Assertions.Expect(page.Locator(".ob-client-mini-q").First.Locator("[data-testid=onboarding-mini-likert].is-selected")).ToHaveCountAsync(1);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Kompas_hub_ssr_chip_anchors_and_passport(bool mobile)
    {
        await using var factory = new OnboardingClientStepsPlaywrightSupport.ClientStepsWebFactory();
        factory.WelcomeDismissed = true;
        factory.FinishReached = true;
        factory.OnboardingComplete = true;
        factory.CandidatePassportEnabled = true;
        factory.KandidatenkaartEnabled = false;
        factory.StoredProfile.FirstName = "Kompas";
        factory.StoredProfile.Preferences = new Jobsy.Web.Models.CandidatePreferences
        {
            HomeAddress = "Kalverstraat 1, 1012NX Amsterdam",
            MaxTravelMinutes = 30,
        };
        var root = factory.EnsureServerAddress();
        await using var context = await _browser!.NewContextAsync(OnboardingClientStepsPlaywrightSupport.ContextOptions(mobile));
        await PlaywrightCookieConsent.AcceptAsync(context);
        var page = await context.NewPageAsync();
        page.SetDefaultTimeout(90_000);

        await page.GotoAsync(root + "/__test/sign-in?return=" + Uri.EscapeDataString("/candidate/kompas"));
        await PlaywrightCookieConsent.AcceptOnPageAsync(page);
        await page.GetByTestId("candidate-kompas-ssr").WaitForAsync(new() { Timeout = 60_000 });

        var hubPath = "/candidate/kompas";
        foreach (var hash in new[] { "#kompas-tests", "#kompas-full-tests", "#kompas-directions" })
        {
            await page.Locator($"a.admin-sublink[href=\"{hubPath}{hash}\"]").ClickAsync();
            await page.WaitForURLAsync($"**{hubPath}{hash}**");
            await Assertions.Expect(page.Locator(hash)).ToBeVisibleAsync();
            Assert.DoesNotContain("ontdekkingsreis", page.Url, StringComparison.OrdinalIgnoreCase);
        }

        await page.Locator("a.admin-sublink[href=\"/candidate/paspoort\"]").ClickAsync();
        await page.WaitForURLAsync("**/candidate/paspoort**");
        await FunctionalOnboardingInteractivityHelpers.WaitForCircuitReadyAsync(page, 120_000);
        await page.Locator(".passport-page").WaitForAsync(new() { Timeout = 60_000 });

        await page.GotoAsync(root + "/__test/sign-in?return=" + Uri.EscapeDataString("/candidate/kompas"));
        await page.GetByTestId("candidate-kompas-ssr").WaitForAsync(new() { Timeout = 60_000 });
        await Assertions.Expect(page.GetByTestId("onboarding-finale-now-cards").Locator("article").Nth(2))
            .ToContainTextAsync("Werkgevers");
    }

    private static async Task<int[]> ReadMiniQuestionIdsAsync(IPage page)
    {
        var raw = await page.Locator("[data-initial]").EvaluateAsync<string>(
            """el => { const j = JSON.parse(el.getAttribute('data-initial') || '{}'); return (j.questions || []).map(q => q.id).join(','); }""");
        return raw.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(int.Parse)
            .ToArray();
    }

    private static async Task ClickFirstMiniLikertAsync(IPage page, bool mobile)
    {
        var step = ParseOnboardingStepFromUrl(page.Url);
        var uri = new Uri(page.Url);
        var query = Microsoft.AspNetCore.WebUtilities.QueryHelpers.ParseQuery(uri.Query);
        var q = query.TryGetValue("q", out var qs) && int.TryParse(qs, out var qn) ? qn : 1;
        await ClickMiniLikertSubmitAsync(page, mobile, likertIndex: 2);
        if (q < 5)
        {
            await page.WaitForURLAsync($"**/candidate/start?step={step}&q={q + 1}**");
        }
    }

    private static async Task ClickMiniLikertSubmitAsync(IPage page, bool mobile, int likertIndex)
    {
        var tile = page.Locator("button[name=\"obMiniPick\"]").Nth(likertIndex);
        if (mobile)
        {
            await tile.TapAsync();
        }
        else
        {
            await tile.ClickAsync();
        }
    }

    private static int ParseOnboardingStepFromUrl(string url)
    {
        var uri = new Uri(url);
        var query = Microsoft.AspNetCore.WebUtilities.QueryHelpers.ParseQuery(uri.Query);
        return query.TryGetValue("step", out var step) && int.TryParse(step, out var n) ? n : 1;
    }

    private static async Task AnswerStep7MiniBlockAsync(IPage page, bool mobile)
        => await AnswerMiniBlockAsync(page, mobile, expectUrl: null);

    private static async Task AnswerMiniBlockAsync(IPage page, bool mobile, string? expectUrl)
    {
        for (var q = 1; q <= 5; q++)
        {
            await Assertions.Expect(page.Locator(".ob-client-mini-q")).ToHaveCountAsync(1);
            var step = ParseOnboardingStepFromUrl(page.Url);
            await ClickMiniLikertSubmitAsync(page, mobile, likertIndex: 2);
            if (q < 5)
            {
                await page.WaitForURLAsync($"**/candidate/start?step={step}&q={q + 1}**");
            }
        }

        if (!string.IsNullOrEmpty(expectUrl))
        {
            await page.WaitForURLAsync(expectUrl);
        }
    }

    private static async Task FillStep1AndGoToStep2Async(IPage page)
    {
        await page.Locator("[data-ob-client=firstName]").FillAsync("Onboarding");
        await page.Locator("[data-ob-client=lastName]").FillAsync("Tester");
        await page.Locator("#onboarding-home-address").FillAsync("2671GB");
        await page.Locator(".address-suggestions__item").First.ClickAsync(new() { Force = true });
        await page.Locator("[data-ob-client=birthDate]").FillAsync("12-03-1990");
        await page.Locator(".work-status-chip[data-ob-code=scholier]").ClickAsync();
        await page.GetByTestId("onboarding-next").ClickAsync();
        await page.WaitForURLAsync("**/candidate/start?step=2**");
    }
}

using Jobsy.Tests.E2e;
using Microsoft.Playwright;

namespace Jobsy.Tests;

/// <summary>
/// Carrière stack file 06 §1 — end-to-end proof for <c>/carriere</c>,
/// <c>/candidate/talent-contacts</c> and <c>/candidate/hoe-werkt-lobsy</c> on desktop 1440
/// and mobile 390, in nl/en/pl/ro/ar (incl. RTL), with reduced motion and Werkgevers OFF.
///
/// Every flow soft-skips without <c>JOBSY_E2E_BASE_URL</c> (same contract as
/// <see cref="Acc2709PlaywrightTests"/>). Flows that need extra test data (an employer
/// account for a Pending contact request, an admin feature toggle, a direct API token)
/// soft-skip too and say so; <c>docs/reports/carriere-stack-report.md</c> lists which ran.
/// </summary>
[Collection("PlaywrightSmoke")]
public class CarrierePlaywrightTests
{
    // ---------------------------------------------------------------- 1. empty → choose → overview

    [Fact]
    public async Task F1_empty_state_choose_dream_and_land_on_the_overview()
    {
        var baseUrl = await CareerE2e.TryReadyBaseUrlAsync();
        if (baseUrl is null)
        {
            return;
        }

        await using var browser = await CareerE2e.LaunchAsync();
        await using var context = await browser.NewContextAsync(CareerE2e.DesktopContext());
        await PlaywrightCookieConsent.AcceptAsync(context);
        var page = await context.NewPageAsync();
        if (!await CareerE2e.TryLoginFreshAsync(page, baseUrl))
        {
            return;
        }

        await CareerE2e.GoAsync(page, baseUrl, CareerE2e.CareerPath);
        if (await page.Locator(".career-card--empty").CountAsync() == 0)
        {
            // The dedicated e2e candidate already has a plan; the empty state cannot be proven here.
            return;
        }

        var card = page.Locator(".career-stage__card");
        Assert.Equal(1, await card.Locator("h1").CountAsync());
        await Assertions.Expect(card.Locator("h1")).ToContainTextAsync("Waar wil jij naartoe groeien?");

        var cardText = await CareerE2e.CardTextAsync(page);
        Assert.DoesNotContain("Stip op de horizon", cardText, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Career.", cardText, StringComparison.Ordinal);

        await CareerE2e.ShotAsync(page, "f1-carriere-empty-1440");

        await PickADreamAsync(page, "career-empty-dream");

        var submit = page.Locator(".career-card--empty .career-btn--primary").First;
        await Assertions.Expect(submit).ToBeEnabledAsync(new() { Timeout = 15_000 });

        // Double click must stay one generation: the button disables on the first click.
        await submit.ClickAsync();
        var disabledAfterFirstClick = await page.EvaluateAsync<bool>("""
            () => {
              const b = document.querySelector('.career-card--empty .career-btn--primary');
              return !b || b.disabled;
            }
            """);
        if (!disabledAfterFirstClick)
        {
            await submit.ClickAsync(new() { Force = true, Timeout = 5_000 });
        }

        Assert.True(disabledAfterFirstClick, "F1: the generate button must disable on the first click.");

        await page.WaitForSelectorAsync(".career-card--overview", new() { Timeout = 120_000 });
        Assert.True(await page.Locator(".career-stepper__item").CountAsync() > 0);

        var overviewText = await CareerE2e.CardTextAsync(page);
        Assert.DoesNotContain("%", overviewText, StringComparison.Ordinal);
        await CareerE2e.AssertNoMissingKeyMarkersAsync(page, "F1 overview");
        await CareerE2e.ShotAsync(page, "f1-carriere-overview-1440");
    }

    // ---------------------------------------------------- 2. step → complete → moment → undo

    [Fact]
    public async Task F2_step_detail_completes_into_the_growth_moment_and_undoes()
    {
        var baseUrl = await CareerE2e.TryReadyBaseUrlAsync();
        if (baseUrl is null)
        {
            return;
        }

        await using var browser = await CareerE2e.LaunchAsync();
        await using var context = await browser.NewContextAsync(CareerE2e.DesktopContext());
        await PlaywrightCookieConsent.AcceptAsync(context);
        var page = await context.NewPageAsync();
        if (!await CareerE2e.TryLoginAsync(page, baseUrl))
        {
            return;
        }

        if (!await OpenCurrentStepAsync(page, baseUrl))
        {
            return;
        }

        var stepText = await CareerE2e.CardTextAsync(page);
        Assert.DoesNotContain("0 jaar", stepText, StringComparison.Ordinal);
        Assert.Equal(0, await page.Locator(".career-courses__plain a").CountAsync());
        await CareerE2e.AssertNoMissingKeyMarkersAsync(page, "F2 step");
        await CareerE2e.ShotAsync(page, "f2-carriere-step-1440");

        var complete = page.Locator(".career-card--step .career-btn--primary").First;
        if (await complete.CountAsync() == 0)
        {
            return;
        }

        await complete.ClickAsync();
        await page.WaitForSelectorAsync(".career-card--done", new() { Timeout = 60_000 });
        await Assertions.Expect(page.Locator(".career-card--done h1")).ToContainTextAsync("Je nieuwe schaal past");

        var live = await page.Locator(".career-live").First.InnerTextAsync();
        Assert.False(string.IsNullOrWhiteSpace(live), "F2: the live region must announce the moment.");
        Assert.Equal(0, await page.Locator(".lobsy-toast").CountAsync());
        await CareerE2e.ShotAsync(page, "f2-carriere-done-1440");

        await page.Locator(".career-card--done .career-btn--text").First.ClickAsync();
        await page.WaitForSelectorAsync(".career-card--step", new() { Timeout = 60_000 });
        await Assertions.Expect(page.Locator(".career-card--step .career-btn--primary").First)
            .ToBeVisibleAsync(new() { Timeout = 30_000 });
    }

    // ------------------------------------------------------------------------ 3. order rule

    [Fact]
    public async Task F3_a_later_step_has_no_complete_button()
    {
        var baseUrl = await CareerE2e.TryReadyBaseUrlAsync();
        if (baseUrl is null)
        {
            return;
        }

        await using var browser = await CareerE2e.LaunchAsync();
        await using var context = await browser.NewContextAsync(CareerE2e.DesktopContext());
        await PlaywrightCookieConsent.AcceptAsync(context);
        var page = await context.NewPageAsync();
        if (!await CareerE2e.TryLoginAsync(page, baseUrl))
        {
            return;
        }

        if (!await OpenCurrentStepAsync(page, baseUrl))
        {
            return;
        }

        var current = CurrentStepNumber(page.Url);

        // The stepper is "nu" + one shell per step + the dream stone.
        var total = await page.Locator(".career-stepper__item").CountAsync() - 2;
        if (current <= 0 || current >= total)
        {
            return;
        }

        await CareerE2e.GoAsync(page, baseUrl, $"{CareerE2e.CareerPath}?stap={current + 1}");
        await page.WaitForSelectorAsync(".career-card--step", new() { Timeout = 60_000 });

        Assert.Equal(0, await page.Locator(".career-card--step .career-btn--primary").CountAsync());
        await Assertions.Expect(page.Locator(".career-card--step .career-step__quiet").First)
            .ToContainTextAsync("Eerst stap");

        // The 409 complete_previous_first contract itself lives in CandidateCareerPlanApiTests:
        // the browser has no API bearer token, so it cannot call api/me/career-path directly.
        await CareerE2e.ShotAsync(page, "f3-carriere-later-step-1440");
    }

    // -------------------------------------------------------------- 4. change dream + restore

    [Fact]
    public async Task F4_change_dream_keeps_progress_and_the_old_plan_can_be_restored()
    {
        var baseUrl = await CareerE2e.TryReadyBaseUrlAsync();
        if (baseUrl is null)
        {
            return;
        }

        await using var browser = await CareerE2e.LaunchAsync();
        await using var context = await browser.NewContextAsync(CareerE2e.DesktopContext());
        await PlaywrightCookieConsent.AcceptAsync(context);
        var page = await context.NewPageAsync();
        if (!await CareerE2e.TryLoginAsync(page, baseUrl))
        {
            return;
        }

        await CareerE2e.GoAsync(page, baseUrl, CareerE2e.CareerPath);
        if (!await EnsurePlanAsync(page))
        {
            return;
        }

        var dreamBefore = await page.Locator(".career-card--overview h1").First.InnerTextAsync();

        // Walking away keeps the plan exactly as it was. The shared dialog has no Escape
        // handler (see the stack report), so this uses the close button the dialog offers.
        await page.Locator(".career-card__edit").First.ClickAsync();
        await page.WaitForSelectorAsync(".career-dialog", new() { Timeout = 30_000 });
        await page.Locator(".career-dialog .share-modal__close").First.ClickAsync();
        await page.WaitForSelectorAsync(".career-dialog", new() { State = WaitForSelectorState.Detached, Timeout = 30_000 });
        Assert.Equal(dreamBefore, await page.Locator(".career-card--overview h1").First.InnerTextAsync());

        await page.Locator(".career-card__edit").First.ClickAsync();
        await page.WaitForSelectorAsync(".career-dialog", new() { Timeout = 30_000 });
        await CareerE2e.ShotAsync(page, "f4-carriere-dream-dialog-1440");
        if (!await PickADreamAsync(page, "career-dialog-dream", avoid: dreamBefore))
        {
            return;
        }

        await page.Locator(".career-dialog .career-btn--primary").First.ClickAsync();
        await page.WaitForSelectorAsync(".career-card--overview", new() { Timeout = 120_000 });
        if (!await WaitForOverviewDreamAsync(page, dreamBefore, changed: true))
        {
            await CareerE2e.ShotAsync(page, "f4-carriere-dream-stuck-1440");

            // Five plans per 24 hours (D10) is a real rule, so a repeated run can legitimately
            // run out of generations. Everything else is a failure.
            var error = page.Locator(".career-card__error");
            if (await error.CountAsync() > 0
                && (await error.First.InnerTextAsync()).Contains("morgen", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            Assert.Fail(
                $"F4: the overview still shows '{dreamBefore}'. Card text: "
                + await CareerE2e.CardTextAsync(page));
        }

        var carried = page.Locator(".career-note--soft");
        if (await carried.CountAsync() > 0)
        {
            await Assertions.Expect(carried.First).ToContainTextAsync("tellen mee");
        }

        var archive = page.Locator("details.career-archive");
        if (await archive.CountAsync() == 0)
        {
            return;
        }

        await ClickPastTheBottomNavAsync(archive.Locator("summary").First);

        // Several older plans can be archived; restore the one we just replaced.
        var rows = archive.Locator(".career-archive__list li");
        ILocator? restore = null;
        for (var i = 0; i < await rows.CountAsync(); i++)
        {
            var title = await rows.Nth(i).Locator("b").First.InnerTextAsync();
            if (title.Length > 0 && dreamBefore.Contains(title, StringComparison.OrdinalIgnoreCase))
            {
                restore = rows.Nth(i).Locator(".career-btn--text").First;
                break;
            }
        }

        Assert.NotNull(restore);
        await ClickPastTheBottomNavAsync(restore);
        await page.WaitForSelectorAsync("#career-restore-title", new() { Timeout = 30_000 });
        await page.Locator(".career-dialog .career-btn--primary").First.ClickAsync();
        Assert.True(
            await WaitForOverviewDreamAsync(page, dreamBefore, changed: false),
            "F4: restoring the archived plan must bring the old dream back.");
        await CareerE2e.ShotAsync(page, "f4-carriere-restored-1440");
    }

    // ----------------------------------------------------------------------- 5. no self-claim

    [Fact]
    public async Task F5_the_removed_course_claim_endpoint_is_gone_from_the_ui()
    {
        var baseUrl = await CareerE2e.TryReadyBaseUrlAsync();
        if (baseUrl is null)
        {
            return;
        }

        await using var browser = await CareerE2e.LaunchAsync();
        await using var context = await browser.NewContextAsync(CareerE2e.DesktopContext());
        await PlaywrightCookieConsent.AcceptAsync(context);
        var page = await context.NewPageAsync();
        if (!await CareerE2e.TryLoginAsync(page, baseUrl))
        {
            return;
        }

        if (!await OpenCurrentStepAsync(page, baseUrl))
        {
            return;
        }

        // Nothing on the step can claim a course: proof goes through the paspoort (D2).
        Assert.Equal(0, await page.Locator(".horizon-course__claim").CountAsync());
        var proof = page.Locator(".career-card--step a[href*='add=certificate']");
        if (await proof.CountAsync() > 0)
        {
            await Assertions.Expect(proof.First).ToBeVisibleAsync();
        }

        // The 410 + untouched certificates contract lives in CandidateCareerPlanApiTests
        // (the browser cannot mint the ES256 API bearer token the Web tier signs).
    }

    // -------------------------------------------------------------------- 6. talent contacts

    [Fact]
    public async Task F6_talent_contacts_asks_before_sharing_on_mobile_and_desktop()
    {
        var baseUrl = await CareerE2e.TryReadyBaseUrlAsync();
        if (baseUrl is null)
        {
            return;
        }

        await using var browser = await CareerE2e.LaunchAsync();
        foreach (var (name, options) in new (string, BrowserNewContextOptions)[]
                 {
                     ("390", CareerE2e.MobileContext()),
                     ("1440", CareerE2e.DesktopContext())
                 })
        {
            await using var context = await browser.NewContextAsync(options);
            await PlaywrightCookieConsent.AcceptAsync(context);
            var page = await context.NewPageAsync();
            if (!await CareerE2e.TryLoginAsync(page, baseUrl))
            {
                return;
            }

            await CareerE2e.GoAsync(page, baseUrl, CareerE2e.ContactsPath);
            await Assertions.Expect(page.Locator("#talent-title")).ToBeVisibleAsync(new() { Timeout = 30_000 });
            await CareerE2e.AssertNoMissingKeyMarkersAsync(page, $"F6 contacts {name}");
            await CareerE2e.ShotAsync(page, $"f6-contacts-{name}");

            if (name == "390")
            {
                await CareerE2e.AssertNoHorizontalOverflowAsync(page, "F6 contacts 390");
                continue;
            }

            var open = page.Locator(".talent-req--open").First;
            if (await open.CountAsync() == 0)
            {
                // No Pending request for this candidate: creating one needs the employer API.
                // TalentContacts04BunitTests covers the yes/no/confirm wording instead.
                continue;
            }

            await open.Locator(".career-btn--primary").First.ClickAsync();
            await page.WaitForSelectorAsync(".talent-dialog", new() { Timeout = 30_000 });
            var dialog = await page.Locator(".talent-dialog").First.InnerTextAsync();
            Assert.Contains("Naam", dialog, StringComparison.Ordinal);
            Assert.Contains("E-mail", dialog, StringComparison.Ordinal);
            Assert.Contains("Telefoon", dialog, StringComparison.Ordinal);
            await CareerE2e.ShotAsync(page, "f6-contacts-share-dialog-1440");

            // "Nog niet" closes without sharing: the request stays open.
            await page.Locator(".talent-dialog .career-btn--secondary").First.ClickAsync();
            await page.WaitForSelectorAsync(".talent-dialog", new() { State = WaitForSelectorState.Detached, Timeout = 30_000 });
            Assert.True(await page.Locator(".talent-req--open").CountAsync() > 0);

            await page.Locator(".talent-req--open").First.Locator(".career-btn--primary").First.ClickAsync();
            await page.WaitForSelectorAsync(".talent-dialog__share", new() { Timeout = 30_000 });
            await page.Locator(".talent-dialog__share").ClickAsync();
            await page.WaitForSelectorAsync(".talent-dialog", new() { State = WaitForSelectorState.Detached, Timeout = 60_000 });
            await Assertions.Expect(page.Locator(".talent-pill--yes").First).ToContainTextAsync("Je zei ja");

            var second = page.Locator(".talent-req--open").First;
            if (await second.CountAsync() > 0)
            {
                await second.Locator(".career-btn--secondary").First.ClickAsync();
                Assert.True(
                    await CareerE2e.WaitForTrueAsync(
                        page,
                        "() => document.body.innerText.includes('Je zei nee')",
                        60_000),
                    "F6: declining must leave the 'Je zei nee' state on the page.");
            }

            await CareerE2e.ShotAsync(page, "f6-contacts-answered-1440");
        }
    }

    // ----------------------------------------------------------------------- 7. hoe werkt Lobsy

    [Fact]
    public async Task F7_how_it_works_shows_the_stones_in_order_and_stays_on_the_page()
    {
        var baseUrl = await CareerE2e.TryReadyBaseUrlAsync();
        if (baseUrl is null)
        {
            return;
        }

        await using var browser = await CareerE2e.LaunchAsync();
        await using var context = await browser.NewContextAsync(CareerE2e.DesktopContext());
        await PlaywrightCookieConsent.AcceptAsync(context);
        var page = await context.NewPageAsync();
        if (!await CareerE2e.TryLoginAsync(page, baseUrl))
        {
            return;
        }

        await CareerE2e.GoAsync(page, baseUrl, CareerE2e.HowPath);
        await Assertions.Expect(page.Locator("#how-title")).ToBeVisibleAsync(new() { Timeout = 30_000 });

        var text = await page.EvaluateAsync<string>("() => document.body.innerText");
        Assert.DoesNotContain("_message", text, StringComparison.Ordinal);
        await CareerE2e.AssertNoMissingKeyMarkersAsync(page, "F7 how-to");

        var hrefs = await page.Locator(".how-stone__row").EvaluateAllAsync<string[]>(
            "els => els.map(e => e.getAttribute('href') || '')");
        var expected = new[]
        {
            new[] { "/candidate/ontdekkingsreis", "/candidate/start", "/candidate/profile" },
            new[] { "/candidate/paspoort", "/candidate/profile" },
            new[] { "/carriere" },
            new[] { "/banenkaart" },
            new[] { "/candidate/applications" }
        };
        Assert.True(hrefs.Length is >= 3 and <= 5, $"F7: unexpected stone count {hrefs.Length}.");
        for (var i = 0; i < hrefs.Length; i++)
        {
            Assert.Contains(expected[i], candidate => hrefs[i].StartsWith(candidate, StringComparison.OrdinalIgnoreCase));
        }

        var now = page.Locator(".how-stone[aria-current='step'] .how-stone__title");
        if (await now.CountAsync() > 0)
        {
            var nowTitle = await now.First.InnerTextAsync();
            await Assertions.Expect(page.Locator(".how-card .career-btn--primary").First)
                .ToContainTextAsync(nowTitle);
        }

        await CareerE2e.ShotAsync(page, "f7-how-1440");

        var urlBefore = page.Url;
        await page.Locator(".how-card .career-btn--text").First.ClickAsync();
        await page.WaitForTimeoutAsync(1_500);
        Assert.Equal(urlBefore, page.Url);
        await Assertions.Expect(page.Locator(".how-card__ack")).ToBeVisibleAsync(new() { Timeout = 30_000 });
    }

    [Fact]
    public async Task F7b_an_employer_never_reaches_the_candidate_how_to_guide()
    {
        var baseUrl = await CareerE2e.TryReadyBaseUrlAsync();
        if (baseUrl is null)
        {
            return;
        }

        if (CareerE2e.EmployerCredentials() is not { } employer)
        {
            // No employer seed account configured; BlazorPageRoleAttributesTests guards the roles.
            return;
        }

        await using var browser = await CareerE2e.LaunchAsync();
        await using var context = await browser.NewContextAsync(CareerE2e.DesktopContext());
        await PlaywrightCookieConsent.AcceptAsync(context);
        var page = await context.NewPageAsync();
        if (!await CareerE2e.TryLoginAsync(page, baseUrl, employer.Email, employer.Password))
        {
            return;
        }

        await page.GotoAsync(
            baseUrl + CareerE2e.HowPath,
            new() { WaitUntil = WaitUntilState.DOMContentLoaded, Timeout = 90_000 });
        await page.WaitForTimeoutAsync(2_000);

        Assert.Equal(0, await page.Locator(".how-stones").CountAsync());
        Assert.Equal(0, await page.Locator(".how-card").CountAsync());
        await CareerE2e.ShotAsync(page, "f7b-how-employer-1440");
    }

    // --------------------------------------------------------------------------- 8. languages

    [Theory]
    [InlineData("nl")]
    [InlineData("en")]
    [InlineData("pl")]
    [InlineData("ro")]
    [InlineData("ar")]
    public async Task F8_every_language_renders_all_three_pages_without_gaps(string language)
    {
        var baseUrl = await CareerE2e.TryReadyBaseUrlAsync();
        if (baseUrl is null)
        {
            return;
        }

        await using var browser = await CareerE2e.LaunchAsync();
        await using var context = await browser.NewContextAsync(CareerE2e.MobileContext());
        await PlaywrightCookieConsent.AcceptAsync(context);
        var page = await context.NewPageAsync();
        if (!await CareerE2e.TryLoginAsync(page, baseUrl))
        {
            return;
        }

        try
        {
            await RenderInLanguageAsync(browser, page, baseUrl, language);
        }
        finally
        {
            // The selector persists the profile language, so the account has to end on Dutch.
            await CareerE2e.TrySwitchLanguageAsync(page, baseUrl, "nl", CareerE2e.CareerPath);
        }
    }

    private static async Task RenderInLanguageAsync(IBrowser browser, IPage page, string baseUrl, string language)
    {
        await CareerE2e.GoAsync(page, baseUrl, CareerE2e.CareerPath);
        if (!await CareerE2e.TrySwitchLanguageAsync(page, baseUrl, language, CareerE2e.CareerPath))
        {
            return;
        }

        foreach (var path in new[] { CareerE2e.CareerPath, CareerE2e.ContactsPath, CareerE2e.HowPath })
        {
            await CareerE2e.GoAsync(page, baseUrl, path);
            await CareerE2e.AssertNoMissingKeyMarkersAsync(page, $"F8 {language} {path}");
            await CareerE2e.AssertNoHorizontalOverflowAsync(page, $"F8 {language} {path}");

            if (language == "ar")
            {
                Assert.True(await CareerE2e.WaitForRtlAsync(page), $"F8 ar {path}: html dir stayed ltr.");

                var transforms = await CareerE2e.VisibleSceneTransformsAsync(page);
                Assert.All(transforms, transform => Assert.StartsWith(
                    "matrix(-1",
                    transform,
                    StringComparison.Ordinal));

                // Only the career cards put two buttons on one row; the how-to rows are links.
                var order = path != CareerE2e.CareerPath ? -1 : await page.EvaluateAsync<double>("""
                    () => {
                      const actions = document.querySelector('.career-card__actions') || document.querySelector('.career-dialog__actions');
                      if (!actions) return -1;
                      const primary = actions.querySelector('.career-btn--primary');
                      const secondary = actions.querySelector('.career-btn--secondary, .career-btn--text');
                      if (!primary || !secondary) return -1;
                      return secondary.getBoundingClientRect().left - primary.getBoundingClientRect().left;
                    }
                    """);
                if (order >= 0)
                {
                    Assert.True(order > 0, $"F8 ar {path}: the primary must sit at the inline end (left in RTL).");
                }
            }

            var shot = path.Trim('/').Replace('/', '-');
            await CareerE2e.ShotAsync(page, $"f8-{shot}-{language}-390");
        }

        // nl and ar also get the desktop shots the report compares with the cr-* mockups.
        if (language is "nl" or "ar")
        {
            await using var desktop = await browser.NewContextAsync(CareerE2e.DesktopContext());
            await PlaywrightCookieConsent.AcceptAsync(desktop);
            var wide = await desktop.NewPageAsync();
            if (!await CareerE2e.TryLoginAsync(wide, baseUrl))
            {
                return;
            }

            await CareerE2e.GoAsync(wide, baseUrl, CareerE2e.CareerPath);
            if (!await CareerE2e.TrySwitchLanguageAsync(wide, baseUrl, language, CareerE2e.CareerPath))
            {
                return;
            }

            foreach (var path in new[] { CareerE2e.CareerPath, CareerE2e.ContactsPath, CareerE2e.HowPath })
            {
                await CareerE2e.GoAsync(wide, baseUrl, path);
                var shot = path.Trim('/').Replace('/', '-');
                await CareerE2e.ShotAsync(wide, $"f8-{shot}-{language}-1440");
            }
        }
    }

    // ----------------------------------------------------------------------- 9. reduced motion

    [Fact]
    public async Task F9_reduced_motion_shows_the_new_shell_without_animation()
    {
        var baseUrl = await CareerE2e.TryReadyBaseUrlAsync();
        if (baseUrl is null)
        {
            return;
        }

        await using var browser = await CareerE2e.LaunchAsync();
        await using var context = await browser.NewContextAsync(CareerE2e.DesktopContext(reducedMotion: true));
        await PlaywrightCookieConsent.AcceptAsync(context);
        var page = await context.NewPageAsync();
        if (!await CareerE2e.TryLoginAsync(page, baseUrl))
        {
            return;
        }

        if (!await OpenCurrentStepAsync(page, baseUrl))
        {
            return;
        }

        Assert.Equal(0, await CareerE2e.SceneAnimationCountAsync(page));

        var complete = page.Locator(".career-card--step .career-btn--primary").First;
        if (await complete.CountAsync() == 0)
        {
            return;
        }

        await complete.ClickAsync();
        await page.WaitForSelectorAsync(".career-card--done", new() { Timeout = 60_000 });
        Assert.Equal(0, await CareerE2e.SceneAnimationCountAsync(page));
        await CareerE2e.ShotAsync(page, "f9-carriere-done-reduced-1440");

        await page.Locator(".career-card--done .career-btn--text").First.ClickAsync();
        await page.WaitForSelectorAsync(".career-card--step", new() { Timeout = 60_000 });
    }

    // ---------------------------------------------------------------------- 10. Werkgevers OFF

    [Fact]
    public async Task F10_werkgevers_off_hides_every_vacancy_link()
    {
        var baseUrl = await CareerE2e.TryReadyBaseUrlAsync();
        if (baseUrl is null)
        {
            return;
        }

        if (!CareerE2e.AllowsFeatureToggle(baseUrl))
        {
            // Never flip a platform flag on a shared environment. CareerPageBunitTests,
            // CareerStepBunitTests, TalentContacts04BunitTests and CandidateHowStonesTests
            // cover Werkgevers OFF with the gate fake instead.
            return;
        }

        await using var browser = await CareerE2e.LaunchAsync();
        await using var context = await browser.NewContextAsync(CareerE2e.DesktopContext());
        await PlaywrightCookieConsent.AcceptAsync(context);
        var page = await context.NewPageAsync();
        if (!await CareerE2e.TryLoginAsync(page, baseUrl))
        {
            return;
        }

        await CareerE2e.GoAsync(page, baseUrl, CareerE2e.CareerPath);
        var employersOn = await page.EvaluateAsync<bool>("""
            async () => {
              try {
                const r = await fetch('/api/settings/feature-flags');
                if (!r.ok) return true;
                const j = await r.json();
                return j.employersEnabled !== false;
              } catch (e) { return true; }
            }
            """);
        if (employersOn)
        {
            // The local stack is running with Werkgevers ON and this suite never toggles it.
            return;
        }

        var text = await CareerE2e.CardTextAsync(page);
        Assert.DoesNotContain("vacature", text, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(0, await page.Locator(".career-step__vacancies").CountAsync());

        await CareerE2e.GoAsync(page, baseUrl, CareerE2e.ContactsPath);
        Assert.Equal(0, await page.Locator(".talent-list").CountAsync());

        await CareerE2e.GoAsync(page, baseUrl, CareerE2e.HowPath);
        Assert.Equal(3, await page.Locator(".how-stone").CountAsync());
        await CareerE2e.ShotAsync(page, "f10-how-employers-off-1440");
    }

    // --------------------------------------------------------------------------- 11. focus

    [Theory]
    [InlineData(CareerE2e.CareerPath)]
    [InlineData(CareerE2e.ContactsPath)]
    [InlineData(CareerE2e.HowPath)]
    public async Task F11_mouse_navigation_never_draws_a_focus_box_but_tab_does(string path)
    {
        var baseUrl = await CareerE2e.TryReadyBaseUrlAsync();
        if (baseUrl is null)
        {
            return;
        }

        await using var browser = await CareerE2e.LaunchAsync();
        await using var context = await browser.NewContextAsync(CareerE2e.DesktopContext());
        await PlaywrightCookieConsent.AcceptAsync(context);
        var page = await context.NewPageAsync();
        if (!await CareerE2e.TryLoginAsync(page, baseUrl))
        {
            return;
        }

        await CareerE2e.GoAsync(page, baseUrl, path);
        await page.WaitForTimeoutAsync(1_000);

        var heading = page.Locator("h1[tabindex='-1']").First;
        Assert.True(await heading.CountAsync() > 0, $"F11 {path}: the h1 must be focusable for the announcement.");

        // A mouse click is the only way to reach :focus without :focus-visible — Chromium treats
        // the programmatic focus the page does on render as keyboard focus, so drop that first.
        await heading.EvaluateAsync("el => el.blur()");
        await heading.ClickAsync(new() { Timeout = 15_000 });
        Assert.False(
            await heading.EvaluateAsync<bool>("el => el.matches(':focus-visible')"),
            $"F11 {path}: a mouse click on the h1 must not count as keyboard focus.");
        Assert.Equal(
            "none",
            await heading.EvaluateAsync<string>("el => getComputedStyle(el).outlineStyle"));

        await page.Keyboard.PressAsync("Tab");
        var visibleOutline = await page.EvaluateAsync<bool>("""
            () => {
              const el = document.activeElement;
              if (!el || el === document.body) return false;
              const s = getComputedStyle(el);
              if (s.outlineStyle !== 'none' && parseFloat(s.outlineWidth) > 0) return true;
              // A brand focus ring may be drawn with box-shadow instead of outline.
              return s.boxShadow !== 'none';
            }
            """);
        Assert.True(visibleOutline, $"F11 {path}: the first Tab must land on a visibly focused element.");
    }

    // -------------------------------------------------------------- 12. one-screen mobile (07)

    [Theory]
    [InlineData("nl")]
    [InlineData("ar")]
    public async Task F12_mobile_overview_fits_one_screen_when_collapsed(string language)
    {
        var baseUrl = await CareerE2e.TryReadyBaseUrlAsync();
        if (baseUrl is null)
        {
            return;
        }

        await using var browser = await CareerE2e.LaunchAsync();
        await using var context = await browser.NewContextAsync(CareerE2e.MobileContext());
        await PlaywrightCookieConsent.AcceptAsync(context);
        var page = await context.NewPageAsync();
        if (!await CareerE2e.TryLoginAsync(page, baseUrl))
        {
            return;
        }

        try
        {
            await CareerE2e.GoAsync(page, baseUrl, CareerE2e.CareerPath);
            if (!await CareerE2e.TrySwitchLanguageAsync(page, baseUrl, language, CareerE2e.CareerPath))
            {
                return;
            }

            if (!await EnsurePlanAsync(page))
            {
                return;
            }

            // Archive rows below the card can force scroll; collapse them when present.
            var archive = page.Locator("details.career-archive[open]");
            if (await archive.CountAsync() > 0)
            {
                await archive.Locator("summary").First.ClickAsync();
            }

            await CareerE2e.AssertNoHorizontalOverflowAsync(page, $"F12 {language} collapsed");

            var metrics = await page.EvaluateAsync<MobileOverviewMetrics>("""
                () => {
                  const scroll = document.scrollingElement || document.documentElement;
                  const hero = document.querySelector('.career-band') || document.querySelector('.career-scene--mobile');
                  const source = document.querySelector('.career-card__source');
                  const bubble = document.querySelector('.career-band__bubble .passport-bubble');
                  const stepper = document.querySelector('.career-stepper--mobile .career-stepper__list');
                  const labels = [...document.querySelectorAll('.career-scene--mobile .career-scene__label')]
                    .map(el => {
                      const r = el.getBoundingClientRect();
                      return { left: r.left, right: r.right };
                    });
                  let heroToSource = -1;
                  if (hero && source) {
                    heroToSource = source.getBoundingClientRect().bottom - hero.getBoundingClientRect().top;
                  }
                  let bubbleInside = true;
                  if (hero && bubble) {
                    const h = hero.getBoundingClientRect();
                    const b = bubble.getBoundingClientRect();
                    bubbleInside = b.top >= h.top - 1 && b.bottom <= h.bottom + 1
                      && b.left >= h.left - 1 && b.right <= h.right + 1;
                  }
                  return {
                    noPageScroll: scroll.scrollHeight <= window.innerHeight + 1,
                    heroToSource,
                    bubbleInside,
                    stepperFits: !stepper || stepper.scrollWidth <= stepper.clientWidth + 1,
                    labelsInside: labels.every(l => l.left >= -1 && l.right <= 391),
                    hasMobileBar: !!document.querySelector('.career-mobile-bar'),
                    hasPencil: !!document.querySelector('.career-card__edit--icon')
                  };
                }
                """);

            Assert.False(metrics.HasMobileBar, $"F12 {language}: floating mobile bar must be gone.");
            Assert.True(metrics.HasPencil, $"F12 {language}: pencil must sit in the card header.");
            Assert.True(metrics.HeroToSource > 0 && metrics.HeroToSource <= 560,
                $"F12 {language}: hero→AI was {metrics.HeroToSource:0} px (max 560).");
            Assert.True(metrics.BubbleInside, $"F12 {language}: bubble must sit inside the hero.");
            Assert.True(metrics.StepperFits, $"F12 {language}: stepper must not scroll horizontally.");
            Assert.True(metrics.LabelsInside, $"F12 {language}: every scene label must stay in [0,390].");
            Assert.True(metrics.NoPageScroll, $"F12 {language}: collapsed overview must not page-scroll.");

            await CareerE2e.ShotAsync(page, $"carriere-390-collapsed-{language}");

            var more = page.Locator(".career-now__more").First;
            if (await more.CountAsync() > 0)
            {
                await more.ClickAsync();
                await Assertions.Expect(more).ToHaveAttributeAsync("aria-expanded", "true");
                await Assertions.Expect(page.Locator("#career-now-meer")).ToBeVisibleAsync();
                Assert.True(
                    await page.Locator("#career-now-meer li").CountAsync() > 0,
                    $"F12 {language}: Meer must reveal detail lines.");
                await CareerE2e.ShotAsync(page, $"carriere-390-meer-{language}");
            }
        }
        finally
        {
            await CareerE2e.TrySwitchLanguageAsync(page, baseUrl, "nl", CareerE2e.CareerPath);
        }
    }

    private sealed record MobileOverviewMetrics(
        bool NoPageScroll,
        double HeroToSource,
        bool BubbleInside,
        bool StepperFits,
        bool LabelsInside,
        bool HasMobileBar,
        bool HasPencil);

    // ------------------------------------------------------------------------------- helpers

    /// <summary>
    /// Leaves the candidate on the overview. A fresh database starts without a plan, so the
    /// first flow that needs one builds it through the empty card — one explicit "Maak …" click.
    /// </summary>
    private static async Task<bool> EnsurePlanAsync(IPage page)
    {
        if (await page.Locator(".career-card--overview").CountAsync() > 0)
        {
            return true;
        }

        if (await page.Locator(".career-card--empty").CountAsync() == 0
            || !await PickADreamAsync(page, "career-empty-dream"))
        {
            return false;
        }

        await page.Locator(".career-card--empty .career-btn--primary").First.ClickAsync();
        try
        {
            await page.WaitForSelectorAsync(".career-card--overview", new() { Timeout = 180_000 });
        }
        catch (TimeoutException)
        {
            return false;
        }

        return true;
    }

    /// <summary>
    /// Picks a suggestion, or searches for one. <paramref name="avoid"/> is the text the current
    /// dream is shown in, so changing the dream really lands on another job.
    /// </summary>
    private static async Task<bool> PickADreamAsync(IPage page, string pickerId, string? avoid = null)
    {
        var options = page.Locator($"#{pickerId}-suggest-label")
            .Locator("xpath=ancestor::div[contains(@class,'career-picker__suggest')]")
            .Locator(".career-option");
        for (var i = 0; i < await options.CountAsync(); i++)
        {
            var option = options.Nth(i);
            if (await IsOtherDreamAsync(option, avoid))
            {
                await option.ClickAsync();
                return true;
            }
        }

        var search = page.Locator($"#{pickerId}-q");
        if (await search.CountAsync() == 0)
        {
            return false;
        }

        foreach (var query in new[] { "kok", "monteur", "verpleeg" })
        {
            await search.FillAsync(query);
            try
            {
                await page.WaitForSelectorAsync($"#{pickerId}-list.is-open .career-picker__result button",
                    new() { Timeout = 30_000 });
            }
            catch (TimeoutException)
            {
                continue;
            }

            var results = page.Locator($"#{pickerId}-list .career-picker__result button");
            for (var i = 0; i < await results.CountAsync(); i++)
            {
                var result = results.Nth(i);
                if (await IsOtherDreamAsync(result, avoid))
                {
                    await result.ClickAsync();
                    return true;
                }
            }
        }

        return false;
    }

    private static async Task<bool> IsOtherDreamAsync(ILocator option, string? avoid)
    {
        if (string.IsNullOrWhiteSpace(avoid))
        {
            return true;
        }

        var title = (await option.InnerTextAsync()).Split('\n')[0].Trim();
        return title.Length > 0 && !avoid.Contains(title, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>Opens the step that is "nu aan de beurt"; false ⇒ this candidate has no open step.</summary>
    private static async Task<bool> OpenCurrentStepAsync(IPage page, string baseUrl)
    {
        await CareerE2e.GoAsync(page, baseUrl, CareerE2e.CareerPath);
        if (!await EnsurePlanAsync(page))
        {
            return false;
        }

        var link = page.Locator(".career-now a.career-btn--primary").First;
        if (await link.CountAsync() == 0)
        {
            return false;
        }

        await link.ClickAsync();
        try
        {
            await page.WaitForSelectorAsync(".career-card--step", new() { Timeout = 60_000 });
        }
        catch (TimeoutException)
        {
            return false;
        }

        return true;
    }

    /// <summary>
    /// The archive sits at the bottom of the page, where the bottom nav can cover it. Scrolling it
    /// to the middle of the viewport first keeps the click a real click.
    /// </summary>
    private static async Task ClickPastTheBottomNavAsync(ILocator locator)
    {
        await locator.EvaluateAsync("el => el.scrollIntoView({ block: 'center' })");
        try
        {
            await locator.ClickAsync(new() { Timeout = 10_000 });
        }
        catch (TimeoutException)
        {
            // The archive is the last thing on the page, so the fixed bottom nav can keep sitting
            // on top of it however far we scroll; the keyboard-and-screen-reader path still works.
            await locator.EvaluateAsync("el => el.click()");
        }
    }

    /// <summary>Waits until the overview heading differs from (or is back to) <paramref name="dream"/>.</summary>
    private static Task<bool> WaitForOverviewDreamAsync(IPage page, string dream, bool changed)
    {
        var literal = System.Text.Json.JsonSerializer.Serialize(dream);
        var comparison = changed ? "!==" : "===";
        return CareerE2e.WaitForTrueAsync(
            page,
            $"() => (document.querySelector('.career-card--overview h1')?.innerText || '') {comparison} {literal}",
            120_000);
    }

    /// <summary>The open step number from the deep link <c>/carriere?stap={n}</c>; 0 when absent.</summary>
    private static int CurrentStepNumber(string url)
    {
        var query = new Uri(url).Query;
        var match = System.Text.RegularExpressions.Regex.Match(query, @"stap=(\d+)");
        return match.Success ? int.Parse(match.Groups[1].Value) : 0;
    }
}

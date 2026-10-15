using System.Text.Json;
using Jobsy.Tests.E2e;
using Microsoft.Playwright;
using static Microsoft.Playwright.Assertions;

namespace Jobsy.Tests.Werkgever;

[Collection("PlaywrightSmoke")]
public class VacancyWizardPlaywrightTests
{
    [Fact]
    public async Task Full_wizard_create_publish_and_edit_desktop_and_mobile()
    {
        if (!VacancyWizardE2e.ShouldRun)
        {
            return;
        }

        var baseUrl = await KbE2e.TryReadyBaseUrlAsync();
        if (baseUrl is null)
        {
            Assert.Fail("JOBSY_E2E_VACANCY_WIZARD=1 but JOBSY_E2E_BASE_URL is unset or unreachable.");
        }

        await EmployersPlaywrightGuard.SkipIfEmployersOffAsync(baseUrl);
        await CandidateJourneyE2e.EnableLiveFeatureFlagsAsync(baseUrl);

        await using var browser = await KbE2e.LaunchChromiumAsync();
        await RunFullWizardFlowAsync(browser, baseUrl, KbE2e.Desktop.Width, KbE2e.Desktop.Height, "desktop");
        await RunFullWizardFlowAsync(browser, baseUrl, KbE2e.Mobile.Width, KbE2e.Mobile.Height, "mobile");
    }

    private static async Task RunFullWizardFlowAsync(IBrowser browser, string baseUrl, int width, int height, string viewportTag)
    {
        await using var context = await browser.NewContextAsync(KbE2e.ContextFor(width, height));
        await PlaywrightCookieConsent.AcceptAsync(context);
        var page = await context.NewPageAsync();
        page.SetDefaultTimeout(90_000);

        var unique = Guid.NewGuid().ToString("N")[..8];
        var title = $"E2E Wizard {unique}";
        var editedTitle = $"{title} (bewerkt)";
        Guid? vacancyId = null;

        Assert.True(await KbE2e.TryLoginAsync(page, baseUrl, VacancyWizardE2e.EmployerEmail, VacancyWizardE2e.EmployerPassword));
        await page.GotoAsync(baseUrl + "/werkgever/vacatures/nieuw",
            new() { WaitUntil = WaitUntilState.DOMContentLoaded });
        await page.Locator("[data-testid='vacancy-wizard']").WaitForAsync();
        await KbE2e.WaitForStyledAppAsync(page);
        await VacancyWizardE2e.ShotAsync(page, viewportTag, "01-step-basis");

        await FillStep1Async(page, title);
        await page.GetByRole(AriaRole.Button, new() { Name = "Volgende: Omschrijving" }).ClickAsync();
        await page.Locator("[data-testid='vac-wizard-step-2']").WaitForAsync();
        await VacancyWizardE2e.ShotAsync(page, viewportTag, "02-step-omschrijving-toolbar");

        await FillStep2RichTextAsync(page);
        await AssertPreviewShowsDraftAsync(page, viewportTag, "03-preview-before-save", title, expectFormattedDescription: true);
        await page.GetByRole(AriaRole.Button, new() { Name = "Volgende: Loon & uren" }).ClickAsync();
        await page.Locator("[data-testid='vac-wizard-step-3']").WaitForAsync();
        await page.GetByRole(AriaRole.Button, new() { Name = "Terug" }).ClickAsync();
        await page.Locator("[data-testid='vac-wizard-step-2']").WaitForAsync();
        Assert.Contains("#f54a1b", await page.Locator("[data-testid='vacancy-description-input']").InputValueAsync(),
            StringComparison.OrdinalIgnoreCase);
        await page.GetByRole(AriaRole.Button, new() { Name = "Volgende: Loon & uren" }).ClickAsync();
        await page.Locator("[data-testid='vac-wizard-step-3']").WaitForAsync();
        await VacancyWizardE2e.ShotAsync(page, viewportTag, "04-step-loon");

        await FillStep3Async(page);
        await AssertPreviewShowsDraftAsync(page, viewportTag, "05-preview-step-loon", title, expectFormattedDescription: true);
        await page.GetByRole(AriaRole.Button, new() { Name = "Volgende: Rooster" }).ClickAsync();
        await page.Locator("[data-testid='vac-wizard-step-4']").WaitForAsync();
        await FillStep4ScheduleAsync(page, width);
        await VacancyWizardE2e.ShotAsync(page, viewportTag, "06-step-rooster");

        await page.GetByRole(AriaRole.Button, new() { Name = "Volgende: Eisen & contact" }).ClickAsync();
        await page.Locator("[data-testid='vac-wizard-step-5']").WaitForAsync();
        await VacancyWizardE2e.ShotAsync(page, viewportTag, "07-step-eisen");

        await page.GetByRole(AriaRole.Button, new() { Name = "Volgende: Controleren" }).ClickAsync();
        await page.Locator("[data-testid='vac-wizard-step-6']").WaitForAsync();
        await VacancyWizardE2e.ShotAsync(page, viewportTag, "08-step-controleren");

        await page.Locator("[data-testid='vacancy-save-draft-submit']:visible").ClickAsync();
        var moderation = page.Locator(".moderation-modal");
        try
        {
            await moderation.WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 3_000 });
            await page.GetByRole(AriaRole.Button, new() { Name = "Tekst aanpassen" }).ClickAsync();
            Assert.Fail("Vacancy description triggered moderation during E2E save.");
        }
        catch (TimeoutException)
        {
            // expected — no moderation block
        }

        var savedSection = page.Locator("[data-testid='vacancy-draft-saved']");
        await savedSection.WaitForAsync(new() { Timeout = 120_000 });
        var savedIdRaw = await savedSection.GetAttributeAsync("data-vacancy-id");
        Assert.False(string.IsNullOrWhiteSpace(savedIdRaw));
        Assert.True(Guid.TryParse(savedIdRaw, out var savedId));
        vacancyId = savedId;
        await VacancyWizardE2e.ShotAsync(page, viewportTag, "09-concept-opgeslagen");

        await page.Locator("[data-testid='vacancy-publish-now']").ClickAsync();
        await page.Locator("[data-testid='publish-confirm']").WaitForAsync();
        await page.Locator("[data-testid='publish-confirm']").ClickAsync();
        await page.Locator("[data-testid='publish-confirm']").WaitForAsync(new() { State = WaitForSelectorState.Hidden, Timeout = 60_000 });
        await page.WaitForTimeoutAsync(2000);

        await page.GotoAsync(baseUrl + "/werkgever/vacatures", new() { WaitUntil = WaitUntilState.DOMContentLoaded });
        await page.Locator(".wg-vac__title").First.WaitForAsync(new() { Timeout = 60_000 });
        var searchBox = page.Locator(".ent-filter-bar input[type=search], .ent-filter-bar input").First;
        if (await searchBox.CountAsync() > 0)
        {
            await searchBox.FillAsync(title);
            await searchBox.PressAsync("Enter");
            await page.WaitForTimeoutAsync(800);
        }

        var rowLink = page.Locator($".wg-vac__title:has-text('{title}')").First;
        await rowLink.WaitForAsync(new() { Timeout = 90_000 });
        var href = await rowLink.GetAttributeAsync("href");
        Assert.False(string.IsNullOrWhiteSpace(href));
        Assert.Contains(savedId.ToString("D"), href, StringComparison.OrdinalIgnoreCase);
        vacancyId = savedId;

        var row = page.Locator("tr").Filter(new() { Has = rowLink });
        await Expect(row.Locator(".wg-vac-status")).ToContainTextAsync("Actief", new() { Timeout = 30_000 });
        await VacancyWizardE2e.ShotAsync(page, viewportTag, "10-werkgever-lijst-gepubliceerd");

        await page.GotoAsync($"{baseUrl}/vacancies/{vacancyId:D}", new() { WaitUntil = WaitUntilState.DOMContentLoaded });
        await page.Locator(".vacancy-description-html").WaitForAsync();
        await AssertPublicDescriptionFormattingAsync(page);
        await VacancyWizardE2e.ShotAsync(page, viewportTag, "11-kandidaat-publieke-pagina");

        await WaitForVacancyInDiscoveryIndexAsync(baseUrl, vacancyId!.Value, title);
        await page.GotoAsync(
            $"{baseUrl}{E2eRoutes.Banenkaart}?weergave=lijst&q={Uri.EscapeDataString(title)}",
            new() { WaitUntil = WaitUntilState.DOMContentLoaded });
        await KbE2e.WaitForStyledAppAsync(page);
        var banenkaartLink = page.Locator($"a[href*='/vacancies/{vacancyId:D}']").First;
        if (await banenkaartLink.CountAsync() > 0)
        {
            await banenkaartLink.WaitForAsync(new() { Timeout = 30_000 });
        }

        await VacancyWizardE2e.ShotAsync(page, viewportTag, "12-banenkaart-lijst");

        await page.GotoAsync($"{baseUrl}/werkgever/vacatures/nieuw?edit={vacancyId:D}",
            new() { WaitUntil = WaitUntilState.DOMContentLoaded });
        await page.Locator("[data-testid='vacancy-wizard']").WaitForAsync();
        var titleInput = page.Locator("[data-testid='vac-wizard-step-1'] input[required]");
        await titleInput.WaitForAsync();
        await page.WaitForFunctionAsync(
            """
            (expected) => {
              const el = document.querySelector("[data-testid='vac-wizard-step-1'] input[required]");
              return el && el.value && el.value.includes(expected);
            }
            """,
            title.Split(' ')[0],
            new() { Timeout = 60_000 });
        await titleInput.FillAsync(editedTitle);
        await page.Locator("[data-testid='vacancy-save-draft']:visible").ClickAsync();
        await page.Locator("[data-testid='vacancy-draft-saved']").WaitForAsync(new() { Timeout = 120_000 });
        await VacancyWizardE2e.ShotAsync(page, viewportTag, "13-bewerken-opgeslagen");

        await page.GotoAsync($"{baseUrl}/werkgever/vacatures/nieuw?edit={vacancyId:D}",
            new() { WaitUntil = WaitUntilState.DOMContentLoaded });
        await titleInput.WaitForAsync();
        await Expect(titleInput).ToHaveValueAsync(editedTitle, new() { Timeout = 30_000 });
        await VacancyWizardE2e.ShotAsync(page, viewportTag, "14-bewerken-herladen");
    }

    [Theory]
    [InlineData(1440, 900)]
    [InlineData(390, 844)]
    public async Task Vacancy_wizard_step_navigation_and_actionbar(int width, int height)
    {
        if (VacancyWizardE2e.ShouldRun)
        {
            // Full E2E run covers navigation; avoid duplicate parallel browser load.
            return;
        }

        var baseUrl = (Environment.GetEnvironmentVariable("JOBSY_E2E_BASE_URL") ?? "").Trim().TrimEnd('/');
        if (string.IsNullOrWhiteSpace(baseUrl))
        {
            return;
        }

        await EmployersPlaywrightGuard.SkipIfEmployersOffAsync(baseUrl);

        var email = (Environment.GetEnvironmentVariable("JOBSY_E2E_BM_EMAIL")
                     ?? Environment.GetEnvironmentVariable("JOBSY_E2E_EMPLOYER_EMAIL")
                     ?? VacancyWizardE2e.EmployerEmail).Trim();
        var password = (Environment.GetEnvironmentVariable("JOBSY_E2E_BM_PASSWORD")
                        ?? Environment.GetEnvironmentVariable("JOBSY_E2E_EMPLOYER_PASSWORD")
                        ?? VacancyWizardE2e.EmployerPassword).Trim();

        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new() { Headless = true });
        await using var context = await browser.NewContextAsync(new()
        {
            ViewportSize = new() { Width = width, Height = height },
            IgnoreHTTPSErrors = true
        });
        var page = await context.NewPageAsync();

        Assert.True(await KbE2e.TryLoginAsync(page, baseUrl, email, password));
        await page.GotoAsync(baseUrl + "/werkgever/vacatures/nieuw",
            new() { WaitUntil = WaitUntilState.DOMContentLoaded, Timeout = 90_000 });

        await page.Locator("[data-testid='vacancy-wizard']").WaitForAsync(new() { Timeout = 30_000 });
        await page.Locator("[data-testid='vacancy-preview-actionbar']:visible").WaitForAsync();
        var toolbarColors = page.Locator(".vac-wizard-richtext__color");
        await FillStep1Async(page, "Playwright testvacature");
        await page.GetByRole(AriaRole.Button, new() { Name = "Volgende: Omschrijving" }).ClickAsync();
        await page.Locator("[data-testid='vac-wizard-step-2']").WaitForAsync();
        Assert.True(await toolbarColors.CountAsync() >= 5);
    }

    private static async Task FillStep1Async(IPage page, string title)
    {
        await page.Locator("[data-testid='vac-wizard-step-1'] input[required]").FillAsync(title);
        await SelectFirstCategoryAsync(page);
        var contractTiles = page.Locator("[data-testid='vac-wizard-step-1'] fieldset .lobsy-choice-tile");
        if (await contractTiles.CountAsync() > 0)
        {
            await contractTiles.First.ClickAsync();
        }

        await page.Locator("[data-testid='vac-wizard-step-1'] fieldset:has(legend:has-text('Branche')) .lobsy-choice-tile")
            .First.ClickAsync();
    }

    private static async Task SelectFirstCategoryAsync(IPage page)
    {
        var picker = page.Locator("[data-testid='vacancy-category-picker'] .lobsy-choice-tile");
        if (await picker.CountAsync() > 0)
        {
            await picker.First.ClickAsync();
            await page.WaitForTimeoutAsync(400);
            return;
        }

        var category = page.Locator("[data-testid='vac-wizard-step-1'] select").First;
        if (await category.CountAsync() == 0)
        {
            return;
        }

        await category.WaitForAsync();
        await page.WaitForFunctionAsync(
            """
            () => {
              const sel = document.querySelector("[data-testid='vac-wizard-step-1'] select");
              if (!sel) return true;
              return [...sel.options].some(o => o.value && !/laden/i.test(o.textContent || ""));
            }
            """,
            null,
            new() { Timeout = 60_000 });

        var options = category.Locator("option");
        var count = await options.CountAsync();
        for (var i = 0; i < count; i++)
        {
            var value = await options.Nth(i).GetAttributeAsync("value");
            if (!string.IsNullOrWhiteSpace(value))
            {
                await category.SelectOptionAsync(new SelectOptionValue { Index = i });
                await page.WaitForTimeoutAsync(400);
                return;
            }
        }
    }

    private static async Task FillStep2RichTextAsync(IPage page)
    {
        var area = page.Locator("[data-testid='vacancy-description-input']");
        await page.GetByRole(AriaRole.Button, new() { Name = "Kop 2" }).ClickAsync();
        await page.GetByRole(AriaRole.Button, new() { Name = "Koraal" }).ClickAsync();

        const string html = "<h2>E2E Kop</h2><p><span style=\"color:#f54a1b\">kleuraccent</span> — intro voor kandidaten.</p>";
        await area.FillAsync(html);
        await page.WaitForTimeoutAsync(400);

        var value = await area.InputValueAsync();
        Assert.Contains("<h2>", value, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("#f54a1b", value, StringComparison.OrdinalIgnoreCase);
    }

    private static async Task FillStep3Async(IPage page)
    {
        var select = page.Locator("[data-testid='vac-wizard-step-3'] select").First;
        await select.WaitForAsync();
        var options = select.Locator("option");
        var count = await options.CountAsync();
        for (var i = 0; i < count; i++)
        {
            var value = await options.Nth(i).GetAttributeAsync("value");
            if (!string.IsNullOrWhiteSpace(value))
            {
                await select.SelectOptionAsync(new SelectOptionValue { Index = i });
                break;
            }
        }

        await page.Locator("[data-testid='vac-wizard-step-3'] .salary-table-preview").WaitForAsync(new() { Timeout = 30_000 });
    }

    private static async Task FillStep4ScheduleAsync(IPage page, int width)
    {
        var flex = page.Locator("label:has-text('Tijden in overleg') input[type=checkbox]").First;
        if (await flex.IsCheckedAsync())
        {
            await flex.UncheckAsync();
        }

        if (width >= 900)
        {
            await page.Locator(".vac-wizard-schedule__desktop input[type=checkbox]:not(:disabled)").First.CheckAsync();
        }
        else
        {
            await page.Locator(".vac-wizard-schedule__mobile .lobsy-choice-tile:not([disabled])").First.ClickAsync();
        }
    }

    private static async Task AssertPreviewShowsDraftAsync(
        IPage page,
        string viewportTag,
        string shotSlug,
        string title,
        bool expectFormattedDescription)
    {
        await page.Locator("[data-testid='vacancy-preview-actionbar']:visible").ClickAsync();
        var overlay = page.Locator("[data-testid='vacancy-candidate-preview-overlay']");
        await overlay.WaitForAsync();
        await page.Locator("[data-testid='kb-detail-preview']").WaitForAsync();
        await Expect(page.Locator("[data-testid='kb-detail-preview'] h1")).ToContainTextAsync(title);
        if (expectFormattedDescription)
        {
            var html = await page.Locator("[data-testid='kb-detail-preview'] .vacancy-description-html").InnerHTMLAsync();
            Assert.Contains("<h2", html, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("#f54a1b", html, StringComparison.OrdinalIgnoreCase);
        }

        await VacancyWizardE2e.ShotAsync(page, viewportTag, shotSlug);
        await page.Locator(".vac-candidate-preview-panel__close").ClickAsync();
        await overlay.WaitForAsync(new() { State = WaitForSelectorState.Hidden });

        var descInput = page.Locator("[data-testid='vacancy-description-input']");
        if (await descInput.IsVisibleAsync())
        {
            var desc = await descInput.InputValueAsync();
            Assert.Contains("#f54a1b", desc, StringComparison.OrdinalIgnoreCase);
        }
        else
        {
            await Expect(page.Locator("[data-testid^='vac-wizard-step-']")).ToBeVisibleAsync();
        }
    }

    private static async Task AssertPublicDescriptionFormattingAsync(IPage page)
    {
        var html = await page.Locator(".vacancy-description-html").InnerHTMLAsync();
        Assert.Contains("<h2", html, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("#f54a1b", html, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("<script", html, StringComparison.OrdinalIgnoreCase);
    }

    private static async Task WaitForVacancyInDiscoveryIndexAsync(string baseUrl, Guid vacancyId, string title)
    {
        var api = baseUrl.Replace(":5201", ":5200", StringComparison.Ordinal).TrimEnd('/');
        using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(15) };
        var idText = vacancyId.ToString("D");
        for (var attempt = 0; attempt < 45; attempt++)
        {
            try
            {
                var json = await http.GetStringAsync(
                    $"{api}/api/vacancies/discover?q={Uri.EscapeDataString(title)}&limit=50");
                using var doc = JsonDocument.Parse(json);
                if (doc.RootElement.TryGetProperty("items", out var items)
                    && items.ValueKind == JsonValueKind.Array)
                {
                    foreach (var item in items.EnumerateArray())
                    {
                        if (item.TryGetProperty("id", out var idProp)
                            && string.Equals(idProp.GetString(), idText, StringComparison.OrdinalIgnoreCase))
                        {
                            return;
                        }

                        if (item.TryGetProperty("title", out var titleProp)
                            && string.Equals(titleProp.GetString(), title, StringComparison.Ordinal))
                        {
                            return;
                        }
                    }
                }
            }
            catch (HttpRequestException)
            {
            }
            catch (TaskCanceledException)
            {
            }

            await Task.Delay(3000);
        }

        Assert.Fail($"Vacancy {idText} not visible in discovery index within timeout.");
    }

}

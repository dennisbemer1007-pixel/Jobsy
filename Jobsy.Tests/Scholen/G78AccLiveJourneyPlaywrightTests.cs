using Microsoft.Playwright;

namespace Jobsy.Tests.Scholen;

/// <summary>
/// Live Acceptatie G78 journey with real clicks. Run:
/// JOBSY_G78_ACC_LIVE=1 JOBSY_ACC_PUPIL_CODE=VKJ-7UU
/// JOBSY_ACC_PUPIL_SCHOOL_ID=99999999-1111-1111-1111-111111111111
/// JOBSY_ACC_PUPIL_CLASS_ID=897a3406-5f03-4b94-8790-60ca17bfc16d
/// JOBSY_WALKTHROUGH_ARTIFACTS=/opt/cursor/artifacts/g78-acc-journey
/// dotnet test --filter G78_acc_live_journey
/// </summary>
[Collection("PlaywrightSmoke")]
public sealed class G78AccLiveJourneyPlaywrightTests
{
    private const string BaseUrl = "https://acceptatie.lobsy.nl";

    [Fact(Timeout = 600_000, DisplayName = "G78_acc_live_journey")]
    public async Task G78_acc_live_journey_real_clicks()
    {
        _ = TestContext.Current.CancellationToken;
        if (!string.Equals(Environment.GetEnvironmentVariable("JOBSY_G78_ACC_LIVE"), "1", StringComparison.Ordinal))
        {
            return;
        }

        var code = Environment.GetEnvironmentVariable("JOBSY_ACC_PUPIL_CODE") ?? "VKJ-7UU";
        var schoolId = Environment.GetEnvironmentVariable("JOBSY_ACC_PUPIL_SCHOOL_ID")
                         ?? "99999999-1111-1111-1111-111111111111";
        var classId = Environment.GetEnvironmentVariable("JOBSY_ACC_PUPIL_CLASS_ID")
                      ?? "897a3406-5f03-4b94-8790-60ca17bfc16d";
        var artifactDir = Environment.GetEnvironmentVariable("JOBSY_WALKTHROUGH_ARTIFACTS")
                          ?? "/opt/cursor/artifacts/g78-acc-journey";
        var storeDir = "/cursor/stores/self/g78-acc-journey";
        Directory.CreateDirectory(artifactDir);
        Directory.CreateDirectory(storeDir);

        _ = Microsoft.Playwright.Program.Main(["install", "chromium"]);
        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new() { Headless = true });

        var consoleErrors = new List<string>();
        var badHttp = new List<string>();

        foreach (var (width, height, tag) in new[] { (1440, 900, "1440"), (390, 844, "390") })
        {
            await using var context = await browser.NewContextAsync(new()
            {
                ViewportSize = new() { Width = width, Height = height }
            });
            var page = await context.NewPageAsync();
            page.SetDefaultTimeout(120_000);
            page.Console += (_, msg) =>
            {
                if (msg.Type is "error" or "warning")
                {
                    consoleErrors.Add($"{tag}: {msg.Text}");
                }
            };
            page.Response += (_, resp) =>
            {
                var u = resp.Url;
                if (resp.Status >= 400 && u.Contains("lobsy.nl", StringComparison.OrdinalIgnoreCase)
                    && !u.Contains("favicon", StringComparison.OrdinalIgnoreCase))
                {
                    badHttp.Add($"{tag}: {resp.Status} {u}");
                }
            };

            await LoginAsync(page, schoolId, classId, code);
            await ShotAsync(page, artifactDir, storeDir, $"{tag}-01-login");

            if (page.Url.Contains("/wereld", StringComparison.OrdinalIgnoreCase))
            {
                var box = await page.Locator(".same svg").First.BoundingBoxAsync();
                Assert.NotNull(box);
                Assert.InRange(box!.Width, 24, 40);
                await page.Locator("[data-testid=ll-wereld-zee]").ClickAsync();
                await page.WaitForURLAsync("**/leerling/start**", new() { Timeout = 60_000 });
                await ShotAsync(page, artifactDir, storeDir, $"{tag}-02-start");
                await page.Locator("a.rk-go, a.btn-primary").First.ClickAsync();
                await page.WaitForURLAsync("**/leerling/reis**", new() { Timeout = 60_000 });
            }

            for (var guard = 0; guard < 100; guard++)
            {
                var url = page.Url;
                if (url.Contains("/paspoort", StringComparison.OrdinalIgnoreCase)
                    || url.Contains("/dit-ben-jij", StringComparison.OrdinalIgnoreCase))
                {
                    break;
                }

                if (url.Contains("/puzzel/", StringComparison.OrdinalIgnoreCase))
                {
                    await ShotAsync(page, artifactDir, storeDir, $"{tag}-puzzle-{guard}");
                    await SolvePuzzleAsync(page);
                    continue;
                }

                if (url.Contains("/vakken", StringComparison.OrdinalIgnoreCase))
                {
                    await ShotAsync(page, artifactDir, storeDir, $"{tag}-vakken-{guard}");
                    await FillVakkenAsync(page);
                    continue;
                }

                if (url.Contains("/eiland", StringComparison.OrdinalIgnoreCase))
                {
                    await ShotAsync(page, artifactDir, storeDir, $"{tag}-eiland-{guard}");
                    await page.Locator("[data-ll-chip-like]").First.ClickAsync();
                    await page.Locator("[data-ll-island-next]").ClickAsync();
                    await page.Locator("[data-ll-chip-dislike]").First.ClickAsync();
                    await page.Locator("[data-testid=ll-island-form] button[type=\"submit\"].btn-main").ClickAsync();
                    continue;
                }

                if (url.Contains("/reis", StringComparison.OrdinalIgnoreCase))
                {
                    var scale = page.Locator("form[data-ll-answer-scale] [data-ll-answer-value]").First;
                    if (await scale.CountAsync() == 0)
                    {
                        break;
                    }

                    await scale.ClickAsync();
                    await page.WaitForLoadStateAsync(LoadState.DOMContentLoaded);
                    continue;
                }

                break;
            }

            await ShotAsync(page, artifactDir, storeDir, $"{tag}-final");
            var scripts = await page.EvaluateAsync<string[]>(
                "() => [...document.querySelectorAll('script[src]')].map(s => s.getAttribute('src') || '')");
            Assert.True(
                scripts.Any(s => s.Contains("leerling-journey-ssr.js?v=", StringComparison.Ordinal)),
                "Deploy journey script with ?v= cache bust (this PR) before expecting green on Acceptatie. Scripts: "
                + string.Join(", ", scripts));
        }

        var badConsole = consoleErrors
            .Where(e => !e.Contains("favicon", StringComparison.OrdinalIgnoreCase))
            .ToList();
        Assert.True(badHttp.Count == 0, string.Join("; ", badHttp));
        Assert.True(badConsole.Count == 0, string.Join("; ", badConsole));
    }

    private static async Task LoginAsync(IPage page, string schoolId, string classId, string code)
    {
        await page.GotoAsync($"{BaseUrl}/leerling?schoolId={schoolId}&classId={classId}");
        await page.SelectOptionAsync("select[name=classId]", classId);
        await page.FillAsync("input[name=code]", code);
        await page.Locator("form:has(input[name=code]) button[type=submit]").ClickAsync();
        await page.WaitForLoadStateAsync(LoadState.DOMContentLoaded);
    }

    private static async Task SolvePuzzleAsync(IPage page)
    {
        if (await page.Locator("[data-ll-puzzle-start]").CountAsync() > 0)
        {
            await page.Locator("[data-ll-puzzle-start]").ClickAsync();
        }

        if (await page.Locator("[data-ll-schelp-choice]").CountAsync() > 0)
        {
            var answer = await page.EvaluateAsync<string>(
                "() => document.querySelector('.ll-puzzle-g78')?.getAttribute('data-ll-puzzle-answer') || ''");
            await page.Locator($"[data-ll-schelp-choice=\"{answer}\"]").First.ClickAsync();
        }
        else if (await page.Locator("[data-ll-lamp-index]").CountAsync() > 0)
        {
            await page.WaitForTimeoutAsync(9000);
            var pattern = await page.EvaluateAsync<string>(
                "() => document.querySelector('[data-ll-puzzle-play-section]')?.getAttribute('data-ll-lamp-pattern') || ''");
            Assert.False(string.IsNullOrWhiteSpace(pattern), "Vuurtorenlampen pattern missing — is leerling-journey-ssr.js loaded and current?");

            foreach (var part in pattern.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                await page.Locator($"[data-ll-lamp-index=\"{part}\"]").ClickAsync();
                await page.WaitForTimeoutAsync(550);
            }
        }
        else if (await page.Locator("[data-ll-card-index]").CountAsync() > 0)
        {
            var cards = page.Locator("[data-ll-card-index]");
            var count = await cards.CountAsync();
            for (var attempt = 0; attempt < 30; attempt++)
            {
                if (await page.Locator("[data-ll-puzzle-continue]:not([disabled])").CountAsync() > 0)
                {
                    break;
                }

                for (var i = 0; i < count; i++)
                {
                    await cards.Nth(i).ClickAsync();
                    await page.WaitForTimeoutAsync(350);
                }
            }
        }

        await page.Locator("[data-ll-puzzle-continue]:not([disabled])").ClickAsync(new() { Timeout = 60_000 });
        await page.WaitForLoadStateAsync(LoadState.DOMContentLoaded);
    }

    private static async Task FillVakkenAsync(IPage page)
    {
        var groups = page.Locator(".ll-vt-sc");
        var n = await groups.CountAsync();
        for (var i = 0; i < n; i++)
        {
            await groups.Nth(i).Locator(".ll-vt-scale-btn").Nth(2).ClickAsync();
            await groups.Nth(i).Locator(".ll-vt-scale-btn").Last.ClickAsync();
        }

        await page.WaitForFunctionAsync(
            "() => { const b = document.querySelector('[data-testid=ll-vt-next]'); return b && !b.disabled; }");
        await page.Locator("[data-testid=ll-vt-next]").ClickAsync();
        await page.WaitForLoadStateAsync(LoadState.DOMContentLoaded);
    }

    private static async Task ShotAsync(IPage page, string artifactDir, string storeDir, string name)
    {
        var file = $"{name}.png";
        await page.ScreenshotAsync(new() { Path = Path.Combine(artifactDir, file), FullPage = true });
        await page.ScreenshotAsync(new() { Path = Path.Combine(storeDir, file), FullPage = true });
    }
}

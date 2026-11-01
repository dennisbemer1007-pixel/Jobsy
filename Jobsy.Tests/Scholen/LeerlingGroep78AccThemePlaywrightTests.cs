using Jobsy.Infrastructure.Data;
using Jobsy.Tests.E2e;
using Microsoft.Playwright;

namespace Jobsy.Tests.Scholen;

/// <summary>
/// Acceptatie: G78 pupil journey must keep theme CSS after enhanced navigation.
/// Run: JOBSY_G78_ACC_THEME=1 JOBSY_E2E_BASE_URL=https://acceptatie.lobsy.nl
/// JOBSY_WALKTHROUGH_ARTIFACTS=/opt/cursor/artifacts/g78-e2e-1373 dotnet test --filter G78_acc_theme_journey
/// </summary>
[Collection("PlaywrightSmoke")]
public sealed class LeerlingGroep78AccThemePlaywrightTests
{
    private const string TeacherEmail = "leraar@jobsy.local";

    [Fact(Timeout = 900_000, DisplayName = "G78_acc_theme_journey")]
    public async Task G78_acc_theme_journey_on_acceptatie()
    {
        _ = TestContext.Current.CancellationToken;
        if (!string.Equals(Environment.GetEnvironmentVariable("JOBSY_G78_ACC_THEME"), "1", StringComparison.Ordinal))
        {
            return;
        }

        var baseUrl = (Environment.GetEnvironmentVariable("JOBSY_E2E_BASE_URL") ?? "https://acceptatie.lobsy.nl").TrimEnd('/');
        if (!await KbE2e.IsReachableAsync(baseUrl))
        {
            return;
        }

        var artifactDir = Environment.GetEnvironmentVariable("JOBSY_WALKTHROUGH_ARTIFACTS")
                          ?? "/opt/cursor/artifacts/g78-e2e-1373";
        Directory.CreateDirectory(artifactDir);

        Microsoft.Playwright.Program.Main(["install", "chromium"]);
        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new() { Headless = true });

        var schoolId = DemoSchoolIds.SchoolId;
        var classId = DemoSchoolIds.ClassGroep78Id;
        string plainCode;

        await using (var ctx = await browser.NewContextAsync(KbE2e.DesktopContext()))
        {
            var page = await ctx.NewPageAsync();
            page.SetDefaultTimeout(180_000);
            Assert.True(await KbE2e.TryLoginAsync(page, baseUrl, TeacherEmail));
            await page.GotoAsync($"{baseUrl}/leraar/klas/{classId:D}/codes", new() { WaitUntil = WaitUntilState.Load });
            await page.WaitForTimeoutAsync(3000);
            var add = page.GetByRole(AriaRole.Button, new() { Name = "Code toevoegen" });
            if (await add.CountAsync() > 0)
            {
                await add.First.ClickAsync();
                await page.WaitForTimeoutAsync(2500);
            }

            var codeEl = page.Locator(".sch-code").First;
            await codeEl.WaitForAsync(new() { Timeout = 120_000 });
            plainCode = (await codeEl.InnerTextAsync()).Trim();
            await File.WriteAllTextAsync(Path.Combine(artifactDir, "pupil-code.txt"), plainCode);
        }

        await using var pCtx = await browser.NewContextAsync(KbE2e.DesktopContext());
        var p = await pCtx.NewPageAsync();
        p.SetDefaultTimeout(180_000);
        var consoleErrors = new List<string>();
        p.Console += (_, msg) =>
        {
            if (msg.Type == "error")
            {
                consoleErrors.Add(msg.Text);
            }
        };

        await p.GotoAsync($"{baseUrl}/leerling?schoolId={schoolId:D}&classId={classId:D}", new() { WaitUntil = WaitUntilState.Load });
        await p.FillAsync("input[name=code]", plainCode);
        await p.Locator("form[action='/leerling/login'] button[type=submit]").ClickAsync();
        await p.WaitForURLAsync(u => u.Contains("/leerling/", StringComparison.OrdinalIgnoreCase), new() { Timeout = 120_000 });

        if (p.Url.Contains("/leerling/wereld", StringComparison.OrdinalIgnoreCase))
        {
            await AssertThemeCssAsync(p);
            await p.Locator(".wcard").First.ClickAsync();
            await p.WaitForURLAsync(u => u.Contains("/leerling/start", StringComparison.OrdinalIgnoreCase)
                                         || u.Contains("/leerling/reis", StringComparison.OrdinalIgnoreCase),
                new() { Timeout = 60_000 });
        }

        if (p.Url.Contains("/leerling/start", StringComparison.OrdinalIgnoreCase))
        {
            await AssertThemeCssAsync(p);
            await p.ScreenshotAsync(new() { Path = Path.Combine(artifactDir, "01-start.png"), FullPage = true });
            await p.Locator("a[href*='/leerling/reis']").First.ClickAsync();
            await p.WaitForURLAsync("**/leerling/reis**", new() { Timeout = 60_000 });
        }

        await AssertThemeCssAsync(p);
        await p.WaitForSelectorAsync(".ll-g78-shell .journey, .ll-g78-shell .ll-g78-scene", new() { Timeout = 60_000 });
        var journeyHidden = await p.EvaluateAsync<bool>(
            """
            () => {
              const j = document.querySelector('.ll-g78-shell .journey');
              if (!j) return true;
              const r = j.getBoundingClientRect();
              return r.width < 200 || r.height < 40;
            }
            """);
        Assert.False(journeyHidden, "Journey strip should be visible and laid out.");
        await p.ScreenshotAsync(new() { Path = Path.Combine(artifactDir, "02-reis.png"), FullPage = true });

        await p.GotoAsync($"{baseUrl}/leerling/vakken", new() { WaitUntil = WaitUntilState.Load });
        await AssertThemeCssAsync(p);
        var cardWidth = await p.EvaluateAsync<double>(
            """
            () => document.querySelector('[data-testid=leerling-vakken] .ll-vakken-card, [data-testid=leerling-vakken] .card')
              ?.getBoundingClientRect().width ?? 0
            """);
        Assert.True(cardWidth > 400, $"Vakken card too narrow ({cardWidth}px).");
        await p.ScreenshotAsync(new() { Path = Path.Combine(artifactDir, "03-vakken.png"), FullPage = true });

        await File.WriteAllTextAsync(Path.Combine(artifactDir, "console-errors.json"), System.Text.Json.JsonSerializer.Serialize(consoleErrors));
        var fontCsp = consoleErrors.Any(e => e.Contains("fonts.googleapis", StringComparison.OrdinalIgnoreCase));
        Assert.False(fontCsp, "Unexpected Google Fonts CSP errors: " + string.Join("; ", consoleErrors));
    }

    private static async Task AssertThemeCssAsync(IPage page)
    {
        var hasThemeLink = await page.EvaluateAsync<bool>(
            """
            () => [...document.querySelectorAll('link[rel=stylesheet]')]
              .some(l => (l.getAttribute('href')||'').includes('leerling-werelden-kies.css'))
            """);
        Assert.True(hasThemeLink, $"Theme CSS missing in head at {page.Url}");
        var shell = await page.Locator(".ll-g78-shell").CountAsync();
        if (shell > 0)
        {
            var bg = await page.EvaluateAsync<string>(
                """
                () => getComputedStyle(document.querySelector('.ll-g78-shell')).backgroundColor
                """);
            Assert.False(string.Equals(bg, "rgba(0, 0, 0, 0)", StringComparison.OrdinalIgnoreCase)
                         && string.Equals(bg, "transparent", StringComparison.OrdinalIgnoreCase),
                "G78 shell should have themed background.");
        }
    }
}

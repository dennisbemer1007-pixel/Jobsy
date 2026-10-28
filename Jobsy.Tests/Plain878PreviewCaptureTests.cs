using System.Text;
using Microsoft.Playwright;

namespace Jobsy.Tests;

/// <summary>JOBSY_PLAIN_MOCKUP_CAPTURE=1 dotnet test --filter Plain_878_preview_capture</summary>
public sealed class Plain878PreviewCaptureTests : IAsyncLifetime, IAsyncDisposable
{
    private static readonly (string RepoPath, string ArtifactName)[] MockupSources =
    [
        ("docs/mockups/kandidaat/plain/renders/desktop/20-paspoort-desktop-1440.png", "mockup-paspoort-1440.png"),
        ("docs/mockups/kandidaat/plain/renders/mobiel/20-paspoort-mobiel-390.png", "mockup-paspoort-390.png"),
        ("docs/mockups/kandidaat/banenkaart/lagen/desktop/02-banenkaart-lagen-desktop-1440.png", "mockup-banenkaart-1440.png"),
        ("docs/mockups/kandidaat/banenkaart/lagen/mobile/01-banenkaart-lagen-mobiel-390.png", "mockup-banenkaart-390.png"),
    ];

    private IllustratedPagesWebFactory? _web;

    public async ValueTask InitializeAsync()
    {
        if (Environment.GetEnvironmentVariable("JOBSY_PLAIN_MOCKUP_CAPTURE") != "1")
        {
            return;
        }

        _web = new IllustratedPagesWebFactory(plainSkin: true);
        await Task.CompletedTask;
    }

    public async ValueTask DisposeAsync()
    {
        if (_web is not null)
        {
            await _web.DisposeAsync();
        }
    }

    [Fact]
    public async Task Plain_878_preview_capture()
    {
        if (Environment.GetEnvironmentVariable("JOBSY_PLAIN_MOCKUP_CAPTURE") != "1")
        {
            return;
        }

        Assert.NotNull(_web);
        var baseUrl = _web!.ServerAddress;
        var artifactDir = Environment.GetEnvironmentVariable("JOBSY_PLAIN_ARTIFACT_DIR")
            ?? "/opt/cursor/artifacts";
        Directory.CreateDirectory(artifactDir);
        CopyMockupRenders(artifactDir);

        _ = Microsoft.Playwright.Program.Main(["install", "chromium"]);
        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new() { Headless = true });

        var reportLines = new List<string>
        {
            "# Plain878 layout-gap rapport",
            "",
            "Drempels: lege strook ≤ 12px, ruimte onder header tot inhoud ≤ 32px.",
            "",
        };
        var failures = new List<string>();

        await using (var refContext = await browser.NewContextAsync(new()
        {
            ViewportSize = new() { Width = 1440, Height = 900 },
        }))
        {
            await refContext.AddInitScriptAsync(CookieInitScript);
            var refPage = await refContext.NewPageAsync();
            await refPage.GotoAsync(
                $"{baseUrl}/__test/sign-in?role=employer&return=%2Fwerkgever",
                new() { Timeout = 60_000 });
            await refPage.WaitForURLAsync("**/werkgever**", new() { Timeout = 60_000 });
            await refPage.WaitForSelectorAsync("header.wg-topbar:not(.wg-topbar--candidate)", new() { Timeout = 30_000 });
            await DismissCookieBannerIfVisibleAsync(refPage);
            await refPage.Locator("header.wg-topbar").First.ScreenshotAsync(new()
            {
                Path = Path.Combine(artifactDir, "reference-werkgever-header-1440.png"),
            });
        }

        const string bkQuery =
            "/banenkaart?maxMinutes=60&transport=Auto&originLat=52.0705&originLng=4.3007&originLabel=Naaldwijk";

        foreach (var (width, height, mobile, tag) in new[]
                 {
                     (1440, 900, false, "1440"),
                     (390, 844, true, "390"),
                 })
        {
            await using var authContext = await browser.NewContextAsync(new()
            {
                ViewportSize = new() { Width = width, Height = height },
                IsMobile = mobile,
                HasTouch = mobile,
            });
            await authContext.AddInitScriptAsync(CookieInitScript);
            var authPage = await authContext.NewPageAsync();
            var ret = Uri.EscapeDataString("/candidate/paspoort");
            await authPage.GotoAsync($"{baseUrl}/__test/sign-in?return={ret}", new() { Timeout = 60_000 });
            await authPage.WaitForURLAsync("**/candidate/paspoort**", new() { Timeout = 60_000 });
            await authPage.WaitForSelectorAsync(".cp-plain878 main.wrap", new() { Timeout = 30_000 });
            await DismissCookieBannerIfVisibleAsync(authPage);
            await Assertions.Expect(authPage.Locator("header.wg-topbar--candidate").First).ToContainTextAsync("Sam de Wit");

            if (mobile)
            {
                var coachOverlap = await Plain878LayoutGapHelpers.CoachOverlapsPlainPaspoortContentAsync(authPage);
                Assert.False(coachOverlap, "Lobsy-hulp pill mag chips op paspoort 390 niet overlappen (elementFromPoint).");
            }

            var header = authPage.Locator("header.wg-topbar--candidate").First;
            await header.WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 30_000 });
            await header.ScreenshotAsync(new()
            {
                Path = Path.Combine(artifactDir, $"preview-header-{tag}.png"),
            });

            var pasGap = await Plain878LayoutGapHelpers.MeasureAsync(authPage);
            await authPage.ScreenshotAsync(new()
            {
                Path = Path.Combine(artifactDir, $"preview-paspoort-{tag}.png"),
                FullPage = true,
            });
            AppendReport(reportLines, failures, $"preview-paspoort-{tag}.png", pasGap);

            await authPage.GotoAsync(baseUrl + bkQuery, new() { Timeout = 60_000 });
            await authPage.WaitForSelectorAsync("[data-plain-banenkaart=\"1\"]", new() { Timeout = 30_000 });
            Assert.Equal(0, await authPage.Locator("[data-testid=kb-mockup-top]").CountAsync());
            await DismissCookieBannerIfVisibleAsync(authPage);
            await authPage.WaitForTimeoutAsync(1200);
            var bkAuthGap = await Plain878LayoutGapHelpers.MeasureAsync(authPage);
            await authPage.ScreenshotAsync(new()
            {
                Path = Path.Combine(artifactDir, $"preview-banenkaart-{tag}.png"),
                FullPage = false,
            });
            AppendReport(reportLines, failures, $"preview-banenkaart-{tag}.png (ingelogd)", bkAuthGap);

            await using var guestContext = await browser.NewContextAsync(new()
            {
                ViewportSize = new() { Width = width, Height = height },
                IsMobile = mobile,
                HasTouch = mobile,
            });
            await guestContext.AddInitScriptAsync(CookieInitScript);
            var guestPage = await guestContext.NewPageAsync();
            await guestPage.GotoAsync(baseUrl + bkQuery, new() { Timeout = 60_000 });
            await guestPage.WaitForSelectorAsync("[data-plain-banenkaart=\"1\"]", new() { Timeout = 30_000 });
            await guestPage.WaitForSelectorAsync("header.wg-topbar--candidate", new() { Timeout = 30_000 });
            Assert.Equal(0, await guestPage.Locator("[data-testid=kb-mockup-top]").CountAsync());
            await DismissCookieBannerIfVisibleAsync(guestPage);
            await guestPage.WaitForTimeoutAsync(1200);
            var bkGuestGap = await Plain878LayoutGapHelpers.MeasureAsync(guestPage);
            await guestPage.ScreenshotAsync(new()
            {
                Path = Path.Combine(artifactDir, $"preview-banenkaart-guest-{tag}.png"),
                FullPage = false,
            });
            AppendReport(reportLines, failures, $"preview-banenkaart-guest-{tag}.png", bkGuestGap);
        }

        var reportPath = Path.Combine(artifactDir, "plain878-layout-gap-report.md");
        await File.WriteAllTextAsync(reportPath, string.Join('\n', reportLines), Encoding.UTF8);
        Assert.True(failures.Count == 0, string.Join('\n', failures) + "\n\nZie " + reportPath);
    }

    private static void AppendReport(
        List<string> lines,
        List<string> failures,
        string shotName,
        Plain878LayoutGapHelpers.GapReport gap)
    {
        lines.Add($"## {shotName}");
        lines.Add("");
        lines.Add(gap.OneLineSummary);
        lines.Add("");
        lines.Add(gap.Pass ? "Status: **geslaagd**" : "Status: **niet geslaagd** (leeg vlak)");
        lines.Add("");
        if (!gap.Pass)
        {
            failures.Add($"{shotName}: {gap.OneLineSummary}");
        }
    }

    private const string CookieInitScript = """
        try {
          localStorage.setItem('Jobsy.CookieConsent', 'necessary');
          document.documentElement.classList.add('cookie-consent-known');
        } catch (e) {}
        """;

    private static void CopyMockupRenders(string artifactDir)
    {
        var repoRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".."));
        foreach (var (rel, name) in MockupSources)
        {
            var src = Path.Combine(repoRoot, rel);
            if (!File.Exists(src))
            {
                continue;
            }

            File.Copy(src, Path.Combine(artifactDir, name), overwrite: true);
        }
    }

    private static async Task DismissCookieBannerIfVisibleAsync(IPage page)
    {
        var necessary = page.Locator("button[data-consent=\"necessary\"]").First;
        if (await necessary.CountAsync() > 0 && await necessary.IsVisibleAsync())
        {
            await necessary.ClickAsync();
            await page.WaitForTimeoutAsync(200);
        }
    }
}

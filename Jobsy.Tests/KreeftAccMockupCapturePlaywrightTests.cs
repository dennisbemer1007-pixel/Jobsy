using Jobsy.Tests.E2e;
using Microsoft.Playwright;
using Xunit;

namespace Jobsy.Tests;

/// <summary>
/// Kreeft v4 acc layout captures vs docs/mockups/kandidaat/kreeft/renders (PR #878 @ 53863b26).
/// Run: JOBSY_CAPTURE_KREEFT_ACC=1 dotnet test --filter Capture_kreeft_acc_mockups
/// </summary>
[Collection("PlaywrightSmoke")]
public sealed class KreeftAccMockupCapturePlaywrightTests
{
    private const string BaseUrl = "http://127.0.0.1:5201";
    private const string Password = KbE2e.DefaultPassword;
    private const string OutputDir = "/cursor/stores/self/artifacts/kreeft-acc-captures";
    private const string MockupCommit = "53863b26";

    private static readonly (string Slug, string MockupDesk, string MockupMob, string Path, bool Auth)[] Routes =
    [
        ("01-landing", "01-landing-desktop-1440.png", "01-landing-mobiel-390.png", "/", false),
        ("02-inloggen", "02-inloggen-desktop-1440.png", "02-inloggen-mobiel-390.png", "/login", false),
        ("03-start-strand", "03-start-strand-desktop-1440.png", "03-start-strand-mobiel-390.png", "/candidate/start?step=1", true),
        ("15-licht-kompas", "15-licht-kompas-desktop-1440.png", "15-licht-kompas-mobiel-390.png", "/candidate/kompas", true),
        ("16-paspoort", "16-paspoort-desktop-1440.png", "16-paspoort-mobiel-390.png", "/candidate/paspoort", true),
        ("23-sollicitaties", "23-sollicitaties-desktop-1440.png", "23-sollicitaties-mobiel-390.png", "/candidate/applications", true),
        ("32-carriere", "32-carriere-desktop-1440.png", "32-carriere-mobiel-390.png", "/carriere", true),
        ("19-banenkaart", "19-banenkaart-desktop-1440.png", "19-banenkaart-mobiel-390.png", "/banenkaart", true),
        ("41-404", "41-404-desktop-1440.png", "41-404-mobiel-390.png", "/status/404", false),
    ];

    [Fact]
    public async Task Capture_kreeft_acc_mockups()
    {
        if ((Environment.GetEnvironmentVariable("JOBSY_CAPTURE_KREEFT_ACC") ?? "").Trim() != "1")
        {
            return;
        }

        if (!await KbE2e.IsReachableAsync(BaseUrl))
        {
            throw new InvalidOperationException($"Stack not reachable at {BaseUrl}");
        }

        Directory.CreateDirectory(OutputDir);
        await ExtractMockupRendersAsync();

        Microsoft.Playwright.Program.Main(["install", "chromium"]);
        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new() { Headless = true });

        foreach (var route in Routes)
        {
            await CaptureRouteAsync(browser, route);
        }
    }

    private static async Task ExtractMockupRendersAsync()
    {
        var mockupDir = Path.Combine(OutputDir, "mockups");
        Directory.CreateDirectory(mockupDir);
        foreach (var route in Routes)
        {
            var desk = $"docs/mockups/kandidaat/kreeft/renders/desktop/{route.MockupDesk}";
            var mob = $"docs/mockups/kandidaat/kreeft/renders/mobile/{route.MockupMob}";
            await GitShowToFileAsync(MockupCommit, desk, Path.Combine(mockupDir, $"{route.Slug}-mock-1440.png"));
            await GitShowToFileAsync(MockupCommit, mob, Path.Combine(mockupDir, $"{route.Slug}-mock-390.png"));
        }
    }

    private static async Task GitShowToFileAsync(string commit, string repoPath, string dest)
    {
        if (File.Exists(dest) && new FileInfo(dest).Length > 0)
        {
            return;
        }

        var psi = new System.Diagnostics.ProcessStartInfo("git", $"show {commit}:{repoPath}")
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            WorkingDirectory = TestRepo.FindRoot(),
        };
        using var proc = System.Diagnostics.Process.Start(psi);
        if (proc is null)
        {
            return;
        }

        await using var fs = File.Create(dest);
        await proc.StandardOutput.BaseStream.CopyToAsync(fs);
        await proc.WaitForExitAsync();
    }

    private static async Task CaptureRouteAsync(
        IBrowser browser,
        (string Slug, string MockupDesk, string MockupMob, string Path, bool Auth) route)
    {
        var context = await browser.NewContextAsync(new()
        {
            ViewportSize = new ViewportSize { Width = 1440, Height = 900 },
            DeviceScaleFactor = 1,
        });

        var page = await context.NewPageAsync();
        if (route.Auth)
        {
            await CandidateJourneyE2e.EnsureTestAiConsentForE2eAsync(KbE2e.DefaultCompleteEmail);
            Assert.True(await KbE2e.TryLoginAsync(page, BaseUrl, KbE2e.DefaultCompleteEmail, Password));
        }

        await page.GotoAsync($"{BaseUrl}{route.Path}", new() { WaitUntil = WaitUntilState.NetworkIdle, Timeout = 120_000 });
        await page.WaitForTimeoutAsync(800);

        foreach (var (width, height, tag) in new[] { (1440, 900, "1440"), (390, 844, "390") })
        {
            await page.SetViewportSizeAsync(width, height);
            await page.WaitForTimeoutAsync(400);
            await AssertLayoutAsync(page, route, width, tag);
            var file = Path.Combine(OutputDir, $"{route.Slug}-actual-{tag}.png");
            await page.ScreenshotAsync(new() { Path = file, FullPage = false });
        }

        await context.CloseAsync();
    }

    private static async Task AssertLayoutAsync(IPage page, (string Slug, string MockupDesk, string MockupMob, string Path, bool Auth) route, int width, string tag)
    {
        var strip = page.Locator("[data-testid='kreeft-header-strip'], .pub-landing__kreeft-strip, .err-kreeft-strip, .ob-rustig__hero-band").First;
        if (await strip.CountAsync() > 0)
        {
            var box = await strip.BoundingBoxAsync();
            Assert.NotNull(box);
            if (tag == "1440")
            {
                Assert.InRange(box!.Height, 210, 240);
            }
            else
            {
                Assert.InRange(box!.Height, 95, 115);
            }

            var imgs = strip.Locator("img");
            var imgCount = await imgs.CountAsync();
            if (imgCount >= 1)
            {
                var visible = 0;
                for (var i = 0; i < imgCount; i++)
                {
                    if (await imgs.Nth(i).IsVisibleAsync())
                    {
                        visible++;
                        var ib = await imgs.Nth(i).BoundingBoxAsync();
                        Assert.NotNull(ib);
                        Assert.InRange(ib!.Width, box.Width * 0.85, box.Width * 1.05);
                    }
                }

                Assert.Equal(1, visible);
            }
        }

        if (route.Slug is "15-licht-kompas" or "16-paspoort" or "03-start-strand" or "02-inloggen")
        {
            var reis = page.Locator("[data-testid='kreeft-reiskaart']");
            if (await reis.CountAsync() > 0)
            {
                var rb = await reis.First.BoundingBoxAsync();
                Assert.NotNull(rb);
                Assert.True(rb!.Width <= width + 2, $"Reiskaart overflow on {route.Slug} @ {tag}: {rb.Width}px");
            }
        }

        if (route.Auth && route.Slug is "15-licht-kompas" or "16-paspoort" or "23-sollicitaties" or "32-carriere")
        {
            var shell = page.Locator("#lobsy-assistant-ssr-shell");
            if (await shell.CountAsync() > 0)
            {
                Assert.True(await shell.First.IsHiddenAsync());
            }
        }
    }
}

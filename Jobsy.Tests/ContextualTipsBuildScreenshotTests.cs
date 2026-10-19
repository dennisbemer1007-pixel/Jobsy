using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Playwright;

namespace Jobsy.Tests;

[Collection("PlaywrightSmoke")]
public sealed class ContextualTipsBuildScreenshotTests : IAsyncLifetime
{
    private const string ArtifactsDir = "/opt/cursor/artifacts/tips-build-r7";
    private IllustratedPagesWebFactory? _web;
    private IPlaywright? _playwright;
    private IBrowser? _browser;

    public async ValueTask InitializeAsync()
    {
        Environment.SetEnvironmentVariable("DOTNET_USE_POLLING_FILE_WATCHER", "true");
        var exit = Microsoft.Playwright.Program.Main(["install", "chromium"]);
        Assert.Equal(0, exit);
        _playwright = await Playwright.CreateAsync();
        _browser = await _playwright.Chromium.LaunchAsync(new() { Headless = true });
        _web = new IllustratedPagesWebFactory(contextualTipsEnabled: true);
        _ = _web.ServerAddress;
    }

    public async ValueTask DisposeAsync()
    {
        if (_browser is not null)
        {
            await _browser.DisposeAsync();
        }

        _playwright?.Dispose();
        _web?.Dispose();
    }

    [Fact]
    public async Task Capture_six_tip_screens_and_flag_proof()
    {
        Directory.CreateDirectory(ArtifactsDir);
        var appId = IllustratedPagesApiStub.SampleApplicationId;
        var offerId = IllustratedPagesApiStub.SampleOfferApplicationId;

        await CaptureReferencePageAsync("ref-instellingen-390", "/candidate/settings");
        await CaptureReferencePageAsync("ref-bewaard-390", "/candidate/liked");
        await CaptureAsync("01-gesprek-gepland", $"/candidate/applications/{appId:D}/gesprek", "[data-testid='application-interview-page']");
        await CaptureAsync("02-aanbod-ontvangen", $"/candidate/applications/{offerId:D}/aanbod", "[data-testid='application-offer-page']");
        await CaptureAsync("03-paspoort-cv-tip", "/candidate/paspoort?tab=proof", "[data-testid='passport-education-tip']");
        await CaptureAsync("04-carriere-ontwikkeltip", "/carriere", "[data-testid='career-dev-tip']");
        await CaptureAsync("05-alle-tips", "/candidate/tips", "[data-testid='contextual-tips-index']");
        await CaptureAsync("06-tip-detail", "/candidate/tips/je-eerste-gesprek", "[data-testid='contextual-tip-detail']");

        await CaptureAdminFunctiesAsync(_web!, "admin-functies-flag-aan");
        await using (var offWeb = new IllustratedPagesWebFactory(contextualTipsEnabled: false))
        {
            _ = offWeb.ServerAddress;
            await CaptureAdminFunctiesAsync(offWeb, "admin-functies-flag-uit");
        }

        await using var offWebCandidate = new IllustratedPagesWebFactory(contextualTipsEnabled: false);
        _ = offWebCandidate.ServerAddress;
        await using var ctx = await _browser!.NewContextAsync(new() { ViewportSize = new() { Width = 1440, Height = 900 } });
        await PlaywrightCookieConsent.AcceptAsync(ctx);
        var page = await ctx.NewPageAsync();
        await page.GotoAsync($"{offWebCandidate.ServerAddress}/__test/sign-in?return=%2Fcandidate%2Ftips");
        await page.GotoAsync($"{offWebCandidate.ServerAddress}/candidate/tips", new() { WaitUntil = WaitUntilState.NetworkIdle });
        await page.WaitForTimeoutAsync(800);
        var offFile = Path.Combine(ArtifactsDir, "flag-uit-1440.png");
        await page.ScreenshotAsync(new() { Path = offFile, FullPage = true });
        Assert.DoesNotContain("/candidate/tips", page.Url, StringComparison.OrdinalIgnoreCase);

        await using var onCtx = await _browser.NewContextAsync(new() { ViewportSize = new() { Width = 1440, Height = 900 } });
        await PlaywrightCookieConsent.AcceptAsync(onCtx);
        var onPage = await onCtx.NewPageAsync();
        await onPage.GotoAsync($"{_web!.ServerAddress}/__test/sign-in?return=%2Fcandidate%2Ftips");
        await onPage.GotoAsync($"{_web.ServerAddress}/candidate/tips", new() { WaitUntil = WaitUntilState.NetworkIdle });
        await onPage.WaitForSelectorAsync("[data-testid='contextual-tips-index']", new() { Timeout = 60_000 });
        var onFile = Path.Combine(ArtifactsDir, "flag-aan-1440.png");
        await onPage.ScreenshotAsync(new() { Path = onFile, FullPage = true });
    }

    private async Task CaptureAdminFunctiesAsync(IllustratedPagesWebFactory web, string namePrefix)
    {
        await using var context = await _browser!.NewContextAsync(new()
        {
            ViewportSize = new() { Width = 1440, Height = 900 },
            Locale = "nl-NL"
        });
        await PlaywrightCookieConsent.AcceptAsync(context);
        var page = await context.NewPageAsync();
        page.SetDefaultTimeout(120_000);
        await page.GotoAsync(
            $"{web.ServerAddress}/__test/sign-in?role=admin&return=%2Fadmin%2Finstellingen",
            new() { WaitUntil = WaitUntilState.DOMContentLoaded });
        await page.GotoAsync($"{web.ServerAddress}/admin/instellingen", new() { WaitUntil = WaitUntilState.NetworkIdle });
        await page.WaitForTimeoutAsync(1200);
        var file = Path.Combine(ArtifactsDir, $"{namePrefix}-1440.png");
        await page.ScreenshotAsync(new() { Path = file, FullPage = true });
        Assert.True(File.Exists(file));
    }

    private async Task CaptureReferencePageAsync(string name, string path)
    {
        await using var context = await _browser!.NewContextAsync(new()
        {
            ViewportSize = new() { Width = 390, Height = 844 },
            Locale = "nl-NL",
            IsMobile = true,
            HasTouch = true
        });
        await PlaywrightCookieConsent.AcceptAsync(context);
        var page = await context.NewPageAsync();
        page.SetDefaultTimeout(120_000);
        await page.GotoAsync(
            $"{_web!.ServerAddress}/__test/sign-in?return={Uri.EscapeDataString(path)}",
            new() { WaitUntil = WaitUntilState.DOMContentLoaded });
        await page.GotoAsync($"{_web.ServerAddress}{path}", new() { WaitUntil = WaitUntilState.NetworkIdle });
        await page.WaitForTimeoutAsync(600);
        var file = Path.Combine(ArtifactsDir, $"{name}.png");
        await page.ScreenshotAsync(new() { Path = file, FullPage = true });
        Assert.True(File.Exists(file));
    }

    private async Task CaptureAsync(string name, string path, string readySelector)
    {
        foreach (var (width, height, tag) in new[] { (1440, 900, "1440"), (390, 844, "390") })
        {
            await using var context = await _browser!.NewContextAsync(new()
            {
                ViewportSize = new() { Width = width, Height = height },
                Locale = "nl-NL",
                IsMobile = width < 500,
                HasTouch = width < 500
            });
            await PlaywrightCookieConsent.AcceptAsync(context);
            var page = await context.NewPageAsync();
            page.SetDefaultTimeout(120_000);
            await page.GotoAsync(
                $"{_web!.ServerAddress}/__test/sign-in?return={Uri.EscapeDataString(path)}",
                new() { WaitUntil = WaitUntilState.DOMContentLoaded });
            await page.GotoAsync($"{_web.ServerAddress}{path}", new() { WaitUntil = WaitUntilState.NetworkIdle });
            await page.WaitForSelectorAsync(readySelector, new() { Timeout = 60_000 });
            await page.WaitForTimeoutAsync(600);
            var file = Path.Combine(ArtifactsDir, $"{name}-{tag}.png");
            await page.ScreenshotAsync(new() { Path = file, FullPage = true });
            Assert.True(File.Exists(file));

            var auditLines = await BuildHeroAuditAsync(page, _web.ServerAddress);
            var auditPath = Path.Combine(ArtifactsDir, $"{name}-{tag}-hero-audit.txt");
            await File.WriteAllTextAsync(auditPath, auditLines, Encoding.UTF8);

            if (width == 390 && name is "01-gesprek-gepland" or "02-aanbod-ontvangen" or "05-alle-tips" or "06-tip-detail")
            {
                var heroZoom = Path.Combine(ArtifactsDir, $"{name}-390-hero-zoom.png");
                var hero = page.Locator(".cand-hero").First;
                var box = await hero.BoundingBoxAsync();
                if (box is { Height: > 40, Width: > 40 })
                {
                    await hero.ScreenshotAsync(new() { Path = heroZoom });
                }
            }
        }
    }

    private static async Task<string> BuildHeroAuditAsync(IPage page, string serverAddress)
    {
        var raw = await page.EvaluateAsync<string?>(@"() => {
            const hero = document.querySelector('.cand-hero');
            if (!hero) return null;
            const lines = [];
            const img = hero.querySelector('img.cand-hero__mob-figures');
            const pictureImg = hero.querySelector('picture img');
            if (img && (img.currentSrc || img.src)) {
                lines.push('mob-figures-img:' + (img.currentSrc || img.src));
            }
            if (pictureImg && (pictureImg.currentSrc || pictureImg.src)) {
                lines.push('picture-img:' + (pictureImg.currentSrc || pictureImg.src));
            }
            const style = getComputedStyle(hero);
            if (style.backgroundImage && style.backgroundImage !== 'none') {
                lines.push('hero-bg:' + style.backgroundImage);
            }
            const media = hero.querySelector('.cand-hero__media');
            if (media) {
                const ms = getComputedStyle(media);
                if (ms.backgroundImage && ms.backgroundImage !== 'none') {
                    lines.push('media-figures:' + ms.backgroundImage);
                }
            }
            const after = getComputedStyle(hero, '::after');
            if (after.backgroundImage && after.backgroundImage !== 'none') {
                lines.push('after-atmosphere:' + after.backgroundImage);
            }
            return lines.join('\n');
        }");

        if (string.IsNullOrWhiteSpace(raw))
        {
            return "no .cand-hero on page\n";
        }

        var sb = new StringBuilder();
        sb.AppendLine(raw);
        foreach (var line in raw.Split('\n', StringSplitOptions.RemoveEmptyEntries))
        {
            var url = ExtractUrlFromAuditLine(line);
            if (url is null)
            {
                continue;
            }

            var md5 = await Md5ForServerUrlAsync(page, serverAddress, url);
            sb.AppendLine($"{line} md5={md5}");
        }

        return sb.ToString();
    }

    private static string? ExtractUrlFromAuditLine(string line)
    {
        var match = Regex.Match(line, @"url\(""([^""]+)""\)|url\('([^']+)'\)|(https?://[^\s""')]+|/[^\s""')]+)");
        if (!match.Success)
        {
            return null;
        }

        return match.Groups[1].Success ? match.Groups[1].Value
            : match.Groups[2].Success ? match.Groups[2].Value
            : match.Groups[3].Value;
    }

#pragma warning disable CA5351 // MD5 for build artifact fingerprinting only
    private static async Task<string> Md5ForServerUrlAsync(IPage page, string serverAddress, string url)
    {
        var absolute = url.StartsWith("http", StringComparison.OrdinalIgnoreCase)
            ? url
            : $"{serverAddress.TrimEnd('/')}{url.Split('?')[0]}";

        var response = await page.Context.APIRequest.GetAsync(absolute);
        if (!response.Ok)
        {
            return $"fetch-failed-{response.Status}";
        }

        var bytes = await response.BodyAsync();
        var hash = MD5.HashData(bytes);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }
#pragma warning restore CA5351
}

using Jobsy.Core.Email;
using Jobsy.Core.Email.Model;
using Microsoft.Playwright;

namespace Jobsy.Tests;

/// <summary>
/// Renders each mail HTML into Chromium (SetContent) — no live stack required.
/// Excluded from the unit job; included in the Playwright smoke filter.
/// </summary>
[Collection("PlaywrightSmoke")]
public class EmailRenderPlaywrightTests : IClassFixture<EmailRenderPlaywrightTests.BrowserFixture>
{
    private readonly BrowserFixture _browser;
    private readonly ITestOutputHelper _output;

    public EmailRenderPlaywrightTests(BrowserFixture browser, ITestOutputHelper output)
    {
        _browser = browser;
        _output = output;
    }

    public static IEnumerable<object[]> Cases()
    {
        foreach (var def in EmailTemplateRegistry.All)
        {
            foreach (var lang in new[] { "nl", "ar" })
            {
                foreach (var width in new[] { 600, 375 })
                {
                    foreach (var theme in new[] { "light", "dark" })
                    {
                        yield return new object[] { def.Key, lang, width, theme };
                    }
                }
            }
        }
    }

    [Theory]
    [MemberData(nameof(Cases))]
    public async Task Render_checks_overflow_typography_and_rtl(string key, string lang, int width, string theme)
    {
        var dark = theme == "dark";
        var sample = EmailSampleContext.ForPreview("https://lobsy.nl");
        using var _ = dark ? TransactionalEmails.UseRenderMode(EmailRenderMode.PreviewDark) : null;
        var mail = TransactionalEmails.Compose(key, sample, EmailCulture.ForLanguage(lang));

        await using var context = await _browser.Browser.NewContextAsync(new BrowserNewContextOptions
        {
            ViewportSize = new() { Width = width, Height = 900 },
            ColorScheme = dark ? ColorScheme.Dark : ColorScheme.Light
        });
        var page = await context.NewPageAsync();
        await page.SetContentAsync(mail.Html, new() { WaitUntil = WaitUntilState.DOMContentLoaded });

        var metrics = await page.EvaluateAsync<RenderMetrics>("""
            () => {
              const root = document.documentElement;
              const body = document.body;
              const h1 = document.querySelector('h1');
              const cta = document.querySelector('[data-lobsy-cta]');
              const ft = document.querySelector('.ft') || body;
              const bodySample = document.querySelector('.t, p') || body;
              const cs = (el, prop) => el ? parseFloat(getComputedStyle(el)[prop]) : 0;
              const bg = (el) => el ? getComputedStyle(el).backgroundColor : '';
              const card = document.querySelector('.card') || body;
              const cells = Array.from(document.querySelectorAll('.kv-l, .kv-v, td')).slice(0, 2);
              const firstX = cells[0] ? cells[0].getBoundingClientRect().x : 0;
              const secondX = cells[1] ? cells[1].getBoundingClientRect().x : 0;
              return {
                scrollWidth: root.scrollWidth,
                innerWidth: window.innerWidth,
                footerFont: cs(ft, 'fontSize'),
                bodyFont: cs(bodySample, 'fontSize'),
                h1Font: cs(h1, 'fontSize'),
                ctaHeight: cta ? cta.getBoundingClientRect().height : 44,
                ctaWidth: cta ? cta.getBoundingClientRect().width : 0,
                dir: root.getAttribute('dir') || document.dir || '',
                cardBg: bg(card),
                firstCellX: firstX,
                secondCellX: secondX
              };
            }
            """);

        Assert.True(metrics.ScrollWidth <= metrics.InnerWidth + 2,
            $"{key}/{lang}/{width}/{theme} overflow {metrics.ScrollWidth}>{metrics.InnerWidth}");
        Assert.True(metrics.FooterFont >= 12.0, $"footer font {metrics.FooterFont}");
        Assert.True(metrics.BodyFont >= 14.0, $"body font {metrics.BodyFont}");
        Assert.True(metrics.H1Font is >= 20 and <= 32, $"h1 font {metrics.H1Font}");
        if (mail.Html.Contains("data-lobsy-cta", StringComparison.Ordinal))
        {
            Assert.True(metrics.CtaHeight >= 36, $"cta height {metrics.CtaHeight}");
        }

        if (lang == "ar")
        {
            Assert.Equal("rtl", metrics.Dir);
        }

        if (dark)
        {
            Assert.DoesNotContain("255, 252, 250", metrics.CardBg ?? "");
        }

        var dir = Path.Combine(FindRepoRoot(), "artifacts", "playwright-email");
        Directory.CreateDirectory(dir);
        var shot = Path.Combine(dir, $"{Sanitize(key)}-{lang}-{width}-{theme}.png");
        await page.ScreenshotAsync(new() { Path = shot, FullPage = true });
        _output.WriteLine($"SHOT {Path.GetFileName(shot)}");
    }

    private static string Sanitize(string key)
        => string.Concat(key.Select(c => char.IsLetterOrDigit(c) ? c : '-'));

    private sealed class RenderMetrics
    {
        public int ScrollWidth { get; set; }
        public int InnerWidth { get; set; }
        public double FooterFont { get; set; }
        public double BodyFont { get; set; }
        public double H1Font { get; set; }
        public double CtaHeight { get; set; }
        public double CtaWidth { get; set; }
        public string Dir { get; set; } = "";
        public string? CardBg { get; set; }
        public double FirstCellX { get; set; }
        public double SecondCellX { get; set; }
    }

    public sealed class BrowserFixture : IAsyncLifetime
    {
        public IPlaywright Playwright { get; private set; } = null!;
        public IBrowser Browser { get; private set; } = null!;

        public async ValueTask InitializeAsync()
        {
            Microsoft.Playwright.Program.Main(["install", "chromium"]);
            Playwright = await Microsoft.Playwright.Playwright.CreateAsync();
            Browser = await Playwright.Chromium.LaunchAsync(new() { Headless = true });
        }

        public async ValueTask DisposeAsync()
        {
            if (Browser is not null)
            {
                await Browser.DisposeAsync();
            }

            Playwright?.Dispose();
        }
    }

    private static string FindRepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "Jobsy.sln")))
            {
                return dir.FullName;
            }

            dir = dir.Parent;
        }

        return Directory.GetCurrentDirectory();
    }
}

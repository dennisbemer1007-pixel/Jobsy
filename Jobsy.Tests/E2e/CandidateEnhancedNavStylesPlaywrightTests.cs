using Microsoft.Playwright;

namespace Jobsy.Tests.E2e;

/// <summary>
/// Route CSS from App.razor is fixed at first document load; enhanced navigation must
/// inject the destination bundle via <see cref="Jobsy.Web.Components.PageRouteStylesHead"/>.
/// </summary>
[Collection("FunctionalE2e")]
[Trait("Suite", "FunctionalE2e")]
public sealed class CandidateEnhancedNavStylesPlaywrightTests
{
    private const string OnboardingEmail = "onboarding.e2e@jobsy.local";
    private const string Password = "Jobsy123!";

    private static readonly (string Href, string RequiredCss, string StyleProbeSelector, string Label)[] NavTargets =
    [
        ("/candidate/ontdekkingsreis", "ontdekkingsreis.css", ".journey-scene__sun", "Ontdekkingsreis"),
        ("/candidate/paspoort", "mijn-paspoort.css", ".passport-page--illustrated, .passport-page", "Paspoort"),
        ("/banenkaart", "banenkaart.css", ".jobsy-discovery", "Banenkaart"),
        ("/candidate/applications", "applications.css", ".apps-page, .app-main", "Sollicitaties"),
        ("/carriere", "carriere.css", ".panel-page, .career-card", "Carrière"),
    ];

    [Theory]
    [InlineData(1440, 900, false)]
    [InlineData(390, 844, true)]
    public async Task Client_side_nav_from_banenkaart_applies_route_stylesheets(int width, int height, bool mobile)
    {
        var baseUrl = await ResolveBaseUrlAsync();
        if (baseUrl is null)
        {
            return;
        }

        Microsoft.Playwright.Program.Main(["install", "chromium"]);
        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new() { Headless = true });
        await using var context = await browser.NewContextAsync(new()
        {
            ViewportSize = new() { Width = width, Height = height },
            IsMobile = mobile,
            HasTouch = mobile,
            Locale = "nl-NL",
            IgnoreHTTPSErrors = true,
        });
        var page = await context.NewPageAsync();
        page.SetDefaultTimeout(120_000);

        await page.GotoAsync(baseUrl + "/", new() { WaitUntil = WaitUntilState.DOMContentLoaded, Timeout = 90_000 });
        await PlaywrightCookieConsent.AcceptOnPageAsync(page);

        var email = baseUrl.Contains("127.0.0.1", StringComparison.Ordinal)
            || baseUrl.Contains("localhost", StringComparison.OrdinalIgnoreCase)
            ? KbE2e.DefaultCompleteEmail
            : OnboardingEmail;
        if (!await KbE2e.TryLoginAsync(page, baseUrl, email, Password))
        {
            throw new InvalidOperationException($"Could not log in as {email} at {baseUrl}.");
        }

        await page.GotoAsync(baseUrl + "/banenkaart", new() { WaitUntil = WaitUntilState.DOMContentLoaded, Timeout = 90_000 });
        await KbE2e.WaitForStyledAppAsync(page, "banenkaart.css");

        var tag = mobile ? "390" : "1440";
        var artifactDir = Path.Combine(FindRepoRoot(), "artifacts", "enhanced-nav-css");
        Directory.CreateDirectory(artifactDir);

        foreach (var (href, requiredCss, probe, label) in NavTargets)
        {
            var link = mobile
                ? page.Locator($"nav.bottom-nav a[href='{href}']").First
                : page.Locator($"a[href='{href}']").First;

            await link.WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 45_000 });
            await link.ClickAsync();

            await page.WaitForURLAsync(u => u.Contains(href.TrimEnd('/'), StringComparison.OrdinalIgnoreCase)
                                             || u.EndsWith(href, StringComparison.OrdinalIgnoreCase),
                new() { Timeout = 60_000 });
            await page.WaitForTimeoutAsync(600);

            var audit = await page.EvaluateAsync<StyleAudit>(
                """
                ({ requiredCss, probe }) => {
                  const sheets = [...document.querySelectorAll('link[rel=stylesheet]')].map(l => l.href || '');
                  const hasCss = sheets.some(h => h.includes(requiredCss));
                  const el = document.querySelector(probe.split(',')[0].trim())
                    || document.querySelector(probe.split(',')[1]?.trim() || probe);
                  let fill = '';
                  let fontSize = '';
                  if (el) {
                    const style = getComputedStyle(el);
                    fontSize = style.fontSize || '';
                    if (el.classList && el.classList.contains('journey-scene__sun')) {
                      fill = style.fill || '';
                    }
                  }
                  const sun = document.querySelector('.journey-scene__sun');
                  if (sun) {
                    fill = getComputedStyle(sun).fill || fill;
                  }
                  const blackSun = fill === 'rgb(0, 0, 0)' || fill === 'black';
                  return {
                    hasCss,
                    blackSun,
                    fill,
                    fontSize,
                    sheetCount: sheets.length
                  };
                }
                """,
                new { requiredCss, probe });

            var phase = baseUrl.Contains("acceptatie.lobsy.nl", StringComparison.OrdinalIgnoreCase) ? "before" : "after";
            var fileName = $"{phase}-{tag}-{label.ToLowerInvariant().Replace(' ', '-')}.png";
            await page.ScreenshotAsync(new()
            {
                Path = Path.Combine(artifactDir, fileName),
                FullPage = false,
            });

            Assert.True(audit.HasCss, $"{label}: missing stylesheet fragment '{requiredCss}' after enhanced nav (sheets={audit.SheetCount}).");
            if (requiredCss == "ontdekkingsreis.css" && audit.BlackSun)
            {
                Assert.Fail($"{label}: journey sun fill is unstyled black ({audit.Fill}).");
            }
        }
    }

    private static async Task<string?> ResolveBaseUrlAsync()
    {
        var baseUrl = FunctionalE2eGate.TryBaseUrl() ?? "https://acceptatie.lobsy.nl";
        if (FunctionalE2eGate.IsRequired)
        {
            return await FunctionalE2eGate.RequireReachableBaseUrlAsync();
        }

        if (!await SmokeReachability.IsReachableAsync(baseUrl))
        {
            return null;
        }

        return baseUrl;
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

        throw new InvalidOperationException("Jobsy.sln not found.");
    }

    private sealed class StyleAudit
    {
        public bool HasCss { get; set; }
        public bool BlackSun { get; set; }
        public string Fill { get; set; } = "";
        public string FontSize { get; set; } = "";
        public int SheetCount { get; set; }
    }
}

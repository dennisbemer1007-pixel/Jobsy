using System.Net;
using System.Net.Http;
using Microsoft.Playwright;

var outputDir = Environment.GetEnvironmentVariable("JOBSY_CAPTURE_OUT")?.Trim();
if (string.IsNullOrEmpty(outputDir))
{
    outputDir = "/opt/cursor/artifacts/testblokken-r3";
}

var mobileOnly = string.Equals(
    Environment.GetEnvironmentVariable("JOBSY_CAPTURE_MOBILE_ONLY"),
    "1",
    StringComparison.Ordinal);
var desktopOnly = string.Equals(
    Environment.GetEnvironmentVariable("JOBSY_CAPTURE_DESKTOP_ONLY"),
    "1",
    StringComparison.Ordinal);
var captureLang = (Environment.GetEnvironmentVariable("JOBSY_CAPTURE_LANG") ?? "nl").Trim().ToLowerInvariant();
if (captureLang is not ("nl" or "en"))
{
    captureLang = "nl";
}

var voorbeeldenOnly = string.Equals(
    Environment.GetEnvironmentVariable("JOBSY_CAPTURE_VOORBEELDEN"),
    "1",
    StringComparison.Ordinal);
var desktopOnly = string.Equals(
    Environment.GetEnvironmentVariable("JOBSY_CAPTURE_DESKTOP_ONLY"),
    "1",
    StringComparison.Ordinal);
const string baseUrl = "http://localhost:5201";
const string consentInit = """
    try { localStorage.setItem('Jobsy.CookieConsent', 'necessary'); } catch (e) {}
    """;

var targets = voorbeeldenOnly
    ? new (string Stem, string ReturnPath, string CaptureTarget)[]
    {
        ("voorbeeld-hoe-werkt", "/__test/capture-test-block?kind=competence&variant=quick", "competence:0"),
        ("voorbeeld-leuk", "/__test/capture-test-block?kind=career&variant=quick", "career:0"),
        ("voorbeeld-waarden", "/__test/capture-test-block?kind=values&variant=quick", "values:0"),
    }
    : new (string Stem, string ReturnPath, string CaptureTarget)[]
    {
        ("quick-scan-hoe-je-werkt", "/__test/capture-test-block?kind=competence&variant=quick", "competence:0"),
        ("quick-scan-leuk", "/__test/capture-test-block?kind=career&variant=quick", "career:0"),
        ("quick-scan-sfeer", "/__test/capture-test-block?kind=culture&variant=quick", "culture:0"),
        ("quick-scan-waarden", "/__test/capture-test-block?kind=values&variant=quick", "values:0"),
        ("verdieping-leuk", "/__test/capture-test-block?kind=career&variant=verdieping", "career:12"),
        ("diepte-competenties", "/__test/capture-test-block?kind=competence&variant=deep", "competence:35:deep")
    };

Directory.CreateDirectory(outputDir);
Microsoft.Playwright.Program.Main(["install", "chromium"]);

using var playwright = await Playwright.CreateAsync();
await using var browser = await playwright.Chromium.LaunchAsync(new()
{
    Headless = true,
    Args = ["--disable-dev-shm-usage", "--no-sandbox"]
});

using var setupHttp = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };

var onlyStem = (Environment.GetEnvironmentVariable("JOBSY_TESTBLOKKEN_STEM") ?? "").Trim();
if (!string.IsNullOrEmpty(onlyStem))
{
    targets = targets.Where(t => t.Stem.Equals(onlyStem, StringComparison.OrdinalIgnoreCase)).ToArray();
    if (targets.Length == 0)
    {
        throw new InvalidOperationException($"Unknown stem: {onlyStem}");
    }
}

foreach (var (stem, returnPath, captureTarget) in targets)
{
    var parts = captureTarget.Split(':', StringSplitOptions.RemoveEmptyEntries);
    var kind = parts[0];
    var answered = parts.Length > 1 ? parts[1] : "6";
    var deep = captureTarget.Contains(":deep", StringComparison.OrdinalIgnoreCase);
    var setUrl = $"{baseUrl}/__test/capture-target?kind={Uri.EscapeDataString(kind)}&answered={answered}&deep={deep.ToString().ToLowerInvariant()}";
    _ = await setupHttp.GetAsync(setUrl);
    Console.WriteLine($"Set capture stub: {setUrl}");

    await using var authContext = await browser.NewContextAsync(new() { IgnoreHTTPSErrors = true });
    await authContext.AddInitScriptAsync(consentInit);
    await SignInAsync(authContext, baseUrl, returnPath);
    var authCookies = (await authContext.CookiesAsync())
        .Select(c => new Microsoft.Playwright.Cookie
        {
            Name = c.Name,
            Value = c.Value,
            Domain = c.Domain,
            Path = c.Path,
            HttpOnly = c.HttpOnly,
            Secure = c.Secure,
            SameSite = c.SameSite
        })
        .ToArray();

    var viewports = mobileOnly
        ? new (int, int, string)[] { (390, 844, "390") }
        : desktopOnly
            ? new (int, int, string)[] { (1440, 900, "1440") }
            : new (int, int, string)[] { (1440, 900, "1440"), (390, 844, "390") };
    foreach (var (width, height, tag) in viewports)
    {
        await using var context = await browser.NewContextAsync(new()
        {
            ViewportSize = new ViewportSize { Width = width, Height = height },
            Locale = captureLang == "en" ? "en-GB" : "nl-NL",
            IsMobile = width < 640,
            HasTouch = width < 640,
            IgnoreHTTPSErrors = true
        });
        await context.AddInitScriptAsync(consentInit);
        await context.AddCookiesAsync(authCookies);
        var cultureCookie = new Microsoft.Playwright.Cookie
        {
            Name = "Jobsy.Culture",
            Value = captureLang,
            Domain = "localhost",
            Path = "/",
            HttpOnly = false,
            Secure = false,
            SameSite = Microsoft.Playwright.SameSiteAttribute.Lax
        };
        await context.AddCookiesAsync(new[] { cultureCookie });
        var page = await context.NewPageAsync();
        page.SetDefaultTimeout(90_000);
        var langQuery = captureLang == "en" ? "&lang=en" : "";
        var pathWithLang = returnPath.Contains('?', StringComparison.Ordinal)
            ? returnPath + langQuery
            : returnPath + "?lang=" + captureLang;
        await page.GotoAsync(baseUrl + pathWithLang, new() { WaitUntil = WaitUntilState.Commit, Timeout = 120_000 });
        await page.EvaluateAsync("""
            () => {
              try { localStorage.setItem('Jobsy.CookieConsent', 'necessary'); } catch (e) {}
              document.documentElement.classList.add('cookie-consent-known');
            }
            """);
        await page.Locator("[data-testid=test-block-stage]").WaitForAsync(new() { Timeout = 120_000 });
        await page.Locator(".test-flow, .test-shell__card").First.WaitForAsync(new() { Timeout = 120_000 });
        await page.Locator("[data-testid=test-block-stage]").WaitForAsync();
        if (width >= 1024 && stem is not ("quick-scan-hoe-je-werkt" or "quick-scan-leuk" or "quick-scan-sfeer" or "quick-scan-waarden"))
        {
            await page.Locator("[data-testid=test-progress-insight-panel]").WaitForAsync(new() { Timeout = 60_000 });
        }
        else if (width < 1024 && stem is "verdieping-leuk" or "diepte-competenties")
        {
            await page.Locator("[data-testid=test-progress-insight-mobile]").WaitForAsync(new() { Timeout = 60_000 });
        }

        if (stem.StartsWith("quick-scan", StringComparison.OrdinalIgnoreCase)
            || stem.StartsWith("voorbeeld-", StringComparison.OrdinalIgnoreCase))
        {
            await page.Locator("[data-testid=test-item-example]").WaitForAsync(new() { Timeout = 60_000 });
        }

        if (width < 640)
        {
            if (mobileOnly)
            {
                await page.EvaluateAsync("""
                    () => {
                      window.scrollTo(0, 0);
                      document.documentElement.scrollTop = 0;
                      document.body.scrollTop = 0;
                      const scroller = document.querySelector('.app-shell__main, .app-main, main');
                      if (scroller) {
                        scroller.scrollTop = 0;
                      }
                    }
                    """);
            }
            else
            {
                await page.EvaluateAsync("""
                    () => {
                      const likert = document.querySelector('.test-likert');
                      if (likert) {
                        likert.scrollIntoView({ block: 'center', behavior: 'instant' });
                      }
                    }
                    """);
            }

            await page.WaitForTimeoutAsync(350);
        }

        if (width < 640)
        {
            var scrollWidth = await page.EvaluateAsync<double>("""
                () => Math.max(document.documentElement.scrollWidth, document.body?.scrollWidth ?? 0)
                """);
            if (Math.Abs(scrollWidth - width) > 1.5)
            {
                throw new InvalidOperationException($"{stem}: scrollWidth {scrollWidth} != viewport {width}");
            }

            var layout = await page.EvaluateAsync<string>("""
                () => {
                  const pick = (sel) => {
                    const el = document.querySelector(sel);
                    if (!el) return null;
                    const r = el.getBoundingClientRect();
                    return Math.round(r.width);
                  };
                  return JSON.stringify({
                    page: pick('.test-page'),
                    stage: pick('[data-testid=test-block-stage]'),
                    flow: pick('.test-flow'),
                    footer: pick('.test-flow__footer[data-sticky-footer]')
                  });
                }
                """);
            Console.WriteLine($"{stem}: layout widths {layout}");
        }

        var langTag = captureLang == "en" ? "en" : "nl";
        var path = Path.Combine(outputDir, $"{stem}-{langTag}-{tag}.png");
        await page.ScreenshotAsync(new() { Path = path, FullPage = false });
        Console.WriteLine($"Wrote {path} ({new FileInfo(path).Length} bytes)");
    }
}

static async Task SignInAsync(IBrowserContext context, string baseUrl, string returnPath)
{
    var signInUrl = $"{baseUrl.TrimEnd('/')}/__test/sign-in?return={Uri.EscapeDataString(returnPath)}";
    var response = await context.APIRequest.GetAsync(signInUrl, new() { MaxRedirects = 0 });
    if (response.Status is not 302 and not 200)
    {
        throw new InvalidOperationException($"Sign-in failed: {(int)response.Status} {response.StatusText}");
    }
}

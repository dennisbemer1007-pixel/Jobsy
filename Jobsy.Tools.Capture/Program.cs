using System.Net;
using System.Net.Http;
using Microsoft.Playwright;

const string outputDir = "/opt/cursor/artifacts/testblokken-r2";
const string baseUrl = "http://localhost:5201";
const string consentInit = """
    try { localStorage.setItem('Jobsy.CookieConsent', 'necessary'); } catch (e) {}
    """;

var targets = new (string Stem, string ReturnPath, string CaptureTarget)[]
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

    foreach (var (width, height, tag) in new (int, int, string)[] { (1440, 900, "1440"), (390, 844, "390") })
    {
        await using var context = await browser.NewContextAsync(new()
        {
            ViewportSize = new ViewportSize { Width = width, Height = height },
            Locale = "nl-NL",
            IsMobile = width < 640,
            HasTouch = width < 640,
            IgnoreHTTPSErrors = true
        });
        await context.AddInitScriptAsync(consentInit);
        await context.AddCookiesAsync(authCookies);
        var page = await context.NewPageAsync();
        page.SetDefaultTimeout(90_000);
        await page.GotoAsync(baseUrl + returnPath, new() { WaitUntil = WaitUntilState.Commit, Timeout = 120_000 });
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

        if (stem.StartsWith("quick-scan", StringComparison.OrdinalIgnoreCase))
        {
            await page.Locator("[data-testid=test-item-example]").WaitForAsync(new() { Timeout = 60_000 });
        }

        if (width < 640)
        {
            await page.Locator(".test-likert").First.ScrollIntoViewIfNeededAsync();
            await page.EvaluateAsync("""
                () => {
                  const likert = document.querySelector('.test-likert');
                  const footer = document.querySelector('.test-flow__footer[data-sticky-footer]');
                  if (!likert || !footer) return;
                  const gap = footer.getBoundingClientRect().top - likert.getBoundingClientRect().bottom;
                  if (gap < 12) {
                    window.scrollBy(0, gap - 24);
                  }
                }
                """);
        }

        var path = Path.Combine(outputDir, $"{stem}-{tag}.png");
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

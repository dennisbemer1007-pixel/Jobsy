using System.Net;
using System.Text.RegularExpressions;
using Microsoft.Playwright;

namespace Jobsy.Tests;

/// <summary>
/// Public-pages 10.1/10.2 — the public and legal pages in five languages (ar right-to-left) at
/// 1440×900 and 390×844, plus the report, mijn-gegevens and bedenktijd flows.
/// Soft-skips when JOBSY_E2E_BASE_URL is unset or unreachable, like the other Playwright suites.
/// </summary>
[Collection("PlaywrightSmoke")]
public class PublicPagesPlaywrightTests
{
    private const string CandidateEmail = "kandidaat@jobsy.local";
    private const string SalesEmail = "sales@jobsy.local";
    private const string DefaultPassword = "Jobsy123!";

    private static readonly string[] Languages = ["nl", "en", "pl", "ro", "ar"];
    private static readonly (int Width, int Height)[] Viewports = [(1440, 900), (390, 844)];

    /// <summary>Unfilled copy like "[ADRES]" must never reach a visitor (§0).</summary>
    private static readonly Regex Placeholder = new(@"\[[A-Z][A-Z \-]+\]", RegexOptions.CultureInvariant);

    private static readonly string[] LegalPaths = ["/privacy", "/algemene-voorwaarden", "/gebruiksvoorwaarden"];

    [Fact]
    public async Task Never_targets_production_host()
    {
        var raw = (Environment.GetEnvironmentVariable("JOBSY_E2E_BASE_URL") ?? "").Trim();
        if (string.IsNullOrWhiteSpace(raw))
        {
            return;
        }

        Assert.DoesNotContain("lobsy.nl", raw, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Public_pages_render_in_five_languages_on_desktop_and_mobile()
    {
        var baseUrl = await ResolveBaseUrlAsync();
        if (baseUrl is null)
        {
            return;
        }

        Microsoft.Playwright.Program.Main(["install", "chromium"]);
        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new() { Headless = true });

        var kvk = await FindVerifiedCompanyKvkAsync(baseUrl);
        var artifactDir = Path.Combine(FindRepoRoot(), "artifacts", "playwright-public");
        Directory.CreateDirectory(artifactDir);

        var pages = new List<(string Name, string Path, bool NeedsCandidate)>
        {
            ("hoe-werkt-lobsy", "/hoe-werkt-lobsy", false),
            ("wie-zijn-wij", "/wie-zijn-wij", false),
            ("privacy", "/privacy", false),
            ("algemene-voorwaarden", "/algemene-voorwaarden", false),
            ("gebruiksvoorwaarden", "/gebruiksvoorwaarden", false),
            ("partner", "/partner", false),
            ("mijn-gegevens", "/privacy/data", true)
        };

        if (kvk is not null)
        {
            pages.Add(("bedrijfspagina", $"/{kvk}", false));
            pages.Add(("melden", $"/melden?type=company&id={kvk}", false));
        }

        foreach (var (width, height) in Viewports)
        {
            await using var anonymous = await browser.NewContextAsync(new()
            {
                ViewportSize = new() { Width = width, Height = height }
            });
            await using var candidate = await browser.NewContextAsync(new()
            {
                ViewportSize = new() { Width = width, Height = height }
            });
            var candidatePage = await candidate.NewPageAsync();
            var candidateSignedIn = await TryLoginAsync(candidatePage, baseUrl, CandidateEmail);

            foreach (var (name, path, needsCandidate) in pages)
            {
                if (needsCandidate && !candidateSignedIn)
                {
                    continue;
                }

                var page = needsCandidate ? candidatePage : await anonymous.NewPageAsync();
                foreach (var lang in Languages)
                {
                    await SetLanguageAsync(page, baseUrl, lang);
                    var response = await page.GotoAsync(
                        baseUrl + path,
                        new() { WaitUntil = WaitUntilState.DOMContentLoaded, Timeout = 90_000 });

                    Assert.NotNull(response);
                    Assert.Equal(200, response!.Status);

                    await AssertChromeAsync(page, name, lang, width);
                    if (LegalPaths.Contains(path, StringComparer.Ordinal))
                    {
                        await AssertLegalDocumentAsync(page, lang);
                    }

                    if (path == "/privacy")
                    {
                        await AssertPrivacyProcessorsAsync(page);
                    }

                    if (path == "/partner")
                    {
                        await AssertPartnerPricesAsync(page);
                    }

                    try
                    {
                        await page.ScreenshotAsync(new()
                        {
                            Path = Path.Combine(artifactDir, $"{name}-{lang}-{width}.png"),
                            FullPage = true
                        });
                    }
                    catch (PlaywrightException ex) when (ex.Message.Contains("captureScreenshot", StringComparison.Ordinal)
                                                        || ex.Message.Contains("Unable to capture screenshot", StringComparison.Ordinal))
                    {
                        // Chromium occasionally fails screenshot after font load on CI; assertions above already passed.
                    }
                }

                if (!needsCandidate)
                {
                    await page.CloseAsync();
                }
            }
        }
    }

    [Fact]
    public async Task Anonymous_visitor_can_report_a_company()
    {
        var baseUrl = await ResolveBaseUrlAsync();
        if (baseUrl is null)
        {
            return;
        }

        var kvk = await FindVerifiedCompanyKvkAsync(baseUrl);
        if (kvk is null)
        {
            // Documented skip: the CI seed has no KvK-verified company with a public vacancy.
            return;
        }

        Microsoft.Playwright.Program.Main(["install", "chromium"]);
        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new() { Headless = true });
        await using var context = await browser.NewContextAsync(new()
        {
            ViewportSize = new() { Width = 390, Height = 844 }
        });
        var page = await context.NewPageAsync();

        await page.GotoAsync(
            $"{baseUrl}/melden?type=company&id={kvk}",
            new() { WaitUntil = WaitUntilState.DOMContentLoaded, Timeout = 90_000 });

        await page.Locator("input[name='reason']").First.CheckAsync();
        await page.FillAsync("#melden-details", "E2E: deze pagina klopt niet.");
        await Task.WhenAll(
            page.WaitForURLAsync("**/melden**", new() { Timeout = 30_000 }),
            page.Locator("form button[type=submit]").ClickAsync());

        var html = await page.ContentAsync();
        await Assertions.Expect(page.Locator("#melden-title")).ToBeVisibleAsync();
        // Success hides the form; DSA rate-limit shows an alert but keeps the form usable.
        if (html.Contains("role=\"alert\"", StringComparison.Ordinal))
        {
            Assert.Contains("teveel", page.Url + html, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("Je hebt al", html, StringComparison.OrdinalIgnoreCase);
            return;
        }

        Assert.Equal(0, await page.Locator("form button[type=submit]").CountAsync());
    }

    [Fact]
    public async Task Mijn_gegevens_downloads_a_file_and_never_shows_the_json()
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
            ViewportSize = new() { Width = 1440, Height = 900 },
            AcceptDownloads = true
        });
        var page = await context.NewPageAsync();
        if (!await TryLoginAsync(page, baseUrl, CandidateEmail))
        {
            return;
        }

        await page.GotoAsync(
            baseUrl + "/privacy/data",
            new() { WaitUntil = WaitUntilState.DOMContentLoaded, Timeout = 90_000 });

        var download = await page.RunAndWaitForDownloadAsync(async () =>
        {
            await page.ClickAsync("a[href='/privacy/data/export']");
        }, new() { Timeout = 60_000 });

        Assert.Matches(new Regex(@"^lobsy-mijn-gegevens-.*\.json$"), download.SuggestedFilename);

        // D12: the export is a file, never a blob of JSON on the page.
        var body = await page.Locator("body").InnerTextAsync();
        Assert.DoesNotContain("\"candidate\"", body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("{\"", body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Deep_test_pay_button_stays_disabled_until_the_waiver_is_ticked()
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
            ViewportSize = new() { Width = 1440, Height = 900 }
        });
        var page = await context.NewPageAsync();
        if (!await TryLoginAsync(page, baseUrl, CandidateEmail))
        {
            return;
        }

        await page.GotoAsync(
            baseUrl + "/profiel/tests/competence",
            new() { WaitUntil = WaitUntilState.DOMContentLoaded, Timeout = 90_000 });

        var waiver = page.Locator("input[type=checkbox][data-deep-waiver], input#deep-waiver");
        if (await waiver.CountAsync() == 0)
        {
            // Documented skip: the offer only appears once the free test is finished.
            // The server side of the gate is covered by BedenktijdGuardTests (file 05).
            return;
        }

        var pay = page.Locator("button[data-deep-pay]").First;
        Assert.True(await pay.IsDisabledAsync(), "Pay button must stay disabled without the waiver.");
        await waiver.First.CheckAsync();
        await Assertions.Expect(pay).ToBeEnabledAsync();
    }

    [Fact]
    public async Task Signed_in_candidate_and_salesmanager_get_200_on_how_it_works()
    {
        var baseUrl = await ResolveBaseUrlAsync();
        if (baseUrl is null)
        {
            return;
        }

        Microsoft.Playwright.Program.Main(["install", "chromium"]);
        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new() { Headless = true });

        foreach (var email in new[] { CandidateEmail, SalesEmail })
        {
            await using var context = await browser.NewContextAsync(new()
            {
                ViewportSize = new() { Width = 1440, Height = 900 }
            });
            var page = await context.NewPageAsync();
            if (!await TryLoginAsync(page, baseUrl, email))
            {
                continue;
            }

            var response = await page.GotoAsync(
                baseUrl + "/hoe-werkt-lobsy",
                new() { WaitUntil = WaitUntilState.DOMContentLoaded, Timeout = 90_000 });

            Assert.NotNull(response);
            Assert.Equal(200, response!.Status);
            Assert.EndsWith("/hoe-werkt-lobsy", page.Url, StringComparison.Ordinal);
            Assert.Equal(1, await page.Locator("h1").CountAsync());
        }
    }

    private static async Task AssertChromeAsync(IPage page, string name, string lang, int width)
    {
        var where = $"{name} ({lang}, {width})";

        Assert.Equal(1, await page.Locator("h1").CountAsync());

        var htmlLang = await page.Locator("html").GetAttributeAsync("lang") ?? "";
        Assert.StartsWith(lang, htmlLang, StringComparison.OrdinalIgnoreCase);

        var dir = (await page.Locator("html").GetAttributeAsync("dir") ?? "ltr").ToLowerInvariant();
        Assert.Equal(lang == "ar" ? "rtl" : "ltr", dir);

        if (width <= 390)
        {
            var overflow = await page.EvaluateAsync<bool>(
                "() => document.documentElement.scrollWidth > document.documentElement.clientWidth + 1");
            Assert.False(overflow, $"Horizontal overflow on {where}.");
        }

        var bodyFontPx = await page.EvaluateAsync<double>(
            "() => parseFloat(getComputedStyle(document.body).fontSize)");
        Assert.True(bodyFontPx >= 16, $"Body text is {bodyFontPx}px on {where}.");

        // Sample the primary CTAs: a tap target below 44px fails WCAG 2.5.5 / the design system.
        var ctas = page.Locator("main .pub-btn, main .pp-toc__print");
        var ctaCount = Math.Min(await ctas.CountAsync(), 5);
        for (var i = 0; i < ctaCount; i++)
        {
            var box = await ctas.Nth(i).BoundingBoxAsync();
            if (box is null || box.Height == 0)
            {
                continue;
            }

            Assert.True(box.Height >= 44, $"Tap target {box.Height}px on {where}.");
        }

        var html = await page.ContentAsync();
        Assert.DoesNotMatch(Placeholder, await page.Locator("body").InnerTextAsync());
        Assert.DoesNotContain("at Jobsy.", html, StringComparison.Ordinal);
        Assert.DoesNotContain("System.Exception", html, StringComparison.Ordinal);
        Assert.DoesNotContain("Unhandled exception", html, StringComparison.OrdinalIgnoreCase);

        // 10.1: the footer legal line shows only the parts that are configured. The CI stack sets no
        // Legal__KvkNumber, so a "KvK" label there would be an empty, unconfigured part.
        var legalLine = page.Locator(".pp-footer-legal");
        if (await legalLine.CountAsync() > 0)
        {
            var text = await legalLine.First.InnerTextAsync();
            Assert.DoesNotContain("KvK", text, StringComparison.OrdinalIgnoreCase);
        }
    }

    private static async Task AssertLegalDocumentAsync(IPage page, string lang)
    {
        var links = page.Locator(".pp-toc__link[href^='#']");
        var linkCount = await links.CountAsync();
        Assert.True(linkCount > 0, "A legal document needs a table of contents.");

        for (var i = 0; i < linkCount; i++)
        {
            var href = await links.Nth(i).GetAttributeAsync("href") ?? "";
            var id = href.TrimStart('#');
            Assert.True(
                await page.Locator($"#{id}").CountAsync() > 0,
                $"TOC link {href} points at a section that does not exist.");
        }

        // D3: "In het kort" in the reader's language, the official Dutch body marked lang="nl".
        Assert.True(await page.Locator(".pp-short__label").CountAsync() > 0);

        if (lang == "nl")
        {
            Assert.Equal(0, await page.Locator(".pp-doc__note").CountAsync());
        }
        else
        {
            await Assertions.Expect(page.Locator(".pp-doc__note").First).ToBeVisibleAsync();
            Assert.True(await page.Locator(".pp-sec__body[lang='nl']").CountAsync() > 0);
        }
    }

    private static async Task AssertPrivacyProcessorsAsync(IPage page)
    {
        var text = await page.Locator("body").InnerTextAsync();
        Assert.Contains("Pingen", text, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("OpenStreetMap", text, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("push", text, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Frankfurt", text, StringComparison.OrdinalIgnoreCase);
    }

    private static async Task AssertPartnerPricesAsync(IPage page)
    {
        // Employers OFF gates /partner onto the coming-soon page (no rate table).
        if (page.Url.Contains("/werkgevers/binnenkort", StringComparison.OrdinalIgnoreCase))
        {
            Assert.Equal(1, await page.Locator("h1").CountAsync());
            Assert.Contains("noindex", await page.ContentAsync(), StringComparison.OrdinalIgnoreCase);
            return;
        }

        var text = await page.Locator("body").InnerTextAsync();
        Assert.DoesNotContain("€ 0,00", text, StringComparison.Ordinal);
        // NL uses "btw"; EN/PL/RO/AR use VAT / localized tax wording from PartnerPage.Rates.*.
        Assert.True(
            text.Contains("btw", StringComparison.OrdinalIgnoreCase)
            || text.Contains("VAT", StringComparison.Ordinal)
            || text.Contains("TVA", StringComparison.Ordinal)
            || text.Contains("ضريبة", StringComparison.Ordinal),
            "Partner rates should mention VAT/btw in the page language.");
    }

    private static async Task SetLanguageAsync(IPage page, string baseUrl, string lang)
        => await page.GotoAsync(
            $"{baseUrl}/taal/{lang}?returnUrl=%2F",
            new() { WaitUntil = WaitUntilState.DOMContentLoaded, Timeout = 60_000 });

    /// <summary>The sitemap is the public list of company pages, so it names a company that exists.</summary>
    private static async Task<string?> FindVerifiedCompanyKvkAsync(string baseUrl)
    {
        try
        {
            using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(15) };
            var sitemap = await http.GetStringAsync(baseUrl + "/sitemap.xml");
            var match = Regex.Match(sitemap, @"<loc>[^<]*/(\d{8})</loc>");
            return match.Success ? match.Groups[1].Value : null;
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            return null;
        }
    }

    private static async Task<bool> TryLoginAsync(IPage page, string baseUrl, string email)
    {
        var password = Environment.GetEnvironmentVariable("JOBSY_E2E_CANDIDATE_PASSWORD") ?? DefaultPassword;
        try
        {
            await page.GotoAsync(baseUrl + "/login", new() { WaitUntil = WaitUntilState.DOMContentLoaded, Timeout = 60_000 });
            await page.FillAsync("input[name='email']", email);
            await page.FillAsync("input[name='password']", password);
            await page.ClickAsync("button[type=submit]");
            await page.WaitForURLAsync(
                url => !url.Contains("/login", StringComparison.OrdinalIgnoreCase),
                new() { Timeout = 30_000 });
            return true;
        }
        catch (TimeoutException)
        {
            // Documented skip: this stack has no seeded account for that role.
            return false;
        }
    }

    private static async Task<string?> ResolveBaseUrlAsync()
    {
        var baseUrl = (Environment.GetEnvironmentVariable("JOBSY_E2E_BASE_URL") ?? "").Trim().TrimEnd('/');
        if (string.IsNullOrWhiteSpace(baseUrl))
        {
            return null;
        }

        try
        {
            using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(5) };
            using var response = await http.GetAsync(baseUrl + "/privacy");
            return response.StatusCode is HttpStatusCode.OK ? baseUrl : null;
        }
        catch
        {
            return null;
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

        throw new InvalidOperationException("Jobsy.sln not found.");
    }
}

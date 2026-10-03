using System.Globalization;
using System.Net;
using System.Text;
using Bunit;
using Deque.AxeCore.Commons;
using Deque.AxeCore.Playwright;
using Jobsy.Web.Components.Landing;
using Jobsy.Web.Components.Layout;
using Jobsy.Web.Components.Pages;
using Jobsy.Web.Features;
using Jobsy.Web.Localization;
using Jobsy.Web.Seo;
using Jobsy.Web.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.JSInterop;
using Microsoft.Playwright;

namespace Jobsy.Tests;

/// <summary>
/// Landing WCAG 2.2 AA: contrast of eyebrows, unavailable CTAs and the closing lead,
/// plus a single h1 with no skipped heading levels.
/// </summary>
public class LandingA11yTests : LandingA11yHarness
{
    [Theory]
    [InlineData("on")]
    [InlineData("zw")]
    public void Headings_are_one_h1_and_do_not_skip_levels(string variant)
    {
        var cut = RenderLanding(variant);
        var headings = cut.FindAll("h1, h2, h3, h4, h5, h6");
        Assert.NotEmpty(headings);
        Assert.Equal("H1", headings[0].TagName);
        Assert.Equal(1, headings.Count(h => h.TagName == "H1"));

        var previous = 1;
        foreach (var heading in headings)
        {
            var level = int.Parse(heading.TagName.AsSpan(1), CultureInfo.InvariantCulture);
            Assert.True(level <= previous + 1,
                $"{variant}: {heading.TagName} \"{Trim(heading.TextContent)}\" follows h{previous}");
            previous = level;
        }
    }

    [Theory]
    [InlineData(LandingVariant.On, 2)]
    [InlineData(LandingVariant.Zw, 1)]
    public void Unavailable_ctas_are_readable_and_marked_coming_soon(LandingVariant variant, int expected)
    {
        var cut = Render<LandingForWhom>(p => p.Add(c => c.Variant, variant));
        var disabled = cut.FindAll(".pub-btn--disabled");
        Assert.Equal(expected, disabled.Count);
        foreach (var el in disabled)
        {
            Assert.Equal("true", el.GetAttribute("aria-disabled"));
            Assert.Equal("link", el.GetAttribute("role"));
            Assert.Contains("Binnenkort", el.TextContent, StringComparison.Ordinal);
            Assert.NotNull(el.QuerySelector(".pub-btn__soon"));
        }
    }

    [Fact]
    public void Coral_ink_and_closing_lead_meet_aa_on_public_surfaces()
    {
        var root = RepoRoot();
        var app = File.ReadAllText(Path.Combine(root, "Jobsy.Web", "wwwroot", "css", "app.css"));
        var theme = File.ReadAllText(Path.Combine(root, "Jobsy.Web", "wwwroot", "css", "features", "public-theme.css"));
        var landing = File.ReadAllText(Path.Combine(root, "Jobsy.Web", "wwwroot", "css", "features", "landing.css"));
        var pages = File.ReadAllText(Path.Combine(root, "Jobsy.Web", "wwwroot", "css", "features", "public-pages.css"));

        var surface = Hex(app, "--surface");
        var warn = Hex(app, "--warn-soft");
        var gold = Hex(app, "--gold");
        var coral = Hex(app, "--coral");
        var accentSoft = Hex(app, "--accent-soft");
        var border = Hex(app, "--border");
        var text = Hex(app, "--text");
        var ink = Hex(theme, "--pub-coral-ink");
        var mutedInk = Hex(theme, "--pub-ink-muted");

        var pearl = warn;
        var trust = Mix(gold, surface, 0.12);
        var kreeft = Mix(accentSoft, surface, 0.55);
        var coralSoft = Mix(coral, surface, 0.12);
        var closing = Mix(gold, surface, 0.32);
        var disabledBg = Mix(border, surface, 0.50);

        foreach (var (name, bg) in new[] { ("white", "#ffffff"), ("pearl", pearl), ("surface", surface), ("trust", trust), ("kreeft", kreeft), ("coral-soft", coralSoft) })
        {
            var ratio = Contrast(ink, bg);
            Assert.True(ratio >= 4.5, $"coral ink {ink} on {name} {bg} is {ratio:0.00}:1");
        }

        var closingRatio = Contrast(mutedInk, closing);
        Assert.True(closingRatio >= 4.5, $"closing lead {mutedInk} on {closing} is {closingRatio:0.00}:1");
        Assert.True(Contrast(text, disabledBg) >= 4.5, "unavailable CTA text");
        Assert.True(Contrast(text, surface) >= 4.5, "binnenkort chip");

        Assert.Contains("var(--pub-coral-ink)", Rule(landing, ".pub-theme .pub-landing__eyebrow"), StringComparison.Ordinal);
        Assert.Contains("var(--pub-ink-muted)", Rule(landing, ".pub-theme .pub-landing__final p"), StringComparison.Ordinal);
        var disabledRule = Rule(theme, ".pub-theme .pub-btn--disabled");
        Assert.DoesNotContain("opacity", disabledRule, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("var(--text)", disabledRule, StringComparison.Ordinal);
        Assert.Contains("var(--pub-coral-ink)", Rule(theme, ".pub-theme .pub-eyebrow"), StringComparison.Ordinal);

        foreach (var selector in new[]
        {
            ".pub-theme .pp-doc__eyebrow",
            ".pub-theme .pp-switch__link--active",
            ".pub-theme .pp-toc__link[aria-current=\"location\"]",
            ".pub-theme .pp-partner__free",
            ".pub-theme .pp-company__tab--active"
        })
        {
            Assert.Contains("var(--pub-coral-ink)", Rule(pages, selector), StringComparison.Ordinal);
        }

        Assert.Contains(".pub-landing__you-card h2", landing, StringComparison.Ordinal);
        Assert.Contains(".pub-landing__job h4", landing, StringComparison.Ordinal);
        Assert.Contains(".pub-landing__tg h3", landing, StringComparison.Ordinal);
        Assert.Contains("font-size: 0.67em", Rule(landing, ".pub-theme .pub-landing__rep h5"), StringComparison.Ordinal);
        Assert.Contains("font-size: 1em", Rule(landing, ".pub-theme .pub-landing__tg h3"), StringComparison.Ordinal);
    }

    private static string Trim(string text)
    {
        var collapsed = string.Join(' ', text.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
        return collapsed.Length <= 60 ? collapsed : collapsed[..60];
    }

    private static string Rule(string css, string selector)
    {
        var start = css.IndexOf(selector, StringComparison.Ordinal);
        Assert.True(start >= 0, $"missing selector {selector}");
        var open = css.IndexOf('{', start);
        var depth = 0;
        for (var i = open; i < css.Length; i++)
        {
            if (css[i] == '{')
            {
                depth++;
            }
            else if (css[i] == '}')
            {
                depth--;
                if (depth == 0)
                {
                    return css[open..(i + 1)];
                }
            }
        }

        throw new InvalidOperationException($"unclosed rule {selector}");
    }

    private static string Hex(string css, string token)
    {
        var marker = token + ":";
        var at = css.IndexOf(marker, StringComparison.Ordinal);
        Assert.True(at >= 0, $"missing token {token}");
        var rest = css[(at + marker.Length)..].TrimStart();
        var end = rest.IndexOf(';');
        var value = rest[..end].Trim();
        Assert.StartsWith("#", value, StringComparison.Ordinal);
        return value[..7];
    }

    private static string Mix(string a, string b, double weightA)
    {
        var (ar, ag, ab) = Rgb(a);
        var (br, bg, bb) = Rgb(b);
        return FormattableString.Invariant($"#{Chan(ar, br, weightA):x2}{Chan(ag, bg, weightA):x2}{Chan(ab, bb, weightA):x2}");
    }

    private static int Chan(int a, int b, double weightA) => (int)Math.Round((weightA * a) + ((1 - weightA) * b));

    private static (int R, int G, int B) Rgb(string hex)
    {
        hex = hex.TrimStart('#');
        return (
            int.Parse(hex.AsSpan(0, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture),
            int.Parse(hex.AsSpan(2, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture),
            int.Parse(hex.AsSpan(4, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture));
    }

    private static double Contrast(string foreground, string background)
    {
        var l1 = Lum(foreground);
        var l2 = Lum(background);
        var hi = Math.Max(l1, l2);
        var lo = Math.Min(l1, l2);
        return (hi + 0.05) / (lo + 0.05);
    }

    private static double Lum(string hex)
    {
        var (r, g, b) = Rgb(hex);
        return (0.2126 * Lin(r)) + (0.7152 * Lin(g)) + (0.0722 * Lin(b));
    }

    private static double Lin(int channel)
    {
        var c = channel / 255d;
        return c <= 0.04045 ? c / 12.92 : Math.Pow((c + 0.055) / 1.055, 2.4);
    }
}

/// <summary>
/// axe color-contrast and heading-order on the rendered landing, mobile and desktop, both variants.
/// Uses the real public CSS (no live server).
/// </summary>
[Collection("PlaywrightSmoke")]
public class LandingAxePlaywrightTests : LandingA11yHarness
{
    [Fact]
    public async Task Landing_passes_axe_contrast_and_heading_order()
    {
        var exit = Microsoft.Playwright.Program.Main(["install", "chromium"]);
        Assert.Equal(0, exit);

        var sheets = new[]
        {
            Path.Combine(RepoRoot(), "Jobsy.Web", "wwwroot", "css", "app.css"),
            Path.Combine(RepoRoot(), "Jobsy.Web", "wwwroot", "css", "features", "public-theme.css"),
            Path.Combine(RepoRoot(), "Jobsy.Web", "wwwroot", "css", "features", "landing.css")
        };
        var css = string.Concat(sheets.Select(path => "<style>" + File.ReadAllText(path) + "</style>"));

        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new()
        {
            Headless = true,
            Args = ["--no-sandbox", "--disable-dev-shm-usage"]
        });

        foreach (var variant in new[] { "on", "zw" })
        {
            var markup = RenderLanding(variant).Markup;
            var html = "<!doctype html><html lang=\"nl\"><head><meta charset=\"utf-8\">" + css + "</head><body>" + markup + "</body></html>";
            var file = Path.Combine(Path.GetTempPath(), $"lobsy-landing-a11y-{variant}.html");
            await File.WriteAllTextAsync(file, html, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));

            foreach (var (width, height) in new[] { (1440, 900), (390, 844) })
            {
                await using var context = await browser.NewContextAsync(new()
                {
                    ViewportSize = new() { Width = width, Height = height },
                    Locale = "nl-NL"
                });
                var page = await context.NewPageAsync();
                await page.GotoAsync(new Uri(file).AbsoluteUri, new() { WaitUntil = WaitUntilState.DOMContentLoaded, Timeout = 60_000 });

                var result = await page.RunAxe(
                    new AxeRunContext { Include = [new AxeSelector(".pub-landing")] },
                    new AxeRunOptions { RunOnly = RunOnlyOptions.Rules("color-contrast", "heading-order") });

                var violations = result.Violations ?? [];
                Assert.False(violations.Any(), Format(variant, width, violations));
            }
        }
    }

    private static string Format(string variant, int width, IEnumerable<AxeResultItem> violations)
    {
        var sb = new StringBuilder();
        sb.Append(CultureInfo.InvariantCulture, $"axe violations on landing variant={variant} width={width}: ");
        foreach (var violation in violations)
        {
            sb.Append(violation.Id).Append(" — ");
            foreach (var node in violation.Nodes ?? [])
            {
                sb.Append(node.Target).Append(' ').Append(node.Html).Append(" | ").Append(node.Impact).Append("; ");
            }
        }

        return sb.ToString();
    }
}

public class LandingA11yHarness : BunitContext
{
    public LandingA11yHarness()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddSingleton<AuthenticationStateProvider>(new LandingA11yAuth());
        Services.AddSingleton<IConfiguration>(new ConfigurationBuilder().Build());
        Services.AddSingleton<ICookieConsentTokenService, CookieConsentTokenService>();
        Services.AddSingleton<IEmployersSwitch, AlwaysOnEmployersSwitch>();
        Services.AddSingleton<IHostEnvironment>(new LandingA11yEnv());
        Services.AddSingleton<IHttpContextAccessor>(new HttpContextAccessor { HttpContext = new DefaultHttpContext() });
        Services.AddMemoryCache();
        Services.AddSingleton<IHttpClientFactory>(new LandingA11yHttp());
        Services.AddSingleton<LandingStatsClient>();
        Services.AddSingleton<LandingPriceClient>();
        Services.AddLogging();
        Services.AddSingleton<LegalIdentityProvider>();
        Services.AddScoped<LandingVariantResolver>();
        Services.AddScoped(sp => new CultureState(
            sp.GetRequiredService<IJSRuntime>(),
            sp,
            sp.GetRequiredService<AuthenticationStateProvider>()));
        Services.AddScoped<PageSeoContext>();
        Services.AddSingleton(new Jobsy.Web.Hosting.MaintenanceState());
        Services.AddSingleton<NavigationManager>(new LandingA11yNav());
    }

    protected IRenderedComponent<PublicLayout> RenderLanding(string variant)
    {
        var http = Services.GetRequiredService<IHttpContextAccessor>().HttpContext!;
        http.Items.Remove(LandingVariantResolver.HttpContextItemsKey);
        http.Request.QueryString = new QueryString("?_variant=" + variant);
        return Render<PublicLayout>(p => p.Add(c => c.Body, (RenderFragment)(b =>
        {
            b.OpenComponent<Landing>(0);
            b.CloseComponent();
        })));
    }

    protected static string RepoRoot()
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

        throw new InvalidOperationException("Jobsy.sln not found");
    }

    private sealed class LandingA11yAuth : AuthenticationStateProvider
    {
        public override Task<AuthenticationState> GetAuthenticationStateAsync()
            => Task.FromResult(new AuthenticationState(new System.Security.Claims.ClaimsPrincipal(new System.Security.Claims.ClaimsIdentity())));
    }

    private sealed class LandingA11yEnv : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = Environments.Development;
        public string ApplicationName { get; set; } = "Jobsy.Tests";
        public string ContentRootPath { get; set; } = AppContext.BaseDirectory;
        public Microsoft.Extensions.FileProviders.IFileProvider ContentRootFileProvider { get; set; }
            = new Microsoft.Extensions.FileProviders.NullFileProvider();
    }

    private sealed class LandingA11yNav : NavigationManager
    {
        public LandingA11yNav() => Initialize("http://localhost/", "http://localhost/");
        protected override void NavigateToCore(string uri, bool forceLoad) { }
    }

    private sealed class LandingA11yHttp : IHttpClientFactory
    {
        public HttpClient CreateClient(string name)
            => new(new LandingA11yHandler()) { BaseAddress = new Uri("http://localhost:5200/") };
    }

    private sealed class LandingA11yHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            => Task.FromResult(new HttpResponseMessage(HttpStatusCode.RequestTimeout));
    }
}

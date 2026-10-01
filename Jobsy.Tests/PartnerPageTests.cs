using System.Net;
using System.Text;
using Bunit;
using Jobsy.Core.Features;
using Jobsy.Web.Components.Pages.Partner;
using Jobsy.Web.Localization;
using Jobsy.Web.Navigation;
using Jobsy.Web.Seo;
using Jobsy.Web.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;

namespace Jobsy.Tests;

/// <summary>
/// The static <c>/partner</c> page (public-pages 09.2): B1 copy, amounts excl. btw with the incl.
/// amount next to them (D5), "Gratis" for free vacancy types and no jargon.
/// </summary>
public class PartnerPageTests : TestContext
{
    /// <summary>1 token = € 12,50 incl. btw → € 10,33 excl. Vrijwilligerswerk is free.</summary>
    private const string CatalogJson =
        """
        {
          "baseTokenValueEuro": 12.50,
          "highlightCarouselTokens": 1,
          "highlightPulseTokens": 1,
          "highlightCarouselDays": 7,
          "startHighlightBonusTokens": 2,
          "vacancyTypeCosts": [
            { "kind": "Regular", "label": "Vacature (regulier)", "costTokens": 2, "priceEuro": 25.00, "isActive": true },
            { "kind": "SideJob", "label": "Bijbaan", "costTokens": 1, "priceEuro": 12.50, "isActive": true },
            { "kind": "Volunteer", "label": "Vrijwilligerswerk", "costTokens": 0, "priceEuro": 0.00, "isActive": true }
          ],
          "packages": []
        }
        """;

    private readonly DefaultHttpContext _http = new();
    private readonly AmbientCultureScope _culture = new();
    private HttpStatusCode _catalogStatus = HttpStatusCode.OK;

    public PartnerPageTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddLogging();
        Services.AddSingleton<IConfiguration>(new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["PublicWebBaseUrl"] = "https://lobsy.nl"
            })
            .Build());
        Services.AddSingleton<IHttpContextAccessor>(new HttpContextAccessor { HttpContext = _http });
        Services.AddSingleton(new JobsyApiClient(new HttpClient(new CatalogHandler(() => _catalogStatus))
        {
            BaseAddress = new Uri("http://localhost/")
        }));
        Services.AddSingleton<AuthenticationStateProvider>(new AnonymousAuth());
        Services.AddScoped(sp => new CultureState(
            sp.GetRequiredService<IJSRuntime>(),
            sp,
            sp.GetRequiredService<AuthenticationStateProvider>()));
        Services.AddScoped<PageSeoContext>();
        Services.AddSingleton<NavigationManager>(new PartnerStaticNavigation(PublicRoutes.Partner));
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing)
        {
            _culture.Dispose();
        }
    }

    private IRenderedComponent<PartnerSales> Render() => RenderComponent<PartnerSales>();

    private static string PageSource => File.ReadAllText(Path.Combine(
        HowLobsyRenderTestBase.RepoRoot(),
        "Jobsy.Web", "Components", "Pages", "Partner", "PartnerSales.razor"));

    [Fact]
    public void The_page_is_static_ssr_without_an_interactive_root()
    {
        var source = PageSource;

        Assert.Contains("[ExcludeFromInteractiveRouting]", source, StringComparison.Ordinal);
        Assert.Contains("[NoBlazorRuntime]", source, StringComparison.Ordinal);
        Assert.Contains("@layout PublicLayout", source, StringComparison.Ordinal);
        Assert.DoesNotContain("InteractiveServer", source, StringComparison.Ordinal);
        Assert.DoesNotContain("@onclick", source, StringComparison.Ordinal);
    }

    [Fact]
    public void The_hero_sells_the_page_in_b1_and_links_to_register_and_the_rates()
    {
        var cut = Render();

        Assert.Equal(UiStrings.Get("PartnerPage.Title"), cut.Find("h1.pp-doc__title").TextContent.Trim());
        Assert.Contains(UiStrings.Get("PartnerPage.Lead"), cut.Markup, StringComparison.Ordinal);

        var ctas = cut.FindAll(".pp-partner__cta a").Select(a => a.GetAttribute("href")).ToList();
        Assert.Contains(PublicRoutes.CompanyRegister, ctas);
        Assert.Contains("#tarieven", ctas);
    }

    [Fact]
    public void A_sales_code_rides_along_in_the_register_link_and_the_flyer_link()
    {
        var cut = RenderComponent<PartnerSales>(p => p.Add(x => x.TrackingCode, "SM-7K2Q9D"));

        Assert.Equal(
            $"{PublicRoutes.CompanyRegister}?ref=SM-7K2Q9D",
            cut.Find(".pp-partner__cta a").GetAttribute("href"));
        Assert.Contains("SM-7K2Q9D", cut.Find(".pp-partner__code").TextContent, StringComparison.Ordinal);
    }

    [Fact]
    public void A_free_vacancy_type_says_gratis_and_never_shows_a_zero_amount()
    {
        var cut = Render();
        var rows = cut.FindAll(".pp-partner__table tbody tr").ToList();

        var free = rows.Single(r => r.TextContent.Contains("Vrijwilligerswerk", StringComparison.Ordinal));
        Assert.Contains(UiStrings.Get("PartnerPage.Rates.Free"), free.TextContent, StringComparison.Ordinal);
        Assert.DoesNotContain("0,00", free.TextContent, StringComparison.Ordinal);
        Assert.DoesNotContain("0 tokens", cut.Markup, StringComparison.Ordinal);
    }

    [Fact]
    public void Amounts_are_excl_btw_with_the_incl_amount_next_to_them()
    {
        var cut = Render();
        var rows = cut.FindAll(".pp-partner__table tbody tr").ToList();

        var sideJob = rows.Single(r => r.TextContent.Contains("Bijbaan", StringComparison.Ordinal));
        Assert.Contains("10,33", sideJob.QuerySelector("strong")!.TextContent, StringComparison.Ordinal);
        Assert.Contains("12,50", sideJob.QuerySelector(".pp-partner__incl")!.TextContent, StringComparison.Ordinal);

        var regular = rows.Single(r => r.TextContent.Contains("regulier", StringComparison.Ordinal));
        Assert.Contains("20,66", regular.QuerySelector("strong")!.TextContent, StringComparison.Ordinal);
        Assert.Contains("25,00", regular.QuerySelector(".pp-partner__incl")!.TextContent, StringComparison.Ordinal);
    }

    [Fact]
    public void The_token_value_and_the_vat_line_say_excl_btw()
    {
        var cut = Render();

        Assert.Contains("10,33", cut.Find(".pp-partner__token-value").TextContent, StringComparison.Ordinal);
        Assert.Equal(
            UiStrings.Get("PartnerPage.Rates.ExclVatNote"),
            cut.Find(".pp-partner__vat-note").TextContent.Trim());
        Assert.Contains("exclusief btw", cut.Markup, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(
            UiStrings.Get("PartnerPage.Rates.PackagesNote"),
            cut.Find(".pp-partner__packages-note").TextContent.Trim());
    }

    [Fact]
    public void The_highlight_row_names_its_window_and_there_is_no_pulse_row()
    {
        var cut = Render();
        var rows = cut.FindAll(".pp-partner__table tbody tr").ToList();

        Assert.Contains(rows, r => r.TextContent.Contains("Highlight (7 dagen)", StringComparison.Ordinal));
        Assert.DoesNotContain("Pulse", cut.Markup, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void A_catalog_failure_drops_the_rates_section_and_never_shows_an_exception()
    {
        _catalogStatus = HttpStatusCode.InternalServerError;
        var cut = Render();

        Assert.Equal(
            UiStrings.Get("PartnerPage.RatesUnavailable"),
            cut.Find(".pp-partner__rates-empty").TextContent.Trim());
        Assert.Empty(cut.FindAll(".pp-partner__table"));
        Assert.DoesNotContain("Exception", cut.Markup, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("boom", cut.Markup, StringComparison.Ordinal);
        Assert.NotNull(cut.Find("h1.pp-doc__title"));
    }

    [Fact]
    public void Sharing_works_without_javascript_as_plain_links()
    {
        var cut = RenderComponent<PartnerSales>(p => p.Add(x => x.TrackingCode, "SM-7K2Q9D"));
        var links = cut.FindAll(".pp-partner__share-link")
            .Select(a => a.GetAttribute("href")!)
            .ToList();

        Assert.Equal(3, links.Count);
        Assert.Contains(links, href => href.StartsWith("https://wa.me/?text=", StringComparison.Ordinal));
        Assert.Contains(links, href => href.StartsWith("mailto:?subject=", StringComparison.Ordinal));
        Assert.Contains(links, href => href == "/partner/flyer.pdf?code=SM-7K2Q9D");
        Assert.Empty(cut.FindAll("button"));
    }

    [Fact]
    public void The_share_subject_drops_the_region_and_has_no_double_encoded_newlines()
    {
        var mail = Render().FindAll(".pp-partner__share-link")
            .Select(a => a.GetAttribute("href")!)
            .Single(href => href.StartsWith("mailto:", StringComparison.Ordinal));

        Assert.Contains(
            Uri.EscapeDataString(UiStrings.Get("PartnerPage.Share.Subject")),
            mail,
            StringComparison.Ordinal);
        Assert.DoesNotContain("Westland", mail, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("%250A", mail, StringComparison.Ordinal);
    }

    [Fact]
    public void No_jargon_is_left_in_the_page_or_in_the_partner_strings()
    {
        var markup = Render().Markup;
        foreach (var jargon in new[] { "spill", "Funda", "Westland & Den Haag", "Pulse" })
        {
            Assert.DoesNotContain(jargon, markup, StringComparison.OrdinalIgnoreCase);
        }

        foreach (var key in new[] { "Partner.Usp1", "Partner.Usp2" })
        {
            foreach (var language in new[] { "nl", "en", "pl", "ro", "ar" })
            {
                var value = UiStrings.Get(key, language);
                Assert.DoesNotContain("spill", value, StringComparison.OrdinalIgnoreCase);
                Assert.DoesNotContain("Funda", value, StringComparison.OrdinalIgnoreCase);
            }
        }
    }

    [Fact]
    public void The_printable_flyer_has_no_jargon_either()
    {
        var source = File.ReadAllText(Path.Combine(
            HowLobsyRenderTestBase.RepoRoot(),
            "Jobsy.Infrastructure", "Services", "PartnerFlyerPdfService.cs"));

        Assert.DoesNotContain("spill", source, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Funda-model", source, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData("nl")]
    [InlineData("en")]
    [InlineData("pl")]
    [InlineData("ro")]
    [InlineData("ar")]
    public void Every_language_renders_its_own_copy_without_raw_keys_or_inline_styles(string language)
    {
        _http.Request.QueryString = new QueryString($"?lang={language}");
        Services.GetRequiredService<CultureState>().InitializeFromRequest(_http);
        var cut = Render();

        Assert.Equal(
            UiStrings.Get("PartnerPage.Title", language),
            cut.Find("h1.pp-doc__title").TextContent.Trim());
        Assert.DoesNotContain("PartnerPage.", cut.Markup, StringComparison.Ordinal);
        Assert.DoesNotContain("style=", cut.Markup, StringComparison.Ordinal);
    }

    private sealed class AnonymousAuth : AuthenticationStateProvider
    {
        public override Task<AuthenticationState> GetAuthenticationStateAsync()
            => Task.FromResult(new AuthenticationState(
                new System.Security.Claims.ClaimsPrincipal(new System.Security.Claims.ClaimsIdentity())));
    }

    private sealed class CatalogHandler(Func<HttpStatusCode> status) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var code = status();
            if (code != HttpStatusCode.OK)
            {
                return Task.FromResult(new HttpResponseMessage(code)
                {
                    Content = new StringContent("boom", Encoding.UTF8, "text/plain")
                });
            }

            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(CatalogJson, Encoding.UTF8, "application/json")
            });
        }
    }

    private sealed class PartnerStaticNavigation : NavigationManager
    {
        public PartnerStaticNavigation(string relativePath)
            => Initialize("http://localhost/", "http://localhost" + relativePath);

        protected override void NavigateToCore(string uri, bool forceLoad)
            => throw new InvalidOperationException($"The page must not redirect, but navigated to {uri}.");
    }
}

/// <summary>Werkgevers actief OFF hides the partner page completely (09.2, Dependency F).</summary>
public class PartnerGateTests
{
    [Fact]
    public void The_page_requires_the_employers_feature_and_falls_back_to_the_home_page()
    {
        var attribute = typeof(PartnerSales)
            .GetCustomAttributes(typeof(RequiresFeatureAttribute), inherit: true)
            .Cast<RequiresFeatureAttribute>()
            .Single();

        Assert.Equal(PlatformFeature.Employers, attribute.Feature);
        Assert.True(attribute.WhenEnabled);
        Assert.Equal("/", attribute.FallbackPath);
    }

    [Fact]
    public void The_company_page_is_gated_the_same_way()
    {
        var attribute = typeof(Jobsy.Web.Components.Pages.CompanyPublicPage)
            .GetCustomAttributes(typeof(RequiresFeatureAttribute), inherit: true)
            .Cast<RequiresFeatureAttribute>()
            .Single();

        Assert.Equal(PlatformFeature.Employers, attribute.Feature);
        Assert.Equal("/", attribute.FallbackPath);
    }

    [Fact]
    public void The_sitemap_drops_the_partner_page_when_employers_are_off()
    {
        var off = PageSeoCatalog.StaticIndexablePathsFor(
            new FeatureFlagSnapshot(EmployersEnabled: false, CandidatePassportEnabled: false));
        var on = PageSeoCatalog.StaticIndexablePathsFor(
            new FeatureFlagSnapshot(EmployersEnabled: true, CandidatePassportEnabled: false));

        Assert.DoesNotContain(PublicRoutes.Partner, off);
        Assert.Contains(PublicRoutes.Partner, on);
    }

    [Fact]
    public void The_flyer_download_is_a_web_endpoint_that_follows_the_same_gate()
    {
        var source = File.ReadAllText(Path.Combine(
            HowLobsyRenderTestBase.RepoRoot(), "Jobsy.Web", "Hosting", "PartnerFlyerEndpoints.cs"));

        Assert.Equal("/partner/flyer.pdf", Jobsy.Web.Hosting.PartnerFlyerEndpoints.Path);
        Assert.Contains("IEmployersSwitch", source, StringComparison.Ordinal);
        Assert.Contains("Results.Redirect(\"/\")", source, StringComparison.Ordinal);
        Assert.Contains("RequireRateLimiting(\"partner-flyer\")", source, StringComparison.Ordinal);
    }
}

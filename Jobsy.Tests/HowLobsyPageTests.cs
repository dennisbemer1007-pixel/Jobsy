using System.Net;
using System.Security.Claims;
using System.Text;
using Bunit;
using Jobsy.Core.Authorization;
using Jobsy.Core.Features;
using Jobsy.Core.Privacy;
using Jobsy.Web.Components.Pages;
using Jobsy.Web.Features;
using Jobsy.Web.Help;
using Jobsy.Web.Localization;
using Jobsy.Web.Navigation;
using Jobsy.Web.Seo;
using Jobsy.Web.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.JSInterop;

namespace Jobsy.Tests;

/// <summary>
/// bUnit host for the static <c>/hoe-werkt-lobsy</c> page (public-pages 08). The route is static
/// SSR, so rendering the component is exactly what a visitor's first byte contains.
/// </summary>
public abstract class HowLobsyRenderTestBase : BunitContext
{
    private readonly DefaultHttpContext _http = new();
    private readonly AmbientCultureScope _culture = new();

    protected HowLobsyRenderTestBase()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddLogging();
        Services.AddMemoryCache();
        Services.AddSingleton<IConfiguration>(new ConfigurationBuilder().Build());
        Services.AddSingleton<ICookieConsentTokenService, CookieConsentTokenService>();
        Services.AddSingleton<IHostEnvironment>(new HowLobsyHostEnvironment());
        Services.AddSingleton<IHttpContextAccessor>(new HttpContextAccessor { HttpContext = _http });
        Services.AddSingleton<IHttpClientFactory>(new HowLobsyHttpClientFactory());
        Services.AddSingleton(new JobsyApiClient(new HttpClient(new HowLobsyApiHandler())
        {
            BaseAddress = new Uri("http://localhost/")
        }));
        Services.AddSingleton<LandingPriceClient>();
        Services.AddScoped<LandingVariantResolver>();
        Services.AddScoped(sp => new CultureState(
            sp.GetRequiredService<IJSRuntime>(),
            sp,
            sp.GetRequiredService<AuthenticationStateProvider>()));
        Services.AddScoped<PageSeoContext>();
        Services.AddSingleton<NavigationManager>(new HowLobsyStaticNavigation(HowLobsyRoleGuides.SharedPath));

        Anonymous();
        EmployersOn();
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing)
        {
            _culture.Dispose();
        }
    }

    protected void EmployersOn() => SetSwitch(LandingVariant.On);

    protected void EmployersOff() => SetSwitch(LandingVariant.Zw);

    private void SetSwitch(LandingVariant variant)
    {
        Services.RemoveAll<IEmployersSwitch>();
        Services.RemoveAll<IFeatureFlags>();
        Services.AddSingleton<IEmployersSwitch>(new FixedEmployersSwitch(variant));
        Services.AddSingleton<IFeatureFlags>(new FixedFlags(variant == LandingVariant.On));
    }

    protected void Anonymous() => SignIn(new ClaimsPrincipal(new ClaimsIdentity()));

    protected void SignInAs(params string[] roles)
        => SignIn(new ClaimsPrincipal(new ClaimsIdentity(
            [
                new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString()),
                .. roles.Select(r => new Claim(ClaimTypes.Role, r))
            ],
            "Test")));

    private void SignIn(ClaimsPrincipal user)
    {
        _http.User = user;
        Services.RemoveAll<AuthenticationStateProvider>();
        Services.AddSingleton<AuthenticationStateProvider>(new FixedAuth(user));
    }

    protected void UseQuery(string queryString)
    {
        _http.Request.QueryString = new QueryString(queryString);
        _http.Items.Remove(LandingVariantResolver.HttpContextItemsKey);
    }

    protected void UseLanguage(string language)
    {
        UseQuery($"?lang={language}");
        Services.GetRequiredService<CultureState>().InitializeFromRequest(_http);
    }

    protected IRenderedComponent<HowLobsyWorks> Render() => Render<HowLobsyWorks>();

    protected static string PageSource => File.ReadAllText(Path.Combine(
        RepoRoot(), "Jobsy.Web", "Components", "Pages", "HowLobsyWorks.razor"));

    public static string RepoRoot()
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

    private sealed class FixedAuth(ClaimsPrincipal user) : AuthenticationStateProvider
    {
        public override Task<AuthenticationState> GetAuthenticationStateAsync()
            => Task.FromResult(new AuthenticationState(user));
    }

    private sealed class FixedEmployersSwitch(LandingVariant variant) : IEmployersSwitch
    {
        public ValueTask<bool> IsEnabledAsync(CancellationToken ct = default)
            => ValueTask.FromResult(variant == LandingVariant.On);

        public ValueTask<LandingVariant> VariantAsync(CancellationToken ct = default)
            => ValueTask.FromResult(variant);
    }

    private sealed class FixedFlags(bool employers) : IFeatureFlags
    {
        public ValueTask<FeatureFlagSnapshot> GetAsync(CancellationToken cancellationToken = default)
            => ValueTask.FromResult(new FeatureFlagSnapshot(employers, CandidatePassportEnabled: false));

        public ValueTask<bool> IsEnabledAsync(PlatformFeature feature, CancellationToken cancellationToken = default)
            => ValueTask.FromResult(new FeatureFlagSnapshot(employers, false).IsEnabled(feature));

        public void Invalidate()
        {
        }
    }

    private sealed class HowLobsyHostEnvironment : IHostEnvironment
    {
        public string ApplicationName { get; set; } = "Jobsy.Tests";
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
        public string ContentRootPath { get; set; } = AppContext.BaseDirectory;
        public string EnvironmentName { get; set; } = Environments.Production;
    }

    private sealed class HowLobsyHttpClientFactory : IHttpClientFactory
    {
        public HttpClient CreateClient(string name)
            => new(new HowLobsyPriceHandler()) { BaseAddress = new Uri("http://localhost/") };
    }

    private sealed class HowLobsyPriceHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
            => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(
                    """{"deepAnalysisPriceEuro":2.99}""", Encoding.UTF8, "application/json")
            });
    }

    /// <summary>The sales/ambassadeur dashboard answers with a tracking code.</summary>
    private sealed class HowLobsyApiHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
            => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("""{"trackingCode":"SM-TEST-1"}""", Encoding.UTF8, "application/json")
            });
    }

    private sealed class HowLobsyStaticNavigation : NavigationManager
    {
        public HowLobsyStaticNavigation(string relativePath)
            => Initialize("http://localhost/", "http://localhost" + relativePath);

        protected override void NavigateToCore(string uri, bool forceLoad)
            => throw new InvalidOperationException($"The page must not redirect, but navigated to {uri}.");
    }
}

/// <summary>The public page is complete static HTML for every visitor (08.2, 08.4).</summary>
public class HowLobsyPageTests : HowLobsyRenderTestBase
{
    [Fact]
    public void Anonymous_visitors_get_the_whole_story_without_a_loading_placeholder()
    {
        var cut = Render();

        Assert.Equal(UiStrings.Get("HowLobsy.Title"), cut.Find("h1.pp-doc__title").TextContent.Trim());
        Assert.DoesNotContain("Laden", cut.Markup, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(4, cut.FindAll("li.pp-how__step").Count);
        Assert.Equal(3, cut.FindAll("details.pp-how__fq").Count);
        Assert.Equal(4, cut.FindAll(".pp-how__promise-list li").Count);
    }

    [Fact]
    public void The_route_is_static_ssr_without_an_interactive_render_mode()
    {
        var source = PageSource;

        Assert.Contains("[ExcludeFromInteractiveRouting]", source, StringComparison.Ordinal);
        Assert.Contains("[NoBlazorRuntime]", source, StringComparison.Ordinal);
        Assert.DoesNotContain("InteractiveServer", source, StringComparison.Ordinal);
        Assert.DoesNotContain("@rendermode", source, StringComparison.Ordinal);
    }

    [Fact]
    public void The_steps_link_to_the_public_routes_and_never_into_the_candidate_area()
    {
        var hrefs = Render().FindAll("a.pp-how__step-link")
            .Select(a => a.GetAttribute("href"))
            .ToList();

        Assert.Contains(PublicRoutes.Banenkaart, hrefs);
        Assert.Contains(PublicRoutes.Test, hrefs);
        Assert.Contains(PublicRoutes.CreateAccount, hrefs);
        Assert.DoesNotContain(hrefs, href => href?.StartsWith("/candidate/", StringComparison.Ordinal) == true);
    }

    [Fact]
    public void The_price_answer_uses_the_public_price_endpoint_and_is_never_typed_in_the_markup()
    {
        var answer = Render().FindAll("details.pp-how__fq p").ToList()[0].TextContent;

        Assert.Contains("€ 2,99", answer, StringComparison.Ordinal);
        Assert.DoesNotContain("2,99", PageSource, StringComparison.Ordinal);
    }

    [Fact]
    public void The_age_answer_comes_from_the_shared_age_sentence()
    {
        var faq = Render().FindAll("details.pp-how__fq").ToList()[2].TextContent;

        Assert.Contains(
            CandidateConsentRules.ParentalConsentAge.ToString(System.Globalization.CultureInfo.InvariantCulture),
            faq,
            StringComparison.Ordinal);
        Assert.DoesNotContain("16 jaar", PageSource, StringComparison.Ordinal);
    }

    [Fact]
    public void Markup_has_no_inline_styles_and_no_raw_string_keys()
    {
        var markup = Render().Markup;

        Assert.DoesNotContain("style=", markup, StringComparison.Ordinal);
        Assert.DoesNotContain("HowLobsy.", markup, StringComparison.Ordinal);
    }
}

/// <summary>The audience pills are server-rendered links with <c>aria-current</c> (08.2).</summary>
public class HowLobsyAudienceTabTests : HowLobsyRenderTestBase
{
    [Fact]
    public void Three_pills_are_plain_links_when_employers_are_active()
    {
        var links = Render().FindAll("nav.pp-how__tabs a").ToList();

        Assert.Equal(3, links.Count);
        Assert.Equal(HowLobsyRoleGuides.SharedPath + "?voor=jou", links[0].GetAttribute("href"));
        Assert.Equal(HowLobsyRoleGuides.SharedPath + "?voor=werkgevers", links[1].GetAttribute("href"));
        Assert.Equal(HowLobsyRoleGuides.SharedPath + "?voor=scholen", links[2].GetAttribute("href"));
        Assert.All(links, a => Assert.Null(a.GetAttribute("onclick")));
    }

    [Theory]
    [InlineData("", "?voor=jou")]
    [InlineData("?voor=jou", "?voor=jou")]
    [InlineData("?voor=werkgevers", "?voor=werkgevers")]
    [InlineData("?voor=scholen", "?voor=scholen")]
    [InlineData("?voor=onzin", "?voor=jou")]
    public void Only_the_selected_pill_is_aria_current_page(string query, string expectedSuffix)
    {
        UseQuery(query);
        var current = Render().FindAll("nav.pp-how__tabs a[aria-current='page']").ToList();

        Assert.Single(current);
        Assert.Equal(HowLobsyRoleGuides.SharedPath + expectedSuffix, current[0].GetAttribute("href"));
        Assert.Contains("pp-how__tab--active", current[0].GetAttribute("class"));
    }

    [Fact]
    public void Voor_werkgevers_renders_the_employer_story_instead_of_the_steps()
    {
        UseQuery("?voor=werkgevers");
        var cut = Render();

        Assert.Equal(
            UiStrings.Get("HowLobsy.Employers.Title"),
            cut.Find(".pp-how__guide-title").TextContent.Trim());
        Assert.Empty(cut.FindAll("li.pp-how__step"));
        Assert.Contains(
            cut.FindAll(".pp-how__actions a"),
            a => a.GetAttribute("href") == PublicRoutes.Employers);
    }

    [Fact]
    public void Voor_scholen_renders_the_school_story()
    {
        UseQuery("?voor=scholen");
        var cut = Render();

        Assert.Equal(
            UiStrings.Get("HowLobsy.Schools.Title"),
            cut.Find(".pp-how__guide-title").TextContent.Trim());
        Assert.Contains(
            cut.FindAll(".pp-how__actions a"),
            a => a.GetAttribute("href") == PublicRoutes.Schools);
    }

    [Fact]
    public void Employers_off_drops_the_employer_pill_the_map_link_and_the_employer_story()
    {
        EmployersOff();
        UseQuery("?voor=werkgevers");
        var cut = Render();

        var hrefs = cut.FindAll("nav.pp-how__tabs a").Select(a => a.GetAttribute("href")).ToList();
        Assert.Equal(2, hrefs.Count);
        Assert.DoesNotContain(HowLobsyRoleGuides.SharedPath + "?voor=werkgevers", hrefs);

        // An employer deep link falls back to "Voor jou" with the -zw first step.
        Assert.Equal(4, cut.FindAll("li.pp-how__step").Count);
        Assert.DoesNotContain(
            cut.FindAll("a").Select(a => a.GetAttribute("href")),
            href => href == PublicRoutes.Banenkaart);
        Assert.Contains(
            UiStrings.Get("HowLobsy.Step1.Title.Zw"),
            cut.Markup,
            StringComparison.Ordinal);
    }

    [Fact]
    public void Employers_on_keeps_the_banenkaart_first_step()
    {
        var cut = Render();

        Assert.Contains(UiStrings.Get("HowLobsy.Step1.Title"), cut.Markup, StringComparison.Ordinal);
        Assert.Contains(
            cut.FindAll("a").Select(a => a.GetAttribute("href")),
            href => href == PublicRoutes.Banenkaart);
    }
}

/// <summary>Signed-in visitors are never redirected; staff keep their role guide (08.2).</summary>
public class HowLobsySignedInTests : HowLobsyRenderTestBase
{
    [Fact]
    public void A_candidate_stays_on_the_page_and_gets_a_button_to_their_start()
    {
        SignInAs(JobsyRoles.Candidate);
        var cut = Render();

        var start = cut.Find(".pp-how__start a");
        Assert.Equal(UiStrings.Get("HowLobsy.You.StartCta"), start.TextContent.Trim());
        Assert.False(string.IsNullOrWhiteSpace(start.GetAttribute("href")));
        Assert.Equal(4, cut.FindAll("li.pp-how__step").Count);
    }

    [Fact]
    public void An_admin_stays_on_the_page_without_a_role_guide()
    {
        SignInAs(JobsyRoles.Admin);
        var cut = Render();

        Assert.Equal(4, cut.FindAll("li.pp-how__step").Count);
        Assert.Empty(cut.FindAll(".pp-how__guide-title"));
        Assert.Empty(cut.FindAll(".pp-how__start"));
    }

    [Fact]
    public void A_sales_manager_sees_the_sales_guide_with_the_tracking_code()
    {
        SignInAs(JobsyRoles.SalesManager);
        var cut = Render();

        Assert.Equal(
            UiStrings.Get(HowLobsyRoleGuides.BuildSalesGuide("SM-TEST-1").TitleKey),
            cut.Find(".pp-how__guide-title").TextContent.Trim());
        Assert.Contains("SM-TEST-1", cut.Markup, StringComparison.Ordinal);
        Assert.Empty(cut.FindAll("li.pp-how__step"));
    }

    [Theory]
    [InlineData(JobsyRoles.BranchManager)]
    [InlineData(JobsyRoles.RegionalManager)]
    [InlineData(JobsyRoles.EnterpriseManager)]
    [InlineData(JobsyRoles.Intermediary)]
    [InlineData(JobsyRoles.Ambassadeur)]
    public void Every_staff_role_keeps_a_guide_and_the_role_is_named_in_the_pill(string role)
    {
        SignInAs(role);
        var cut = Render();

        Assert.NotNull(cut.Find(".pp-how__guide-title"));
        var pill = cut.Find("nav.pp-how__tabs a[aria-current='page']").TextContent;
        Assert.Contains(UiStrings.Get("Role." + role), pill, StringComparison.Ordinal);
    }

    [Fact]
    public void The_page_never_calls_the_navigation_manager()
        => Assert.DoesNotContain("NavigateTo", PageSource, StringComparison.Ordinal);
}

/// <summary>The new public copy is translated for real, not copied from Dutch (08.2).</summary>
public class HowLobsyStringsTests
{
    private static readonly string[] PublicKeys =
    [
        "HowLobsy.Seo.Title",
        "HowLobsy.Seo.Description",
        "HowLobsy.Eyebrow",
        "HowLobsy.Title",
        "HowLobsy.Lead",
        "HowLobsy.Tab.You",
        "HowLobsy.Tab.Employers",
        "HowLobsy.Tab.Schools",
        "HowLobsy.Step1.Title",
        "HowLobsy.Step2.Title",
        "HowLobsy.Step3.Title",
        "HowLobsy.Step4.Title",
        "HowLobsy.Promise.Title",
        "HowLobsy.Faq.Title",
        "HowLobsy.Faq.PriceQ",
        "HowLobsy.Faq.CvQ",
        "HowLobsy.Faq.AgeQ",
        "HowLobsy.Employers.Title",
        "HowLobsy.Schools.Title"
    ];

    [Theory]
    [InlineData("en")]
    [InlineData("pl")]
    [InlineData("ro")]
    [InlineData("ar")]
    public void Public_keys_are_translated_and_differ_from_dutch(string language)
    {
        foreach (var key in PublicKeys)
        {
            var nl = UiStrings.Get(key, "nl");
            var translated = UiStrings.Get(key, language);

            Assert.False(string.IsNullOrWhiteSpace(translated), $"{key} missing for {language}.");
            Assert.NotEqual(key, translated);
            Assert.NotEqual(nl, translated);
        }
    }

    [Fact]
    public void The_old_guest_guide_keys_are_gone()
    {
        var root = HowLobsyRenderTestBase.RepoRoot();
        foreach (var file in Directory.EnumerateFiles(
            Path.Combine(root, "Jobsy.Web"), "*.cs", SearchOption.AllDirectories))
        {
            Assert.DoesNotContain("HowLobsy.Guest.", File.ReadAllText(file), StringComparison.Ordinal);
        }
    }

    [Theory]
    [InlineData("nl")]
    [InlineData("en")]
    [InlineData("pl")]
    [InlineData("ro")]
    [InlineData("ar")]
    public void The_zw_variant_has_its_own_first_step_and_lead(string language)
    {
        Assert.NotEqual(
            UiStrings.Get("HowLobsy.Step1.Title", language),
            UiStrings.Get("HowLobsy.Step1.Title.Zw", language));
        Assert.NotEqual("HowLobsy.Lead.Zw", UiStrings.Get("HowLobsy.Lead.Zw", language));
    }
}

/// <summary>The page is crawlable: in the sitemap for both variants, canonical without query (08.2).</summary>
public class HowLobsySitemapTests
{
    [Fact]
    public void The_route_is_a_static_indexable_path()
        => Assert.Contains(HowLobsyRoleGuides.SharedPath, PageSeoCatalog.StaticIndexablePaths);

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Both_variants_keep_the_route_in_the_sitemap(bool employersEnabled)
    {
        var paths = PageSeoCatalog.StaticIndexablePathsFor(
            new FeatureFlagSnapshot(employersEnabled, CandidatePassportEnabled: false));

        Assert.Contains(HowLobsyRoleGuides.SharedPath, paths);
    }

    [Fact]
    public void The_route_has_its_own_title_and_description()
    {
        var entry = PageSeoCatalog.Resolve(HowLobsyRoleGuides.SharedPath);

        Assert.Equal("HowLobsy.Seo.Title", entry.TitleKey);
        Assert.Equal("HowLobsy.Seo.Description", entry.DescriptionKey);
        Assert.True(entry.Indexable);
    }
}

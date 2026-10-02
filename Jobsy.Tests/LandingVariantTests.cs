using Bunit;
using Jobsy.Web.Auth;
using Jobsy.Web.Components.Landing;
using Jobsy.Web.Components.Layout;
using Jobsy.Web.Components.Pages;
using Jobsy.Web.Components.Public;
using Jobsy.Web.Features;
using Jobsy.Web.Localization;
using Jobsy.Web.Navigation;
using Jobsy.Web.Seo;
using Jobsy.Web.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.JSInterop;
using System.Security.Claims;

namespace Jobsy.Tests;

public class LandingVariantTests : TestContext
{
    public LandingVariantTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddSingleton<AuthenticationStateProvider>(new FakeAuth());
        Services.AddSingleton<IConfiguration>(new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["Landing:ForceVariant"] = "zw" })
            .Build());
        Services.AddSingleton<ICookieConsentTokenService, CookieConsentTokenService>();
        Services.AddSingleton<IEmployersSwitch>(new FixedEmployersSwitch(false));
        Services.AddSingleton<IHostEnvironment>(new FakeEnv(Environments.Development));
        Services.AddSingleton<IHttpContextAccessor>(new HttpContextAccessor
        {
            HttpContext = new DefaultHttpContext()
        });
        Services.AddMemoryCache();
        Services.AddSingleton<IHttpClientFactory>(new StubHttpClientFactory());
        Services.AddSingleton<IExternalAuthCredentialSource, StubExternalAuth>();
        Services.AddSingleton<LandingStatsClient>();
        Services.AddSingleton<LandingPriceClient>();
        Services.AddSingleton<LegalIdentityProvider>();
        // PublicLayout shows the maintenance admin banner (errors 05).
        Services.AddSingleton(new Jobsy.Web.Hosting.MaintenanceState());
        Services.AddScoped<LandingVariantResolver>();
        Services.AddScoped(sp => new CultureState(
            sp.GetRequiredService<IJSRuntime>(),
            sp,
            sp.GetRequiredService<AuthenticationStateProvider>()));
        Services.AddScoped<PageSeoContext>();
        Services.AddSingleton<NavigationManager>(new FakeNavigation("/"));
    }

    [Fact]
    public void Zw_variant_renders_off_sections_without_employer_links()
    {
        var cut = RenderComponent<PublicLayout>(p => p
            .Add(c => c.Body, (RenderFragment)(b =>
            {
                b.OpenComponent<Landing>(0);
                b.CloseComponent();
            })));

        var landing = cut.Find(".pub-landing");
        Assert.Equal("zw", landing.GetAttribute("data-variant"));
        foreach (var id in new[] { "top", "wat-is-lobsy", "kreeft", "wat-je-krijgt", "ontdekkingsreis", "voor-wie", "privacy", "faq" })
        {
            Assert.NotNull(cut.Find($"#{id}"));
        }

        var html = landing.OuterHtml;
        Assert.Contains("Past dit beroep", html, StringComparison.Ordinal);
        Assert.Contains("dit past bij mij", html, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Alleen jij", html, StringComparison.Ordinal);
        Assert.DoesNotContain("/banenkaart", html, StringComparison.Ordinal);
        Assert.DoesNotContain("/banen", html, StringComparison.Ordinal);
        Assert.DoesNotContain("/vacancies", html, StringComparison.Ordinal);
        Assert.DoesNotContain("/werkgevers", html, StringComparison.Ordinal);
        Assert.DoesNotContain("/register", html, StringComparison.Ordinal);
        Assert.DoesNotContain("/partner", html, StringComparison.Ordinal);
        Assert.DoesNotContain("/westland", html, StringComparison.Ordinal);
        Assert.DoesNotContain("/candidate/match", html, StringComparison.Ordinal);
        Assert.DoesNotContain("vacature", html, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("werkgever", html, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("data-kpi=\"LandingCtaMap\"", html, StringComparison.Ordinal);
        Assert.Contains("Functiefit", html, StringComparison.Ordinal);
        Assert.Contains("Droombaan-check", html, StringComparison.Ordinal);
    }

    [Fact]
    public void Zw_faq_and_seo_keys_differ_from_on()
    {
        Assert.NotEqual(
            LandingText.For("Landing.Seo.Title", LandingVariant.On, "nl"),
            LandingText.For("Landing.Seo.Title", LandingVariant.Zw, "nl"));
        Assert.Contains("richting", LandingText.For("Landing.Seo.Title", LandingVariant.Zw, "nl"), StringComparison.OrdinalIgnoreCase);

        var off = LandingFaq.BuildPairs(LandingVariant.Zw, "nl", 2.99m);
        var on = LandingFaq.BuildPairs(LandingVariant.On, "nl", 2.99m);
        Assert.Equal(7, off.Count);
        Assert.DoesNotContain("banenkaart", off[0].Answer, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("school", off[4].Question, StringComparison.OrdinalIgnoreCase);
        Assert.NotEqual(on[0].Answer, off[0].Answer);
    }

    [Fact]
    public void Zw_passport_tab_fit_label()
    {
        Assert.Equal("Past dit beroep?", LandingText.For("Landing.Get.Passport.TabFit", LandingVariant.Zw, "nl"));
        Assert.Equal("Past dit beroep?", LandingText.For("GratisDna.Locked.TabFit", LandingVariant.Zw, "nl"));
        Assert.Equal("Past deze baan?", LandingText.For("Landing.Get.Passport.TabFit", LandingVariant.On, "nl"));
    }

    [Fact]
    public void Zw_signup_unlock_list_and_no_jobs_teaser()
    {
        Assert.Equal(
            "“Past dit beroep bij mij?” voor elk beroep",
            LandingText.For("GratisDna.Signup.Unlock.Fit", LandingVariant.Zw, "nl"));
        Assert.Equal(
            "Je droombaan-check en je volgende stap",
            LandingText.For("GratisDna.Signup.Unlock.Dream", LandingVariant.Zw, "nl"));
        Assert.Equal(
            "De Ontdekkingsreis, stap voor stap",
            LandingText.For("GratisDna.Signup.Unlock.Journey", LandingVariant.Zw, "nl"));

        var result = RenderComponent<GratisDnaResultView>(p => p
            .Add(c => c.Variant, LandingVariant.Zw)
            .Add(c => c.ShowSticky, false)
            .Add(c => c.StrengthSentence, "x"));
        Assert.Empty(result.FindAll("[data-testid='gd-jobs-teaser']"));
        Assert.Contains("Past dit beroep?", result.Markup, StringComparison.Ordinal);
    }

    [Fact]
    public void Sitemap_paths_drop_employer_urls_when_off()
    {
        var on = PageSeoCatalog.StaticIndexablePathsFor(true);
        var off = PageSeoCatalog.StaticIndexablePathsFor(false);
        Assert.Contains("/banenkaart", on);
        Assert.Contains("/partner", on);
        Assert.DoesNotContain("/banenkaart", off);
        Assert.DoesNotContain("/partner", off);
        Assert.DoesNotContain("/westland", off);
        Assert.Contains("/", off);
        Assert.Contains("/ontdek", off);

        var xmlOn = SitemapXml.Build("https://lobsy.nl", on);
        var xmlOff = SitemapXml.Build("https://lobsy.nl", off);
        Assert.Contains("/banenkaart", xmlOn, StringComparison.Ordinal);
        Assert.DoesNotContain("/banenkaart", xmlOff, StringComparison.Ordinal);
        Assert.NotEqual(SitemapXml.WeakETag(xmlOn, true), SitemapXml.WeakETag(xmlOff, false));
    }

    [Fact]
    public void Website_json_ld_omits_search_action_when_off()
    {
        var on = StructuredData.WebsiteAndOrganization("https://lobsy.nl", includeSearchAction: true);
        var off = StructuredData.WebsiteAndOrganization("https://lobsy.nl", includeSearchAction: false);
        Assert.Contains("SearchAction", on, StringComparison.Ordinal);
        Assert.Contains("/banenkaart", on, StringComparison.Ordinal);
        Assert.DoesNotContain("SearchAction", off, StringComparison.Ordinal);
        Assert.DoesNotContain("/banenkaart", off, StringComparison.Ordinal);
    }

    private sealed class FakeAuth : AuthenticationStateProvider
    {
        public override Task<AuthenticationState> GetAuthenticationStateAsync()
            => Task.FromResult(new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity())));
    }

    private sealed class FakeEnv(string name) : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = name;
        public string ApplicationName { get; set; } = "Jobsy.Tests";
        public string ContentRootPath { get; set; } = AppContext.BaseDirectory;
        public Microsoft.Extensions.FileProviders.IFileProvider ContentRootFileProvider { get; set; }
            = new Microsoft.Extensions.FileProviders.NullFileProvider();
    }

    private sealed class FakeNavigation : NavigationManager
    {
        public FakeNavigation(string uri) => Initialize("http://localhost/", "http://localhost" + uri);
        protected override void NavigateToCore(string uri, bool forceLoad) { }
    }

    private sealed class StubHttpClientFactory : IHttpClientFactory
    {
        public HttpClient CreateClient(string name)
            => new(new StubHandler()) { BaseAddress = new Uri("http://localhost:5200/") };
    }

    private sealed class StubExternalAuth : IExternalAuthCredentialSource
    {
        public Task<bool> IsGoogleConfiguredAsync(CancellationToken cancellationToken = default) => Task.FromResult(false);
        public Task<bool> IsEntraConfiguredAsync(CancellationToken cancellationToken = default) => Task.FromResult(false);
        public Task<ExternalOAuthCredentials?> GetGoogleAsync(CancellationToken cancellationToken = default) => Task.FromResult<ExternalOAuthCredentials?>(null);
        public Task<ExternalOAuthCredentials?> GetEntraAsync(CancellationToken cancellationToken = default) => Task.FromResult<ExternalOAuthCredentials?>(null);
    }

    private sealed class StubHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            => Task.FromResult(new HttpResponseMessage(System.Net.HttpStatusCode.RequestTimeout));
    }
}

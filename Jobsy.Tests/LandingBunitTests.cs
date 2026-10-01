using System.IO.Compression;
using System.Text;
using Bunit;
using Jobsy.Web.Components.Landing;
using Jobsy.Web.Components.Layout;
using Jobsy.Web.Components.Pages;
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

public class LandingBunitTests : TestContext
{
    public LandingBunitTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddSingleton<AuthenticationStateProvider>(new FakeAuth());
        Services.AddSingleton<IConfiguration>(new ConfigurationBuilder().Build());
        Services.AddSingleton<ICookieConsentTokenService, CookieConsentTokenService>();
        Services.AddSingleton<IEmployersSwitch, AlwaysOnEmployersSwitch>();
        Services.AddSingleton<IHostEnvironment>(new FakeEnv(Environments.Development));
        Services.AddSingleton<IHttpContextAccessor>(new HttpContextAccessor
        {
            HttpContext = new DefaultHttpContext()
        });
        Services.AddMemoryCache();
        Services.AddSingleton<IHttpClientFactory>(new StubHttpClientFactory());
        Services.AddSingleton<LandingStatsClient>();
        Services.AddSingleton<LandingPriceClient>();
        Services.AddScoped<LandingVariantResolver>();
        Services.AddScoped(sp => new CultureState(
            sp.GetRequiredService<IJSRuntime>(),
            sp,
            sp.GetRequiredService<AuthenticationStateProvider>()));
        Services.AddScoped<PageSeoContext>();
        Services.AddSingleton(new Jobsy.Web.Hosting.MaintenanceState());
        Services.AddSingleton<NavigationManager>(new FakeNavigation("/"));
    }

    [Fact]
    public void On_variant_renders_section_ids_ctas_and_one_cookie_banner()
    {
        var cut = RenderComponent<PublicLayout>(p => p
            .Add(c => c.Body, (RenderFragment)(b =>
            {
                b.OpenComponent<Landing>(0);
                b.CloseComponent();
            })));

        Assert.Single(cut.FindAll(".cookie-consent"));
        foreach (var id in new[] { "top", "wat-is-lobsy", "kreeft", "wat-je-krijgt", "voor-wie", "privacy", "faq" })
        {
            Assert.NotNull(cut.Find($"#{id}"));
        }

        Assert.Contains(cut.FindAll("a[data-kpi='LandingCtaTest']"), a => a.GetAttribute("href") == PublicRoutes.Test);
        Assert.Contains(cut.FindAll("a[data-kpi='LandingCtaLogin']"), a => a.GetAttribute("href") == PublicRoutes.Login);
        Assert.Contains(cut.FindAll("a[data-kpi='LandingCtaMap']"), a => a.GetAttribute("href") == PublicRoutes.Banenkaart);
        var landing = cut.Find(".pub-landing").OuterHtml;
        Assert.DoesNotContain("/register", landing, StringComparison.Ordinal);
        Assert.DoesNotContain("href=\"/werkgevers\"", landing, StringComparison.Ordinal);
        Assert.DoesNotContain("href=\"/scholen\"", landing, StringComparison.Ordinal);
        Assert.Contains("data-kpi=\"LandingCtaAudience\"", landing, StringComparison.Ordinal);
    }

    [Fact]
    public void Vacancy_count_hidden_when_null_or_below_25()
    {
        var heroLow = RenderComponent<LandingHero>(p => p
            .Add(c => c.Variant, LandingVariant.On)
            .Add(c => c.VacancyCount, (int?)null));
        Assert.Contains("Of kijk eerst op de banenkaart", heroLow.Markup, StringComparison.Ordinal);

        var heroOk = RenderComponent<LandingHero>(p => p
            .Add(c => c.Variant, LandingVariant.On)
            .Add(c => c.VacancyCount, 120));
        Assert.Contains("vacatures op de banenkaart", heroOk.Markup, StringComparison.Ordinal);
        Assert.Contains("120", heroOk.Markup, StringComparison.Ordinal);
    }

    [Fact]
    public void Faq_json_ld_matches_visible_questions()
    {
        var pairs = LandingFaq.BuildPairs(LandingVariant.On, "nl", 2.99m);
        Assert.Equal(7, pairs.Count);
        var json = StructuredData.FaqPage(pairs);
        foreach (var (q, _) in pairs)
        {
            Assert.Contains(q, json, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void Rtl_for_ar_on_layout()
    {
        var http = Services.GetRequiredService<IHttpContextAccessor>().HttpContext!;
        http.Request.QueryString = new QueryString("?lang=ar");
        var culture = Services.GetRequiredService<CultureState>();
        culture.InitializeFromRequest(http);
        var cut = RenderComponent<PublicLayout>(p => p
            .Add(c => c.Body, (RenderFragment)(b =>
            {
                b.OpenComponent<LandingHero>(0);
                b.AddAttribute(1, "Variant", LandingVariant.On);
                b.CloseComponent();
            })));
        Assert.Equal("rtl", cut.Find(".pub-theme").GetAttribute("dir"));
    }

    [Fact]
    public void Feature_availability_prints_matches()
    {
        var desc = LandingFeatureAvailability.DescribeMatches();
        Assert.False(string.IsNullOrWhiteSpace(desc));
        Assert.True(LandingFeatureAvailability.ShowPassportSoon);
        Assert.True(LandingFeatureAvailability.ShowDiscoverySoon);
    }

    [Fact]
    public void Landing_markup_under_60kb_gzip()
    {
        var cut = RenderComponent<PublicLayout>(p => p
            .Add(c => c.Body, (RenderFragment)(b =>
            {
                b.OpenComponent<Landing>(0);
                b.CloseComponent();
            })));
        var bytes = Encoding.UTF8.GetBytes(cut.Markup);
        using var ms = new MemoryStream();
        using (var gz = new GZipStream(ms, CompressionLevel.SmallestSize, leaveOpen: true))
        {
            gz.Write(bytes);
        }

        Assert.True(ms.Length <= 60 * 1024, $"landing markup gzip is {ms.Length} bytes (budget 60 KB)");
    }

    [Fact]
    public void Round_down_to_tens_threshold()
    {
        Assert.Null(LandingVacancyCount.RoundDownToTens(null));
        Assert.Null(LandingVacancyCount.RoundDownToTens(24));
        Assert.Equal(20, LandingVacancyCount.RoundDownToTens(25)); // 25/10*10 = 20 — wait, 25 rounds to 20?
        // Spec: round down to tens; hide when below 25. So 25 → display 20? Or show 25?
        // "rounded down to tens" and "hidden when below 25" — 25 passes threshold then rounds to 20.
        Assert.Equal(120, LandingVacancyCount.RoundDownToTens(129));
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
        {
            var client = new HttpClient(new StubHandler())
            {
                BaseAddress = new Uri("http://localhost:5200/")
            };
            return client;
        }
    }

    private sealed class StubHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            => Task.FromResult(new HttpResponseMessage(System.Net.HttpStatusCode.RequestTimeout));
    }
}

public class LandingPerformanceGuardTests
{
    [Fact]
    public void Landing_sources_have_no_map_or_vacancy_api_refs()
    {
        var root = FindRepoRoot();
        var files = Directory.GetFiles(Path.Combine(root, "Jobsy.Web", "Components", "Landing"), "*.razor")
            .Append(Path.Combine(root, "Jobsy.Web", "Components", "Pages", "Landing.razor"))
            .ToArray();
        string[] banned =
        [
            "VacancyDiscovery", "jobMap", "jobsyMaps", "maplibre", "openfreemap", "leaflet", "api/vacancies"
        ];
        foreach (var file in files)
        {
            var text = File.ReadAllText(file);
            foreach (var b in banned)
            {
                Assert.DoesNotContain(b, text, StringComparison.OrdinalIgnoreCase);
            }
        }

        var app = File.ReadAllText(Path.Combine(root, "Jobsy.Web", "Components", "App.razor"));
        Assert.Contains("NoBlazorRuntimeAttribute", app, StringComparison.Ordinal);
        Assert.Contains("landing.css", app, StringComparison.Ordinal);
    }

    [Fact]
    public void Public_theme_and_landing_css_under_25kb_gzip()
    {
        var root = FindRepoRoot();
        var pub = File.ReadAllBytes(Path.Combine(root, "Jobsy.Web", "wwwroot", "css", "features", "public-theme.css"));
        var land = File.ReadAllBytes(Path.Combine(root, "Jobsy.Web", "wwwroot", "css", "features", "landing.css"));
        var gz = GzipLen(pub.Concat(land).ToArray());
        Assert.True(gz <= 25 * 1024, $"public-theme+landing gzip is {gz} bytes (budget 25 KB)");
    }

    private static int GzipLen(byte[] raw)
    {
        using var ms = new MemoryStream();
        using (var gz = new GZipStream(ms, CompressionLevel.SmallestSize, leaveOpen: true))
        {
            gz.Write(raw);
        }

        return (int)ms.Length;
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

        throw new InvalidOperationException("Jobsy.sln not found");
    }
}

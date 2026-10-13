using System.Net;
using System.Text;
using Bunit;
using Jobsy.Web.Components.Pages;
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
/// <c>/{kvk}</c> in the public layout (public-pages 09.3): breadcrumb, header card with the city
/// and the KvK chip, vestiging tabs only for branches with a public vacancy, and a real 404 with the
/// shared status page for every not-found case.
/// </summary>
public class CompanyPageLayoutTests : BunitContext
{
    private const string Kvk = "12345678";

    private string _companyJson =
        """
        {
          "kvk": "12345678",
          "kvkNumber": "12345678",
          "name": "Bakkerij De Gouden Korrel",
          "city": "Delft",
          "branches": [
            { "name": "Delft, Markt", "city": "Delft", "vestigingsnummer": "111", "path": "/12345678/111", "vacancyCount": 3 },
            { "name": "Den Haag", "city": "Den Haag", "vestigingsnummer": "222", "path": "/12345678/222", "vacancyCount": 1 },
            { "name": "Rotterdam (gesloten)", "city": "Rotterdam", "vestigingsnummer": "333", "path": "/12345678/333", "vacancyCount": 0 }
          ]
        }
        """;

    private string _vacanciesJson =
        """
        [
          { "id": "11111111-1111-1111-1111-111111111111", "title": "Medewerker bakkerij", "companyName": "Bakkerij De Gouden Korrel",
            "companyId": "22222222-2222-2222-2222-222222222222", "companyAddress": "Delft", "kvkNumber": "12345678",
            "vestigingsnummer": "111", "workTypes": ["SideJob"], "categoryName": "Bakkerij", "minHoursPerWeek": 12,
            "maxHoursPerWeek": 20, "latitude": 52.01, "longitude": 4.36, "kind": "Regular" },
          { "id": "33333333-3333-3333-3333-333333333333", "title": "Bezorger (e-bike)", "companyName": "Bakkerij De Gouden Korrel",
            "companyId": "22222222-2222-2222-2222-222222222222", "companyAddress": "Delft", "kvkNumber": "12345678",
            "vestigingsnummer": "111", "workTypes": ["SideJob"], "categoryName": "Bakkerij", "latitude": 52.02,
            "longitude": 4.37, "kind": "Regular" }
        ]
        """;

    private readonly DefaultHttpContext _http = new();
    private readonly AmbientCultureScope _culture = new();
    private HttpStatusCode _companyStatus = HttpStatusCode.OK;

    public CompanyPageLayoutTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddLogging();
        Services.AddSingleton<IConfiguration>(Configuration("https://lobsy.nl"));
        Services.AddSingleton<IHttpContextAccessor>(new HttpContextAccessor { HttpContext = _http });
        Services.AddSingleton(new JobsyApiClient(new HttpClient(new CompanyHandler(
            () => _companyStatus,
            () => _companyJson,
            () => _vacanciesJson))
        {
            BaseAddress = new Uri("http://localhost/")
        }));
        Services.AddSingleton<AuthenticationStateProvider>(new AnonymousAuth());
        Services.AddScoped(sp => new CultureState(
            sp.GetRequiredService<IJSRuntime>(),
            sp,
            sp.GetRequiredService<AuthenticationStateProvider>()));
        Services.AddScoped<PageSeoContext>();
        Services.AddSingleton<NavigationManager>(new CompanyStaticNavigation($"/{Kvk}"));
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing)
        {
            _culture.Dispose();
        }
    }

    internal static IConfiguration Configuration(string? publicWebBaseUrl) =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["PublicWebBaseUrl"] = publicWebBaseUrl
            })
            .Build();

    private IRenderedComponent<CompanyPublicPage> Render(string? vestiging = null)
        => Render<CompanyPublicPage>(p =>
        {
            p.Add(x => x.KvkNumber, Kvk);
            p.Add(x => x.Vestigingsnummer, vestiging);
        });

    private static string PageSource => File.ReadAllText(Path.Combine(
        HowLobsyRenderTestBase.RepoRoot(),
        "Jobsy.Web", "Components", "Pages", "CompanyPublicPage.razor"));

    [Fact]
    public void The_page_renders_in_the_public_layout_and_not_in_the_map_chrome()
    {
        var source = PageSource;

        Assert.Contains("@layout PublicLayout", source, StringComparison.Ordinal);
        Assert.DoesNotContain("jobsy-chrome", source, StringComparison.Ordinal);
        Assert.DoesNotContain("<LanguageSelector", source, StringComparison.Ordinal);
        Assert.DoesNotContain("<AuthHeader", source, StringComparison.Ordinal);
        Assert.DoesNotContain("jobsy-logo", source, StringComparison.Ordinal);
    }

    [Fact]
    public void The_header_card_shows_the_city_the_vacancy_count_and_the_kvk_chip()
    {
        var cut = Render();
        var header = cut.Find(".pp-company__header");

        Assert.Equal("Bakkerij De Gouden Korrel", cut.Find("h1.pp-doc__title").TextContent.Trim());
        Assert.Contains("Delft", header.TextContent, StringComparison.Ordinal);
        Assert.Contains("2 vacatures", header.TextContent, StringComparison.Ordinal);
        Assert.Equal(
            UiStrings.Get("CompanyPage.KvkVerified"),
            header.QuerySelector(".pp-company__kvk")!.TextContent.Trim());
    }

    [Fact]
    public void A_company_without_an_uploaded_logo_gets_an_emoji_tile_from_its_category()
    {
        var header = Render().Find(".pp-company__header");

        Assert.Null(header.QuerySelector("img.pp-company__logo"));
        Assert.Equal("🥐", header.QuerySelector(".pp-company__tile")!.TextContent.Trim());
    }

    [Fact]
    public void No_street_address_and_no_internal_ids_reach_the_page()
    {
        var markup = Render().Markup;

        Assert.DoesNotContain("Markt 1", markup, StringComparison.Ordinal);
        Assert.DoesNotContain("22222222-2222", markup, StringComparison.Ordinal);
        Assert.DoesNotContain("52.01", markup, StringComparison.Ordinal);
    }

    [Fact]
    public void The_breadcrumb_links_back_to_the_job_map()
    {
        var crumbs = Render().Find(".pp-company__crumbs");

        Assert.Equal(PublicRoutes.Banenkaart, crumbs.QuerySelector("a")!.GetAttribute("href"));
        Assert.Equal(UiStrings.Get("CompanyPage.Breadcrumb.Map"), crumbs.QuerySelector("a")!.TextContent.Trim());
        Assert.Contains("Bakkerij De Gouden Korrel", crumbs.TextContent, StringComparison.Ordinal);
    }

    [Fact]
    public void Vestiging_tabs_exist_only_for_branches_with_a_public_vacancy()
    {
        var tabs = Render().FindAll(".pp-company__tab").ToList();

        Assert.Equal(3, tabs.Count);
        Assert.Equal(UiStrings.Get("CompanyPage.Branches.All").Replace("{0}", "2"), tabs[0].TextContent.Trim());
        Assert.Equal("page", tabs[0].GetAttribute("aria-current"));
        Assert.Equal("/12345678/111", tabs[1].GetAttribute("href"));
        Assert.Equal("/12345678/222", tabs[2].GetAttribute("href"));
        Assert.DoesNotContain(tabs, t => t.TextContent.Contains("gesloten", StringComparison.Ordinal));
    }

    [Fact]
    public void On_a_vestiging_page_that_tab_is_the_current_one()
    {
        var tabs = Render("111").FindAll(".pp-company__tab").ToList();

        Assert.Null(tabs[0].GetAttribute("aria-current"));
        Assert.Equal("page", tabs[1].GetAttribute("aria-current"));
    }

    [Fact]
    public void The_vacancy_cards_and_the_map_pane_are_both_on_the_page()
    {
        var cut = Render();

        Assert.Equal(2, cut.FindAll(".pp-company__list article.job-card").Count);
        Assert.NotNull(cut.Find(".pp-company__map"));
    }

    [Fact]
    public void A_report_link_points_at_the_melden_form_for_this_company()
    {
        var report = Render().Find(".pp-company__report");

        Assert.Contains(UiStrings.Get("Report.Company.Prompt"), report.TextContent, StringComparison.Ordinal);
        Assert.Equal(
            $"/melden?type=company&id={Kvk}",
            report.QuerySelector("a")!.GetAttribute("href"));
        Assert.Equal(UiStrings.Get("Report.Company.PromptCta"), report.QuerySelector("a")!.TextContent.Trim());
    }

    [Fact]
    public void An_unknown_kvk_number_is_a_real_404_with_the_shared_status_page_and_no_map()
    {
        _companyStatus = HttpStatusCode.NotFound;
        var cut = Render();

        Assert.Equal(StatusCodes.Status404NotFound, _http.Response.StatusCode);
        Assert.Equal(UiStrings.Get("Status.NotFound.Title"), cut.Find("h1").TextContent.Trim());
        Assert.Empty(cut.FindAll(".pp-company__map"));
        Assert.DoesNotContain("job-map", cut.Markup, StringComparison.Ordinal);
    }

    [Fact]
    public void A_company_without_a_public_vacancy_is_a_404_too()
    {
        _vacanciesJson = "[]";
        var cut = Render();

        Assert.Equal(StatusCodes.Status404NotFound, _http.Response.StatusCode);
        Assert.Equal(UiStrings.Get("Status.NotFound.Title"), cut.Find("h1").TextContent.Trim());
    }

    [Fact]
    public void A_served_page_is_indexable_with_its_own_canonical()
    {
        Render();
        var seo = Services.GetRequiredService<PageSeoContext>().Current;

        Assert.True(seo?.Indexable);
        Assert.Equal($"/{Kvk}", seo!.CanonicalPath);
    }

    [Fact]
    public void A_vestiging_page_is_canonical_to_itself()
    {
        Render("111");

        Assert.Equal($"/{Kvk}/111", Services.GetRequiredService<PageSeoContext>().Current!.CanonicalPath);
    }

    [Fact]
    public void A_not_found_page_is_never_indexed()
    {
        _companyStatus = HttpStatusCode.NotFound;
        Render();

        Assert.False(Services.GetRequiredService<PageSeoContext>().Current?.Indexable);
    }

    private sealed class AnonymousAuth : AuthenticationStateProvider
    {
        public override Task<AuthenticationState> GetAuthenticationStateAsync()
            => Task.FromResult(new AuthenticationState(
                new System.Security.Claims.ClaimsPrincipal(new System.Security.Claims.ClaimsIdentity())));
    }

    internal sealed class CompanyHandler(
        Func<HttpStatusCode> companyStatus,
        Func<string> companyJson,
        Func<string> vacanciesJson) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var path = request.RequestUri!.AbsolutePath;
            if (path.EndsWith("/vacancies", StringComparison.Ordinal))
            {
                return Task.FromResult(Json(vacanciesJson()));
            }

            if (path.Contains("/engagement", StringComparison.Ordinal))
            {
                return Task.FromResult(Json("[]"));
            }

            var status = companyStatus();
            return Task.FromResult(status == HttpStatusCode.OK
                ? Json(companyJson())
                : new HttpResponseMessage(status) { Content = new StringContent("") });
        }

        private static HttpResponseMessage Json(string body) => new(HttpStatusCode.OK)
        {
            Content = new StringContent(body, Encoding.UTF8, "application/json")
        };
    }

    internal sealed class CompanyStaticNavigation : NavigationManager
    {
        public CompanyStaticNavigation(string relativePath)
            => Initialize("http://localhost/", "http://localhost" + relativePath);

        protected override void NavigateToCore(string uri, bool forceLoad)
            => throw new InvalidOperationException($"The page must not redirect, but navigated to {uri}.");
    }
}

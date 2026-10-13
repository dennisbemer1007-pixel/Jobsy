using System.Net;
using System.Text.Json;
using Bunit;
using Jobsy.Web.Components.Pages;
using Jobsy.Web.Localization;
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
/// The company page's <c>Organization</c> markup (public-pages 09.3): the origin comes from the
/// configured public base URL, the address carries the city only, and no <c>JobPosting</c> is
/// duplicated here.
/// </summary>
public class CompanyJsonLdTests : BunitContext
{
    private const string Kvk = "12345678";

    private const string CompanyJson =
        """
        {
          "kvk": "12345678",
          "kvkNumber": "12345678",
          "name": "Bakkerij De Gouden Korrel",
          "city": "Delft",
          "logoUrl": "/media/company-logo.png",
          "branches": []
        }
        """;

    private const string VacanciesJson =
        """
        [
          { "id": "11111111-1111-1111-1111-111111111111", "title": "Medewerker bakkerij",
            "companyName": "Bakkerij De Gouden Korrel", "companyId": "22222222-2222-2222-2222-222222222222",
            "companyAddress": "Delft", "kvkNumber": "12345678", "workTypes": ["SideJob"],
            "categoryName": "Bakkerij", "latitude": 52.01, "longitude": 4.36, "kind": "Regular" }
        ]
        """;

    private readonly DefaultHttpContext _http = new();
    private readonly AmbientCultureScope _culture = new();

    public CompanyJsonLdTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddLogging();
        Services.AddSingleton<IHttpContextAccessor>(new HttpContextAccessor { HttpContext = _http });
        Services.AddSingleton(new JobsyApiClient(new HttpClient(
            new CompanyPageLayoutTests.CompanyHandler(
                () => HttpStatusCode.OK,
                () => CompanyJson,
                () => VacanciesJson))
        {
            BaseAddress = new Uri("http://localhost/")
        }));
        Services.AddSingleton<AuthenticationStateProvider>(new AnonymousAuth());
        Services.AddScoped(sp => new CultureState(
            sp.GetRequiredService<IJSRuntime>(),
            sp,
            sp.GetRequiredService<AuthenticationStateProvider>()));
        Services.AddScoped<PageSeoContext>();
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing)
        {
            _culture.Dispose();
        }
    }

    private JsonElement RenderJsonLd(string requestHost, string? configuredOrigin)
    {
        Services.AddSingleton<IConfiguration>(CompanyPageLayoutTests.Configuration(configuredOrigin));
        Services.AddSingleton<NavigationManager>(new HostNavigation(requestHost, $"/{Kvk}"));

        Render<CompanyPublicPage>(p =>
        {
            p.Add(x => x.KvkNumber, Kvk);
            p.Add(x => x.Vestigingsnummer, null);
        });

        var jsonLd = Services.GetRequiredService<PageSeoContext>().Current?.JsonLd;
        Assert.False(string.IsNullOrWhiteSpace(jsonLd));
        return JsonDocument.Parse(jsonLd!).RootElement.Clone();
    }

    [Fact]
    public void The_origin_is_the_configured_public_origin_even_when_the_request_host_differs()
    {
        var json = RenderJsonLd("https://preview.onrender.com", "https://lobsy.nl");

        Assert.Equal($"https://lobsy.nl/{Kvk}", json.GetProperty("url").GetString());
        Assert.Equal("https://lobsy.nl/media/company-logo.png", json.GetProperty("logo").GetString());
    }

    [Fact]
    public void Without_configuration_the_request_origin_is_the_fallback()
    {
        var json = RenderJsonLd("https://lobsy.nl", null);

        Assert.Equal($"https://lobsy.nl/{Kvk}", json.GetProperty("url").GetString());
    }

    [Fact]
    public void The_address_is_the_city_only_and_there_is_no_job_posting()
    {
        var json = RenderJsonLd("https://lobsy.nl", "https://lobsy.nl");
        var address = json.GetProperty("address");

        Assert.Equal("Organization", json.GetProperty("@type").GetString());
        Assert.Equal("Delft", address.GetProperty("addressLocality").GetString());
        Assert.False(address.TryGetProperty("streetAddress", out _));
        Assert.DoesNotContain("JobPosting", json.GetRawText(), StringComparison.Ordinal);
    }

    private sealed class AnonymousAuth : AuthenticationStateProvider
    {
        public override Task<AuthenticationState> GetAuthenticationStateAsync()
            => Task.FromResult(new AuthenticationState(
                new System.Security.Claims.ClaimsPrincipal(new System.Security.Claims.ClaimsIdentity())));
    }

    private sealed class HostNavigation : NavigationManager
    {
        public HostNavigation(string origin, string relativePath)
            => Initialize(origin.TrimEnd('/') + "/", origin.TrimEnd('/') + relativePath);

        protected override void NavigateToCore(string uri, bool forceLoad)
            => throw new InvalidOperationException($"The page must not redirect, but navigated to {uri}.");
    }
}

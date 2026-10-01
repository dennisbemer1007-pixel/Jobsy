using System.Net;
using System.Security.Claims;
using System.Text;
using Bunit;
using Jobsy.Web.Components.Legal;
using Jobsy.Web.Localization;
using Jobsy.Web.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;

namespace Jobsy.Tests;

/// <summary>
/// The identity card only renders rows whose value is configured: an empty <c>Legal:*</c> value
/// hides its line, never a placeholder (D1 / 02.6).
/// </summary>
public class LegalIdentityCardTests : TestContext
{
    private string _legalJson = "{}";

    public LegalIdentityCardTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddLogging();
        Services.AddMemoryCache();
        Services.AddSingleton<AuthenticationStateProvider>(new AnonymousAuth());
        Services.AddSingleton<IHttpContextAccessor>(new HttpContextAccessor { HttpContext = new DefaultHttpContext() });
        Services.AddSingleton<IHttpClientFactory>(new JsonHttpClientFactory(() => _legalJson));
        Services.AddSingleton<LegalIdentityProvider>();
        Services.AddScoped(sp => new CultureState(
            sp.GetRequiredService<IJSRuntime>(),
            sp,
            sp.GetRequiredService<AuthenticationStateProvider>()));
        Services.AddSingleton<NavigationManager>(new StaticNavigation());
    }

    [Fact]
    public void Empty_values_render_neither_a_row_nor_a_label()
    {
        _legalJson = """{"tradeName":"Lobsy","supportEmail":"support@lobsy.nl"}""";
        var cut = RenderComponent<LegalIdentityCard>();

        var labels = cut.FindAll(".pp-identity__label").Select(l => l.TextContent.Trim()).ToList();
        Assert.Contains(UiStrings.Get("Legal.IdentityCard.Name"), labels);
        Assert.DoesNotContain(UiStrings.Get("Legal.IdentityCard.Address"), labels);
        Assert.DoesNotContain(UiStrings.Get("Legal.IdentityCard.Kvk"), labels);
        Assert.DoesNotContain(UiStrings.Get("Legal.IdentityCard.Vat"), labels);

        var markup = cut.Markup;
        Assert.DoesNotContain("[", markup, StringComparison.Ordinal);
        Assert.DoesNotContain("wordt ingevuld", markup, StringComparison.Ordinal);
    }

    [Fact]
    public void Configured_values_render_one_row_each()
    {
        _legalJson = """
            {"name":"Lobsy B.V.","street":"Teststraat 1","postalCode":"1234 AB","city":"Testdorp",
             "kvkNumber":"12345678","vatNumber":"NL001234567B01","privacyEmail":"privacy@lobsy.nl",
             "supportEmail":"support@lobsy.nl"}
            """;
        var cut = RenderComponent<LegalIdentityCard>(p => p.Add(c => c.ShowPrivacyContact, true));

        var labels = cut.FindAll(".pp-identity__label").Select(l => l.TextContent.Trim()).ToList();
        Assert.Contains(UiStrings.Get("Legal.IdentityCard.Address"), labels);
        Assert.Contains(UiStrings.Get("Legal.IdentityCard.Kvk"), labels);
        Assert.Contains(UiStrings.Get("Legal.IdentityCard.Vat"), labels);
        Assert.Contains(UiStrings.Get("Legal.IdentityCard.PrivacyQuestions"), labels);
        Assert.DoesNotContain(UiStrings.Get("Legal.IdentityCard.Contact"), labels);

        Assert.Equal(5, cut.FindAll(".pp-identity__row").Count);
        Assert.Equal("mailto:privacy@lobsy.nl", cut.Find(".pp-identity a").GetAttribute("href"));
    }

    [Fact]
    public void Terms_card_shows_the_support_contact_instead_of_privacy_questions()
    {
        _legalJson = """{"name":"Lobsy B.V.","supportEmail":"support@lobsy.nl","privacyEmail":"privacy@lobsy.nl"}""";
        var cut = RenderComponent<LegalIdentityCard>();

        var labels = cut.FindAll(".pp-identity__label").Select(l => l.TextContent.Trim()).ToList();
        Assert.Contains(UiStrings.Get("Legal.IdentityCard.Contact"), labels);
        Assert.DoesNotContain(UiStrings.Get("Legal.IdentityCard.PrivacyQuestions"), labels);
    }

    private sealed class AnonymousAuth : AuthenticationStateProvider
    {
        public override Task<AuthenticationState> GetAuthenticationStateAsync()
            => Task.FromResult(new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity())));
    }

    private sealed class JsonHttpClientFactory(Func<string> json) : IHttpClientFactory
    {
        public HttpClient CreateClient(string name)
            => new(new JsonHandler(json)) { BaseAddress = new Uri("http://localhost/") };
    }

    private sealed class JsonHandler(Func<string> json) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
            => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(json(), Encoding.UTF8, "application/json")
            });
    }

    private sealed class StaticNavigation : NavigationManager
    {
        public StaticNavigation() => Initialize("http://localhost/", "http://localhost/privacy");

        protected override void NavigateToCore(string uri, bool forceLoad)
        {
        }
    }
}

using Bunit;
using Jobsy.Web.Components.Pages.Public;
using Jobsy.Web.Localization;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;
using System.Security.Claims;

namespace Jobsy.Tests;

/// <summary>
/// <c>/melden</c> is a plain POST form (public-pages 06): it must render without JS and carry an
/// antiforgery token, because a visitor reporting content may well have scripts disabled.
/// </summary>
public class ContentReportFormBunitTests : TestContext
{
    public ContentReportFormBunitTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddSingleton<AuthenticationStateProvider>(new AnonymousAuthState());
        Services.AddSingleton(sp => new CultureState(
            sp.GetRequiredService<IJSRuntime>(),
            sp,
            sp.GetRequiredService<AuthenticationStateProvider>()));
        Services.AddSingleton<IHttpContextAccessor>(new HttpContextAccessor
        {
            HttpContext = new DefaultHttpContext()
        });
        Services.AddSingleton<IAntiforgery>(new AlwaysValidAntiforgery());
        Services.AddSingleton<IHttpClientFactory>(new NoApiHttpClientFactory());
        Services.AddSingleton<NavigationManager>(new StaticNavigation("/melden?type=vacancy&id=" + Guid.NewGuid()));
    }

    [Fact]
    public void Form_posts_to_melden_with_an_antiforgery_token_and_six_reasons()
    {
        var cut = RenderComponent<Melden>();

        var form = cut.Find("form");
        Assert.Equal("post", form.GetAttribute("method"));
        Assert.Equal("/melden", form.GetAttribute("action"));
        Assert.Equal("false", form.GetAttribute("data-enhance"));

        var token = cut.Find("input[name='form']");
        Assert.Equal("req", token.GetAttribute("value"));

        Assert.Equal(6, cut.FindAll("input[name='reason']").Count);
        Assert.Contains("Melding versturen", cut.Markup, StringComparison.Ordinal);
        Assert.Contains("Meld alleen iets als je denkt dat het echt niet klopt.", cut.Markup, StringComparison.Ordinal);
        Assert.Contains("We bewaren je IP-adres niet.", cut.Markup, StringComparison.Ordinal);
    }

    [Fact]
    public void Success_state_thanks_the_visitor_and_hides_the_form()
    {
        Services.AddSingleton<NavigationManager>(new StaticNavigation("/melden?type=vacancy&id=x&verzonden=mail"));
        var cut = RenderComponent<Melden>();

        Assert.Contains("Dank je. We kijken ernaar.", cut.Markup, StringComparison.Ordinal);
        Assert.Contains("Je krijgt een bevestiging per e-mail.", cut.Markup, StringComparison.Ordinal);
        Assert.Empty(cut.FindAll("form"));
    }

    [Fact]
    public void Rate_limited_visitor_sees_a_plain_message()
    {
        Services.AddSingleton<NavigationManager>(new StaticNavigation("/melden?type=company&id=90000601&fout=teveel"));
        var cut = RenderComponent<Melden>();

        Assert.Contains("Je hebt al veel gemeld.", cut.Markup, StringComparison.Ordinal);
        Assert.DoesNotContain("Exception", cut.Markup, StringComparison.Ordinal);
    }

    private sealed class AlwaysValidAntiforgery : IAntiforgery
    {
        public AntiforgeryTokenSet GetAndStoreTokens(HttpContext httpContext)
            => new("req", "cookie", "form", "header");

        public AntiforgeryTokenSet GetTokens(HttpContext httpContext) => GetAndStoreTokens(httpContext);
        public Task<bool> IsRequestValidAsync(HttpContext httpContext) => Task.FromResult(true);
        public void ValidateRequest(HttpContext httpContext) { }
        public Task ValidateRequestAsync(HttpContext httpContext) => Task.CompletedTask;
        public void SetCookieTokenAndHeader(HttpContext httpContext) { }
    }

    /// <summary>The target name is decoration; without an API the form must still render.</summary>
    private sealed class NoApiHttpClientFactory : IHttpClientFactory
    {
        public HttpClient CreateClient(string name)
            => new(new FailingHandler()) { BaseAddress = new Uri("http://api.invalid/") };

        private sealed class FailingHandler : HttpMessageHandler
        {
            protected override Task<HttpResponseMessage> SendAsync(
                HttpRequestMessage request,
                CancellationToken cancellationToken)
                => throw new HttpRequestException("no api in bunit");
        }
    }

    private sealed class AnonymousAuthState : AuthenticationStateProvider
    {
        public override Task<AuthenticationState> GetAuthenticationStateAsync()
            => Task.FromResult(new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity())));
    }

    private sealed class StaticNavigation : NavigationManager
    {
        public StaticNavigation(string uri) => Initialize("http://localhost/", "http://localhost" + uri);

        protected override void NavigateToCore(string uri, bool forceLoad)
        {
        }
    }
}

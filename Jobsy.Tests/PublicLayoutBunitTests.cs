using Bunit;
using Jobsy.Web.Components;
using Jobsy.Web.Components.Layout;
using Jobsy.Web.Components.Layout.Public;
using Jobsy.Web.Features;
using Jobsy.Web.Localization;
using Jobsy.Web.Navigation;
using Jobsy.Web.Seo;
using Jobsy.Web.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.JSInterop;
using System.Security.Claims;

namespace Jobsy.Tests;

public class PublicLayoutBunitTests : TestContext
{
    public PublicLayoutBunitTests()
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
        Services.AddScoped<LandingVariantResolver>();
        Services.AddScoped(sp => new CultureState(
            sp.GetRequiredService<IJSRuntime>(),
            sp,
            sp.GetRequiredService<AuthenticationStateProvider>()));
        Services.AddScoped<PageSeoContext>();
        Services.AddSingleton<NavigationManager>(new FakeNavigation("/"));
    }

    [Fact]
    public void Renders_one_cookie_banner_skip_link_and_available_header_only()
    {
        var cut = RenderComponent<PublicLayout>(p => p
            .Add(c => c.Body, (RenderFragment)(b => b.AddMarkupContent(0, "<div data-testid=\"body\">body</div>"))));

        Assert.Single(cut.FindAll(".cookie-consent"));
        var skip = cut.Find("a.pub-skip");
        Assert.Equal("#main", skip.GetAttribute("href"));
        Assert.Contains("Naar de inhoud", skip.TextContent, StringComparison.Ordinal);

        // First focusable-ish content order: skip link appears before header brand.
        var markup = cut.Markup;
        Assert.True(markup.IndexOf("pub-skip", StringComparison.Ordinal) < markup.IndexOf("pub-header", StringComparison.Ordinal));

        Assert.Contains("/banenkaart", markup, StringComparison.Ordinal);
        Assert.DoesNotContain("/werkgevers", markup, StringComparison.Ordinal);
        Assert.Contains("/partner", markup, StringComparison.Ordinal);
        Assert.Contains("/hoe-werkt-lobsy", markup, StringComparison.Ordinal);
        Assert.Contains("pub-theme", markup, StringComparison.Ordinal);
    }

    [Fact]
    public void Rtl_when_culture_is_ar()
    {
        var http = Services.GetRequiredService<IHttpContextAccessor>().HttpContext!;
        http.Request.QueryString = new QueryString("?lang=ar");
        var culture = Services.GetRequiredService<CultureState>();
        culture.InitializeFromRequest(http);

        var cut = RenderComponent<PublicLayout>(p => p
            .Add(c => c.Body, (RenderFragment)(b => b.AddContent(0, "ar"))));
        var root = cut.Find(".pub-theme");
        Assert.Equal("rtl", root.GetAttribute("dir"));
        Assert.Equal("ar", root.GetAttribute("lang"));
    }

    [Fact]
    public void Probe_content_renders_under_layout()
    {
        var cut = RenderComponent<PublicLayout>(p => p
            .Add(c => c.Body, (RenderFragment)(b =>
            {
                b.OpenComponent<PublicLayoutProbe>(0);
                b.CloseComponent();
            })));
        Assert.Contains("data-testid=\"pub-layout-probe\"", cut.Markup, StringComparison.Ordinal);
        Assert.Contains("pub-header", cut.Markup, StringComparison.Ordinal);
        Assert.Contains("pub-footer", cut.Markup, StringComparison.Ordinal);
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
}

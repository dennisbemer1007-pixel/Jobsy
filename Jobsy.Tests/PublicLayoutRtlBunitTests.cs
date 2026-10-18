using Bunit;
using Jobsy.Core.Localization;
using Jobsy.Web.Components.Layout;
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

/// <summary>Isolated from <see cref="PublicLayoutBunitTests"/> so culture is not pinned before <c>?lang=ar</c>.</summary>
public sealed class PublicLayoutRtlBunitTests : BunitContext
{
    public PublicLayoutRtlBunitTests()
    {
        JobsyLanguages.ConfigureEnabledCultures("nl,en,pl,ro,ar");
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddSingleton<AuthenticationStateProvider>(new FakeAuth());
        Services.AddSingleton<IConfiguration>(new ConfigurationBuilder().Build());
        Services.AddSingleton<ICookieConsentTokenService, CookieConsentTokenService>();
        Services.AddSingleton<IEmployersSwitch, AlwaysOnEmployersSwitch>();
        Services.AddSingleton<IHostEnvironment>(new FakeEnv(Environments.Development));
        var httpContext = new DefaultHttpContext { Request = { QueryString = new QueryString("?lang=ar") } };
        Services.AddSingleton<IHttpContextAccessor>(new HttpContextAccessor { HttpContext = httpContext });
        Services.AddLogging();
        Services.AddMemoryCache();
        Services.AddSingleton<IHttpClientFactory>(new TimeoutHttpClientFactory());
        Services.AddSingleton<LegalIdentityProvider>();
        Services.AddScoped<LandingVariantResolver>();
        Services.AddScoped(sp => new CultureState(
            sp.GetRequiredService<IJSRuntime>(),
            sp,
            sp.GetRequiredService<AuthenticationStateProvider>()));
        Services.AddScoped<PageSeoContext>();
        Services.AddSingleton(new Jobsy.Web.Hosting.MaintenanceState());
        Services.AddSingleton<NavigationManager>(new RtlFakeNavigation("/"));
    }

    [Fact]
    public void Rtl_when_culture_is_ar()
    {
        try
        {
            var cut = Render<PublicLayout>(p => p
                .Add(c => c.Body, (RenderFragment)(b => b.AddContent(0, "ar"))));
            var root = cut.Find(".pub-theme");
            Assert.Equal("rtl", root.GetAttribute("dir"));
            Assert.Equal("ar", root.GetAttribute("lang"));
        }
        finally
        {
            JobsyLanguages.ResetEnabledForTests();
        }
    }

    private sealed class FakeAuth : AuthenticationStateProvider
    {
        public override Task<AuthenticationState> GetAuthenticationStateAsync()
            => Task.FromResult(new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity())));
    }

    private sealed class FakeEnv(string env) : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = env;
        public string ApplicationName { get; set; } = "Jobsy.Tests";
        public string ContentRootPath { get; set; } = AppContext.BaseDirectory;
        public Microsoft.Extensions.FileProviders.IFileProvider ContentRootFileProvider { get; set; }
            = new Microsoft.Extensions.FileProviders.NullFileProvider();
    }

    private sealed class RtlFakeNavigation : NavigationManager
    {
        public RtlFakeNavigation(string uri) => Initialize("http://localhost/", "http://localhost" + uri);
        protected override void NavigateToCore(string uri, bool forceLoad) { }
    }

    private sealed class TimeoutHttpClientFactory : IHttpClientFactory
    {
        public HttpClient CreateClient(string name)
            => new(new TimeoutHandler()) { BaseAddress = new Uri("http://localhost/") };
    }

    private sealed class TimeoutHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
            => Task.FromResult(new HttpResponseMessage(System.Net.HttpStatusCode.RequestTimeout));
    }
}

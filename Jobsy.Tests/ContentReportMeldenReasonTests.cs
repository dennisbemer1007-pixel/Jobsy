using System.Net;
using System.Security.Claims;
using Bunit;
using Jobsy.Core.Enums;
using Jobsy.Web.Components.Pages.Public;
using Jobsy.Web.Hosting;
using Jobsy.Web.Localization;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.JSInterop;
using System.Threading.RateLimiting;

namespace Jobsy.Tests;

public class ContentReportMeldenReasonTests
{
    [Fact]
    public void AppendReasonQuery_keeps_defined_enum_and_ignores_junk()
    {
        Assert.Equal("&reden=Fake", ContentReportEndpoints.AppendReasonQuery("Fake"));
        Assert.Equal("&reden=Other", ContentReportEndpoints.AppendReasonQuery("other"));
        Assert.Equal(string.Empty, ContentReportEndpoints.AppendReasonQuery("Scam"));
        Assert.Equal(string.Empty, ContentReportEndpoints.AppendReasonQuery("not-a-reason"));
        Assert.Equal(string.Empty, ContentReportEndpoints.AppendReasonQuery(null));
    }

    [Fact]
    public async Task Post_with_valid_reason_that_fails_forward_keeps_reden_on_redirect()
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Services.AddLogging();
        builder.Services.AddRouting();
        builder.Services.RemoveAll<IAntiforgery>();
        builder.Services.AddSingleton<IAntiforgery>(new AlwaysValidAntiforgery());
        builder.Services.AddRateLimiter(options =>
        {
            options.AddPolicy("report-form", _ => RateLimitPartition.GetNoLimiter("melden-test"));
        });
        builder.Services.AddHttpClient(ContentReportEndpoints.HttpClientName, client =>
            {
                client.BaseAddress = new Uri("http://api.test/");
            })
            .ConfigurePrimaryHttpMessageHandler(() => new FailingApiHandler());

        await using var app = builder.Build();
        app.UseRateLimiter();
        app.MapContentReportEndpoints();
        await app.StartAsync();

        var client = app.GetTestClient();
        using var content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["type"] = "vacancy",
            ["id"] = Guid.NewGuid().ToString("D"),
            ["reason"] = nameof(ContentReportReason.Fake),
            ["details"] = "test"
        });

        var post = await client.PostAsync("/melden/verstuur", content);
        Assert.True(
            post.StatusCode is HttpStatusCode.Redirect or HttpStatusCode.RedirectMethod or HttpStatusCode.SeeOther,
            $"unexpected status {post.StatusCode}: {await post.Content.ReadAsStringAsync()}");
        var location = post.Headers.Location?.ToString() ?? string.Empty;
        Assert.Contains("fout=opnieuw", location, StringComparison.Ordinal);
        Assert.Contains("reden=Fake", location, StringComparison.Ordinal);
    }

    private sealed class FailingApiHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
            => Task.FromResult(new HttpResponseMessage(HttpStatusCode.InternalServerError));
    }

    private sealed class AlwaysValidAntiforgery : IAntiforgery
    {
        public AntiforgeryTokenSet GetAndStoreTokens(HttpContext httpContext)
            => new("req", "cookie", "form", "header");

        public AntiforgeryTokenSet GetTokens(HttpContext httpContext) => GetAndStoreTokens(httpContext);
        public Task<bool> IsRequestValidAsync(HttpContext httpContext) => Task.FromResult(true);
        public void ValidateRequest(HttpContext _) { }
        public Task ValidateRequestAsync(HttpContext _) => Task.CompletedTask;
        public void SetCookieTokenAndHeader(HttpContext httpContext) { }
    }
}

public class ContentReportMeldenReasonBunitTests : BunitContext
{
    public ContentReportMeldenReasonBunitTests()
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
    }

    [Fact]
    public void Error_query_with_reden_Fake_checks_that_radio()
    {
        Services.AddSingleton<NavigationManager>(
            new StaticNavigation("/melden?type=vacancy&id=abc&fout=opnieuw&reden=Fake"));
        var cut = Render<Melden>();

        var checkedRadio = cut.Find("input[name='reason'][value='Fake']");
        Assert.True(checkedRadio.HasAttribute("checked"));
    }

    [Fact]
    public void Invalid_reden_is_ignored()
    {
        Services.AddSingleton<NavigationManager>(
            new StaticNavigation("/melden?type=vacancy&id=abc&fout=opnieuw&reden=Scam"));
        var cut = Render<Melden>();

        Assert.Empty(cut.FindAll("input[name='reason'][checked]"));
    }

    private sealed class AlwaysValidAntiforgery : IAntiforgery
    {
        public AntiforgeryTokenSet GetAndStoreTokens(HttpContext httpContext)
            => new("req", "cookie", "form", "header");

        public AntiforgeryTokenSet GetTokens(HttpContext httpContext) => GetAndStoreTokens(httpContext);
        public Task<bool> IsRequestValidAsync(HttpContext httpContext) => Task.FromResult(true);
        public void ValidateRequest(HttpContext _) { }
        public Task ValidateRequestAsync(HttpContext _) => Task.CompletedTask;
        public void SetCookieTokenAndHeader(HttpContext httpContext) { }
    }

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

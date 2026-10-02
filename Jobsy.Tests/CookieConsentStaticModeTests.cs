using Jobsy.Core.Privacy;
using Jobsy.Web.Components;
using Jobsy.Web.Localization;
using Jobsy.Web.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Bunit;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.JSInterop;
using System.Net;
using System.Security.Claims;
using System.Text.Json;

namespace Jobsy.Tests;

public class CookieConsentStaticModeTests : BunitContext
{
    public CookieConsentStaticModeTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddSingleton<IConfiguration>(new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["JobsyAuth:DevelopmentAuthSecret"] = "test-dev-secret-for-cookie-consent-token-xx"
            })
            .Build());
        Services.AddSingleton<ICookieConsentTokenService, CookieConsentTokenService>();
        Services.AddSingleton<AuthenticationStateProvider>(new FakeAuth());
    }

    [Fact]
    public void Static_markup_has_data_consent_and_no_blazor_onclick()
    {
        var cut = Render<CookieConsentBanner>(p =>
            p.Add(c => c.Mode, CookieConsentBanner.CookieConsentMode.Static));
        var markup = cut.Markup;
        Assert.Contains("data-consent=\"necessary\"", markup, StringComparison.Ordinal);
        Assert.Contains("data-consent=\"analytics\"", markup, StringComparison.Ordinal);
        Assert.DoesNotContain("blazor:onclick", markup, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("@onclick", markup, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Token_service_matches_legacy_mint_format()
    {
        var config = Services.GetRequiredService<IConfiguration>();
        var svc = Services.GetRequiredService<ICookieConsentTokenService>();
        var a = svc.MintAnalyticsToken();
        var b = CookieConsentBanner.MintAnalyticsToken(config);
        Assert.StartsWith(CookieConsentNames.AnalyticsValue + ".", a, StringComparison.Ordinal);
        Assert.StartsWith(CookieConsentNames.AnalyticsValue + ".", b, StringComparison.Ordinal);
        Assert.Equal(3, a.Split('.').Length);
    }

    [Fact]
    public async Task Token_endpoint_rejects_cross_site()
    {
        await using var app = await CreateAppAsync();
        var client = app.GetTestClient();
        using var req = new HttpRequestMessage(HttpMethod.Post, "/account/cookie-consent/analytics-token");
        req.Headers.TryAddWithoutValidation("Sec-Fetch-Site", "cross-site");
        req.Headers.TryAddWithoutValidation("Origin", "https://evil.example");
        var response = await client.SendAsync(req);
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Token_endpoint_returns_token_for_same_origin()
    {
        await using var app = await CreateAppAsync();
        var client = app.GetTestClient();
        using var req = new HttpRequestMessage(HttpMethod.Post, "/account/cookie-consent/analytics-token");
        req.Headers.TryAddWithoutValidation("Sec-Fetch-Site", "same-origin");
        req.Headers.TryAddWithoutValidation("Origin", "http://localhost");
        var response = await client.SendAsync(req);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.True(json.RootElement.TryGetProperty("token", out var token));
        Assert.StartsWith("analytics.", token.GetString(), StringComparison.Ordinal);
    }

    private static async Task<WebApplication> CreateAppAsync()
    {
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions
        {
            EnvironmentName = Environments.Development
        });
        builder.WebHost.UseTestServer();
        builder.Configuration["JobsyAuth:DevelopmentAuthSecret"] = "test-dev-secret-for-cookie-consent-token-xx";
        builder.Services.AddSingleton<ICookieConsentTokenService, CookieConsentTokenService>();
        var app = builder.Build();
        app.MapCookieConsentEndpoints();
        await app.StartAsync();
        return app;
    }

    private sealed class FakeAuth : AuthenticationStateProvider
    {
        public override Task<AuthenticationState> GetAuthenticationStateAsync()
            => Task.FromResult(new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity())));
    }
}

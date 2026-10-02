using System.Net;
using Jobsy.Core.Localization;
using Jobsy.Web.Auth;
using Jobsy.Web.Localization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Jobsy.Tests;

public class LanguageEndpointTests
{
    [Fact]
    public async Task Valid_lang_sets_cookie_and_redirects_local()
    {
        await using var app = await CreateAppAsync();
        var client = app.GetTestClient();
        var response = await client.GetAsync("/taal/en?returnUrl=/ontdek");
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal("/ontdek", response.Headers.Location?.ToString());
        Assert.Contains(response.Headers.GetValues("Set-Cookie"), c => c.Contains("Jobsy.Culture=en", StringComparison.Ordinal));
    }

    [Fact]
    public async Task Invalid_lang_redirects_home()
    {
        await using var app = await CreateAppAsync();
        var client = app.GetTestClient();
        var response = await client.GetAsync("/taal/xx?returnUrl=/ontdek");
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal("/", response.Headers.Location?.ToString());
    }

    [Theory]
    [InlineData("https://evil.example/")]
    [InlineData("//evil")]
    [InlineData("\\\\evil")]
    public async Task Unsafe_returnUrl_falls_back_to_home(string evil)
    {
        await using var app = await CreateAppAsync();
        var client = app.GetTestClient();
        var response = await client.GetAsync("/taal/en?returnUrl=" + Uri.EscapeDataString(evil));
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal("/", response.Headers.Location?.ToString());
    }

    [Fact]
    public void Lang_query_resolves_ar_without_cookie()
    {
        var http = new DefaultHttpContext();
        http.Request.QueryString = new QueryString("?lang=ar");
        Assert.Equal("ar", CultureRequest.ResolveLanguage(http));
        Assert.True(JobsyLanguages.Get("ar").IsRightToLeft);
        Assert.True(AuthRedirects.IsLocalReturnUrl("/ontdek"));
        Assert.False(AuthRedirects.IsLocalReturnUrl("https://evil"));
    }

    private static async Task<WebApplication> CreateAppAsync()
    {
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions
        {
            EnvironmentName = Environments.Development
        });
        builder.WebHost.UseTestServer();
        var app = builder.Build();
        app.MapLanguageEndpoints();
        await app.StartAsync();
        return app;
    }
}

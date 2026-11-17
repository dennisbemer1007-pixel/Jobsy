using Jobsy.Api.Authorization;
using Jobsy.Api.Security;
using Jobsy.Core.Security;
using Jobsy.Web.Auth;
using Jobsy.Web.Security;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;

namespace Jobsy.Tests;

/// <summary>
/// Render leaves JWT PEMs and CLOUDFLARE_ORIGIN_SECRET as sync:false.
/// JWT keys are fail-closed outside Development/Testing; Cloudflare middleware still boots with a logged bootstrap.
/// </summary>
public class JwtPemBootstrapTests
{
    [Fact]
    public void Api_authorization_fails_in_Production_without_Jwt_PEMs()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddMemoryCache();
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["AzureAd:ClientId"] = ""
        }).Build();
        var env = new FakeHostEnvironment { EnvironmentName = Environments.Production };

        var ex = Assert.Throws<InvalidOperationException>(() =>
            services.AddJobsyApiAuthorization(config, env));

        Assert.Contains("PEM keys are required", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Web_issuer_fails_in_Production_without_PrivateKeyPem()
    {
        var config = new ConfigurationBuilder().AddInMemoryCollection().Build();
        var env = new FakeHostEnvironment { EnvironmentName = Environments.Production };

        Assert.Throws<InvalidOperationException>(() => new JobsyAccessTokenIssuer(config, env));
    }

    [Fact]
    public void Cloudflare_middleware_boots_in_Production_without_origin_secret()
    {
        var config = new ConfigurationBuilder().AddInMemoryCollection().Build();
        var env = new FakeHostEnvironment { EnvironmentName = Environments.Production };
        RequestDelegate next = _ => Task.CompletedTask;

        var api = new Jobsy.Api.Security.CloudflareOriginMiddleware(next, config, env);
        var web = new Jobsy.Web.Security.CloudflareOriginMiddleware(next, config, env);
        Assert.NotNull(api);
        Assert.NotNull(web);
    }

    [Fact]
    public async Task Cloudflare_middleware_sets_https_Scheme_without_origin_secret()
    {
        var config = new ConfigurationBuilder().AddInMemoryCollection().Build();
        var env = new FakeHostEnvironment { EnvironmentName = Environments.Production };
        var scheme = "http";
        RequestDelegate next = ctx =>
        {
            scheme = ctx.Request.Scheme;
            return Task.CompletedTask;
        };

        var web = new Jobsy.Web.Security.CloudflareOriginMiddleware(next, config, env);
        var http = new DefaultHttpContext();
        http.Request.Scheme = "http";
        http.Request.Headers["X-Forwarded-Proto"] = "https, https";

        await web.InvokeAsync(http);

        Assert.Equal(Uri.UriSchemeHttps, scheme);
        Assert.Equal(Uri.UriSchemeHttps, http.Request.Scheme);
    }

    private sealed class FakeHostEnvironment : IHostEnvironment, IWebHostEnvironment
    {
        public string EnvironmentName { get; set; } = Environments.Production;
        public string ApplicationName { get; set; } = "Jobsy.Tests";
        public string ContentRootPath { get; set; } = AppContext.BaseDirectory;
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
        public string WebRootPath { get; set; } = AppContext.BaseDirectory;
        public IFileProvider WebRootFileProvider { get; set; } = new NullFileProvider();
    }
}

using Jobsy.Api.Authorization;
using Jobsy.Api.Security;
using Jobsy.Core.Security;
using Jobsy.Web.Auth;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;

namespace Jobsy.Tests;

/// <summary>
/// Render leaves JWT PEMs and CLOUDFLARE_ORIGIN_SECRET as sync:false. Production must
/// boot with a logged bootstrap instead of exit 134.
/// </summary>
public class JwtPemBootstrapTests
{
    [Fact]
    public void Api_authorization_boots_in_Production_without_Jwt_PEMs()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddMemoryCache();
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["AzureAd:ClientId"] = ""
        }).Build();
        var env = new FakeHostEnvironment { EnvironmentName = Environments.Production };

        services.AddJobsyApiAuthorization(config, env);

        using var sp = services.BuildServiceProvider();
        var parms = sp.GetRequiredService<TokenValidationParameters>();
        Assert.NotNull(parms.IssuerSigningKey);
        Assert.Equal(JobsyAccessToken.DevelopmentPublicKeyPem.Trim(), JobsyDevJwtKeys.PublicPem?.Trim());
    }

    [Fact]
    public void Web_issuer_boots_in_Production_without_PrivateKeyPem()
    {
        var config = new ConfigurationBuilder().AddInMemoryCollection().Build();
        var env = new FakeHostEnvironment { EnvironmentName = Environments.Production };

        var issuer = new JobsyAccessTokenIssuer(config, env);
        var token = issuer.TryCreate(CreatePrincipal(Guid.NewGuid()));
        Assert.False(string.IsNullOrWhiteSpace(token));
    }

    [Fact]
    public void Cloudflare_middleware_boots_in_Production_without_origin_secret()
    {
        var config = new ConfigurationBuilder().AddInMemoryCollection().Build();
        var env = new FakeHostEnvironment { EnvironmentName = Environments.Production };
        RequestDelegate next = _ => Task.CompletedTask;

        var api = new CloudflareOriginMiddleware(next, config, env);
        var web = new Jobsy.Web.Security.CloudflareOriginMiddleware(next, config, env);
        Assert.NotNull(api);
        Assert.NotNull(web);
    }

    private static System.Security.Claims.ClaimsPrincipal CreatePrincipal(Guid userId)
    {
        var identity = new System.Security.Claims.ClaimsIdentity(
            [
                new System.Security.Claims.Claim(
                    System.Security.Claims.ClaimTypes.NameIdentifier,
                    userId.ToString("D")),
                new System.Security.Claims.Claim(JobsyAccessToken.SessionVersionClaim, "1")
            ],
            authenticationType: "test");
        return new System.Security.Claims.ClaimsPrincipal(identity);
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

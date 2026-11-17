using Jobsy.Api.Authorization;
using Jobsy.Core.Security;
using Jobsy.Web.Auth;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;

namespace Jobsy.Tests;

public class JwtKeyBootstrapTests
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
    public void Api_authorization_fails_in_Production_with_Dev_public_PEM()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddMemoryCache();
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["AzureAd:ClientId"] = "",
            ["JobsyAuth:Jwt:PublicKeyPem"] = JobsyAccessToken.DevelopmentPublicKeyPem,
            ["JobsyAuth:Jwt:PrivateKeyPem"] = JobsyAccessToken.DevelopmentPrivateKeyPem
        }).Build();
        var env = new FakeHostEnvironment { EnvironmentName = Environments.Production };

        var ex = Assert.Throws<InvalidOperationException>(() =>
            services.AddJobsyApiAuthorization(config, env));

        Assert.Contains("must not use the repository Development PEM", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Web_issuer_fails_in_Production_without_PrivateKeyPem()
    {
        var config = new ConfigurationBuilder().Build();
        var env = new FakeHostEnvironment { EnvironmentName = Environments.Production };

        Assert.Throws<InvalidOperationException>(() => new JobsyAccessTokenIssuer(config, env));
    }

    [Fact]
    public void Token_signed_with_dev_private_key_is_rejected_when_validation_uses_dedicated_PEM()
    {
        var (_, publicPem) = JobsyTestAuth.ProductionTestKeyPair;
        var parms = JobsyAccessToken.CreateValidationParameters(
            publicPem,
            JobsyAccessToken.DefaultIssuer,
            JobsyAccessToken.DefaultAudience);

        var forged = JobsyAccessToken.Create(
            Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee"),
            0,
            JobsyAccessToken.DevelopmentPrivateKeyPem);

        var handler = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler();
        Assert.ThrowsAny<SecurityTokenException>(() =>
            handler.ValidateToken(forged, parms.Clone(), out _));
    }

    private sealed class FakeHostEnvironment : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = Environments.Production;
        public string ApplicationName { get; set; } = "Jobsy.Tests";
        public string ContentRootPath { get; set; } = AppContext.BaseDirectory;
        public Microsoft.Extensions.FileProviders.IFileProvider ContentRootFileProvider { get; set; }
            = new Microsoft.Extensions.FileProviders.NullFileProvider();
    }
}

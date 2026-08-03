using Jobsy.Api.Controllers;
using Jobsy.Api.Models;
using Jobsy.Core.Entities;
using Jobsy.Core.Enums;
using Jobsy.Infrastructure.Data;
using Jobsy.Infrastructure.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;

namespace Jobsy.Tests;

public class ExternalProviderConfigTests
{
    [Fact]
    public async Task External_providers_status_reflects_integrations()
    {
        await using var db = CreateDb();
        var credentials = new IntegrationCredentialService(db, new PassthroughSecretProtector());
        await credentials.UpsertAsync(
            IntegrationKey.MicrosoftEntra,
            new Jobsy.Core.Interfaces.IntegrationCredentialUpdate(
                ClientId: "entra-client",
                ClientSecret: "entra-secret",
                TenantId: "common"));

        var sut = CreateAuthController(db, credentials, "secret");
        sut.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() };

        var result = await sut.GetExternalProviders(CancellationToken.None);
        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var body = Assert.IsType<ExternalProvidersStatusResponse>(ok.Value);
        Assert.True(body.Entra);
        Assert.False(body.Google);
    }

    [Fact]
    public async Task External_provider_config_requires_secret_and_returns_creds()
    {
        await using var db = CreateDb();
        var credentials = new IntegrationCredentialService(db, new PassthroughSecretProtector());
        await credentials.UpsertAsync(
            IntegrationKey.GoogleEntra,
            new Jobsy.Core.Interfaces.IntegrationCredentialUpdate(
                ClientId: "google-client",
                ClientSecret: "google-secret"));

        var sut = CreateAuthController(db, credentials, "secret");
        var http = new DefaultHttpContext();
        http.Request.Headers["X-Jobsy-Provision-Secret"] = "secret";
        sut.ControllerContext = new ControllerContext { HttpContext = http };

        var result = await sut.GetExternalProviderConfig("google", CancellationToken.None);
        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var body = Assert.IsType<ExternalProviderConfigResponse>(ok.Value);
        Assert.Equal("google-client", body.ClientId);
        Assert.Equal("google-secret", body.ClientSecret);
    }

    private static AuthController CreateAuthController(
        JobsyDbContext db,
        IntegrationCredentialService credentials,
        string secret)
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["JobsyAuth:DevelopmentAuthSecret"] = secret
            })
            .Build();
        return new AuthController(
            db, config, credentials,
            new AmbassadeurAttributionService(db, new AmbassadeurSettingsService(db), Microsoft.Extensions.Logging.Abstractions.NullLogger<AmbassadeurAttributionService>.Instance),
            new StubHostEnvironment { EnvironmentName = Environments.Development });
    }

    private static JobsyDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<JobsyDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new JobsyDbContext(options);
    }

    private sealed class StubHostEnvironment : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = Environments.Development;
        public string ApplicationName { get; set; } = "Jobsy.Tests";
        public string ContentRootPath { get; set; } = AppContext.BaseDirectory;
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}

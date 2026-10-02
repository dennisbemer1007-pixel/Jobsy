using System.Net;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Jobsy.Infrastructure.Data;

namespace Jobsy.Tests;

public class OpenApiExternalDocumentTests
{
    [Fact]
    public async Task Development_openapi_external_json_is_200_with_api_key_scheme()
    {
        await using var factory = new OpenApiApiFactory("Development");
        var client = factory.CreateClient();
        var response = await client.GetAsync("/openapi/external.json");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.Content.ReadAsStringAsync();
        Assert.Contains("Lobsy externe vacature-API", json, StringComparison.Ordinal);
        Assert.Contains("api/external/vacancies", json, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("X-API-Key", json, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData("Production")]
    [InlineData("Acceptatie")]
    public async Task Non_development_openapi_and_scalar_return_404(string environment)
    {
        await using var factory = new OpenApiApiFactory(environment);
        var client = factory.CreateClient();
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync("/openapi/external.json")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync("/openapi/v1.json")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync("/scalar")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync("/swagger/index.html")).StatusCode);
    }

    [Fact]
    public void Swashbuckle_package_is_gone()
    {
        var props = File.ReadAllText(Path.Combine(RepoRoot(), "Directory.Packages.props"));
        var csproj = File.ReadAllText(Path.Combine(RepoRoot(), "Jobsy.Api", "Jobsy.Api.csproj"));
        Assert.DoesNotContain("Swashbuckle", props, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Swashbuckle", csproj, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Microsoft.AspNetCore.OpenApi", props, StringComparison.Ordinal);
        Assert.Contains("Scalar.AspNetCore", props, StringComparison.Ordinal);
    }

    private static string RepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "Jobsy.sln")))
            {
                return dir.FullName;
            }

            dir = dir.Parent;
        }

        throw new InvalidOperationException("Jobsy.sln not found.");
    }

    private sealed class OpenApiApiFactory(string environment) : WebApplicationFactory<Jobsy.Api.ApiAssemblyMarker>
    {
        private readonly string _dbName = "OpenApi-" + Guid.NewGuid();

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment(environment);
            JobsyTestAuth.ApplyStandardAuthSettings(builder);
            builder.UseSetting("Seed:Enabled", "false");
            // Non-Development host without a real key-store DB (CI only has JobsyCi): allow ephemeral keys
            // like the other Production-environment test hosts do.
            builder.UseSetting("JobsyAuth:AllowEphemeralDataProtection", "true");
            builder.UseSetting("VerificationCodes:Pepper", "test-pepper-openapi-external-doc-32chars!");
            builder.UseSetting(
                "ConnectionStrings:JobsyDb",
                "Host=127.0.0.1;Port=5432;Database=JobsyTest;Username=postgres;Password=postgres");

            builder.ConfigureTestServices(services =>
            {
                services.RemoveAll<IHostedService>();

                var efDescriptors = services
                    .Where(d =>
                        d.ServiceType == typeof(JobsyDbContext)
                        || d.ServiceType == typeof(DbContextOptions<JobsyDbContext>)
                        || (d.ServiceType.FullName?.Contains("EntityFrameworkCore", StringComparison.Ordinal) == true
                            && d.ServiceType.FullName.Contains("JobsyDbContext", StringComparison.Ordinal)))
                    .ToList();
                foreach (var d in efDescriptors)
                {
                    services.Remove(d);
                }

                services.AddDbContext<JobsyDbContext>(o => o.UseInMemoryDatabase(_dbName));
            });
        }
    }
}

using Jobsy.Api.Jobs;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Jobsy.Tests;

/// <summary>
/// Guards against reintroducing startup wipes (DemoDataPurge time bomb) in Production.
/// </summary>
public class ProductionNoWipeTests
{
    [Fact]
    public void DemoDataPurge_source_file_is_removed()
    {
        var path = Path.Combine(FindRepoRoot(), "Jobsy.Infrastructure", "Data", "DemoDataPurge.cs");
        Assert.False(File.Exists(path), "DemoDataPurge.cs must not exist — operational wipe was a production time bomb.");
    }

    [Fact]
    public void DatabaseSeedHostedService_does_not_wipe_users_or_operational_tables()
    {
        var hosted = File.ReadAllText(
            Path.Combine(FindRepoRoot(), "Jobsy.Api", "Jobs", "DatabaseSeedHostedService.cs"));
        Assert.DoesNotContain("PurgeDemoData", hosted, StringComparison.Ordinal);
        Assert.DoesNotContain("PreferWipeOverSeed", hosted, StringComparison.Ordinal);
        Assert.DoesNotContain("DemoDataPurge", hosted, StringComparison.Ordinal);
        Assert.DoesNotContain("Operational wipe", hosted, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Seed:Enabled", hosted, StringComparison.Ordinal);
    }

    [Fact]
    public void JobsyDbSeeder_has_no_purge_entry_points()
    {
        var seeder = File.ReadAllText(
            Path.Combine(FindRepoRoot(), "Jobsy.Infrastructure", "Data", "JobsyDbSeeder.cs"));
        Assert.DoesNotContain("PurgeDemoData", seeder, StringComparison.Ordinal);
        Assert.DoesNotContain("PreferWipeOverSeed", seeder, StringComparison.Ordinal);
        Assert.DoesNotContain("DemoDataPurge", seeder, StringComparison.Ordinal);
        Assert.DoesNotContain("ExecuteDelete", seeder, StringComparison.Ordinal);
        Assert.DoesNotContain("TRUNCATE", seeder, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Production_host_does_not_register_a_purge_hosted_service()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IHostEnvironment>(new FakeProductionEnvironment());
        // Only the real seed hosted service type should exist; no DemoDataPurge* IHostedService.
        Assert.Null(Type.GetType("Jobsy.Infrastructure.Data.DemoDataPurge, Jobsy.Infrastructure"));
        Assert.True(typeof(DatabaseSeedHostedService).IsAssignableTo(typeof(IHostedService)));
        var purgeTypes = typeof(DatabaseSeedHostedService).Assembly.GetTypes()
            .Concat(typeof(Jobsy.Infrastructure.Data.JobsyDbContext).Assembly.GetTypes())
            .Where(t => t.Name.Contains("Purge", StringComparison.OrdinalIgnoreCase)
                        && typeof(IHostedService).IsAssignableFrom(t));
        Assert.Empty(purgeTypes);
    }

    [Fact]
    public void Render_blueprint_does_not_enable_purge_or_development_auth_on_production()
    {
        var yaml = File.ReadAllText(Path.Combine(FindRepoRoot(), "render.yaml"));
        Assert.DoesNotContain("Seed__PurgeDemoData", yaml, StringComparison.Ordinal);
        // Production block (jobsy-api) must set AllowDevelopmentAuth false.
        Assert.Contains("JobsyAuth__AllowDevelopmentAuth", yaml, StringComparison.Ordinal);
        Assert.DoesNotContain(
            """
              - key: JobsyAuth__AllowDevelopmentAuth
                value: "true"
            """.Replace("\r\n", "\n"),
            yaml.Replace("\r\n", "\n"),
            StringComparison.Ordinal);
    }

    private sealed class FakeProductionEnvironment : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = Environments.Production;
        public string ApplicationName { get; set; } = "Jobsy.Tests";
        public string ContentRootPath { get; set; } = "/tmp";
        public Microsoft.Extensions.FileProviders.IFileProvider ContentRootFileProvider { get; set; }
            = new Microsoft.Extensions.FileProviders.NullFileProvider();
    }

    private static string FindRepoRoot()
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

        throw new InvalidOperationException("Jobsy.sln not found from test base directory.");
    }
}

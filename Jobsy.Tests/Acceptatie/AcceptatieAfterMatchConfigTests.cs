using Jobsy.Infrastructure.Hosting;
using Microsoft.Extensions.Configuration;

namespace Jobsy.Tests.Acceptatie;

public sealed class AcceptatieAfterMatchConfigTests
{
    [Fact]
    public void Api_acceptatie_appsettings_enables_na_de_match()
    {
        var path = Path.Combine(TestRepo.FindRoot(), "Jobsy.Api/appsettings.Acceptatie.json");
        var json = File.ReadAllText(path);
        Assert.Contains("\"NaDeMatchEnabled\": true", json, StringComparison.Ordinal);
    }

    [Fact]
    public void Acceptatie_bootstrap_hosted_service_source_requires_after_match()
    {
        var path = Path.Combine(
            TestRepo.FindRoot(),
            "Jobsy.Api/Jobs/PlatformFeatureAcceptatieBootstrapHostedService.cs");
        var source = File.ReadAllText(path);
        Assert.Contains("snap.AfterMatchEnabled", source, StringComparison.Ordinal);
        Assert.Contains("AfterMatchEnabled: true", source, StringComparison.Ordinal);
    }

    [Fact]
    public void IsAcceptatieDeployment_matches_lobsy_env()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Lobsy:DeploymentEnvironment"] = "Acceptatie",
            })
            .Build();
        Assert.True(AcceptatieConfigurationExtensions.IsAcceptatieDeployment(config));
    }
}

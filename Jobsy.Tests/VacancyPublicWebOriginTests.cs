using Jobsy.Core.Media;
using Microsoft.Extensions.Configuration;

namespace Jobsy.Tests;

public sealed class VacancyPublicWebOriginTests
{
    [Fact]
    public void Resolve_uses_PublicWebBaseUrl_when_set()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["PublicWebBaseUrl"] = "https://custom.example/",
                ["Lobsy:DeploymentEnvironment"] = "Acceptatie"
            })
            .Build();

        Assert.Equal("https://custom.example", VacancyPublicWebOrigin.Resolve(config));
    }

    [Theory]
    [InlineData("Acceptatie", VacancyPublicWebOrigin.AcceptatieDefault)]
    [InlineData("acceptatie", VacancyPublicWebOrigin.AcceptatieDefault)]
    [InlineData("Productie", VacancyPublicWebOrigin.ProductionDefault)]
    [InlineData("productie", VacancyPublicWebOrigin.ProductionDefault)]
    public void Resolve_defaults_by_deployment_when_PublicWebBaseUrl_missing(string deployment, string expected)
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Lobsy:DeploymentEnvironment"] = deployment
            })
            .Build();

        Assert.Equal(expected, VacancyPublicWebOrigin.Resolve(config));
    }

    [Fact]
    public void Resolve_localhost_when_no_config()
    {
        var config = new ConfigurationBuilder().Build();
        Assert.Equal(VacancyPublicWebOrigin.LocalDefault, VacancyPublicWebOrigin.Resolve(config));
    }
}

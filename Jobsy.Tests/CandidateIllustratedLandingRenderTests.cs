using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Jobsy.Web.Features;

namespace Jobsy.Tests;

public class CandidateIllustratedLandingRenderTests
{
    [Fact]
    public async Task Landing_renders_illustrated_hero_and_steps()
    {
        await using var factory = new IllustratedLandingWebFactory();
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var html = await client.GetStringAsync("/");

        Assert.Contains("pub-landing__hero--illustrated", html, StringComparison.Ordinal);
        Assert.Contains("pub-landing__hero-art-band", html, StringComparison.Ordinal);
        Assert.Contains("pub-landing__steps-grid", html, StringComparison.Ordinal);
        Assert.Contains("vervoer-blok.jpg", html, StringComparison.Ordinal);
        Assert.Contains("NL en EN", html, StringComparison.Ordinal);
        Assert.DoesNotContain("landing-hero-passport", html, StringComparison.Ordinal);
    }

    private sealed class IllustratedLandingWebFactory : WebApplicationFactory<Jobsy.Web.WebAssemblyMarker>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Development");
            builder.ConfigureAppConfiguration((_, config) =>
            {
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["CandidatePublic:PlainSkin"] = "false",
                    ["Landing:ForceVariant"] = "on",
                    ["CLOUDFLARE_ORIGIN_SECRET"] = "",
                    ["JobsyAuth:Jwt:PrivateKeyPem"] = Jobsy.Core.Security.JobsyAccessToken.DevelopmentPrivateKeyPem
                });
            });
            builder.ConfigureTestServices(services =>
            {
                services.RemoveAll<IEmployersSwitch>();
                services.AddSingleton<IEmployersSwitch>(new AlwaysOnEmployersSwitch());
            });
        }
    }
}

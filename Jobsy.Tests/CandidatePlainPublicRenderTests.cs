using Jobsy.Web.Features;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Jobsy.Tests;

public sealed class CandidatePlainPublicRenderTests
{
    [Fact]
    public async Task Landing_renders_plain_hero_when_plain_skin_enabled()
    {
        await using var factory = new PlainPublicWebFactory();
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var html = await client.GetStringAsync("/");

        Assert.Contains("cp-plain-landing", html, StringComparison.Ordinal);
        Assert.Contains("/css/features/candidate-plain-public.css", html, StringComparison.Ordinal);
        Assert.Contains("data-plain-public-css", html, StringComparison.Ordinal);
        Assert.Contains("Werk dat bij je past", html, StringComparison.Ordinal);
        Assert.Contains("Account maken", html, StringComparison.Ordinal);
        Assert.Contains("href=\"/account-maken\"", html, StringComparison.Ordinal);
        Assert.Contains("href=\"/login\"", html, StringComparison.Ordinal);
        Assert.Contains("href=\"/privacy\"", html, StringComparison.Ordinal);
        Assert.DoesNotContain("pub-landing__hero--illustrated", html, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Login_renders_plain_card_when_plain_skin_enabled()
    {
        await using var factory = new PlainPublicWebFactory();
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var html = await client.GetStringAsync("/login");

        Assert.Contains("cp-plain-card", html, StringComparison.Ordinal);
        Assert.Contains("Fijn dat je er bent", html, StringComparison.Ordinal);
        Assert.Contains("action=\"/account/login\"", html, StringComparison.Ordinal);
        Assert.Contains("href=\"/account-maken", html, StringComparison.Ordinal);
        Assert.DoesNotContain("kreeft-header-strip", html, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Signup_renders_plain_card_when_plain_skin_enabled()
    {
        await using var factory = new PlainPublicWebFactory();
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var html = await client.GetStringAsync("/account-maken");

        Assert.Contains("cp-plain-card", html, StringComparison.Ordinal);
        Assert.Contains("action=\"/account/email-code/start\"", html, StringComparison.Ordinal);
        Assert.DoesNotContain("kreeft-header-strip", html, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Forgot_password_renders_plain_card_when_plain_skin_enabled()
    {
        await using var factory = new PlainPublicWebFactory();
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var html = await client.GetStringAsync("/wachtwoord-vergeten");

        Assert.Contains("cp-plain-card", html, StringComparison.Ordinal);
        Assert.Contains("Wachtwoord vergeten", html, StringComparison.Ordinal);
        Assert.DoesNotContain("au-card__mascot", html, StringComparison.Ordinal);
    }

    private sealed class PlainPublicWebFactory : WebApplicationFactory<Jobsy.Web.WebAssemblyMarker>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Development");
            builder.ConfigureAppConfiguration((_, config) =>
            {
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["CandidatePublic:PlainSkin"] = "true",
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

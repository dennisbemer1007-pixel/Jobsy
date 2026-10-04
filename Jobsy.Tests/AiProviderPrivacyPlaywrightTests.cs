using System.Net.Http.Headers;
using System.Text;
using Jobsy.Core.Features;
using Jobsy.Web.Hosting;
using Jobsy.Web.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Playwright;

namespace Jobsy.Tests;

/// <summary>
/// The privacy page is static SSR. The HTTP fact always runs in CI.
/// The browser fact soft-skips when Chromium cannot be installed.
/// </summary>
public class AiProviderPrivacyPageTests
{
    [Fact]
    public async Task Privacy_html_names_mistral_when_the_provider_is_mistral()
    {
        await using var factory = new MistralPrivacyWebFactory();
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("text/html"));

        var response = await client.GetAsync("/privacy");
        response.EnsureSuccessStatusCode();
        var html = await response.Content.ReadAsStringAsync();

        Assert.Contains("Mistral AI", html, StringComparison.Ordinal);
        Assert.Contains("Parijs", html, StringComparison.Ordinal);
        Assert.DoesNotContain("naar OpenAI", html, StringComparison.Ordinal);
        Assert.Contains("Frankrijk (Parijs)", html, StringComparison.Ordinal);
    }
}

[Collection("PlaywrightSmoke")]
public class AiProviderPrivacyPlaywrightTests
{
    [Fact]
    public async Task Browser_privacy_page_names_mistral_when_provider_is_mistral()
    {
        var exit = Microsoft.Playwright.Program.Main(["install", "chromium"]);
        if (exit != 0)
        {
            return;
        }

        await using var factory = new MistralPrivacyKestrelFactory();
        using var client = factory.CreateClient();
        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions { Headless = true });
        var page = await browser.NewPageAsync();
        var response = await page.GotoAsync(
            factory.ServerAddress + "/privacy",
            new PageGotoOptions { WaitUntil = WaitUntilState.DOMContentLoaded, Timeout = 90_000 });

        Assert.NotNull(response);
        Assert.Equal(200, response!.Status);
        var ai = await page.Locator("#ai").InnerTextAsync();
        var table = await page.Locator("#delen .pp-table__grid").InnerTextAsync();
        Assert.Contains("Mistral AI", ai, StringComparison.Ordinal);
        Assert.Contains("Parijs", ai, StringComparison.Ordinal);
        Assert.DoesNotContain("naar OpenAI", ai, StringComparison.Ordinal);
        Assert.Contains("Mistral AI", table, StringComparison.Ordinal);
        Assert.DoesNotContain("OpenAI", table, StringComparison.Ordinal);
    }
}

file sealed class MistralPrivacyWebFactory : WebApplicationFactory<Jobsy.Web.WebAssemblyMarker>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(MistralPrivacyHost.Settings);
        });
        builder.ConfigureTestServices(MistralPrivacyHost.ReplaceOutboundHttp);
    }
}

file sealed class MistralPrivacyKestrelFactory : WebApplicationFactory<Jobsy.Web.WebAssemblyMarker>
{
    private IHost? _kestrel;

    public string ServerAddress { get; private set; } = "";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(MistralPrivacyHost.Settings);
        });
        builder.ConfigureTestServices(MistralPrivacyHost.ReplaceOutboundHttp);
    }

    protected override IHost CreateHost(IHostBuilder builder)
    {
        var testHost = builder.Build();
        builder.ConfigureWebHost(webHostBuilder =>
        {
            webHostBuilder.UseKestrel();
            webHostBuilder.UseUrls("http://127.0.0.1:0");
        });
        _kestrel = builder.Build();
        _kestrel.Start();
        var addresses = _kestrel.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>();
        ServerAddress = addresses!.Addresses.First(a => a.StartsWith("http://", StringComparison.Ordinal));
        testHost.Start();
        return testHost;
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _kestrel?.Dispose();
        }

        base.Dispose(disposing);
    }
}

file static class MistralPrivacyHost
{
    public static Dictionary<string, string?> Settings { get; } = new()
    {
        ["ApiBaseUrl"] = "http://api.test/",
        ["CLOUDFLARE_ORIGIN_SECRET"] = "",
        ["JobsyAuth:Jwt:PrivateKeyPem"] = Jobsy.Core.Security.JobsyAccessToken.DevelopmentPrivateKeyPem,
        ["Ai:Provider"] = "Mistral",
        ["Mistral:ApiKey"] = "mistral-test-key",
        ["Mistral:Model"] = "mistral-small-latest",
        ["Mistral:BaseUrl"] = "https://api.mistral.ai/v1/"
    };

    public static void ReplaceOutboundHttp(IServiceCollection services)
    {
        services.RemoveAll<IVacancyMapApiForwarder>();
        services.AddSingleton<IVacancyMapApiForwarder>(_ => new NoopForwarder());
        services.RemoveAll<IFeatureFlags>();
        services.AddSingleton<IFeatureFlags>(_ => new AlwaysOnFeatureFlags());
        services.RemoveAll<IHttpClientFactory>();
        services.AddSingleton<IHttpClientFactory>(new ImmediateJsonFactory());
    }

    private sealed class NoopForwarder : IVacancyMapApiForwarder
    {
        public Task ForwardAsync(HttpContext http, string apiPath, CancellationToken ct)
            => Task.CompletedTask;
    }

    private sealed class AlwaysOnFeatureFlags : IFeatureFlags
    {
        public ValueTask<FeatureFlagSnapshot> GetAsync(CancellationToken cancellationToken = default)
            => ValueTask.FromResult(new FeatureFlagSnapshot(EmployersEnabled: true, CandidatePassportEnabled: false));

        public ValueTask<bool> IsEnabledAsync(PlatformFeature feature, CancellationToken cancellationToken = default)
            => ValueTask.FromResult(true);

        public void Invalidate()
        {
        }
    }

    private sealed class ImmediateJsonFactory : IHttpClientFactory
    {
        public HttpClient CreateClient(string name)
            => new(new ImmediateJsonHandler()) { BaseAddress = new Uri("http://api.test/") };
    }

    private sealed class ImmediateJsonHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
            => Task.FromResult(new HttpResponseMessage(System.Net.HttpStatusCode.OK)
            {
                Content = new StringContent("{}", Encoding.UTF8, "application/json")
            });
    }
}

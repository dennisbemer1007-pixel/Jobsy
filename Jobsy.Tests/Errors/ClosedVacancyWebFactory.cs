using Jobsy.Web.Auth;
using Jobsy.Web.Features;
using Jobsy.Web.Hosting;
using Jobsy.Web.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Jobsy.Tests.Errors;

/// <summary>
/// Web host for the closed-vacancy (410) page tests. Unlike <see cref="ErrorPagesWebFactory"/> /
/// <see cref="ForbiddenWebFactory"/> (every outbound API call throws), the vacancy detail page's
/// closed-state content is loaded as part of its normal page load (not the error layout), so this
/// forwards the Web client's API calls to a real in-process <see cref="ClosedVacancyApiFactory"/>
/// TestServer instead of faking the response.
/// </summary>
public sealed class ClosedVacancyWebFactory : WebApplicationFactory<Jobsy.Web.WebAssemblyMarker>
{
    public ClosedVacancyApiFactory Api { get; } = new();

    public HttpClient CreateHtmlClient()
    {
        var client = CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        client.DefaultRequestHeaders.Add("Accept", "text/html,application/xhtml+xml");
        return client;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        Api.EnsureSeeded();

        builder.UseEnvironment("Development");
        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ApiBaseUrl"] = "http://api.test/",
                ["CLOUDFLARE_ORIGIN_SECRET"] = "",
                ["Support:Email"] = "support@lobsy.nl",
                [ErrorPagesExtensions.ForceHandlerConfigKey] = "true",
                ["JobsyAuth:Jwt:PrivateKeyPem"] = Jobsy.Core.Security.JobsyAccessToken.DevelopmentPrivateKeyPem
            });
        });

        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<IVacancyMapApiForwarder>();
            services.AddSingleton<IVacancyMapApiForwarder>(new NoopForwarder());

            services.RemoveAll<IEmployersSwitch>();
            services.AddSingleton<IEmployersSwitch>(new FixedEmployersSwitch(true));

            // "http://api.test/" fails the API host's AllowedHosts check (TestServer runs the real
            // middleware pipeline); "http://localhost/" matches WebApplicationFactory.CreateClient's
            // own default base address and passes.
            services.RemoveAll<JobsyApiClient>();
            services.AddScoped(_ => new JobsyApiClient(
                new HttpClient(Api.Server.CreateHandler()) { BaseAddress = new Uri("http://localhost/") },
                new MeGetCache()));
            services.AddHttpClient(AuthApiClient.HttpClientName)
                .ConfigurePrimaryHttpMessageHandler(() => Api.Server.CreateHandler())
                .ConfigureHttpClient(c => c.BaseAddress = new Uri("http://localhost/"));
        });
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing)
        {
            Api.Dispose();
        }
    }

    private sealed class NoopForwarder : IVacancyMapApiForwarder
    {
        public Task ForwardAsync(HttpContext http, string apiPath, CancellationToken ct) => Task.CompletedTask;
    }

    private sealed class FixedEmployersSwitch(bool enabled) : IEmployersSwitch
    {
        public ValueTask<bool> IsEnabledAsync(CancellationToken ct = default) => ValueTask.FromResult(enabled);

        public ValueTask<LandingVariant> VariantAsync(CancellationToken ct = default)
            => ValueTask.FromResult(enabled ? LandingVariant.On : LandingVariant.Zw);
    }
}

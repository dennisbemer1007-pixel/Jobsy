using Jobsy.Web.Auth;
using Jobsy.Web.Features;
using Jobsy.Web.Hosting;
using Jobsy.Web.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

namespace Jobsy.Tests.Errors;

/// <summary>
/// Web host for the maintenance middleware (errors 05 §05.9). The polled
/// <see cref="MaintenanceState"/> is replaced by a fixed one and the poller is removed, so the
/// tests exercise the middleware instead of a timer. Every API call still throws: the
/// onderhoudspagina must render with the API down.
/// </summary>
public sealed class MaintenanceWebFactory : WebApplicationFactory<Jobsy.Web.WebAssemblyMarker>
{
    public bool MaintenanceEnabled { get; init; }

    public DateTime? ExpectedEndUtc { get; init; }

    public HttpClient CreateHtmlClient(string? culture = null)
    {
        var client = CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        client.DefaultRequestHeaders.Add("Accept", "text/html,application/xhtml+xml");
        if (culture is not null)
        {
            client.DefaultRequestHeaders.Add("Cookie", $"Jobsy.Culture={culture}");
        }

        return client;
    }

    public HttpClient CreateAdminClient()
    {
        var client = CreateHtmlClient();
        client.DefaultRequestHeaders.Add(TestAuthHandler.RoleHeader, Jobsy.Core.Authorization.JobsyRoles.Admin);
        client.DefaultRequestHeaders.Add(TestAuthHandler.EmailHeader, "beheer@lobsy.local");
        client.DefaultRequestHeaders.Add(TestAuthHandler.NameHeader, "Ada Admin");
        return client;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
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
            services.AddSingleton<IEmployersSwitch>(new FixedEmployersSwitch());

            services.RemoveAll<JobsyApiClient>();
            services.AddScoped(sp => new JobsyApiClient(
                new HttpClient(new ExplodingHandler()) { BaseAddress = new Uri("http://api.test/") },
                sp.GetRequiredService<MeGetCache>()));
            services.AddHttpClient(AuthApiClient.HttpClientName)
                .ConfigurePrimaryHttpMessageHandler(() => new ExplodingHandler())
                .ConfigureHttpClient(c => c.BaseAddress = new Uri("http://api.test/"));

            // No timer in tests: a fixed state instead of the 15 s poller.
            services.RemoveAll<MaintenanceState>();
            services.AddSingleton(FixedMaintenanceState.Create(MaintenanceEnabled, ExpectedEndUtc));
            services.RemoveAll<IHostedService>();

            services.AddAuthentication(TestAuthHandler.SchemeName)
                .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(TestAuthHandler.SchemeName, _ => { });
            services.PostConfigure<Microsoft.AspNetCore.Authentication.AuthenticationOptions>(options =>
            {
                options.DefaultScheme = TestAuthHandler.SchemeName;
                options.DefaultAuthenticateScheme = TestAuthHandler.SchemeName;
                options.DefaultChallengeScheme = TestAuthHandler.SchemeName;
            });
        });
    }

    private sealed class NoopForwarder : IVacancyMapApiForwarder
    {
        public Task ForwardAsync(HttpContext http, string apiPath, CancellationToken ct) => Task.CompletedTask;
    }

    private sealed class FixedEmployersSwitch : IEmployersSwitch
    {
        public ValueTask<bool> IsEnabledAsync(CancellationToken ct = default) => ValueTask.FromResult(true);

        public ValueTask<LandingVariant> VariantAsync(CancellationToken ct = default)
            => ValueTask.FromResult(LandingVariant.On);
    }

    private sealed class ExplodingHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
            => throw new HttpRequestException("The onderhoudspagina must not call the API.");
    }
}

/// <summary>A <see cref="MaintenanceState"/> that never polls.</summary>
public static class FixedMaintenanceState
{
    public static MaintenanceState Create(bool enabled, DateTime? expectedEndUtc)
    {
        var state = new MaintenanceState();
        state.Apply(enabled, expectedEndUtc);
        return state;
    }
}

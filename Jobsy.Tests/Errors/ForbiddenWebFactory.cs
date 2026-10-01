using System.Security.Claims;
using System.Text.Encodings.Web;
using Jobsy.Web.Auth;
using Jobsy.Web.Features;
using Jobsy.Web.Hosting;
using Jobsy.Web.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Jobsy.Tests.Errors;

/// <summary>
/// Web host for the 403 ("Geen toegang") tests. Signs in a <see cref="TestAuthHandler"/>
/// principal from a header so the suite can exercise role-protected pages without a real
/// cookie / antiforgery / demo-login round trip. Every outbound API call still throws
/// (errors pages never call the API or the database to render).
/// </summary>
public sealed class ForbiddenWebFactory : WebApplicationFactory<Jobsy.Web.WebAssemblyMarker>
{
    public bool EmployersEnabled { get; init; } = true;

    public HttpClient CreateHtmlClient()
    {
        var client = CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        client.DefaultRequestHeaders.Add("Accept", "text/html,application/xhtml+xml");
        return client;
    }

    /// <summary>A client authenticated as <paramref name="role"/> via the test header scheme.</summary>
    public HttpClient CreateSignedInClient(string role, string email = "kandidaat@lobsy.local", string name = "Kim Kandidaat")
    {
        var client = CreateHtmlClient();
        client.DefaultRequestHeaders.Add(TestAuthHandler.RoleHeader, role);
        client.DefaultRequestHeaders.Add(TestAuthHandler.EmailHeader, email);
        client.DefaultRequestHeaders.Add(TestAuthHandler.NameHeader, name);
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
            services.AddSingleton<IEmployersSwitch>(new FixedEmployersSwitch(EmployersEnabled));

            services.RemoveAll<JobsyApiClient>();
            services.AddScoped(sp => new JobsyApiClient(
                new HttpClient(new ExplodingHandler()) { BaseAddress = new Uri("http://api.test/") },
                sp.GetRequiredService<MeGetCache>()));
            services.AddHttpClient(AuthApiClient.HttpClientName)
                .ConfigurePrimaryHttpMessageHandler(() => new ExplodingHandler())
                .ConfigureHttpClient(c => c.BaseAddress = new Uri("http://api.test/"));

            // Make this host's default authentication scheme the test header handler so
            // AuthorizeRouteView's cascading AuthenticationState sees it for every request.
            services.AddAuthentication(TestAuthHandler.SchemeName)
                .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(TestAuthHandler.SchemeName, _ => { });
            services.PostConfigure<AuthenticationOptions>(options =>
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

    private sealed class FixedEmployersSwitch(bool enabled) : IEmployersSwitch
    {
        public ValueTask<bool> IsEnabledAsync(CancellationToken ct = default) => ValueTask.FromResult(enabled);

        public ValueTask<LandingVariant> VariantAsync(CancellationToken ct = default)
            => ValueTask.FromResult(enabled ? LandingVariant.On : LandingVariant.Zw);
    }

    private sealed class ExplodingHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
            => throw new HttpRequestException("Error pages must not call the API.");
    }
}

/// <summary>
/// Reads <see cref="RoleHeader"/>/<see cref="EmailHeader"/>/<see cref="NameHeader"/> request
/// headers and authenticates as that principal; anonymous when the role header is absent.
/// </summary>
public sealed class TestAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public const string SchemeName = "Test";
    public const string RoleHeader = "X-Test-Role";
    public const string EmailHeader = "X-Test-Email";
    public const string NameHeader = "X-Test-Name";

    public TestAuthHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder)
        : base(options, logger, encoder)
    {
    }

    /// <summary>No signed-in test principal: mirror the cookie scheme's redirect-to-login (302), not a bare 401.</summary>
    protected override Task HandleChallengeAsync(AuthenticationProperties properties)
    {
        var returnUrl = Uri.EscapeDataString(Request.Path + Request.QueryString);
        Response.Redirect($"/login?returnUrl={returnUrl}");
        return Task.CompletedTask;
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.TryGetValue(RoleHeader, out var role) || string.IsNullOrWhiteSpace(role))
        {
            return Task.FromResult(AuthenticateResult.NoResult());
        }

        var claims = new List<Claim>
        {
            new(ClaimTypes.Role, role.ToString()),
            new(Jobsy.Core.Authorization.JobsyClaimTypes.MfaVerified, "1"),
        };
        if (Request.Headers.TryGetValue(EmailHeader, out var email) && !string.IsNullOrWhiteSpace(email))
        {
            claims.Add(new Claim(ClaimTypes.Email, email.ToString()));
        }

        if (Request.Headers.TryGetValue(NameHeader, out var name) && !string.IsNullOrWhiteSpace(name))
        {
            claims.Add(new Claim(ClaimTypes.Name, name.ToString()));
        }

        var identity = new ClaimsIdentity(claims, SchemeName);
        var ticket = new AuthenticationTicket(new ClaimsPrincipal(identity), SchemeName);
        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}

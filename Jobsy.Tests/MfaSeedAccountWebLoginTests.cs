using System.Net;
using Jobsy.Core.Features;
using Jobsy.Web.Auth;
using Jobsy.Web.Features;
using Jobsy.Web.Hosting;
using Jobsy.Web.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Jobsy.Tests.TestSupport;

namespace Jobsy.Tests;

public class MfaSeedAccountWebLoginTests
{
    [Fact]
    public async Task Account_login_skips_mfa_redirect_for_jobsy_local_when_acceptatie_flag_on()
    {
        await using var factory = new MfaSeedAcceptatieWebFactory(requiresMfaFromApi: true, useDemoUserStore: false);
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        var token = await ReadAntiforgeryTokenAsync(client);
        var post = await client.PostAsync("/account/login", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = token,
            ["email"] = "ondernemer@jobsy.local",
            ["password"] = "Jobsy123!",
            ["returnUrl"] = "/home"
        }));

        Assert.Equal(HttpStatusCode.Redirect, post.StatusCode);
        var location = post.Headers.Location?.ToString() ?? "";
        Assert.DoesNotContain("/account/mfa/setup", location, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("/account/mfa", location, StringComparison.OrdinalIgnoreCase);
        Assert.True(
            location.Contains("/home", StringComparison.OrdinalIgnoreCase)
            || location.Contains("/werkgever", StringComparison.OrdinalIgnoreCase),
            location);
    }

    [Fact]
    public async Task DemoUser_store_login_skips_mfa_setup_for_jobsy_local_on_acceptatie()
    {
        await using var factory = new MfaSeedAcceptatieWebFactory(requiresMfaFromApi: false, useDemoUserStore: true);
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        var token = await ReadAntiforgeryTokenAsync(client);
        var post = await client.PostAsync("/account/login", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = token,
            ["email"] = "ondernemer@jobsy.local",
            ["password"] = "Jobsy123!",
            ["returnUrl"] = "/home"
        }));

        Assert.Equal(HttpStatusCode.Redirect, post.StatusCode);
        var location = post.Headers.Location?.ToString() ?? "";
        Assert.DoesNotContain("/account/mfa/setup", location, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Account_login_still_redirects_to_mfa_for_non_seed_email()
    {
        await using var factory = new MfaSeedAcceptatieWebFactory(
            requiresMfaFromApi: true,
            useDemoUserStore: false,
            skipMfaForSeedAccounts: true,
            loginEmail: "admin@test.local");
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        var token = await ReadAntiforgeryTokenAsync(client);
        var post = await client.PostAsync("/account/login", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = token,
            ["email"] = "admin@test.local",
            ["password"] = "SecretPass1!",
            ["returnUrl"] = "/home"
        }));

        Assert.Equal(HttpStatusCode.Redirect, post.StatusCode);
        var location = post.Headers.Location?.ToString() ?? "";
        Assert.True(
            location.Contains("/account/mfa/setup", StringComparison.OrdinalIgnoreCase),
            "Expected MFA setup redirect, got: " + location);
    }

    private static async Task<string> ReadAntiforgeryTokenAsync(HttpClient client)
    {
        var get = await client.GetAsync("/login");
        get.EnsureSuccessStatusCode();
        var html = await get.Content.ReadAsStringAsync();
        const string start = "name=\"__RequestVerificationToken\" value=\"";
        var i = html.IndexOf(start, StringComparison.Ordinal);
        Assert.True(i >= 0);
        i += start.Length;
        var j = html.IndexOf('"', i);
        Assert.True(j > i);
        return html[i..j];
    }

    private sealed class MfaSeedAcceptatieWebFactory : WebWebApplicationFactory
    {
        private readonly bool _requiresMfaFromApi;
        private readonly bool _useDemoUserStore;
        private readonly bool _skipMfaForSeedAccounts;
        private readonly string _loginEmail;

        public MfaSeedAcceptatieWebFactory(
            bool requiresMfaFromApi,
            bool useDemoUserStore,
            bool skipMfaForSeedAccounts = true,
            string loginEmail = "ondernemer@jobsy.local")
        {
            _requiresMfaFromApi = requiresMfaFromApi;
            _useDemoUserStore = useDemoUserStore;
            _skipMfaForSeedAccounts = skipMfaForSeedAccounts;
            _loginEmail = loginEmail;
        }

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment(Environments.Development);
            builder.ConfigureAppConfiguration((_, config) =>
            {
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Lobsy:DeploymentEnvironment"] = _skipMfaForSeedAccounts ? "Acceptatie" : "Production",
                    ["Auth:SkipMfaForSeedAccounts"] = _skipMfaForSeedAccounts ? "true" : "false",
                    ["ApiBaseUrl"] = "http://api.test/",
                    ["CLOUDFLARE_ORIGIN_SECRET"] = "",
                    ["JobsyAuth:DevelopmentAuthSecret"] = "test-secret",
                    ["JobsyAuth:ExternalProvisionSecret"] = "test-provision",
                    ["JobsyAuth:InternalClientIpSecret"] = "test-internal-ip",
                    ["JobsyAuth:Jwt:PrivateKeyPem"] = Jobsy.Core.Security.JobsyAccessToken.DevelopmentPrivateKeyPem,
                    ["JobsyAuth:AllowDevelopmentAuth"] = _useDemoUserStore ? "true" : "false",
                    ["Authentication:DemoUsers:0:Email"] = "ondernemer@jobsy.local",
                    ["Authentication:DemoUsers:0:Password"] = "Jobsy123!",
                    ["Authentication:DemoUsers:0:FullName"] = "Demo Filiaalmanager",
                    ["Authentication:DemoUsers:0:Role"] = "BranchManager",
                    ["Authentication:DemoUsers:0:CompanyId"] = "11111111-1111-1111-1111-111111111111",
                    ["Authentication:DemoUsers:0:UserId"] = "bbbbbbbb-1111-1111-1111-111111111111"
                });
            });
            builder.ConfigureTestServices(services =>
            {
                services.RemoveAll<IVacancyMapApiForwarder>();
                services.AddSingleton<IVacancyMapApiForwarder>(_ => new NoopForwarder());
                services.RemoveAll<IEmployersSwitch>();
                services.AddSingleton<IEmployersSwitch>(_ => new FixedEmployers(true));
                services.RemoveAll<Jobsy.Core.Features.IFeatureFlags>();
                services.AddSingleton<Jobsy.Core.Features.IFeatureFlags>(_ => new FixedFlags(true));

                services.AddHttpClient(AuthApiClient.HttpClientName)
                    .ConfigurePrimaryHttpMessageHandler(() => new SeedLoginHandler(_requiresMfaFromApi, _loginEmail))
                    .ConfigureHttpClient(c => c.BaseAddress = new Uri("http://api.test/"));
                services.RemoveAll<AuthApiClient>();
                services.AddSingleton<AuthApiClient>();
            });
        }

        private sealed class NoopForwarder : IVacancyMapApiForwarder
        {
            public Task ForwardAsync(HttpContext http, string apiPath, CancellationToken ct)
                => Task.CompletedTask;
        }

        private sealed class FixedEmployers(bool on) : IEmployersSwitch
        {
            public ValueTask<bool> IsEnabledAsync(CancellationToken ct = default) => ValueTask.FromResult(on);
            public ValueTask<LandingVariant> VariantAsync(CancellationToken ct = default)
                => ValueTask.FromResult(on ? LandingVariant.On : LandingVariant.Zw);
        }

        private sealed class FixedFlags(bool employers) : IFeatureFlags
        {
            public ValueTask<FeatureFlagSnapshot> GetAsync(CancellationToken ct = default)
                => ValueTask.FromResult(new FeatureFlagSnapshot(employers, false));

            public async ValueTask<bool> IsEnabledAsync(PlatformFeature feature, CancellationToken cancellationToken = default)
                => (await GetAsync(cancellationToken)).IsEnabled(feature);

            public void Invalidate()
            {
            }
        }

        private sealed class SeedLoginHandler(bool requiresMfa, string loginEmail) : HttpMessageHandler
        {
            protected override Task<HttpResponseMessage> SendAsync(
                HttpRequestMessage request,
                CancellationToken cancellationToken)
            {
                if (request.Method == HttpMethod.Post
                    && request.RequestUri?.AbsolutePath.EndsWith("/api/auth/local-login", StringComparison.OrdinalIgnoreCase) == true)
                {
                    var json = requiresMfa
                        ? $$"""
                          {
                            "email": "{{loginEmail}}",
                            "fullName": "Test Admin",
                            "role": "Admin",
                            "companyIds": [],
                            "requiresMfa": true,
                            "mfaEnrolled": false,
                            "mfaChallengeToken": "challenge-token",
                            "userId": "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"
                          }
                          """
                        : $$"""
                          {
                            "email": "{{loginEmail}}",
                            "fullName": "Demo Filiaalmanager",
                            "role": "BranchManager",
                            "companyId": "11111111-1111-1111-1111-111111111111",
                            "companyIds": [],
                            "sessionToken": "session",
                            "userId": "bbbbbbbb-1111-1111-1111-111111111111"
                          }
                          """;
                    return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
                    {
                        Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json")
                    });
                }

                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound));
            }
        }
    }
}

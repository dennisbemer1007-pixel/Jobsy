using System.Net;
using Jobsy.Web.Auth;
using Jobsy.Web.Features;
using Jobsy.Web.Hosting;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Jobsy.Tests;

public class LoginPageRenderTests
{
    [Fact]
    public async Task Login_is_static_page_without_dialog()
    {
        await using var factory = new LoginPageWebFactory(entra: true, google: true, employers: true);
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var html = await client.GetStringAsync("/login");

        Assert.DoesNotContain("login-modal__dialog", html, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("login-modal--compact", html, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("au-card", html, StringComparison.Ordinal);
        Assert.Contains("data-au-login", html, StringComparison.Ordinal);
        Assert.Contains("autocomplete=\"username\"", html, StringComparison.Ordinal);
        Assert.Contains("autocomplete=\"current-password\"", html, StringComparison.Ordinal);
        Assert.Contains("name=\"rememberDevice\"", html, StringComparison.Ordinal);
        Assert.DoesNotContain("name=\"rememberDevice\" value=\"true\" checked", html, StringComparison.Ordinal);
        Assert.DoesNotContain("is-disabled", html, StringComparison.Ordinal);
        Assert.Contains("Wachtwoord vergeten?", html, StringComparison.Ordinal);
        Assert.True(AuthFeatures.PasswordResetAvailable);
        Assert.Contains("Bedrijf registreren (KvK)", html, StringComparison.Ordinal);
        Assert.Contains("Maak gratis een account", html, StringComparison.Ordinal);
        Assert.Equal(1, CountOccurrences(html, "<h1"));
    }

    [Fact]
    public async Task Login_hides_unconfigured_providers()
    {
        await using var factory = new LoginPageWebFactory(entra: false, google: false, employers: true);
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var html = await client.GetStringAsync("/login");

        Assert.DoesNotContain("/account/external/entra", html, StringComparison.Ordinal);
        Assert.DoesNotContain("/account/external/google", html, StringComparison.Ordinal);
        Assert.DoesNotContain("au-divider", html, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Login_hides_google_in_admin_return_context()
    {
        await using var factory = new LoginPageWebFactory(entra: true, google: true, employers: true);
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var html = await client.GetStringAsync("/login?returnUrl=/admin");

        Assert.Contains("/account/external/entra", html, StringComparison.Ordinal);
        Assert.DoesNotContain("/account/external/google", html, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Login_hides_employer_register_when_employers_off()
    {
        await using var factory = new LoginPageWebFactory(entra: true, google: true, employers: false);
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var html = await client.GetStringAsync("/login");

        Assert.DoesNotContain("Bedrijf registreren (KvK)", html, StringComparison.Ordinal);
    }

    private static int CountOccurrences(string haystack, string needle)
    {
        var count = 0;
        var idx = 0;
        while ((idx = haystack.IndexOf(needle, idx, StringComparison.OrdinalIgnoreCase)) >= 0)
        {
            count++;
            idx += needle.Length;
        }

        return count;
    }
}

public class LoginStatesRenderTests
{
    [Theory]
    [InlineData("invalid", "au-alert--danger", "role=\"alert\"", "Dat klopt niet helemaal")]
    [InlineData("too-many", "au-alert--sun", "role=\"status\"", "Te veel pogingen")]
    [InlineData("unavailable", "au-alert--sky", "role=\"status\"", "Inloggen lukt nu even niet")]
    [InlineData("session-expired", "au-alert--sky", "role=\"status\"", "Je sessie is verlopen")]
    [InlineData("mfa-required", "au-alert--sky", "role=\"status\"", "Log opnieuw in")]
    [InlineData("admin-provider", "au-alert--sun", "role=\"status\"", "Beheerders loggen in")]
    public async Task Error_states_render_expected_tint_and_copy(
        string error, string tint, string role, string snippet)
    {
        await using var factory = new LoginPageWebFactory(entra: true, google: true, employers: true);
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var html = await client.GetStringAsync($"/login?error={error}");

        Assert.Contains(tint, html, StringComparison.Ordinal);
        Assert.Contains(role, html, StringComparison.Ordinal);
        Assert.Contains(snippet, html, StringComparison.Ordinal);
        Assert.Contains("au-form", html, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Locked_renders_pause_card_without_form()
    {
        await using var factory = new LoginPageWebFactory(entra: true, google: true, employers: true);
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var until = DateTimeOffset.UtcNow.AddMinutes(15).ToUnixTimeSeconds();
        var html = await client.GetStringAsync($"/login?error=locked&until={until}");

        Assert.Contains("Even pauze", html, StringComparison.Ordinal);
        Assert.Contains("au-pause", html, StringComparison.Ordinal);
        Assert.DoesNotContain("au-form", html, StringComparison.Ordinal);
        Assert.DoesNotContain("name=\"password\"", html, StringComparison.Ordinal);
        Assert.Contains("/account/external/entra", html, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Setup_done_renders_mint_status()
    {
        await using var factory = new LoginPageWebFactory(entra: false, google: false, employers: true);
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var html = await client.GetStringAsync("/login?setup=done");

        Assert.Contains("au-alert--mint", html, StringComparison.Ordinal);
        Assert.Contains("Je wachtwoord is opgeslagen", html, StringComparison.Ordinal);
    }
}

public class LoginHintCookieTests
{
    [Fact]
    public async Task Invalid_post_sets_hint_cookie_and_next_get_prefills_then_clears()
    {
        await using var factory = new LoginPageWebFactory(entra: false, google: false, employers: true);
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            HandleCookies = true
        });

        var get = await client.GetAsync("/login");
        Assert.Equal(HttpStatusCode.OK, get.StatusCode);
        var html = await get.Content.ReadAsStringAsync();
        var tokenField = Extract(html, "name=\"__RequestVerificationToken\" value=\"", "\"");
        Assert.False(string.IsNullOrEmpty(tokenField));

        var content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = tokenField!,
            ["email"] = "hint-user@example.nl",
            ["password"] = "WrongPass1!",
            ["returnUrl"] = "/home"
        });
        var post = await client.PostAsync("/account/login", content);
        Assert.Equal(HttpStatusCode.Redirect, post.StatusCode);
        var location = post.Headers.Location?.ToString() ?? "";
        Assert.Contains("error=invalid", location, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("hint-user", location, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("email=", location, StringComparison.OrdinalIgnoreCase);

        var again = await client.GetAsync("/login?error=invalid&returnUrl=%2Fhome");
        var againHtml = await again.Content.ReadAsStringAsync();
        Assert.Contains("value=\"hint-user@example.nl\"", againHtml, StringComparison.OrdinalIgnoreCase);

        var third = await client.GetAsync("/login?error=invalid&returnUrl=%2Fhome");
        var thirdHtml = await third.Content.ReadAsStringAsync();
        Assert.DoesNotContain("value=\"hint-user@example.nl\"", thirdHtml, StringComparison.OrdinalIgnoreCase);
    }

    private static string? Extract(string source, string start, string end)
    {
        var i = source.IndexOf(start, StringComparison.Ordinal);
        if (i < 0)
        {
            return null;
        }

        i += start.Length;
        var j = source.IndexOf(end, i, StringComparison.Ordinal);
        return j < 0 ? null : source[i..j];
    }
}

file sealed class LoginPageWebFactory : WebApplicationFactory<Jobsy.Web.WebAssemblyMarker>
{
    private readonly bool _entra;
    private readonly bool _google;
    private readonly bool _employers;

    public LoginPageWebFactory(bool entra, bool google, bool employers)
    {
        _entra = entra;
        _google = google;
        _employers = employers;
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
                ["JobsyAuth:Jwt:PrivateKeyPem"] = Jobsy.Core.Security.JobsyAccessToken.DevelopmentPrivateKeyPem
            });
        });
        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<IVacancyMapApiForwarder>();
            services.AddSingleton<IVacancyMapApiForwarder>(_ => new NoopForwarder());
            services.RemoveAll<IExternalAuthCredentialSource>();
            services.AddSingleton<IExternalAuthCredentialSource>(new FakeExternalAuth(_entra, _google));
            services.RemoveAll<IEmployersSwitch>();
            services.AddSingleton<IEmployersSwitch>(new FixedEmployers(_employers));
            services.RemoveAll<Jobsy.Core.Features.IFeatureFlags>();
            services.AddSingleton<Jobsy.Core.Features.IFeatureFlags>(new FixedFlags(_employers));

            services.AddHttpClient(AuthApiClient.HttpClientName)
                .ConfigurePrimaryHttpMessageHandler(() => new FailHandler())
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

    private sealed class FakeExternalAuth(bool entra, bool google) : IExternalAuthCredentialSource
    {
        public Task<bool> IsEntraConfiguredAsync(CancellationToken ct = default) => Task.FromResult(entra);
        public Task<bool> IsGoogleConfiguredAsync(CancellationToken ct = default) => Task.FromResult(google);
        public Task<ExternalOAuthCredentials?> GetEntraAsync(CancellationToken ct = default)
            => Task.FromResult<ExternalOAuthCredentials?>(null);
        public Task<ExternalOAuthCredentials?> GetGoogleAsync(CancellationToken ct = default)
            => Task.FromResult<ExternalOAuthCredentials?>(null);
    }

    private sealed class FixedEmployers(bool on) : IEmployersSwitch
    {
        public ValueTask<bool> IsEnabledAsync(CancellationToken ct = default) => ValueTask.FromResult(on);
        public ValueTask<LandingVariant> VariantAsync(CancellationToken ct = default)
            => ValueTask.FromResult(on ? LandingVariant.On : LandingVariant.Zw);
    }

    private sealed class FixedFlags(bool employers) : Jobsy.Core.Features.IFeatureFlags
    {
        public ValueTask<Jobsy.Core.Features.FeatureFlagSnapshot> GetAsync(CancellationToken ct = default)
            => ValueTask.FromResult(new Jobsy.Core.Features.FeatureFlagSnapshot(employers, false));

        public async ValueTask<bool> IsEnabledAsync(
            Jobsy.Core.Features.PlatformFeature feature,
            CancellationToken cancellationToken = default)
            => (await GetAsync(cancellationToken)).IsEnabled(feature);

        public void Invalidate()
        {
        }
    }

    private sealed class FailHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var body = """{"error":"invalid_credentials"}""";
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.Unauthorized)
            {
                Content = new StringContent(body, System.Text.Encoding.UTF8, "application/json")
            });
        }
    }
}

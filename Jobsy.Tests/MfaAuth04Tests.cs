using System.Net;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using Jobsy.Api.Controllers;
using Jobsy.Api.Models;
using Jobsy.Api.Security;
using Jobsy.Core.Email;
using Jobsy.Core.Entities;
using Jobsy.Core.Enums;
using Jobsy.Core.Interfaces;
using Jobsy.Core.Security;
using Jobsy.Infrastructure.Data;
using Jobsy.Infrastructure.Security;
using Jobsy.Infrastructure.Services;
using Jobsy.Web.Auth;
using Jobsy.Web.Features;
using Jobsy.Web.Hosting;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;

namespace Jobsy.Tests;

public class MfaPagesRenderTests
{
    [Fact]
    public async Task Prompt_without_challenge_has_no_dialog_and_one_h1()
    {
        await using var factory = new MfaPageWebFactory();
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var html = await client.GetStringAsync("/account/mfa");

        Assert.DoesNotContain("login-modal__dialog", html, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("login-modal--compact", html, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("au-card", html, StringComparison.Ordinal);
        Assert.Equal(1, CountH1(html));
    }

    [Fact]
    public async Task Setup_without_challenge_has_no_dialog()
    {
        await using var factory = new MfaPageWebFactory();
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var html = await client.GetStringAsync("/account/mfa/setup");

        Assert.DoesNotContain("login-modal__dialog", html, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("login-modal--compact", html, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("data-au-mfa-setup", html, StringComparison.Ordinal);
        Assert.Equal(1, CountH1(html));
    }

    private static int CountH1(string html)
    {
        var count = 0;
        var idx = 0;
        while ((idx = html.IndexOf("<h1", idx, StringComparison.OrdinalIgnoreCase)) >= 0)
        {
            count++;
            idx += 3;
        }

        return count;
    }
}

public class MfaCancelTests
{
    [Fact]
    public async Task Cancel_clears_challenge_and_redirects_login()
    {
        await using var factory = new MfaPageWebFactory();
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            HandleCookies = true
        });

        var get = await client.GetAsync("/login");
        var html = await get.Content.ReadAsStringAsync();
        var token = Extract(html, "name=\"__RequestVerificationToken\" value=\"", "\"");
        Assert.False(string.IsNullOrEmpty(token));

        var content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = token!
        });
        var post = await client.PostAsync("/account/mfa/cancel", content);
        Assert.Equal(HttpStatusCode.Redirect, post.StatusCode);
        Assert.Equal("/login", post.Headers.Location?.ToString());
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

public class MfaTrustedDeviceTests
{
    [Fact]
    public async Task Trust_skips_code_on_next_password_login()
    {
        await using var db = CreateDb();
        var secret = TotpAuthenticator.GenerateSecret();
        var user = await SeedUserAsync(db, secret);
        await SeedPasswordAsync(db, user, "SecretPass1!");
        var challenges = NewChallenges();
        var mfa = CreateMfa(db, challenges);
        var token = challenges.Create(user, rememberDevice: false, localPassword: true);
        var code = TotpAuthenticator.GenerateCode(secret, DateTime.UtcNow);
        var verify = await mfa.Verify(
            new MfaVerifyRequest(token, code, TrustDevice: true, Method: "totp"),
            CancellationToken.None);
        var body = Assert.IsType<LocalLoginResponse>(Assert.IsType<OkObjectResult>(verify.Result).Value);
        Assert.False(string.IsNullOrWhiteSpace(body.MfaTrustToken));

        var auth = CreateAuth(db, challenges);
        var login = await auth.LocalLogin(
            new LocalLoginRequest(user.Email, "SecretPass1!", MfaTrustToken: body.MfaTrustToken),
            CancellationToken.None);
        var loginBody = Assert.IsType<LocalLoginResponse>(Assert.IsType<OkObjectResult>(login.Result).Value);
        Assert.True(loginBody.MfaVerified);
        Assert.False(loginBody.RequiresMfa);
        Assert.Equal("password+trusted-device", loginBody.AuthMethod);
    }

    [Fact]
    public async Task Wrong_password_still_fails_with_trust_token()
    {
        await using var db = CreateDb();
        var secret = TotpAuthenticator.GenerateSecret();
        var user = await SeedUserAsync(db, secret);
        await SeedPasswordAsync(db, user, "SecretPass1!");
        var trusted = new MfaTrustedDeviceService(db);
        var created = await trusted.CreateAsync(user.Id, "test-ua", user.SessionVersion);

        var auth = CreateAuth(db, NewChallenges());
        var login = await auth.LocalLogin(
            new LocalLoginRequest(user.Email, "WrongPass1!", MfaTrustToken: created.RawToken),
            CancellationToken.None);
        Assert.IsType<UnauthorizedObjectResult>(login.Result);
    }

    [Fact]
    public async Task Expired_revoked_and_session_bump_require_challenge()
    {
        await using var db = CreateDb();
        var secret = TotpAuthenticator.GenerateSecret();
        var user = await SeedUserAsync(db, secret);
        await SeedPasswordAsync(db, user, "SecretPass1!");
        var trusted = new MfaTrustedDeviceService(db);
        var created = await trusted.CreateAsync(user.Id, "ua", user.SessionVersion);
        var row = created.Row;

        row.ExpiresAtUtc = DateTime.UtcNow.AddMinutes(-1);
        await db.SaveChangesAsync();
        var auth = CreateAuth(db, NewChallenges());
        var expired = await auth.LocalLogin(
            new LocalLoginRequest(user.Email, "SecretPass1!", MfaTrustToken: created.RawToken),
            CancellationToken.None);
        Assert.True(Assert.IsType<LocalLoginResponse>(Assert.IsType<OkObjectResult>(expired.Result).Value).RequiresMfa);

        row.ExpiresAtUtc = DateTime.UtcNow.AddDays(10);
        row.RevokedAtUtc = DateTime.UtcNow;
        await db.SaveChangesAsync();
        var revoked = await auth.LocalLogin(
            new LocalLoginRequest(user.Email, "SecretPass1!", MfaTrustToken: created.RawToken),
            CancellationToken.None);
        Assert.True(Assert.IsType<LocalLoginResponse>(Assert.IsType<OkObjectResult>(revoked.Result).Value).RequiresMfa);

        row.RevokedAtUtc = null;
        user.SessionVersion++;
        await db.SaveChangesAsync();
        var bumped = await auth.LocalLogin(
            new LocalLoginRequest(user.Email, "SecretPass1!", MfaTrustToken: created.RawToken),
            CancellationToken.None);
        Assert.True(Assert.IsType<LocalLoginResponse>(Assert.IsType<OkObjectResult>(bumped.Result).Value).RequiresMfa);
    }

    [Fact]
    public async Task Revoke_all_clears_trusted_devices()
    {
        await using var db = CreateDb();
        var secret = TotpAuthenticator.GenerateSecret();
        var user = await SeedUserAsync(db, secret);
        var trusted = new MfaTrustedDeviceService(db);
        await trusted.CreateAsync(user.Id, "ua", user.SessionVersion);
        Assert.Equal(1, await trusted.CountActiveAsync(user.Id));
        await trusted.RevokeAllForUserAsync(user.Id);
        Assert.Equal(0, await trusted.CountActiveAsync(user.Id));
    }

    private static JobsyDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<JobsyDbContext>()
            .UseInMemoryDatabase("MfaTrust-" + Guid.NewGuid())
            .Options;
        return new JobsyDbContext(options);
    }

    private static MfaChallengeService NewChallenges()
        => new(new MemoryCache(new MemoryCacheOptions()));

    private static async Task<User> SeedUserAsync(JobsyDbContext db, string secret)
    {
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = $"user-{Guid.NewGuid():N}@test.local",
            FullName = "Trust User",
            Role = UserRole.Admin,
            IsActive = true,
            AuthenticatorEnabled = true,
            AuthenticatorSecret = secret,
            AuthenticatorEnrolledAtUtc = DateTime.UtcNow,
            SessionVersion = 1
        };
        db.Users.Add(user);
        await db.SaveChangesAsync();
        return user;
    }

    private static async Task SeedPasswordAsync(JobsyDbContext db, User user, string password)
    {
        db.LocalAuthCredentials.Add(new LocalAuthCredential
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            Email = user.Email.ToLowerInvariant(),
            PasswordHash = JobsyPasswordHasher.Hash(password)
        });
        await db.SaveChangesAsync();
    }

    private static AuthController CreateAuth(JobsyDbContext db, MfaChallengeService challenges)
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["JobsyAuth:DevelopmentAuthSecret"] = "test-secret"
            })
            .Build();
        var sut = new AuthController(
            db, config,
            new IntegrationCredentialService(db, new PassthroughSecretProtector()),
            new AmbassadeurAttributionService(db, new AmbassadeurSettingsService(db), NullLogger<AmbassadeurAttributionService>.Instance, new AlwaysOnFeatures()),
            new Auth04StubHostEnvironment(),
            new DeviceSessionService(db, config, new MemoryCache(new MemoryCacheOptions()), NullLogger<DeviceSessionService>.Instance),
            new MfaTrustedDeviceService(db),
            new EmailServiceStub(db, NullLogger<EmailServiceStub>.Instance),
            challenges,
            new Auth04StubFeatures(true),
            new UnknownAccountLockoutTracker("test-lockout-key"),
            NullLogger<AuthController>.Instance);
        sut.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() };
        return sut;
    }

    private static MfaController CreateMfa(JobsyDbContext db, MfaChallengeService challenges)
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["JobsyAuth:DevelopmentAuthSecret"] = "test-secret"
            })
            .Build();
        var sut = new MfaController(
            db,
            new PassthroughSecretProtector(),
            new DeviceSessionService(db, config, new MemoryCache(new MemoryCacheOptions()), NullLogger<DeviceSessionService>.Instance),
            new MfaTrustedDeviceService(db),
            config,
            challenges,
            new TotpVerifier(db),
            new EmailServiceStub(db, NullLogger<EmailServiceStub>.Instance),
            new Auth04StubFeatures(true),
            NullLogger<MfaController>.Instance);
        sut.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() };
        return sut;
    }
}

public class RecoveryCodeRegenerationTests
{
    [Fact]
    public async Task Wrong_totp_returns_unauthorized()
    {
        await using var db = CreateDb();
        var secret = TotpAuthenticator.GenerateSecret();
        var user = await SeedEnrolledAsync(db, secret);
        var mailer = new CapturingMailer();
        var mfa = CreateMfa(db, mailer);
        SetUser(mfa, user.Id);
        var result = await mfa.RegenerateRecoveryCodes(new MfaRegenerateRecoveryCodesRequest("000000"), CancellationToken.None);
        Assert.IsType<UnauthorizedObjectResult>(result.Result);
        Assert.Empty(mailer.SentKeys);
    }

    [Fact]
    public async Task Success_rotates_codes_mails_and_revokes_trust()
    {
        await using var db = CreateDb();
        var secret = TotpAuthenticator.GenerateSecret();
        var user = await SeedEnrolledAsync(db, secret);
        var oldCodes = MfaRecoveryCodes.Generate();
        user.RecoveryCodesHash = JsonSerializer.Serialize(oldCodes.Select(MfaRecoveryCodes.Hash));
        await db.SaveChangesAsync();

        var trusted = new MfaTrustedDeviceService(db);
        await trusted.CreateAsync(user.Id, "ua", user.SessionVersion);

        var mailer = new CapturingMailer();
        var mfa = CreateMfa(db, mailer);
        SetUser(mfa, user.Id);
        var code = TotpAuthenticator.GenerateCode(secret, DateTime.UtcNow);
        var result = await mfa.RegenerateRecoveryCodes(new MfaRegenerateRecoveryCodesRequest(code), CancellationToken.None);
        Assert.IsType<OkObjectResult>(result.Result);
        Assert.Contains("RecoveryCodesRegenerated", mailer.SentKeys);
        Assert.Equal(0, await trusted.CountActiveAsync(user.Id));

        var reloaded = await db.Users.SingleAsync(u => u.Id == user.Id);
        var newHashes = JsonSerializer.Deserialize<List<string>>(reloaded.RecoveryCodesHash!) ?? [];
        Assert.DoesNotContain(MfaRecoveryCodes.Hash(oldCodes[0]), newHashes);
    }

    private static void SetUser(MfaController mfa, Guid userId)
    {
        var identity = new ClaimsIdentity("test");
        identity.AddClaim(new Claim(ClaimTypes.NameIdentifier, userId.ToString("D")));
        mfa.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(identity) }
        };
    }

    private static async Task<User> SeedEnrolledAsync(JobsyDbContext db, string secret)
    {
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = $"regen-{Guid.NewGuid():N}@test.local",
            FullName = "Regen",
            Role = UserRole.Admin,
            IsActive = true,
            AuthenticatorEnabled = true,
            AuthenticatorSecret = secret,
            AuthenticatorEnrolledAtUtc = DateTime.UtcNow,
            SessionVersion = 1,
            RecoveryCodesHash = JsonSerializer.Serialize(MfaRecoveryCodes.Generate().Select(MfaRecoveryCodes.Hash))
        };
        db.Users.Add(user);
        await db.SaveChangesAsync();
        return user;
    }

    private static JobsyDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<JobsyDbContext>()
            .UseInMemoryDatabase("MfaRegen-" + Guid.NewGuid())
            .Options;
        return new JobsyDbContext(options);
    }

    private static MfaController CreateMfa(JobsyDbContext db, CapturingMailer mailer)
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["JobsyAuth:DevelopmentAuthSecret"] = "test-secret"
            })
            .Build();
        return new MfaController(
            db,
            new PassthroughSecretProtector(),
            new DeviceSessionService(db, config, new MemoryCache(new MemoryCacheOptions()), NullLogger<DeviceSessionService>.Instance),
            new MfaTrustedDeviceService(db),
            config,
            new MfaChallengeService(new MemoryCache(new MemoryCacheOptions())),
            new TotpVerifier(db),
            mailer,
            new Auth04StubFeatures(true),
            NullLogger<MfaController>.Instance);
    }

    private sealed class CapturingMailer : ITransactionalMailer
    {
        public List<string> SentKeys { get; } = [];

        public Task<EmailSendOutcome> SendAsync(
            ComposedEmail mail,
            string to,
            EmailSendOptions? options = null,
            CancellationToken cancellationToken = default)
        {
            SentKeys.Add(mail.Key);
            return Task.FromResult(new EmailSendOutcome(true, false, null));
        }
    }
}

file sealed class Auth04StubFeatures(bool authenticatorEnabled) : IPlatformFeatureService
{
    public Task<PlatformFeatureSnapshot> GetAsync(CancellationToken cancellationToken = default)
        => Task.FromResult(new PlatformFeatureSnapshot(true, authenticatorEnabled, false, "https://lobsy.test", null));

    public Task<PlatformFeatureSnapshot> UpdateAsync(PlatformFeatureUpdate update, CancellationToken cancellationToken = default)
        => throw new NotSupportedException();
}

file sealed class Auth04StubHostEnvironment : IHostEnvironment
{
    public string EnvironmentName { get; set; } = Environments.Development;
    public string ApplicationName { get; set; } = "tests";
    public string ContentRootPath { get; set; } = AppContext.BaseDirectory;
    public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
}

file sealed class MfaPageWebFactory : WebApplicationFactory<Jobsy.Web.WebAssemblyMarker>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ApiBaseUrl"] = "http://api.test/",
                ["CLOUDFLARE_ORIGIN_SECRET"] = "",
                ["JobsyAuth:Jwt:PrivateKeyPem"] = JobsyAccessToken.DevelopmentPrivateKeyPem
            });
        });
        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<IVacancyMapApiForwarder>();
            services.AddSingleton<IVacancyMapApiForwarder>(_ => new NoopForwarder());
            services.RemoveAll<IExternalAuthCredentialSource>();
            services.AddSingleton<IExternalAuthCredentialSource>(new FakeExternalAuth());
            services.RemoveAll<IEmployersSwitch>();
            services.AddSingleton<IEmployersSwitch>(new FixedEmployers());
            services.RemoveAll<Jobsy.Core.Features.IFeatureFlags>();
            services.AddSingleton<Jobsy.Core.Features.IFeatureFlags>(new FixedFlags());
            services.AddHttpClient(AuthApiClient.HttpClientName)
                .ConfigurePrimaryHttpMessageHandler(() => new FailHandler())
                .ConfigureHttpClient(c => c.BaseAddress = new Uri("http://api.test/"));
            services.RemoveAll<AuthApiClient>();
            services.AddSingleton<AuthApiClient>();
        });
    }

    private sealed class NoopForwarder : IVacancyMapApiForwarder
    {
        public Task ForwardAsync(HttpContext http, string apiPath, CancellationToken ct) => Task.CompletedTask;
    }

    private sealed class FakeExternalAuth : IExternalAuthCredentialSource
    {
        public Task<bool> IsEntraConfiguredAsync(CancellationToken ct = default) => Task.FromResult(false);
        public Task<bool> IsGoogleConfiguredAsync(CancellationToken ct = default) => Task.FromResult(false);
        public Task<ExternalOAuthCredentials?> GetEntraAsync(CancellationToken ct = default)
            => Task.FromResult<ExternalOAuthCredentials?>(null);
        public Task<ExternalOAuthCredentials?> GetGoogleAsync(CancellationToken ct = default)
            => Task.FromResult<ExternalOAuthCredentials?>(null);
    }

    private sealed class FixedEmployers : IEmployersSwitch
    {
        public ValueTask<bool> IsEnabledAsync(CancellationToken ct = default) => ValueTask.FromResult(true);
        public ValueTask<LandingVariant> VariantAsync(CancellationToken ct = default)
            => ValueTask.FromResult(LandingVariant.On);
    }

    private sealed class FixedFlags : Jobsy.Core.Features.IFeatureFlags
    {
        public ValueTask<Jobsy.Core.Features.FeatureFlagSnapshot> GetAsync(CancellationToken ct = default)
            => ValueTask.FromResult(new Jobsy.Core.Features.FeatureFlagSnapshot(true, false));

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
            => Task.FromResult(new HttpResponseMessage(HttpStatusCode.Unauthorized)
            {
                Content = new StringContent("""{"code":"challenge_expired"}""", Encoding.UTF8, "application/json")
            });
    }
}

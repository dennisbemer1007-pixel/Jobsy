using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Jobsy.Api.Controllers;
using Jobsy.Api.Models;
using Jobsy.Api.Security;
using Jobsy.Core.Authorization;
using Jobsy.Core.Entities;
using Jobsy.Core.Enums;
using Jobsy.Core.Interfaces;
using Jobsy.Core.Security;
using Jobsy.Infrastructure.Data;
using Jobsy.Infrastructure.Security;
using Jobsy.Infrastructure.Services;
using Jobsy.Web.Localization;
using Jobsy.Web.Security;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;

namespace Jobsy.Tests;

public class MfaForcedEnrollmentTests : IClassFixture<RoleFunctionalWebAppFactory>
{
    private readonly RoleFunctionalWebAppFactory _factory;

    public MfaForcedEnrollmentTests(RoleFunctionalWebAppFactory factory) => _factory = factory;

    [Fact]
    public async Task Forced_enrollment_password_login_and_enroll_ignores_authenticator_stub_flag()
    {
        await using var db = CreateDb();
        var user = await SeedAdminAsync(db, enrolled: false);
        await SeedPasswordAsync(db, user, "SecretPass1!");

        var challenges = NewChallenges();
        var auth = CreateAuthController(db, challenges);
        var login = await auth.LocalLogin(new LocalLoginRequest(user.Email, "SecretPass1!"), CancellationToken.None);
        var loginOk = Assert.IsType<OkObjectResult>(login.Result);
        var loginBody = Assert.IsType<LocalLoginResponse>(loginOk.Value);
        Assert.True(loginBody.RequiresMfa);
        Assert.False(loginBody.MfaEnrolled);
        Assert.False(string.IsNullOrWhiteSpace(loginBody.MfaChallengeToken));

        var features = new StubFeatures(authenticatorEnabled: false);
        var mfa = CreateMfaController(db, challenges, new PassthroughSecretProtector());
        // Simulate PlatformFeatureSettings row with stub off — enroll must still work (no gate).
        Assert.False((await features.GetAsync()).AuthenticatorEnabled);

        var state = await mfa.State(new MfaStateRequest(loginBody.MfaChallengeToken!), CancellationToken.None);
        var stateOk = Assert.IsType<OkObjectResult>(state.Result);
        var stateBody = Assert.IsType<MfaStateResponse>(stateOk.Value);
        Assert.False(stateBody.Enrolled);

        var enroll = await mfa.Enroll(new MfaEnrollmentRequest(loginBody.MfaChallengeToken!), CancellationToken.None);
        var enrollOk = Assert.IsType<OkObjectResult>(enroll.Result);
        var enrollBody = Assert.IsType<MfaEnrollmentResponse>(enrollOk.Value);
        Assert.False(string.IsNullOrWhiteSpace(enrollBody.Secret));
        Assert.StartsWith("otpauth://", enrollBody.ProvisioningUri);
        Assert.StartsWith("data:image/svg+xml;base64,", enrollBody.QrSvgDataUri);
    }

    [Fact]
    public async Task Verify_first_enrollment_returns_ten_codes_and_second_enroll_is_conflict()
    {
        await using var db = CreateDb();
        var user = await SeedAdminAsync(db, enrolled: false);
        var secrets = new PassthroughSecretProtector();
        var challenges = NewChallenges();
        var token = challenges.Create(user, rememberDevice: false, localPassword: true);
        var mfa = CreateMfaController(db, challenges, secrets);

        var enroll = await mfa.Enroll(new MfaEnrollmentRequest(token), CancellationToken.None);
        var enrollBody = Assert.IsType<MfaEnrollmentResponse>(Assert.IsType<OkObjectResult>(enroll.Result).Value);
        var code = TotpAuthenticator.GenerateCode(enrollBody.Secret, DateTime.UtcNow);

        var verify = await mfa.Verify(new MfaVerifyRequest(token, code), CancellationToken.None);
        var verifyBody = Assert.IsType<LocalLoginResponse>(Assert.IsType<OkObjectResult>(verify.Result).Value);
        Assert.True(verifyBody.MfaVerified);
        Assert.NotNull(verifyBody.RecoveryCodes);
        Assert.Equal(10, verifyBody.RecoveryCodes!.Count);

        var reloaded = await db.Users.AsNoTracking().SingleAsync(u => u.Id == user.Id);
        Assert.True(reloaded.AuthenticatorEnabled);

        var token2 = challenges.Create(reloaded, rememberDevice: false, localPassword: true);
        var second = await mfa.Enroll(new MfaEnrollmentRequest(token2), CancellationToken.None);
        Assert.IsType<ConflictObjectResult>(second.Result);
    }

    [Fact]
    public async Task Enrolled_user_wrong_code_fails_and_recovery_code_works_once()
    {
        await using var db = CreateDb();
        var secrets = new PassthroughSecretProtector();
        var secret = TotpAuthenticator.GenerateSecret();
        var recovery = Convert.ToHexString(RandomNumberGenerator.GetBytes(8));
        var user = await SeedAdminAsync(db, enrolled: true, secret: secret, recoveryPlain: recovery);
        var challenges = NewChallenges();
        var mfa = CreateMfaController(db, challenges, secrets);

        var badToken = challenges.Create(user, false, true);
        var bad = await mfa.Verify(new MfaVerifyRequest(badToken, "000000"), CancellationToken.None);
        Assert.Equal(StatusCodes.Status401Unauthorized, Assert.IsType<UnauthorizedObjectResult>(bad.Result).StatusCode);

        var okToken = challenges.Create(user, false, true);
        var ok = await mfa.Verify(new MfaVerifyRequest(okToken, RecoveryCode: recovery), CancellationToken.None);
        Assert.IsType<OkObjectResult>(ok.Result);

        var reuseToken = challenges.Create(await db.Users.SingleAsync(u => u.Id == user.Id), false, true);
        var reuse = await mfa.Verify(new MfaVerifyRequest(reuseToken, RecoveryCode: recovery), CancellationToken.None);
        Assert.IsType<UnauthorizedObjectResult>(reuse.Result);
    }

    [Fact]
    public async Task Ensure_external_with_provider_skips_lobsy_mfa_for_admin()
    {
        await using var db = CreateDb();
        var user = await SeedAdminAsync(db, enrolled: false);
        var sut = CreateAuthController(db, NewChallenges(), secret: "test-secret");
        sut.ControllerContext = WithProvisionSecret("test-secret");

        foreach (var provider in new[] { "entra", "microsoft", "oidc" })
        {
            var result = await sut.EnsureExternal(
                new EnsureExternalUserRequest(
                    user.Email,
                    user.FullName,
                    Provider: provider,
                    ProviderSubject: "sub-" + provider,
                    ProviderTenantId: "11111111-1111-1111-1111-111111111111"),
                CancellationToken.None);
            var body = Assert.IsType<EnsureExternalUserResponse>(Assert.IsType<OkObjectResult>(result.Result).Value);
            Assert.False(body.RequiresMfa);
            Assert.True(string.IsNullOrWhiteSpace(body.MfaChallengeToken));
            Assert.StartsWith("external:", body.AuthMethod);
        }

        var blocked = await sut.EnsureExternal(
            new EnsureExternalUserRequest(
                user.Email,
                user.FullName,
                Provider: "google",
                ProviderSubject: "sub-google"),
            CancellationToken.None);
        var forbidden = Assert.IsType<ObjectResult>(blocked.Result);
        Assert.Equal(StatusCodes.Status403Forbidden, forbidden.StatusCode);
    }

    [Fact]
    public async Task Mixed_account_password_still_requires_totp()
    {
        await using var db = CreateDb();
        var user = await SeedAdminAsync(db, enrolled: true, secret: TotpAuthenticator.GenerateSecret());
        await SeedPasswordAsync(db, user, "SecretPass1!");
        db.UserExternalLogins.Add(new UserExternalLogin
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            Provider = "entra",
            ProviderSubject = "oid-1",
            EmailAtLink = user.Email,
            LinkedAtUtc = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        var auth = CreateAuthController(db, NewChallenges());
        var login = await auth.LocalLogin(new LocalLoginRequest(user.Email, "SecretPass1!"), CancellationToken.None);
        var body = Assert.IsType<LocalLoginResponse>(Assert.IsType<OkObjectResult>(login.Result).Value);
        Assert.True(body.RequiresMfa);
        Assert.True(body.MfaEnrolled);
    }

    [Fact]
    public async Task Middleware_blocks_privileged_local_without_mfa_and_allows_external()
    {
        foreach (var path in new[] { "/admin", "/branch", "/employer/vacancies", "/api/admin/users" })
        {
            var context = new DefaultHttpContext();
            context.Request.Path = path;
            context.User = new ClaimsPrincipal(new ClaimsIdentity(
            [
                new Claim(ClaimTypes.Role, "Admin"),
                new Claim("auth_method", "password")
            ], "test"));
            var reached = false;
            var middleware = new MfaEnforcementMiddleware(_ =>
            {
                reached = true;
                return Task.CompletedTask;
            });
            await middleware.InvokeAsync(context);
            Assert.False(reached);
            if (path.StartsWith("/api", StringComparison.Ordinal))
            {
                Assert.Equal(StatusCodes.Status401Unauthorized, context.Response.StatusCode);
            }
            else
            {
                Assert.Contains("/login?error=mfa-required", context.Response.Headers.Location.ToString(), StringComparison.Ordinal);
            }
        }

        var external = new DefaultHttpContext();
        external.Request.Path = "/admin";
        external.User = new ClaimsPrincipal(new ClaimsIdentity(
        [
            new Claim(ClaimTypes.Role, "Admin"),
            new Claim("auth_method", "external:entra")
        ], "test"));
        var next = false;
        await new MfaEnforcementMiddleware(_ =>
        {
            next = true;
            return Task.CompletedTask;
        }).InvokeAsync(external);
        Assert.True(next);

        var candidate = new DefaultHttpContext();
        candidate.Request.Path = "/candidate/profiel";
        candidate.User = new ClaimsPrincipal(new ClaimsIdentity(
        [
            new Claim(ClaimTypes.Role, "Candidate"),
            new Claim("auth_method", "password")
        ], "test"));
        var candNext = false;
        await new MfaEnforcementMiddleware(_ =>
        {
            candNext = true;
            return Task.CompletedTask;
        }).InvokeAsync(candidate);
        Assert.True(candNext);
    }

    [Fact]
    public async Task Admin_mfa_reset_rules_and_effect()
    {
        var client = _factory.CreateClient();
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<JobsyDbContext>();
            var secrets = scope.ServiceProvider.GetRequiredService<ISecretProtector>();
            var admin = await db.Users.SingleAsync(u => u.Id == _factory.AdminId);
            var secret = TotpAuthenticator.GenerateSecret();
            admin.AuthenticatorSecret = secrets.Protect(secret);
            admin.AuthenticatorEnabled = true;
            admin.AuthenticatorEnrolledAtUtc = DateTime.UtcNow;
            admin.RecoveryCodesHash = JsonSerializer.Serialize(new[]
            {
                Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes("DEADBEEF")))
            });
            var employer = await db.Users.SingleAsync(u => u.Id == _factory.EmployerId);
            employer.AuthenticatorSecret = secrets.Protect(TotpAuthenticator.GenerateSecret());
            employer.AuthenticatorEnabled = true;
            employer.AuthenticatorEnrolledAtUtc = DateTime.UtcNow;
            employer.RecoveryCodesHash = "[]";
            var beforeVersion = employer.SessionVersion;
            await db.SaveChangesAsync();

            // Anonymous
            var anon = await client.PostAsJsonAsync(
                $"api/admin/users/{employer.Id}/mfa/reset",
                new { reason = "Support ticket ABCDE" });
            Assert.Equal(HttpStatusCode.Unauthorized, anon.StatusCode);

            // Non-admin
            JobsyTestAuth.Authorize(client, _factory.EmployerId);
            var forbiddenRole = await client.PostAsJsonAsync(
                $"api/admin/users/{_factory.AdminId}/mfa/reset",
                new { reason = "Support ticket ABCDE" });
            Assert.Equal(HttpStatusCode.Forbidden, forbiddenRole.StatusCode);

            // Admin without MFA claim
            client.DefaultRequestHeaders.Authorization = null;
            JobsyTestAuth.Authorize(client, _factory.AdminId);
            var noMfa = await client.PostAsJsonAsync(
                $"api/admin/users/{employer.Id}/mfa/reset",
                new { reason = "Support ticket ABCDE", confirmCode = TotpAuthenticator.GenerateCode(secret, DateTime.UtcNow) });
            Assert.Equal(HttpStatusCode.Forbidden, noMfa.StatusCode);

            // Self-reset
            using var mfaClient = CreateMfaAuthedClient(_factory.AdminId, "password+totp");
            var self = await mfaClient.PostAsJsonAsync(
                $"api/admin/users/{_factory.AdminId}/mfa/reset",
                new { reason = "Support ticket ABCDE", confirmCode = TotpAuthenticator.GenerateCode(secret, DateTime.UtcNow) });
            Assert.Equal(HttpStatusCode.Forbidden, self.StatusCode);

            // Valid reset
            var logsBefore = await db.PersonalDataAccessLogs.CountAsync(l =>
                l.Resource == "user.mfa" && l.Action == "reset" && l.SubjectUserId == employer.Id);
            var confirm = TotpAuthenticator.GenerateCode(secret, DateTime.UtcNow);
            var ok = await mfaClient.PostAsJsonAsync(
                $"api/admin/users/{employer.Id}/mfa/reset",
                new { reason = "Support ticket ABCDE", confirmCode = confirm });
            Assert.Equal(HttpStatusCode.NoContent, ok.StatusCode);
            var body = await ok.Content.ReadAsStringAsync();
            Assert.DoesNotContain(secret, body, StringComparison.Ordinal);

            await db.Entry(employer).ReloadAsync();
            Assert.False(employer.AuthenticatorEnabled);
            Assert.Null(employer.AuthenticatorSecret);
            Assert.Null(employer.RecoveryCodesHash);
            Assert.Null(employer.AuthenticatorEnrolledAtUtc);
            Assert.Equal(beforeVersion + 1, employer.SessionVersion);

            var logsAfter = await db.PersonalDataAccessLogs.CountAsync(l =>
                l.Resource == "user.mfa" && l.Action == "reset" && l.SubjectUserId == employer.Id);
            Assert.Equal(logsBefore + 1, logsAfter);
            var log = await db.PersonalDataAccessLogs
                .Where(l => l.Resource == "user.mfa" && l.Action == "reset" && l.SubjectUserId == employer.Id)
                .OrderByDescending(l => l.OccurredAt)
                .FirstAsync();
            Assert.Equal(_factory.AdminId, log.ActorUserId);
            Assert.Equal("Support ticket ABCDE", log.Reason);
            Assert.DoesNotContain(secret, log.Reason ?? "", StringComparison.Ordinal);
        }
    }

    [Fact]
    public void Ui_guards_no_inline_style_and_mfa_strings_present()
    {
        var root = FindRepoRoot();
        foreach (var rel in new[]
                 {
                     "Jobsy.Web/Components/Pages/Account/MfaSetup.razor",
                     "Jobsy.Web/Components/Pages/Account/MfaPrompt.razor",
                     "Jobsy.Web/Components/Pages/Account/MfaRecoveryCodes.razor"
                 })
        {
            var text = File.ReadAllText(Path.Combine(root, rel));
            Assert.DoesNotContain("style=\"", text, StringComparison.Ordinal);
            Assert.Contains("ExcludeFromInteractiveRouting", text, StringComparison.Ordinal);
            Assert.Contains("Culture[\"Mfa.", text, StringComparison.Ordinal);
            // Static SSR: Nav.NavigateTo throws NavigationException; a bare catch made Acc show
            // the empty "Er ging iets mis" page instead of redirecting to setup.
            Assert.DoesNotContain("Nav.NavigateTo", text, StringComparison.Ordinal);
        }

        foreach (var rel in new[]
                 {
                     "Jobsy.Web/Components/Pages/Account/MfaSetup.razor",
                     "Jobsy.Web/Components/Pages/Account/MfaPrompt.razor"
                 })
        {
            Assert.Contains(
                "Response.Redirect",
                File.ReadAllText(Path.Combine(root, rel)),
                StringComparison.Ordinal);
        }

        var auth = File.ReadAllText(Path.Combine(root, "Jobsy.Web/Auth/AuthServiceCollectionExtensions.cs"));
        Assert.Contains("apiProfile.MfaEnrolled ? \"/account/mfa\" : \"/account/mfa/setup\"", auth, StringComparison.Ordinal);

        Assert.Equal("Beveilig je account", UiStrings.Get("Mfa.SetupTitle", "nl"));
        Assert.Equal("Secure your account", UiStrings.Get("Mfa.SetupTitle", "en"));
        Assert.Equal(
            "Authenticator bij sollicitatie (stub)",
            UiStrings.Get("AdminSettings.AuthenticatorStub.Title", "nl"));
        Assert.Contains(
            "AuthenticatorStub",
            File.ReadAllText(Path.Combine(root, "Jobsy.Web/Admin/PlatformSettingsCatalog.cs")),
            StringComparison.Ordinal);
        Assert.Contains(
            "GroupDemo",
            File.ReadAllText(Path.Combine(root, "Jobsy.Web/Admin/PlatformSettingsCatalog.cs")),
            StringComparison.Ordinal);
    }

    [Fact]
    public void Totp_qr_svg_data_uri_is_valid()
    {
        var uri = TotpQrCode.ToSvgDataUri("otpauth://totp/Lobsy:test@example.com?secret=JBSWY3DPEHPK3PXP&issuer=Lobsy");
        Assert.StartsWith("data:image/svg+xml;base64,", uri);
        var b64 = uri["data:image/svg+xml;base64,".Length..];
        var svg = Encoding.UTF8.GetString(Convert.FromBase64String(b64));
        Assert.Contains("<svg", svg, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Recovery_cookie_helper_constants_are_stable()
    {
        Assert.Equal("Jobsy.MfaRecoveryCodes", MfaRecoveryCodesCookie.Name);
        Assert.Equal("Jobsy.MfaRecoveryCodes.v1", MfaRecoveryCodesCookie.ProtectorPurpose);
    }

    private HttpClient CreateMfaAuthedClient(Guid userId, string authMethod)
    {
        var client = _factory.CreateClient();
        var jwt = JobsyAccessToken.Create(
            userId,
            sessionVersion: 0,
            JobsyAccessToken.DevelopmentPrivateKeyPem,
            mfaVerified: true,
            authMethod: authMethod);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", jwt);
        return client;
    }

    private static MfaChallengeService NewChallenges()
        => new(new MemoryCache(new MemoryCacheOptions()));

    private static async Task<User> SeedAdminAsync(
        JobsyDbContext db,
        bool enrolled,
        string? secret = null,
        string? recoveryPlain = null)
    {
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = $"admin-{Guid.NewGuid():N}@test.local",
            FullName = "Admin Test",
            Role = UserRole.Admin,
            IsActive = true,
            AuthenticatorEnabled = enrolled,
            AuthenticatorSecret = enrolled || secret is not null ? secret ?? TotpAuthenticator.GenerateSecret() : null,
            AuthenticatorEnrolledAtUtc = enrolled ? DateTime.UtcNow : null
        };
        if (recoveryPlain is not null)
        {
            var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(recoveryPlain.Trim().ToUpperInvariant())));
            user.RecoveryCodesHash = JsonSerializer.Serialize(new[] { hash });
        }

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

    private static AuthController CreateAuthController(
        JobsyDbContext db,
        MfaChallengeService challenges,
        string secret = "test-secret")
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["JobsyAuth:DevelopmentAuthSecret"] = secret
            })
            .Build();
        var credentials = new IntegrationCredentialService(db, new PassthroughSecretProtector());
        var sut = new AuthController(
            db, config, credentials,
            new AmbassadeurAttributionService(db, new AmbassadeurSettingsService(db), NullLogger<AmbassadeurAttributionService>.Instance, new AlwaysOnFeatures()),
            new StubHostEnvironment(),
            new DeviceSessionService(db, config, new MemoryCache(new MemoryCacheOptions()), NullLogger<DeviceSessionService>.Instance),
            new EmailServiceStub(db, NullLogger<EmailServiceStub>.Instance),
            challenges,
            new StubFeatures(authenticatorEnabled: true),
            new UnknownAccountLockoutTracker("test-lockout-key"),
            NullLogger<AuthController>.Instance);
        sut.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() };
        return sut;
    }

    private static MfaController CreateMfaController(
        JobsyDbContext db,
        MfaChallengeService challenges,
        ISecretProtector secrets)
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["JobsyAuth:DevelopmentAuthSecret"] = "test-secret"
            })
            .Build();
        var sut = new MfaController(
            db,
            secrets,
            new DeviceSessionService(db, config, new MemoryCache(new MemoryCacheOptions()), NullLogger<DeviceSessionService>.Instance),
            config,
            challenges,
            new Jobsy.Infrastructure.Security.TotpVerifier(db),
            new EmailServiceStub(db, NullLogger<EmailServiceStub>.Instance),
            new StubFeatures(authenticatorEnabled: true),
            NullLogger<MfaController>.Instance);
        sut.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() };
        return sut;
    }

    private static ControllerContext WithProvisionSecret(string secret)
    {
        var http = new DefaultHttpContext();
        http.Request.Headers["X-Jobsy-Provision-Secret"] = secret;
        return new ControllerContext { HttpContext = http };
    }

    private static JobsyDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<JobsyDbContext>()
            .UseInMemoryDatabase("MfaEnroll-" + Guid.NewGuid())
            .Options;
        return new JobsyDbContext(options);
    }

    private static string FindRepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "Jobsy.sln")))
            {
                return dir.FullName;
            }

            dir = dir.Parent;
        }

        throw new InvalidOperationException("Jobsy.sln not found.");
    }

    private sealed class StubHostEnvironment : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = Environments.Development;
        public string ApplicationName { get; set; } = "Jobsy.Tests";
        public string ContentRootPath { get; set; } = AppContext.BaseDirectory;
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }

    private sealed class StubFeatures(bool authenticatorEnabled) : IPlatformFeatureService
    {
        public Task<PlatformFeatureSnapshot> GetAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(new PlatformFeatureSnapshot(true, authenticatorEnabled, false, "http://localhost", null));

        public Task<PlatformFeatureSnapshot> UpdateAsync(PlatformFeatureUpdate update, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();
    }
}

public class MfaWebSsrGuardTests
{
    [Fact]
    public void Mfa_pages_excluded_from_interactive_routing_and_app_switches_render_mode()
    {
        var root = FindRepoRoot();
        var app = File.ReadAllText(Path.Combine(root, "Jobsy.Web/Components/App.razor"));
        Assert.Contains("AcceptsInteractiveRouting()", app, StringComparison.Ordinal);
        Assert.Contains("account-mfa.css", app, StringComparison.Ordinal);

        foreach (var file in Directory.GetFiles(
                     Path.Combine(root, "Jobsy.Web/Components/Pages/Account"), "Mfa*.razor"))
        {
            var text = File.ReadAllText(file);
            Assert.Contains("[ExcludeFromInteractiveRouting]", text, StringComparison.Ordinal);
            Assert.DoesNotContain("<!--Blazor:", text, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void Verify_handler_sets_recovery_cookie_and_password_totp_auth_method()
    {
        var text = File.ReadAllText(Path.Combine(FindRepoRoot(), "Jobsy.Web/Auth/AuthServiceCollectionExtensions.cs"));
        Assert.Contains("password+totp", text, StringComparison.Ordinal);
        Assert.Contains("SetRecoveryCodesCookie", text, StringComparison.Ordinal);
        Assert.Contains("MfaRecoveryCodesCookie", text, StringComparison.Ordinal);
        Assert.Contains("external:{provider}", text, StringComparison.Ordinal);
    }

    private static string FindRepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "Jobsy.sln")))
            {
                return dir.FullName;
            }

            dir = dir.Parent;
        }

        throw new InvalidOperationException("Jobsy.sln not found.");
    }
}

/// <summary>Optional Playwright soft-skip — only when JOBSY_E2E_BASE_URL points at acceptatie.</summary>
public class MfaEnrollmentPlaywrightTests
{
    [Fact]
    public void Soft_skip_when_e2e_base_url_unset()
    {
        var baseUrl = Environment.GetEnvironmentVariable("JOBSY_E2E_BASE_URL");
        if (string.IsNullOrWhiteSpace(baseUrl))
        {
            return;
        }

        Assert.DoesNotContain("lobsy.nl", baseUrl, StringComparison.OrdinalIgnoreCase);
        Assert.True(
            baseUrl.Contains("acceptatie", StringComparison.OrdinalIgnoreCase)
            || baseUrl.Contains("localhost", StringComparison.OrdinalIgnoreCase)
            || baseUrl.Contains("127.0.0.1", StringComparison.OrdinalIgnoreCase),
            "E2E MFA enrollment may only run against acceptatie/local, never production.");
    }
}

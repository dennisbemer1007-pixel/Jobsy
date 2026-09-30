using System.Security.Cryptography;
using System.Text.RegularExpressions;
using Jobsy.Api.Controllers;
using Jobsy.Api.Models;
using Jobsy.Api.Security;
using Jobsy.Core.Entities;
using Jobsy.Core.Enums;
using Jobsy.Core.Interfaces;
using Jobsy.Core.Security;
using Jobsy.Infrastructure.Data;
using Jobsy.Infrastructure.Security;
using Jobsy.Infrastructure.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Jobsy.Core.Email;

namespace Jobsy.Tests;

public class EmailCodeAuthTests
{
    [Theory]
    [InlineData("nieuw@example.com")]
    [InlineData("kandidaat@example.com")]
    [InlineData("werkgever@example.com")]
    [InlineData("not-an-email")]
    public async Task Start_returns_identical_202_for_all_address_shapes(string email)
    {
        await using var db = CreateDb();
        db.Users.Add(new User
        {
            Id = Guid.NewGuid(),
            Email = "kandidaat@example.com",
            FullName = "Kandidaat",
            Role = UserRole.Candidate,
            IsActive = true
        });
        db.Users.Add(new User
        {
            Id = Guid.NewGuid(),
            Email = "werkgever@example.com",
            FullName = "Werkgever",
            Role = UserRole.BranchManager,
            IsActive = true
        });
        await db.SaveChangesAsync();

        var emailSvc = new CapturingEmail();
        var sut = CreateAuthController(db, emailSvc, secret: "prov-secret");
        sut.ControllerContext = WithProvisionSecret("prov-secret");

        var result = await sut.StartEmailCode(
            new EmailCodeStartRequest(email, FirstName: "Sam", Culture: "nl"),
            CancellationToken.None);

        var accepted = Assert.IsType<AcceptedResult>(result.Result);
        Assert.Equal(StatusCodes.Status202Accepted, accepted.StatusCode);
        Assert.IsType<EmailCodeStartResponse>(accepted.Value);

        if (string.Equals(email, "werkgever@example.com", StringComparison.OrdinalIgnoreCase))
        {
            Assert.Contains(emailSvc.Sent, m => m.Category == "EmailCodeUsePassword");
            Assert.DoesNotContain(emailSvc.Sent, m => m.Category is "EmailSignUpCode" or "EmailSignInCode");
            Assert.DoesNotContain(db.EmailSignInChallenges, c =>
                c.EmailNormalized == "werkgever@example.com" && c.ConsumedAtUtc is null);
        }
    }

    [Fact]
    public async Task Start_requires_provision_secret()
    {
        await using var db = CreateDb();
        var sut = CreateAuthController(db, new CapturingEmail(), secret: "prov-secret", production: true);
        sut.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() };

        var result = await sut.StartEmailCode(
            new EmailCodeStartRequest("a@example.com"),
            CancellationToken.None);

        Assert.IsType<UnauthorizedObjectResult>(result.Result);
    }

    [Fact]
    public async Task Start_throttles_after_three_in_fifteen_minutes()
    {
        await using var db = CreateDb();
        var emailSvc = new CapturingEmail();
        var sut = CreateAuthController(db, emailSvc, secret: "prov-secret");
        sut.ControllerContext = WithProvisionSecret("prov-secret");

        for (var i = 0; i < 3; i++)
        {
            var ok = await sut.StartEmailCode(new EmailCodeStartRequest("throttle@example.com"), CancellationToken.None);
            Assert.IsType<AcceptedResult>(ok.Result);
        }

        var blocked = await sut.StartEmailCode(new EmailCodeStartRequest("throttle@example.com"), CancellationToken.None);
        var conflict = Assert.IsType<ConflictObjectResult>(blocked.Result);
        Assert.Contains("too_many_codes", conflict.Value!.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task Verify_sign_up_creates_candidate_with_terms_and_referral()
    {
        await using var db = CreateDb();
        var amb = new User
        {
            Id = Guid.NewGuid(),
            Email = "am@example.com",
            FullName = "Am",
            Role = UserRole.Ambassadeur,
            IsActive = true
        };
        db.Users.Add(amb);
        var now = DateTime.UtcNow;
        db.AmbassadeurProfiles.Add(new AmbassadeurProfile
        {
            Id = Guid.NewGuid(),
            UserId = amb.Id,
            TrackingCode = "AM-TEST01",
            BaseCommissionPercentage = 5m,
            AgreementSignedAt = now,
            AgreementVersion = "v1",
            OnboardingCompletedAt = now,
            CreatedAt = now,
            UpdatedAt = now,
            CompanyName = "AM BV",
            KvkNumber = "12345678"
        });
        await db.SaveChangesAsync();

        var emailSvc = new CapturingEmail();
        var sut = CreateAuthController(db, emailSvc, secret: "prov-secret");
        sut.ControllerContext = WithProvisionSecret("prov-secret");

        var start = await sut.StartEmailCode(
            new EmailCodeStartRequest(
                "nieuw@example.com",
                FirstName: "Nova",
                ReferralCode: "AM-TEST01",
                Culture: "en"),
            CancellationToken.None);
        var startBody = Assert.IsType<EmailCodeStartResponse>(Assert.IsType<AcceptedResult>(start.Result).Value);
        var code = ExtractOtp(emailSvc.Sent.Single(m => m.Category == "EmailSignUpCode").BodyHtml);

        var verify = await sut.VerifyEmailCode(
            new EmailCodeVerifyRequest(startBody.ChallengeId, code, RememberDevice: false),
            CancellationToken.None);
        var profile = Assert.IsType<LocalLoginResponse>(Assert.IsType<OkObjectResult>(verify.Result).Value);

        Assert.Equal("nieuw@example.com", profile.Email);
        Assert.Equal("Candidate", profile.Role);
        Assert.Equal("Nova", profile.FullName);

        var user = await db.Users.SingleAsync(u => u.Email == "nieuw@example.com");
        Assert.NotNull(user.TermsAcceptedAt);
        Assert.Equal(UserRole.Candidate, user.Role);
        Assert.Equal(amb.Id, user.ReferredByAmbassadeurUserId);
    }

    [Fact]
    public async Task Verify_wrong_code_five_times_burns_challenge()
    {
        await using var db = CreateDb();
        var emailSvc = new CapturingEmail();
        var sut = CreateAuthController(db, emailSvc, secret: "prov-secret");
        sut.ControllerContext = WithProvisionSecret("prov-secret");

        var start = await sut.StartEmailCode(new EmailCodeStartRequest("burn@example.com"), CancellationToken.None);
        var id = Assert.IsType<EmailCodeStartResponse>(Assert.IsType<AcceptedResult>(start.Result).Value).ChallengeId;

        for (var i = 0; i < VerificationCodes.MaxFailedAttempts - 1; i++)
        {
            var wrong = await sut.VerifyEmailCode(new EmailCodeVerifyRequest(id, "000000"), CancellationToken.None);
            Assert.IsType<UnauthorizedObjectResult>(wrong.Result);
        }

        var burned = await sut.VerifyEmailCode(new EmailCodeVerifyRequest(id, "000000"), CancellationToken.None);
        Assert.Equal(StatusCodes.Status410Gone, Assert.IsType<ObjectResult>(burned.Result).StatusCode);

        var challenge = await db.EmailSignInChallenges.SingleAsync(c => c.Id == id);
        Assert.NotNull(challenge.ConsumedAtUtc);
    }

    [Fact]
    public async Task Verify_expired_and_consumed_return_410()
    {
        await using var db = CreateDb();
        var sut = CreateAuthController(db, new CapturingEmail(), secret: "prov-secret");
        sut.ControllerContext = WithProvisionSecret("prov-secret");

        var expiredId = Guid.NewGuid();
        db.EmailSignInChallenges.Add(new EmailSignInChallenge
        {
            Id = expiredId,
            EmailNormalized = "x@example.com",
            CodeHash = VerificationCodes.Hash("123456"),
            Purpose = EmailSignInPurpose.SignUp,
            CreatedAtUtc = DateTime.UtcNow.AddMinutes(-20),
            ExpiresAtUtc = DateTime.UtcNow.AddMinutes(-10)
        });
        var consumedId = Guid.NewGuid();
        db.EmailSignInChallenges.Add(new EmailSignInChallenge
        {
            Id = consumedId,
            EmailNormalized = "y@example.com",
            CodeHash = VerificationCodes.Hash("123456"),
            Purpose = EmailSignInPurpose.SignIn,
            CreatedAtUtc = DateTime.UtcNow,
            ExpiresAtUtc = DateTime.UtcNow.AddMinutes(10),
            ConsumedAtUtc = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        var expired = await sut.VerifyEmailCode(new EmailCodeVerifyRequest(expiredId, "123456"), CancellationToken.None);
        Assert.Equal(StatusCodes.Status410Gone, Assert.IsType<ObjectResult>(expired.Result).StatusCode);

        var consumed = await sut.VerifyEmailCode(new EmailCodeVerifyRequest(consumedId, "123456"), CancellationToken.None);
        Assert.Equal(StatusCodes.Status410Gone, Assert.IsType<ObjectResult>(consumed.Result).StatusCode);
    }

    [Fact]
    public async Task Verify_parallel_only_succeeds_once()
    {
        await using var db = CreateDb();
        var emailSvc = new CapturingEmail();
        var sut = CreateAuthController(db, emailSvc, secret: "prov-secret");
        sut.ControllerContext = WithProvisionSecret("prov-secret");

        var start = await sut.StartEmailCode(new EmailCodeStartRequest("race@example.com"), CancellationToken.None);
        var id = Assert.IsType<EmailCodeStartResponse>(Assert.IsType<AcceptedResult>(start.Result).Value).ChallengeId;
        var code = ExtractOtp(emailSvc.Sent.Single().BodyHtml);

        var first = await sut.VerifyEmailCode(new EmailCodeVerifyRequest(id, code, RememberDevice: false), CancellationToken.None);
        Assert.IsType<OkObjectResult>(first.Result);

        var second = await sut.VerifyEmailCode(new EmailCodeVerifyRequest(id, code, RememberDevice: false), CancellationToken.None);
        Assert.Equal(StatusCodes.Status410Gone, Assert.IsType<ObjectResult>(second.Result).StatusCode);
        Assert.Equal(1, await db.Users.CountAsync(u => u.Email == "race@example.com"));
    }

    [Fact]
    public async Task Verify_mfa_enrolled_candidate_returns_requires_mfa()
    {
        await using var db = CreateDb();
        db.Users.Add(new User
        {
            Id = Guid.NewGuid(),
            Email = "mfa@example.com",
            FullName = "Mfa User",
            Role = UserRole.Candidate,
            IsActive = true,
            AuthenticatorEnabled = true,
            AuthenticatorSecret = "enc"
        });
        await db.SaveChangesAsync();

        var emailSvc = new CapturingEmail();
        var sut = CreateAuthController(db, emailSvc, secret: "prov-secret");
        sut.ControllerContext = WithProvisionSecret("prov-secret");

        var start = await sut.StartEmailCode(new EmailCodeStartRequest("mfa@example.com"), CancellationToken.None);
        var id = Assert.IsType<EmailCodeStartResponse>(Assert.IsType<AcceptedResult>(start.Result).Value).ChallengeId;
        var code = ExtractOtp(emailSvc.Sent.Single().BodyHtml);

        var verify = await sut.VerifyEmailCode(new EmailCodeVerifyRequest(id, code), CancellationToken.None);
        var body = Assert.IsType<LocalLoginResponse>(Assert.IsType<OkObjectResult>(verify.Result).Value);
        Assert.True(body.RequiresMfa);
        Assert.True(body.MfaEnrolled);
        Assert.False(string.IsNullOrWhiteSpace(body.MfaChallengeToken));
    }

    [Fact]
    public async Task Non_candidate_challenge_cannot_verify()
    {
        await using var db = CreateDb();
        db.Users.Add(new User
        {
            Id = Guid.NewGuid(),
            Email = "boss@example.com",
            FullName = "Boss",
            Role = UserRole.EnterpriseManager,
            IsActive = true
        });
        await db.SaveChangesAsync();

        var emailSvc = new CapturingEmail();
        var sut = CreateAuthController(db, emailSvc, secret: "prov-secret");
        sut.ControllerContext = WithProvisionSecret("prov-secret");

        var start = await sut.StartEmailCode(new EmailCodeStartRequest("boss@example.com"), CancellationToken.None);
        var dummyId = Assert.IsType<EmailCodeStartResponse>(Assert.IsType<AcceptedResult>(start.Result).Value).ChallengeId;

        var verify = await sut.VerifyEmailCode(new EmailCodeVerifyRequest(dummyId, "123456"), CancellationToken.None);
        Assert.Equal(StatusCodes.Status410Gone, Assert.IsType<ObjectResult>(verify.Result).StatusCode);
    }

    [Fact]
    public void Email_templates_compose_for_catalog()
    {
        var ctx = Jobsy.Core.Email.EmailSampleContext.ForPreview("https://lobsy.nl");
        foreach (var key in new[] { "EmailSignUpCode", "EmailSignInCode", "EmailCodeUsePassword" })
        {
            var mail = Jobsy.Core.Email.TransactionalEmails.Compose(key, ctx);
            Assert.False(string.IsNullOrWhiteSpace(mail.Subject));
            Assert.False(string.IsNullOrWhiteSpace(mail.Html));
        }
    }

    private static string ExtractOtp(string html)
    {
        var m = Regex.Match(html, "data-lobsy-otp=\"(\\d{6})\"");
        Assert.True(m.Success, "OTP marker missing from e-mail HTML");
        return m.Groups[1].Value;
    }

    private static AuthController CreateAuthController(
        JobsyDbContext db,
        ITransactionalMailer email,
        string secret,
        bool production = false)
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["JobsyAuth:ExternalProvisionSecret"] = secret,
                ["PublicWebBaseUrl"] = "https://lobsy.nl"
            })
            .Build();
        var credentials = new IntegrationCredentialService(db, new PassthroughSecretProtector());
        var features = new AlwaysOnFeatures();
        return new AuthController(
            db,
            config,
            credentials,
            new AmbassadeurAttributionService(
                db,
                new AmbassadeurSettingsService(db),
                NullLogger<AmbassadeurAttributionService>.Instance,
                features),
            new StubHostEnvironment
            {
                EnvironmentName = production ? Environments.Production : Environments.Development
            },
            new DeviceSessionService(
                db,
                config,
                new MemoryCache(new MemoryCacheOptions()),
                NullLogger<DeviceSessionService>.Instance),
            new MfaTrustedDeviceService(db),
            email,
            new MfaChallengeService(new MemoryCache(new MemoryCacheOptions())),
            features,
            new UnknownAccountLockoutTracker("test-lockout-key"),
            NullLogger<AuthController>.Instance);
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
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new JobsyDbContext(options);
    }

    private sealed class CapturingEmail : IEmailService, ITransactionalMailer
    {
        public async Task<EmailSendOutcome> SendAsync(
            ComposedEmail mail,
            string to,
            EmailSendOptions? options = null,
            CancellationToken cancellationToken = default)
        {
            var delivery = await SendAsync(
                new EmailMessage(to, mail.Subject, mail.Html ?? string.Empty, mail.Category),
                cancellationToken);
            return new EmailSendOutcome(true, false, null, delivery.Kind);
        }

        public List<EmailMessage> Sent { get; } = [];

        public Task<EmailDeliveryResult> SendAsync(EmailMessage message, CancellationToken cancellationToken = default)
        {
            Sent.Add(message);
            return Task.FromResult(EmailDeliveryResult.Stub);
        }
    }

    private sealed class StubHostEnvironment : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = Environments.Development;
        public string ApplicationName { get; set; } = "Jobsy.Tests";
        public string ContentRootPath { get; set; } = AppContext.BaseDirectory;
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}

using Jobsy.Api.Controllers;
using Jobsy.Core.Email;
using Jobsy.Core.Entities;
using Jobsy.Core.Enums;
using Jobsy.Core.Interfaces;
using Jobsy.Core.Security;
using Jobsy.Infrastructure.Data;
using Jobsy.Infrastructure.Security;
using Jobsy.Infrastructure.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;

namespace Jobsy.Tests;

public class PasswordResetRequestTests
{
    [Fact]
    public async Task Request_returns_202_for_known_unknown_and_bad_input()
    {
        await using var db = CreateDb();
        await SeedLocalUserAsync(db, "known@test.local", "SecretPass1!");
        var mailer = new PasswordResetFixtures.CapturingMailer();
        var sut = CreateController(db, mailer);

        Assert.IsType<AcceptedResult>(await sut.RequestReset(new PasswordResetController.PasswordResetRequestBody("known@test.local", "nl"), default));
        Assert.IsType<AcceptedResult>(await sut.RequestReset(new PasswordResetController.PasswordResetRequestBody("unknown@test.local", "nl"), default));
        Assert.IsType<AcceptedResult>(await sut.RequestReset(new PasswordResetController.PasswordResetRequestBody("not-an-email", "nl"), default));

        await Task.Delay(250);
        Assert.Contains("PasswordReset", mailer.SentKeys);
        Assert.Equal(1, mailer.SentKeys.Count(k => k == "PasswordReset"));
    }

    [Fact]
    public async Task External_only_gets_external_mail_without_link()
    {
        await using var db = CreateDb();
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "ext@test.local",
            FullName = "Ext",
            Role = UserRole.Candidate,
            IsActive = true
        };
        db.Users.Add(user);
        db.UserExternalLogins.Add(new UserExternalLogin
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            Provider = "google",
            ProviderSubject = "sub-1"
        });
        await db.SaveChangesAsync();

        var mailer = new PasswordResetFixtures.CapturingMailer();
        var sut = CreateController(db, mailer);
        Assert.IsType<AcceptedResult>(await sut.RequestReset(new PasswordResetController.PasswordResetRequestBody(user.Email, "nl"), default));
        await Task.Delay(250);
        Assert.Contains("PasswordResetExternalOnly", mailer.SentKeys);
        Assert.DoesNotContain("PasswordReset", mailer.SentKeys);
    }

    [Fact]
    public async Task Fourth_request_in_hour_sends_no_mail()
    {
        await using var db = CreateDb();
        await SeedLocalUserAsync(db, "rate@test.local", "SecretPass1!");
        var mailer = new PasswordResetFixtures.CapturingMailer();
        var limiter = new PasswordResetRequestLimiter("test-key");
        var sut = CreateController(db, mailer, limiter);
        for (var i = 0; i < 3; i++)
        {
            await sut.RequestReset(new PasswordResetController.PasswordResetRequestBody("rate@test.local", "nl"), default);
        }

        await Task.Delay(400);
        var before = mailer.SentKeys.Count;
        await sut.RequestReset(new PasswordResetController.PasswordResetRequestBody("rate@test.local", "nl"), default);
        await Task.Delay(200);
        Assert.Equal(before, mailer.SentKeys.Count);
    }

    [Fact]
    public async Task New_request_invalidates_older_link()
    {
        await using var db = CreateDb();
        var user = await SeedLocalUserAsync(db, "rotate@test.local", "SecretPass1!");
        var links = new OneTimeLinkService(db, NullLogger<OneTimeLinkService>.Instance);
        var first = await links.CreateAsync(OneTimeLinkPurpose.PasswordReset, user.Id, null, user.Email, OneTimeLinkRules.PasswordResetLifetime);
        var second = await links.CreateAsync(OneTimeLinkPurpose.PasswordReset, user.Id, null, user.Email, OneTimeLinkRules.PasswordResetLifetime);
        Assert.Null(await links.ConsumeAsync(OneTimeLinkPurpose.PasswordReset, first.Token));
        Assert.NotNull(await links.ConsumeAsync(OneTimeLinkPurpose.PasswordReset, second.Token));
    }

    private static JobsyDbContext CreateDb() => PasswordResetFixtures.CreateDb();
    private static Task<User> SeedLocalUserAsync(JobsyDbContext db, string email, string password)
        => PasswordResetFixtures.SeedLocalUserAsync(db, email, password);
    private static PasswordResetController CreateController(
        JobsyDbContext db, ITransactionalMailer mailer, PasswordResetRequestLimiter? limiter = null)
        => PasswordResetFixtures.CreateController(db, mailer, limiter);
}

public class PasswordResetCompleteTests
{
    [Fact]
    public async Task Complete_sets_password_revokes_trust_and_keeps_2fa()
    {
        await using var db = PasswordResetFixtures.CreateDb();
        var secret = TotpAuthenticator.GenerateSecret();
        var user = await PasswordResetFixtures.SeedLocalUserAsync(db, "reset@test.local", "OldPass123456!", enrolledSecret: secret);
        var trusted = new MfaTrustedDeviceService(db);
        await trusted.CreateAsync(user.Id, "ua", user.SessionVersion);
        var links = new OneTimeLinkService(db, NullLogger<OneTimeLinkService>.Instance);
        var created = await links.CreateAsync(OneTimeLinkPurpose.PasswordReset, user.Id, null, user.Email, OneTimeLinkRules.PasswordResetLifetime);

        var beforeVersion = user.SessionVersion;
        var mailer = new PasswordResetFixtures.CapturingMailer();
        var sut = PasswordResetFixtures.CreateController(db, mailer);
        var result = await sut.Complete(
            new PasswordResetController.PasswordResetCompleteBody(created.Token, "NewPass123456!"),
            default);
        Assert.IsType<OkObjectResult>(result);

        var cred = await db.LocalAuthCredentials.SingleAsync(c => c.UserId == user.Id);
        Assert.True(JobsyPasswordHasher.Verify("NewPass123456!", cred.PasswordHash));
        Assert.False(JobsyPasswordHasher.Verify("OldPass123456!", cred.PasswordHash));
        Assert.Equal(0, await trusted.CountActiveAsync(user.Id));
        var reloaded = await db.Users.AsNoTracking().SingleAsync(u => u.Id == user.Id);
        Assert.True(reloaded.AuthenticatorEnabled);
        Assert.Equal(secret, reloaded.AuthenticatorSecret);
        Assert.True(reloaded.SessionVersion > beforeVersion);
        Assert.Contains("PasswordChanged", mailer.SentKeys);

        Assert.IsType<BadRequestObjectResult>(await sut.Complete(
            new PasswordResetController.PasswordResetCompleteBody(created.Token, "AnotherPass999!"),
            default));
    }

    [Fact]
    public async Task Short_password_does_not_consume_token()
    {
        await using var db = PasswordResetFixtures.CreateDb();
        var user = await PasswordResetFixtures.SeedLocalUserAsync(db, "short@test.local", "OldPass123456!");
        var links = new OneTimeLinkService(db, NullLogger<OneTimeLinkService>.Instance);
        var created = await links.CreateAsync(OneTimeLinkPurpose.PasswordReset, user.Id, null, user.Email, OneTimeLinkRules.PasswordResetLifetime);
        var sut = PasswordResetFixtures.CreateController(db, new PasswordResetFixtures.CapturingMailer());
        Assert.IsType<BadRequestObjectResult>(await sut.Complete(
            new PasswordResetController.PasswordResetCompleteBody(created.Token, "short"),
            default));
        var peek = await links.PeekAsync(OneTimeLinkPurpose.PasswordReset, created.Token);
        Assert.True(peek.Valid);
    }
}

file static class PasswordResetFixtures
{
    public static JobsyDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<JobsyDbContext>()
            .UseInMemoryDatabase("PwdReset-" + Guid.NewGuid())
            .Options;
        return new JobsyDbContext(options);
    }

    public static async Task<User> SeedLocalUserAsync(
        JobsyDbContext db,
        string email,
        string password,
        string? enrolledSecret = null)
    {
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = email,
            FullName = "Reset User",
            Role = UserRole.Candidate,
            IsActive = true,
            AuthenticatorEnabled = enrolledSecret is not null,
            AuthenticatorSecret = enrolledSecret,
            AuthenticatorEnrolledAtUtc = enrolledSecret is null ? null : DateTime.UtcNow,
            SessionVersion = 1
        };
        db.Users.Add(user);
        db.LocalAuthCredentials.Add(new LocalAuthCredential
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            Email = email.ToLowerInvariant(),
            PasswordHash = JobsyPasswordHasher.Hash(password),
            FailedLoginCount = 3,
            LockoutUntil = DateTime.UtcNow.AddMinutes(10),
            LockoutCount = 2
        });
        await db.SaveChangesAsync();
        return user;
    }

    public static PasswordResetController CreateController(
        JobsyDbContext db,
        ITransactionalMailer mailer,
        PasswordResetRequestLimiter? limiter = null)
    {
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["JobsyAuth:DevelopmentAuthSecret"] = "test-secret"
        }).Build();
        var sut = new PasswordResetController(
            db,
            new OneTimeLinkService(db, NullLogger<OneTimeLinkService>.Instance),
            new DeviceSessionService(db, config, new MemoryCache(new MemoryCacheOptions()), NullLogger<DeviceSessionService>.Instance),
            new MfaTrustedDeviceService(db),
            mailer,
            new StubFeatures(),
            limiter ?? new PasswordResetRequestLimiter("test-key"),
            NullLogger<PasswordResetController>.Instance);
        sut.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() };
        return sut;
    }

    public sealed class CapturingMailer : ITransactionalMailer
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

    private sealed class StubFeatures : IPlatformFeatureService
    {
        public Task<PlatformFeatureSnapshot> GetAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(new PlatformFeatureSnapshot(true, true, false, "https://lobsy.test", null));

        public Task<PlatformFeatureSnapshot> UpdateAsync(PlatformFeatureUpdate update, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();
    }
}

using System.Security.Claims;
using Jobsy.Core.Entities;
using Jobsy.Core.Enums;
using Jobsy.Core.Interfaces;
using Jobsy.Core.Privacy;
using Jobsy.Core.Security;
using Jobsy.Core.ValueObjects;
using Jobsy.Infrastructure.Data;
using Jobsy.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;

namespace Jobsy.Tests;

public class AccountUnsubscribeTests
{
    [Fact]
    public async Task Request_and_confirm_unsubscribe_blocks_account_cleans_data_and_logs_reason()
    {
        await using var db = CreateDb();
        var candidateId = Guid.NewGuid();
        var companyId = Guid.NewGuid();
        var vacancyId = Guid.NewGuid();
        const string email = "kandidaat@test.nl";

        db.Companies.Add(new Company
        {
            Id = companyId,
            Name = "Demo BV",
            Address = "Straat 1",
            KvkNumber = "123",
            Location = new GeoPoint(52.0, 4.3)
        });
        db.Users.Add(new User
        {
            Id = candidateId,
            Email = email,
            FullName = "Test Kandidaat",
            Role = UserRole.Candidate,
            IsActive = true,
            OpenForWork = true,
            PreferencesJson = """{"roles":["horeca"]}""",
            CandidateHowToCompletedAt = DateTime.UtcNow.AddDays(-1),
            LastLoginAtUtc = DateTime.UtcNow
        });
        db.LocalAuthCredentials.Add(new LocalAuthCredential
        {
            Id = Guid.NewGuid(),
            UserId = candidateId,
            Email = email,
            PasswordHash = "hash"
        });
        db.Vacancies.Add(new Vacancy
        {
            Id = vacancyId,
            CompanyId = companyId,
            Title = "Test vacature",
            Description = "x",
            HourlyWage = 14m,
            StartDate = DateOnly.FromDateTime(DateTime.UtcNow),
            EndDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(30)),
            Status = VacancyStatus.Active,
            Location = new GeoPoint(52, 4),
            RequiredTransport = TransportMode.Bike
        });
        db.Applications.Add(new Application
        {
            Id = Guid.NewGuid(),
            VacancyId = vacancyId,
            CandidateUserId = candidateId,
            CandidateName = "Test Kandidaat",
            CandidateEmail = email,
            PreferredTransport = "Bike",
            Status = ApplicationStatus.Pending,
            SnapshotAboutMe = "Persoonlijke bio",
            CreatedAt = DateTime.UtcNow
        });
        db.VacancyLikes.Add(new VacancyLike
        {
            Id = Guid.NewGuid(),
            VacancyId = vacancyId,
            UserId = candidateId,
            CreatedAt = DateTime.UtcNow
        });
        db.SiteVisits.Add(new SiteVisit
        {
            Id = Guid.NewGuid(),
            UserId = candidateId,
            Path = "/candidate/profile",
            CreatedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        var privacy = CreatePrivacy(db, out var mail);
        var principal = CreatePrincipal(email);

        await privacy.RequestUnsubscribeAsync(
            principal,
            AccountUnsubscribeReasons.Other,
            "Ik wil even stoppen met zoeken");

        var pending = await db.Users.SingleAsync(u => u.Id == candidateId);
        Assert.Equal(AccountUnsubscribeReasons.Other, pending.UnsubscribeReasonCode);
        Assert.Equal("Ik wil even stoppen met zoeken", pending.UnsubscribeReasonOther);
        Assert.False(string.IsNullOrWhiteSpace(pending.UnsubscribeVerificationCode));
        Assert.Equal(VerificationCodes.HashLength, pending.UnsubscribeVerificationCode!.Length);
        Assert.NotNull(pending.UnsubscribeVerificationExpiresAt);

        var requestLog = await db.PlatformLogs
            .Where(l => l.Category == "Unsubscribe" && l.Message.Contains("aangevraagd"))
            .SingleAsync();
        Assert.Contains("Ik wil even stoppen met zoeken", requestLog.Message);

        var code = ExtractOtpFromMail(mail);
        Assert.True(VerificationCodes.MatchesHash(pending.UnsubscribeVerificationCode, code));
        await privacy.ConfirmUnsubscribeAsync(principal, code);

        var user = await db.Users.SingleAsync(u => u.Id == candidateId);
        Assert.False(user.IsActive);
        Assert.StartsWith("deleted-", user.Email);
        Assert.Equal("Verwijderde gebruiker", user.FullName);
        Assert.Null(user.PreferencesJson);
        Assert.Null(user.CandidateHowToCompletedAt);
        Assert.Null(user.LastLoginAtUtc);
        Assert.Null(user.UnsubscribeVerificationCode);
        Assert.Null(user.UnsubscribeReasonCode);

        var app = await db.Applications.SingleAsync(a => a.VacancyId == vacancyId);
        Assert.Null(app.CandidateUserId);
        Assert.Null(app.SnapshotAboutMe);
        Assert.StartsWith("deleted-", app.CandidateEmail);

        Assert.Equal(0, await db.VacancyLikes.CountAsync(l => l.UserId == candidateId));
        Assert.Equal(0, await db.SiteVisits.CountAsync(v => v.UserId == candidateId));
        Assert.Equal(0, await db.LocalAuthCredentials.CountAsync(c => c.UserId == candidateId));

        var confirmLog = await db.PlatformLogs
            .Where(l => l.Category == "Unsubscribe" && l.Message.Contains("bevestigd"))
            .SingleAsync();
        Assert.Contains("Anders", confirmLog.Message);
        Assert.Contains("Ik wil even stoppen met zoeken", confirmLog.Message);
        Assert.Contains(candidateId.ToString(), confirmLog.Message);
    }

    [Fact]
    public async Task Confirm_rejects_wrong_or_expired_code()
    {
        await using var db = CreateDb();
        var candidateId = Guid.NewGuid();
        const string email = "kandidaat2@test.nl";
        db.Users.Add(new User
        {
            Id = candidateId,
            Email = email,
            FullName = "Test",
            Role = UserRole.Candidate,
            IsActive = true,
            UnsubscribeReasonCode = AccountUnsubscribeReasons.FoundJob,
            UnsubscribeVerificationCode = VerificationCodes.Hash("123456"),
            UnsubscribeVerificationExpiresAt = DateTime.UtcNow.AddMinutes(-1)
        });
        await db.SaveChangesAsync();

        var privacy = CreatePrivacy(db);
        var principal = CreatePrincipal(email);

        var expired = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            privacy.ConfirmUnsubscribeAsync(principal, "123456"));
        Assert.Contains("verlopen", expired.Message, StringComparison.OrdinalIgnoreCase);

        var user = await db.Users.SingleAsync(u => u.Id == candidateId);
        user.UnsubscribeVerificationExpiresAt = DateTime.UtcNow.AddMinutes(5);
        await db.SaveChangesAsync();

        var wrong = await Assert.ThrowsAsync<ArgumentException>(() =>
            privacy.ConfirmUnsubscribeAsync(principal, "000000"));
        Assert.Contains("Onjuiste", wrong.Message, StringComparison.OrdinalIgnoreCase);
        Assert.True((await db.Users.SingleAsync(u => u.Id == candidateId)).IsActive);
        Assert.Equal(1, (await db.Users.SingleAsync(u => u.Id == candidateId)).UnsubscribeVerificationFailedAttempts);
    }

    [Fact]
    public async Task Confirm_locks_out_after_max_failed_otp_attempts()
    {
        await using var db = CreateDb();
        var candidateId = Guid.NewGuid();
        const string email = "lockout@test.nl";
        db.Users.Add(new User
        {
            Id = candidateId,
            Email = email,
            FullName = "Test",
            Role = UserRole.Candidate,
            IsActive = true,
            UnsubscribeReasonCode = AccountUnsubscribeReasons.FoundJob,
            UnsubscribeVerificationCode = VerificationCodes.Hash("654321"),
            UnsubscribeVerificationExpiresAt = DateTime.UtcNow.AddMinutes(10)
        });
        await db.SaveChangesAsync();

        var privacy = CreatePrivacy(db);
        var principal = CreatePrincipal(email);

        for (var i = 1; i < VerificationCodes.MaxFailedAttempts; i++)
        {
            var wrong = await Assert.ThrowsAsync<ArgumentException>(() =>
                privacy.ConfirmUnsubscribeAsync(principal, "000000"));
            Assert.Contains("Onjuiste", wrong.Message, StringComparison.OrdinalIgnoreCase);
        }

        var locked = await Assert.ThrowsAsync<ArgumentException>(() =>
            privacy.ConfirmUnsubscribeAsync(principal, "000000"));
        Assert.Contains("Te veel", locked.Message, StringComparison.OrdinalIgnoreCase);

        var user = await db.Users.SingleAsync(u => u.Id == candidateId);
        Assert.Null(user.UnsubscribeVerificationCode);
        Assert.True(user.IsActive);
    }

    [Fact]
    public async Task After_unsubscribe_original_email_is_free_for_clean_reregistration()
    {
        await using var db = CreateDb();
        var oldId = Guid.NewGuid();
        const string email = "opnieuw@test.nl";
        db.Users.Add(new User
        {
            Id = oldId,
            Email = email,
            FullName = "Oud",
            Role = UserRole.Candidate,
            IsActive = true,
            PreferencesJson = """{"roles":["retail"]}""",
            OpenForWork = true
        });
        await db.SaveChangesAsync();

        var privacy = CreatePrivacy(db, out var mail);
        var principal = CreatePrincipal(email);
        await privacy.RequestUnsubscribeAsync(principal, AccountUnsubscribeReasons.FoundJob, null);
        var code = ExtractOtpFromMail(mail);
        await privacy.ConfirmUnsubscribeAsync(principal, code);

        Assert.False(await db.Users.AnyAsync(u => u.Email == email && u.IsActive));

        // Simulate ensure-external / new signup: original email is free again.
        var newUser = new User
        {
            Id = Guid.NewGuid(),
            Email = email,
            FullName = "Nieuw",
            Role = UserRole.Candidate,
            IsActive = true
        };
        db.Users.Add(newUser);
        await db.SaveChangesAsync();

        var fresh = await db.Users.SingleAsync(u => u.Email == email && u.IsActive);
        Assert.NotEqual(oldId, fresh.Id);
        Assert.Null(fresh.PreferencesJson);
        Assert.False(fresh.OpenForWork);
        Assert.Null(fresh.CandidateHowToCompletedAt);
    }

    [Fact]
    public async Task Request_requires_other_text_for_anders()
    {
        await using var db = CreateDb();
        db.Users.Add(new User
        {
            Id = Guid.NewGuid(),
            Email = "x@test.nl",
            FullName = "X",
            Role = UserRole.Candidate,
            IsActive = true
        });
        await db.SaveChangesAsync();

        var privacy = CreatePrivacy(db);
        var ex = await Assert.ThrowsAsync<ArgumentException>(() =>
            privacy.RequestUnsubscribeAsync(CreatePrincipal("x@test.nl"), AccountUnsubscribeReasons.Other, "  "));
        Assert.Contains("toelichting", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Non_candidate_can_also_unsubscribe_with_otp()
    {
        await using var db = CreateDb();
        var userId = Guid.NewGuid();
        const string email = "werkgever@test.nl";
        db.Users.Add(new User
        {
            Id = userId,
            Email = email,
            FullName = "Filiaal",
            Role = UserRole.BranchManager,
            IsActive = true,
            PreferencesJson = null
        });
        await db.SaveChangesAsync();

        var privacy = CreatePrivacy(db, out var mail);
        var principal = new ClaimsPrincipal(new ClaimsIdentity(
        [
            new Claim(ClaimTypes.Email, email),
            new Claim(ClaimTypes.Role, "BranchManager")
        ], "test"));

        await privacy.RequestUnsubscribeAsync(principal, AccountUnsubscribeReasons.Privacy, null);
        var code = ExtractOtpFromMail(mail);
        await privacy.ConfirmUnsubscribeAsync(principal, code);

        var user = await db.Users.SingleAsync(u => u.Id == userId);
        Assert.False(user.IsActive);
        Assert.StartsWith("deleted-", user.Email);
    }

    private static PrivacyDataService CreatePrivacy(JobsyDbContext db, out CapturingEmailService email)
    {
        email = new CapturingEmailService();
        return new PrivacyDataService(db, new StubUserLookup(db), email);
    }

    private static PrivacyDataService CreatePrivacy(JobsyDbContext db)
        => CreatePrivacy(db, out _);

    private static string ExtractOtpFromMail(CapturingEmailService email)
    {
        var html = email.Messages.LastOrDefault()?.BodyHtml
            ?? throw new InvalidOperationException("Geen e-mail verzonden.");
        var match = System.Text.RegularExpressions.Regex.Match(html, @"\b(\d{6})\b");
        Assert.True(match.Success, "Geen 6-cijferige OTP in e-mail.");
        return match.Groups[1].Value;
    }

    private static ClaimsPrincipal CreatePrincipal(string email) =>
        new(new ClaimsIdentity(
        [
            new Claim(ClaimTypes.Email, email),
            new Claim(ClaimTypes.Role, "Candidate")
        ], "test"));

    private static JobsyDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<JobsyDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new JobsyDbContext(options);
    }

    private sealed class CapturingEmailService : IEmailService
    {
        public List<EmailMessage> Messages { get; } = [];

        public Task SendAsync(EmailMessage message, CancellationToken cancellationToken = default)
        {
            Messages.Add(message);
            return Task.CompletedTask;
        }
    }

    private sealed class StubUserLookup(JobsyDbContext db) : Jobsy.Core.Interfaces.IUserLookupService
    {
        public Task<User?> FindByPrincipalAsync(ClaimsPrincipal principal, CancellationToken cancellationToken = default)
        {
            var email = principal.FindFirst(ClaimTypes.Email)?.Value;
            return db.Users.FirstOrDefaultAsync(u => u.Email == email && u.IsActive, cancellationToken);
        }
    }
}

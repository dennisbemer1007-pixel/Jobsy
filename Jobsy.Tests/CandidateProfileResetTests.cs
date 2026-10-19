using System.Security.Claims;
using Jobsy.Core.Email;
using Jobsy.Core.Entities;
using Jobsy.Core.Enums;
using Jobsy.Core.Interfaces;
using Jobsy.Core.Privacy;
using Jobsy.Core.ValueObjects;
using Jobsy.Infrastructure.Data;
using Jobsy.Infrastructure.Services;
using Jobsy.Tests.TestStubs;
using Microsoft.EntityFrameworkCore;

namespace Jobsy.Tests;

public class CandidateProfileResetTests
{
    [Theory]
    [InlineData("nl", "VERWIJDER", true)]
    [InlineData("nl", "V E R W I J D E R", true)]
    [InlineData("nl", "verwijder", false)]
    [InlineData("en", "DELETE", true)]
    [InlineData("pl", "USUŃ", true)]
    [InlineData("ro", "STERGE", true)]
    public void Confirm_phrase_is_language_specific(string lang, string typed, bool ok)
        => Assert.Equal(ok, CandidateProfileResetConfirm.Matches(lang, typed));

    [Fact]
    public async Task Reset_clears_candidate_tables_keeps_login_and_anonymises_verified_application()
    {
        await using var db = CreateDb();
        var candidateId = Guid.NewGuid();
        var otherId = Guid.NewGuid();
        var companyId = Guid.NewGuid();
        var vacancyId = Guid.NewGuid();
        const string email = "reset-me@test.nl";

        db.Users.AddRange(
            new User
            {
                Id = candidateId,
                Email = email,
                FullName = "Reset Kandidaat",
                Role = UserRole.Candidate,
                IsActive = true,
                PreferencesJson = """{"roles":["horeca"]}""",
                CandidateHowToCompletedAt = DateTime.UtcNow,
                ResearchConsentAt = DateTime.UtcNow,
                ResearchConsentVersion = PrivacyConstants.ResearchConsentTextVersion,
                OpenForWork = true
            },
            new User
            {
                Id = otherId,
                Email = "other@test.nl",
                FullName = "Andere",
                Role = UserRole.Candidate,
                IsActive = true,
                PreferencesJson = """{"roles":["retail"]}"""
            });
        db.Companies.Add(new Company
        {
            Id = companyId,
            Name = "Demo BV",
            Address = "Straat 1",
            KvkNumber = "123",
            Location = new GeoPoint(52.0, 4.3),
            VerificationStatus = CompanyVerificationStatus.Verified,
            VerificationMethod = CompanyVerificationMethod.AdminCreated,
            VerifiedAtUtc = DateTime.UtcNow,
            VerificationUpdatedAtUtc = DateTime.UtcNow
        });
        db.Vacancies.Add(new Vacancy
        {
            Id = vacancyId,
            CompanyId = companyId,
            Title = "Test",
            Description = "x",
            HourlyWage = 14m,
            StartDate = DateOnly.FromDateTime(DateTime.UtcNow),
            EndDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(30)),
            Status = VacancyStatus.Active,
            Location = new GeoPoint(52, 4),
            RequiredTransport = TransportMode.Bike
        });
        var verifiedAppId = Guid.NewGuid();
        var draftAppId = Guid.NewGuid();
        db.Applications.AddRange(
            new Application
            {
                Id = verifiedAppId,
                VacancyId = vacancyId,
                CandidateUserId = candidateId,
                CandidateName = "Reset Kandidaat",
                CandidateEmail = email,
                PreferredTransport = "Bike",
                Status = ApplicationStatus.Pending,
                EmailVerifiedAt = DateTime.UtcNow,
                SnapshotAboutMe = "Bio",
                CreatedAt = DateTime.UtcNow
            },
            new Application
            {
                Id = draftAppId,
                VacancyId = vacancyId,
                CandidateUserId = candidateId,
                CandidateName = "Reset Kandidaat",
                CandidateEmail = email,
                PreferredTransport = "Bike",
                Status = ApplicationStatus.Pending,
                CreatedAt = DateTime.UtcNow
            });
        db.CandidateCompetencies.Add(new CandidateCompetency { Id = Guid.NewGuid(), UserId = candidateId });
        db.CandidateOnboardings.Add(new CandidateOnboarding
        {
            Id = Guid.NewGuid(),
            UserId = candidateId,
            CurrentStep = 5,
            CompletedAtUtc = DateTime.UtcNow,
            StartedAtUtc = DateTime.UtcNow.AddDays(-1),
            UpdatedAtUtc = DateTime.UtcNow
        });
        db.VacancyLikes.Add(new VacancyLike { Id = Guid.NewGuid(), UserId = candidateId, VacancyId = vacancyId, CreatedAt = DateTime.UtcNow });
        db.AssessmentResearchAttempts.Add(new AssessmentResearchAttempt
        {
            Id = Guid.NewGuid(),
            UserId = candidateId,
            RespondentHash = "abc",
            TestKey = "competence",
            CompletedAtUtc = DateTime.UtcNow
        });
        db.CandidateCompetencies.Add(new CandidateCompetency { Id = Guid.NewGuid(), UserId = otherId });
        await db.SaveChangesAsync();

        var service = new CandidateProfileResetService(db, new StubUserLookup(db));
        var principal = CreatePrincipal(email);
        await service.ResetAsync(principal, "VERWIJDER", "nl");

        var user = await db.Users.SingleAsync(u => u.Id == candidateId);
        Assert.True(user.IsActive);
        Assert.Equal(email, user.Email);
        Assert.Null(user.PreferencesJson);
        Assert.Null(user.CandidateHowToCompletedAt);
        Assert.Null(user.ResearchConsentAt);
        Assert.False(user.OpenForWork);

        Assert.False(await db.CandidateCompetencies.AnyAsync(c => c.UserId == candidateId));
        Assert.False(await db.CandidateOnboardings.AnyAsync(o => o.UserId == candidateId));
        Assert.False(await db.VacancyLikes.AnyAsync(l => l.UserId == candidateId));
        Assert.False(await db.AssessmentResearchAttempts.AnyAsync(a => a.UserId == candidateId));
        Assert.True(await db.CandidateCompetencies.AnyAsync(c => c.UserId == otherId));

        Assert.False(await db.Applications.AnyAsync(a => a.Id == draftAppId));
        var verified = await db.Applications.SingleAsync(a => a.Id == verifiedAppId);
        Assert.Null(verified.CandidateUserId);
        Assert.Equal("Verwijderde gebruiker", verified.CandidateName);
        Assert.Null(verified.SnapshotAboutMe);

        var log = await db.PlatformLogs.OrderByDescending(l => l.CreatedAt).FirstAsync();
        Assert.Contains("Kandidaat-reset uitgevoerd", log.Message, StringComparison.Ordinal);
        Assert.DoesNotContain(email, log.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Candidate_user_relation_catalog_requires_cleanup_for_every_candidate_scoped_user_fk()
    {
        using var db = CreateDb();
        var required = CandidateUserRelationCatalog.CandidateScopedRelations(db);
        var covered = CandidateUserRelationCatalog.CleanupCoverageEntityTypes;

        var missing = required
            .Where(r => !covered.Contains(r.EntityClrType))
            .Select(r => $"{r.EntityClrType.Name}.{r.ForeignKeyProperty}")
            .ToList();

        Assert.True(
            missing.Count == 0,
            "Add CandidateOwnedDataCleanup handling (and catalog entry) for: "
            + string.Join(", ", missing));
    }

    [Fact]
    public async Task Reset_from_heavily_seeded_mid_onboarding_state_clears_new_tables()
    {
        await using var db = CreateDb();
        var candidateId = Guid.NewGuid();
        var companyId = Guid.NewGuid();
        var vacancyId = Guid.NewGuid();
        var campaignId = Guid.NewGuid();
        var deviceSessionId = Guid.NewGuid();
        var draftAppId = Guid.NewGuid();
        const string email = "heavy-reset@test.nl";

        db.Companies.Add(new Company
        {
            Id = companyId,
            Name = "Reset BV",
            Address = "Straat 1",
            KvkNumber = "12345678",
            Location = new GeoPoint(52, 4),
            VerificationStatus = CompanyVerificationStatus.Verified,
            VerificationMethod = CompanyVerificationMethod.AdminCreated,
            VerifiedAtUtc = DateTime.UtcNow,
            VerificationUpdatedAtUtc = DateTime.UtcNow
        });
        db.Users.Add(new User
        {
            Id = candidateId,
            Email = email,
            FullName = "Heavy Reset",
            Role = UserRole.Candidate,
            IsActive = true,
            PreferencesJson = """{"discovery":true}""",
            OpenForWork = true
        });
        db.Vacancies.Add(new Vacancy
        {
            Id = vacancyId,
            CompanyId = companyId,
            Title = "Vacature",
            Description = "x",
            HourlyWage = 14m,
            StartDate = DateOnly.FromDateTime(DateTime.UtcNow),
            EndDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(14)),
            Status = VacancyStatus.Active,
            Location = new GeoPoint(52, 4),
            RequiredTransport = TransportMode.Bike
        });
        db.Applications.Add(new Application
        {
            Id = draftAppId,
            VacancyId = vacancyId,
            CandidateUserId = candidateId,
            CandidateName = "Heavy Reset",
            CandidateEmail = email,
            PreferredTransport = "Bike",
            Status = ApplicationStatus.Pending,
            CreatedAt = DateTime.UtcNow
        });
        db.ApplicationPlacements.Add(new ApplicationPlacement
        {
            ApplicationId = draftAppId,
            BillingCompanyId = companyId
        });
        db.CandidateCompetencies.Add(new CandidateCompetency
        {
            Id = Guid.NewGuid(),
            UserId = candidateId,
            QuickScanVersion = 3,
            AnswersJson = """{"1":4}"""
        });
        db.CandidateAssessmentAttempts.Add(new CandidateAssessmentAttempt
        {
            Id = Guid.NewGuid(),
            UserId = candidateId,
            Kind = AssessmentKind.Competence,
            Variant = AssessmentVariant.Quick,
            AnswersJson = "{}",
            StartedAtUtc = DateTime.UtcNow
        });
        db.CandidatePrivatePreferences.Add(new CandidatePrivatePreferences
        {
            UserId = candidateId,
            DislikesJson = """["noise"]""",
            CustomDislikesJson = "[]",
            UpdatedAtUtc = DateTime.UtcNow
        });
        db.CandidateOnboardings.Add(new CandidateOnboarding
        {
            Id = Guid.NewGuid(),
            UserId = candidateId,
            CurrentStep = 2,
            StartedAtUtc = DateTime.UtcNow.AddHours(-1),
            UpdatedAtUtc = DateTime.UtcNow
        });
        db.KandidatenkaartInviteCampaigns.Add(new KandidatenkaartInviteCampaign
        {
            Id = campaignId,
            VacancyId = vacancyId,
            CompanyId = companyId,
            AreaKey = "grid-1",
            AreaLabel = "Centrum"
        });
        db.KandidatenkaartInviteDeliveries.Add(new KandidatenkaartInviteDelivery
        {
            Id = Guid.NewGuid(),
            CampaignId = campaignId,
            CandidateUserId = candidateId,
            MatchPercent = 72
        });
        db.CandidateKandidatenkaartSettings.Add(new CandidateKandidatenkaartSettings { UserId = candidateId });
        db.UserDeviceSessions.Add(new UserDeviceSession
        {
            Id = deviceSessionId,
            UserId = candidateId,
            RefreshTokenHash = "hash",
            ExpiresAtUtc = DateTime.UtcNow.AddDays(1)
        });
        db.WebPushSubscriptions.Add(new WebPushSubscription
        {
            Id = Guid.NewGuid(),
            UserId = candidateId,
            DeviceSessionId = deviceSessionId,
            Endpoint = "https://push.example/end",
            P256dh = "p256",
            Auth = "auth"
        });
        var externalVacancyId = Guid.NewGuid();
        db.CandidateExternalVacancies.Add(new CandidateExternalVacancy
        {
            Id = externalVacancyId,
            CandidateUserId = candidateId,
            SourceUrl = "https://example.com/job",
            SourceHost = "example.com",
            SavedAtUtc = DateTime.UtcNow
        });
        db.CandidateExternalVacancyOutbounds.Add(new CandidateExternalVacancyOutbound
        {
            Id = Guid.NewGuid(),
            ExternalVacancyId = externalVacancyId,
            EmployerEmailNormalized = "hr@example.com",
            Motivation = "Interesse",
            InitialSentAtUtc = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        var service = new CandidateProfileResetService(db, new StubUserLookup(db));
        await service.ResetAsync(CreatePrincipal(email), "V E R W I J D E R", "nl");

        Assert.False(await db.CandidateOnboardings.AnyAsync(o => o.UserId == candidateId));
        Assert.False(await db.CandidateCompetencies.AnyAsync(c => c.UserId == candidateId));
        Assert.False(await db.CandidateAssessmentAttempts.AnyAsync(a => a.UserId == candidateId));
        Assert.False(await db.CandidatePrivatePreferences.AnyAsync(p => p.UserId == candidateId));
        Assert.False(await db.KandidatenkaartInviteDeliveries.AnyAsync(d => d.CandidateUserId == candidateId));
        Assert.False(await db.CandidateKandidatenkaartSettings.AnyAsync(s => s.UserId == candidateId));
        Assert.False(await db.WebPushSubscriptions.AnyAsync(s => s.UserId == candidateId));
        Assert.False(await db.UserDeviceSessions.AnyAsync(s => s.UserId == candidateId));
        Assert.False(await db.CandidateExternalVacancies.AnyAsync(v => v.CandidateUserId == candidateId));
        Assert.False(await db.Applications.AnyAsync(a => a.Id == draftAppId));
        var user = await db.Users.SingleAsync(u => u.Id == candidateId);
        Assert.Equal(email, user.Email);
        Assert.True(user.IsActive);
        Assert.Null(user.PreferencesJson);
    }

    [Fact]
    public async Task Reset_removes_candidate_messages_and_after_match_reminder_data()
    {
        await using var db = CreateDb();
        var candidateId = Guid.NewGuid();
        var employerId = Guid.NewGuid();
        var companyId = Guid.NewGuid();
        var vacancyId = Guid.NewGuid();
        var appId = Guid.NewGuid();
        const string email = "after-match-reset@test.nl";

        db.Companies.Add(new Company
        {
            Id = companyId,
            Name = "BV",
            Address = "Straat 1",
            KvkNumber = "12345678",
            Location = new GeoPoint(52, 4),
            VerificationStatus = CompanyVerificationStatus.Verified,
            VerificationMethod = CompanyVerificationMethod.AdminCreated,
            VerifiedAtUtc = DateTime.UtcNow,
            VerificationUpdatedAtUtc = DateTime.UtcNow
        });
        db.Users.AddRange(
            new User
            {
                Id = candidateId,
                Email = email,
                FullName = "Kandidaat",
                Role = UserRole.Candidate,
                IsActive = true
            },
            new User
            {
                Id = employerId,
                Email = "wg@test.nl",
                FullName = "WG",
                Role = UserRole.BranchManager,
                IsActive = true
            });
        db.Vacancies.Add(new Vacancy
        {
            Id = vacancyId,
            CompanyId = companyId,
            Title = "Vacature",
            Description = "x",
            HourlyWage = 14m,
            StartDate = DateOnly.FromDateTime(DateTime.UtcNow),
            EndDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(14)),
            Status = VacancyStatus.Active,
            Location = new GeoPoint(52, 4),
            RequiredTransport = TransportMode.Bike
        });
        db.Applications.Add(new Application
        {
            Id = appId,
            VacancyId = vacancyId,
            CandidateUserId = candidateId,
            CandidateName = "Kandidaat",
            CandidateEmail = email,
            PreferredTransport = "Bike",
            Status = ApplicationStatus.Accepted,
            EmailVerifiedAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow
        });
        db.ApplicationMessages.Add(new ApplicationMessage
        {
            Id = Guid.NewGuid(),
            ApplicationId = appId,
            SenderUserId = candidateId,
            Body = "Hallo werkgever",
            CreatedAtUtc = DateTime.UtcNow
        });
        db.AfterMatchReminderPreferences.Add(new AfterMatchReminderPreference
        {
            UserId = candidateId,
            UpdatedAtUtc = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        var service = new CandidateProfileResetService(db, new StubUserLookup(db));
        await service.ResetAsync(CreatePrincipal(email), "VERWIJDER", "nl");

        Assert.False(await db.ApplicationMessages.AnyAsync(m => m.SenderUserId == candidateId));
        Assert.False(await db.AfterMatchReminderPreferences.AnyAsync(p => p.UserId == candidateId));
        var app = await db.Applications.SingleAsync(a => a.Id == appId);
        Assert.Null(app.CandidateUserId);
    }

    [Fact]
    public async Task After_profile_reset_account_delete_frees_email_for_new_signup()
    {
        await using var db = CreateDb();
        var userId = Guid.NewGuid();
        const string email = "reset-then-delete@test.nl";
        db.Users.Add(new User
        {
            Id = userId,
            Email = email,
            FullName = "Cycle User",
            Role = UserRole.Candidate,
            IsActive = true,
            PreferencesJson = """{"roles":["horeca"]}""",
            OpenForWork = true
        });
        db.LocalAuthCredentials.Add(new LocalAuthCredential
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Email = email,
            PasswordHash = "hash"
        });
        db.CandidateCompetencies.Add(new CandidateCompetency { Id = Guid.NewGuid(), UserId = userId });
        await db.SaveChangesAsync();

        var reset = new CandidateProfileResetService(db, new StubUserLookup(db));
        await reset.ResetAsync(CreatePrincipal(email), "VERWIJDER", "nl");

        var privacy = new PrivacyDataService(db, new StubUserLookup(db), new NoOpMailer(), new AlwaysOnFeatures());
        await privacy.DeleteOrAnonymizeAsync(CreatePrincipal(email));

        var deleted = await db.Users.SingleAsync(u => u.Id == userId);
        Assert.False(deleted.IsActive);
        Assert.StartsWith("deleted-", deleted.Email, StringComparison.Ordinal);

        var newUserId = Guid.NewGuid();
        db.Users.Add(new User
        {
            Id = newUserId,
            Email = email,
            FullName = "Nieuwe Cycle",
            Role = UserRole.Candidate,
            IsActive = true
        });
        await db.SaveChangesAsync();

        Assert.True(await db.Users.AnyAsync(u => u.Id == newUserId && u.Email == email && u.IsActive));
    }

    [Fact]
    public async Task Non_candidate_cannot_reset()
    {
        await using var db = CreateDb();
        var userId = Guid.NewGuid();
        db.Users.Add(new User
        {
            Id = userId,
            Email = "sm@test.nl",
            FullName = "SM",
            Role = UserRole.SalesManager,
            IsActive = true
        });
        await db.SaveChangesAsync();

        var service = new CandidateProfileResetService(db, new StubUserLookup(db));
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.ResetAsync(CreatePrincipal("sm@test.nl"), "VERWIJDER", "nl"));
    }

    private static ClaimsPrincipal CreatePrincipal(string email)
        => new(new ClaimsIdentity([new Claim(ClaimTypes.Email, email)], "test"));

    private static JobsyDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<JobsyDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;
        return new JobsyDbContext(options);
    }

    private sealed class StubUserLookup(JobsyDbContext db) : IUserLookupService
    {
        public Task<User?> FindByPrincipalAsync(ClaimsPrincipal principal, CancellationToken cancellationToken = default)
        {
            var email = principal.FindFirst(ClaimTypes.Email)?.Value;
            return db.Users.FirstOrDefaultAsync(u => u.Email == email && u.IsActive, cancellationToken);
        }
    }

    private sealed class NoOpMailer : ITransactionalMailer
    {
        public Task<EmailSendOutcome> SendAsync(
            ComposedEmail mail,
            string to,
            EmailSendOptions? options = null,
            CancellationToken cancellationToken = default)
            => Task.FromResult(new EmailSendOutcome(Sent: false, Suppressed: false, Reason: "test"));
    }
}

using System.Security.Claims;
using Jobsy.Core.Email;
using Jobsy.Core.Entities;
using Jobsy.Core.Enums;
using Jobsy.Core.Interfaces;
using Jobsy.Core.Privacy;
using Jobsy.Core.ValueObjects;
using Jobsy.Infrastructure.Data;
using Jobsy.Infrastructure.Services;
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
    public void Candidate_user_relation_catalog_requires_profile_reset_coverage_for_every_model_entity()
    {
        using var db = CreateDb();

        var userRelationTypes = db.Model.GetEntityTypes()
            .Where(entity => entity.ClrType != typeof(User)
                && (entity.FindProperty("UserId") is not null
                    || entity.FindProperty("CandidateUserId") is not null))
            .Select(entity => entity.ClrType)
            .OrderBy(type => type.Name)
            .ToArray();

        var resetCoverage = new[]
        {
            typeof(AmbassadeurProfile),
            typeof(Application),
            typeof(AssessmentResearchAttempt),
            typeof(CandidateActionToken),
            typeof(CandidateAssessmentAdjustment),
            typeof(CandidateAssessmentAttempt),
            typeof(CandidateCareerInterest),
            typeof(CandidateCareerGeneration),
            typeof(CandidateCareerPlan),
            typeof(CandidateCareerStepProgress),
            typeof(CandidateCompetency),
            typeof(CandidateCulturePersonalityProfile),
            typeof(CandidateDeepAnalysis),
            typeof(CandidateDiplomaEvaluation),
            typeof(CandidateExternalVacancy),
            typeof(CandidateMatchSnapshot),
            typeof(CandidateOnboarding),
            typeof(CandidateProactiveMatchingSettings),
            typeof(CandidateKandidatenkaartSettings),
            typeof(KandidatenkaartInviteDelivery),
            typeof(CandidateReference),
            typeof(CandidateReminderPreference),
            typeof(ReferenceConfirmation),
            typeof(ReferenceConfirmationConsentLog),
            typeof(ReferenceMisuseReport),
            typeof(WorkExperienceReferenceRequest),
            typeof(WorkExperienceReferenceToken),
            typeof(CandidateRoleFitCheck),
            typeof(CandidateUploadedCv),
            typeof(CandidateVacancyCultureFit),
            typeof(CandidateValuesProfile),
            typeof(CandidateWhoAmIProfile),
            typeof(ComebackReminderLog),
            typeof(ConsumerPurchaseInvoice),
            typeof(CandidatePrivatePreferences),
            typeof(DeepAnalysisCheckout),
            typeof(DeviceLoginHandoff),
            typeof(LocalAuthCredential),
            typeof(MfaTrustedDevice),
            typeof(OneTimeLink),
            typeof(PartnerAffiliateProfile),
            typeof(PassportAccessLog),
            typeof(PassportPartnerCandidateLink),
            typeof(PhoneVerificationChallenge),
            typeof(PlatformFeedback),
            typeof(ProactiveMatchSuggestion),
            typeof(SalesIbanChangePending),
            typeof(SalesManagerProfile),
            typeof(SalesSelfBillingConsent),
            typeof(SiteVisit),
            typeof(TalentContactRequest),
            typeof(TrainingClick),
            typeof(UserCompany),
            typeof(UserDeviceSession),
            typeof(UserExternalLogin),
            typeof(UserNotification),
            typeof(VacancyClick),
            typeof(VacancyLike),
            typeof(VacancySearchImpression),
            typeof(VacancyShare),
            typeof(WebPushSubscription)
        }
        .OrderBy(type => type.Name)
        .ToArray();

        Assert.Equal(userRelationTypes, resetCoverage);
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
}

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
            SnapshotCertificatesJson = """[{"name":"BHV","year":2024}]""",
            SnapshotShowAddressOnCv = true,
            Motivation = "Ik wil graag werken",
            CandidateAgeYears = 28,
            WorkPermitConfirmed = true,
            MatchPercent = 81,
            MatchBreakdownJson = """{"score":81}""",
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
        db.UserNotifications.Add(new UserNotification
        {
            Id = Guid.NewGuid(),
            UserId = candidateId,
            Title = "Test bericht",
            Body = "Persoonlijke notificatie",
            Category = "PushBom",
            ActionUrl = "/candidate/actions/set-unavailable?token=secret",
            CreatedAtUtc = DateTime.UtcNow
        });
            db.CandidateActionTokens.Add(new CandidateActionToken
            {
                Id = Guid.NewGuid(),
                UserId = candidateId,
                Purpose = "SetUnavailable",
                TokenHash = VerificationCodes.Hash("abcdef"),
                ExpiresAtUtc = DateTime.UtcNow.AddDays(7),
                CreatedAtUtc = DateTime.UtcNow
            });
            db.CandidateCompetencies.Add(new CandidateCompetency
            {
                Id = Guid.NewGuid(),
                UserId = candidateId,
                Status = CandidateCompetencyStatuses.Completed,
                AnswersJson = """{"1":5,"2":4}""",
                SamenwerkenPercent = 72,
                ResultaatgerichtheidPercent = 68,
                StressbestendigheidPercent = 55,
                InnovatiePercent = 60,
                CreatedAtUtc = DateTime.UtcNow,
                UpdatedAtUtc = DateTime.UtcNow,
                CompletedAtUtc = DateTime.UtcNow
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
        // Free-text ReasonOther must not appear in platform logs (AVG minimization).
        Assert.DoesNotContain("Ik wil even stoppen met zoeken", requestLog.Message);
        Assert.Contains("toelichting aanwezig", requestLog.Message);
        Assert.Contains("\"HasReasonOther\":true", requestLog.DetailsJson);

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
        Assert.Null(app.SnapshotCertificatesJson);
        Assert.False(app.SnapshotShowAddressOnCv);
        Assert.Null(app.Motivation);
        Assert.Null(app.CandidateAgeYears);
        Assert.False(app.WorkPermitConfirmed);
        Assert.Null(app.MatchPercent);
        Assert.Null(app.MatchBreakdownJson);
        Assert.StartsWith("deleted-", app.CandidateEmail);

        Assert.Equal(0, await db.VacancyLikes.CountAsync(l => l.UserId == candidateId));
        Assert.Equal(0, await db.SiteVisits.CountAsync(v => v.UserId == candidateId));
        Assert.Equal(0, await db.LocalAuthCredentials.CountAsync(c => c.UserId == candidateId));
        Assert.Equal(0, await db.UserNotifications.CountAsync(n => n.UserId == candidateId));
        Assert.Equal(0, await db.CandidateActionTokens.CountAsync(t => t.UserId == candidateId));
        Assert.Equal(0, await db.CandidateCompetencies.CountAsync(c => c.UserId == candidateId));

        var confirmLog = await db.PlatformLogs
            .Where(l => l.Category == "Unsubscribe" && l.Message.Contains("bevestigd"))
            .SingleAsync();
        Assert.Contains("Anders", confirmLog.Message);
        Assert.DoesNotContain("Ik wil even stoppen met zoeken", confirmLog.Message);
        Assert.Contains("toelichting aanwezig", confirmLog.Message);
        Assert.Contains(candidateId.ToString(), confirmLog.Message);
    }

    [Fact]
    public async Task Export_includes_certificate_snapshot_and_address_on_cv_flag()
    {
        await using var db = CreateDb();
        var candidateId = Guid.NewGuid();
        var vacancyId = Guid.NewGuid();
        var companyId = Guid.NewGuid();
        const string email = "export-certs@test.nl";

        db.Companies.Add(new Company
        {
            Id = companyId,
            Name = "Export Co",
            Address = "Straat 1",
            KvkNumber = "999",
            Location = new GeoPoint(52, 4)
        });
        db.Users.Add(new User
        {
            Id = candidateId,
            Email = email,
            FullName = "Export Kandidaat",
            Role = UserRole.Candidate,
            IsActive = true
        });
        db.Vacancies.Add(new Vacancy
        {
            Id = vacancyId,
            CompanyId = companyId,
            Title = "Export vacature",
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
            CandidateName = "Export Kandidaat",
            CandidateEmail = email,
            CandidateAddress = "Voorstraat 1, Naaldwijk",
            CandidateCity = "Naaldwijk",
            PreferredTransport = "Bike",
            Status = ApplicationStatus.Pending,
            SnapshotCertificatesJson = """[{"name":"BHV","year":2024}]""",
            SnapshotShowAddressOnCv = false,
            CreatedAt = DateTime.UtcNow
        });
        db.UserNotifications.Add(new UserNotification
        {
            Id = Guid.NewGuid(),
            UserId = candidateId,
            Title = "Export notificatie",
            Body = "Hallo uit export",
            Category = "ApplicationHired",
            ActionUrl = "/candidate/actions/withdraw-others?hiredApplicationId=11111111-1111-1111-1111-111111111111&token=secret",
            CreatedAtUtc = DateTime.UtcNow
        });
        db.CandidateCompetencies.Add(new CandidateCompetency
        {
            Id = Guid.NewGuid(),
            UserId = candidateId,
            Status = CandidateCompetencyStatuses.Completed,
            AnswersJson = """{"1":4,"6":5}""",
            SamenwerkenPercent = 70,
            ResultaatgerichtheidPercent = 80,
            StressbestendigheidPercent = 60,
            InnovatiePercent = 65,
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow,
            CompletedAtUtc = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        var privacy = CreatePrivacy(db);
        var export = await privacy.ExportAsync(CreatePrincipal(email));
        var json = System.Text.Json.JsonSerializer.Serialize(export);

        Assert.Contains("SnapshotCertificatesJson", json);
        Assert.Contains("BHV", json);
        Assert.Contains("SnapshotShowAddressOnCv", json);
        Assert.Contains("CandidateAddress", json);
        Assert.Contains("Voorstraat 1", json);
        Assert.Contains("CandidateName", json);
        Assert.Contains("Export Kandidaat", json);
        Assert.Contains("Notifications", json);
        Assert.Contains("Export notificatie", json);
        Assert.Contains("Competencies", json);
        Assert.Contains("SamenwerkenPercent", json);
        Assert.Contains("CareerInterests", json);
        Assert.Contains("RoleFitChecks", json);
        Assert.Contains("DeepAnalysis", json);
        Assert.Contains("TalentContactRequests", json);
        Assert.DoesNotContain("token=secret", json);
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

    [Fact]
    public void Candidate_user_relation_catalog_requires_anonymize_coverage_for_every_model_entity()
    {
        using var db = CreateDb();

        var userRelationTypes = db.Model.GetEntityTypes()
            .Where(entity => entity.ClrType != typeof(User)
                && (entity.FindProperty("UserId") is not null
                    || entity.FindProperty("CandidateUserId") is not null))
            .Select(entity => entity.ClrType)
            .OrderBy(type => type.Name)
            .ToArray();

        // This catalog is intentionally exhaustive: additions to the EF model must be
        // explicitly reviewed and added to PrivacyDataService.AnonymizeUserAsync.
        var anonymizeCoverage = new[]
        {
            typeof(AmbassadeurProfile),
            typeof(Application),
            typeof(CandidateActionToken),
            typeof(CandidateCareerInterest),
            typeof(CandidateCareerPlan),
            typeof(CandidateCareerStepProgress),
            typeof(CandidateCompetency),
            typeof(CandidateCulturePersonalityProfile),
            typeof(CandidateDeepAnalysis),
            typeof(CandidateMatchSnapshot),
            typeof(CandidateOnboarding),
            typeof(CandidateReference),
            typeof(CandidateRoleFitCheck),
            typeof(CandidateUploadedCv),
            typeof(CandidateVacancyCultureFit),
            typeof(CandidateValuesProfile),
            typeof(CandidateWhoAmIProfile),
            typeof(DeepAnalysisCheckout),
            typeof(DeviceLoginHandoff),
            typeof(LocalAuthCredential),
            typeof(PartnerAffiliateProfile),
            typeof(PlatformFeedback),
            typeof(SalesManagerProfile),
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

        Assert.Equal(userRelationTypes, anonymizeCoverage);
    }

    [Fact]
    public async Task Delete_removes_candidate_data_sessions_and_identity_bindings()
    {
        await using var db = CreateDb();
        var userId = Guid.NewGuid();
        var companyId = Guid.NewGuid();
        var vacancyId = Guid.NewGuid();
        const string email = "privacy-coverage@test.nl";

        db.Users.Add(new User
        {
            Id = userId,
            Email = email,
            FullName = "Privacy kandidaat",
            Role = UserRole.Candidate,
            IsActive = true,
            ReferredByAmbassadeurUserId = Guid.NewGuid(),
            ReferredByAmbassadeurTrackingCode = "AM-PRIVATE"
        });
        db.Companies.Add(new Company
        {
            Id = companyId,
            Name = "Privacy BV",
            KvkNumber = "12345678",
            Address = "Straat 1",
            Location = new GeoPoint(52, 4),
            ReferredByAmbassadeurUserId = userId,
            CommissionAmbassadeurRateSnapshot = 0.05m
        });
        db.Vacancies.Add(new Vacancy
        {
            Id = vacancyId,
            CompanyId = companyId,
            Title = "Privacy vacature",
            Description = "x",
            HourlyWage = 14m,
            StartDate = DateOnly.FromDateTime(DateTime.UtcNow),
            EndDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1)),
            Status = VacancyStatus.Active,
            Location = new GeoPoint(52, 4),
            RequiredTransport = TransportMode.Bike
        });
        db.Applications.Add(new Application
        {
            Id = Guid.NewGuid(),
            VacancyId = vacancyId,
            CandidateUserId = userId,
            CandidateName = "Privacy kandidaat",
            CandidateEmail = email,
            PreferredTransport = "Bike",
            Status = ApplicationStatus.Pending,
            SnapshotWhoAmIJson = """{"story":"privé"}""",
            CreatedAt = DateTime.UtcNow
        });
        var planId = Guid.NewGuid();
        var deviceSessionId = Guid.NewGuid();
        db.CandidateValuesProfiles.Add(new CandidateValuesProfile { Id = Guid.NewGuid(), UserId = userId });
        db.CandidateCareerPlans.Add(new CandidateCareerPlan
        {
            Id = planId,
            UserId = userId,
            DreamTitle = "Verpleegkundige",
            DreamKey = "verpleegkundige",
            MatchSummary = "Past goed"
        });
        db.CandidateCareerStepProgress.Add(new CandidateCareerStepProgress
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            PlanId = planId,
            StepKey = "start"
        });
        db.CandidateVacancyCultureFits.Add(new CandidateVacancyCultureFit
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            VacancyId = vacancyId
        });
        db.CandidateMatchSnapshots.Add(new CandidateMatchSnapshot { Id = Guid.NewGuid(), UserId = userId });
        db.CandidateOnboardings.Add(new CandidateOnboarding { Id = Guid.NewGuid(), UserId = userId });
        db.CandidateWhoAmIProfiles.Add(new CandidateWhoAmIProfile { Id = Guid.NewGuid(), UserId = userId });
        db.UserExternalLogins.Add(new UserExternalLogin
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Provider = "google",
            ProviderSubject = "subject-private"
        });
        db.UserDeviceSessions.Add(new UserDeviceSession
        {
            Id = deviceSessionId,
            UserId = userId,
            RefreshTokenHash = "refresh-secret",
            ExpiresAtUtc = DateTime.UtcNow.AddDays(1)
        });
        db.DeviceLoginHandoffs.Add(new DeviceLoginHandoff
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            CodeHash = "handoff-secret",
            ExpiresAtUtc = DateTime.UtcNow.AddMinutes(5)
        });
        db.WebPushSubscriptions.Add(new WebPushSubscription
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            DeviceSessionId = deviceSessionId,
            Endpoint = "https://push.example/private",
            P256dh = "p256dh-secret",
            Auth = "auth-secret"
        });
        await db.SaveChangesAsync();

        await CreatePrivacy(db).DeleteOrAnonymizeAsync(CreatePrincipal(email));

        Assert.False(await db.CandidateValuesProfiles.AnyAsync(row => row.UserId == userId));
        Assert.False(await db.CandidateCareerPlans.AnyAsync(row => row.UserId == userId));
        Assert.False(await db.CandidateCareerStepProgress.AnyAsync(row => row.UserId == userId));
        Assert.False(await db.CandidateVacancyCultureFits.AnyAsync(row => row.UserId == userId));
        Assert.False(await db.CandidateMatchSnapshots.AnyAsync(row => row.UserId == userId));
        Assert.False(await db.CandidateOnboardings.AnyAsync(row => row.UserId == userId));
        Assert.False(await db.CandidateWhoAmIProfiles.AnyAsync(row => row.UserId == userId));
        Assert.False(await db.UserExternalLogins.AnyAsync(row => row.UserId == userId));
        Assert.False(await db.UserDeviceSessions.AnyAsync(row => row.UserId == userId));
        Assert.False(await db.DeviceLoginHandoffs.AnyAsync(row => row.UserId == userId));
        Assert.False(await db.WebPushSubscriptions.AnyAsync(row => row.UserId == userId));

        var application = await db.Applications.SingleAsync();
        Assert.Null(application.CandidateUserId);
        Assert.Null(application.SnapshotWhoAmIJson);
        var user = await db.Users.SingleAsync(row => row.Id == userId);
        Assert.Null(user.ReferredByAmbassadeurUserId);
        Assert.Null(user.ReferredByAmbassadeurTrackingCode);
        var company = await db.Companies.SingleAsync(row => row.Id == companyId);
        Assert.Null(company.ReferredByAmbassadeurUserId);
        Assert.Null(company.CommissionAmbassadeurRateSnapshot);
    }

    [Fact]
    public async Task Export_includes_new_dutch_privacy_sections_without_credentials()
    {
        await using var db = CreateDb();
        var userId = Guid.NewGuid();
        var vacancyId = Guid.NewGuid();
        const string email = "export-privacy@test.nl";
        db.Users.Add(new User
        {
            Id = userId,
            Email = email,
            FullName = "Export privacy",
            Role = UserRole.Candidate,
            IsActive = true
        });
        db.Applications.Add(new Application
        {
            Id = Guid.NewGuid(),
            VacancyId = vacancyId,
            CandidateUserId = userId,
            CandidateName = "Export privacy",
            CandidateEmail = email,
            PreferredTransport = "Bike",
            SnapshotWhoAmIJson = """{"story":"Mijn verhaal"}"""
        });
        var planId = Guid.NewGuid();
        db.CandidateValuesProfiles.Add(new CandidateValuesProfile
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            AnswersJson = """{"waarde":"autonomie"}"""
        });
        db.CandidateCareerPlans.Add(new CandidateCareerPlan
        {
            Id = planId,
            UserId = userId,
            DreamTitle = "Zorgverlener",
            DreamKey = "zorgverlener",
            MatchSummary = "Sterke match"
        });
        db.CandidateCareerStepProgress.Add(new CandidateCareerStepProgress
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            PlanId = planId,
            StepKey = "opleiding"
        });
        db.CandidateVacancyCultureFits.Add(new CandidateVacancyCultureFit
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            VacancyId = vacancyId,
            ResultJson = """{"fit":88}"""
        });
        db.CandidateMatchSnapshots.Add(new CandidateMatchSnapshot
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            MatchesJson = """[{"vacancy":"zorg"}]"""
        });
        var deviceSessionId = Guid.NewGuid();
        db.UserDeviceSessions.Add(new UserDeviceSession
        {
            Id = deviceSessionId,
            UserId = userId,
            RefreshTokenHash = "refresh-secret",
            ExpiresAtUtc = DateTime.UtcNow.AddDays(1),
            DeviceName = "Test device"
        });
        db.WebPushSubscriptions.Add(new WebPushSubscription
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            DeviceSessionId = deviceSessionId,
            Endpoint = "https://push.example/private",
            P256dh = "p256dh-secret",
            Auth = "auth-secret"
        });
        db.UserExternalLogins.Add(new UserExternalLogin
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Provider = "google",
            ProviderSubject = "subject-private",
            EmailAtLink = "linked-private@test.nl"
        });
        db.CandidateWhoAmIProfiles.Add(new CandidateWhoAmIProfile
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            StoryText = "Mijn verhaal"
        });
        await db.SaveChangesAsync();

        var export = await CreatePrivacy(db).ExportAsync(CreatePrincipal(email));
        var json = System.Text.Json.JsonSerializer.Serialize(export);

        Assert.Contains("Waardenprofielen", json);
        Assert.Contains("Loopbaanplannen", json);
        Assert.Contains("Cultuurfits", json);
        Assert.Contains("Matchmomentopnamen", json);
        Assert.Contains("Apparaatsessies", json);
        Assert.Contains("Pushabonnementen", json);
        Assert.Contains("ExterneAanmeldingen", json);
        Assert.Contains("WieBenIkMomentopnamen", json);
        Assert.Contains("google", json);
        Assert.Contains("Mijn verhaal", json);
        Assert.DoesNotContain("refresh-secret", json);
        Assert.DoesNotContain("p256dh-secret", json);
        Assert.DoesNotContain("auth-secret", json);
        Assert.DoesNotContain("push.example", json);
        Assert.DoesNotContain("subject-private", json);
        Assert.DoesNotContain("linked-private@test.nl", json);
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
        var match = System.Text.RegularExpressions.Regex.Match(
            html,
            @"data-lobsy-otp=""(\d{6})""");
        if (!match.Success)
        {
            match = System.Text.RegularExpressions.Regex.Match(html, @"\b(\d{6})\b");
        }
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

        public Task<EmailDeliveryResult> SendAsync(EmailMessage message, CancellationToken cancellationToken = default)
        {
            Messages.Add(message);
            return Task.FromResult(EmailDeliveryResult.Provider);
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

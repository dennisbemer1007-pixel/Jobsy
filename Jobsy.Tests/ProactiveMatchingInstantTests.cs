using Jobsy.Core.Contracts;
using Jobsy.Core.Email;
using Jobsy.Core.Email.Model;
using Jobsy.Core.Entities;
using Jobsy.Core.Enums;
using Jobsy.Core.Features;
using Jobsy.Core.Interfaces;
using Jobsy.Core.Options;
using Jobsy.Core.Privacy;
using Jobsy.Core.Rules;
using Jobsy.Core.ValueObjects;
using Jobsy.Infrastructure.Data;
using Jobsy.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Jobsy.Tests;

public sealed class ProactiveMatchingInstantTests
{
    [Fact]
    public async Task Instant_matching_delivers_notification_when_flag_on_and_match_strong()
    {
        await using var db = CreateDb();
        var (vacancyId, candidateId) = await SeedEligibleCandidateAndVacancyAsync(db);
        var flags = new StubFlags(proactiveOn: true);
        var matches = new StubMatchService(vacancyId, scorePercent: 88);
        var service = CreateService(db, flags, matches);

        var sent = await service.RunInstantMatchingForVacancyAsync(vacancyId);

        Assert.Equal(1, sent);
        var suggestion = await db.ProactiveMatchSuggestions.SingleAsync();
        Assert.Equal(candidateId, suggestion.CandidateUserId);
        Assert.Equal(vacancyId, suggestion.VacancyId);
        var notification = await db.UserNotifications.SingleAsync();
        Assert.Equal(candidateId, notification.UserId);
        Assert.Contains("Nieuwe match:", notification.Title, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Daily_run_does_not_duplicate_after_instant_delivery()
    {
        await using var db = CreateDb();
        var (vacancyId, candidateId) = await SeedEligibleCandidateAndVacancyAsync(db);
        var flags = new StubFlags(proactiveOn: true);
        var matches = new StubMatchService(vacancyId, scorePercent: 88);
        var service = CreateService(db, flags, matches);

        var instant = await service.RunInstantMatchingForVacancyAsync(vacancyId);
        var daily = await service.RunDailyMatchingAsync();

        Assert.Equal(1, instant);
        Assert.Equal(0, daily);
        Assert.Single(await db.ProactiveMatchSuggestions.Where(s => s.CandidateUserId == candidateId).ToListAsync());
        Assert.Single(await db.UserNotifications.Where(n => n.UserId == candidateId).ToListAsync());
    }

    [Fact]
    public async Task Instant_matching_does_nothing_when_flag_off()
    {
        await using var db = CreateDb();
        var (vacancyId, _) = await SeedEligibleCandidateAndVacancyAsync(db);
        var flags = new StubFlags(proactiveOn: false);
        var matches = new StubMatchService(vacancyId, scorePercent: 88);
        var service = CreateService(db, flags, matches);

        var sent = await service.RunInstantMatchingForVacancyAsync(vacancyId);

        Assert.Equal(0, sent);
        Assert.Empty(await db.ProactiveMatchSuggestions.ToListAsync());
        Assert.Empty(await db.UserNotifications.ToListAsync());
    }

    [Fact]
    public void Publish_success_enqueues_instant_matching_when_queue_wired()
    {
        var queue = new ProactiveVacancyInstantMatchQueue();
        var vacancyId = Guid.NewGuid();
        var outcome = new VacancyProductOutcome(
            true,
            null,
            new Vacancy { Id = vacancyId, Status = VacancyStatus.Active });

        var indexed = InvokeIndexed(outcome, queue);

        Assert.True(indexed.Succeeded);
        Assert.False(queue.TryEnqueue(vacancyId));
    }

    private static VacancyProductOutcome InvokeIndexed(
        VacancyProductOutcome outcome,
        IProactiveVacancyInstantMatchQueue queue)
    {
        var service = new VacancyProductService(
            db: null!,
            tokens: null!,
            salesCommercial: null!,
            categories: null!,
            push: null!,
            mailer: null!,
            features: null!,
            routing: null!,
            notifications: null!,
            actionTokens: null!,
            logger: NullLogger<VacancyProductService>.Instance,
            instantProactiveMatch: queue);

        var method = typeof(VacancyProductService).GetMethod(
            "Indexed",
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
        return (VacancyProductOutcome)method!.Invoke(service, [outcome])!;
    }

    private static ProactiveMatchingService CreateService(
        JobsyDbContext db,
        IFeatureFlags flags,
        IProfileVacancyMatchService matches)
        => new(
            db,
            matches,
            new StubDiscovery(db),
            new UserNotificationService(db),
            new NoMail(),
            flags,
            Options.Create(new ProactiveMatchingOptions
            {
                MinScorePercent = 80,
                MaxNotificationsPerWeek = 7,
                SameVacancyCooldownDays = 30
            }),
            NullLogger<ProactiveMatchingService>.Instance);

    private static async Task<(Guid VacancyId, Guid CandidateId)> SeedEligibleCandidateAndVacancyAsync(
        JobsyDbContext db)
    {
        var companyId = Guid.NewGuid();
        db.Companies.Add(new Company
        {
            Id = companyId,
            Name = "Westland Bloemen",
            KvkNumber = "12345678",
            Address = "Westland 1",
            Location = new GeoPoint(52.0, 4.3),
            KvkVerificationStatus = KvkVerificationStatus.Verified,
            VerificationStatus = CompanyVerificationStatus.Verified
        });

        var vacancyId = Guid.NewGuid();
        db.Vacancies.Add(new Vacancy
        {
            Id = vacancyId,
            CompanyId = companyId,
            Title = "Orderpicker",
            Description = "Pick orders in the warehouse.",
            Status = VacancyStatus.Active,
            HourlyWage = 15m,
            StartDate = DateOnly.FromDateTime(DateTime.UtcNow),
            EndDate = DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(2)),
            Location = new GeoPoint(52.0, 4.3),
            RequiredTransport = TransportMode.Bike,
            ContentModerationPassed = true
        });

        var candidateId = Guid.NewGuid();
        var user = new User
        {
            Id = candidateId,
            Email = "kandidaat@test.local",
            Role = UserRole.Candidate,
            IsActive = true,
            OpenForWork = true,
            TalentPoolConsentAt = DateTime.UtcNow,
            DateOfBirth = DateOnly.FromDateTime(DateTime.UtcNow.AddYears(-25))
        };
        user.TalentPoolConsentVersion = PrivacyConstants.CandidateProfilingConsentVersion;
        db.Users.Add(user);

        db.CandidateProactiveMatchingSettings.Add(new CandidateProactiveMatchingSettings
        {
            UserId = candidateId,
            Enabled = true,
            ConsentAcceptedAtUtc = DateTime.UtcNow,
            ConsentVersion = ProactiveMatchingRules.ConsentVersion,
            Frequency = ProactiveMatchingFrequency.Daily
        });

        await db.SaveChangesAsync();
        return (vacancyId, candidateId);
    }

    private static JobsyDbContext CreateDb()
        => new(new DbContextOptionsBuilder<JobsyDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options);

    private sealed class StubFlags(bool proactiveOn) : IFeatureFlags
    {
        public ValueTask<FeatureFlagSnapshot> GetAsync(CancellationToken cancellationToken = default)
            => ValueTask.FromResult(new FeatureFlagSnapshot(
                EmployersEnabled: true,
                CandidatePassportEnabled: true,
                ProactiveMatchingEnabled: proactiveOn));

        public async ValueTask<bool> IsEnabledAsync(PlatformFeature feature, CancellationToken cancellationToken = default)
            => (await GetAsync(cancellationToken)).IsEnabled(feature);

        public void Invalidate()
        {
        }
    }

    private sealed class StubMatchService(Guid vacancyId, int scorePercent) : IProfileVacancyMatchService
    {
        public Task<ProfileVacancyMatchContext?> TryLoadForPrincipalAsync(
            System.Security.Claims.ClaimsPrincipal user,
            CancellationToken cancellationToken = default)
            => Task.FromResult<ProfileVacancyMatchContext?>(MinimalContext());

        public Task<ProfileVacancyMatchContext?> TryLoadForUserIdAsync(
            Guid userId,
            CancellationToken cancellationToken = default)
            => Task.FromResult<ProfileVacancyMatchContext?>(MinimalContext());

        private static ProfileVacancyMatchContext MinimalContext()
            => new() { Prefs = new CandidatePreferencesDto([], null, null) };

        public Task<IReadOnlyDictionary<Guid, ProfileVacancyMatch>> ScoreAsync(
            ProfileVacancyMatchContext context,
            IEnumerable<(VacancyDiscoveryRecord Record, int? TravelMinutes)> vacancies,
            CancellationToken cancellationToken = default)
        {
            var match = new ProfileVacancyMatch
            {
                VacancyId = vacancyId,
                TotalPercent = scorePercent,
                Core = new MatchScoreBreakdown { TotalPercent = scorePercent, LegalEligible = true }
            };
            return Task.FromResult<IReadOnlyDictionary<Guid, ProfileVacancyMatch>>(
                new Dictionary<Guid, ProfileVacancyMatch> { [vacancyId] = match });
        }
    }

    private sealed class StubDiscovery(JobsyDbContext db) : IVacancyDiscoveryIndex
    {
        public DateTime? LastRefreshedAtUtc => null;

        public void Invalidate()
        {
        }

        public Task InvalidateCompanyAsync(Guid companyId, CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public Task RefreshAsync(CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public async Task<IReadOnlyList<VacancyDiscoveryRecord>> GetActiveAsync(CancellationToken cancellationToken = default)
        {
            var rows = await db.Vacancies.AsNoTracking()
                .Include(v => v.Company)
                .Where(v => v.Status == VacancyStatus.Active)
                .ToListAsync(cancellationToken);
            return rows.Select(VacancyDiscoveryIndex.ToRecord).ToList();
        }

        public Task<VacancyMapView> GetMapViewAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(new VacancyMapView(52.0, 4.3, 10, 1));
    }

    private sealed class NoMail : ITransactionalMailer
    {
        public Task<EmailSendOutcome> SendAsync(
            ComposedEmail mail,
            string to,
            EmailSendOptions? options = null,
            CancellationToken cancellationToken = default)
            => Task.FromResult(new EmailSendOutcome(true, false, null));
    }
}

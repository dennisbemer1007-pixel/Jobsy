using System.Net;
using System.Security.Claims;
using Jobsy.Core.Contracts;
using Jobsy.Core.Entities;
using Jobsy.Core.Enums;
using Jobsy.Core.Features;
using Jobsy.Core.Interfaces;
using Jobsy.Core.Rules;
using Jobsy.Infrastructure.Data;
using Jobsy.Infrastructure.Services;
using Jobsy.Web.Services;
using Microsoft.EntityFrameworkCore;

namespace Jobsy.Tests;

public class CandidateOnboardingStep1SaveTests
{
    [Fact]
    public async Task After_profile_reset_onboarding_row_is_recreated_and_progress_save_succeeds()
    {
        await using var db = CreateDb();
        var userId = Guid.NewGuid();
        const string email = "onboarding-after-reset@test.nl";
        db.Users.Add(new User
        {
            Id = userId,
            Email = email,
            FullName = "Dennis Bemer",
            FirstName = "Dennis",
            LastName = "Bemer",
            Role = UserRole.Candidate,
            IsActive = true,
            DateOfBirth = new DateOnly(1990, 1, 1),
            PreferencesJson = """{"maxTravelMinutes":30}""",
            CandidateHowToCompletedAt = DateTime.UtcNow
        });
        db.CandidateOnboardings.Add(new CandidateOnboarding
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            CurrentStep = 8,
            CompletedAtUtc = null,
            StartedAtUtc = DateTime.UtcNow.AddDays(-2),
            UpdatedAtUtc = DateTime.UtcNow
        });
        db.CandidateCompetencies.Add(new CandidateCompetency { Id = Guid.NewGuid(), UserId = userId });
        await db.SaveChangesAsync();

        var reset = new CandidateProfileResetService(db, new StubUserLookup(db));
        await reset.ResetAsync(
            new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.Email, email)], "test")),
            "VERWIJDER",
            "nl");

        Assert.False(await db.CandidateOnboardings.AnyAsync(o => o.UserId == userId));

        var onboarding = CreateOnboardingService(db);
        var state = await onboarding.GetAsync(userId);
        Assert.Equal(1, state.CurrentStep);
        Assert.True(state.ShouldShow);

        state = await onboarding.SaveProgressAsync(
            userId,
            new CandidateOnboardingProgressRequest(1, StepCompleted: false));
        Assert.Equal(1, state.CurrentStep);

        var user = await db.Users.SingleAsync(u => u.Id == userId);
        user.FirstName = "Dennis";
        user.LastName = "Bemer";
        user.PreferencesJson = """{"maxTravelMinutes":30,"preferredTransport":"Fiets"}""";
        await db.SaveChangesAsync();

        Assert.Null(user.DateOfBirth);
        Assert.Null(user.CandidateHowToCompletedAt);
    }

    [Fact]
    public async Task Fresh_signup_onboarding_step1_progress_save_succeeds()
    {
        await using var db = CreateDb();
        var userId = Guid.NewGuid();
        db.Users.Add(new User
        {
            Id = userId,
            Email = "fresh-signup@test.nl",
            FullName = "Dennis Bemer",
            FirstName = "Dennis",
            LastName = "Bemer",
            Role = UserRole.Candidate,
            IsActive = true
        });
        await db.SaveChangesAsync();

        var onboarding = CreateOnboardingService(db);
        var state = await onboarding.GetAsync(userId);
        Assert.Equal(1, state.CurrentStep);
        Assert.True(state.ShouldShow);

        state = await onboarding.SaveProgressAsync(
            userId,
            new CandidateOnboardingProgressRequest(1, StepCompleted: false));
        Assert.Equal(1, state.CurrentStep);
        Assert.True(await db.CandidateOnboardings.AnyAsync(o => o.UserId == userId));
    }

    [Fact]
    public void Http_status_errors_are_not_shown_as_network_outage()
    {
        var ex = new HttpRequestException("bad", inner: null, statusCode: HttpStatusCode.BadRequest);
        Assert.Equal("Common.Error.Validation", UserFacingError.MessageKeyFor(ex));
    }

    [Fact]
    public void Connection_failures_without_status_remain_network_errors()
    {
        var ex = new HttpRequestException("connection reset");
        Assert.Equal("Common.Error.Network", UserFacingError.MessageKeyFor(ex));
    }

    private static CandidateOnboardingService CreateOnboardingService(JobsyDbContext db)
        => new(db, new FakeMatches(), new FakeInsightsQueue(), new FakeCareerPlans(), new FixedFlags(true), new FakeDiscovery());

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

    private sealed class FixedFlags(bool passport) : IFeatureFlags
    {
        public ValueTask<FeatureFlagSnapshot> GetAsync(CancellationToken cancellationToken = default)
            => ValueTask.FromResult(new FeatureFlagSnapshot(true, passport));

        public ValueTask<bool> IsEnabledAsync(PlatformFeature feature, CancellationToken cancellationToken = default)
            => ValueTask.FromResult(feature == PlatformFeature.CandidatePassport ? passport : true);

        public void Invalidate() { }
    }

    private sealed class FakeDiscovery : IVacancyDiscoveryIndex
    {
        public DateTime? LastRefreshedAtUtc => DateTime.UtcNow;
        public Task<IReadOnlyList<VacancyDiscoveryRecord>> GetActiveAsync(CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<VacancyDiscoveryRecord>>([]);

        public Task<VacancyDiscoveryRecord?> FindActiveByIdAsync(Guid id, CancellationToken cancellationToken = default)
            => Task.FromResult<VacancyDiscoveryRecord?>(null);

        public Task<VacancyMapView> GetMapViewAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(new VacancyMapView(52.1, 5.1, 8, 0));
        public void Invalidate() { }
        public Task InvalidateCompanyAsync(Guid companyId, CancellationToken cancellationToken = default)
            => Task.CompletedTask;
        public Task RefreshAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    private sealed class FakeMatches : ICandidateMatchSnapshotService
    {
        public Task<(IReadOnlyList<CandidateMatchedVacancyDto> Matches, string InsightsStatus)> GetAsync(
            Guid userId, CancellationToken cancellationToken = default)
            => Task.FromResult<(IReadOnlyList<CandidateMatchedVacancyDto>, string)>(([], "ready"));
        public Task SaveComputedAsync(Guid userId, IReadOnlyList<CandidateMatchedVacancyDto> matches, string inputFingerprint, CancellationToken cancellationToken = default)
            => Task.CompletedTask;
        public Task<string> ComputeInputFingerprintAsync(Guid userId, CancellationToken cancellationToken = default)
            => Task.FromResult("");
        public Task<IReadOnlyList<CandidateMatchedVacancyDto>> ComputeLiveAsync(Guid userId, CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<CandidateMatchedVacancyDto>>([]);
        public void InvalidateContextCache(Guid userId) { }
        public Task MarkInputsStaleAsync(Guid userId, CancellationToken cancellationToken = default)
            => Task.CompletedTask;
    }

    private sealed class FakeInsightsQueue : ICandidateInsightsQueue
    {
        public bool TryEnqueue(Guid userId) => true;
        public ValueTask<Guid> DequeueAsync(CancellationToken cancellationToken) => ValueTask.FromResult(Guid.Empty);
        public void MarkCompleted(Guid userId) { }
    }

    private sealed class FakeCareerPlans : ICandidateCareerPlanService
    {
        public Task<HorizonCareerPathPlanView?> GetAsync(Guid userId, CancellationToken cancellationToken = default)
            => Task.FromResult<HorizonCareerPathPlanView?>(null);
        public Task SaveDreamAsync(Guid userId, string? dreamTitle, CancellationToken cancellationToken = default)
            => Task.CompletedTask;
        public Task ClearDreamAsync(Guid userId, CancellationToken cancellationToken = default)
            => Task.CompletedTask;
        public Task<string?> GetDreamTitleAsync(Guid userId, CancellationToken cancellationToken = default)
            => Task.FromResult<string?>(null);
        public Task EnqueueGenerationIfNeededAsync(Guid userId, CancellationToken cancellationToken = default)
            => Task.CompletedTask;
        public Task TryGeneratePendingAsync(Guid userId, HorizonCareerProfileSnapshot snapshot, CancellationToken cancellationToken = default)
            => Task.CompletedTask;
        public Task<HorizonCareerPathPlanView> GenerateAndSaveAsync(
            Guid userId, string? dreamTitle, HorizonCareerProfileSnapshot snapshot, string? catalogKey = null,
            string? dreamSource = null, string? planLanguage = null, bool force = false, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();
        public Task<HorizonCareerPathPlanView?> CompleteStepAsync(Guid userId, string stepKey, CancellationToken cancellationToken = default)
            => Task.FromResult<HorizonCareerPathPlanView?>(null);
        public Task<HorizonCareerPathPlanView?> UncompleteStepAsync(Guid userId, string stepKey, CancellationToken cancellationToken = default)
            => Task.FromResult<HorizonCareerPathPlanView?>(null);
        public Task<IReadOnlyList<ArchivedCareerPlanView>> ListArchivedAsync(Guid userId, CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<ArchivedCareerPlanView>>([]);
        public Task<HorizonCareerPathPlanView?> RestoreArchivedAsync(Guid userId, Guid planId, CancellationToken cancellationToken = default)
            => Task.FromResult<HorizonCareerPathPlanView?>(null);
        public Task<CareerDreamOptionsView> GetDreamOptionsAsync(Guid userId, string? query, CancellationToken cancellationToken = default)
            => Task.FromResult(new CareerDreamOptionsView([], []));
    }
}

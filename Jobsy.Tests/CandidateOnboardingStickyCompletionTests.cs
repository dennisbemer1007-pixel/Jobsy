using Jobsy.Core.Contracts;
using Jobsy.Core.Entities;
using Jobsy.Core.Enums;
using Jobsy.Core.Features;
using Jobsy.Core.Interfaces;
using Jobsy.Core.Rules;
using Jobsy.Infrastructure.Data;
using Jobsy.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;

namespace Jobsy.Tests;

public sealed class CandidateOnboardingStickyCompletionTests
{
    [Fact]
    public async Task SaveProgress_after_finish_does_not_clear_finish_reached_when_step_pointer_moves_back()
    {
        await using var db = CreateDb();
        var userId = Guid.NewGuid();
        db.Users.Add(new User
        {
            Id = userId,
            Email = "sticky-finish@test.local",
            FullName = "Sticky Finish",
            Role = UserRole.Candidate,
            IsActive = true,
            TestAiConsentAt = DateTime.UtcNow,
        });
        var stepsJson = OnboardingStepAnalytics.Serialize(
            Enumerable.Range(1, 10)
                .Select(s => new OnboardingStepAnalytics.StepEvent(
                    s,
                    StartedAtUtc: DateTime.UtcNow.AddDays(-1),
                    CompletedAtUtc: DateTime.UtcNow.AddDays(-1)))
                .ToList());
        db.CandidateOnboardings.Add(new CandidateOnboarding
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            CurrentStep = 10,
            FinishReached = true,
            WizardVersion = OnboardingWizardCatalog.WizardVersionV3,
            StepsJson = stepsJson,
            StartedAtUtc = DateTime.UtcNow.AddDays(-2),
            UpdatedAtUtc = DateTime.UtcNow.AddDays(-1),
        });
        await db.SaveChangesAsync();

        var svc = CreateService(db);
        var after = await svc.SaveProgressAsync(
            userId,
            new CandidateOnboardingProgressRequest(2, StepCompleted: false));

        Assert.True(after.FinishReached);
        var row = await db.CandidateOnboardings.SingleAsync(o => o.UserId == userId);
        Assert.True(row.FinishReached);
        Assert.Equal(2, row.CurrentStep);
    }

    [Fact]
    public async Task GetAsync_rehydrates_finish_reached_when_all_steps_are_done_but_flag_was_lost()
    {
        await using var db = CreateDb();
        var userId = Guid.NewGuid();
        db.Users.Add(new User
        {
            Id = userId,
            Email = "rehydrate-finish@test.local",
            FullName = "Rehydrate",
            Role = UserRole.Candidate,
            IsActive = true,
        });
        var stepsJson = OnboardingStepAnalytics.Serialize(
            Enumerable.Range(1, 10)
                .Select(s => new OnboardingStepAnalytics.StepEvent(
                    s,
                    StartedAtUtc: DateTime.UtcNow.AddDays(-1),
                    CompletedAtUtc: DateTime.UtcNow.AddDays(-1)))
                .ToList());
        db.CandidateOnboardings.Add(new CandidateOnboarding
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            CurrentStep = 2,
            FinishReached = false,
            WizardVersion = OnboardingWizardCatalog.WizardVersionV3,
            StepsJson = stepsJson,
            StartedAtUtc = DateTime.UtcNow.AddDays(-2),
            UpdatedAtUtc = DateTime.UtcNow.AddDays(-1),
        });
        await db.SaveChangesAsync();

        var svc = CreateService(db);
        var state = await svc.GetAsync(userId);

        Assert.True(state.FinishReached);
        var row = await db.CandidateOnboardings.SingleAsync(o => o.UserId == userId);
        Assert.True(row.FinishReached);
    }

    private static CandidateOnboardingService CreateService(JobsyDbContext db)
        => new(db, new FakeMatches(), new FakeInsightsQueue(), new FakeCareerPlans(), new FixedFlags(true), new FakeDiscovery());

    private static JobsyDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<JobsyDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;
        return new JobsyDbContext(options);
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

using Jobsy.Core.Entities;
using Jobsy.Core.Interfaces;
using Jobsy.Core.Rules;
using Jobsy.Infrastructure.Data;
using Jobsy.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;

namespace Jobsy.Tests;

public class CultureValuesPreviewScoresTests
{
    [Fact]
    public void Culture_score_from_partial_answers_fills_answered_dimensions_only()
    {
        var answers = new Dictionary<int, int> { [1] = 5, [2] = 4 };
        var preview = CulturePersonalityCatalog.Score(answers);
        Assert.NotNull(preview);
        Assert.False(preview!.IsComplete);
        Assert.True(
            preview.Autonomy is not null
            || preview.Informal is not null
            || preview.Collaboration is not null
            || preview.Flexibility is not null
            || preview.Innovation is not null
            || preview.PeopleFirst is not null);
    }

    [Fact]
    public void Values_score_from_partial_answers_fills_answered_dimensions_only()
    {
        var answers = new Dictionary<int, int> { [1] = 5, [2] = 4, [3] = 3 };
        var preview = SchwartzValuesCatalog.Score(answers);
        Assert.NotNull(preview);
        Assert.False(preview!.IsComplete);
        Assert.True(
            preview.Autonomy is not null
            || preview.Connection is not null
            || preview.Achievement is not null
            || preview.Stability is not null
            || preview.Impact is not null);
    }

    [Fact]
    public async Task Culture_ToDto_exposes_PreviewScores_while_Scores_stay_null_for_draft()
    {
        await using var db = CreateDb();
        var userId = Guid.NewGuid();
        db.CandidateCulturePersonalityProfiles.Add(new CandidateCulturePersonalityProfile
        {
            UserId = userId,
            Status = CandidateCompetencyStatuses.Draft,
            AnswersJson = CulturePersonalityCatalog.SerializeAnswers(new Dictionary<int, int> { [1] = 5, [2] = 4, [3] = 3 }),
            UpdatedAtUtc = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        var sut = new CandidateCulturePersonalityService(db, new StubCommercial(), new StubQueue(), new StubMatchSnapshots(), new AssessmentSaveGuard(db, new AssessmentAdjustmentService(db)));
        var dto = await sut.GetAsync(userId);

        Assert.Equal(CandidateCompetencyStatuses.Draft, dto.Status);
        Assert.Null(dto.Scores);
        Assert.NotNull(dto.PreviewScores);
        Assert.Equal(3, dto.Answers.Count);
    }

    [Fact]
    public async Task Values_ToDto_exposes_PreviewScores_while_Scores_stay_null_for_draft()
    {
        await using var db = CreateDb();
        var userId = Guid.NewGuid();
        db.CandidateValuesProfiles.Add(new CandidateValuesProfile
        {
            UserId = userId,
            Status = CandidateCompetencyStatuses.Draft,
            AnswersJson = SchwartzValuesCatalog.SerializeAnswers(new Dictionary<int, int> { [1] = 5, [6] = 4, [11] = 3 }),
            UpdatedAtUtc = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        var sut = new CandidateValuesService(db, new StubCommercial(), new StubQueue(), new StubMatchSnapshots(), new AssessmentSaveGuard(db, new AssessmentAdjustmentService(db)));
        var dto = await sut.GetAsync(userId);

        Assert.Equal(CandidateCompetencyStatuses.Draft, dto.Status);
        Assert.Null(dto.Scores);
        Assert.NotNull(dto.PreviewScores);
        Assert.Equal(3, dto.Answers.Count);
    }

    [Fact]
    public async Task Completed_values_save_with_empty_answers_keeps_the_result()
    {
        await using var db = CreateDb();
        var userId = Guid.NewGuid();
        var answers = Enumerable.Range(1, 25).ToDictionary(id => id, _ => 4);
        db.CandidateValuesProfiles.Add(new CandidateValuesProfile
        {
            UserId = userId,
            Status = CandidateCompetencyStatuses.Completed,
            AnswersJson = SchwartzValuesCatalog.SerializeAnswers(answers),
            AutonomyPercent = 70,
            ConnectionPercent = 80,
            AchievementPercent = 60,
            StabilityPercent = 55,
            ImpactPercent = 50,
            CompletedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        var sut = new CandidateValuesService(
            db, new StubCommercial(), new StubQueue(), new StubMatchSnapshots(),
            new AssessmentSaveGuard(db, new AssessmentAdjustmentService(db)));
        var dto = await sut.SaveAsync(userId, new Dictionary<int, int>(), complete: true);

        Assert.Equal(CandidateCompetencyStatuses.Completed, dto.Status);
        Assert.Equal(25, dto.Answers.Count);
        Assert.Equal(80, dto.Scores?.Connection);
    }

    private static JobsyDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<JobsyDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;
        return new JobsyDbContext(options);
    }

    private sealed class StubMatchSnapshots : ICandidateMatchSnapshotService
    {
        public Task<(IReadOnlyList<CandidateMatchedVacancyDto> Matches, string InsightsStatus)> GetAsync(
            Guid userId,
            CancellationToken cancellationToken = default)
            => Task.FromResult<(IReadOnlyList<CandidateMatchedVacancyDto>, string)>(([], "Ready"));

        public Task SaveComputedAsync(
            Guid userId,
            IReadOnlyList<CandidateMatchedVacancyDto> matches,
            string inputFingerprint,
            CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public Task<string> ComputeInputFingerprintAsync(Guid userId, CancellationToken cancellationToken = default)
            => Task.FromResult("stub");

        public Task<IReadOnlyList<CandidateMatchedVacancyDto>> ComputeLiveAsync(
            Guid userId,
            CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<CandidateMatchedVacancyDto>>([]);

        public void InvalidateContextCache(Guid userId)
        {
        }

        public Task MarkInputsStaleAsync(Guid userId, CancellationToken cancellationToken = default)
            => Task.CompletedTask;
    }

    private sealed class StubQueue : ICandidateInsightsQueue
    {
        public bool TryEnqueue(Guid userId) => true;

        public ValueTask<Guid> DequeueAsync(CancellationToken cancellationToken)
            => ValueTask.FromResult(Guid.Empty);

        public void MarkCompleted(Guid userId)
        {
        }
    }

    private sealed class StubCommercial : IFlexCommercialService
    {
        public Task<FlexCommercialSettingsDto> GetAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(new FlexCommercialSettingsDto(
                0,
                "test",
                FlexCommercialSettings.DefaultDeepAnalysisPriceEuro,
                FlexCommercialSettings.DefaultDeepAnalysisPriceEuro,
                FlexCommercialSettings.DefaultDeepAnalysisPriceEuro,
                FlexCommercialSettings.DefaultDeepAnalysisPriceEuro,
                0,
                0,
                DateTime.UtcNow));

        public Task<FlexCommercialSettingsDto> UpdateAsync(
            FlexCommercialSettingsUpdate update,
            CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<bool> HasActiveAgencySubscriptionAsync(Guid companyId, CancellationToken cancellationToken = default)
            => Task.FromResult(false);

        public Task<AgencySubscriptionDto?> GetAgencySubscriptionAsync(
            Guid companyId,
            CancellationToken cancellationToken = default)
            => Task.FromResult<AgencySubscriptionDto?>(null);

        public Task<AgencySubscriptionDto> ActivateAgencySubscriptionAsync(
            Guid companyId,
            DateTime? startsAtUtc = null,
            string? note = null,
            CancellationToken cancellationToken = default)
            => throw new NotSupportedException();
    }
}

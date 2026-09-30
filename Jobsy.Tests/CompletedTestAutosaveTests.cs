using Jobsy.Core.Entities;
using Jobsy.Core.Enums;
using Jobsy.Core.Interfaces;
using Jobsy.Core.Reports.Competence;
using Jobsy.Core.Rules;
using Jobsy.Infrastructure.Data;
using Jobsy.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;

namespace Jobsy.Tests;

public class CompletedTestAutosaveTests
{
    [Fact]
    public async Task Competency_autosave_keeps_completed_scores()
    {
        await using var db = CreateDb();
        var userId = Guid.NewGuid();
        var answers = FullCompetencyAnswers();
        var preview = CompetencyTestCatalog.Score(answers)!;
        db.CandidateCompetencies.Add(new CandidateCompetency
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Status = CandidateCompetencyStatuses.Completed,
            AnswersJson = CompetencyTestCatalog.SerializeAnswers(answers),
            SamenwerkenPercent = preview.Samenwerken,
            ResultaatgerichtheidPercent = preview.Resultaatgerichtheid,
            StressbestendigheidPercent = preview.Stressbestendigheid,
            InnovatiePercent = preview.Innovatie,
            ExtraversiePercent = preview.Extraversie,
            RiasecTagsJson = "[]",
            MatchTagsJson = CompetencyTestCatalog.SerializeTags(CompetencyTestCatalog.DeriveMatchTags(preview)),
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow,
            CompletedAtUtc = DateTime.UtcNow.AddDays(-1)
        });
        await db.SaveChangesAsync();

        var edited = new Dictionary<int, int>(answers) { [1] = answers[1] == 5 ? 4 : 5 };
        var sut = new CandidateCompetencyService(db, new StubCommercial(), new StubMatchSnapshots(), new StubQueue(), new AssessmentSaveGuard(db, new AssessmentAdjustmentService(db)));
        var dto = await sut.SaveAsync(userId, edited, complete: false);

        Assert.Equal(CandidateCompetencyStatuses.Completed, dto.Status);
        var row = await db.CandidateCompetencies.SingleAsync();
        Assert.Equal(CandidateCompetencyStatuses.Completed, row.Status);
        Assert.Equal(preview.Samenwerken, row.SamenwerkenPercent);
        Assert.Equal(preview.Resultaatgerichtheid, row.ResultaatgerichtheidPercent);
        Assert.Equal(preview.Stressbestendigheid, row.StressbestendigheidPercent);
        Assert.Equal(preview.Innovatie, row.InnovatiePercent);
        Assert.NotNull(row.CompletedAtUtc);
        Assert.Equal(edited[1], CompetencyTestCatalog.ParseAnswersJson(row.AnswersJson)[1]);
    }

    [Fact]
    public async Task Career_autosave_keeps_completed_scores()
    {
        await using var db = CreateDb();
        var userId = Guid.NewGuid();
        var answers = FullCareerAnswers();
        var preview = CareerTestCatalog.Score(answers)!;
        var tags = CareerTestCatalog.DeriveRiasecTags(preview);
        var completedAt = DateTime.UtcNow.AddDays(-2);
        db.CandidateCareerInterests.Add(new CandidateCareerInterest
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Status = CandidateCompetencyStatuses.Completed,
            AnswersJson = CareerTestCatalog.SerializeAnswers(answers),
            RealisticPercent = preview.Realistic,
            InvestigativePercent = preview.Investigative,
            ArtisticPercent = preview.Artistic,
            SocialPercent = preview.Social,
            EnterprisingPercent = preview.Enterprising,
            ConventionalPercent = preview.Conventional,
            HollandCode = CareerTestCatalog.HollandCode(preview),
            RiasecTagsJson = CareerTestCatalog.SerializeTags(tags),
            MatchTagsJson = CareerTestCatalog.SerializeTags(tags),
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow,
            CompletedAtUtc = completedAt
        });
        await db.SaveChangesAsync();

        var edited = new Dictionary<int, int>(answers) { [3] = 1 };
        var sut = new CandidateCareerInterestService(db, new StubCommercial(), new StubMatchSnapshots(), new StubQueue(), new AssessmentSaveGuard(db, new AssessmentAdjustmentService(db)));
        await sut.SaveAsync(userId, edited, complete: false);

        var row = await db.CandidateCareerInterests.SingleAsync();
        Assert.Equal(CandidateCompetencyStatuses.Completed, row.Status);
        Assert.Equal(preview.Realistic, row.RealisticPercent);
        Assert.Equal(CareerTestCatalog.HollandCode(preview), row.HollandCode);
        Assert.Equal(completedAt, row.CompletedAtUtc);
        Assert.Equal(1, CareerTestCatalog.ParseAnswersJson(row.AnswersJson)[3]);
    }

    [Fact]
    public async Task Culture_autosave_keeps_completed_scores()
    {
        await using var db = CreateDb();
        var userId = Guid.NewGuid();
        var answers = Enumerable.Range(1, CulturePersonalityCatalog.QuestionCount)
            .ToDictionary(i => i, _ => 4);
        var sut = new CandidateCulturePersonalityService(db, new StubCommercial(), new StubQueue(), new StubMatchSnapshots(), new AssessmentSaveGuard(db, new AssessmentAdjustmentService(db)));
        await sut.SaveAsync(userId, answers, complete: true);
        var before = await db.CandidateCulturePersonalityProfiles.AsNoTracking().SingleAsync();
        Assert.Equal(CandidateCompetencyStatuses.Completed, before.Status);

        var edited = new Dictionary<int, int>(answers) { [2] = 1 };
        await sut.SaveAsync(userId, edited, complete: false);

        var after = await db.CandidateCulturePersonalityProfiles.SingleAsync();
        Assert.Equal(CandidateCompetencyStatuses.Completed, after.Status);
        Assert.Equal(before.CompletedAtUtc, after.CompletedAtUtc);
        Assert.Equal(before.MatchTagsJson, after.MatchTagsJson);
        Assert.Equal(1, CulturePersonalityCatalog.ParseAnswers(after.AnswersJson)[2]);
    }

    [Fact]
    public async Task Values_autosave_keeps_completed_scores()
    {
        await using var db = CreateDb();
        var userId = Guid.NewGuid();
        var answers = Enumerable.Range(1, SchwartzValuesCatalog.QuestionCount)
            .ToDictionary(i => i, _ => 4);
        var sut = new CandidateValuesService(db, new StubCommercial(), new StubQueue(), new StubMatchSnapshots(), new AssessmentSaveGuard(db, new AssessmentAdjustmentService(db)));
        await sut.SaveAsync(userId, answers, complete: true);
        var before = await db.CandidateValuesProfiles.AsNoTracking().SingleAsync();
        Assert.Equal(CandidateCompetencyStatuses.Completed, before.Status);

        var edited = new Dictionary<int, int>(answers) { [5] = 2 };
        await sut.SaveAsync(userId, edited, complete: false);

        var after = await db.CandidateValuesProfiles.SingleAsync();
        Assert.Equal(CandidateCompetencyStatuses.Completed, after.Status);
        Assert.Equal(before.CompletedAtUtc, after.CompletedAtUtc);
        Assert.Equal(before.MatchTagsJson, after.MatchTagsJson);
        Assert.Equal(2, SchwartzValuesCatalog.ParseAnswers(after.AnswersJson)[5]);
    }

    [Fact]
    public async Task Deep_analysis_autosave_keeps_completed_status_and_tags()
    {
        await using var db = CreateDb();
        var userId = Guid.NewGuid();
        var kind = AssessmentKind.Career;
        var answers = Enumerable.Range(1, DeepAnalysisCatalog.QuestionCountFor(kind))
            .ToDictionary(i => i, _ => 4);
        var tagsJson = """["realistic","social"]""";
        var completedAt = DateTime.UtcNow.AddHours(-3);
        db.CandidateDeepAnalyses.Add(new CandidateDeepAnalysis
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Kind = kind,
            Status = CandidateDeepAnalysisStatuses.Completed,
            AnswersJson = DeepAnalysisCatalog.SerializeAnswers(answers, kind),
            TagsJson = tagsJson,
            UnlockedAtUtc = DateTime.UtcNow.AddDays(-1),
            CompletedAtUtc = completedAt,
            ReportGeneratedAtUtc = completedAt,
            UpdatedAtUtc = completedAt
        });
        await db.SaveChangesAsync();

        var edited = new Dictionary<int, int>(answers) { [10] = 1 };
        var sut = CreateDeepSut(db);
        await sut.SaveAsync(userId, kind, edited, complete: false);

        var row = await db.CandidateDeepAnalyses.SingleAsync();
        Assert.Equal(CandidateDeepAnalysisStatuses.Completed, row.Status);
        Assert.Equal(completedAt, row.CompletedAtUtc);
        Assert.Equal(completedAt, row.ReportGeneratedAtUtc);
        Assert.Equal(tagsJson, row.TagsJson);
        Assert.Equal(1, DeepAnalysisCatalog.ParseAnswersJson(row.AnswersJson, kind)[10]);
    }

    [Fact]
    public async Task Completing_again_rescores_after_pending_edit()
    {
        await using var db = CreateDb();
        var userId = Guid.NewGuid();
        var answers = FullCompetencyAnswers();
        var sut = new CandidateCompetencyService(db, new StubCommercial(), new StubMatchSnapshots(), new StubQueue(), new AssessmentSaveGuard(db, new AssessmentAdjustmentService(db)));
        await sut.SaveAsync(userId, answers, complete: true);
        var first = await db.CandidateCompetencies.AsNoTracking().SingleAsync();

        var edited = new Dictionary<int, int>(answers) { [1] = answers[1] == 5 ? 1 : 5 };
        await sut.SaveAsync(userId, edited, complete: false);
        Assert.Equal(first.SamenwerkenPercent, (await db.CandidateCompetencies.AsNoTracking().SingleAsync()).SamenwerkenPercent);

        await sut.SaveAsync(userId, edited, complete: true);
        var rescored = CompetencyTestCatalog.Score(edited)!;
        var after = await db.CandidateCompetencies.SingleAsync();
        Assert.Equal(CandidateCompetencyStatuses.Completed, after.Status);
        Assert.Equal(rescored.Samenwerken, after.SamenwerkenPercent);
    }

    private static Dictionary<int, int> FullCompetencyAnswers()
        => Enumerable.Range(1, CompetencyTestCatalog.QuestionCount).ToDictionary(
            i => i,
            i => CompetencyTestCatalog.Questions.First(q => q.Id == i).Reverse ? 1 : 5);

    private static Dictionary<int, int> FullCareerAnswers()
        => Enumerable.Range(1, CareerTestCatalog.QuestionCount).ToDictionary(
            i => i,
            i => CareerTestCatalog.Questions.First(q => q.Id == i).Reverse ? 1 : 5);

    private static DeepAnalysisService CreateDeepSut(JobsyDbContext db)
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["JobsyAuth:AllowStubPayments"] = "true"
            })
            .Build();
        return new DeepAnalysisService(
            db,
            new FlexCommercialService(db),
            new FakeHostEnvironment(),
            config,
            new StubCareerCompass(),
            new StubCompetenceDeepReportService(),
            NullLogger<DeepAnalysisService>.Instance,
            new AssessmentSaveGuard(db, new AssessmentAdjustmentService(db)));
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

    private sealed class StubCompetenceDeepReportService : ICompetenceDeepReportService
    {
        public Task<CompetenceDeepReport?> GetStoredAsync(Guid userId, CancellationToken ct)
            => Task.FromResult<CompetenceDeepReport?>(null);

        public Task<CompetenceDeepReport> BuildAndStoreAsync(Guid userId, bool tryAi, CancellationToken ct)
            => Task.FromResult(new CompetenceDeepReport());

        public Task RefineAiAsync(Guid userId, CancellationToken ct) => Task.CompletedTask;
    }

    private sealed class StubCareerCompass : ICareerCompassGenerationService
    {
        public Task<CareerCompassSnapshot> GenerateFromCareerDeepAsync(
            IReadOnlyDictionary<int, int> answers,
            CancellationToken cancellationToken = default)
        {
            var scores = DeepAnalysisCatalog.ToRiasecScores(
                DeepAnalysisCatalog.ScoreDomains(answers, AssessmentKind.Career));
            return Task.FromResult(CareerCompassBuilder.Build(scores, fromDeepAnalysis: true));
        }
    }

    private sealed class FakeHostEnvironment : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = Environments.Development;
        public string ApplicationName { get; set; } = "Jobsy.Tests";
        public string ContentRootPath { get; set; } = ".";
        public Microsoft.Extensions.FileProviders.IFileProvider ContentRootFileProvider { get; set; }
            = new Microsoft.Extensions.FileProviders.NullFileProvider();
    }
}

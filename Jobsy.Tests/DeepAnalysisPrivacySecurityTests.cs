using Jobsy.Core.Entities;
using Jobsy.Core.Enums;
using Jobsy.Core.Rules;
using Jobsy.Infrastructure.Data;
using Jobsy.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;

namespace Jobsy.Tests;

public class DeepAnalysisPrivacySecurityTests
{
    [Fact]
    public async Task Complete_checkout_requires_stub_gate_and_matching_user()
    {
        await using var db = CreateDb();
        var userId = Guid.NewGuid();
        var otherId = Guid.NewGuid();
        db.Users.Add(new User
        {
            Id = userId,
            Email = "deep@test.nl",
            FullName = "Deep",
            Role = UserRole.Candidate,
            IsActive = true
        });
        await db.SaveChangesAsync();

        var sut = CreateSut(db, isDevelopment: true, allowStub: false);
        var checkout = await sut.StartCheckoutAsync(userId, AssessmentKind.Competence);

        Assert.False(await sut.TryFulfillPaidCheckoutAsync(
            checkout.PaymentId,
            expectedUserId: otherId,
            allowDevStubMarkPaid: true));

        Assert.False(await sut.TryFulfillPaidCheckoutAsync(
            checkout.PaymentId,
            expectedUserId: userId,
            allowDevStubMarkPaid: false));

        var production = CreateSut(db, isDevelopment: false, allowStub: false);
        Assert.False(await production.TryFulfillPaidCheckoutAsync(
            checkout.PaymentId,
            expectedUserId: userId,
            allowDevStubMarkPaid: true));

        Assert.True(await sut.TryFulfillPaidCheckoutAsync(
            checkout.PaymentId,
            expectedUserId: userId,
            allowDevStubMarkPaid: true));

        var state = await sut.GetStateAsync(userId, AssessmentKind.Competence);
        Assert.True(state.IsUnlocked);
    }

    [Fact]
    public async Task Start_checkout_blocked_when_stub_payments_disabled()
    {
        await using var db = CreateDb();
        var userId = Guid.NewGuid();
        db.Users.Add(new User
        {
            Id = userId,
            Email = "nostub@test.nl",
            FullName = "No Stub",
            Role = UserRole.Candidate,
            IsActive = true
        });
        await db.SaveChangesAsync();

        var sut = CreateSut(db, isDevelopment: false, allowStub: false);
        await Assert.ThrowsAsync<InvalidOperationException>(() => sut.StartCheckoutAsync(userId, AssessmentKind.Competence));
    }

    [Fact]
    public async Task Empty_save_does_not_wipe_existing_answers()
    {
        await using var db = CreateDb();
        var userId = Guid.NewGuid();
        db.Users.Add(new User
        {
            Id = userId,
            Email = "save@test.nl",
            FullName = "Save",
            Role = UserRole.Candidate,
            IsActive = true
        });
        db.CandidateDeepAnalyses.Add(new CandidateDeepAnalysis
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Kind = AssessmentKind.Competence,
            Status = CandidateDeepAnalysisStatuses.Draft,
            AnswersJson = """{"1":4,"2":3}""",
            TagsJson = "[]",
            UnlockedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        var sut = CreateSut(db, isDevelopment: true, allowStub: false);
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => sut.SaveAsync(userId, AssessmentKind.Competence, new Dictionary<int, int>(), complete: false));

        var row = await db.CandidateDeepAnalyses.SingleAsync(d => d.UserId == userId);
        Assert.Contains("\"1\":4", row.AnswersJson);
    }

    [Fact]
    public async Task Tag_backfill_fills_empty_match_tags_for_completed_quick_scan()
    {
        await using var db = CreateDb();
        var userId = Guid.NewGuid();
        // High Big Five scores for competence match tags. Q21–Q25 still map to the
        // legacy compact RIASEC probes (R/I/A/S/E); keep only R and S at ≥4 so
        // Take(3) cannot crowd Social out alphabetically.
        var answers = Enumerable.Range(1, 25).ToDictionary(
            i => i,
            i => CompetencyTestCatalog.Questions.First(q => q.Id == i).Reverse ? 1 : 5);
        answers[21] = 5;
        answers[22] = 1;
        answers[23] = 1;
        answers[24] = 5;
        answers[25] = 1;
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
            RiasecTagsJson = "[]",
            MatchTagsJson = "[]",
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow,
            CompletedAtUtc = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        await CompetencyTagBackfillSeeder.BackfillAsync(db, NullLogger.Instance);

        var row = await db.CandidateCompetencies.SingleAsync();
        Assert.NotEqual("[]", row.MatchTagsJson);
        var career = await db.CandidateCareerInterests.SingleAsync();
        Assert.Contains(CompetencyTestCatalog.RiasecRealistic, CareerTestCatalog.ParseTagsJson(career.RiasecTagsJson));
        Assert.Contains(CompetencyTestCatalog.RiasecSocial, CareerTestCatalog.ParseTagsJson(career.RiasecTagsJson));
    }

    private static DeepAnalysisService CreateSut(JobsyDbContext db, bool isDevelopment, bool allowStub)
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["JobsyAuth:AllowStubPayments"] = allowStub ? "true" : "false"
            })
            .Build();
        return new DeepAnalysisService(
            db,
            new FlexCommercialService(db),
            new FakeHostEnvironment(isDevelopment ? Environments.Development : Environments.Production),
            config,
            NullLogger<DeepAnalysisService>.Instance);
    }

    private static JobsyDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<JobsyDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new JobsyDbContext(options);
    }

    private sealed class FakeHostEnvironment : IHostEnvironment
    {
        public FakeHostEnvironment(string environmentName) => EnvironmentName = environmentName;
        public string EnvironmentName { get; set; }
        public string ApplicationName { get; set; } = "Jobsy.Tests";
        public string ContentRootPath { get; set; } = ".";
        public Microsoft.Extensions.FileProviders.IFileProvider ContentRootFileProvider { get; set; }
            = new Microsoft.Extensions.FileProviders.NullFileProvider();
    }
}

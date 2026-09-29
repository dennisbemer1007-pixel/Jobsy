using Jobsy.Core.Entities;
using Jobsy.Core.Enums;
using Jobsy.Core.Interfaces;
using Jobsy.Core.Rules;
using Jobsy.Infrastructure.Data;
using Jobsy.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;

namespace Jobsy.Tests;

public class AssessmentAdjustmentServiceTests
{
    [Fact]
    public async Task First_completion_does_not_count_and_dto_starts_at_max()
    {
        await using var db = CreateDb();
        var sut = new AssessmentAdjustmentService(db);
        var userId = Guid.NewGuid();

        var dto = await sut.GetAsync(userId, AssessmentKind.Competence, AssessmentVariant.Quick);
        Assert.Equal(0, dto.Used);
        Assert.Equal(AssessmentAdjustmentRules.MaxAdjustments, dto.Remaining);
        Assert.Equal(AssessmentAdjustmentRules.MaxAdjustments, dto.Max);
    }

    [Fact]
    public async Task Edit_and_retake_each_count_once_up_to_three_then_409()
    {
        await using var db = CreateDb();
        var sut = new AssessmentAdjustmentService(db);
        var userId = Guid.NewGuid();

        await sut.RecordAsync(userId, AssessmentKind.Competence, AssessmentVariant.Quick, AssessmentAdjustmentType.Edit, null, "e1");
        await sut.RecordAsync(userId, AssessmentKind.Competence, AssessmentVariant.Quick, AssessmentAdjustmentType.Retake, Guid.NewGuid(), "r1");
        await sut.RecordAsync(userId, AssessmentKind.Competence, AssessmentVariant.Quick, AssessmentAdjustmentType.Edit, null, "e2");

        var dto = await sut.GetAsync(userId, AssessmentKind.Competence, AssessmentVariant.Quick);
        Assert.Equal(3, dto.Used);
        Assert.Equal(0, dto.Remaining);

        await Assert.ThrowsAsync<AssessmentAdjustmentLimitException>(() =>
            sut.RecordAsync(userId, AssessmentKind.Competence, AssessmentVariant.Quick, AssessmentAdjustmentType.Edit, null, "e3"));
    }

    [Fact]
    public async Task Double_submit_same_idempotency_key_counts_once()
    {
        await using var db = CreateDb();
        var sut = new AssessmentAdjustmentService(db);
        var userId = Guid.NewGuid();

        await sut.RecordAsync(userId, AssessmentKind.Career, AssessmentVariant.Quick, AssessmentAdjustmentType.Edit, null, "same");
        await sut.RecordAsync(userId, AssessmentKind.Career, AssessmentVariant.Quick, AssessmentAdjustmentType.Edit, null, "same");

        var dto = await sut.GetAsync(userId, AssessmentKind.Career, AssessmentVariant.Quick);
        Assert.Equal(1, dto.Used);
    }

    [Fact]
    public async Task Quick_and_deep_counters_are_independent()
    {
        await using var db = CreateDb();
        var sut = new AssessmentAdjustmentService(db);
        var userId = Guid.NewGuid();

        await sut.RecordAsync(userId, AssessmentKind.Values, AssessmentVariant.Quick, AssessmentAdjustmentType.Edit, null, "q1");
        await sut.RecordAsync(userId, AssessmentKind.Values, AssessmentVariant.Deep, AssessmentAdjustmentType.Edit, null, "d1");

        Assert.Equal(1, (await sut.GetAsync(userId, AssessmentKind.Values, AssessmentVariant.Quick)).Used);
        Assert.Equal(1, (await sut.GetAsync(userId, AssessmentKind.Values, AssessmentVariant.Deep)).Used);
    }

    [Fact]
    public async Task Retake_abandon_does_not_consume_adjustment()
    {
        await using var db = CreateDb();
        var adjustments = new AssessmentAdjustmentService(db);
        var retakes = new AssessmentRetakeService(db, adjustments);
        var userId = Guid.NewGuid();

        var attempt = await retakes.StartAsync(userId, AssessmentKind.Culture, AssessmentVariant.Quick);
        await retakes.AbandonAsync(userId, attempt.Id);

        var dto = await adjustments.GetAsync(userId, AssessmentKind.Culture, AssessmentVariant.Quick);
        Assert.Equal(0, dto.Used);
        Assert.Equal(CandidateAssessmentAttempt.AttemptStatus.Abandoned,
            (await db.CandidateAssessmentAttempts.SingleAsync()).Status);
    }

    private static JobsyDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<JobsyDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;
        return new JobsyDbContext(options);
    }
}

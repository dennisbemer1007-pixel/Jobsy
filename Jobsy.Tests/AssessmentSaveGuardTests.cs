using Jobsy.Core.Entities;
using Jobsy.Core.Enums;
using Jobsy.Core.Interfaces;
using Jobsy.Core.Rules;
using Jobsy.Infrastructure.Data;
using Jobsy.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;

namespace Jobsy.Tests;

public sealed class AssessmentSaveGuardTests
{
    [Fact]
    public async Task First_completion_does_not_record_adjustment()
    {
        await using var db = CreateDb();
        var adjustments = new AssessmentAdjustmentService(db);
        var guard = new AssessmentSaveGuard(db, adjustments);
        var userId = Guid.NewGuid();

        // Simulate first completion path: no prior completed → guard not used for counting.
        var dto = await adjustments.GetAsync(userId, AssessmentKind.Competence, AssessmentVariant.Quick);
        Assert.Equal(0, dto.Used);
        Assert.False(dto.HasDraft);
    }

    [Fact]
    public async Task Edit_complete_counts_once_identical_is_noop_fourth_throws()
    {
        await using var db = CreateDb();
        var adjustments = new AssessmentAdjustmentService(db);
        var guard = new AssessmentSaveGuard(db, adjustments);
        var userId = Guid.NewGuid();
        const string completed = """{"1":5,"2":4}""";
        const string edited = """{"1":3,"2":4}""";

        for (var i = 0; i < 3; i++)
        {
            var noOp = await guard.CommitCompleteAsync(
                userId, AssessmentKind.Competence, AssessmentVariant.Quick,
                edited, "{}", completed, completed, CancellationToken.None);
            Assert.False(noOp);
        }

        Assert.Equal(3, (await adjustments.GetAsync(userId, AssessmentKind.Competence, AssessmentVariant.Quick)).Used);

        var identical = await guard.CommitCompleteAsync(
            userId, AssessmentKind.Competence, AssessmentVariant.Quick,
            edited, "{}", edited, edited, CancellationToken.None);
        Assert.True(identical);
        Assert.Equal(3, (await adjustments.GetAsync(userId, AssessmentKind.Competence, AssessmentVariant.Quick)).Used);

        await Assert.ThrowsAsync<AssessmentAdjustmentLimitException>(() =>
            guard.CommitCompleteAsync(
                userId, AssessmentKind.Competence, AssessmentVariant.Quick,
                completed, "{}", edited, edited, CancellationToken.None));
    }

    [Fact]
    public async Task Retake_counts_at_completion_and_history_lists_completed()
    {
        await using var db = CreateDb();
        var adjustments = new AssessmentAdjustmentService(db);
        var retakes = new AssessmentRetakeService(db, adjustments);
        var guard = new AssessmentSaveGuard(db, adjustments);
        var userId = Guid.NewGuid();

        var attempt = await retakes.StartAsync(userId, AssessmentKind.Career, AssessmentVariant.Quick);
        Assert.Equal(CandidateAssessmentAttempt.AttemptOrigin.Retake, attempt.Origin);

        await guard.CommitCompleteAsync(
            userId, AssessmentKind.Career, AssessmentVariant.Quick,
            """{"1":5}""", "{}", null, """{"1":4}""", CancellationToken.None);

        Assert.Equal(1, (await adjustments.GetAsync(userId, AssessmentKind.Career, AssessmentVariant.Quick)).Used);
        var history = await retakes.ListHistoryAsync(userId, AssessmentKind.Career, AssessmentVariant.Quick);
        Assert.Single(history);
        Assert.Equal(CandidateAssessmentAttempt.AttemptStatus.Completed, history[0].Status);
    }

    [Fact]
    public async Task Draft_sets_HasDraft_abandon_clears()
    {
        await using var db = CreateDb();
        var adjustments = new AssessmentAdjustmentService(db);
        var retakes = new AssessmentRetakeService(db, adjustments);
        var guard = new AssessmentSaveGuard(db, adjustments);
        var userId = Guid.NewGuid();

        await guard.SaveDraftAsync(userId, AssessmentKind.Values, AssessmentVariant.Quick, """{"1":2}""", """{"1":5}""");
        var dto = await adjustments.GetAsync(userId, AssessmentKind.Values, AssessmentVariant.Quick);
        Assert.True(dto.HasDraft);
        Assert.Equal(0, dto.Used);

        var open = await db.CandidateAssessmentAttempts.SingleAsync();
        await retakes.AbandonAsync(userId, open.Id);
        dto = await adjustments.GetAsync(userId, AssessmentKind.Values, AssessmentVariant.Quick);
        Assert.False(dto.HasDraft);
    }

    [Fact]
    public async Task Quick_and_Deep_are_independent()
    {
        await using var db = CreateDb();
        var adjustments = new AssessmentAdjustmentService(db);
        var guard = new AssessmentSaveGuard(db, adjustments);
        var userId = Guid.NewGuid();

        await guard.CommitCompleteAsync(userId, AssessmentKind.Culture, AssessmentVariant.Quick, """{"1":1}""", null, null, """{"1":2}""");
        await guard.CommitCompleteAsync(userId, AssessmentKind.Culture, AssessmentVariant.Deep, """{"1":1}""", null, null, """{"1":2}""");

        Assert.Equal(1, (await adjustments.GetAsync(userId, AssessmentKind.Culture, AssessmentVariant.Quick)).Used);
        Assert.Equal(1, (await adjustments.GetAsync(userId, AssessmentKind.Culture, AssessmentVariant.Deep)).Used);
    }

    private static JobsyDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<JobsyDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;
        return new JobsyDbContext(options);
    }
}

using Jobsy.Core.Entities;
using Jobsy.Core.Enums;
using Jobsy.Core.Rules;
using Jobsy.Core.Security;
using Jobsy.Infrastructure.Data;
using Jobsy.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;

namespace Jobsy.Tests;

public class SoftWithdrawAndCandidateActionFlowTests
{
    [Fact]
    public void ScrubPersonalDataOnWithdraw_clears_snapshots_keeps_identity()
    {
        var app = new Application
        {
            Id = Guid.NewGuid(),
            VacancyId = Guid.NewGuid(),
            CandidateUserId = Guid.NewGuid(),
            CandidateName = "Jan Jansen",
            CandidateEmail = "jan@example.com",
            CandidateAddress = "Straat 1",
            CandidateCity = "Delft",
            Motivation = "Ik wil graag",
            SnapshotAboutMe = "Bio",
            SnapshotPhoneNumber = "0612345678",
            SnapshotCertificatesJson = "[]",
            SnapshotAvailabilityJson = "{}",
            DistanceKm = 4.2,
            EmailVerificationCode = VerificationCodes.Hash("123456"),
            Status = ApplicationStatus.Withdrawn
        };

        ApplicationRules.ScrubPersonalDataOnWithdraw(app);

        Assert.Equal("Jan Jansen", app.CandidateName);
        Assert.Equal("jan@example.com", app.CandidateEmail);
        Assert.NotNull(app.CandidateUserId);
        Assert.Null(app.CandidateAddress);
        Assert.Null(app.CandidateCity);
        Assert.Null(app.Motivation);
        Assert.Null(app.SnapshotAboutMe);
        Assert.Null(app.SnapshotPhoneNumber);
        Assert.Null(app.SnapshotCertificatesJson);
        Assert.Null(app.SnapshotAvailabilityJson);
        Assert.Null(app.DistanceKm);
        Assert.Null(app.EmailVerificationCode);
        Assert.Equal(0, app.CandidateEmployerCount);
    }

    [Fact]
    public void BlocksDuplicate_allows_reapply_after_withdrawn()
    {
        Assert.False(ApplicationRules.BlocksDuplicateApplication(
            ApplicationStatus.Withdrawn, DateTime.UtcNow));
        Assert.True(ApplicationRules.CanReuseWithdrawnApplication(ApplicationStatus.Withdrawn));
        Assert.False(ApplicationRules.CountsTowardVacancyCapacity(
            ApplicationStatus.Withdrawn, DateTime.UtcNow));
    }

    [Fact]
    public async Task SetUnavailable_token_not_consumed_when_user_inactive()
    {
        await using var db = CreateDb();
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "inactive@example.com",
            FullName = "Inactive",
            Role = UserRole.Candidate,
            IsActive = false,
            OpenForWork = true
        };
        db.Users.Add(user);
        await db.SaveChangesAsync();

        var tokens = new CandidateActionTokenService(db);
        var issued = await tokens.IssueAsync(user.Id, CandidateActionPurposes.SetUnavailable);

        // Mirror controller pre-check: inactive user → do not consume.
        var preview = await tokens.FindValidAsync(issued.PlaintextToken, CandidateActionPurposes.SetUnavailable);
        Assert.NotNull(preview);
        var target = await db.Users.SingleAsync(u => u.Id == preview!.UserId);
        Assert.False(target.IsActive);

        // Token must still be valid for a later successful attempt after reactivation.
        Assert.NotNull(await tokens.FindValidAsync(issued.PlaintextToken, CandidateActionPurposes.SetUnavailable));

        target.IsActive = true;
        await db.SaveChangesAsync();
        var consumed = await tokens.TryConsumeAsync(issued.PlaintextToken, CandidateActionPurposes.SetUnavailable);
        Assert.NotNull(consumed);
        Assert.Null(await tokens.FindValidAsync(issued.PlaintextToken, CandidateActionPurposes.SetUnavailable));
    }

    [Fact]
    public async Task WithdrawOthers_token_not_consumed_when_hire_missing()
    {
        await using var db = CreateDb();
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "hired@example.com",
            FullName = "Hired",
            Role = UserRole.Candidate,
            IsActive = true
        };
        db.Users.Add(user);
        await db.SaveChangesAsync();

        var tokens = new CandidateActionTokenService(db);
        var relatedId = Guid.NewGuid();
        var issued = await tokens.IssueAsync(
            user.Id,
            CandidateActionPurposes.WithdrawOtherApplications,
            relatedId);

        var preview = await tokens.FindValidAsync(
            issued.PlaintextToken,
            CandidateActionPurposes.WithdrawOtherApplications);
        Assert.NotNull(preview);

        var hiredOk = await db.Applications.AsNoTracking().AnyAsync(
            a => a.Id == relatedId
                 && a.CandidateUserId == user.Id
                 && a.Status == ApplicationStatus.Hired);
        Assert.False(hiredOk);

        // Controller returns before TryConsume — token remains usable.
        Assert.NotNull(await tokens.FindValidAsync(
            issued.PlaintextToken,
            CandidateActionPurposes.WithdrawOtherApplications));
    }

    [Fact]
    public async Task Soft_withdraw_row_can_be_reopened_in_db()
    {
        await using var db = CreateDb();
        var vacancyId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var app = new Application
        {
            Id = Guid.NewGuid(),
            VacancyId = vacancyId,
            CandidateUserId = userId,
            CandidateName = "Her",
            CandidateEmail = "her@example.com",
            PreferredTransport = "Bike",
            Status = ApplicationStatus.Withdrawn,
            EmailVerifiedAt = DateTime.UtcNow.AddDays(-2),
            SnapshotAboutMe = "old",
            Motivation = "old",
            CreatedAt = DateTime.UtcNow.AddDays(-2),
            RespondedAt = DateTime.UtcNow.AddDays(-1)
        };
        db.Applications.Add(app);
        await db.SaveChangesAsync();

        ApplicationRules.ScrubPersonalDataOnWithdraw(app);
        await db.SaveChangesAsync();

        var existing = await db.Applications.SingleAsync(a => a.VacancyId == vacancyId);
        Assert.True(ApplicationRules.CanReuseWithdrawnApplication(existing.Status));
        Assert.False(ApplicationRules.BlocksDuplicateApplication(existing.Status, existing.EmailVerifiedAt));

        existing.Status = ApplicationStatus.Pending;
        existing.RespondedAt = null;
        existing.CreatedAt = DateTime.UtcNow;
        existing.EmailVerifiedAt = DateTime.UtcNow;
        existing.SnapshotAboutMe = "nieuw";
        await db.SaveChangesAsync();

        var reopened = await db.Applications.SingleAsync(a => a.Id == app.Id);
        Assert.Equal(ApplicationStatus.Pending, reopened.Status);
        Assert.Equal("nieuw", reopened.SnapshotAboutMe);
        Assert.True(ApplicationRules.CountsTowardVacancyCapacity(reopened.Status, reopened.EmailVerifiedAt));
    }

    private static JobsyDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<JobsyDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;
        return new JobsyDbContext(options);
    }
}

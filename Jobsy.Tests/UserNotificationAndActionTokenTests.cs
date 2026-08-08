using Jobsy.Core.Entities;
using Jobsy.Core.Enums;
using Jobsy.Core.Interfaces;
using Jobsy.Core.Rules;
using Jobsy.Infrastructure.Data;
using Jobsy.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;

namespace Jobsy.Tests;

public class UserNotificationAndActionTokenTests
{
    [Fact]
    public async Task NotificationService_creates_lists_and_marks_read()
    {
        await using var db = CreateDb();
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "kandidaat@example.com",
            FullName = "Kandidaat",
            Role = UserRole.Candidate,
            IsActive = true
        };
        db.Users.Add(user);
        await db.SaveChangesAsync();

        var sut = new UserNotificationService(db);
        await sut.CreateAsync(new NotificationCreateRequest(
            user.Id,
            "Titel",
            "Body tekst",
            "ApplicationConfirmation",
            "/candidate/applications"));

        Assert.Equal(1, await sut.CountUnreadAsync(user.Id));
        var list = await sut.ListForUserAsync(user.Id);
        Assert.Single(list);
        Assert.False(list[0].IsRead);

        await sut.MarkReadAsync(user.Id, list[0].Id);
        Assert.Equal(0, await sut.CountUnreadAsync(user.Id));
    }

    [Fact]
    public async Task NotificationService_strips_token_from_action_url()
    {
        await using var db = CreateDb();
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "kandidaat@example.com",
            FullName = "Kandidaat",
            Role = UserRole.Candidate,
            IsActive = true
        };
        db.Users.Add(user);
        await db.SaveChangesAsync();

        var sut = new UserNotificationService(db);
        await sut.CreateAsync(new NotificationCreateRequest(
            user.Id,
            "PushBom",
            "Body",
            "PushBom",
            "/vacancies/abc",
            "Zet op Niet beschikbaar",
            "/candidate/actions/set-unavailable?token=deadbeef"));

        var list = await sut.ListForUserAsync(user.Id);
        Assert.Equal("/candidate/actions/set-unavailable", list[0].ActionUrl);
        Assert.DoesNotContain("token=", list[0].ActionUrl ?? string.Empty);

        Assert.Equal(
            "/candidate/actions/withdraw-others?hiredApplicationId=11111111-1111-1111-1111-111111111111",
            UserNotificationService.SanitizeActionUrl(
                "/candidate/actions/withdraw-others?hiredApplicationId=11111111-1111-1111-1111-111111111111&token=abc"));
    }

    [Fact]
    public async Task ActionToken_issues_and_consumes_once()
    {
        await using var db = CreateDb();
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "kandidaat@example.com",
            FullName = "Kandidaat",
            Role = UserRole.Candidate,
            IsActive = true,
            OpenForWork = true
        };
        db.Users.Add(user);
        await db.SaveChangesAsync();

        var sut = new CandidateActionTokenService(db);
        var issued = await sut.IssueAsync(user.Id, CandidateActionPurposes.SetUnavailable);
        Assert.Contains("set-unavailable", issued.RelativeActionPath);
        Assert.Contains("token=", issued.RelativeActionPath);

        var consumed = await sut.TryConsumeAsync(issued.PlaintextToken, CandidateActionPurposes.SetUnavailable);
        Assert.NotNull(consumed);
        Assert.NotNull(consumed!.UsedAtUtc);

        var again = await sut.TryConsumeAsync(issued.PlaintextToken, CandidateActionPurposes.SetUnavailable);
        Assert.Null(again);
        Assert.Null(await sut.FindValidAsync(issued.PlaintextToken, CandidateActionPurposes.SetUnavailable));
    }

    private static JobsyDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<JobsyDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;
        return new JobsyDbContext(options);
    }
}

using Jobsy.Core.Entities;
using Jobsy.Core.Enums;
using Jobsy.Core.ValueObjects;
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
    public async Task NotificationService_after_match_categories_allow_repeat_titles()
    {
        await using var db = CreateDb();
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "wg@example.com",
            FullName = "Werkgever",
            Role = UserRole.BranchManager,
            IsActive = true
        };
        db.Users.Add(user);
        await db.SaveChangesAsync();

        var companyId = Guid.NewGuid();
        var vacId = Guid.NewGuid();
        var appId = Guid.NewGuid();
        db.Companies.Add(new Company
        {
            Id = companyId,
            Name = "Test BV",
            KvkNumber = "12345678",
            ContactEmail = "test@example.com",
            Type = CompanyType.Employer,
            Location = new GeoPoint(52.0, 4.3)
        });
        db.Vacancies.Add(new Vacancy
        {
            Id = vacId,
            CompanyId = companyId,
            Title = "Test",
            Description = "x",
            HourlyWage = 10,
            StartDate = DateOnly.FromDateTime(DateTime.UtcNow),
            EndDate = DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(1)),
            Status = VacancyStatus.Active,
            RequiredTransport = TransportMode.Bike,
            WorkTypes = WorkType.Horeca,
            Location = new Jobsy.Core.ValueObjects.GeoPoint(52.0, 4.3)
        });
        db.Applications.Add(new Application
        {
            Id = appId,
            VacancyId = vacId,
            CandidateUserId = Guid.NewGuid(),
            CandidateName = "Kandidaat",
            Status = ApplicationStatus.Accepted,
            CreatedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        var sut = new UserNotificationService(db);
        var title = "Nieuwe momenten";
        await sut.CreateAsync(new NotificationCreateRequest(
            user.Id,
            title,
            "Eerste voorstel",
            "InterviewSlotsProposed",
            "/werkgever/sollicitaties",
            RelatedEntityType: "Application",
            RelatedEntityId: appId));
        await sut.CreateAsync(new NotificationCreateRequest(
            user.Id,
            title,
            "Tweede voorstel",
            "InterviewSlotsProposed",
            "/werkgever/sollicitaties",
            RelatedEntityType: "Application",
            RelatedEntityId: appId));

        Assert.Equal(2, await sut.CountUnreadAsync(user.Id));
        Assert.True(UserNotificationService.SkipsDuplicateGuard("InterviewSlotsProposed"));
    }

    [Fact]
    public async Task ListForUser_skips_stale_recent_rows_and_finds_older_live_unread()
    {
        await using var db = CreateDb();
        var userId = Guid.NewGuid();
        db.Users.Add(new User
        {
            Id = userId,
            Email = "wg-bell@example.com",
            FullName = "Werkgever",
            Role = UserRole.BranchManager,
            IsActive = true
        });

        var liveVacancy = new Vacancy
        {
            Id = Guid.NewGuid(),
            CompanyId = Guid.NewGuid(),
            Title = "Live",
            Description = "x",
            HourlyWage = 10,
            StartDate = DateOnly.FromDateTime(DateTime.UtcNow),
            EndDate = DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(1)),
            Status = VacancyStatus.Active,
            Location = new GeoPoint(52, 4)
        };
        liveVacancy.Company = new Company
        {
            Id = liveVacancy.CompanyId,
            Name = "Live BV",
            KvkNumber = "87654321",
            ContactEmail = "live@example.com",
            Type = CompanyType.Employer,
            Location = new GeoPoint(52, 4)
        };
        var liveApp = new Application
        {
            Id = Guid.NewGuid(),
            VacancyId = liveVacancy.Id,
            CandidateName = "Kandidaat",
            CandidateEmail = "k@example.com",
            Status = ApplicationStatus.Accepted,
            CreatedAt = DateTime.UtcNow
        };
        db.Companies.Add(liveVacancy.Company);
        db.Vacancies.Add(liveVacancy);
        db.Applications.Add(liveApp);

        var now = DateTime.UtcNow;
        for (var i = 0; i < 150; i++)
        {
            db.UserNotifications.Add(new UserNotification
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                Title = "Stale",
                Body = "gone",
                Category = "ApplicationConfirmation",
                IsRead = false,
                CreatedAtUtc = now.AddMinutes(i),
                RelatedEntityType = "Application",
                RelatedEntityId = Guid.NewGuid()
            });
        }

        db.UserNotifications.Add(new UserNotification
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Title = "Live melding",
            Body = "Nog relevant",
            Category = "InterviewSlotsProposed",
            IsRead = false,
            CreatedAtUtc = now.AddDays(-30),
            RelatedEntityType = "Application",
            RelatedEntityId = liveApp.Id
        });

        await db.SaveChangesAsync();

        var sut = new UserNotificationService(db);
        Assert.Equal(1, await sut.CountUnreadAsync(userId));
        var list = await sut.ListForUserAsync(userId, take: 10);
        Assert.Single(list);
        Assert.Equal("Live melding", list[0].Title);
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

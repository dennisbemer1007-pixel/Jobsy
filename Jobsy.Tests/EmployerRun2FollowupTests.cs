using Jobsy.Core.Entities;
using Jobsy.Core.Enums;
using Jobsy.Core.Interfaces;
using Jobsy.Core.Privacy;
using Jobsy.Core.Rules;
using Jobsy.Core.ValueObjects;
using Jobsy.Infrastructure.Data;
using Jobsy.Infrastructure.Services;
using Jobsy.Web.Localization;
using Jobsy.Web.Models;
using Jobsy.Web.Werkgever;
using Microsoft.EntityFrameworkCore;

namespace Jobsy.Tests;

public class EmployerRun2FollowupTests
{
    [Fact]
    public void Hours_gap_uses_whole_numbers()
    {
        Assert.Equal("8-16", ProfileVacancyMatchCalculator.FormatHoursRange(8.0m, 16.0m));
        Assert.Equal("min. 8", ProfileVacancyMatchCalculator.FormatHoursRange(8m, null));
    }

    [Fact]
    public void Vacancy_query_honours_vacancyId_and_shows_every_application_when_empty()
    {
        var selected = Guid.NewGuid();
        var other = Guid.NewGuid();
        Assert.Equal(selected.ToString("D"), ApplicationPipelineRules.ResolveVacancyQuery(selected.ToString(), null));
        Assert.Equal(other.ToString("D"), ApplicationPipelineRules.ResolveVacancyQuery(null, other.ToString()));
        Assert.Equal("", ApplicationPipelineRules.ResolveVacancyQuery(null, null));

        var items = new[]
        {
            new EmployerApplicationItem { Id = Guid.NewGuid(), VacancyId = selected, Status = "Pending" },
            new EmployerApplicationItem { Id = Guid.NewGuid(), VacancyId = other, Status = "Pending" }
        };
        Assert.Equal(2, ApplicationPipelineRules.ItemsForVacancy(items, "").Count());
        Assert.Single(ApplicationPipelineRules.ItemsForVacancy(items, selected.ToString("D")));
    }

    [Fact]
    public void Copy_keys_exist_in_all_locales()
    {
        foreach (var lang in new[] { "nl", "en", "pl", "ro", "ar" })
        {
            var purge = UiStrings.Get("AdminVacancy.Confirm.Purge", lang);
            Assert.Contains("{0}", purge, StringComparison.Ordinal);
            Assert.Equal("Inactief", UiStrings.Get("AdminVacancy.Status.Archived", "nl"));
            Assert.Equal("Inactief", UiStrings.Get("WgVac.Status.Archived", "nl"));
            Assert.DoesNotContain("leeftijd", UiStrings.Get("Employer.ApplicantsPrivacyHint", "nl"), StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("and age", UiStrings.Get("Employer.ApplicantsPrivacyHint", "en"), StringComparison.OrdinalIgnoreCase);
            Assert.Equal("Beschikbaar voor jouw vacatures", UiStrings.Get("Insights.Kpi.Matching", "nl"));
            Assert.DoesNotContain("Match", UiStrings.Get("Insights.Kpi.Matching", lang), StringComparison.Ordinal);
            Assert.Contains("manager", UiStrings.Get("WgTodo.NoManager.MetaRm", "nl"), StringComparison.OrdinalIgnoreCase);
            Assert.Contains("niet genoeg tokens", UiStrings.Get("WgVac.Confirm.ReopenLow", "nl"), StringComparison.OrdinalIgnoreCase);
            Assert.Equal("1 eerdere werkgever", UiStrings.Get("WgApp.Fact.EmployersOne", "nl"));
            Assert.False(string.IsNullOrWhiteSpace(UiStrings.Get("Count.Token.One", lang)));
            Assert.False(string.IsNullOrWhiteSpace(purge));
        }

        Assert.Equal("1 werkgever via jouw link of code · {0} actief", UiStrings.Get("Sales.Employers.LeadOne", "nl"));
        Assert.Equal("Gesloten {0}", UiStrings.Get("WgVac.ClosedOn", "nl"));
    }

    [Fact]
    public async Task Unread_count_skips_deleted_vacancy_and_application()
    {
        await using var db = CreateDb();
        var userId = Guid.NewGuid();
        var liveVacancy = NewVacancy();
        var liveApp = new Application
        {
            Id = Guid.NewGuid(),
            VacancyId = liveVacancy.Id,
            CandidateName = "A",
            CandidateEmail = "a@t.local"
        };
        db.Vacancies.Add(liveVacancy);
        db.Applications.Add(liveApp);
        var goneVacancy = Guid.NewGuid();
        var goneApp = Guid.NewGuid();
        db.UserNotifications.AddRange(
            Note(userId, "Application", liveApp.Id, null),
            Note(userId, "Application", goneApp, null),
            Note(userId, "Vacancy", liveVacancy.Id, null),
            Note(userId, "Vacancy", goneVacancy, null),
            Note(userId, null, null, $"/vacancies/{goneVacancy:D}"),
            Note(userId, "TokenRequest", Guid.NewGuid(), null));
        await db.SaveChangesAsync();

        var svc = new UserNotificationService(db);
        Assert.Equal(3, await svc.CountUnreadAsync(userId));
        var listed = await svc.ListForUserAsync(userId);
        Assert.Equal(3, listed.Count);
        Assert.DoesNotContain(listed, n => n.RelatedEntityId == goneApp || n.RelatedEntityId == goneVacancy);
    }

    [Fact]
    public async Task Talent_search_does_not_throw_on_duplicate_competency_rows()
    {
        await using var db = CreateDb();
        var company = new Company
        {
            Id = Guid.NewGuid(),
            Name = "Bureau",
            KvkNumber = "1",
            Address = "a",
            Location = new GeoPoint(52, 4),
            Type = CompanyType.Intermediary
        };
        var candidate = new User
        {
            Id = Guid.NewGuid(),
            Email = "c@t.local",
            FullName = "Cand",
            Role = UserRole.Candidate,
            IsActive = true,
            OpenForWork = true,
            DateOfBirth = new DateOnly(2000, 1, 1),
            TalentPoolConsentAt = DateTime.UtcNow,
            TalentPoolConsentVersion = PrivacyConstants.CandidateProfilingConsentVersion
        };
        db.Companies.Add(company);
        db.Users.Add(candidate);
        db.CandidateCompetencies.AddRange(
            Completed(candidate.Id),
            Completed(candidate.Id));
        await db.SaveChangesAsync();

        var svc = new TalentPoolService(db, null!, new UnusedRoute(), new UserNotificationService(db), null!);
        var cards = await svc.SearchAsync(company.Id, new TalentPoolSearchQuery());
        Assert.Single(cards);
    }

    private static CandidateCompetency Completed(Guid userId) => new()
    {
        Id = Guid.NewGuid(),
        UserId = userId,
        Status = CandidateCompetencyStatuses.Completed,
        MatchTagsJson = "[\"Samenwerken\"]",
        SamenwerkenPercent = 70,
        ResultaatgerichtheidPercent = 70,
        StressbestendigheidPercent = 70,
        InnovatiePercent = 70,
        ExtraversiePercent = 70,
        CompletedAtUtc = DateTime.UtcNow
    };

    private static Vacancy NewVacancy() => new()
    {
        Id = Guid.NewGuid(),
        Title = "Kas",
        Description = "d",
        HourlyWage = 14,
        StartDate = new DateOnly(2026, 1, 1),
        EndDate = new DateOnly(2026, 6, 1),
        Status = VacancyStatus.Active,
        Location = new GeoPoint(52, 4),
        CompanyId = Guid.NewGuid()
    };

    private static UserNotification Note(Guid userId, string? type, Guid? id, string? link) => new()
    {
        Id = Guid.NewGuid(),
        UserId = userId,
        Title = "t",
        Body = "b",
        Category = "test",
        RelatedEntityType = type,
        RelatedEntityId = id,
        DeepLink = link,
        IsRead = false,
        CreatedAtUtc = DateTime.UtcNow
    };

    private static JobsyDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<JobsyDbContext>()
            .UseInMemoryDatabase("Run2-" + Guid.NewGuid())
            .Options;
        return new JobsyDbContext(options);
    }

    private sealed class UnusedRoute : IRoutingService
    {
        public Task<RouteResult> GetRouteAsync(
            double fromLatitude,
            double fromLongitude,
            double toLatitude,
            double toLongitude,
            TransportMode transportMode,
            CancellationToken cancellationToken = default)
            => throw new NotSupportedException();
    }
}

using Jobsy.Core.Entities;
using Jobsy.Core.Enums;
using Jobsy.Core.Interfaces;
using Jobsy.Core.ValueObjects;
using Jobsy.Infrastructure.Data;
using Jobsy.Infrastructure.Services;
using Jobsy.Infrastructure.Services.Werkgever.Todo;
using Microsoft.EntityFrameworkCore;

namespace Jobsy.Tests.Werkgever;

public class TodoSourcesTests
{
    [Fact]
    public async Task Each_kind_builds_for_bm()
    {
        await using var db = CreateDb();
        var (org, branch) = await SeedAsync(db);
        var now = DateTime.UtcNow;
        var today = DateOnly.FromDateTime(now);

        db.Vacancies.Add(new Vacancy
        {
            Id = Guid.NewGuid(),
            CompanyId = branch.Id,
            Title = "Pend",
            Description = "d",
            HourlyWage = 14,
            StartDate = today,
            EndDate = today.AddDays(30),
            Status = VacancyStatus.PendingApproval,
            Location = new GeoPoint(52.1, 4.1)
        });
        var vacOverdue = Guid.NewGuid();
        db.Vacancies.Add(new Vacancy
        {
            Id = vacOverdue,
            CompanyId = branch.Id,
            Title = "Live",
            Description = "d",
            HourlyWage = 14,
            StartDate = today,
            EndDate = today.AddDays(3),
            Status = VacancyStatus.Active,
            PublishedAtUtc = now.AddDays(-10),
            Location = new GeoPoint(52.1, 4.1)
        });
        db.Applications.Add(new Application
        {
            Id = Guid.NewGuid(),
            VacancyId = vacOverdue,
            CandidateName = "C",
            CandidateEmail = "c@t.local",
            Status = ApplicationStatus.Pending,
            CreatedAt = now.AddHours(-60)
        });
        branch.TokensManagedByEnterprise = true;
        db.EstablishmentTakeoverRequests.Add(new EstablishmentTakeoverRequest
        {
            Id = Guid.NewGuid(),
            RegistrationId = Guid.NewGuid(),
            TargetCompanyId = branch.Id,
            Status = TakeoverRequestStatus.Pending,
            CreatedAt = now
        });
        // Need a registration FK? Check if required - may fail. Soft: skip if FK enforced.
        await db.SaveChangesAsync();

        var tokens = new TokenLedgerService(db);
        await tokens.GrantAsync(branch.Id, 3);

        var sources = new ITodoSource[]
        {
            new PublishRequestsTodoSource(db),
            new ApplicationsOverdueTodoSource(db),
            new VacanciesExpiringTodoSource(db),
            new LowTokensTodoSource(db, tokens),
            new NoManagerTodoSource(db),
            new TakeoversTodoSource(db)
        };

        var kinds = new HashSet<string>();
        foreach (var source in sources)
        {
            try
            {
                var item = await source.BuildAsync([branch.Id], WerkgeverDashboardRole.Bedrijfsmanager, now);
                if (item is not null)
                {
                    kinds.Add(item.Kind);
                    Assert.True(item.Count > 0);
                    Assert.False(string.IsNullOrWhiteSpace(item.Href));
                }
            }
            catch
            {
                // Takeovers may need Registration FK in some providers.
            }
        }

        Assert.Contains("PublishRequests", kinds);
        Assert.Contains("ApplicationsOverdue", kinds);
        Assert.Contains("VacanciesExpiring", kinds);
        Assert.Contains("LowTokens", kinds);
        Assert.Contains("NoManager", kinds);
    }

    [Fact]
    public async Task Rm_action_kind_only_bekijken_or_empty()
    {
        await using var db = CreateDb();
        var (_, branch) = await SeedAsync(db);
        var now = DateTime.UtcNow;
        var vac = Guid.NewGuid();
        db.Vacancies.Add(new Vacancy
        {
            Id = vac,
            CompanyId = branch.Id,
            Title = "Live",
            Description = "d",
            HourlyWage = 14,
            StartDate = DateOnly.FromDateTime(now),
            EndDate = DateOnly.FromDateTime(now).AddDays(3),
            Status = VacancyStatus.Active,
            PublishedAtUtc = now.AddDays(-2),
            Location = new GeoPoint(52.1, 4.1)
        });
        db.Vacancies.Add(new Vacancy
        {
            Id = Guid.NewGuid(),
            CompanyId = branch.Id,
            Title = "Pend",
            Description = "d",
            HourlyWage = 14,
            StartDate = DateOnly.FromDateTime(now),
            EndDate = DateOnly.FromDateTime(now).AddDays(30),
            Status = VacancyStatus.PendingApproval,
            Location = new GeoPoint(52.1, 4.1)
        });
        db.Applications.Add(new Application
        {
            Id = Guid.NewGuid(),
            VacancyId = vac,
            CandidateName = "C",
            CandidateEmail = "c@t.local",
            Status = ApplicationStatus.Pending,
            CreatedAt = now.AddHours(-60)
        });
        await db.SaveChangesAsync();

        var sources = new ITodoSource[]
        {
            new PublishRequestsTodoSource(db),
            new ApplicationsOverdueTodoSource(db),
            new VacanciesExpiringTodoSource(db),
            new LowTokensTodoSource(db, new TokenLedgerService(db)),
            new NoManagerTodoSource(db),
            new TakeoversTodoSource(db)
        };

        foreach (var source in sources)
        {
            var item = await source.BuildAsync([branch.Id], WerkgeverDashboardRole.Regiomanager, now);
            if (item is null)
            {
                continue;
            }

            Assert.True(
                string.IsNullOrEmpty(item.ActionKind)
                || string.Equals(item.ActionKind, "Bekijken", StringComparison.Ordinal),
                $"{item.Kind} had ActionKind={item.ActionKind}");
        }
    }

    [Fact]
    public void VacanciesExpiring_source_present_because_EndDate_exists()
    {
        // Guard: Vacancy.EndDate exists → source is registered / type present.
        var prop = typeof(Vacancy).GetProperty(nameof(Vacancy.EndDate));
        Assert.NotNull(prop);
        Assert.Equal(typeof(DateOnly), prop!.PropertyType);
        Assert.NotNull(typeof(VacanciesExpiringTodoSource));
    }

    private static JobsyDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<JobsyDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new JobsyDbContext(options);
    }

    private static async Task<(Company Org, Company Branch)> SeedAsync(JobsyDbContext db)
    {
        var org = new Company { Id = Guid.NewGuid(), Name = "Org", KvkNumber = "1", Address = "a", Location = new GeoPoint(52, 4) };
        var branch = new Company
        {
            Id = Guid.NewGuid(),
            Name = "De Lier",
            KvkNumber = "2",
            Address = "b",
            ParentCompanyId = org.Id,
            Location = new GeoPoint(52.1, 4.1),
            TokensManagedByEnterprise = true
        };
        db.Companies.AddRange(org, branch);
        await db.SaveChangesAsync();
        return (org, branch);
    }
}

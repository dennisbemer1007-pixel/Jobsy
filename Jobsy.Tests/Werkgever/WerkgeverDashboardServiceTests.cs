using Jobsy.Core.Entities;
using Jobsy.Core.Enums;
using Jobsy.Core.Interfaces;
using Jobsy.Core.ValueObjects;
using Jobsy.Infrastructure.Data;
using Jobsy.Infrastructure.Services;
using Jobsy.Infrastructure.Services.Werkgever;
using Jobsy.Infrastructure.Services.Werkgever.Todo;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace Jobsy.Tests.Werkgever;

public class WerkgeverDashboardServiceTests
{
    [Fact]
    public async Task Vm_sees_only_own_vestiging_kpis()
    {
        await using var db = CreateDb();
        var (org, a, b) = await SeedOrgAsync(db);
        await SeedVacancyAsync(db, a.Id, VacancyStatus.Active);
        await SeedVacancyAsync(db, b.Id, VacancyStatus.Active);
        var tokens = new TokenLedgerService(db);
        var sut = CreateSut(db, tokens);

        var dto = await sut.GetDashboardAsync([a.Id], "30d", WerkgeverDashboardRole.Vestigingsmanager);
        Assert.Equal(1, dto.VestigingCount);
        Assert.Equal(1, dto.Kpis.ActiveVacancies);
        Assert.Single(dto.Branches);
        Assert.Equal(a.Id, dto.Branches[0].CompanyId);
        Assert.True(dto.ShowTokenBalance);
        Assert.False(dto.ShowTokenUsage);
    }

    [Fact]
    public async Task Rm_sees_region_scope_and_token_usage()
    {
        await using var db = CreateDb();
        var (org, a, b) = await SeedOrgAsync(db);
        await SeedVacancyAsync(db, a.Id, VacancyStatus.Active);
        await SeedVacancyAsync(db, b.Id, VacancyStatus.Active);
        var tokens = new TokenLedgerService(db);
        await tokens.GrantAsync(a.Id, 10);
        db.TokenTransactions.Add(new TokenTransaction
        {
            Id = Guid.NewGuid(),
            CompanyId = a.Id,
            Amount = -2m,
            Kind = TokenTransactionKind.Spend,
            Reason = TokenSpendReason.Publish,
            OldBalance = 10,
            NewBalance = 8,
            CreatedAt = DateTime.UtcNow.AddDays(-1)
        });
        await db.SaveChangesAsync();
        var sut = CreateSut(db, tokens);

        var dto = await sut.GetDashboardAsync([a.Id, b.Id], "30d", WerkgeverDashboardRole.Regiomanager);
        Assert.Equal(2, dto.Kpis.ActiveVacancies);
        Assert.True(dto.ShowTokenUsage);
        Assert.False(dto.ShowTokenBalance);
        Assert.Equal(2m, dto.Kpis.TokensUsed);
    }

    [Fact]
    public async Task Empty_scope_yields_zeros()
    {
        await using var db = CreateDb();
        var sut = CreateSut(db, new TokenLedgerService(db));
        var emptyId = Guid.NewGuid();
        db.Companies.Add(new Company
        {
            Id = emptyId,
            Name = "Empty",
            KvkNumber = "9",
            Address = "x",
            Location = new GeoPoint(52, 4)
        });
        await db.SaveChangesAsync();

        var dto = await sut.GetDashboardAsync([emptyId], "7d", WerkgeverDashboardRole.Bedrijfsmanager);
        Assert.Equal(0, dto.Kpis.ActiveVacancies);
        Assert.Equal(0, dto.Kpis.NewApplications);
        Assert.Equal(0, dto.Funnel.Applications);
        Assert.Empty(await sut.GetTodoAsync([emptyId], WerkgeverDashboardRole.Bedrijfsmanager));
    }

    [Fact]
    public async Task Deltas_compare_to_previous_period()
    {
        await using var db = CreateDb();
        var (org, a, _) = await SeedOrgAsync(db);
        var now = DateTime.UtcNow;
        await SeedApplicationAsync(db, a.Id, ApplicationStatus.Pending, now.AddDays(-2));
        await SeedApplicationAsync(db, a.Id, ApplicationStatus.Pending, now.AddDays(-40));
        var sut = CreateSut(db, new TokenLedgerService(db));
        var dto = await sut.GetDashboardAsync([a.Id], "30d", WerkgeverDashboardRole.Bedrijfsmanager);
        Assert.Equal(1, dto.Kpis.NewApplications);
        Assert.NotNull(dto.Kpis.NewApplicationsDeltaPercent);
    }

    private static WerkgeverDashboardService CreateSut(JobsyDbContext db, ITokenLedgerService tokens)
    {
        ITodoSource[] sources =
        [
            new PublishRequestsTodoSource(db),
            new ApplicationsOverdueTodoSource(db),
            new VacanciesExpiringTodoSource(db),
            new LowTokensTodoSource(db, tokens),
            new NoManagerTodoSource(db),
            new TakeoversTodoSource(db)
        ];
        return new WerkgeverDashboardService(db, tokens, sources, new MemoryCache(new MemoryCacheOptions()));
    }

    private static JobsyDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<JobsyDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new JobsyDbContext(options);
    }

    private static async Task<(Company Org, Company A, Company B)> SeedOrgAsync(JobsyDbContext db)
    {
        var org = new Company { Id = Guid.NewGuid(), Name = "Org", KvkNumber = "1", Address = "a", Location = new GeoPoint(52, 4) };
        var a = new Company { Id = Guid.NewGuid(), Name = "A", KvkNumber = "2", Address = "a", ParentCompanyId = org.Id, Location = new GeoPoint(52.1, 4.1), TokensManagedByEnterprise = true };
        var b = new Company { Id = Guid.NewGuid(), Name = "B", KvkNumber = "3", Address = "b", ParentCompanyId = org.Id, Location = new GeoPoint(52.2, 4.2), TokensManagedByEnterprise = true };
        db.Companies.AddRange(org, a, b);
        await db.SaveChangesAsync();
        return (org, a, b);
    }

    private static async Task SeedVacancyAsync(JobsyDbContext db, Guid companyId, VacancyStatus status)
    {
        db.Vacancies.Add(new Vacancy
        {
            Id = Guid.NewGuid(),
            CompanyId = companyId,
            Title = "T",
            Description = "D",
            HourlyWage = 14,
            StartDate = DateOnly.FromDateTime(DateTime.UtcNow),
            EndDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(30)),
            Status = status,
            PublishedAtUtc = DateTime.UtcNow.AddDays(-10),
            Location = new GeoPoint(52.1, 4.1)
        });
        await db.SaveChangesAsync();
    }

    private static async Task SeedApplicationAsync(
        JobsyDbContext db,
        Guid companyId,
        ApplicationStatus status,
        DateTime createdAt)
    {
        var vacancyId = Guid.NewGuid();
        db.Vacancies.Add(new Vacancy
        {
            Id = vacancyId,
            CompanyId = companyId,
            Title = "T",
            Description = "D",
            HourlyWage = 14,
            StartDate = DateOnly.FromDateTime(DateTime.UtcNow),
            EndDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(30)),
            Status = VacancyStatus.Active,
            PublishedAtUtc = DateTime.UtcNow.AddDays(-20),
            Location = new GeoPoint(52.1, 4.1)
        });
        db.Applications.Add(new Application
        {
            Id = Guid.NewGuid(),
            VacancyId = vacancyId,
            CandidateName = "C",
            CandidateEmail = "c@test.local",
            Status = status,
            CreatedAt = createdAt
        });
        await db.SaveChangesAsync();
    }
}

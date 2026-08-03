using Jobsy.Core.Entities;
using Jobsy.Core.Enums;
using Jobsy.Core.Interfaces;
using Jobsy.Core.ValueObjects;
using Jobsy.Infrastructure.Data;
using Jobsy.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;

namespace Jobsy.Tests;

public class EmployerRaamflyerServiceTests
{
    [Fact]
    public async Task Resolve_One_Active_Vacancy_Targets_Vacancy_Detail_Kind()
    {
        await using var db = CreateDb();
        var companyId = Guid.NewGuid();
        var vacancyId = Guid.NewGuid();
        SeedCompany(db, companyId, "Filiaal West");
        SeedVacancy(db, vacancyId, companyId, "Medewerker");
        await db.SaveChangesAsync();

        var sut = CreateSut(db);
        var target = await sut.ResolveBranchQrTargetAsync(companyId);

        Assert.Equal(RaamflyerQrKind.VacancyDetail, target.Kind);
        Assert.Equal(1, target.ActiveVacancyCount);
        Assert.Equal(vacancyId, target.SingleVacancyId);
        Assert.Contains($"/vestiging/{companyId:D}", target.AbsoluteUrl, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Resolve_Two_Active_Vacancies_Targets_Map_Cluster_Kind()
    {
        await using var db = CreateDb();
        var companyId = Guid.NewGuid();
        SeedCompany(db, companyId, "Filiaal Centrum");
        SeedVacancy(db, Guid.NewGuid(), companyId, "Kassamedewerker");
        SeedVacancy(db, Guid.NewGuid(), companyId, "Vulploeg");
        await db.SaveChangesAsync();

        var sut = CreateSut(db);
        var target = await sut.ResolveBranchQrTargetAsync(companyId);

        Assert.Equal(RaamflyerQrKind.MapCompanyCluster, target.Kind);
        Assert.Equal(2, target.ActiveVacancyCount);
        Assert.Null(target.SingleVacancyId);
        Assert.Contains($"/vestiging/{companyId:D}", target.AbsoluteUrl, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Resolve_Zero_Active_Vacancies_Targets_Empty_Branch_Kind()
    {
        await using var db = CreateDb();
        var companyId = Guid.NewGuid();
        SeedCompany(db, companyId, "Filiaal Zuid");
        await db.SaveChangesAsync();

        var sut = CreateSut(db);
        var target = await sut.ResolveBranchQrTargetAsync(companyId);

        Assert.Equal(RaamflyerQrKind.MapEmptyBranch, target.Kind);
        Assert.Equal(0, target.ActiveVacancyCount);
    }

    [Fact]
    public async Task Render_Branch_And_Overview_Produce_Pdf_Bytes()
    {
        await using var db = CreateDb();
        var companyA = Guid.NewGuid();
        var companyB = Guid.NewGuid();
        SeedCompany(db, companyA, "Vestiging A");
        SeedCompany(db, companyB, "Vestiging B");
        SeedVacancy(db, Guid.NewGuid(), companyA, "Hulp in de zaak");
        SeedVacancy(db, Guid.NewGuid(), companyA, "Bezorging");
        await db.SaveChangesAsync();

        var sut = CreateSut(db);
        var branch = await sut.RenderBranchFlyerAsync(companyA, RaamflyerFormat.A4);
        var overview = await sut.RenderOverviewFlyerAsync(
            [companyA, companyB],
            "Westland",
            RaamflyerFormat.A3);

        Assert.True(branch.Length > 500);
        Assert.True(overview.Length > 500);
        Assert.Equal((byte)'%', branch[0]);
        Assert.Equal((byte)'P', branch[1]);
        Assert.Equal((byte)'%', overview[0]);
    }

    [Fact]
    public async Task Resolve_Unknown_Company_Throws()
    {
        await using var db = CreateDb();
        var sut = CreateSut(db);
        await Assert.ThrowsAsync<KeyNotFoundException>(
            () => sut.ResolveBranchQrTargetAsync(Guid.NewGuid()));
    }

    private static EmployerRaamflyerService CreateSut(JobsyDbContext db)
        => new(db, new FakeFeatures(), new FakeCompanySettings());

    private static JobsyDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<JobsyDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new JobsyDbContext(options);
    }

    private static void SeedCompany(JobsyDbContext db, Guid id, string name)
    {
        db.Companies.Add(new Company
        {
            Id = id,
            Name = name,
            Address = "Voorbeeldstraat 1, Naaldwijk",
            KvkNumber = "12345678",
            Location = new GeoPoint(51.99, 4.21),
            Type = CompanyType.Employer
        });
    }

    private static void SeedVacancy(JobsyDbContext db, Guid id, Guid companyId, string title)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        db.Vacancies.Add(new Vacancy
        {
            Id = id,
            CompanyId = companyId,
            Title = title,
            Description = "Demo",
            Status = VacancyStatus.Active,
            StartDate = today.AddDays(-1),
            EndDate = today.AddDays(30),
            Location = new GeoPoint(51.99, 4.21),
            RequiredTransport = TransportMode.Bike,
            WorkTypes = WorkType.None,
            WorkTypeLabels = "Retail",
            MinHoursPerWeek = 8,
            MaxHoursPerWeek = 24,
            HourlyWage = 14m
        });
    }

    private sealed class FakeCompanySettings : IPlatformCompanySettingsService
    {
        public Task<PlatformCompanySnapshot> GetAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(new PlatformCompanySnapshot(
                "Lobsy", "Slogan", null, null, null, null, null, null, null, null, null, null));

        public Task<PlatformCompanySnapshot> UpdateAsync(
            PlatformCompanyUpdate update,
            CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public byte[] GetBrandLogoPng() => [];

        public byte[] GetBrandWatermarkPng() => [];
    }

    private sealed class FakeFeatures : IPlatformFeatureService
    {
        public Task<PlatformFeatureSnapshot> GetAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(new PlatformFeatureSnapshot(
                false, false, false, "https://lobsy.nl", null));

        public Task<PlatformFeatureSnapshot> UpdateAsync(
            PlatformFeatureUpdate update,
            CancellationToken cancellationToken = default)
            => throw new NotSupportedException();
    }
}

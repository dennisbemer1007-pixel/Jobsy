using Jobsy.Core.Enums;
using Jobsy.Core.Rules;
using Jobsy.Infrastructure.Data;
using Jobsy.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;

namespace Jobsy.Tests;

public class VacancyCategoryServiceTests
{
    [Fact]
    public async Task EnsureDefaults_seeds_seven_categories_and_volunteer_is_free()
    {
        await using var db = CreateDb();
        var sut = new VacancyCategoryService(db);

        var active = await sut.GetActiveAsync();
        var all = await sut.GetAllAdminAsync();

        Assert.Equal(6, active.Count);
        Assert.Equal(7, all.Count);
        Assert.DoesNotContain(active, c => c.Slug == "highlight");
        Assert.Contains(all, c => c.Slug == "highlight" && !c.IsActive && !c.ShowInMapFilter);
        var volunteer = Assert.Single(active, c => c.Slug == "vrijwilligerswerk");
        Assert.True(volunteer.IsAlwaysFree);
        Assert.Equal(0m, volunteer.PublishCostTokens);
        var internship = Assert.Single(active, c => c.Slug == "stageplekken");
        Assert.False(internship.IsAlwaysFree);
        Assert.Equal(0m, internship.PublishCostTokens);
        Assert.False(volunteer.HighlightAvailable);
        Assert.False(volunteer.PushBomAvailable);
        Assert.Contains(VacancyCategoryExtraFields.OrganizationType, volunteer.ExtraFields);
        Assert.All(active, c => Assert.Matches("^#[0-9A-F]{6}$", c.ColorHex));
    }

    [Fact]
    public async Task EnsureDefaults_backfills_legacy_internship_publish_cost_to_zero()
    {
        await using var db = CreateDb();
        db.VacancyCategories.Add(new Jobsy.Core.Entities.VacancyCategory
        {
            Id = VacancyCategoryDefaults.InternshipId,
            Slug = "stageplekken",
            Name = "Stageplekken",
            ColorHex = "#0EA5E9",
            PublishCostTokens = 0.5m,
            HighlightAvailable = true,
            HighlightCostTokens = 1m,
            PlacementKind = VacancyKind.Internship,
            IsActive = true
        });
        await db.SaveChangesAsync();

        var sut = new VacancyCategoryService(db);
        await sut.EnsureDefaultsAsync();

        var internship = await sut.GetEntityAsync(VacancyCategoryDefaults.InternshipId);
        Assert.Equal(0m, internship!.PublishCostTokens);
    }

    [Fact]
    public async Task ResolvePricing_uses_category_costs_and_gates_upgrades()
    {
        await using var db = CreateDb();
        var sut = new VacancyCategoryService(db);
        await sut.EnsureDefaultsAsync();

        var inclusive = await sut.ResolvePricingAsync(VacancyCategoryDefaults.InclusiefId, VacancyKind.Regular);
        Assert.Equal(0.5m, inclusive.PublishCostTokens);
        Assert.True(inclusive.HighlightAvailable);
        Assert.Equal(1m, inclusive.HighlightCostTokens);
        Assert.True(inclusive.PushBomAvailable);
        Assert.Equal(2m, inclusive.PushBomCostTokens);
        Assert.False(inclusive.UseTierPushBomPricing);

        var volunteer = await sut.ResolvePricingAsync(VacancyCategoryDefaults.VolunteerId, VacancyKind.Volunteer);
        Assert.Equal(0m, volunteer.PublishCostTokens);
        Assert.False(volunteer.HighlightAvailable);
        Assert.False(volunteer.PushBomAvailable);

        var internship = await sut.ResolvePricingAsync(VacancyCategoryDefaults.InternshipId, VacancyKind.Internship);
        Assert.Equal(0m, internship.PublishCostTokens);
        Assert.True(internship.HighlightAvailable);
    }

    [Fact]
    public async Task Create_and_update_category_drives_extra_fields()
    {
        await using var db = CreateDb();
        var sut = new VacancyCategoryService(db);

        var created = await sut.CreateAsync(
            "Seizoenswerk",
            "#112233",
            1.5m,
            highlightAvailable: true,
            highlightCostTokens: 2m,
            pushBomAvailable: false,
            pushBomCostTokens: null,
            isAlwaysFree: false,
            VacancyKind.Regular,
            [VacancyCategoryExtraFields.ContractType, VacancyCategoryExtraFields.HoursPerWeek],
            sortOrder: 99,
            showInMapFilter: true,
            showInLegend: true);

        Assert.Equal("Seizoenswerk", created.Name);
        Assert.Equal("#112233", created.ColorHex);
        Assert.False(created.PushBomAvailable);
        Assert.Equal(2, created.ExtraFields.Count);

        var updated = await sut.UpdateAsync(
            created.Id,
            "Seizoenswerk NL",
            "#AABBCC",
            2m,
            highlightAvailable: false,
            highlightCostTokens: 9m,
            pushBomAvailable: true,
            pushBomCostTokens: 4m,
            isAlwaysFree: false,
            VacancyKind.Regular,
            [VacancyCategoryExtraFields.ExperienceLevel],
            sortOrder: 5,
            isActive: true,
            showInMapFilter: true,
            showInLegend: false);

        Assert.NotNull(updated);
        Assert.Equal("Seizoenswerk NL", updated!.Name);
        Assert.False(updated.HighlightAvailable);
        Assert.Equal(0m, updated.HighlightCostTokens);
        Assert.True(updated.PushBomAvailable);
        Assert.Equal(4m, updated.PushBomCostTokens);
        Assert.False(updated.ShowInLegend);
        Assert.Equal([VacancyCategoryExtraFields.ExperienceLevel], updated.ExtraFields);
    }

    [Fact]
    public async Task Always_free_forces_zero_cost_and_no_upgrades()
    {
        await using var db = CreateDb();
        var sut = new VacancyCategoryService(db);

        var created = await sut.CreateAsync(
            "Buurtvrijwillig",
            "#10B981",
            publishCostTokens: 5m,
            highlightAvailable: true,
            highlightCostTokens: 3m,
            pushBomAvailable: true,
            pushBomCostTokens: 2m,
            isAlwaysFree: true,
            VacancyKind.Regular,
            [VacancyCategoryExtraFields.Frequency],
            sortOrder: null,
            showInMapFilter: true,
            showInLegend: true);

        Assert.True(created.IsAlwaysFree);
        Assert.Equal(0m, created.PublishCostTokens);
        Assert.False(created.HighlightAvailable);
        Assert.False(created.PushBomAvailable);
        Assert.Equal(VacancyKind.Volunteer.ToString(), created.PlacementKind);
    }

    [Fact]
    public async Task Backfill_assigns_default_category_from_kind()
    {
        await using var db = CreateDb();
        var company = new Jobsy.Core.Entities.Company
        {
            Id = Guid.NewGuid(),
            Name = "Test BV",
            Location = new Jobsy.Core.ValueObjects.GeoPoint(52.0, 4.3)
        };
        db.Companies.Add(company);
        db.Vacancies.Add(new Jobsy.Core.Entities.Vacancy
        {
            Id = Guid.NewGuid(),
            Title = "Stage",
            Description = "Desc",
            CompanyId = company.Id,
            Kind = VacancyKind.Internship,
            Status = Jobsy.Core.Enums.VacancyStatus.Draft,
            Location = company.Location,
            StartDate = DateOnly.FromDateTime(DateTime.UtcNow),
            EndDate = DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(3))
        });
        await db.SaveChangesAsync();

        var sut = new VacancyCategoryService(db);
        await sut.BackfillVacancyCategoriesAsync();

        var vacancy = await db.Vacancies.SingleAsync();
        Assert.Equal(VacancyCategoryDefaults.InternshipId, vacancy.CategoryId);
    }

    private static JobsyDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<JobsyDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;
        return new JobsyDbContext(options);
    }
}

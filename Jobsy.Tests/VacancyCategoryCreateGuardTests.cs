using Jobsy.Core.Enums;
using Jobsy.Core.Rules;
using Jobsy.Infrastructure.Data;
using Jobsy.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;

namespace Jobsy.Tests;

public class VacancyCategoryCreateGuardTests
{
    [Fact]
    public async Task Free_categories_are_always_volunteer_placement()
    {
        await using var db = CreateDb();
        var sut = new VacancyCategoryService(db);
        await sut.EnsureDefaultsAsync();

        var volunteer = await sut.GetEntityAsync(VacancyCategoryDefaults.VolunteerId);
        Assert.NotNull(volunteer);
        Assert.True(volunteer!.IsAlwaysFree);
        Assert.Equal(VacancyKind.Volunteer, volunteer.PlacementKind);
        Assert.Equal(0m, volunteer.PublishCostTokens);
        Assert.False(volunteer.HighlightAvailable);
        Assert.False(volunteer.PushBomAvailable);
    }

    [Fact]
    public async Task Public_active_categories_omit_inactive_defaults_after_deactivate()
    {
        await using var db = CreateDb();
        var sut = new VacancyCategoryService(db);
        await sut.EnsureDefaultsAsync();

        await sut.UpdateAsync(
            VacancyCategoryDefaults.RegulierId,
            "Reguliere vacature",
            "#F54A1B",
            1m,
            true,
            2m,
            true,
            null,
            false,
            VacancyKind.Regular,
            [VacancyCategoryExtraFields.ContractType],
            20,
            isActive: false,
            showInMapFilter: true,
            showInLegend: true);

        var active = await sut.GetActiveAsync();
        Assert.DoesNotContain(active, c => c.Id == VacancyCategoryDefaults.RegulierId);
        Assert.Contains(active, c => c.Id == VacancyCategoryDefaults.VolunteerId);
    }

    private static JobsyDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<JobsyDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;
        return new JobsyDbContext(options);
    }
}

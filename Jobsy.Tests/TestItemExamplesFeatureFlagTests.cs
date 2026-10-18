using Jobsy.Core.Features;
using Jobsy.Core.Interfaces;
using Jobsy.Core.Options;
using Jobsy.Infrastructure.Data;
using Jobsy.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

namespace Jobsy.Tests;

public sealed class TestItemExamplesFeatureFlagTests
{
    [Fact]
    public void Default_snapshot_is_off()
    {
        var snap = FeatureFlagSnapshot.Defaults;
        Assert.False(snap.TestItemExamplesEnabled);
        Assert.False(snap.IsEnabled(PlatformFeature.TestItemExamples));
    }

    [Fact]
    public async Task PlatformFeatureService_round_trips_test_item_examples()
    {
        await using var db = CreateDb();
        var sut = new PlatformFeatureService(
            db,
            Options.Create(new JobsyFeatureOptions()),
            new ConfigurationBuilder().Build());

        var updated = await sut.UpdateAsync(new PlatformFeatureUpdate(TestItemExamplesEnabled: true));
        Assert.True(updated.TestItemExamplesEnabled);

        var again = await sut.GetAsync();
        Assert.True(again.TestItemExamplesEnabled);
    }

    private static JobsyDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<JobsyDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;
        return new JobsyDbContext(options);
    }
}

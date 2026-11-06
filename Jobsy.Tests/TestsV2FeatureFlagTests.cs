using Jobsy.Core.Enums;
using Jobsy.Core.Features;
using Jobsy.Core.Interfaces;
using Jobsy.Infrastructure.Data;
using Jobsy.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Jobsy.Core.Options;

namespace Jobsy.Tests;

public sealed class TestsV2FeatureFlagTests
{
    [Fact]
    public void FeatureFlagSnapshot_tests_v2_off_by_default()
    {
        var snap = FeatureFlagSnapshot.Defaults;
        Assert.False(snap.TestsV2Enabled);
        Assert.False(snap.IsEnabled(PlatformFeature.TestsV2));
    }

    [Fact]
    public void QuickScanVersionPolicy_uses_v2_only_when_flag_on()
    {
        Assert.Equal(AssessmentTestVersions.V1, QuickScanVersionPolicy.ResolveVersionForNew(null, testsV2Enabled: false));
        Assert.Equal(AssessmentTestVersions.V3, QuickScanVersionPolicy.ResolveVersionForNew(null, testsV2Enabled: true));
        Assert.Equal(AssessmentTestVersions.V1, QuickScanVersionPolicy.ResolveVersionForNew(AssessmentTestVersions.V1, testsV2Enabled: true));
    }

    [Fact]
    public async Task PlatformFeatureService_round_trips_tests_v2()
    {
        await using var db = CreateDb();
        var sut = new PlatformFeatureService(
            db,
            Options.Create(new JobsyFeatureOptions()),
            new ConfigurationBuilder().Build());

        var updated = await sut.UpdateAsync(new PlatformFeatureUpdate(TestsV2Enabled: true));
        Assert.True(updated.TestsV2Enabled);

        var again = await sut.GetAsync();
        Assert.True(again.TestsV2Enabled);
    }

    private static JobsyDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<JobsyDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;
        return new JobsyDbContext(options);
    }
}

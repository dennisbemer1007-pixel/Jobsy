using Jobsy.Core.Enums;
using Jobsy.Core.Scholen.MboHbo;
using Jobsy.Core.WerkgeverApk;

namespace Jobsy.Tests.WerkgeverApk;

public sealed class WerkgeverApkGroupInsightsTests
{
    [Fact]
    public void Aggregate_hides_insights_below_min_group_size()
    {
        var states = Enumerable.Range(0, 4).Select(_ => EmptyState()).ToList();
        var managers = states.Select(_ => new Dictionary<string, int>()).ToList();
        var catalog = WerkgeverApkEmployeeAbCatalog.ForPackage(WerkgeverApkPackageKind.Basis);
        var dto = WerkgeverApkGroupInsights.Aggregate(states, managers, catalog, SchoolLevel.Mbo3);
        Assert.False(dto.Visible);
        Assert.Equal(4, dto.SharedCompletedCount);
    }

    [Fact]
    public void Aggregate_shows_insights_at_min_group_size()
    {
        var states = Enumerable.Range(0, WerkgeverApkAnonymity.MinGroupSize).Select(_ => EmptyState()).ToList();
        var managers = states.Select(_ => new Dictionary<string, int>()).ToList();
        var catalog = WerkgeverApkEmployeeAbCatalog.ForPackage(WerkgeverApkPackageKind.Basis);
        var dto = WerkgeverApkGroupInsights.Aggregate(states, managers, catalog, SchoolLevel.Mbo3);
        Assert.True(dto.Visible);
        Assert.Equal(WerkgeverApkAnonymity.MinGroupSize, dto.SharedCompletedCount);
    }

    private static PupilMboHboProgressState EmptyState() => PupilMboHboProgressState.Parse("{}");
}

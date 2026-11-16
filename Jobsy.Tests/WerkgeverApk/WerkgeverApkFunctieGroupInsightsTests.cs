using Jobsy.Core.Enums;
using Jobsy.Core.Scholen.MboHbo;
using Jobsy.Core.WerkgeverApk;

namespace Jobsy.Tests.WerkgeverApk;

public sealed class WerkgeverApkFunctieGroupInsightsTests
{
    private const string OrderpickerEsco = "cd94def5-3442-4c2e-ae3d-0761a3008bcb";

    [Fact]
    public void Aggregate_hides_bars_below_min_group_size()
    {
        var shared = Enumerable.Range(0, 4)
            .Select(_ => ((string?)OrderpickerEsco, PupilMboHboProgressState.Parse("{}")))
            .ToList();
        var dto = WerkgeverApkFunctieGroupInsights.Aggregate(shared, OrderpickerEsco, SchoolLevel.Mbo3);
        Assert.False(dto.Visible);
        Assert.Equal(4, dto.SharedCompletedCount);
        Assert.Empty(dto.Bars);
        Assert.Empty(dto.Training);
    }

    [Fact]
    public void Aggregate_never_leaks_per_person_below_threshold()
    {
        var shared = Enumerable.Range(0, 4)
            .Select(i => ((string?)OrderpickerEsco, State(i)))
            .ToList();
        var dto = WerkgeverApkFunctieGroupInsights.Aggregate(shared, OrderpickerEsco, SchoolLevel.Mbo3);
        Assert.All(dto.Bars, b => Assert.Equal(0, b.Total));
    }

    [Fact]
    public void Aggregate_shows_bars_at_min_group_size()
    {
        var shared = Enumerable.Range(0, WerkgeverApkAnonymity.MinGroupSize)
            .Select(i => ((string?)OrderpickerEsco, State(i)))
            .ToList();
        var dto = WerkgeverApkFunctieGroupInsights.Aggregate(shared, OrderpickerEsco, SchoolLevel.Mbo3);
        Assert.True(dto.Visible);
        Assert.NotEmpty(dto.Bars);
        Assert.Equal(WerkgeverApkAnonymity.MinGroupSize, dto.SharedCompletedCount);
    }

    [Fact]
    public void Aggregate_bars_are_unique_by_label()
    {
        var shared = Enumerable.Range(0, WerkgeverApkAnonymity.MinGroupSize)
            .Select(i => ((string?)OrderpickerEsco, State(i)))
            .ToList();
        var dto = WerkgeverApkFunctieGroupInsights.Aggregate(shared, OrderpickerEsco, SchoolLevel.Mbo3);
        Assert.True(dto.Visible);
        var labels = dto.Bars.Select(b => b.Label).ToList();
        Assert.Equal(labels.Count, labels.Distinct(StringComparer.OrdinalIgnoreCase).Count());
        Assert.Equal(1, labels.Count(l => l.Contains("Duidelijk doorgeven", StringComparison.OrdinalIgnoreCase)));
    }

    private static PupilMboHboProgressState State(int i)
    {
        var catalog = WerkgeverApkEmployeeCatalog.Default;
        var answers = new Dictionary<string, int>(StringComparer.Ordinal);
        foreach (var block in WerkgeverApkEntryPaths.EmployeeBlocks)
        {
            foreach (var q in catalog.QuestionsFor(block, SchoolLevel.Mbo3))
            {
                answers[q.Id] = 3 + (i % 2);
            }
        }

        return PupilMboHboProgressState.Parse(System.Text.Json.JsonSerializer.Serialize(new { answers }));
    }
}

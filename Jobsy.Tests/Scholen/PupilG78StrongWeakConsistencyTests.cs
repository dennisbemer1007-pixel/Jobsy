using Jobsy.Core.Scholen;

namespace Jobsy.Tests.Scholen;

public sealed class PupilG78StrongWeakConsistencyTests
{
    [Theory]
    [MemberData(nameof(Fixtures))]
    public void Strong_competency_labels_do_not_overlap_weak_mentions((Jobsy.Core.Entities.Scholen.PupilResult Result, Jobsy.Core.Entities.Scholen.PupilProgress Progress) fixture)
    {
        var pkg = PupilG78FactStory.Build(fixture.Result, fixture.Progress, "nl");
        var overlaps = PupilG78PageCoherence.FindStrongWeakOverlaps(pkg, fixture.Progress.AnswersJson);
        Assert.True(overlaps.Count == 0, string.Join(", ", overlaps));
    }

    [Theory]
    [MemberData(nameof(Fixtures))]
    public void Growth_dimensions_are_disjoint_from_story_strong_top_three(
        (Jobsy.Core.Entities.Scholen.PupilResult Result, Jobsy.Core.Entities.Scholen.PupilProgress Progress) fixture)
    {
        var answers = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, int>>(fixture.Progress.AnswersJson!)!;
        var avgs = PupilG78GrowthCompetencies.DimensionAverages(answers);
        var top = PupilG78GrowthCompetencies.TopStrongCategories(avgs, 3).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var pkg = PupilG78FactStory.Build(fixture.Result, fixture.Progress, "nl");
        foreach (var g in pkg.GrowthCompetencies)
        {
            Assert.DoesNotContain(g.Category, top);
        }
    }

    public static IEnumerable<object[]> Fixtures() =>
    [
        [PupilG78RealisticFixtures.HelpsOthers()],
        [PupilG78RealisticFixtures.LeadsGroup()],
        [PupilG78RealisticFixtures.Maker()],
    ];
}

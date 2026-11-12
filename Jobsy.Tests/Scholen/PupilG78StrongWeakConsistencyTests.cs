using Jobsy.Core.Entities.Scholen;
using Jobsy.Core.Scholen;

namespace Jobsy.Tests.Scholen;

public sealed class PupilG78StrongWeakConsistencyTests
{
    [Theory]
    [MemberData(nameof(Fixtures))]
    public void Strong_competency_labels_do_not_overlap_weak_mentions((PupilResult Result, PupilProgress Progress) fixture)
    {
        var pkg = PupilG78FactStory.Build(fixture.Result, fixture.Progress, "nl");
        var overlaps = PupilG78PageCoherence.FindStrongWeakOverlaps(pkg, fixture.Progress.AnswersJson);
        Assert.True(overlaps.Count == 0, string.Join(", ", overlaps));
    }

    [Theory]
    [MemberData(nameof(Fixtures))]
    public void Item_ids_are_unique_across_story_tiles_and_growth_tooltips(
        (PupilResult Result, PupilProgress Progress) fixture)
    {
        var pkg = PupilG78FactStory.Build(fixture.Result, fixture.Progress, "nl");
        var overlaps = PupilG78PageCoherence.FindDuplicateItemIds(pkg);
        Assert.True(overlaps.Count == 0, string.Join(", ", overlaps));
    }

    [Theory]
    [MemberData(nameof(Fixtures))]
    public void Growth_dimensions_are_disjoint_from_story_strong_top_three(
        (PupilResult Result, PupilProgress Progress) fixture)
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

    [Theory]
    [InlineData(200)]
    public void Random_answer_sets_have_no_theme_or_id_clashes(int seedCount)
    {
        for (var seed = 0; seed < seedCount; seed++)
        {
            var (result, progress) = PupilG78RandomFixtures.ForSeed(seed);
            var pkg = PupilG78FactStory.Build(result, progress, "nl");
            var themeOverlaps = PupilG78PageCoherence.FindStrongWeakOverlaps(pkg, progress.AnswersJson);
            var idOverlaps = PupilG78PageCoherence.FindDuplicateItemIds(pkg);
            Assert.True(themeOverlaps.Count == 0, $"seed {seed}: {string.Join(", ", themeOverlaps)}");
            Assert.True(idOverlaps.Count == 0, $"seed {seed}: ids {string.Join(", ", idOverlaps)}");
        }
    }

    public static IEnumerable<object[]> Fixtures() =>
    [
        [PupilG78RealisticFixtures.HelpsOthers()],
        [PupilG78RealisticFixtures.LeadsGroup()],
        [PupilG78RealisticFixtures.Maker()],
    ];
}

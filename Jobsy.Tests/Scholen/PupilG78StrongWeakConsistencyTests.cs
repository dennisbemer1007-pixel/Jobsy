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
        var answers = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, int>>(fixture.Progress.AnswersJson!)!;
        var overlaps = PupilG78PageCoherence.FindStrongWeakOverlaps(pkg, answers);
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
    public void Growth_block_has_two_to_four_items_and_no_strength_on_page(
        (PupilResult Result, PupilProgress Progress) fixture)
    {
        var answers = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, int>>(fixture.Progress.AnswersJson!)!;
        var pkg = PupilG78FactStory.Build(fixture.Result, fixture.Progress, "nl");
        Assert.InRange(pkg.GrowthCompetencies.Count, 2, 4);
        foreach (var g in pkg.GrowthCompetencies)
        {
            Assert.False(
                PupilG78StrengthGuard.PagePublishesCategoryAsStrength(pkg.UsedItemIds, g.Category, answers),
                g.Category);
            Assert.False(string.IsNullOrWhiteSpace(g.WhyExplanation));
        }
    }

    [Theory]
    [MemberData(nameof(Fixtures))]
    public void Tiles_contain_no_minder_lines((PupilResult Result, PupilProgress Progress) fixture)
    {
        var pkg = PupilG78FactStory.Build(fixture.Result, fixture.Progress, "nl");
        foreach (var tile in pkg.Tiles)
        {
            Assert.DoesNotContain("minder", tile.Explanation, StringComparison.OrdinalIgnoreCase);
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

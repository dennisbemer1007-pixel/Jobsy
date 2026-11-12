using Jobsy.Core.Rules;
using Jobsy.Core.Scholen;

namespace Jobsy.Tests.Scholen;

public sealed class PupilG78GrowthCompetenciesTests
{
    [Fact]
    public void Growth_list_is_at_most_five_lowest_first_and_disjoint_from_strong()
    {
        foreach (var fixture in new[] {
                     PupilG78RealisticFixtures.HelpsOthers(),
                     PupilG78RealisticFixtures.LeadsGroup(),
                     PupilG78RealisticFixtures.Maker() })
        {
            var answers = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, int>>(
                fixture.Progress.AnswersJson!)!;
            var avgs = PupilG78GrowthCompetencies.DimensionAverages(answers);
            var strong = PupilG78GrowthCompetencies.TopStrongCategories(avgs).ToHashSet(StringComparer.OrdinalIgnoreCase);
            var growth = PupilG78GrowthCompetencies.Build(answers, "nl");

            Assert.InRange(growth.Count, 1, 5);
            for (var i = 1; i < growth.Count; i++)
            {
                Assert.True(avgs[growth[i].Category] >= avgs[growth[i - 1].Category] - 0.001);
            }

            foreach (var g in growth)
            {
                if (strong.Contains(g.Category) && avgs[g.Category] >= 4.0)
                {
                    Assert.True(
                        g.WhyExplanation.Contains("midden", StringComparison.OrdinalIgnoreCase)
                        || g.WhyExplanation.StartsWith("Je gaf aan dat", StringComparison.Ordinal));
                }
            }
        }
    }

    [Fact]
    public void Growth_explanations_are_neutral_fact_only()
    {
        var answers = PupilG78RealisticFixtures.LeadsGroup().Progress.AnswersJson!;
        var dict = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, int>>(answers)!;
        foreach (var g in PupilG78GrowthCompetencies.Build(dict, "nl"))
        {
            Assert.DoesNotContain("moet", g.WhyExplanation, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("je bent slecht", g.WhyExplanation, StringComparison.OrdinalIgnoreCase);
            Assert.True(
                g.WhyExplanation.StartsWith("Je gaf aan dat", StringComparison.Ordinal)
                || g.WhyExplanation.StartsWith("Je antwoorden", StringComparison.Ordinal));
        }
    }

    [Fact]
    public void Story_low_competency_appears_in_growth_block()
    {
        var (result, progress) = PupilG78RealisticFixtures.LeadsGroup();
        var pkg = PupilG78FactStory.Build(result, progress, "nl");
        var answers = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, int>>(progress.AnswersJson!)!;
        var avgs = PupilG78GrowthCompetencies.DimensionAverages(answers);
        var lowest = avgs.OrderBy(kv => kv.Value).First().Key;
        Assert.Contains(pkg.GrowthCompetencies, g => string.Equals(g.Category, lowest, StringComparison.OrdinalIgnoreCase));
        Assert.True(
            pkg.Body.Contains(PupilG78CompetencyNarrative.MinderNaarVoren(lowest), StringComparison.OrdinalIgnoreCase)
            || pkg.GrowthCompetencies.Any(x => string.Equals(x.Category, lowest, StringComparison.OrdinalIgnoreCase)));
    }
}

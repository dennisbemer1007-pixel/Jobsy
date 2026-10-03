using Jobsy.Core.Rules;

namespace Jobsy.Tests;

public class DimensionRankingTests
{
    [Fact]
    public void Value_tie_does_not_pick_autonomy_just_because_of_the_code()
    {
        var ranked = DimensionRanking.Rank(
            [
                (SchwartzValuesCatalog.Autonomy, 60),
                (SchwartzValuesCatalog.Connection, 60),
                (SchwartzValuesCatalog.Stability, 60)
            ],
            DimensionRanking.ValueTieBreak);

        Assert.Equal(SchwartzValuesCatalog.Connection, ranked[0].Code);
        Assert.Equal(SchwartzValuesCatalog.Autonomy, ranked[^1].Code);
    }

    [Fact]
    public void Career_passport_and_outcome_use_the_same_riasec_order()
    {
        var ranked = RiasecRanking.Rank(90, 38, 31, 88, 81, 100);
        Assert.Equal(CareerTestCatalog.Conventional, ranked[0].Code);
        Assert.Equal(CareerTestCatalog.Realistic, ranked[1].Code);
        Assert.Equal(CareerTestCatalog.Social, ranked[2].Code);

        var line = AssessmentOutcomeLines.Career(90, 38, 31, 88, 81, 100);
        Assert.NotNull(line);
        Assert.DoesNotContain("Autonomy", line, StringComparison.Ordinal);
    }
}

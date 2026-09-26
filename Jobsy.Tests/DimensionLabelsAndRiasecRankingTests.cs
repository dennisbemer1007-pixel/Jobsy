using Jobsy.Core.Rules;

namespace Jobsy.Tests;

public class DimensionLabelsAndRiasecRankingTests
{
    [Theory]
    [InlineData(SchwartzValuesCatalog.Stability, "Zekerheid & traditie")]
    [InlineData(SchwartzValuesCatalog.Impact, "Impact & rechtvaardigheid")]
    [InlineData(CompetencyTestCatalog.Stressbestendigheid, "Kalm blijven")]
    [InlineData(CompetencyTestCatalog.Innovatie, "Nieuwe dingen proberen")]
    public void DimensionLabels_match_dna_and_ui_source(string code, string expected)
        => Assert.Equal(expected, DimensionLabels.For(code));

    [Fact]
    public void DomainLabel_and_EverydayLabel_share_provider()
    {
        Assert.Equal(
            DimensionLabels.For(SchwartzValuesCatalog.Stability),
            DeepAnalysisQuestionHelp.DomainLabel(SchwartzValuesCatalog.Stability));
        Assert.Equal(
            DimensionLabels.For(SchwartzValuesCatalog.Stability),
            SchwartzValuesCatalog.EverydayLabel(SchwartzValuesCatalog.Stability));
    }

    [Theory]
    [InlineData("zekerheid en voorspelbaarheid", "Zekerheid & traditie")]
    [InlineData("Stressbestendigheid", "Kalm blijven")]
    [InlineData("Innovatie", "Nieuwe dingen proberen")]
    public void MapStored_rewrites_legacy_outcome_phrases(string stored, string expected)
        => Assert.Equal(expected, DimensionLabels.MapStored(stored));

    [Fact]
    public void RiasecRanking_tie_break_uses_holland_order_when_scores_equal()
    {
        // Artistic and Social both 80; A before S in R-I-A-S-E-C → co-leaders, A listed first.
        var top = RiasecRanking.TopCodes(70, 60, 80, 80, 50, 40);
        Assert.Equal(2, top.Count);
        Assert.Equal(CareerTestCatalog.Artistic, top[0]);
        Assert.Equal(CareerTestCatalog.Social, top[1]);
        Assert.Equal("A en S even sterk", RiasecRanking.FormatTopEqualOrLabel(top));
    }

    [Fact]
    public void RiasecRanking_single_winner_uses_type_label()
    {
        var top = RiasecRanking.TopCodes(90, 40, 40, 40, 40, 40);
        Assert.Equal(CareerTestCatalog.Realistic, Assert.Single(top));
        Assert.Equal(
            CareerCompassBuilder.TypeLabel(CareerTestCatalog.Realistic),
            RiasecRanking.FormatTopEqualOrLabel(top));
    }

    [Fact]
    public void AssessmentOutcomeLines_Career_agrees_with_RiasecRanking()
    {
        var line = AssessmentOutcomeLines.Career(70, 60, 80, 80, 50, 40);
        Assert.Equal(RiasecRanking.FormatCareerOutcomeLine(70, 60, 80, 80, 50, 40), line);
        Assert.Contains("even sterk", line);
    }
}

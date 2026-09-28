using Jobsy.Core.Enums;
using Jobsy.Core.Reports;

namespace Jobsy.Tests;

public class DeepReportCapabilitiesTests
{
    [Fact]
    public void Competence_exposes_all_six_paid_cards()
    {
        var keys = DeepReportCapabilities.CardKeys(AssessmentKind.Competence);
        Assert.Equal(6, keys.Count);
        Assert.Contains(DeepReportCardKey.RadarVsNorm, keys);
        Assert.Contains(DeepReportCardKey.Comparison, keys);
        Assert.Contains(DeepReportCardKey.Facets, keys);
        Assert.Contains(DeepReportCardKey.Occupations, keys);
        Assert.Contains(DeepReportCardKey.ActionPlan, keys);
        Assert.Contains(DeepReportCardKey.StrengthsPitfalls, keys);
        Assert.Equal(9, DeepReportCapabilities.For(AssessmentKind.Competence).PdfPageCount);
    }

    [Fact]
    public void Culture_and_values_hide_locked_cards_until_part_b()
    {
        Assert.Empty(DeepReportCapabilities.CardKeys(AssessmentKind.Culture));
        Assert.Empty(DeepReportCapabilities.CardKeys(AssessmentKind.Values));
    }

    [Fact]
    public void Career_only_gates_real_occupations_card()
    {
        var keys = DeepReportCapabilities.CardKeys(AssessmentKind.Career);
        Assert.Equal(new[] { DeepReportCardKey.Occupations }, keys);
    }

    [Fact]
    public void Unknown_card_is_not_in_capabilities()
    {
        Assert.False(DeepReportCapabilities.HasCard(AssessmentKind.Competence, DeepReportCardKey.HollandCode));
        Assert.False(DeepReportCapabilities.HasCard(AssessmentKind.Career, DeepReportCardKey.RadarVsNorm));
    }
}

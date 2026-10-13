using Jobsy.Core.Enums;
using Jobsy.Core.Reports;

namespace Jobsy.Tests;

public class DeepReportCapabilitiesTests
{
    [Theory]
    [InlineData(AssessmentKind.Competence)]
    [InlineData(AssessmentKind.Career)]
    [InlineData(AssessmentKind.Culture)]
    [InlineData(AssessmentKind.Values)]
    public void Each_kind_exposes_six_paid_cards(AssessmentKind kind)
    {
        var keys = DeepReportCapabilities.CardKeys(kind);
        Assert.Equal(6, keys.Count);
        Assert.True(DeepReportCapabilities.For(kind).PdfPageCount >= 7);
    }

    [Fact]
    public void Comparison_hidden_when_lobsy_norms_not_ready()
    {
        var visible = DeepReportCapabilities.VisibleCards(AssessmentKind.Career, lobsyNormsReady: false);
        Assert.DoesNotContain(visible, c => c.Key == DeepReportCardKey.Comparison);
        Assert.Contains(DeepReportCapabilities.VisibleCards(AssessmentKind.Career, true),
            c => c.Key == DeepReportCardKey.Comparison);
    }

    [Fact]
    public void Competence_keeps_comparison_without_lobsy_norms()
    {
        var visible = DeepReportCapabilities.VisibleCards(AssessmentKind.Competence, lobsyNormsReady: false);
        Assert.Contains(visible, c => c.Key == DeepReportCardKey.Comparison);
    }

    [Fact]
    public void Career_includes_holland_code()
    {
        Assert.True(DeepReportCapabilities.HasCard(AssessmentKind.Career, DeepReportCardKey.HollandCode));
        Assert.False(DeepReportCapabilities.HasCard(AssessmentKind.Competence, DeepReportCardKey.HollandCode));
    }
}

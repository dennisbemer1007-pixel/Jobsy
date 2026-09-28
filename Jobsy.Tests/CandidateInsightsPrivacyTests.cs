using Jobsy.Core.Privacy;
using Jobsy.Core.Rules;

namespace Jobsy.Tests;

public class CandidateInsightsPrivacyTests
{
    [Fact]
    public void Cohort_below_threshold_is_insufficient()
    {
        var (status, value) = CandidateInsightsPrivacy.SuppressCount(9);
        Assert.Equal(CandidateInsightsPrivacy.StatusInsufficient, status);
        Assert.Null(value);
    }

    [Fact]
    public void Count_of_10_is_ok_and_rounded_to_10()
    {
        var (status, value) = CandidateInsightsPrivacy.SuppressCount(10);
        Assert.Equal(CandidateInsightsPrivacy.StatusOk, status);
        Assert.Equal(10, value);
    }

    [Fact]
    public void Count_rounds_to_nearest_5()
    {
        Assert.Equal(10, CandidateInsightsPrivacy.RoundCount(12));
        Assert.Equal(15, CandidateInsightsPrivacy.RoundCount(13));
        Assert.Equal(15, CandidateInsightsPrivacy.RoundCount(14));
        Assert.Equal(15, CandidateInsightsPrivacy.RoundCount(15));
        Assert.Equal(20, CandidateInsightsPrivacy.RoundCount(18));
    }

    [Fact]
    public void Percent_requires_both_numerator_and_denominator_above_threshold()
    {
        var low = CandidateInsightsPrivacy.SuppressPercent(9, 100);
        Assert.Equal(CandidateInsightsPrivacy.StatusInsufficient, low.Status);
        Assert.Null(low.Percent);

        var ok = CandidateInsightsPrivacy.SuppressPercent(25, 100);
        Assert.Equal(CandidateInsightsPrivacy.StatusOk, ok.Status);
        Assert.Equal(25, ok.Percent);
    }

    [Fact]
    public void Density_grid_is_stable_and_not_vestiging_anchored()
    {
        var a = CandidateInsightsDensityGrid.ToCell(new Core.ValueObjects.GeoPoint(52.01, 4.21));
        var b = CandidateInsightsDensityGrid.ToCell(new Core.ValueObjects.GeoPoint(52.01, 4.21));
        Assert.Equal(a, b);
        var (lat, lng) = CandidateInsightsDensityGrid.CellCenter(a.CellX, a.CellY);
        Assert.InRange(lat, 51.9, 52.2);
        Assert.InRange(lng, 4.1, 4.3);
    }
}

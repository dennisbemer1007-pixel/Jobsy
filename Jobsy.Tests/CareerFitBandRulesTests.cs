using Jobsy.Core.Rules;
using Jobsy.Web.Localization;

namespace Jobsy.Tests;

public class CareerFitBandRulesTests
{
    [Theory]
    [InlineData(75, CareerFitBandRules.CareerFitBand.Good)]
    [InlineData(100, CareerFitBandRules.CareerFitBand.Good)]
    [InlineData(74, CareerFitBandRules.CareerFitBand.Fair)]
    [InlineData(50, CareerFitBandRules.CareerFitBand.Fair)]
    [InlineData(49, CareerFitBandRules.CareerFitBand.NotYet)]
    [InlineData(0, CareerFitBandRules.CareerFitBand.NotYet)]
    public void From_percent_matches_role_thresholds(int percent, CareerFitBandRules.CareerFitBand expected)
        => Assert.Equal(expected, CareerFitBandRules.From(percent));

    [Fact]
    public void From_null_is_unknown()
        => Assert.Equal(CareerFitBandRules.CareerFitBand.Unknown, CareerFitBandRules.From(null));

    [Fact]
    public void Label_keys_use_career_fit_strings()
    {
        Assert.Equal("CareerFit.Good", CareerFitBandRules.LabelKey(CareerFitBandRules.CareerFitBand.Good));
        Assert.Equal("Past goed", UiStrings.Get("CareerFit.Good", "nl"));
    }
}

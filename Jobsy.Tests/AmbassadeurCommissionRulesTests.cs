using Jobsy.Core.Rules;

namespace Jobsy.Tests;

public class AmbassadeurCommissionRulesTests
{
    [Fact]
    public void CalculatePercentage_Starts_At_Base()
    {
        var pct = AmbassadeurCommissionRules.CalculatePercentage(0);
        Assert.Equal(5.0m, pct);
    }

    [Fact]
    public void CalculatePercentage_Adds_One_Percent_Per_Threshold()
    {
        Assert.Equal(5.0m, AmbassadeurCommissionRules.CalculatePercentage(49));
        Assert.Equal(6.0m, AmbassadeurCommissionRules.CalculatePercentage(50));
        Assert.Equal(6.0m, AmbassadeurCommissionRules.CalculatePercentage(99));
        Assert.Equal(7.0m, AmbassadeurCommissionRules.CalculatePercentage(100));
        Assert.Equal(8.0m, AmbassadeurCommissionRules.CalculatePercentage(150));
    }

    [Fact]
    public void CalculatePercentage_Caps_At_Max()
    {
        var pct = AmbassadeurCommissionRules.CalculatePercentage(
            10_000,
            maxPercentage: 15m);
        Assert.Equal(15.0m, pct);
    }

    [Fact]
    public void ResolveCurrentPercentage_Uses_Override_Then_Caps()
    {
        var overridden = AmbassadeurCommissionRules.ResolveCurrentPercentage(
            registeredCandidateCount: 0,
            basePercentage: 5m,
            threshold: 50,
            percentPerThreshold: 1m,
            maxPercentage: 15m,
            percentageOverride: 12.5m);
        Assert.Equal(12.5m, overridden);

        var capped = AmbassadeurCommissionRules.ResolveCurrentPercentage(
            0, 5m, 50, 1m, 15m, percentageOverride: 99m);
        Assert.Equal(15.0m, capped);
    }

    [Fact]
    public void PercentageToRate_Converts_Correctly()
    {
        Assert.Equal(0.05m, AmbassadeurCommissionRules.PercentageToRate(5m));
        Assert.Equal(0.15m, AmbassadeurCommissionRules.PercentageToRate(15m));
    }

    [Theory]
    [InlineData("AM-ABC123", true)]
    [InlineData("am-abc123", true)]
    [InlineData("SM-DEMO01", false)]
    [InlineData("", false)]
    [InlineData(null, false)]
    public void IsAmbassadeurTrackingCode(string? code, bool expected)
        => Assert.Equal(expected, AmbassadeurCommissionRules.IsAmbassadeurTrackingCode(code));
}

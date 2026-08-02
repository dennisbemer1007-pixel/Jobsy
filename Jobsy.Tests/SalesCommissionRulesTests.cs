using Jobsy.Core.Rules;

namespace Jobsy.Tests;

public class SalesCommissionRulesTests
{
    [Fact]
    public void FounderBonus_Is_20_Percent_Of_2500()
    {
        Assert.Equal(2500.00m, SalesCommissionRules.FirstYearOnboardingEuro);
        Assert.Equal(500.00m, SalesCommissionRules.FounderBonusExVat);
        Assert.Equal(105.00m, SalesCommissionRules.VatOn(500.00m));
        Assert.Equal(605.00m, SalesCommissionRules.InclVat(500.00m));
    }

    [Fact]
    public void TokenCommissionRate_Is_Fixed_Five_Percent_For_Referred_Suppliers()
    {
        var start = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        Assert.Equal(0.05m, SalesCommissionRules.TokenCommissionRate(start, start.AddMonths(6)));
        Assert.Equal(0.05m, SalesCommissionRules.TokenCommissionRate(start, start.AddYears(1).AddDays(1)));
        Assert.Equal(0.05m, SalesCommissionRules.TokenCommissionRate(start, start.AddYears(2).AddDays(1)));
        Assert.Null(SalesCommissionRules.TokenCommissionRate(null, DateTime.UtcNow));
    }

    [Fact]
    public void RevenueShare_Is_15_5_80()
    {
        Assert.Equal(0.15m, SalesCommissionRules.AmbassadorShareRate);
        Assert.Equal(0.05m, SalesCommissionRules.SalesManagerShareRate);
        Assert.Equal(0.80m, SalesCommissionRules.PlatformShareRate);
        Assert.Equal(15.00m, SalesCommissionRules.ShareEuro(100m, SalesCommissionRules.AmbassadorShareRate));
        Assert.Equal(5.00m, SalesCommissionRules.ShareEuro(100m, SalesCommissionRules.SalesManagerShareRate));
        Assert.Equal(80.00m, SalesCommissionRules.ShareEuro(100m, SalesCommissionRules.PlatformShareRate));
        Assert.Equal(1.50m, SalesCommissionRules.AmbassadorTokens(10));
    }

    [Theory]
    [InlineData(1, true)]
    [InlineData(10, true)]
    [InlineData(11, false)]
    [InlineData(null, false)]
    public void FounderSlots_Only_First_Ten(int? slot, bool expected)
        => Assert.Equal(expected, SalesCommissionRules.IsEligibleFounderSlot(slot));
}

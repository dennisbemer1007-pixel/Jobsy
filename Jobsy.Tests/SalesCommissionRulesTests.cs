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
    public void TokenCommissionRate_Follows_Three_Year_Staffel()
    {
        var start = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        Assert.Equal(0.25m, SalesCommissionRules.TokenCommissionRate(start, start.AddMonths(6)));
        Assert.Equal(0.10m, SalesCommissionRules.TokenCommissionRate(start, start.AddYears(1)));
        Assert.Equal(0.05m, SalesCommissionRules.TokenCommissionRate(start, start.AddYears(2)));
        Assert.Null(SalesCommissionRules.TokenCommissionRate(start, start.AddYears(3)));
        Assert.Null(SalesCommissionRules.TokenCommissionRate(null, DateTime.UtcNow));
        Assert.Equal(0.12m, SalesCommissionRules.TokenCommissionRate(start, start.AddDays(30), directRate: 0.12m));
        Assert.Null(SalesCommissionRules.TokenCommissionRate(start, start.AddYears(1), durationDays: 365));
    }

    [Fact]
    public void IndirectCommissionRate_Applies_In_Year_One_Only()
    {
        var start = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        Assert.Equal(0.05m, SalesCommissionRules.IndirectCommissionRate(start, start.AddMonths(3)));
        Assert.Null(SalesCommissionRules.IndirectCommissionRate(start, start.AddYears(1)));
        Assert.Null(SalesCommissionRules.IndirectCommissionRate(start, start.AddMonths(1), indirectRate: 0m));
    }

    [Fact]
    public void RevenueShare_Defaults_Ambassador_15_Direct_25_Indirect_5()
    {
        Assert.Equal(0.15m, SalesCommissionRules.AmbassadorShareRate);
        Assert.Equal(0.25m, SalesCommissionRules.SalesManagerShareRate);
        Assert.Equal(0.05m, SalesCommissionRules.DefaultIndirectCommissionRate);
        Assert.Equal(0.10m, SalesCommissionRules.DefaultYear2DirectCommissionRate);
        Assert.Equal(0.05m, SalesCommissionRules.DefaultYear3DirectCommissionRate);
        Assert.Equal(0.20m, SalesCommissionRules.DefaultReferredYear1DirectCommissionRate);
        Assert.Equal(0.60m, SalesCommissionRules.PlatformShareRate(0.25m));
        Assert.Equal(0.55m, SalesCommissionRules.PlatformShareRate(0.25m, 0.05m));
        Assert.Equal(15.00m, SalesCommissionRules.ShareEuro(100m, SalesCommissionRules.AmbassadorShareRate));
        Assert.Equal(25.00m, SalesCommissionRules.ShareEuro(100m, SalesCommissionRules.SalesManagerShareRate));
        Assert.Equal(5.00m, SalesCommissionRules.ShareEuro(100m, SalesCommissionRules.DefaultIndirectCommissionRate));
        Assert.Equal(1.50m, SalesCommissionRules.AmbassadorTokens(10));
        Assert.Equal(0.20m, SalesCommissionRules.Year1RateForSalesManager(true, 0.25m, 0.20m));
        Assert.Equal(0.25m, SalesCommissionRules.Year1RateForSalesManager(false, 0.25m, 0.20m));
    }

    [Theory]
    [InlineData(1, true)]
    [InlineData(10, true)]
    [InlineData(11, false)]
    [InlineData(null, false)]
    public void FounderSlots_Only_First_Ten(int? slot, bool expected)
        => Assert.Equal(expected, SalesCommissionRules.IsEligibleFounderSlot(slot));
}

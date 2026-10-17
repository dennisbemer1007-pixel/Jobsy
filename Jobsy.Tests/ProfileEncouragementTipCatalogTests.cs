using Jobsy.Web.Navigation;

namespace Jobsy.Tests;

public class ProfileEncouragementTipCatalogTests
{
    [Fact]
    public void No_test_tips_when_culture_or_values_complete()
    {
        var ctx = new ProfileEncouragementContext
        {
            UserId = "u1",
            CultureOrValuesComplete = true,
            ApplyBasicsReady = false,
            RotationBucket = 1
        };
        var pool = ProfileEncouragementTipCatalog.All
            .Where(t => t.Screens.Contains("paspoort-home"))
            .Where(t => ProfileEncouragementTipCatalog.IsEligible(t, ctx))
            .ToList();
        Assert.DoesNotContain(pool, t => t.Audience == ProfileEncouragementTipCatalog.Audience.TestsIncomplete);
        Assert.Contains(pool, t => t.Id == "wist-passport-apply");
    }

    [Fact]
    public void Preview_tip_respects_eligibility()
    {
        var ctx = new ProfileEncouragementContext
        {
            UserId = "u1",
            CultureOrValuesComplete = false,
            ApplyBasicsReady = false,
            RotationBucket = 1
        };
        var tip = ProfileEncouragementTipCatalog.PickForScreen(ctx, "vacancy-pre-apply", "wist-tests-match");
        Assert.NotNull(tip);
        Assert.Equal("wist-tests-match", tip!.Id);
    }
}

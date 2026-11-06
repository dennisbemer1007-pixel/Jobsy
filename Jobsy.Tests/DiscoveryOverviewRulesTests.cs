using Jobsy.Core.Rules;

namespace Jobsy.Tests;

public class DiscoveryOverviewRulesTests
{
    [Fact]
    public void Finished_v3_journey_does_not_mark_steps_3_to_6_as_new()
    {
        Assert.False(DiscoveryOverviewRules.StepLooksNew(true, 3, 4, stepFilled: false));
        Assert.False(DiscoveryOverviewRules.StepLooksNew(true, 3, 3, stepFilled: true));
    }

    [Fact]
    public void Finished_v2_journey_marks_unfilled_steps_3_to_6_as_new()
    {
        Assert.True(DiscoveryOverviewRules.StepLooksNew(true, 2, 5, stepFilled: false));
        Assert.False(DiscoveryOverviewRules.StepLooksNew(true, 2, 5, stepFilled: true));
        Assert.False(DiscoveryOverviewRules.StepLooksNew(true, 2, 2, stepFilled: false));
        Assert.False(DiscoveryOverviewRules.StepLooksNew(false, 2, 5, stepFilled: false));
    }
}

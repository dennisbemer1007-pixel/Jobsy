using Jobsy.Web.Models;
using Jobsy.Web.Passport;

namespace Jobsy.Tests;

public class CareerPlanFreshnessTests
{
    [Fact]
    public void StepsMatchDream_false_when_piloot_steps_for_verpleegkundige_dream()
    {
        var plan = new CareerPathPlanApiModel
        {
            DreamTitle = "Verpleegkundige",
            Steps =
            [
                new CareerPathStepApiModel { Order = 1, Title = "Vliegopleiding", Summary = "Cockpit training" },
                new CareerPathStepApiModel { Order = 2, Title = "Type rating", Summary = "Airline" }
            ]
        };

        Assert.False(CareerPlanFreshness.StepsMatchDream(plan));
    }

    [Fact]
    public void StepsMatchDream_true_when_steps_share_dream_domain()
    {
        var plan = new CareerPathPlanApiModel
        {
            DreamTitle = "Verpleegkundige",
            Steps =
            [
                new CareerPathStepApiModel { Order = 1, Title = "MBO Verpleegkunde", Summary = "Zorg" }
            ]
        };

        Assert.True(CareerPlanFreshness.StepsMatchDream(plan));
    }
}

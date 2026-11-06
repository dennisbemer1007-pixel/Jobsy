using Jobsy.Core.Contracts;
using Jobsy.Core.Rules;

namespace Jobsy.Tests;

public class CareerPlanCarryOverTests
{
    [Fact]
    public void Apply_stops_at_first_non_matching_step_prefix_only()
    {
        var archivedSteps = new[]
        {
            new CareerPlanCarryOver.OldStep("a1", 1, "Basis", []),
            new CareerPlanCarryOver.OldStep("a2", 2, "Midden", []),
            new CareerPlanCarryOver.OldStep("a3", 3, "Eind", [])
        };
        var progress = new[]
        {
            new CareerPlanCarryOver.OldProgress("a1", DateTime.UtcNow.AddDays(-2), CareerStepProgressSources.Manual),
            new CareerPlanCarryOver.OldProgress("a2", DateTime.UtcNow.AddDays(-1), CareerStepProgressSources.Manual),
            new CareerPlanCarryOver.OldProgress("a3", DateTime.UtcNow, CareerStepProgressSources.Manual)
        };
        var newSteps = new[]
        {
            new CareerPlanCarryOver.NewStep("n1", 1, "Basis", []),
            new CareerPlanCarryOver.NewStep("n2", 2, "Anders", []),
            new CareerPlanCarryOver.NewStep("n3", 3, "Eind", [])
        };

        var result = CareerPlanCarryOver.Apply(newSteps, archivedSteps, progress, certificates: null);

        Assert.Equal(1, result.CarriedCount);
        Assert.Single(result.Carried);
        Assert.Equal("n1", result.Carried[0].StepKey);
    }

    [Fact]
    public void Apply_carries_when_all_courses_match_certificates()
    {
        var newSteps = new[]
        {
            new CareerPlanCarryOver.NewStep("n1", 1, "Stap", ["Heftruck", "Veiligheid"])
        };
        var certs = new[]
        {
            new CandidateCertificateDto("Heftruck"),
            new CandidateCertificateDto("Veiligheid")
        };

        var result = CareerPlanCarryOver.Apply(newSteps, [], [], certs);

        Assert.Equal(1, result.CarriedCount);
    }
}

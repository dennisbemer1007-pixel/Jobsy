using Jobsy.Core.Rules;

namespace Jobsy.Tests;

public class CareerGoalFitTests
{
    [Fact]
    public void Dream_and_experience_surface_above_the_riasec_list_without_changing_percents()
    {
        var titles = CareerCompassBuilder.Occupations.Select(o => o.Title).ToList();
        var hits = CareerGoalFit.Pick(titles, "Teamleider logistiek", "orderpicker logistiek", "MBO 2 Logistiek");

        Assert.Contains(hits, h => h.Title.Contains("Teamleider logistiek", StringComparison.Ordinal));
        Assert.Contains(hits, h => h.Title.Contains("orderpicker", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(hits, h => h.Title.Contains("Boekhouder", StringComparison.Ordinal));

        var above = CareerGoalFit.AboveEducation(titles, "MBO 2 Logistiek");
        Assert.Contains(above, t => t.Contains("Boekhouder", StringComparison.Ordinal));
        Assert.Equal(2, CareerGoalFit.EducationRank("MBO 2 – Logistiek"));
    }
}

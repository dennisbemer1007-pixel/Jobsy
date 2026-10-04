using Jobsy.Core.Rules;

namespace Jobsy.Tests;

public class CareerGoalFitTests
{
    [Fact]
    public void Dream_and_experience_surface_above_the_riasec_list_without_changing_percents()
    {
        var titles = CareerCompassBuilder.Occupations.Select(o => o.Title).ToList();
        var hits = CareerGoalFit.Pick(titles, "Teamleider logistiek", "orderpicker logistiek", "MBO 2 Logistiek");

        Assert.Equal("Teamleider logistiek", hits[0].Title);
        Assert.Contains(hits, h => h.Title == "Planner");
        Assert.Contains(hits, h => h.Title == "Voorman");
        Assert.Contains(hits, h => h.Title == "Planningsmedewerker");
        Assert.DoesNotContain(hits, h => h.Title.Contains("winkel", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(hits, h => h.Title.Contains("orderpicker", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(hits, h => h.Title.Contains("Boekhouder", StringComparison.Ordinal));

        var above = CareerGoalFit.AboveEducation(titles, "MBO 2 Logistiek");
        Assert.Contains(above, t => t.Contains("Boekhouder", StringComparison.Ordinal));
        Assert.Contains(above, t => t.Contains("HR-medewerker", StringComparison.Ordinal));
        Assert.Equal(2, CareerGoalFit.EducationRank("MBO 2 – Logistiek"));
        Assert.False(CareerGoalFit.BelongsInSuper("Boekhouder / administrateur", 100, "MBO 2 Logistiek"));
        Assert.Equal(94, CareerGoalFit.DisplayPercent("Boekhouder / administrateur", 100, "MBO 2 Logistiek"));
        Assert.True(CareerGoalFit.BelongsInSuper("Administratief medewerker", 100, "MBO 2 Logistiek"));
    }
}

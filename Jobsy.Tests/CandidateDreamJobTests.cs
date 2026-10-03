using Jobsy.Core.Careers;
using Jobsy.Core.Rules;
using Jobsy.Web.Components.Candidate;
using Jobsy.Web.Models;

namespace Jobsy.Tests;

public class CandidateDreamJobTests
{
    [Fact]
    public void Unknown_or_adult_age_is_not_young()
    {
        var today = new DateOnly(2026, 10, 3);
        Assert.False(CandidateAge.IsYoung(null, today));
        Assert.False(CandidateAge.IsYoung(new DateOnly(1992, 4, 1), today));
        Assert.True(CandidateAge.IsYoung(new DateOnly(2012, 10, 4), today));
        Assert.False(CandidateAge.IsYoung(new DateOnly(2008, 10, 3), today));
    }

    [Fact]
    public void Adult_catalog_search_finds_logistics_leadership()
    {
        var hits = CareerDreamCatalog.Search("teamleider logistiek", 8);
        Assert.Contains(hits, h => h.Key == "teamleider-logistiek");
    }

    [Fact]
    public void Saved_dream_without_steps_stays_visible_on_carriere()
    {
        var page = CareerPlanViewBuilder.BuildPage(new CareerPathPlanApiModel
        {
            DreamTitle = "Teamleider logistiek",
            Steps = []
        }, "nl");

        Assert.False(page.HasPlan);
        Assert.Equal("Teamleider logistiek", page.DreamTitle);
        Assert.Equal("Mbo4", page.DreamLevel);
    }
}

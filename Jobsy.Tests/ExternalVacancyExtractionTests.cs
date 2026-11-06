using Jobsy.Infrastructure.Services.CandidateExternalVacancies;

namespace Jobsy.Tests;

public class ExternalVacancyExtractionTests
{
    [Fact]
    public void Parse_json_sample_without_live_ai()
    {
        const string json =
            """
            {
              "title":"Magazijnmedewerker",
              "company":"Voorbeeld BV",
              "place":"Utrecht",
              "hours":"32-40 uur",
              "pay":"€ 14 per uur",
              "start":"Per direct",
              "training":"Inwerken on the job",
              "requirementBullets":["Rijbewijs B","Nederlands spreken"]
            }
            """;

        var result = ExternalVacancyExtractionService.ParseJsonContent(json);
        Assert.NotNull(result);
        Assert.Equal("Magazijnmedewerker", result!.Title);
        Assert.Equal(2, result.RequirementBullets.Count);
    }

    [Fact]
    public void Visible_text_strips_scripts()
    {
        const string html =
            """
            <html><head><script>alert(1)</script></head><body><h1>Vacature</h1><p>Functie omschrijving</p></body></html>
            """;
        var text = ExternalVacancyUrlFetchService.ExtractVisibleText(html);
        Assert.Contains("Vacature", text, StringComparison.Ordinal);
        Assert.Contains("Functie omschrijving", text, StringComparison.Ordinal);
        Assert.DoesNotContain("alert", text, StringComparison.Ordinal);
    }

    [Fact]
    public void Robots_disallow_blocks_path()
    {
        const string robots =
            """
            User-agent: *
            Disallow: /vacatures/
            """;
        Assert.True(ExternalVacancyUrlFetchService.RobotsDisallowsPath(robots, "/vacatures/123"));
        Assert.False(ExternalVacancyUrlFetchService.RobotsDisallowsPath(robots, "/over-ons"));
    }
}

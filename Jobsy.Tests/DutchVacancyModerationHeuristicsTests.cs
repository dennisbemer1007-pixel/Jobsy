using Jobsy.Infrastructure.Services;

namespace Jobsy.Tests;

public class DutchVacancyModerationHeuristicsTests
{
    [Fact]
    public void Allows_neutral_vacancy_copy()
    {
        var result = DutchVacancyModerationHeuristics.Check(
            "Medewerker logistiek",
            "Je werkt in ons magazijn. Ervaring is een pré. Goede beheersing van Nederlands is wenselijk.");

        Assert.True(result.IsAllowed);
    }

    [Theory]
    [InlineData("Maximaal 25 jaar", "Wij zoeken een enthousiaste collega.")]
    [InlineData("Magazijnmedewerker", "Bij voorkeur jongeren voor dit team.")]
    public void Blocks_age_discrimination(string title, string description)
    {
        var result = DutchVacancyModerationHeuristics.Check(title, description);

        Assert.False(result.IsAllowed);
        Assert.Contains("leeftijd", result.Warning!, StringComparison.OrdinalIgnoreCase);
        Assert.False(string.IsNullOrWhiteSpace(result.Suggestion));
    }

    [Fact]
    public void Blocks_gender_preference()
    {
        var result = DutchVacancyModerationHeuristics.Check(
            "Verkoopmedewerker",
            "Alleen vrouwen komen in aanmerking voor deze rol.");

        Assert.False(result.IsAllowed);
        Assert.Contains("geslacht", result.Warning!, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Blocks_origin_preference()
    {
        var result = DutchVacancyModerationHeuristics.Check(
            "Bezorging",
            "Geen buitenlanders. Alleen Nederlandse afkomst.");

        Assert.False(result.IsAllowed);
        Assert.Contains("afkomst", result.Warning!, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Blocks_harsh_requirements()
    {
        var result = DutchVacancyModerationHeuristics.Check(
            "Junior developer",
            "Perfect Nederlands vereist. Minimaal 15 jaar ervaring. Geen starters.");

        Assert.False(result.IsAllowed);
        Assert.Contains("eisen", result.Warning!, StringComparison.OrdinalIgnoreCase);
    }
}

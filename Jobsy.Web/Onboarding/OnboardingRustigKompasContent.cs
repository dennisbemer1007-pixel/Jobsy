using Jobsy.Web.Models;

namespace Jobsy.Web.Onboarding;

public static class OnboardingRustigKompasContent
{
    public sealed record DirectionCard(string TitleKey, string BodyKey, string ImageUrl);

    public static readonly DirectionCard[] Directions =
    [
        new("Onboarding.Rustig.Kompas.Dir.Zorg.Title", "Onboarding.Rustig.Kompas.Dir.Zorg.Body", OnboardingRustigKompasIllustrationAssets.DirectionZorg),
        new("Onboarding.Rustig.Kompas.Dir.Groen.Title", "Onboarding.Rustig.Kompas.Dir.Groen.Body", OnboardingRustigKompasIllustrationAssets.DirectionGroen),
        new("Onboarding.Rustig.Kompas.Dir.Team.Title", "Onboarding.Rustig.Kompas.Dir.Team.Body", OnboardingRustigKompasIllustrationAssets.DirectionTeam),
    ];

    public sealed record TestSummaryCard(string TabKey, string? Title, string? Summary);

    public static IReadOnlyList<TestSummaryCard> TestSummaries(OnboardingImpression? impression)
    {
        if (impression is null)
        {
            return [];
        }

        var cards = new List<TestSummaryCard>();
        if (impression.Strengths.FirstOrDefault() is { } strength)
        {
            cards.Add(new("Onboarding.Test.Competency", strength.Label, strength.Sentence));
        }

        if (impression.Riasec.FirstOrDefault() is { } riasec)
        {
            cards.Add(new("Onboarding.Test.Career", riasec.Label, riasec.Sentence));
        }

        if (impression.CultureHighlight is { } culture)
        {
            cards.Add(new("Onboarding.Test.Culture", culture.Label, culture.Sentence));
        }

        if (impression.TopValue is { } value)
        {
            cards.Add(new("Onboarding.Test.Values", value.Label, value.Sentence));
        }

        return cards;
    }
}

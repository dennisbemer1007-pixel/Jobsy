using Jobsy.Core.Enums;

namespace Jobsy.Core.Rules;

/// <summary>
/// Visual test-block families (Quick-Scan, verdieping, diepte-analyse) and bottom-band artwork slugs.
/// </summary>
public enum TestBlockFamily
{
    HoeJeWerkt,
    Leuk,
    Sfeer,
    Waarden
}

public static class TestBlockArt
{
    public static TestBlockFamily FamilyForAssessment(AssessmentKind kind) => kind switch
    {
        AssessmentKind.Career => TestBlockFamily.Leuk,
        AssessmentKind.Culture => TestBlockFamily.Sfeer,
        AssessmentKind.Values => TestBlockFamily.Waarden,
        _ => TestBlockFamily.HoeJeWerkt
    };

    public static string SlugForAssessment(AssessmentKind kind)
        => SlugForFamily(FamilyForAssessment(kind));

    public static string SlugForFamily(TestBlockFamily family) => family switch
    {
        TestBlockFamily.Leuk => "leuk",
        TestBlockFamily.Sfeer => "sfeer",
        TestBlockFamily.Waarden => "waarden",
        _ => "hoe-je-werkt"
    };

    public static string ImagePathForSlug(string slug) => $"/images/tests/bg-{slug}.jpg";

    public static string HeroFigurePathForSlug(string slug)
        => $"/images/tests/figures/{slug}-hero.png";

    public static string HeroMobileCompositePathForSlug(string slug)
        => $"/images/tests/figures/{slug}-mobhero.png";

    /// <summary>Maps diepte-analyse domain codes to the closest Quick-Scan block art.</summary>
    public static string SlugForDeepDomain(AssessmentKind kind, string? domain)
    {
        if (string.IsNullOrWhiteSpace(domain))
        {
            return SlugForAssessment(kind);
        }

        if (kind == AssessmentKind.Culture)
        {
            return IsCulturePersonalityFacet(domain)
                ? SlugForFamily(TestBlockFamily.HoeJeWerkt)
                : SlugForFamily(TestBlockFamily.Sfeer);
        }

        return SlugForAssessment(kind);
    }

    public static int WizardStepForFamily(TestBlockFamily family) => family switch
    {
        TestBlockFamily.Leuk => 8,
        TestBlockFamily.Sfeer => 9,
        TestBlockFamily.Waarden => 10,
        _ => 7
    };

    private static bool IsCulturePersonalityFacet(string domain)
    {
        return domain.Equals("Openness", StringComparison.OrdinalIgnoreCase)
               || domain.Equals("Conscientiousness", StringComparison.OrdinalIgnoreCase)
               || domain.Equals("Extraversion", StringComparison.OrdinalIgnoreCase)
               || domain.Equals("Agreeableness", StringComparison.OrdinalIgnoreCase)
               || domain.Equals("EmotionalStability", StringComparison.OrdinalIgnoreCase);
    }
}

using Jobsy.Web.Onboarding;

namespace Jobsy.Web.OnboardingRustigMockups;

public enum OnboardingRustigMockupKind
{
    Start,
    Personal,
    TestOverview,
    TestQuestion,
    Kompas,
}

public sealed record OnboardingRustigMockupScreen(
    string Slug,
    int WizardStep,
    string PhaseLabel,
    int MinutesLeft,
    OnboardingRustigMockupKind Kind,
    string? Title,
    string? Lead,
    string HeroJpg,
    string HeroWebp,
    string HeroPosition,
    bool ShowJobsRail = false,
    bool ShowSaved = false,
    string? TestKey = null);

/// <summary>Ordered PO mockup catalog (start → step 10 → kompas).</summary>
public static class OnboardingRustigMockupRegistry
{
    public static readonly IReadOnlyList<OnboardingRustigMockupScreen> Ordered =
    [
        new("start", 0, "Over jou", 12, OnboardingRustigMockupKind.Start,
            null, null,
            OnboardingIllustrationAssets.KennismakenJpg,
            OnboardingIllustrationAssets.KennismakenWebp, "50% 45%"),
        new("step-01", 1, "Over jou", 12, OnboardingRustigMockupKind.Personal,
            "Even kennismaken", "Klopt dit? We hebben je naam overgenomen van je account.",
            OnboardingIllustrationAssets.KennismakenJpg,
            OnboardingIllustrationAssets.KennismakenWebp, "55% 48%"),
        new("step-02", 2, "Over jou", 11, OnboardingRustigMockupKind.Personal,
            "Wanneer kun je werken?", "Kies één of meer opties. Wij zetten uren en dagen voor je klaar.",
            OnboardingIllustrationAssets.BeschikbaarheidJpg,
            OnboardingIllustrationAssets.BeschikbaarheidWebp, "50% 45%", ShowJobsRail: true, ShowSaved: true),
        new("step-03", 3, "Over jou", 9, OnboardingRustigMockupKind.Personal,
            "Hoe kom je op je werk?", "Dan laten we alleen banen zien die goed te bereiken zijn.",
            OnboardingIllustrationAssets.VervoerJpg,
            OnboardingIllustrationAssets.VervoerWebp, "52% 42%"),
        new("step-04", 4, "Achtergrond", 8, OnboardingRustigMockupKind.Personal,
            "Heb je al gewerkt?", "Bijbaan, vrijwilligerswerk of stage telt ook mee.",
            OnboardingIllustrationAssets.WerkervaringJpg,
            OnboardingIllustrationAssets.WerkervaringWebp, "50% 40%"),
        new("step-05", 5, "Achtergrond", 7, OnboardingRustigMockupKind.Personal,
            "Wat is je opleiding?", "Kies je hoogste afgeronde opleiding. Geen diploma? Kies Geen.",
            OnboardingIllustrationAssets.OpleidingJpg,
            OnboardingIllustrationAssets.OpleidingWebp, "48% 42%"),
        new("step-06", 6, "Droom", 6, OnboardingRustigMockupKind.Personal,
            "Wat is je droombaan?", "Denk groot. We maken er in Carrière een route naartoe.",
            OnboardingIllustrationAssets.DreamJobJpg,
            OnboardingIllustrationAssets.DreamJobWebp, "50% 38%"),
        new("step-07", 7, "Wie ben jij", 5, OnboardingRustigMockupKind.TestQuestion,
            "Nu even over jou", "Vier korte tests. Samen ongeveer 10 minuten tot je Kompas klaar is.",
            OnboardingRustigPanoramaAssets.Desktop("step-07"),
            OnboardingRustigPanoramaAssets.Mobile("step-07"), "48% 40%", ShowSaved: true, TestKey: "werkt"),
        new("step-08", 8, "Wie ben jij", 4, OnboardingRustigMockupKind.TestQuestion,
            null, null,
            OnboardingRustigPanoramaAssets.Desktop("step-08"),
            OnboardingRustigPanoramaAssets.Mobile("step-08"), "50% 38%", ShowSaved: true, TestKey: "leuk"),
        new("step-09", 8, "Wie ben jij", 4, OnboardingRustigMockupKind.TestQuestion,
            null, null,
            OnboardingRustigPanoramaAssets.Desktop("step-09"),
            OnboardingRustigPanoramaAssets.Mobile("step-09"), "50% 38%", ShowSaved: true, TestKey: "leuk"),
        new("step-10", 9, "Wie ben jij", 3, OnboardingRustigMockupKind.TestQuestion,
            null, null,
            OnboardingRustigPanoramaAssets.Desktop("step-10"),
            OnboardingRustigPanoramaAssets.Mobile("step-10"), "50% 38%", ShowSaved: true, TestKey: "sfeer"),
        new("step-11", 10, "Wie ben jij", 2, OnboardingRustigMockupKind.TestQuestion,
            null, null,
            OnboardingRustigPanoramaAssets.Desktop("step-11"),
            OnboardingRustigPanoramaAssets.Mobile("step-11"), "50% 38%", ShowSaved: true, TestKey: "waarden"),
        new("kompas", 10, "Jouw Kompas", 1, OnboardingRustigMockupKind.Kompas,
            null, null,
            "/images/onboarding/finale-hero.jpg",
            "/images/onboarding/finale-hero.jpg", "50% 42%"),
    ];

    public static OnboardingRustigMockupScreen? Find(string slug) =>
        Ordered.FirstOrDefault(s => string.Equals(s.Slug, slug, StringComparison.OrdinalIgnoreCase));
}

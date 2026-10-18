namespace Jobsy.Web.Onboarding;

/// <summary>Shared onboarding illustration paths (WebP + JPG fallback).</summary>
public static class OnboardingIllustrationAssets
{
    public const string Version = "20261018-r10";
    public const string MobileVersion = "20261018-r15";

    public const string KennismakenWebp = $"/images/onboarding/kennismaken.webp?v={Version}";
    public const string KennismakenJpg = $"/images/onboarding/kennismaken.jpg?v={Version}";

    public const string BeschikbaarheidWebp = $"/images/onboarding/beschikbaarheid.webp?v={Version}";
    public const string BeschikbaarheidJpg = $"/images/onboarding/beschikbaarheid.jpg?v={Version}";

    public const string VervoerWebp = $"/images/onboarding/vervoer.webp?v={Version}";
    public const string VervoerJpg = $"/images/onboarding/vervoer.jpg?v={Version}";

    public const string WerkervaringWebp = $"/images/onboarding/werkervaring.webp?v={Version}";
    public const string WerkervaringJpg = $"/images/onboarding/werkervaring.jpg?v={Version}";

    public const string OpleidingWebp = $"/images/onboarding/opleiding.webp?v={Version}";
    public const string OpleidingJpg = $"/images/onboarding/opleiding.jpg?v={Version}";

    public const string DreamJobWebp = $"/images/onboarding/droombaan.webp?v={Version}";
    public const string DreamJobJpg = $"/images/onboarding/droombaan.jpg?v={Version}";

    public const string KennismakenMobileBgWebp = $"/images/onboarding/kennismaken-mobile-bg.webp?v={MobileVersion}";
    public const string KennismakenMobileBgPng = $"/images/onboarding/kennismaken-mobile-bg.png?v={MobileVersion}";
    public const string KennismakenMobileWebp = $"/images/onboarding/kennismaken-mobile.webp?v={MobileVersion}";
    public const string KennismakenMobilePng = $"/images/onboarding/kennismaken-mobile.png?v={MobileVersion}";

    public const string BeschikbaarheidMobileBgWebp = $"/images/onboarding/beschikbaarheid-mobile-bg.webp?v={MobileVersion}";
    public const string BeschikbaarheidMobileBgPng = $"/images/onboarding/beschikbaarheid-mobile-bg.png?v={MobileVersion}";
    public const string BeschikbaarheidMobileWebp = $"/images/onboarding/beschikbaarheid-mobile.webp?v={MobileVersion}";
    public const string BeschikbaarheidMobilePng = $"/images/onboarding/beschikbaarheid-mobile.png?v={MobileVersion}";

    public const string VervoerMobileBgWebp = $"/images/onboarding/vervoer-mobile-bg.webp?v={MobileVersion}";
    public const string VervoerMobileBgPng = $"/images/onboarding/vervoer-mobile-bg.png?v={MobileVersion}";
    public const string VervoerMobileWebp = $"/images/onboarding/vervoer-mobile.webp?v={MobileVersion}";
    public const string VervoerMobilePng = $"/images/onboarding/vervoer-mobile.png?v={MobileVersion}";

    public const string WerkervaringMobileBgWebp = $"/images/onboarding/werkervaring-mobile-bg.webp?v={MobileVersion}";
    public const string WerkervaringMobileBgPng = $"/images/onboarding/werkervaring-mobile-bg.png?v={MobileVersion}";
    public const string WerkervaringMobileWebp = $"/images/onboarding/werkervaring-mobile.webp?v={MobileVersion}";
    public const string WerkervaringMobilePng = $"/images/onboarding/werkervaring-mobile.png?v={MobileVersion}";

    public const string OpleidingMobileBgWebp = $"/images/onboarding/opleiding-mobile-bg.webp?v={MobileVersion}";
    public const string OpleidingMobileBgPng = $"/images/onboarding/opleiding-mobile-bg.png?v={MobileVersion}";
    public const string OpleidingMobileWebp = $"/images/onboarding/opleiding-mobile.webp?v={MobileVersion}";
    public const string OpleidingMobilePng = $"/images/onboarding/opleiding-mobile.png?v={MobileVersion}";

    public const string DreamJobMobileBgWebp = $"/images/onboarding/droombaan-mobile-bg.webp?v={MobileVersion}";
    public const string DreamJobMobileBgPng = $"/images/onboarding/droombaan-mobile-bg.png?v={MobileVersion}";
    public const string DreamJobMobileWebp = $"/images/onboarding/droombaan-mobile.webp?v={MobileVersion}";
    public const string DreamJobMobilePng = $"/images/onboarding/droombaan-mobile.png?v={MobileVersion}";

    public static string MobileHeroWebp(string slug) =>
        $"/images/onboarding/{slug}-mobile-hero.webp?v={MobileVersion}";

    public static string MobileHeroJpg(string slug) =>
        $"/images/onboarding/{slug}-mobile-hero.jpg?v={MobileVersion}";

    public static string MobileHeroSlug(string testId) => testId switch
    {
        "onboarding-step-1-illustrated" => "kennismaken",
        "onboarding-step-2-illustrated" => "beschikbaarheid",
        "onboarding-step-3-illustrated" => "vervoer",
        "onboarding-step-4-illustrated" => "werkervaring",
        "onboarding-step-5-illustrated" => "opleiding",
        "onboarding-step-6-illustrated" => "droombaan",
        _ => "kennismaken",
    };
}

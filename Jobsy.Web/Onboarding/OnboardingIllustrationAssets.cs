namespace Jobsy.Web.Onboarding;

/// <summary>Shared onboarding illustration paths (WebP + JPG fallback).</summary>
public static class OnboardingIllustrationAssets
{
    public const string Version = "20261018-r2";

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
}

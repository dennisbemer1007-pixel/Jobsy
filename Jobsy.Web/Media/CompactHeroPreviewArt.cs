namespace Jobsy.Web.Media;

/// <summary>Preview-only wide art URLs for ?heroPreview=c (not production rollout).</summary>
public static class CompactHeroPreviewArt
{
    public static string CandHeroArtForVariantC(string artUrl)
    {
        if (string.IsNullOrWhiteSpace(artUrl))
        {
            return artUrl;
        }

        if (artUrl.Contains("opleiding.jpg", StringComparison.OrdinalIgnoreCase)
            || artUrl.Contains("leren-groeien-hero", StringComparison.OrdinalIgnoreCase))
        {
            return CandidateIllustratedImages.LearnGrowHeroBandC;
        }

        if (artUrl.Contains("kompas-hero", StringComparison.OrdinalIgnoreCase))
        {
            return CandidateIllustratedImages.KompasHeroBandC;
        }

        if (artUrl.Contains("kennismaken", StringComparison.OrdinalIgnoreCase)
            || artUrl.Contains("wie-ben-jij-profession-wide", StringComparison.OrdinalIgnoreCase)
            || artUrl.Contains("finale-hero", StringComparison.OrdinalIgnoreCase))
        {
            return OnboardingKennismakenDesktop;
        }

        return artUrl;
    }

    public const string OnboardingKennismakenDesktop =
        "/images/candidate-illustrated/wie-ben-jij-profession-wide.jpg";

    public const string OnboardingKennismakenMobile = "/images/onboarding/kennismaken-mobile-hero.jpg";
}

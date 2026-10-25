namespace Jobsy.Web.Media;

/// <summary>Band-C art URLs for the default compact candidate hero.</summary>
public static class CompactHeroPreviewArt
{
    public static string CandHeroArtForVariantC(string artUrl)
    {
        if (string.IsNullOrWhiteSpace(artUrl))
        {
            return artUrl;
        }

        if (artUrl.Contains("/images/kreeft/strips/", StringComparison.Ordinal))
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

        if (artUrl.Contains("werkervaring-profession-wide", StringComparison.OrdinalIgnoreCase)
            || artUrl.Contains("sollicitaties", StringComparison.OrdinalIgnoreCase))
        {
            return CandidateIllustratedImages.SollicitatiesHeroBandC;
        }

        if (artUrl.Contains("vervoer-profession-wide", StringComparison.OrdinalIgnoreCase))
        {
            return CandidateIllustratedImages.BewaardHeroBandC;
        }

        if (artUrl.Contains("beschikbaarheid-profession-wide", StringComparison.OrdinalIgnoreCase))
        {
            return CandidateIllustratedImages.InstellingenHeroBandC;
        }

        if (artUrl.Contains("bedrijvenkaart-hero", StringComparison.OrdinalIgnoreCase))
        {
            return CandidateIllustratedImages.BedrijvenkaartHeroBandC;
        }

        return artUrl;
    }

    /// <summary>Mobile strip or watercolor figures for variant C compact hero.</summary>
    public static string? CandHeroMobileArtForVariantC(string? artUrl, string? mobileFiguresUrl)
    {
        if (!string.IsNullOrWhiteSpace(artUrl)
            && artUrl.Contains("/images/kreeft/strips/", StringComparison.Ordinal)
            && artUrl.Contains("-desk.webp", StringComparison.Ordinal))
        {
            return artUrl.Replace("-desk.webp", "-mob.webp", StringComparison.Ordinal);
        }

        return string.IsNullOrWhiteSpace(mobileFiguresUrl) ? null : mobileFiguresUrl;
    }

    public const string OnboardingKennismakenDesktop =
        "/images/candidate-illustrated/wie-ben-jij-profession-wide.jpg";

    public const string OnboardingKennismakenMobile = "/images/onboarding/kennismaken-mobile-hero.jpg";
}

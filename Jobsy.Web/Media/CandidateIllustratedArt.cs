namespace Jobsy.Web.Media;

/// <summary>Maps pages and dream jobs to warm illustrated spots (diversity + branch fit).</summary>
public static class CandidateIllustratedArt
{
    /// <summary>Wide landscape art safe for full-bleed heroes (min ~1280px wide); never tiny job spots.</summary>
    public sealed record HeroArt(string Url, string BackgroundPosition = "42% 38%");

    public static HeroArt HeroLandscapeForDream(string? dreamTitle)
    {
        var t = (dreamTitle ?? "").Trim();
        if (t.Length == 0)
        {
            return new HeroArt(CandidateIllustratedImages.DroombaanBand, "38% 42%");
        }

        if (ContainsAny(t, "dieren", "paraveter", "dierenarts", "veter"))
        {
            return new HeroArt(CandidateIllustratedImages.FinaleHero, "58% center");
        }

        if (ContainsAny(t, "zorg", "verpleeg", "helpende", "begeleid", "dagbested"))
        {
            return new HeroArt(CandidateIllustratedImages.DroombaanBand, "32% 38%");
        }

        if (ContainsAny(t, "logistiek", "magazijn", "orderpicker", "chauffeur", "bezorg", "teamleider logist"))
        {
            return new HeroArt(CandidateIllustratedImages.Spot("kaart.jpg"), "60% 45%");
        }

        if (ContainsAny(t, "retail", "winkel", "verkoop", "kass"))
        {
            return new HeroArt(CandidateIllustratedImages.DroombaanBand, "48% 40%");
        }

        if (ContainsAny(t, "kok", "horeca", "bedien", "chef", "restaurant"))
        {
            return new HeroArt(CandidateIllustratedImages.SollicitatiesBand, "50% 42%");
        }

        if (ContainsAny(t, "schoonma", "clean"))
        {
            return new HeroArt(CandidateIllustratedImages.InstellingenBand, "45% 40%");
        }

        if (ContainsAny(t, "monteur", "techn", "installateur", "elektr", "lasser", "bouw"))
        {
            return new HeroArt(CandidateIllustratedImages.SollicitatiesBand, "58% 38%");
        }

        if (ContainsAny(t, "docent", "leraar", "onderwijs", "pabo"))
        {
            return new HeroArt(CandidateIllustratedImages.CarriereBand, "40% 36%");
        }

        if (ContainsAny(t, "groen", "hovenier", "tuin"))
        {
            return new HeroArt(CandidateIllustratedImages.Spot("vervoer-blok.jpg"), "52% 44%");
        }

        if (ContainsAny(t, "ict", "developer", "programmeur", "software"))
        {
            return new HeroArt(CandidateIllustratedImages.Spot("dna.jpg"), "50% 42%");
        }

        return new HeroArt(CandidateIllustratedImages.DroombaanBand, "38% 42%");
    }

    /// <summary>Landscape hero when a vacancy has no employer photo (category + title aware).</summary>
    public static HeroArt VacancyHeroFallback(string? categoryName, string? title)
    {
        var seed = string.Join(' ', new[] { categoryName, title }.Where(s => !string.IsNullOrWhiteSpace(s)));
        if (ContainsAny(seed, "dieren", "pension", "veter", "paraveter", "dierenverzorg"))
        {
            return new HeroArt(CandidateIllustratedImages.Spot("job-dieren.jpg"), "center bottom");
        }

        return HeroLandscapeForDream(string.IsNullOrWhiteSpace(seed) ? null : seed);
    }

    /// <summary>Small spots for cards/thumbs only (not heroes).</summary>
    public static string SpotForDream(string? dreamTitle)
    {
        var t = (dreamTitle ?? "").Trim();
        if (ContainsAny(t, "dieren", "paraveter", "dierenarts", "veter"))
        {
            return CandidateIllustratedImages.Spot("job-dieren.jpg");
        }

        if (ContainsAny(t, "zorg", "verpleeg", "helpende"))
        {
            return CandidateIllustratedImages.Spot("job-zorg.jpg");
        }

        if (ContainsAny(t, "logistiek", "magazijn", "orderpicker"))
        {
            return CandidateIllustratedImages.Spot("job-logistiek.jpg");
        }

        if (ContainsAny(t, "retail", "winkel", "verkoop"))
        {
            return CandidateIllustratedImages.Spot("job-retail.jpg");
        }

        return CandidateIllustratedImages.SavedNoPhotoArt;
    }

    public static string CareerEmptySpot => CandidateIllustratedImages.DroombaanBand;

    private static bool ContainsAny(string text, params string[] parts)
    {
        foreach (var part in parts)
        {
            if (text.Contains(part, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }
}

namespace Jobsy.Web.Media;

/// <summary>Maps pages and dream jobs to warm illustrated spots (diversity + branch fit).</summary>
public static class CandidateIllustratedArt
{
    public static string HeroForDream(string? dreamTitle)
    {
        var t = (dreamTitle ?? "").Trim();
        if (t.Length == 0)
        {
            return CandidateIllustratedImages.DroombaanBand;
        }

        if (ContainsAny(t, "dieren", "paraveter", "dierenarts", "veter"))
        {
            return CandidateIllustratedImages.Spot("job-dieren.jpg");
        }

        if (ContainsAny(t, "zorg", "verpleeg", "helpende", "begeleid", "dagbested"))
        {
            return CandidateIllustratedImages.Spot("job-zorg.jpg");
        }

        if (ContainsAny(t, "logistiek", "magazijn", "orderpicker", "chauffeur", "bezorg", "teamleider logist"))
        {
            return CandidateIllustratedImages.Spot("job-logistiek.jpg");
        }

        if (ContainsAny(t, "retail", "winkel", "verkoop", "kass"))
        {
            return CandidateIllustratedImages.Spot("job-retail.jpg");
        }

        if (ContainsAny(t, "kok", "horeca", "bedien", "chef", "restaurant"))
        {
            return CandidateIllustratedImages.CarriereBand;
        }

        if (ContainsAny(t, "schoonma", "clean"))
        {
            return CandidateIllustratedImages.InstellingenBand;
        }

        if (ContainsAny(t, "monteur", "techn", "installateur", "elektr", "lasser", "bouw"))
        {
            return CandidateIllustratedImages.SollicitatiesBand;
        }

        if (ContainsAny(t, "docent", "leraar", "onderwijs", "pabo"))
        {
            return CandidateIllustratedImages.CarriereBand;
        }

        if (ContainsAny(t, "groen", "hovenier", "tuin"))
        {
            return CandidateIllustratedImages.Spot("banen.jpg");
        }

        if (ContainsAny(t, "ict", "developer", "programmeur", "software"))
        {
            return CandidateIllustratedImages.Spot("dna.jpg");
        }

        if (ContainsAny(t, "kantoor", "administr", "hr", "adviseur"))
        {
            return CandidateIllustratedImages.Spot("werkgever.jpg");
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

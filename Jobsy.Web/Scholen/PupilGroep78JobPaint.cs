using Jobsy.Core.Scholen;

namespace Jobsy.Web.Scholen;

internal static class PupilGroep78JobPaint
{
    internal static string PaintImagePath(string jobKey)
    {
        var key = jobKey.ToLowerInvariant();
        var (stem, hero) = key switch
        {
            "dierenverzorger" => ("zorg-dieren", false),
            "dierenarts" => ("zorg-dieren", true),
            "bioloog" or "wetenschapper" => ("onderzoek", false),
            "boswachter" => ("onderzoek", true),
            "grafisch-ontwerper" or "muzikant" => ("creatief", false),
            "fotograaf" or "contentmaker" or "acteur" => ("creatief", true),
            "hovenier" => ("buiten-groen", false),
            "brandweer" or "verpleegkundige" => ("helpen", false),
            "politie" => ("helpen", true),
            "kok" or "bakker" => ("creatief", true),
            "programmeur" or "ict-medewerker" or "game-developer" => ("onderzoek", true),
            _ => PaintStemFromBeroepssoort(jobKey)
        };

        var file = hero ? $"{stem}-hero.jpg" : $"{stem}.jpg";
        return $"/leerling/werelden/img/jobs/{file}";
    }

    private static (string Stem, bool Hero) PaintStemFromBeroepssoort(string jobKey)
    {
        var beroep = PupilG78CareerScoring.PrimaryBeroepssoortKey(jobKey);
        return beroep switch
        {
            "dieren-leven" or "zorg-mensen" => ("zorg-dieren", false),
            "exact-wetenschap" => ("onderzoek", false),
            "creatief-design" => ("creatief", false),
            "media-digital" => ("creatief", true),
            "natuur-buiten" => ("buiten-groen", false),
            "veiligheid-hulp" => ("helpen", false),
            "sport-bewegen" => ("helpen", true),
            _ => ("onderzoek", false)
        };
    }

    internal static string PaintThemeClass(string? journeyThemeKey)
    {
        var theme = PupilJourneyTheme.NormalizeOrDefault(journeyThemeKey);
        return theme switch
        {
            PupilJourneyTheme.Eilandtuin => "jpick-paint-w-tuin",
            PupilJourneyTheme.Sterrenkust => "jpick-paint-w-ster",
            _ => "jpick-paint-w-zee"
        };
    }
}

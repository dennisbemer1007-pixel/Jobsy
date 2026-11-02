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
            "grafisch-ontwerper" or "fotograaf" or "contentmaker" => ("creatief", false),
            "hovenier" => ("buiten-groen", false),
            "brandweer" or "verpleegkundige" => ("helpen", false),
            "politie" => ("helpen", true),
            "kok" or "bakker" => ("creatief", true),
            "programmeur" or "ict-medewerker" => ("onderzoek", true),
            _ => ("onderzoek", false)
        };

        var file = hero ? $"{stem}-hero.jpg" : $"{stem}.jpg";
        return $"/leerling/werelden/img/jobs/{file}";
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

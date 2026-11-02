using Jobsy.Core.Scholen;

namespace Jobsy.Web.Scholen;

internal static class PupilGroep78JobPaint
{
    internal static string PaintImagePath(string jobKey)
    {
        var beroep = PupilG78CareerScoring.PrimaryBeroepssoortKey(jobKey);
        var stem = beroep switch
        {
            "dieren-leven" => "zorg-dieren",
            "zorg-mensen" => "zorg-dieren",
            "exact-wetenschap" => "onderzoek",
            "creatief-design" => "creatief",
            "media-digital" => "creatief",
            "taal-verhalen" => "creatief",
            "natuur-buiten" => "buiten-groen",
            "sport-bewegen" => "buiten-groen",
            "veiligheid-hulp" => "helpen",
            "techniek-maken" => "buiten-groen",
            _ => JobKeyStem(jobKey)
        };

        return $"/leerling/werelden/img/jobs/{stem}.jpg";
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

    private static string JobKeyStem(string jobKey) => jobKey.ToLowerInvariant() switch
    {
        "dierenverzorger" or "dierenarts" => "zorg-dieren",
        "bioloog" or "wetenschapper" or "boswachter" => "onderzoek",
        "grafisch-ontwerper" or "fotograaf" or "contentmaker" => "creatief",
        "hovenier" => "buiten-groen",
        "brandweer" or "verpleegkundige" or "politie" => "helpen",
        _ => "onderzoek"
    };
}

using Jobsy.Core.Enums;
using Jobsy.Core.Scholen;
using Jobsy.Core.Scholen.PupilPassport;

namespace Jobsy.Web.Scholen;

internal static class PupilGroep78JobPaint
{
    /// <summary>Beroepsgroep label for G78 job cards (can differ from scoring when a job fits multiple clusters).</summary>
    internal static string? DisplayBeroepssoortKey(string jobKey)
    {
        var key = jobKey.ToLowerInvariant();
        return key switch
        {
            "fysiotherapeut" => "zorg-mensen",
            "personal-trainer" or "profsporter" => "sport-bewegen",
            "bioloog" => "exact-wetenschap",
            _ => PupilG78CareerScoring.PrimaryBeroepssoortKey(jobKey)
        };
    }

    internal static (string Domain, string Blurb) PickCardCopy(string jobKey, string language)
    {
        var beroep = DisplayBeroepssoortKey(jobKey);
        var domain = beroep is null
            ? ""
            : PupilG78BeroepssoortCatalog.Title(beroep, english: false);
        var blurb = PupilPassportLocalizedCopy.JobBlurb(jobKey, PupilQuestionSet.Groep78, language);
        return (domain, blurb);
    }
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
            "fysiotherapeut" => ("zorg-dieren", true),
            "personal-trainer" => ("sport", false),
            "profsporter" => ("sport", true),
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
            "sport-bewegen" => ("sport", false),
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

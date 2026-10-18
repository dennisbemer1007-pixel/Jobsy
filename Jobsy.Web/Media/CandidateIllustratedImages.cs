namespace Jobsy.Web.Media;

/// <summary>Versioned URLs for candidate-facing illustrated pages (login, start, kompas, banenkaart).</summary>
public static class CandidateIllustratedImages
{
    public const string Version = "20261018-r10";
    private const string Base = $"/images/candidate-illustrated";

    public static string Url(string file) => $"{Base}/{file}?v={Version}";
    public static string Spot(string file) => $"{Base}/spots/{file}?v={Version}";

    public const string FinaleHero = $"{Base}/finale-hero.jpg?v={Version}";
    public const string LoginLeft = $"{Base}/spots/login-links.jpg?v={Version}";
    public const string LoginRight = $"{Base}/spots/login-rechts.jpg?v={Version}";
    public const string VervoerBlok = $"{Base}/spots/vervoer-blok.jpg?v={Version}";
    public const string KompasMedallions = $"{Base}/spots/dna.jpg?v={Version}";
    public const string TipVragen = $"{Base}/spots/vragen.jpg?v={Version}";
    public const string BanenkaartCyclists = $"{Base}/spots/banen.jpg?v={Version}";

    public const string StepVragen = $"{Base}/spots/vragen.jpg?v={Version}";
    public const string StepDna = $"{Base}/spots/dna.jpg?v={Version}";
    public const string StepBanen = $"{Base}/spots/banen.jpg?v={Version}";
    public const string EmpWerkgever = $"{Base}/spots/werkgever.jpg?v={Version}";

    /// <summary>Reference heroes — crème left, diverse trio on the right (dedicated crop).</summary>
    public const string RefVerifRequest = $"{Base}/spots/ref-verif-hero.jpg?v={Version}";

    public const string RefVerifPublic = $"{Base}/spots/ref-verif-hero.jpg?v={Version}";

    /// <summary>Desktop hero crop: trio fully in art zone; crème-only in gradient band.</summary>
    public const string RefVerifHeroDesktopPosition = "100% 42%";

    /// <summary>Mobile illustration band crop (full heads).</summary>
    public const string RefVerifHeroMobilePosition = "82% 42%";
}

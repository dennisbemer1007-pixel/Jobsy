namespace Jobsy.Web.Media;

/// <summary>Versioned URLs for candidate-facing illustrated pages (login, start, kompas, banenkaart).</summary>
public static class CandidateIllustratedImages
{
    public const string Version = "20261018-16";
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

    /// <summary>Reference heroes — PR #737 ref-full (two women at table).</summary>
    public const string RefVerifRequest = $"/images/candidate/ref-full.jpg?v={Version}";

    public const string RefVerifPublic = $"/images/candidate/ref-full.jpg?v={Version}";

    public const string RefVerifMobile = $"/images/candidate/ref-full.jpg?v={Version}";

    public const string RefVerifHeroArtPosition = "right bottom";

    public const string RefVerifHeroMobilePosition = "right bottom";
}

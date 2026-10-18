namespace Jobsy.Web.Media;

/// <summary>Versioned URLs for candidate-facing illustrated pages (login, start, kompas, banenkaart).</summary>
public static class CandidateIllustratedImages
{
    public const string Version = "20261018-08";
    private const string Base = $"/images/candidate-illustrated";

    public static string Url(string file) => $"{Base}/{file}?v={Version}";
    public static string Spot(string file) => $"{Base}/spots/{file}?v={Version}";

    public const string FinaleHero = $"{Base}/finale-hero.jpg?v={Version}";
    public const string VacancyDierenStockPhoto = "/images/vacancies/dierenpension-kennel.jpg";
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

    public const string SollicitatiesBand = $"{Base}/werkervaring.jpg?v={Version}";
    public const string BewaardBand = $"{Base}/spots/banen.jpg?v={Version}";
    public const string CarriereBand = $"{Base}/opleiding.jpg?v={Version}";
    public const string LearnGrowHero = $"{Base}/leren-groeien-hero.jpg?v={Version}";
    public const string LearnGrowHeroWide = $"{Base}/leren-groeien-hero-wide.jpg?v={Version}";
    public const string InstellingenBand = $"{Base}/beschikbaarheid.jpg?v={Version}";
    public const string DroombaanBand = $"{Base}/droombaan.jpg?v={Version}";
    public const string DroombaanProfessionWide = $"{Base}/droombaan-profession-wide.jpg?v={Version}";
    public const string KennismakenHero = $"{Base}/kennismaken.jpg?v={Version}";
    public const string AccountMobileArt = $"{Base}/kennismaken.jpg?v={Version}";
    public const string SavedNoPhotoArt = $"{Base}/spots/job-flex.jpg?v={Version}";

    public const string SpotKaart = $"{Base}/spots/kaart.jpg?v={Version}";
    public const string SpotVragen = $"{Base}/spots/vragen.jpg?v={Version}";
    public const string SpotWerkgever = $"{Base}/spots/werkgever.jpg?v={Version}";
}

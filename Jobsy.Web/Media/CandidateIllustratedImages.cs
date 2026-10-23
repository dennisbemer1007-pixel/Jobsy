namespace Jobsy.Web.Media;

/// <summary>Versioned URLs for candidate-facing illustrated pages (login, start, kompas, banenkaart).</summary>
public static class CandidateIllustratedImages
{
    public const string Version = "20261023-acc9";
    private const string Base = $"/images/candidate-illustrated";

    public static string Url(string file) => $"{Base}/{file}?v={Version}";
    public static string Spot(string file) => $"{Base}/spots/{file}?v={Version}";

    public const string FinaleHero = $"{Base}/finale-hero.jpg?v={Version}";
    public const string LoginLeft = $"{Base}/spots/login-links.jpg?v={Version}";
    public const string LoginRight = $"{Base}/spots/login-rechts.jpg?v={Version}";
    public const string VervoerBlok = $"{Base}/spots/vervoer-blok.jpg?v={Version}";
    public const string KompasMedallions = $"{Base}/spots/dna.jpg?v={Version}";
    public const string BanenkaartCyclists = $"{Base}/spots/banen.jpg?v={Version}";

    /// <summary>Banenkaart header strip (Dennis variant C, PR #878 @ 211fff91).</summary>
    public const string BanenkaartHeroStripDesktopWebp = $"{Base}/banenkaart-hero-strip-desktop.webp?v={Version}";

    public const string BanenkaartHeroStripDesktopWebp2x = $"{Base}/banenkaart-hero-strip-desktop-2x.webp?v={Version}";

    public const string BanenkaartHeroStripMobileWebp = $"{Base}/banenkaart-hero-strip-mobile.webp?v={Version}";

    public const string BanenkaartHeroStripMobileWebp2x = $"{Base}/banenkaart-hero-strip-mobile-2x.webp?v={Version}";

    public const string BanenkaartHeroStripDesktopJpg = $"{Base}/banenkaart-hero-strip-desktop.jpg?v={Version}";

    public const string BanenkaartHeroStripDesktopJpg2x = $"{Base}/banenkaart-hero-strip-desktop-2x.jpg?v={Version}";

    public const string BanenkaartHeroStripMobileJpg = $"{Base}/banenkaart-hero-strip-mobile.jpg?v={Version}";

    public const string BanenkaartHeroStripMobileJpg2x = $"{Base}/banenkaart-hero-strip-mobile-2x.jpg?v={Version}";
    public const string PopupTravelAccent = $"{Base}/spots/vervoer-blok.jpg?v={Version}";

    /// <summary>Kompas tip card — zorg + logistiek, incl. hijab.</summary>
    public const string TipVragen = $"{Base}/spots/job-inclusief-zorg-logistiek.jpg?v={Version}";

    /// <summary>Start step 1 — diverse jongvolwassenen (vragen/kompas).</summary>
    public const string StepVragen = $"{Base}/spots/vragen.jpg?v={Version}";
    public const string StepDna = $"{Base}/spots/dna.jpg?v={Version}";
    public const string StepBanen = $"{Base}/spots/banen.jpg?v={Version}";
    public const string EmpWerkgever = $"{Base}/spots/werkgever.jpg?v={Version}";

    public const string SollicitatiesBand = $"{Base}/werkervaring-profession-wide.jpg?v={Version}";
    public const string SollicitatiesHeroBandC = SollicitatiesBand;
    public const string BewaardBand = $"{Base}/vervoer-profession-wide.jpg?v={Version}";
    public const string BewaardHeroBandC = BewaardBand;
    public const string CarriereBand = $"{Base}/opleiding.jpg?v={Version}";
    public const string LearnGrowHero = $"{Base}/leren-groeien-hero.jpg?v={Version}";
    public const string LearnGrowHeroWide = $"{Base}/leren-groeien-hero-wide.jpg?v={Version}";
    public const string LearnGrowHeroCompactC = $"{Base}/leren-groeien-hero-compact-c.jpg?v={Version}";
    public const string LearnGrowHeroBandC = $"{Base}/leren-groeien-hero-band-c.jpg?v={Version}";

    /// <summary>UWV ondernemer intro: home / small-business scene (no portrait crop).</summary>
    public const string OndernemerIntroHero = AccountMobileArt;

    /// <summary>UWV ondernemer result — complete werkplaats vignette on cream (no edge crop).</summary>
    public const string OndernemerResultScene = $"{Base}/ondernemer-result-vignette.png?v={Version}";
    public const string LearnGrowHeroFiguresMobile = $"{Base}/leren-groeien-hero-figures-mobile.png?v={Version}";
    public const string LearnGrowHeroMobileAtmosphere = $"{Base}/leren-groeien-hero-mobile-atmosphere.png?v={Version}";
    public const string InstellingenBand = $"{Base}/beschikbaarheid-profession-wide.jpg?v={Version}";
    public const string InstellingenHeroBandC = InstellingenBand;
    public const string KompasHeroWide = $"{Base}/kompas-hero-wide.jpg?v={Version}";
    public const string KompasHeroBandC = $"{Base}/kompas-hero-band-c.jpg?v={Version}";
    public const string BedrijvenkaartHeroMobile = $"{Base}/bedrijvenkaart-hero-mobile.jpg?v={Version}";
    public const string BedrijvenkaartHeroDoorCrop = $"{Base}/bedrijvenkaart-hero-door-crop.jpg?v={Version}";
    public const string BedrijvenkaartHeroDoorDesktop = $"{Base}/bedrijvenkaart-hero-door-desktop.jpg?v={Version}";
    public const string BedrijvenkaartHeroBandC = BedrijvenkaartHeroDesktopPng;
    public const string BedrijvenkaartHeroDesktopPng = $"{Base}/bedrijvenkaart-hero-desktop.png?v={Version}";
    public const string CompanyPublicHeroArtPng = $"{Base}/company-public-hero-art.png?v={Version}";

    private const string MobFiguresBase = $"{Base}/mob-figures";

    public const string MobFiguresSollicitaties = $"{MobFiguresBase}/werkervaring-mob-figures.png?v={Version}";
    public const string MobFiguresBewaard = $"{MobFiguresBase}/vervoer-mob-figures.png?v={Version}";
    public const string MobFiguresInstellingen = $"{MobFiguresBase}/beschikbaarheid-mob-figures.png?v={Version}";
    public const string MobFiguresKompas = $"{MobFiguresBase}/kompas-mob-figures.png?v={Version}";
    public const string MobFiguresDroombaan = $"{MobFiguresBase}/droombaan-mob-figures.png?v={Version}";
    public const string MobFiguresOpleiding = $"{MobFiguresBase}/opleiding-mob-figures.png?v={Version}";

    /// <summary>Opaque figure PNG for mobile hero (right slot); null when band art has no mob extract.</summary>
    public static string? MobileFigureUrl(string? bandArtUrl)
    {
        if (string.IsNullOrWhiteSpace(bandArtUrl))
        {
            return null;
        }

        if (bandArtUrl.Contains("werkervaring-profession-wide", StringComparison.OrdinalIgnoreCase))
        {
            return MobFiguresSollicitaties;
        }

        if (bandArtUrl.Contains("vervoer-profession-wide", StringComparison.OrdinalIgnoreCase))
        {
            return MobFiguresBewaard;
        }

        if (bandArtUrl.Contains("beschikbaarheid-profession-wide", StringComparison.OrdinalIgnoreCase))
        {
            return MobFiguresInstellingen;
        }

        if (bandArtUrl.Contains("kompas-hero-wide", StringComparison.OrdinalIgnoreCase))
        {
            return MobFiguresKompas;
        }

        if (bandArtUrl.Contains("droombaan-profession-wide", StringComparison.OrdinalIgnoreCase))
        {
            return MobFiguresDroombaan;
        }

        if (bandArtUrl.Contains("opleiding.jpg", StringComparison.OrdinalIgnoreCase))
        {
            return MobFiguresOpleiding;
        }

        return null;
    }
    public const string DroombaanProfessionWide = $"{Base}/droombaan-profession-wide.jpg?v={Version}";
    public const string DroombaanBand = $"{Base}/droombaan.jpg?v={Version}";
    public const string AccountMobileArt = $"{Base}/kennismaken.jpg?v={Version}";

    /// <summary>Fallback when a saved vacancy has no photo — kantoor, incl. rolstoel.</summary>
    public const string SavedNoPhotoArt = $"{Base}/spots/job-inclusief-kantoor.jpg?v={Version}";

    public const string EmptyLikesArt = $"{Base}/droombaan.jpg?v={Version}";

    public const string SpotKaart = $"{Base}/spots/kaart.jpg?v={Version}";
    public const string SpotVragen = $"{Base}/spots/vragen.jpg?v={Version}";
    public const string SpotWerkgever = $"{Base}/spots/werkgever.jpg?v={Version}";
    public const string SpotWerkervaring = $"{Base}/werkervaring.jpg?v={Version}";
    public const string SpotOpleiding = $"{Base}/opleiding.jpg?v={Version}";
    public const string SpotDroombaan = $"{Base}/droombaan.jpg?v={Version}";

    /// <summary>Care sector spot — full-figure scene (not portrait crop).</summary>
    public const string SpotJobZorg = $"{Base}/spots/job-zorg-scene.jpg?v={Version}";
    public const string SpotJobLogistiek = $"{Base}/spots/job-logistiek.jpg?v={Version}";
    public const string SpotJobRetail = $"{Base}/spots/job-retail.jpg?v={Version}";
    public const string SpotJobDieren = $"{Base}/spots/job-dieren.jpg?v={Version}";
    public const string SpotJobFlex = $"{Base}/spots/job-flex.jpg?v={Version}";
    public const string SpotJobSchoonmaak = $"{Base}/spots/job-schoonmaak.jpg?v={Version}";
    public const string SpotInclusiefZorgLogistiek = $"{Base}/spots/job-inclusief-zorg-logistiek.jpg?v={Version}";
    public const string SpotInclusiefKantoor = $"{Base}/spots/job-inclusief-kantoor.jpg?v={Version}";

    /// <summary>Sector keys for banenkaart popup illustrated fallbacks (see jobMap illustratedSpotForVacancy).</summary>
    public static IReadOnlyDictionary<string, string> BanenkaartIllustratedSpots { get; } =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["zorg"] = SpotJobZorg,
            ["logistiek"] = SpotJobLogistiek,
            ["horeca"] = SpotJobZorg,
            ["retail"] = SpotJobRetail,
            ["winkel"] = SpotJobRetail,
            ["bouw"] = SpotJobLogistiek,
            ["techniek"] = SpotJobLogistiek,
            ["kantoor"] = SpotInclusiefKantoor,
            ["schoonmaak"] = SpotJobSchoonmaak,
            ["tuin"] = SpotJobDieren,
            ["dieren"] = SpotJobDieren,
            ["default"] = SpotInclusiefKantoor
        };

    /// <summary>Reference heroes — PR #737 ref-full (two women at table).</summary>
    public const string RefVerifRequest = $"/images/candidate/ref-full.jpg?v={Version}";

    public const string RefVerifPublic = $"/images/candidate/ref-full.jpg?v={Version}";

    public const string RefVerifMobile = $"/images/candidate/ref-full.jpg?v={Version}";

    public const string RefVerifHeroArtPosition = "right bottom";

    public const string RefVerifHeroMobilePosition = "right bottom";
}

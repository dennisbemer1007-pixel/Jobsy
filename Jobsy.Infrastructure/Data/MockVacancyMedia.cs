using Jobsy.Core.Enums;

namespace Jobsy.Infrastructure.Data;

/// <summary>
/// Shared mock media + copy helpers for vacancy seeders and backfill.
/// Uses picsum.photos seed URLs (stable HTTPS images) instead of fragile Unsplash IDs.
/// </summary>
internal static class MockVacancyMedia
{
    public const int MinRichDescriptionLength = 400;

    public static readonly string[] DemoVideos =
    [
        "https://www.youtube.com/watch?v=9No-FiEInLA",
        "https://www.youtube.com/watch?v=4Cr2I4aKgC4",
        "https://www.youtube.com/watch?v=dQw4w9WgXcQ"
    ];

    public static string ImageUrl(Guid vacancyId) =>
        $"https://picsum.photos/seed/jobsy-{vacancyId:N}/600/400";

    public static string ImageUrl(string seed) =>
        $"https://picsum.photos/seed/{Uri.EscapeDataString(seed)}/600/400";

    public static string VideoUrl(int index) =>
        DemoVideos[Math.Abs(index) % DemoVideos.Length];

    public static string VideoUrl(Guid vacancyId)
    {
        var hash = vacancyId.GetHashCode();
        return DemoVideos[Math.Abs(hash) % DemoVideos.Length];
    }

    /// <summary>
    /// True when the stored image is missing or likely broken (legacy Unsplash IDs).
    /// Local /images/ and picsum URLs are kept.
    /// </summary>
    public static bool NeedsImageBackfill(string? imageUrl)
    {
        if (string.IsNullOrWhiteSpace(imageUrl))
        {
            return true;
        }

        if (imageUrl.StartsWith("/images/", StringComparison.OrdinalIgnoreCase)
            || imageUrl.StartsWith("data:image/", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (imageUrl.Contains("picsum.photos", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        // Many historical Unsplash photo IDs in seed data now 404.
        if (imageUrl.Contains("images.unsplash.com", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return false;
    }

    public static bool NeedsVideoBackfill(string? videoUrl) =>
        string.IsNullOrWhiteSpace(videoUrl);

    public static bool NeedsDescriptionBackfill(string? description) =>
        string.IsNullOrWhiteSpace(description)
        || description.Trim().Length < MinRichDescriptionLength;

    public static string BuildRichDescription(
        string title,
        string? existing,
        string? companyName,
        WorkType workTypes,
        decimal? hourlyWage,
        int index)
    {
        if (!NeedsDescriptionBackfill(existing) && !string.IsNullOrWhiteSpace(existing))
        {
            return existing!;
        }

        var company = string.IsNullOrWhiteSpace(companyName) ? "Ons team" : companyName.Trim();
        var branch = FirstWorkTypeLabel(workTypes);
        var wageLine = hourlyWage is > 0
            ? $"Je start op €{hourlyWage.Value:0.00} per uur (bruto), afhankelijk van ervaring en inzet."
            : "Het uurtarief bespreken we graag in het kennismakingsgesprek.";
        var intro = string.IsNullOrWhiteSpace(existing)
            ? $"{company} zoekt versterking voor de rol {title}."
            : existing.Trim();

        var duties = DutiesFor(workTypes, index);
        var offer = Offers[index % Offers.Length];
        var profile = Profiles[index % Profiles.Length];

        return
            $"{intro} " +
            $"Je komt terecht in een {branch.ToLowerInvariant()}-omgeving met een informeel team en korte lijnen.\n\n" +
            $"Wat ga je doen?\n{duties}\n\n" +
            $"Wat bieden wij?\n{wageLine} {offer} " +
            $"We zorgen voor een snelle inwerkperiode en een vast aanspreekpunt op de werkvloer.\n\n" +
            $"Wie zoeken wij?\n{profile} " +
            $"Je communiceert helder, komt afspraken na en vindt het leuk om samen resultaat te boeken. " +
            $"Solliciteer via Jobsy — we reageren doorgaans binnen één werkdag. " +
            $"(Mock vacaturetekst #{index + 1}.)";
    }

    private static string FirstWorkTypeLabel(WorkType workTypes)
    {
        foreach (WorkType flag in Enum.GetValues<WorkType>())
        {
            if (flag is WorkType.None || !workTypes.HasFlag(flag))
            {
                continue;
            }

            return flag.ToString();
        }

        return "Flex";
    }

    private static string DutiesFor(WorkType workTypes, int index)
    {
        if (workTypes.HasFlag(WorkType.Horeca))
        {
            return DutiesHoreca[index % DutiesHoreca.Length];
        }

        if (workTypes.HasFlag(WorkType.Winkel))
        {
            return DutiesWinkel[index % DutiesWinkel.Length];
        }

        if (workTypes.HasFlag(WorkType.Logistiek))
        {
            return DutiesLogistiek[index % DutiesLogistiek.Length];
        }

        if (workTypes.HasFlag(WorkType.Tuinbouw))
        {
            return DutiesTuinbouw[index % DutiesTuinbouw.Length];
        }

        if (workTypes.HasFlag(WorkType.Zorg))
        {
            return DutiesZorg[index % DutiesZorg.Length];
        }

        if (workTypes.HasFlag(WorkType.Kantoor))
        {
            return DutiesKantoor[index % DutiesKantoor.Length];
        }

        if (workTypes.HasFlag(WorkType.Bouw))
        {
            return DutiesBouw[index % DutiesBouw.Length];
        }

        if (workTypes.HasFlag(WorkType.Schoonmaak))
        {
            return DutiesSchoonmaak[index % DutiesSchoonmaak.Length];
        }

        if (workTypes.HasFlag(WorkType.Productie))
        {
            return DutiesProductie[index % DutiesProductie.Length];
        }

        return "Je pakt wisselende taken op, stemt af met collega’s en houdt de werkplek netjes en veilig.";
    }

    private static readonly string[] Offers =
    [
        "Reiskostenvergoeding volgens onze regeling en korting bij partners.",
        "Doorgroeimogelijkheden naar allround of leidinggevende rollen.",
        "Personeelskorting en een teamuitje per seizoen.",
        "Goede werkkleding en veiligheidsmiddelen worden vergoed.",
        "Uitbetaling via payroll zonder gedoe, met duidelijke urenregistratie.",
        "Ruimte om je uren af te stemmen op school of andere werkzaamheden.",
        "Certificeringstrajecten (bijv. BHV of heftruck) in overleg.",
        "Direct een vast aanspreekpunt en een warme overdracht bij de start."
    ];

    private static readonly string[] Profiles =
    [
        "Je werkt netjes, veilig en houdt van aanpakken.",
        "Klantvriendelijkheid en een positieve houding vinden we belangrijk.",
        "Je kunt zelfstandig werken én goed samenwerken in een klein team.",
        "Stressbestendigheid helpt: het kan soms druk zijn.",
        "Initiatief tonen mag: zie je iets liggen, pak je het op.",
        "Je bent representatief, betrouwbaar en leert snel."
    ];

    private static readonly string[] DutiesHoreca =
    [
        "Je bereidt dranken, bedient gasten, houdt de toonbank bij en helpt met opruimen aan het eind van de shift.",
        "Je doet mise-en-place, ondersteunt de keuken tijdens piekmomenten en houdt hygiëne hoog op de agenda.",
        "Je neemt bestellingen op, serveert, rekent af en zorgt dat tafels snel weer klaarstaan."
    ];

    private static readonly string[] DutiesWinkel =
    [
        "Je werkt aan de kassa, helpt klanten, houdt de servicebalie bij en springt bij op de vloer.",
        "Je pakt rollcontainers uit, vult schappen, draait FEFO en ruimt retouren netjes op.",
        "Je wisselt tussen klantenservice, prijzen, voorraad en kleine administratieve klussen."
    ];

    private static readonly string[] DutiesLogistiek =
    [
        "Je picked orders met scanner, bouwt pallets, controleert aantallen en levert af bij expeditie.",
        "Je ontvangt zendingen, zet voorraad weg, doet tellingen en helpt bij laden en lossen.",
        "Je scant colli, bouwt ritten klaar, stemt af met chauffeurs en houdt de dockzone ordelijk."
    ];

    private static readonly string[] DutiesTuinbouw =
    [
        "Je plukt, sorteert en verzorgt gewassen volgens planning en kwaliteitsafspraken.",
        "Je rijdt karren, houdt paden vrij en werkt veilig met gereedschap in de kas.",
        "Je bundelt, controleert en maakt producten klaar voor transport of veiling."
    ];

    private static readonly string[] DutiesZorg =
    [
        "Je helpt bij dagelijkse activiteiten, begeleidt cliënten en stemt af met het zorgteam.",
        "Je ondersteunt bij huishoudelijke taken, activiteiten en een warme, veilige sfeer.",
        "Je observeert, rapporteert bijzonderheden en werkt volgens afgesproken protocollen."
    ];

    private static readonly string[] DutiesKantoor =
    [
        "Je verwerkt post en e-mail, plant afspraken en houdt administratie actueel.",
        "Je ondersteunt planning, facturatie en interne communicatie met korte lijnen.",
        "Je beantwoordt vragen, bereidt stukken voor en bewaakt deadlines."
    ];

    private static readonly string[] DutiesBouw =
    [
        "Je assisteert op de bouwplaats, draagt materialen aan en werkt veilig volgens instructies.",
        "Je helpt met voorbereiding, afplakken, opruimen en eenvoudige uitvoerende taken.",
        "Je werkt mee aan montage of afbouw en houdt de werkplek netjes."
    ];

    private static readonly string[] DutiesSchoonmaak =
    [
        "Je maakt werkruimtes schoon volgens schema, vult verbruiksmiddelen bij en meldt gebreken.",
        "Je poetst sanitaire voorzieningen, vloeren en contactpunten met aandacht voor hygiëne.",
        "Je werkt zelfstandig langs meerdere locaties en laat elke ruimte presentabel achter."
    ];

    private static readonly string[] DutiesProductie =
    [
        "Je werkt aan de lijn: inpakken, controleren, labelen en doorgeven volgens kwaliteitseisen.",
        "Je doet steekproeven, houdt de werkplek veilig en volgt de productiestappen nauwkeurig.",
        "Je wisselt tussen machinebediening, controle en korte omstellingen."
    ];
}

using Jobsy.Core.Enums;
using Jobsy.Core.Media;

namespace Jobsy.Infrastructure.Data;

/// <summary>
/// Shared mock media + copy helpers for vacancy seeders and backfill.
/// Uses local category fallback photos so demo listings do not contact an image provider.
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
        VacancyImageUrls.Placeholder(vacancyId);

    public static string ImageUrl(Guid vacancyId, WorkType workTypes)
    {
        return VacancyImageUrls.Placeholder(vacancyId, workTypes);
    }

    /// <summary>
    /// Deterministic seed photo from the local category WebP pool so neighbouring
    /// seeded cards (and vacancies of the same company) do not share one image.
    /// Does not change the app-side <see cref="VacancyImageUrls.Placeholder"/> fallback.
    /// </summary>
    public static string SeedImageUrl(int stableIndex, WorkType preferredWorkTypes = WorkType.None)
    {
        _ = preferredWorkTypes; // reserved for future preferred-first rotation
        var pool = SeedPhotoSlugs;
        var idx = Math.Abs(stableIndex) % pool.Length;
        return $"{VacancyImageUrls.LocalPrefix}{pool[idx]}{VacancyImageUrls.FallbackExtension}";
    }

    /// <summary>
    /// Per-company sequential index so no two seed vacancies of the same company share a photo
    /// when the company has ≤ <see cref="SeedPhotoSlugs"/>.Length vacancies.
    /// </summary>
    public static string SeedImageUrlForCompany(Guid companyId, int indexWithinCompany, WorkType preferredWorkTypes = WorkType.None)
    {
        // Mix company salt so neighbouring companies with the same local index differ.
        unchecked
        {
            var salt = companyId.GetHashCode() & 0x7fffffff;
            return SeedImageUrl(salt + indexWithinCompany, preferredWorkTypes);
        }
    }

    /// <summary>Local category WebP slugs under <c>/images/vacancies/</c> (excluding -400 companions).</summary>
    public static readonly string[] SeedPhotoSlugs =
    [
        "bouw", "flex", "horeca", "kantoor", "logistiek", "onderwijs",
        "productie", "schoonmaak", "tuinbouw", "winkel", "zorg"
    ];

    public static string ImageUrl(string seed) =>
        VacancyImageUrls.Placeholder(Guid.Empty, seed);

    /// <summary>
    /// Ensures seeded vacancies: unique ImageUrl per company (while count ≤ pool size),
    /// and no shared ImageUrl for same-category neighbours within ~1 km.
    /// </summary>
    public static void EnsureUniqueSeedPhotos(IReadOnlyList<Jobsy.Core.Entities.Vacancy> vacancies)
    {
        if (vacancies.Count == 0)
        {
            return;
        }

        var pool = SeedPhotoSlugs;
        var byCompany = vacancies
            .GroupBy(v => v.CompanyId)
            .ToDictionary(g => g.Key, g => g.OrderBy(v => v.Id).ToList());

        foreach (var (_, list) in byCompany)
        {
            for (var i = 0; i < list.Count; i++)
            {
                list[i].ImageUrl = SeedImageUrlForCompany(list[i].CompanyId, i, list[i].WorkTypes);
            }
        }

        // Resolve same-category neighbours within 1 km that still share a photo.
        for (var i = 0; i < vacancies.Count; i++)
        {
            for (var j = i + 1; j < vacancies.Count; j++)
            {
                var a = vacancies[i];
                var b = vacancies[j];
                if (a.WorkTypes != b.WorkTypes
                    || a.Location is null
                    || b.Location is null
                    || string.IsNullOrWhiteSpace(a.ImageUrl)
                    || !string.Equals(a.ImageUrl, b.ImageUrl, StringComparison.Ordinal))
                {
                    continue;
                }

                if (HaversineKm(a.Location.Latitude, a.Location.Longitude, b.Location.Latitude, b.Location.Longitude) > 1.0)
                {
                    continue;
                }

                // Pick the next unused slug for b within its company when possible.
                var companyUrls = byCompany[b.CompanyId]
                    .Where(v => v.Id != b.Id)
                    .Select(v => v.ImageUrl)
                    .ToHashSet(StringComparer.Ordinal);
                for (var k = 0; k < pool.Length; k++)
                {
                    var candidate = $"{VacancyImageUrls.LocalPrefix}{pool[k]}{VacancyImageUrls.FallbackExtension}";
                    if (companyUrls.Contains(candidate)
                        || string.Equals(candidate, a.ImageUrl, StringComparison.Ordinal))
                    {
                        continue;
                    }

                    b.ImageUrl = candidate;
                    break;
                }
            }
        }
    }

    private static double HaversineKm(double lat1, double lon1, double lat2, double lon2)
    {
        const double R = 6371.0;
        static double Rad(double d) => d * Math.PI / 180.0;
        var dLat = Rad(lat2 - lat1);
        var dLon = Rad(lon2 - lon1);
        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2)
                + Math.Cos(Rad(lat1)) * Math.Cos(Rad(lat2))
                * Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
        return 2 * R * Math.Asin(Math.Min(1, Math.Sqrt(a)));
    }

    public static string VideoUrl(int index) =>
        DemoVideos[Math.Abs(index) % DemoVideos.Length];

    public static string VideoUrl(Guid vacancyId)
    {
        var hash = vacancyId.GetHashCode();
        return DemoVideos[Math.Abs(hash) % DemoVideos.Length];
    }

    /// <summary>
    /// True when the stored image is missing, a third-party placeholder (Unsplash/picsum),
    /// an old local work-type SVG, or otherwise not a usable local/upload URL.
    /// Current category WebP fallbacks, uploads and data-URIs are kept.
    /// </summary>
    public static bool NeedsImageBackfill(string? imageUrl)
    {
        if (string.IsNullOrWhiteSpace(imageUrl))
        {
            return true;
        }

        if (VacancyImageUrls.IsBrokenUnsplash(imageUrl) || VacancyImageUrls.IsPicsum(imageUrl))
        {
            return true;
        }

        // One-time migration: replace mug/leaf/briefcase SVGs with category WebP photos.
        if (VacancyImageUrls.IsLocalVacancySvg(imageUrl))
        {
            return true;
        }

        if (VacancyImageUrls.IsLocalVacancyFallbackPhoto(imageUrl))
        {
            return false;
        }

        if (imageUrl.StartsWith("/images/", StringComparison.OrdinalIgnoreCase)
            || imageUrl.StartsWith("data:image/", StringComparison.OrdinalIgnoreCase))
        {
            return false;
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
            $"Je kunt direct solliciteren. We reageren doorgaans binnen één werkdag.";
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

using Jobsy.Core.Entities;
using Jobsy.Core.Enums;
using Jobsy.Core.Rules;
using Jobsy.Core.ValueObjects;
using Jobsy.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Jobsy.Infrastructure.Data;

/// <summary>
/// Idempotent banenkaart seed for Den Haag (100), Delft (75) and Zoetermeer (50).
/// Vacancies differ in title, description, image, video, transport, wage and (where set) rijbewijs.
/// Every vacancy gets a working picsum image, a YouTube VideoUrl and an extensive description.
/// </summary>
internal static class HaaglandenVacanciesSeeder
{
    private const string SeedMarker = "Haaglanden banenkaart seed DH100-Delft75-Zoetermeer50";

    // Deterministic company ids: c{region}000000-0000-4000-8000-0000000000NN
    // region: 2 = Den Haag, 3 = Delft, 4 = Zoetermeer
    private static Guid CompanyId(int region, int n) =>
        Guid.Parse($"c{region}000000-0000-4000-8000-{n:D12}");

    // Deterministic vacancy ids: a{region}000000-0000-4000-8000-0000000000NN
    private static Guid VacancyId(int region, int n) =>
        Guid.Parse($"a{region}000000-0000-4000-8000-{n:D12}");

    public static async Task SeedHaaglandenBanenkaartAsync(JobsyDbContext db, ILogger logger)
    {
        if (await db.PlatformLogs.AnyAsync(l =>
                l.Category == "Seed" && l.Message == SeedMarker))
        {
            return;
        }

        if (await db.Vacancies.AnyAsync(v => v.Id == VacancyId(2, 1)))
        {
            db.PlatformLogs.Add(new PlatformLog
            {
                Id = Guid.NewGuid(),
                Level = PlatformLogLevel.Info,
                Category = "Seed",
                Message = SeedMarker,
                CreatedAt = DateTime.UtcNow
            });
            await db.SaveChangesAsync();
            return;
        }

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var endDate = today.AddMonths(4);

        await EnsureCompaniesAsync(db);
        await db.SaveChangesAsync();
        await WmlSalaryTableService.EnsureForAllCompaniesAsync(db);
        var wmlByCompany = await db.CompanySalaryTables
            .AsNoTracking()
            .Where(t => t.IsSystemWml && t.IsActive && t.Rates.Any())
            .Select(t => new { t.CompanyId, t.Id })
            .ToListAsync();
        var salaryByCompany = wmlByCompany
            .GroupBy(t => t.CompanyId)
            .ToDictionary(g => g.Key, g => g.OrderBy(t => t.Id).First().Id);

        var vacancies = BuildAllVacancies(today, endDate, salaryByCompany);
        db.Vacancies.AddRange(vacancies);

        db.PlatformLogs.Add(new PlatformLog
        {
            Id = Guid.NewGuid(),
            Level = PlatformLogLevel.Info,
            Category = "Seed",
            Message = SeedMarker,
            CreatedAt = DateTime.UtcNow
        });

        await db.SaveChangesAsync();
        logger.LogInformation(
            "Haaglanden banenkaart seed: {VacancyCount} active vacancies (Den Haag 100, Delft 75, Zoetermeer 50).",
            vacancies.Length);
    }

    private static async Task EnsureCompaniesAsync(JobsyDbContext db)
    {
        foreach (var city in Cities)
        {
            for (var i = 0; i < city.Companies.Length; i++)
            {
                var c = city.Companies[i];
                var id = CompanyId(city.Region, i + 1);
                if (await db.Companies.AnyAsync(x => x.Id == id)
                    || db.Companies.Local.Any(x => x.Id == id))
                {
                    continue;
                }

                var kvk = $"{city.KvkPrefix}{(i + 1):D3}";
                db.Companies.Add(new Company
                {
                    Id = id,
                    Name = c.Name,
                    KvkNumber = kvk,
                    KvkEstablishmentId = $"{kvk}_0001",
                    Address = c.Address,
                    LogoUrl = null,
                    Type = CompanyType.Employer,
                    Location = new GeoPoint(c.Lat, c.Lng)
                });

                db.TokenTransactions.Add(new TokenTransaction
                {
                    Id = Guid.NewGuid(),
                    CompanyId = id,
                    Amount = 10m,
                    Kind = TokenTransactionKind.Grant,
                    Reason = TokenSpendReason.None,
                    OldBalance = 0,
                    NewBalance = 10m,
                    Note = $"Haaglanden seed grant ({city.Name})",
                    CreatedAt = DateTime.UtcNow
                });
            }
        }
    }

    private static Vacancy[] BuildAllVacancies(
        DateOnly today,
        DateOnly endDate,
        IReadOnlyDictionary<Guid, Guid> salaryByCompany)
    {
        var list = new List<Vacancy>(225);

        foreach (var city in Cities)
        {
            for (var i = 0; i < city.VacancyCount; i++)
            {
                var role = Roles[i % Roles.Length];
                // Rotate roles with a city offset so the same index differs per city.
                var roleOffset = (city.Region * 17 + i * 3) % Roles.Length;
                role = Roles[roleOffset];

                var area = city.Areas[i % city.Areas.Length];
                var companyN = (i % city.Companies.Length) + 1;
                var company = city.Companies[companyN - 1];
                var companyId = CompanyId(city.Region, companyN);

                var license = PickLicense(role, i, city.Region);
                var transport = PickTransport(role, license, i);
                var wage = PickWage(role, i);
                var highlight = i % 19 == 0;
                var vacancyId = VacancyId(city.Region, i + 1);

                var title = BuildTitle(role, area.Name, company.Name, i, city.Name);
                var description = BuildDescription(role, city, area, company.Name, license, wage, i);
                var education = PickEducation(role, i);
                salaryByCompany.TryGetValue(companyId, out var salaryTableId);

                list.Add(new Vacancy
                {
                    Id = vacancyId,
                    Title = title,
                    Description = description,
                    HourlyWage = wage,
                    StartDate = today,
                    EndDate = endDate,
                    Status = VacancyStatus.Active,
                    CompanyId = companyId,
                    Location = new GeoPoint(
                        area.Lat + ((i % 7) - 3) * 0.0012,
                        area.Lng + ((i % 5) - 2) * 0.0015),
                    RequiredTransport = transport,
                    WorkTypes = role.WorkType,
                    WorkTypeLabels = string.Join(", ", WorkTypeLabels.Expand(role.WorkType).Take(2)),
                    ImageUrl = MockVacancyMedia.ImageUrl(vacancyId),
                    IsHighlighted = highlight,
                    HighlightedUntil = highlight
                        ? DateTime.UtcNow.AddDays(VacancyProductRules.HighlightDays)
                        : null,
                    MaxApplications = 8 + (i % 5),
                    SalaryTableId = salaryTableId == Guid.Empty ? null : salaryTableId,
                    RequiredDrivingLicense = DrivingLicenseLabels.Combine(
                        string.IsNullOrWhiteSpace(license) ? null : DrivingLicenseLabels.Split(license)),
                    RequiredEducation = EducationLevelLabels.Combine(
                        education is null ? null : EducationLevelLabels.Split(education),
                        forVacancy: true),
                    VideoUrl = MockVacancyMedia.VideoUrl(city.Region * 1000 + i)
                });
            }
        }

        return list.ToArray();
    }

    private static string BuildTitle(RoleSpec role, string areaName, string companyName, int index, string cityName)
    {
        var companyShort = companyName.Split(' ', 2)[0];
        var variants = new[]
        {
            $"{role.Title} – {areaName}",
            $"{role.Title} bij {companyShort}",
            $"{role.Title} in {areaName} ({cityName})",
            $"{role.ShortTitle} {areaName} · {companyShort}",
            $"Flex: {role.Title} {areaName}"
        };
        // Keep titles distinct within a city (roles/areas cycle).
        var baseTitle = variants[index % variants.Length];
        return index < variants.Length ? baseTitle : $"{baseTitle} ({index + 1})";
    }

    private static string BuildDescription(
        RoleSpec role,
        CitySpec city,
        AreaSpec area,
        string companyName,
        string? license,
        decimal wage,
        int index)
    {
        var shift = Shifts[index % Shifts.Length];
        var perk = Perks[(index + city.Region) % Perks.Length];
        var soft = SoftSkills[(index * 2 + city.Region) % SoftSkills.Length];
        var licenseLine = string.IsNullOrWhiteSpace(license)
            ? "Voor deze functie is geen rijbewijs verplicht."
            : $"Voor deze functie is rijbewijs {license} verplicht; zonder geldig rijbewijs kunnen we je helaas niet inzetten.";

        return
            $"{companyName} zoekt een {role.Title.ToLowerInvariant()} in {area.Name}, {city.Name}. " +
            $"{role.Intro} " +
            $"Je werkt vooral in en rond {area.Landmark}. " +
            $"De locatie past bij wie graag in de regio Haaglanden aan de slag wil en snel resultaat wil zien.\n\n" +
            $"Wat ga je doen?\n{role.Duties} " +
            $"Shifts: {shift}. " +
            $"Je stemt af met collega’s, houdt de werkplek netjes en werkt volgens onze veiligheidsafspraken.\n\n" +
            $"Wat bieden wij?\nJe start op €{wage:0.00} per uur (bruto). {perk} " +
            $"Het team is informeel en praktisch: we helpen je snel wegwijs. " +
            $"Bekijk ook de vacaturevideo voor een indruk van het werk en de sfeer.\n\n" +
            $"Wie zoeken wij?\n{role.Profile} {soft} {licenseLine} " +
            $"Bereikbaarheid: {area.TravelHint}. " +
            $"Solliciteer via Jobsy — we reageren doorgaans binnen één werkdag. " +
            $"(Haaglanden banenkaart testdata #{city.Region}-{index + 1}.)";
    }

    private static string? PickLicense(RoleSpec role, int index, int region)
    {
        // Roughly 30% of vacancies require a license; prefer roles that need one.
        if (role.PreferredLicense is not null && (index + region) % 3 == 0)
        {
            return role.PreferredLicense;
        }

        if ((index + region * 5) % 7 == 0)
        {
            return LicensePool[(index + region) % LicensePool.Length];
        }

        return null;
    }

    private static TransportMode PickTransport(RoleSpec role, string? license, int index)
    {
        if (!string.IsNullOrWhiteSpace(license))
        {
            // License roles almost always need car (sometimes + bike).
            return index % 2 == 0
                ? TransportMode.Car
                : TransportMode.Car | TransportMode.Bike;
        }

        return role.TransportOptions[index % role.TransportOptions.Length];
    }

    private static decimal PickWage(RoleSpec role, int index)
    {
        var step = (index % 6) * 0.25m;
        var wage = role.BaseWage + step;
        return Math.Round(wage, 2);
    }

    private static string? PickEducation(RoleSpec role, int index)
    {
        if (role.WorkType.HasFlag(WorkType.Zorg) && index % 4 == 0)
        {
            return "MBO";
        }

        if (role.WorkType.HasFlag(WorkType.Kantoor) && index % 5 == 0)
        {
            return "MBO, HBO";
        }

        if (role.WorkType.HasFlag(WorkType.Bouw) && index % 6 == 0)
        {
            return "VMBO, MBO";
        }

        return null;
    }

    private sealed record CompanySpec(string Name, string Address, double Lat, double Lng);
    private sealed record AreaSpec(string Name, string Landmark, string TravelHint, double Lat, double Lng);
    private sealed record RoleSpec(
        string Title,
        string ShortTitle,
        WorkType WorkType,
        decimal BaseWage,
        string? PreferredLicense,
        TransportMode[] TransportOptions,
        string Intro,
        string Duties,
        string Profile);
    private sealed record CitySpec(
        string Name,
        int Region,
        int VacancyCount,
        string KvkPrefix,
        CompanySpec[] Companies,
        AreaSpec[] Areas);

    private static readonly string[] LicensePool = ["B", "BE", "AM", "Heftruck", "B, Heftruck", "C", "T"];

    private static readonly string[] Shifts =
    [
        "ochtend- en middagdiensten doordeweeks",
        "avonddiensten en weekendshifts",
        "flexibele dagdelen (min. 12 uur/week)",
        "vroege start rond 06:30",
        "middag tot sluiting",
        "weekendfocus met doordeweekse bijspringers",
        "2-ploegendienst",
        "op afroep binnen een vast rooster"
    ];

    private static readonly string[] Perks =
    [
        "Reiskostenvergoeding volgens onze regeling en korting bij partners.",
        "Direct een vast aanspreekpunt op de werkvloer en snelle inwerkperiode.",
        "Doorgroeimogelijkheden naar allround of leidinggevende rollen.",
        "Personeelskorting en een teamuitje per seizoen.",
        "Goede werkkleding en veiligheidsmiddelen worden vergoed.",
        "Uitbetaling wekelijks via payroll, zonder gedoe.",
        "Ruimte om je uren af te stemmen op school of andere werkzaamheden.",
        "Certificeringstrajecten (bijv. BHV of heftruck) in overleg."
    ];

    private static readonly string[] SoftSkills =
    [
        "Je communiceert duidelijk en houdt van aanpakken.",
        "Je werkt netjes, veilig en komt afspraken na.",
        "Klantvriendelijkheid en een positieve houding vinden we belangrijk.",
        "Je kunt zelfstandig werken én goed samenwerken in een klein team.",
        "Stressbestendigheid helpt: het kan soms druk zijn.",
        "Initiatief tonen mag: zie je iets liggen, pak je het op."
    ];

    private static readonly TransportMode[] MixedTransport =
    [
        TransportMode.Walking | TransportMode.Bike | TransportMode.PublicTransport,
        TransportMode.Bike | TransportMode.PublicTransport,
        TransportMode.Bike | TransportMode.Car,
        TransportMode.PublicTransport | TransportMode.Bike | TransportMode.Car,
        TransportMode.Walking | TransportMode.Bike,
        TransportMode.Bike,
        TransportMode.PublicTransport,
        TransportMode.Car | TransportMode.PublicTransport
    ];

    private static readonly TransportMode[] CarHeavy =
    [
        TransportMode.Car,
        TransportMode.Car | TransportMode.Bike,
        TransportMode.Car | TransportMode.PublicTransport
    ];

    private static readonly RoleSpec[] Roles =
    [
        new("Barista / bediening", "Barista", WorkType.Horeca, 13.40m, null, MixedTransport,
            "In onze zaak draait alles om snelle service en een warme sfeer.",
            "Je bereidt koffie en fris, bedient gasten aan tafel of balie, houdt de toonbank bij en helpt met opruimen aan het eind van de shift.",
            "Ervaring in horeca is een pré, maar enthousiasme telt zwaarder. Je bent representatief en kunt multitasken."),
        new("Keukenhulp", "Keukenhulp", WorkType.Horeca, 13.10m, null, MixedTransport,
            "De keuken is het hart van onze locatie; we zoeken iemand die tempo aankan.",
            "Je doet mise-en-place, afwas, voorraad bijvullen en ondersteunt de kok tijdens piekmomenten.",
            "Hygiënisch werken is vanzelfsprekend. Eerdere keukenervaring is fijn, niet verplicht."),
        new("Bediening avondhoreca", "Bediening", WorkType.Horeca, 13.80m, null, MixedTransport,
            "Avondhoreca met een stevig tempo en een vast team.",
            "Je neemt bestellingen op, serveert, rekent af en zorgt dat tafels snel weer klaarstaan.",
            "Je blijft kalm als het druk is en communiceert helder met keuken en bar."),
        new("Kassamedewerker", "Kassa", WorkType.Winkel, 12.90m, null, MixedTransport,
            "Retail met veel klantcontact in een buurtgerichte winkel.",
            "Je werkt aan de kassa, helpt klanten, houdt de servicebalie bij en springt bij op de vloer.",
            "Je bent vriendelijk, cijfermatig nauwkeurig en gewend om op je tenen te lopen in de spits."),
        new("Vakkenvuller", "Vakkenvuller", WorkType.Winkel, 12.70m, null, MixedTransport,
            "Avond- en ochtendvullen zodat de winkel er scherp uitziet.",
            "Je pakt rollcontainers uit, vult schappen, draait FEFO en ruimt retouren netjes op.",
            "Je werkt zelfstandig, tilt lichte tot middelzware dozen en houdt van een opgeruimde winkel."),
        new("Winkelmedewerker allround", "Winkelmedewerker", WorkType.Winkel, 13.20m, null, MixedTransport,
            "Allround retail: kassa, vloer en soms ontvangst van goederen.",
            "Je wisselt tussen klantenservice, vakken vullen, prijzen en kleine administratieve klussen.",
            "Flexibiliteit en een klantgerichte houding zijn belangrijker dan jaren ervaring."),
        new("Orderpicker", "Orderpicker", WorkType.Logistiek, 14.60m, "Heftruck", CarHeavy,
            "In ons DC of magazijn verzamelen we orders voor retail en horeca.",
            "Je picked orders met scanner, bouwt pallets, controleert aantallen en levert af bij expeditie.",
            "Je werkt veilig, tempo-gericht en nauwkeurig. Magazijnervaring is een pré."),
        new("Magazijnmedewerker", "Magazijn", WorkType.Logistiek, 14.30m, null, MixedTransport,
            "Goederenstromen binnenhouden: inkomend, opslag en uitgaand.",
            "Je ontvangt zendingen, zet voorraad weg, doet tellingen en helpt bij laden/lossen.",
            "Je bent stevig op de been, werkt veilig en kunt eenvoudige administratie bijhouden."),
        new("Expeditie medewerker", "Expeditie", WorkType.Logistiek, 15.00m, "B", CarHeavy,
            "Expeditie is de laatste check voordat zendingen de deur uitgaan.",
            "Je scant colli, bouwt ritten klaar, stemt af met chauffeurs en houdt de dockzone ordelijk.",
            "Je communiceert kort en duidelijk en houdt van een strakke planning."),
        new("Bezorgchauffeur", "Bezorgchauffeur", WorkType.Logistiek, 15.40m, "B", CarHeavy,
            "Bezorgen in de regio met vaste routes en contact met ontvangers.",
            "Je laadt de bus, volgt de route, levert af, laat tekenen en meldt afwijkingen direct.",
            "Je rijdt zorgvuldig in de stad en kunt zelfstandig problemen oplossen onderweg."),
        new("Interne chauffeur", "Chauffeur", WorkType.Logistiek, 16.10m, "B, BE", CarHeavy,
            "Interne ritten tussen vestigingen, loodsen en klanten in Haaglanden.",
            "Je rijdt bestelbus of aanhanger, laadt/lost en zorgt voor een nette ritadministratie.",
            "Je hebt aantoonbare rijervaring en een schone staat van dienst."),
        new("Heftruckchauffeur", "Heftruck", WorkType.Logistiek | WorkType.Productie, 16.40m, "Heftruck", CarHeavy,
            "Heftruckwerk in magazijn of productiehal met wisselende ladingen.",
            "Je verplaatst pallets, vult bufferbanen en werkt volgens veiligheidsinstructies.",
            "Geldig heftruckcertificaat is vereist; ervaring met reachtruck is een plus."),
        new("Zorgassistent", "Zorgassistent", WorkType.Zorg, 15.20m, null, MixedTransport,
            "Ondersteuning van cliënten in een warme, kleinschalige setting.",
            "Je helpt bij dagelijkse activiteiten, begeleidt cliënten en stemt af met het zorgteam.",
            "Empathie en betrouwbaarheid staan voorop. Zorgervaring of MBO-Zorg is een pré."),
        new("Thuiszorg ondersteuner", "Thuiszorg", WorkType.Zorg, 15.50m, "B", CarHeavy,
            "Huishoudelijke en praktische ondersteuning bij cliënten thuis.",
            "Je plant je ronde, ondersteunt bij huishouden, doet boodschappen en rapporteert bijzonderheden.",
            "Je werkt discrete, zelfstandig en kunt goed omgaan met verschillende thuissituaties."),
        new("Activiteitenbegeleider", "Activiteitenbegeleider", WorkType.Zorg, 15.80m, null, MixedTransport,
            "Dagbesteding met creatieve en sociale activiteiten.",
            "Je bereidt activiteiten voor, begeleidt de groep en evalueert kort met collega’s.",
            "Je bent creatief, geduldig en kunt een groep enthousiasmeren."),
        new("Administratief medewerker", "Administratie", WorkType.Kantoor, 15.60m, null, MixedTransport,
            "Kantoorondersteuning voor planning, facturen en mailbox.",
            "Je verwerkt post en e-mail, boekt eenvoudige stukken, plant afspraken en houdt dossiers bij.",
            "Je werkt nauwkeurig in Excel/Office en houdt van een opgeruimde backlog."),
        new("Receptiemedewerker", "Receptie", WorkType.Kantoor, 14.80m, null, MixedTransport,
            "Eerste aanspreekpunt voor bezoekers en telefonische vragen.",
            "Je ontvangt gasten, beheert de agenda van de vestiging en regelt kleine facilitaire zaken.",
            "Je bent representatief, stressbestendig en servicegericht."),
        new("HR / planning assistent", "HR-assistent", WorkType.Kantoor, 16.20m, null, MixedTransport,
            "Ondersteuning van HR en roostering bij een groeiend team.",
            "Je helpt bij roosters, verlofaanvragen, onboardingchecklists en personeelsdossiers.",
            "Je schakelt snel, werkt discreet met persoonsgegevens en hebt affiniteit met HR."),
        new("Timmerhulp bouwplaats", "Timmerhulp", WorkType.Bouw, 16.50m, "B", CarHeavy,
            "Hulp op de bouwplaats bij nieuwbouw en renovatie in de regio.",
            "Je helpt met materiaal, eenvoudig timmerwerk, opruimen en veiligheidsrondes.",
            "Je bent niet bang om buiten te werken en volgt instructies van de voorman."),
        new("Schilder assistent", "Schilder", WorkType.Bouw, 15.70m, null, MixedTransport,
            "Schilderwerk binnenshuis bij woningen en kleine utiliteit.",
            "Je plakt af, schuurt, brengt grondverf aan en helpt met opruimen van de werkplek.",
            "Nette afwerking en oog voor detail zijn belangrijk; VCA is een pré."),
        new("Klusjesman / allround faciliteit", "Klusjesman", WorkType.Bouw | WorkType.Schoonmaak, 15.90m, "B", CarHeavy,
            "Allround klussen op locatie: klein onderhoud en facilitaire support.",
            "Je wisselt lampen, hangt spullen op, doet kleine reparaties en houdt magazijn bij.",
            "Je bent technisch handig, zelfstandig en klantvriendelijk op locatie."),
        new("Schoonmaker kantoren", "Schoonmaker", WorkType.Schoonmaak, 13.50m, null, MixedTransport,
            "Dagelijkse schoonmaak van kantoren en gemeenschappelijke ruimtes.",
            "Je stofzuigt, dweilt, poetst sanitair en vult verbruiksartikelen bij volgens checklist.",
            "Je werkt stipt, discreet en volgens hygiëneprotocollen."),
        new("Glas- en gevelreiniger", "Glaswasser", WorkType.Schoonmaak, 14.90m, "B", CarHeavy,
            "Ramen en gevels schoonmaken bij bedrijven en woontorens.",
            "Je plant routes, reinigt glas veilig en rapporteert schade of risico’s.",
            "Hoogtevrees is een no-go; veilig werken met materiaal is verplicht."),
        new("Productiemedewerker", "Productie", WorkType.Productie, 14.40m, null, MixedTransport,
            "Productielijn met verpakken, controleren en omstellen.",
            "Je bedient eenvoudige machines, controleert kwaliteit en houdt de lijn op tempo.",
            "Je houdt van ritme, werkt veilig en kunt in ploegen draaien."),
        new("Inpakker / kwaliteitscontrole", "Inpakker", WorkType.Productie, 13.90m, null, MixedTransport,
            "Inpakken van eindproducten met steekproefsgewijze controle.",
            "Je pakt volgens order, labelt, weegt en meldt afwijkingen aan de teamleider.",
            "Nauwkeurigheid en tempo gaan hier hand in hand."),
        new("Tuinonderhoud medewerker", "Tuinonderhoud", WorkType.Tuinbouw | WorkType.Schoonmaak, 14.20m, "T", CarHeavy,
            "Groenonderhoud in plantsoenen, tuinen en bedrijventerreinen.",
            "Je maait, snoeit, wiedt en houdt paden netjes; soms rijden met klein materieel.",
            "Je werkt graag buiten in weer en wind en gaat zorgvuldig om met gereedschap."),
        new("Kas- / kwekerijhulp", "Kasnulp", WorkType.Tuinbouw, 13.70m, null, MixedTransport,
            "Seizoenswerk in kas of kwekerij aan de rand van de stad.",
            "Je plant, plukt, sorteert en maakt fust klaar voor transport.",
            "Conditie en zorgvuldigheid zijn belangrijker dan diploma’s."),
        new("Logistiek planner junior", "Planner", WorkType.Kantoor | WorkType.Logistiek, 16.80m, null, MixedTransport,
            "Planning van ritten en magazijnbezetting vanaf kantoor.",
            "Je plant routes, belt chauffeurs, bewaakt ETA’s en stuurt bij bij storingen.",
            "Je bent analytisch, telefonisch sterk en kunt prioriteiten stellen."),
        new("Retail teamleider avond", "Teamleider retail", WorkType.Winkel | WorkType.Kantoor, 17.20m, null, MixedTransport,
            "Aansturen van de avondploeg in de winkel.",
            "Je verdeelt taken, bewaakt kassaschappen, sluit de winkel af en rapporteert aan de filiaalmanager.",
            "Leidinggevende ervaring is een pré; je neemt verantwoordelijkheid."),
        new("Horeca teamleider weekend", "Teamleider horeca", WorkType.Horeca, 17.00m, null, MixedTransport,
            "Weekendregie op de vloer: tempo, kwaliteit en teamgevoel.",
            "Je plant breaks, lost klachten op, bewaakt mise-en-place en sluit de kassa.",
            "Je bent een rustige leider die meewerkt waar nodig.")
    ];

    private static readonly CitySpec[] Cities =
    [
        new(
            "Den Haag",
            Region: 2,
            VacancyCount: 100,
            KvkPrefix: "72001",
            Companies:
            [
                new("Haags Haven Café", "Scheveningseweg 12, Den Haag", 52.0945, 4.2800),
                new("Centrum Retail Den Haag", "Grote Marktstraat 50, Den Haag", 52.0768, 4.3115),
                new("Binckhorst Logistiek", "Binckhorstlaan 120, Den Haag", 52.0685, 4.3360),
                new("Zorggroep Haagse Hout", "Theresiastraat 8, Den Haag", 52.0870, 4.3300),
                new("Kantoorlaan Bezuidenhout", "Bezuidenhoutseweg 30, Den Haag", 52.0855, 4.3400),
                new("Bouwteam Laak", "Laakkade 22, Den Haag", 52.0605, 4.3250),
                new("Schoon Service Haagstad", "Escamplaan 5, Den Haag", 52.0600, 4.2700),
                new("Productie Ypenburg", "Laan van Ypenburg 40, Den Haag", 52.0410, 4.3600),
                new("Strandpaviljoen Scheveningen", "Strandweg 1, Scheveningen", 52.1130, 4.2805),
                new("Statenkwartier Supermarkt", "Frederik Hendriklaan 100, Den Haag", 52.0915, 4.2820),
                new("Transvaal Winkelplein", "Paul Krugerlaan 15, Den Haag", 52.0695, 4.2950),
                new("Haagse Schoonmaakploeg", "Waldorpstraat 2, Den Haag", 52.0680, 4.3200)
            ],
            Areas:
            [
                new("Centrum", "de Grote Markt en Spuiplein", "Tram 2/3/4/6 en fiets; veel locaties loopbaar.", 52.0775, 4.3110),
                new("Scheveningen", "de boulevard en Keizerstraat", "Tram 1/9 of fiets langs de kust.", 52.1045, 4.2755),
                new("Statenkwartier", "Frederik Hendriklaan", "Fiets of tram 11; beperkt parkeren.", 52.0910, 4.2815),
                new("Bezuidenhout", "Theresia- en Bezuidenhoutseweg", "OV via HS/CS of fiets.", 52.0860, 4.3380),
                new("Binckhorst", "Binckhorstlaan en Trekvlietplein", "Fiets/auto; tram in de buurt.", 52.0680, 4.3350),
                new("Laak", "Laakkwartier en Rijswijkseplein", "Tram/bus en fiets vanuit HS.", 52.0615, 4.3240),
                new("Escamp", "Leyweg en Moerwijk", "Bus/tram; auto handig voor vroege shifts.", 52.0550, 4.2750),
                new("Ypenburg", "bedrijventerrein Ypenburg", "Auto of bus; beperkte fietsroutes.", 52.0420, 4.3580),
                new("Zeeheldenkwartier", "Prins Hendrikstraat", "Fiets of tram; dicht bij centrum.", 52.0830, 4.2950),
                new("Transvaal", "Paul Krugerlaan", "Tram/bus; goed bereikbaar per fiets.", 52.0700, 4.2960),
                new("Schilderswijk", "Hoefkade", "OV en fiets; korte reis vanuit HS.", 52.0675, 4.3050),
                new("Loosduinen", "Loosduinse Hoofdstraat", "Bus of auto; fiets vanuit Escamp.", 52.0530, 4.2380),
                new("Mariahoeve", "Voorburgseweg", "Trein/tram RandstadRail of fiets.", 52.0935, 4.3600),
                new("Duindorp", "Nieboerweg", "Fiets of bus; auto voor late diensten.", 52.0960, 4.2550),
                new("Haagse Hout", "Benokalibaan / Leidsestraatweg", "Fiets of auto; OV via station Laan van NOI.", 52.0950, 4.3450)
            ]),
        new(
            "Delft",
            Region: 3,
            VacancyCount: 75,
            KvkPrefix: "73001",
            Companies:
            [
                new("Café Markt Delft", "Markt 20, Delft", 52.0114, 4.3587),
                new("Retail Centrum Delft", "Choorstraat 8, Delft", 52.0125, 4.3560),
                new("Technopolis Logistiek", "Schmelzerlaan 10, Delft", 51.9900, 4.3800),
                new("Zorg Delft Oost", "Oostsingel 5, Delft", 52.0155, 4.3700),
                new("Campus Kantoor Delft", "Mekelweg 4, Delft", 51.9985, 4.3750),
                new("Bouw Delft Zuid", "Kruithuisweg 25, Delft", 51.9850, 4.3600),
                new("Schoon Delft Centrum", "Voldersgracht 3, Delft", 52.0120, 4.3595),
                new("Productie Schieoevers", "Schieoevers 12, Delft", 51.9950, 4.3500),
                new("Tuin & Groen Delft", "Abtswoudseweg 2, Delft", 51.9820, 4.3650),
                new("Delfland Winkelgroep", "Bastiaansplein 1, Delft", 52.0100, 4.3620)
            ],
            Areas:
            [
                new("Centrum", "Markt en Voldersgracht", "Station Delft op loopafstand; fiets ideaal.", 52.0116, 4.3588),
                new("TU-wijk", "Mekelweg / campus", "Bus/fiets vanuit station; beperkte auto-parkeerplekken.", 51.9988, 4.3740),
                new("Tanthof", "Tanthofdreef", "Bus of fiets; auto handig voor vroege diensten.", 51.9855, 4.3550),
                new("Voorhof", "Bastiaansplein", "Tram/bus en fiets vanuit centrum.", 52.0085, 4.3480),
                new("Buitenhof", "Buitenhofdreef", "Fiets of bus; kort naar A13.", 52.0005, 4.3450),
                new("Schieoevers", "bedrijventerrein Schieoevers", "Auto of fiets langs de Schie.", 51.9945, 4.3490),
                new("Wippolder", "Oostsingel", "Fiets vanuit centrum of station.", 52.0145, 4.3680),
                new("Technopolis", "Schmelzerlaan", "Auto/bus; groeiend bedrijventerrein.", 51.9895, 4.3820),
                new("Vrijenban", "Delfgauwseweg", "Fiets of auto richting Pijnacker.", 52.0200, 4.3750),
                new("Hof van Delft", "Papsouwselaan", "Bus/fiets; dicht bij A13 afrit.", 52.0050, 4.3400)
            ]),
        new(
            "Zoetermeer",
            Region: 4,
            VacancyCount: 50,
            KvkPrefix: "74001",
            Companies:
            [
                new("Café Stadshart Zoetermeer", "Handelskade 1, Zoetermeer", 52.0608, 4.4940),
                new("Retail Rokkeveen", "Buytenparklaan 20, Zoetermeer", 52.0450, 4.4700),
                new("DC Zoetermeer Noord", "Zilverstraat 8, Zoetermeer", 52.0800, 4.5100),
                new("Zorg Palenstein", "Palenstein 4, Zoetermeer", 52.0650, 4.5050),
                new("Kantoor Driemanspolder", "Zuidweg 15, Zoetermeer", 52.0520, 4.4800),
                new("Bouw Seghwaert", "Seghwaert 12, Zoetermeer", 52.0700, 4.5200),
                new("Schoon Zoetermeer", "Stationstraat 3, Zoetermeer", 52.0580, 4.4920),
                new("Productie Bleizo", "Bleiswijkseweg 40, Zoetermeer", 52.0400, 4.5300)
            ],
            Areas:
            [
                new("Stadshart", "winkelhart en Spazio", "RandstadRail/station Zoetermeer; fiets centraal.", 52.0605, 4.4935),
                new("Rokkeveen", "Buytenpark en winkelstrip", "Bus/fiets; auto via N209.", 52.0455, 4.4680),
                new("Seghwaert", "Seghwaertplein", "RandstadRail Seghwaert of fiets.", 52.0710, 4.5180),
                new("Palenstein", "wijkcentrum Palenstein", "Bus/RandstadRail; kort naar A12.", 52.0660, 4.5040),
                new("Meerzicht", "Meerzichtlaan", "Fiets of bus; rustige woonwijk.", 52.0550, 4.4700),
                new("Buytenwegh", "Buytenweghplein", "Bus/fiets; dicht bij Buytenpark.", 52.0500, 4.4550),
                new("Noordhove", "Noordhovelaan", "Auto of bus; bedrijventerrein dichtbij.", 52.0780, 4.5050),
                new("Driemanspolder", "RandstadRailhalte Driemanspolder", "OV uitstekend; fiets vanuit centrum.", 52.0525, 4.4820),
                new("Bleizo", "Bleizo bedrijventerrein", "Auto of bus; grens Zoetermeer/Lansingerland.", 52.0390, 4.5320),
                new("Oosterheem", "Oosterheemplein", "RandstadRail Oosterheem of fiets.", 52.0750, 4.5350)
            ])
    ];

}

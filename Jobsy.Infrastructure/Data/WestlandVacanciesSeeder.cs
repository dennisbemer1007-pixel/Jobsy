using Jobsy.Core.Entities;
using Jobsy.Core.Enums;
using Jobsy.Core.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Jobsy.Infrastructure.Data;

/// <summary>
/// Idempotent banenkaart test seed: ~50 Active vacancies across Westland,
/// covering all work types, transport modes, wage bands and distance/radius scenarios.
/// </summary>
internal static class WestlandVacanciesSeeder
{
    private const string SeedMarker = "Westland banenkaart seed 50";
    private static readonly Guid WestlandFreshId = Guid.Parse("11111111-1111-1111-1111-111111111111");

    // Deterministic company ids: c1000000-0000-4000-8000-0000000000NN
    private static Guid CompanyId(int n) => Guid.Parse($"c1000000-0000-4000-8000-{n:D12}");

    // Deterministic vacancy ids: a1000000-0000-4000-8000-0000000000NN
    private static Guid VacancyId(int n) => Guid.Parse($"a1000000-0000-4000-8000-{n:D12}");

    public static async Task SeedWestlandBanenkaartAsync(JobsyDbContext db, ILogger logger)
    {
        if (await db.PlatformLogs.AnyAsync(l =>
                l.Category == "Seed" && l.Message == SeedMarker))
        {
            return;
        }

        // Upgrade path: marker missing but vacancies already present (partial prior run).
        if (await db.Vacancies.AnyAsync(v => v.Id == VacancyId(1)))
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
        var salaryTableId = await EnsureSalaryTableAsync(db);
        await db.SaveChangesAsync();

        var westlandFreshExists = await db.Companies.AnyAsync(c => c.Id == WestlandFreshId)
            || db.Companies.Local.Any(c => c.Id == WestlandFreshId);
        var vacancies = BuildVacancies(today, endDate, salaryTableId, westlandFreshExists);
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
            "Westland banenkaart seed: {CompanyCount} companies, {VacancyCount} active vacancies.",
            12,
            vacancies.Length);
    }

    private static async Task EnsureCompaniesAsync(JobsyDbContext db)
    {
        var companies = new (int N, string Name, string Kvk, string Address, double Lat, double Lng)[]
        {
            (1, "KasWerk Naaldwijk", "71001001", "Dijkweg 12, Naaldwijk", 51.9944, 4.2097),
            (2, "Tomatenpark De Lier", "71001002", "Burgemeester Elsenweg 40, De Lier", 51.9750, 4.2480),
            (3, "Bloemenhal Honselersdijk", "71001003", "Veilinglaan 5, Honselersdijk", 51.9825, 4.2210),
            (4, "Strandpaviljoen Monster", "71001004", "Zeeweg 8, Monster", 52.0240, 4.1750),
            (5, "Supermarkt Poeldijk", "71001005", "Voorstraat 22, Poeldijk", 52.0150, 4.2200),
            (6, "Zorghuis Wateringen", "71001006", "Plein 3, Wateringen", 52.0235, 4.2730),
            (7, "Bouwbedrijf Maasdijk", "71001007", "Maasdijkseweg 18, Maasdijk", 51.9590, 4.2150),
            (8, "Kantoor Kwintsheul", "71001008", "Heulweg 9, Kwintsheul", 52.0050, 4.2400),
            (9, "Logistiek 's-Gravenzande", "71001009", "Naaldwijkseweg 100, 's-Gravenzande", 51.9980, 4.1650),
            (10, "Productie Heenweg", "71001010", "Industrieweg 4, Heenweg", 51.9900, 4.1550),
            (11, "Schoon & Fris Westland", "71001011", "Stationsweg 1, Naaldwijk", 51.9910, 4.2050),
            (12, "Café De Hond Westland", "71001012", "Wilhelminaplein 2, Naaldwijk", 51.9955, 4.2075),
        };

        foreach (var c in companies)
        {
            var id = CompanyId(c.N);
            if (await db.Companies.AnyAsync(x => x.Id == id))
            {
                continue;
            }

            db.Companies.Add(new Company
            {
                Id = id,
                Name = c.Name,
                KvkNumber = c.Kvk,
                KvkEstablishmentId = $"{c.Kvk}_0001",
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
                Note = "Westland banenkaart seed grant",
                CreatedAt = DateTime.UtcNow
            });
        }
    }

    private static async Task<Guid?> EnsureSalaryTableAsync(JobsyDbContext db)
    {
        // Prefer existing De Fred table; otherwise create a Westland youth scale on company 5.
        var existing = await db.CompanySalaryTables
            .Where(t => t.IsActive)
            .Select(t => (Guid?)t.Id)
            .FirstOrDefaultAsync();
        if (existing is not null)
        {
            return existing;
        }

        var companyId = CompanyId(5);
        if (!await db.Companies.AnyAsync(c => c.Id == companyId)
            && !db.Companies.Local.Any(c => c.Id == companyId))
        {
            return null;
        }

        var tableId = Guid.Parse("55555555-5555-5555-5555-555555555501");
        if (!await db.CompanySalaryTables.AnyAsync(t => t.Id == tableId))
        {
            db.CompanySalaryTables.Add(new CompanySalaryTable
            {
                Id = tableId,
                CompanyId = companyId,
                Name = "Westland jeugdschaal",
                IsActive = true
            });
            db.CompanySalaryRates.AddRange(
                new CompanySalaryRate { Id = Guid.NewGuid(), SalaryTableId = tableId, AgeYears = 15, HourlyRate = 4.50m, Label = "15" },
                new CompanySalaryRate { Id = Guid.NewGuid(), SalaryTableId = tableId, AgeYears = 16, HourlyRate = 5.20m, Label = "16" },
                new CompanySalaryRate { Id = Guid.NewGuid(), SalaryTableId = tableId, AgeYears = 17, HourlyRate = 5.90m, Label = "17" },
                new CompanySalaryRate { Id = Guid.NewGuid(), SalaryTableId = tableId, AgeYears = 18, HourlyRate = 8.00m, Label = "18" },
                new CompanySalaryRate { Id = Guid.NewGuid(), SalaryTableId = tableId, AgeYears = 19, HourlyRate = 9.50m, Label = "19" },
                new CompanySalaryRate { Id = Guid.NewGuid(), SalaryTableId = tableId, AgeYears = 20, HourlyRate = 11.50m, Label = "20" },
                new CompanySalaryRate { Id = Guid.NewGuid(), SalaryTableId = tableId, AgeYears = 21, HourlyRate = 14.50m, Label = "21+" });
        }

        return tableId;
    }

    private static Vacancy[] BuildVacancies(
        DateOnly today,
        DateOnly endDate,
        Guid? salaryTableId,
        bool westlandFreshExists)
    {
        // Image pool by work type for variety on the map cards.
        var img = new Dictionary<WorkType, string>
        {
            [WorkType.Horeca] = "https://images.unsplash.com/photo-1495474472287-4d71bcdd2085?auto=format&fit=crop&w=600&q=80",
            [WorkType.Winkel] = "https://images.unsplash.com/photo-1556742049-0cfed4f6a45d?auto=format&fit=crop&w=600&q=80",
            [WorkType.Logistiek] = "https://images.unsplash.com/photo-1586528116311-ad8dd3c8310d?auto=format&fit=crop&w=600&q=80",
            [WorkType.Tuinbouw] = "https://images.unsplash.com/photo-1416879595882-3373a0480b5b?auto=format&fit=crop&w=600&q=80",
            [WorkType.Zorg] = "https://images.unsplash.com/photo-1576765608535-5f04d1e3f289?auto=format&fit=crop&w=600&q=80",
            [WorkType.Kantoor] = "https://images.unsplash.com/photo-1497366216548-37526070297c?auto=format&fit=crop&w=600&q=80",
            [WorkType.Bouw] = "https://images.unsplash.com/photo-1504307651254-35680f356dfd?auto=format&fit=crop&w=600&q=80",
            [WorkType.Schoonmaak] = "https://images.unsplash.com/photo-1581578731548-c64695cc6952?auto=format&fit=crop&w=600&q=80",
            [WorkType.Productie] = "https://images.unsplash.com/photo-1565793298595-6a879b1d9492?auto=format&fit=crop&w=600&q=80",
        };

        string Img(WorkType t)
        {
            foreach (WorkType flag in Enum.GetValues<WorkType>())
            {
                if (flag is WorkType.None || !t.HasFlag(flag))
                {
                    continue;
                }

                if (img.TryGetValue(flag, out var url))
                {
                    return url;
                }
            }

            return img[WorkType.Logistiek];
        }

        // A small pool of thematic YouTube videos (publicly embeddable) assigned to
        // a handful of vacancies by 1-based index so the detail page can demo video playback.
        var videoByIndex = new Dictionary<int, string>
        {
            [2]  = "https://www.youtube.com/watch?v=9No-FiEInLA",  // barista/horeca sfeer
            [6]  = "https://www.youtube.com/watch?v=4Cr2I4aKgC4",  // orderpicken / logistiek
            [15] = "https://www.youtube.com/watch?v=dQw4w9WgXcQ",  // teamleider kas (placeholder)
            [22] = "https://www.youtube.com/watch?v=4Cr2I4aKgC4",  // heftruck / logistiek
            [33] = "https://www.youtube.com/watch?v=9No-FiEInLA",  // tomatenplukker tuinbouw
            [49] = "https://www.youtube.com/watch?v=dQw4w9WgXcQ",  // senior monteur
        };

        // Locations spread across Westland (and a few edge spots for radius/travel filters).
        // Candidate home seed ≈ 51.9850, 4.2300 (Honselersdijk).
        var specs = new (string Title, string Desc, int CompanyN, double Lat, double Lng,
            decimal Wage, TransportMode Transport, WorkType Types, bool UseSalaryTable, bool Highlight)[]
        {
            // --- Close (< ~2 km): walking / short bike ---
            ("Kasmedewerker (dichtbij)", "Plukken en sorteren op loopafstand van Honselersdijk.", 3, 51.9835, 4.2250, 13.50m, TransportMode.Walking | TransportMode.Bike, WorkType.Tuinbouw, false, false),
            ("Barista centrum Naaldwijk", "Koffiebar op het plein; te voet of met de fiets.", 12, 51.9955, 4.2075, 13.20m, TransportMode.Walking | TransportMode.Bike | TransportMode.PublicTransport, WorkType.Horeca, false, true),
            ("Vakkenvuller Poeldijk", "Avondvullen in de buurtsuper.", 5, 52.0150, 4.2200, 12.80m, TransportMode.Bike | TransportMode.PublicTransport, WorkType.Winkel, true, false),
            ("Schoonmaker kantoor Kwintsheul", "Avondschoonmaak dichtbij Kwintsheul.", 11, 52.0050, 4.2400, 14.00m, TransportMode.Walking | TransportMode.Bike, WorkType.Schoonmaak, false, false),
            ("Magazijn Naaldwijk", "Inkomend goederen ontvangen.", 1, 51.9940, 4.2105, 14.20m, TransportMode.Bike | TransportMode.Car, WorkType.Logistiek, false, false),

            // --- Mid distance (~3–8 km): bike / OV / car mix ---
            ("Orderpicker De Lier", "Orderpicken in de glastuinbouw.", 2, 51.9750, 4.2480, 14.50m, TransportMode.Bike | TransportMode.Car, WorkType.Logistiek | WorkType.Tuinbouw, false, true),
            ("Hulp in de zorg Wateringen", "Ondersteuning bij dagbesteding.", 6, 52.0235, 4.2730, 15.20m, TransportMode.Bike | TransportMode.PublicTransport | TransportMode.Car, WorkType.Zorg, false, false),
            ("Administratief medewerker", "Facturen en planning op kantoor.", 8, 52.0050, 4.2400, 15.80m, TransportMode.Bike | TransportMode.PublicTransport | TransportMode.Car, WorkType.Kantoor, false, false),
            ("Timmerhulp Maasdijk", "Hulp op de bouwplaats.", 7, 51.9590, 4.2150, 16.50m, TransportMode.Bike | TransportMode.Car, WorkType.Bouw, false, false),
            ("Productiemedewerker Heenweg", "Verpakken en controleren op de lijn.", 10, 51.9900, 4.1550, 14.80m, TransportMode.Bike | TransportMode.Car, WorkType.Productie, false, false),
            ("Chauffeur intern 's-Gravenzande", "Interne ritten tussen loodsen (rijbewijs B).", 9, 51.9980, 4.1650, 16.00m, TransportMode.Car, WorkType.Logistiek, false, false),
            ("Keukenhulp Monster", "Prep en afwas bij het strandpaviljoen.", 4, 52.0240, 4.1750, 13.00m, TransportMode.Bike | TransportMode.PublicTransport, WorkType.Horeca, false, false),
            ("Kassamedewerker Naaldwijk", "Kassa en servicebalie.", 5, 51.9930, 4.2080, 12.50m, TransportMode.Bike | TransportMode.PublicTransport | TransportMode.Walking, WorkType.Winkel, true, false),
            ("Bloemverzorger", "Snijden en bundelen van bloemen.", 3, 51.9825, 4.2210, 13.80m, TransportMode.Bike | TransportMode.Car, WorkType.Tuinbouw, false, false),
            ("Teamleider kas", "Aansturen van een plukteam.", 1, 51.9960, 4.2120, 17.50m, TransportMode.Bike | TransportMode.Car, WorkType.Tuinbouw | WorkType.Logistiek, false, true),

            // --- Farther Westland edge (~8–15 km): travel time / radius filters ---
            ("Strandbediening Ter Heijde", "Seizoensbediening aan zee; auto of OV handig.", 4, 52.0300, 4.1600, 13.40m, TransportMode.Bike | TransportMode.Car | TransportMode.PublicTransport, WorkType.Horeca, false, false),
            ("Nachtdienst productie", "Nachtdienst verpakking (auto vereist).", 10, 51.9880, 4.1500, 17.00m, TransportMode.Car, WorkType.Productie, false, false),
            ("Zorgondersteuner Den Hoorn", "Ondersteuning in zorgappartementen.", 6, 52.0000, 4.2800, 15.00m, TransportMode.Bike | TransportMode.PublicTransport | TransportMode.Car, WorkType.Zorg, false, false),
            ("Magazijn Hoek van Holland", "DC aan de kust; reistijd-test (ver).", 9, 51.9775, 4.1330, 15.50m, TransportMode.Car | TransportMode.PublicTransport, WorkType.Logistiek, false, false),
            ("Schoonmaak loodsen", "Periodieke schoonmaak van teeltloodsen.", 11, 51.9700, 4.2000, 14.30m, TransportMode.Bike | TransportMode.Car, WorkType.Schoonmaak | WorkType.Tuinbouw, false, false),

            // --- Transport exclusivity (so each mode filter returns hits) ---
            ("Alleen lopend: café hulp", "Kleine horeca-klus, alleen te voet bereikbaar gedacht.", 12, 51.9840, 4.2280, 12.00m, TransportMode.Walking, WorkType.Horeca, false, false),
            ("Alleen fiets: folder bezorgen", "Retail folders in Naaldwijk-wijk.", 5, 51.9920, 4.2150, 11.50m, TransportMode.Bike, WorkType.Winkel, false, false),
            ("Alleen auto: heftruck", "Heftruckcertificaat + auto naar afgelegen loods.", 2, 51.9680, 4.2550, 16.80m, TransportMode.Car, WorkType.Logistiek | WorkType.Productie, false, false),
            ("Alleen OV: receptie", "Receptie bij OV-halte Kwintsheul.", 8, 52.0065, 4.2420, 14.10m, TransportMode.PublicTransport, WorkType.Kantoor, false, false),
            ("OV + fiets: winkelhulp", "Winkelhulp in Wateringen centrum.", 5, 52.0220, 4.2700, 12.90m, TransportMode.Bike | TransportMode.PublicTransport, WorkType.Winkel, true, false),

            // --- All work types well covered (extra volume) ---
            ("Bediening lunchroom", "Lunchbediening en opruimen.", 12, 51.9970, 4.2050, 13.10m, TransportMode.Walking | TransportMode.Bike | TransportMode.PublicTransport, WorkType.Horeca, false, false),
            ("Kokshulp", "Voorbereiding en mise-en-place.", 4, 52.0220, 4.1780, 14.60m, TransportMode.Bike | TransportMode.Car, WorkType.Horeca, false, false),
            ("Vulploeg avond", "Vakken vullen na sluiting.", 5, 52.0140, 4.2180, 13.00m, TransportMode.Bike | TransportMode.Car, WorkType.Winkel, true, false),
            ("Klantenservice winkel", "Servicebalie en retouren.", 5, 51.9945, 4.2060, 13.60m, TransportMode.Bike | TransportMode.PublicTransport, WorkType.Winkel | WorkType.Kantoor, false, false),
            ("Reachtruck chauffeur", "Reachtruck in het DC.", 9, 51.9990, 4.1680, 16.20m, TransportMode.Car | TransportMode.Bike, WorkType.Logistiek, false, false),
            ("Expeditie medewerker", "Laden en lossen van trailers.", 9, 51.9965, 4.1620, 15.10m, TransportMode.Bike | TransportMode.Car, WorkType.Logistiek, false, false),
            ("Tomatenplukker", "Seizoenswerk in de kas.", 2, 51.9765, 4.2500, 13.90m, TransportMode.Bike | TransportMode.Car, WorkType.Tuinbouw, false, false),
            ("Paprikaplukker", "Plukken en karren rijden.", 1, 51.9935, 4.2140, 13.70m, TransportMode.Bike | TransportMode.Car, WorkType.Tuinbouw, false, false),
            ("Gewasverzorging", "Snoeien en gewaswerk.", 3, 51.9800, 4.2190, 14.40m, TransportMode.Bike | TransportMode.Car, WorkType.Tuinbouw, false, false),
            ("Thuiszorg assistent", "Ondersteuning bij huishouden cliënten.", 6, 52.0210, 4.2680, 15.40m, TransportMode.Bike | TransportMode.Car | TransportMode.PublicTransport, WorkType.Zorg, false, false),
            ("Activiteitenbegeleider", "Activiteiten in de dagopvang.", 6, 52.0250, 4.2750, 15.90m, TransportMode.Bike | TransportMode.PublicTransport, WorkType.Zorg, false, false),
            ("HR assistent", "Ondersteuning personeelszaken.", 8, 52.0040, 4.2380, 16.10m, TransportMode.Bike | TransportMode.PublicTransport | TransportMode.Car, WorkType.Kantoor, false, false),
            ("Planner logistiek", "Ritten plannen op kantoor.", 8, 52.0070, 4.2450, 16.70m, TransportMode.Bike | TransportMode.Car | TransportMode.PublicTransport, WorkType.Kantoor | WorkType.Logistiek, false, false),
            ("Metselaar hulp", "Hulp bij metselwerk.", 7, 51.9610, 4.2180, 16.40m, TransportMode.Bike | TransportMode.Car, WorkType.Bouw, false, false),
            ("Schilder assistent", "Voorbereiden en afplakken.", 7, 51.9570, 4.2120, 15.60m, TransportMode.Bike | TransportMode.Car, WorkType.Bouw, false, false),
            ("Schoonmaker kantoren", "Dagelijkse kantoorschoonmaak.", 11, 51.9900, 4.2030, 13.30m, TransportMode.Walking | TransportMode.Bike | TransportMode.PublicTransport, WorkType.Schoonmaak, false, false),
            ("Glaswasser", "Ramen van bedrijfspanden.", 11, 51.9850, 4.2000, 14.90m, TransportMode.Bike | TransportMode.Car, WorkType.Schoonmaak, false, false),
            ("Inpakker verse producten", "Inpaklijn verse groenten.", 10, 51.9915, 4.1580, 13.50m, TransportMode.Bike | TransportMode.Car, WorkType.Productie | WorkType.Tuinbouw, false, false),
            ("Kwaliteitscontroleur", "Steekproeven op de lijn.", 10, 51.9890, 4.1520, 15.30m, TransportMode.Bike | TransportMode.Car, WorkType.Productie, false, false),

            // --- Wage band extremes (min/max hourly with age) ---
            ("Jeugd bijbaan winkel", "Bijbaan 15+ met leeftijdsschaal (laag).", 5, 52.0130, 4.2220, 12.00m, TransportMode.Bike | TransportMode.PublicTransport | TransportMode.Walking, WorkType.Winkel, true, false),
            ("Senior monteur kas", "Technisch onderhoud kassen (hoog loon).", 1, 51.9950, 4.2160, 18.50m, TransportMode.Car | TransportMode.Bike, WorkType.Tuinbouw | WorkType.Bouw, false, true),
            ("Stagiair kantoor", "Meewerkstage administratie (instaploon).", 8, 52.0035, 4.2360, 8.50m, TransportMode.Bike | TransportMode.PublicTransport, WorkType.Kantoor, false, false),
            ("Flex horeca weekend", "Weekendshifts, middenloon.", 12, 51.9940, 4.2090, 14.00m, TransportMode.Walking | TransportMode.Bike | TransportMode.PublicTransport | TransportMode.Car, WorkType.Horeca, false, false),
            ("Allround Westland Fresh", "Extra orderpick-capaciteit bij Westland Fresh.", 0, 51.9812, 4.2235, 14.50m, TransportMode.Bike | TransportMode.Car, WorkType.Logistiek | WorkType.Tuinbouw, false, false),
        };

        var list = new List<Vacancy>(specs.Length);
        for (var i = 0; i < specs.Length; i++)
        {
            var s = specs[i];
            // CompanyN 0 → existing Westland Fresh; fallback to KasWerk Naaldwijk.
            var companyId = s.CompanyN == 0
                ? (westlandFreshExists ? WestlandFreshId : CompanyId(1))
                : CompanyId(s.CompanyN);
            list.Add(new Vacancy
            {
                Id = VacancyId(i + 1),
                Title = s.Title,
                Description = s.Desc + " Westland banenkaart testdata.",
                HourlyWage = s.Wage,
                StartDate = today,
                EndDate = endDate,
                Status = VacancyStatus.Active,
                CompanyId = companyId,
                Location = new GeoPoint(s.Lat, s.Lng),
                RequiredTransport = s.Transport,
                WorkTypes = s.Types,
                ImageUrl = Img(s.Types),
                IsHighlighted = s.Highlight,
                MaxApplications = 8,
                SalaryTableId = s.UseSalaryTable ? salaryTableId : null,
                VideoUrl = videoByIndex.GetValueOrDefault(i + 1)
            });
        }

        return list.ToArray();
    }
}

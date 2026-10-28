using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using Jobsy.Infrastructure.Media;
using Jobsy.Web.Models;
using Microsoft.AspNetCore.WebUtilities;

namespace Jobsy.Tests;

/// <summary>
/// Fake banenkaart catalog for illustrated captures — Westland / Naaldwijk mockup scenario
/// (Kasweg 12, ~40 vacatures, employer + via-intermediary).
/// </summary>
public static class IllustratedPagesVacancyStub
{
    internal const double OriginLat = 51.9935;
    internal const double OriginLng = 4.2085;
    internal const string OriginLabel = "Kasweg 12, 2671 AB Naaldwijk";

    /// <summary>Acceptatie repro: Delft origin, ~181 vacatures in travel ring.</summary>
    internal const double DelftOriginLat = 51.99389784;
    internal const double DelftOriginLng = 4.37897242;

    private static readonly JsonSerializerOptions Json = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    private static readonly Guid CompanyId = Guid.Parse("a1000000-0000-0000-0000-000000000001");
    private static readonly Guid IntermediaryCompanyId = Guid.Parse("a1000000-0000-0000-0000-000000000002");

    private static readonly SeedVacancy[] WestlandSeeds = BuildWestlandSeeds();
    private static readonly SeedVacancy[] DelftSeeds = BuildDelftSeeds();

    public static string? ThumbnailForImageRequest(string path)
    {
        var id = TryParseVacancyId(path);
        if (id is null)
        {
            return null;
        }

        var seed = FindSeedById(id.Value);
        return seed?.Image;
    }

    public static byte[]? MapPopupPhotoBytes(string path, string? queryString)
    {
        var id = TryParseVacancyId(path);
        if (id is null)
        {
            return null;
        }

        var q = string.IsNullOrEmpty(queryString)
            ? new Dictionary<string, string>()
            : ParseQuery("?" + queryString.TrimStart('?'));
        var variant = q.GetValueOrDefault("variant") ?? "desktop";
        var width = VacancyMapPopupPhotoEncoder.WidthForVariant(variant);
        var seed = FindSeedById(id.Value);
        var webPath = seed?.Image ?? "/images/vacancies/tuinbouw.webp";
        var repoRoot = Directory.GetCurrentDirectory();
        while (!string.IsNullOrEmpty(repoRoot) && !File.Exists(Path.Combine(repoRoot, "Jobsy.sln")))
        {
            repoRoot = Directory.GetParent(repoRoot)?.FullName ?? "";
        }

        if (string.IsNullOrEmpty(repoRoot))
        {
            return null;
        }

        var full = Path.Combine(repoRoot, "Jobsy.Web", "wwwroot", webPath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
        if (!File.Exists(full))
        {
            full = Path.Combine(repoRoot, "Jobsy.Web", "wwwroot", "images", "vacancies", "tuinbouw.webp");
        }

        if (!File.Exists(full))
        {
            return null;
        }

        return VacancyMapPopupPhotoEncoder.ToWebp(File.ReadAllBytes(full), width);
    }

    public static string JsonForPath(string path)
    {
        if (path.Contains("/isochrones", StringComparison.OrdinalIgnoreCase))
        {
            var q = ParseQuery(path);
            var lat = TryDouble(q, "lat") ?? OriginLat;
            var lng = TryDouble(q, "lng") ?? OriginLng;
            q.TryGetValue("minutes", out var minutes);
            return LagenIsochroneFixtures.FeatureCollectionJson(lat, lng, minutes);
        }

        if (path.Contains("/discover", StringComparison.OrdinalIgnoreCase))
        {
            return Serialize(BuildDiscoverPageFromQuery(path));
        }

        if (path.Contains("/map-view", StringComparison.OrdinalIgnoreCase))
        {
            return Serialize(new VacancyMapViewResponse
            {
                Lat = OriginLat,
                Lng = OriginLng,
                Zoom = 11.4,
                PinCount = WestlandSeeds.Length
            });
        }

        if (path.Contains("/banenkaart-catalog", StringComparison.OrdinalIgnoreCase)
            || path.Contains("/banenkaart-client/all", StringComparison.OrdinalIgnoreCase))
        {
            return BuildCatalogColumnarJson();
        }

        if (path.Contains("/pins", StringComparison.OrdinalIgnoreCase))
        {
            return Serialize(BuildPinsFromQuery(path));
        }

        if (path.Contains("/cards", StringComparison.OrdinalIgnoreCase))
        {
            return Serialize(BuildCards());
        }

        if (path.Contains("/card", StringComparison.OrdinalIgnoreCase))
        {
            var id = TryParseVacancyId(path);
            if (id is not null)
            {
                var card = BuildCards().FirstOrDefault(c => c.Id == id);
                if (card is not null)
                {
                    return Serialize(card);
                }
            }
        }

        return "[]";
    }

    private static VacancyListPage BuildDiscoverPageFromQuery(string pathAndQuery)
    {
        var q = ParseQuery(pathAndQuery);
        var originLat = TryDouble(q, "originLat") ?? OriginLat;
        var originLng = TryDouble(q, "originLng") ?? OriginLng;
        var transport = q.GetValueOrDefault("transport") ?? "Fiets";
        var maxMinutes = (int)(TryDouble(q, "maxMinutes") ?? 20);
        var skip = Math.Max(0, (int)(TryDouble(q, "skip") ?? 0));
        var take = Math.Max(1, (int)(TryDouble(q, "take") ?? 20));

        var seeds = ResolveSeeds(originLat, originLng);
        var filtered = seeds
            .Where(s => TravelMinutes(originLat, originLng, s.Lat, s.Lng, transport) <= maxMinutes)
            .OrderBy(s => TravelMinutes(originLat, originLng, s.Lat, s.Lng, transport))
            .ToList();

        var page = filtered.Skip(skip).Take(take).Select(ToListItem).ToList();
        return new VacancyListPage
        {
            Items = page,
            Skip = skip,
            Take = take,
            TotalCount = filtered.Count,
            HasMore = skip + page.Count < filtered.Count
        };
    }

    private static VacancyListItem ToListItem(SeedVacancy s)
    {
        var start = DateOnly.FromDateTime(DateTime.UtcNow.Date);
        var end = start.AddMonths(2);
        var mins = TravelMinutes(OriginLat, OriginLng, s.Lat, s.Lng, "Fiets");
        return new VacancyListItem
        {
            Id = s.Id,
            Title = s.Title,
            Description = "Illustrated Westland vacature.",
            HourlyWage = 14.50m + (s.Index % 5) * 0.25m,
            WageVisible = true,
            StartDate = start,
            EndDate = end,
            Status = "Active",
            CompanyId = s.IsIntermediary ? IntermediaryCompanyId : CompanyId,
            CompanyName = s.Company,
            CompanyAddress = s.Place,
            OfferedByLabel = s.OfferedBy,
            ImageUrl = s.Image,
            Latitude = s.Lat,
            Longitude = s.Lng,
            RequiredTransport = ["Fiets", "OV"],
            TravelMinutes = mins,
            DistanceKm = HaversineKm(OriginLat, OriginLng, s.Lat, s.Lng),
            WorkTypes = [s.WorkType],
            CategoryColorHex = s.Colour,
            MatchPercent = 68 + (s.Index % 25),
            FitPercent = 68 + (s.Index % 25),
            FitBand = "Good",
            MatchColorBand = "good",
            FitWhyLine = s.FitWhy,
            FitWhyKinds = ["interest"]
        };
    }

    private static string BuildCatalogColumnarJson()
    {
        var n = WestlandSeeds.Length;
        var ids = WestlandSeeds.Select(s => s.Id.ToString("D")).ToArray();
        var lats = WestlandSeeds.Select(s => s.Lat).ToArray();
        var lngs = WestlandSeeds.Select(s => s.Lng).ToArray();
        var titles = WestlandSeeds.Select(s => s.Title).ToArray();
        var companies = WestlandSeeds.Select(s => s.Company).ToArray();
        var offeredBy = WestlandSeeds.Select(s => s.OfferedBy ?? "").ToArray();
        var places = WestlandSeeds.Select(s => s.Place).ToArray();
        var catNames = WestlandSeeds.Select(s => s.CategoryName).ToArray();
        var colours = WestlandSeeds.Select(s => s.Colour).ToArray();
        var pm = WestlandSeeds.Select(s => s.IsIntermediary ? 12 : 0).ToArray();
        var co = WestlandSeeds.Select(s => (s.IsIntermediary ? IntermediaryCompanyId : CompanyId).ToString("D")).ToArray();
        return $$"""
            {"v":1,"n":{{n}},"id":{{JsonSerializer.Serialize(ids)}},"lat":{{JsonSerializer.Serialize(lats)}},"lng":{{JsonSerializer.Serialize(lngs)}},"t":{{JsonSerializer.Serialize(titles)}},"c":{{JsonSerializer.Serialize(companies)}},"ob":{{JsonSerializer.Serialize(offeredBy)}},"pl":{{JsonSerializer.Serialize(places)}},"cn":{{JsonSerializer.Serialize(catNames)}},"w":{{JsonSerializer.Serialize(Enumerable.Repeat(1450, n).ToArray())}},"h0":{{JsonSerializer.Serialize(Enumerable.Repeat(16, n).ToArray())}},"h1":{{JsonSerializer.Serialize(Enumerable.Repeat(32, n).ToArray())}},"cat":{{JsonSerializer.Serialize(Enumerable.Repeat("", n).ToArray())}},"co":{{JsonSerializer.Serialize(co)}},"wt":{{JsonSerializer.Serialize(Enumerable.Repeat(1, n).ToArray())}},"tr":{{JsonSerializer.Serialize(Enumerable.Repeat(1, n).ToArray())}},"col":{{JsonSerializer.Serialize(colours)}},"s65":{{JsonSerializer.Serialize(Enumerable.Repeat(0, n).ToArray())}},"pm":{{JsonSerializer.Serialize(pm)}},"iu":{{JsonSerializer.Serialize(Enumerable.Repeat("", n).ToArray())}}}
            """;
    }

    private static List<VacancyPinBootItem> BuildPinsFromQuery(string pathAndQuery)
    {
        var q = ParseQuery(pathAndQuery);
        var originLat = TryDouble(q, "originLat") ?? OriginLat;
        var originLng = TryDouble(q, "originLng") ?? OriginLng;
        var transport = q.GetValueOrDefault("transport") ?? "Fiets";
        var maxMinutes = (int)(TryDouble(q, "maxMinutes") ?? 20);
        var seeds = ResolveSeeds(originLat, originLng);
        var filtered = seeds
            .Where(s => TravelMinutes(originLat, originLng, s.Lat, s.Lng, transport) <= maxMinutes)
            .ToList();

        if (TryParseBbox(q, out var bbox))
        {
            filtered = filtered
                .Where(s => Contains(bbox, s.Lat, s.Lng))
                .ToList();
            // Mimic acc: tight viewport refetch after zoom can drop the whole set (client must not wipe).
            var zoom = TryDouble(q, "zoom");
            if (zoom is >= 12 && filtered.Count == 0 && seeds.Length > 50)
            {
                return [];
            }
        }

        return filtered.Select(s => new VacancyPinBootItem
        {
            Id = s.Id,
            Lat = s.Lat,
            Lng = s.Lng,
            Colour = s.Colour,
            MatchPercent = 68 + (s.Index % 25),
            WorkType = s.WorkType,
            MatchColorBand = "good"
        }).ToList();
    }

    private static List<VacancyCardStub> BuildCards()
        => WestlandSeeds.Select(s => new VacancyCardStub
        {
            Id = s.Id,
            Title = s.Title,
            CompanyName = s.Company,
            OfferedByLabel = s.OfferedBy,
            Place = s.Place,
            ThumbnailUrl = s.Image,
            HourlyWage = 14.50m,
            WageVisible = true,
            WorkTypes = [s.WorkType],
            TravelMinutes = TravelMinutes(OriginLat, OriginLng, s.Lat, s.Lng, "Fiets"),
            MatchPercent = 72,
            MatchColorBand = "good",
            CategoryColorHex = s.Colour,
            CompanyAddress = s.Place,
            FitWhyLine = s.FitWhy
        }).ToList();

    private static SeedVacancy? FindSeedById(Guid id)
        => WestlandSeeds.FirstOrDefault(s => s.Id == id)
           ?? DelftSeeds.FirstOrDefault(s => s.Id == id);

    private static SeedVacancy[] ResolveSeeds(double originLat, double originLng)
    {
        var dKm = HaversineKm(originLat, originLng, DelftOriginLat, DelftOriginLng);
        var wKm = HaversineKm(originLat, originLng, OriginLat, OriginLng);
        return dKm + 0.01 < wKm ? DelftSeeds : WestlandSeeds;
    }

    private static bool TryParseBbox(
        IReadOnlyDictionary<string, string> q,
        out (double MinLat, double MinLng, double MaxLat, double MaxLng) bbox)
    {
        bbox = default;
        var minLat = TryDouble(q, "minLat");
        var minLng = TryDouble(q, "minLng");
        var maxLat = TryDouble(q, "maxLat");
        var maxLng = TryDouble(q, "maxLng");
        if (minLat is null || minLng is null || maxLat is null || maxLng is null)
        {
            return false;
        }

        if (minLat > maxLat || minLng > maxLng)
        {
            return false;
        }

        bbox = (minLat.Value, minLng.Value, maxLat.Value, maxLng.Value);
        return true;
    }

    private static bool Contains(
        (double MinLat, double MinLng, double MaxLat, double MaxLng) box,
        double lat,
        double lng)
        => lat >= box.MinLat && lat <= box.MaxLat && lng >= box.MinLng && lng <= box.MaxLng;

    private static SeedVacancy[] BuildDelftSeeds()
    {
        var list = new List<SeedVacancy>();
        var titles = new[]
        {
            "Operator productie", "Magazijnmedewerker", "Orderpicker", "Technisch operator",
            "Logistiek planner", "Kwaliteitsmedewerker", "Procesoperator", "Montagemedewerker",
            "Chauffeur B", "Administratief medewerker", "Teamleider shift", "Schoonmaak industrie"
        };
        var companies = new[]
        {
            "Delft Tech Campus", "Holland Logistics", "FlexWork Delft", "BlueChem Labs",
            "Stadshaven BV", "Precision Parts", "FoodProcess NL", "CleanFacility"
        };
        var categories = new[] { "Productie", "Logistiek", "Techniek", "Administratie", "Industrie" };
        var workTypes = new[] { "Parttime", "Fulltime", "Bijbaan" };
        // Dense stacks (acc-like): many vacancies share coordinates.
        var stacks = new (double Lat, double Lng, string Place)[]
        {
            (51.9952, 4.3712, "Delft"),
            (51.9918, 4.3825, "Delft"),
            (51.9889, 4.3761, "Delft"),
            (51.9971, 4.3855, "Delft"),
            (51.9865, 4.3688, "Delfgauw"),
            (51.9998, 4.3620, "Ypenburg"),
            (51.9842, 4.3910, "Pijnacker"),
            (51.9939, 4.3790, "Delft centrum"),
            (51.9905, 4.3745, "TNO campus"),
            (51.9968, 4.3778, "Municipal yard"),
            (51.9877, 4.3833, "Rail hub"),
            (51.9924, 4.3665, "Industrial park")
        };

        for (var i = 0; i < 181; i++)
        {
            var stack = stacks[i % stacks.Length];
            var jitter = (i / stacks.Length) * 0.000015;
            var lat = stack.Lat + Math.Sin(i * 1.71) * 0.00004 + jitter;
            var lng = stack.Lng + Math.Cos(i * 2.03) * 0.00005;
            var isIntermediary = i % 3 == 1;
            var company = companies[i % companies.Length];
            var offeredBy = isIntermediary ? "Uitzend Delft" : null;
            list.Add(new SeedVacancy(
                Index: i,
                Id: Guid.Parse($"b3{(i + 1):D6}-0000-0000-0000-000000000001"),
                Title: titles[i % titles.Length] + " " + (i + 1),
                Company: company,
                OfferedBy: offeredBy,
                Place: stack.Place,
                Lat: lat,
                Lng: lng,
                Image: i % 5 == 0 ? "/images/vacancies/tuinbouw.webp" : null,
                WorkType: workTypes[i % workTypes.Length],
                CategoryName: categories[i % categories.Length],
                Colour: i % 3 == 0 ? "#3366cc" : (i % 3 == 1 ? "#1e7a4c" : "#b4500f"),
                FitWhy: "passend rond Delft",
                IsIntermediary: isIntermediary));
        }

        return list.ToArray();
    }

    private static SeedVacancy[] BuildWestlandSeeds()
    {
        var list = new List<SeedVacancy>();
        var titles = new[]
        {
            "Oogstmedewerker tuinbouw", "Orderpicker koelhuis", "Magazijnmedewerker", "Kassamedewerker",
            "Logistiek medewerker", "Inpakmedewerker", "Schoonmaak avond", "Horeca allrounder",
            "Productiemedewerker", "Zorgondersteuner", "Chauffeur distributie", "Teamleider kas",
            "Technisch medewerker", "Administratief medewerker", "Bezorger fiets", "Montagemedewerker"
        };
        var companies = new[]
        {
            "Westland Fresh", "De Kwekerij", "GreenPack DC", "Flora Logistics", "Tomatenhof",
            "Kasteelhof Zorg", "Horeca De Serre", "Tuinbouw Coöp", "LogiWest", "FreshCut BV"
        };
        var categories = new[] { "Tuinbouw", "Logistiek", "Winkel", "Zorg", "Horeca", "Productie" };
        var workTypes = new[] { "Parttime", "Fulltime", "Bijbaan" };

        for (var i = 0; i < 42; i++)
        {
            var angle = i * (Math.PI * 2 / 42) * 1.31 + (i % 7) * 0.09;
            var km = 0.38 + i * 0.152;
            var (lat, lng) = OffsetKm(OriginLat, OriginLng, km, angle);
            // Playwright catalog only: spread coords so initial view shows mockup pins, not one mega-cluster.
            lat += Math.Sin(i * 2.17) * 0.00042;
            lng += Math.Cos(i * 1.93) * 0.00048;
            var isIntermediary = i % 3 == 1;
            var company = companies[i % companies.Length];
            var offeredBy = isIntermediary ? "FlexPlus Westland" : "";
            list.Add(new SeedVacancy(
                Index: i,
                Id: Guid.Parse($"b2{(i + 1):D6}-0000-0000-0000-000000000001"),
                Title: titles[i % titles.Length] + " " + (i + 1),
                Company: company,
                OfferedBy: string.IsNullOrEmpty(offeredBy) ? null : offeredBy,
                Place: i % 2 == 0 ? "Naaldwijk" : "De Lier",
                Lat: lat,
                Lng: lng,
                Image: i % 4 == 0 ? "/images/vacancies/tuinbouw.webp" : null,
                WorkType: workTypes[i % workTypes.Length],
                CategoryName: categories[i % categories.Length],
                Colour: i % 3 == 0 ? "#3366cc" : (i % 3 == 1 ? "#1e7a4c" : "#b4500f"),
                FitWhy: "passend in de regio",
                IsIntermediary: isIntermediary));
        }

        return list.ToArray();
    }

    private static (double Lat, double Lng) OffsetKm(double lat, double lng, double km, double bearingRad)
    {
        var r = 6371.0;
        var lat1 = lat * Math.PI / 180;
        var lng1 = lng * Math.PI / 180;
        var ang = km / r;
        var lat2 = Math.Asin(Math.Sin(lat1) * Math.Cos(ang) + Math.Cos(lat1) * Math.Sin(ang) * Math.Cos(bearingRad));
        var lng2 = lng1 + Math.Atan2(
            Math.Sin(bearingRad) * Math.Sin(ang) * Math.Cos(lat1),
            Math.Cos(ang) - Math.Sin(lat1) * Math.Sin(lat2));
        return (lat2 * 180 / Math.PI, lng2 * 180 / Math.PI);
    }

    private static int TravelMinutes(double oLat, double oLng, double lat, double lng, string transport)
    {
        var km = HaversineKm(oLat, oLng, lat, lng);
        var speed = transport.Contains("Auto", StringComparison.OrdinalIgnoreCase) ? 80 / 1.35
            : transport.Contains("OV", StringComparison.OrdinalIgnoreCase) ? 25 / 1.5
            : transport.Contains("Loop", StringComparison.OrdinalIgnoreCase) ? 5 / 1.4
            : 18 / 1.7;
        return Math.Max(1, (int)Math.Ceiling(km / speed * 60));
    }

    private static double HaversineKm(double lat1, double lng1, double lat2, double lng2)
    {
        const double r = 6371;
        var dLat = (lat2 - lat1) * Math.PI / 180;
        var dLng = (lng2 - lng1) * Math.PI / 180;
        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2)
                + Math.Cos(lat1 * Math.PI / 180) * Math.Cos(lat2 * Math.PI / 180)
                * Math.Sin(dLng / 2) * Math.Sin(dLng / 2);
        return r * 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
    }

    private static Dictionary<string, string> ParseQuery(string pathAndQuery)
    {
        var qIdx = pathAndQuery.IndexOf('?');
        if (qIdx < 0)
        {
            return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }

        var parsed = QueryHelpers.ParseQuery(pathAndQuery[qIdx..]);
        var dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var pair in parsed)
        {
            dict[pair.Key] = pair.Value.ToString();
        }

        return dict;
    }

    private static double? TryDouble(IReadOnlyDictionary<string, string> q, string key)
    {
        if (!q.TryGetValue(key, out var raw) || string.IsNullOrWhiteSpace(raw))
        {
            return null;
        }

        return double.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out var v) ? v : null;
    }

    private static Guid? TryParseVacancyId(string path)
    {
        foreach (var segment in path.Split('/', StringSplitOptions.RemoveEmptyEntries))
        {
            if (Guid.TryParse(segment, out var id))
            {
                return id;
            }
        }

        return null;
    }

    private static string Serialize<T>(T value) => JsonSerializer.Serialize(value, Json);

    private sealed record SeedVacancy(
        int Index,
        Guid Id,
        string Title,
        string Company,
        string? OfferedBy,
        string Place,
        double Lat,
        double Lng,
        string? Image,
        string WorkType,
        string CategoryName,
        string Colour,
        string FitWhy,
        bool IsIntermediary);

    private sealed class VacancyCardStub
    {
        public Guid Id { get; init; }
        public string Title { get; init; } = "";
        public string CompanyName { get; init; } = "";
        public string? OfferedByLabel { get; init; }
        public string Place { get; init; } = "";
        public string? ThumbnailUrl { get; init; }
        public string? LogoUrl { get; init; }
        public decimal? HourlyWage { get; init; }
        public bool WageVisible { get; init; }
        public string[] WorkTypes { get; init; } = [];
        public bool IsHighlighted { get; init; }
        public int? TravelMinutes { get; init; }
        public int? MatchPercent { get; init; }
        public string? MatchColorBand { get; init; }
        public string? CategoryColorHex { get; init; }
        public string? CompanyAddress { get; init; }
        public string? FitWhyLine { get; init; }
    }
}

using System.Text.Json;
using System.Text.Json.Serialization;
using Jobsy.Web.Models;

namespace Jobsy.Tests;

/// <summary>Fake banenkaart catalog for illustrated page Playwright captures (Delft / Zaandam area).</summary>
internal static class IllustratedPagesVacancyStub
{
    private static readonly JsonSerializerOptions Json = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    private static readonly Guid CompanyId = Guid.Parse("a1000000-0000-0000-0000-000000000001");

    private static readonly (Guid Id, string Title, string Company, string City, double Lat, double Lng, string? Image, string WorkType, string FitWhy)[] Seeds =
    [
        (Guid.Parse("b1000001-0000-0000-0000-000000000001"), "Zorgmedewerker weekend", "Care & Co", "Delft", 52.0128, 4.3612, "/images/vacancies/zorg.webp", "Parttime", "mensen helpen"),
        (Guid.Parse("b1000002-0000-0000-0000-000000000002"), "Orderpicker ochtend", "LogiZaand", "Zaandam", 52.4412, 4.8298, "/images/vacancies/logistiek.webp", "Parttime", "praktisch werken"),
        (Guid.Parse("b1000003-0000-0000-0000-000000000003"), "Horeca allrounder", "De Brasserie", "Delft", 52.0095, 4.3521, "/images/vacancies/horeca.webp", "Bijbaan", "in het contact"),
        (Guid.Parse("b1000004-0000-0000-0000-000000000004"), "Magazijnmedewerker", "Zaans Pakhuis", "Zaandam", 52.4358, 4.8185, "/images/vacancies/productie.webp", "Fulltime", "overzicht houden"),
        (Guid.Parse("b1000005-0000-0000-0000-000000000005"), "Tuinbouw assistent", "Groen Delft", "Delft", 52.0145, 4.3688, "/images/vacancies/tuinbouw.webp", "Parttime", "buiten werken"),
        (Guid.Parse("b1000006-0000-0000-0000-000000000006"), "Winkelmedewerker", "Mode Zaandam", "Zaandam", 52.4395, 4.8220, "/images/vacancies/winkel.webp", "Bijbaan", "klanten helpen"),
        (Guid.Parse("b1000007-0000-0000-0000-000000000007"), "Kantoor ondersteuning", "Delft Office", "Delft", 52.0082, 4.3495, "/images/vacancies/kantoor.webp", "Parttime", "gestructureerd werken"),
        (Guid.Parse("b1000008-0000-0000-0000-000000000008"), "Schoonmaak avond", "Schoon Zaans", "Zaandam", 52.4370, 4.8310, null, "Parttime", "netjes achterlaten"),
    ];

    public static string? ThumbnailForImageRequest(string path)
    {
        var id = TryParseVacancyId(path);
        if (id is null)
        {
            return null;
        }

        var seed = Seeds.FirstOrDefault(s => s.Id == id);
        return seed.Image;
    }

    public static string JsonForPath(string path)
    {
        if (path.Contains("/isochrones", StringComparison.OrdinalIgnoreCase))
        {
            return """{"type":"FeatureCollection","features":[]}""";
        }

        if (path.Contains("/discover", StringComparison.OrdinalIgnoreCase))
        {
            if (path.Contains("originLat=", StringComparison.OrdinalIgnoreCase))
            {
                return Serialize(new VacancyListPage
                {
                    Items = [],
                    TotalCount = 0,
                    HasMore = false
                });
            }

            return Serialize(BuildDiscoverPage());
        }

        if (path.Contains("/map-view", StringComparison.OrdinalIgnoreCase))
        {
            return Serialize(new VacancyMapViewResponse
            {
                Lat = 52.22,
                Lng = 4.59,
                Zoom = 9.2,
                PinCount = Seeds.Length
            });
        }

        if (path.Contains("/banenkaart-catalog", StringComparison.OrdinalIgnoreCase))
        {
            return BuildCatalogColumnarJson();
        }

        if (path.Contains("/pins", StringComparison.OrdinalIgnoreCase))
        {
            return Serialize(BuildPins());
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

    private static VacancyListPage BuildDiscoverPage()
    {
        var start = DateOnly.FromDateTime(DateTime.UtcNow.Date);
        var end = start.AddMonths(2);
        var items = Seeds.Select(s => new VacancyListItem
        {
            Id = s.Id,
            Title = s.Title,
            Description = "Illustrated screenshot vacature.",
            HourlyWage = 14.50m,
            WageVisible = true,
            StartDate = start,
            EndDate = end,
            Status = "Active",
            CompanyId = CompanyId,
            CompanyName = s.Company,
            CompanyAddress = s.City,
            ImageUrl = s.Image,
            Latitude = s.Lat,
            Longitude = s.Lng,
            RequiredTransport = ["Fiets", "OV"],
            TravelMinutes = 18,
            DistanceKm = 4.2,
            WorkTypes = [s.WorkType],
            CategoryColorHex = "#3366cc",
            MatchPercent = 78,
            FitPercent = 78,
            FitBand = "Good",
            MatchColorBand = "good",
            FitWhyLine = s.FitWhy,
            FitWhyKinds = ["interest"]
        }).ToList();

        return new VacancyListPage
        {
            Items = items,
            Skip = 0,
            Take = items.Count,
            TotalCount = items.Count,
            HasMore = false
        };
    }

    private static string BuildCatalogColumnarJson()
    {
        var ids = Seeds.Select(s => s.Id.ToString("D")).ToArray();
        var lats = Seeds.Select(s => s.Lat).ToArray();
        var lngs = Seeds.Select(s => s.Lng).ToArray();
        var titles = Seeds.Select(s => s.Title).ToArray();
        var companies = Seeds.Select(s => s.Company).ToArray();
        return $$"""
            {"v":1,"n":{{Seeds.Length}},"id":{{JsonSerializer.Serialize(ids)}},"lat":{{JsonSerializer.Serialize(lats)}},"lng":{{JsonSerializer.Serialize(lngs)}},"t":{{JsonSerializer.Serialize(titles)}},"c":{{JsonSerializer.Serialize(companies)}},"ob":{{JsonSerializer.Serialize(Enumerable.Repeat("", Seeds.Length).ToArray())}},"w":{{JsonSerializer.Serialize(Enumerable.Repeat(1450, Seeds.Length).ToArray())}},"h0":{{JsonSerializer.Serialize(Enumerable.Repeat(16, Seeds.Length).ToArray())}},"h1":{{JsonSerializer.Serialize(Enumerable.Repeat(32, Seeds.Length).ToArray())}},"cat":{{JsonSerializer.Serialize(Enumerable.Repeat("", Seeds.Length).ToArray())}},"co":{{JsonSerializer.Serialize(Enumerable.Repeat(CompanyId.ToString("D"), Seeds.Length).ToArray())}},"wt":{{JsonSerializer.Serialize(Enumerable.Repeat(1, Seeds.Length).ToArray())}},"tr":{{JsonSerializer.Serialize(Enumerable.Repeat(1, Seeds.Length).ToArray())}},"col":{{JsonSerializer.Serialize(Enumerable.Repeat("#3366cc", Seeds.Length).ToArray())}},"s65":{{JsonSerializer.Serialize(Enumerable.Repeat(0, Seeds.Length).ToArray())}},"pm":{{JsonSerializer.Serialize(Enumerable.Repeat(0, Seeds.Length).ToArray())}},"iu":{{JsonSerializer.Serialize(Enumerable.Repeat("", Seeds.Length).ToArray())}}}
            """;
    }

    private static List<VacancyPinBootItem> BuildPins()
        => Seeds.Select(s => new VacancyPinBootItem
        {
            Id = s.Id,
            Lat = s.Lat,
            Lng = s.Lng,
            Colour = "#3366cc",
            MatchPercent = 78,
            WorkType = s.WorkType,
            MatchColorBand = "good"
        }).ToList();

    private static List<VacancyCardStub> BuildCards()
        => Seeds.Select(s => new VacancyCardStub
        {
            Id = s.Id,
            Title = s.Title,
            CompanyName = s.Company,
            Place = s.City,
            ThumbnailUrl = s.Image,
            HourlyWage = 14.50m,
            WageVisible = true,
            WorkTypes = [s.WorkType],
            TravelMinutes = 18,
            MatchPercent = 78,
            MatchColorBand = "good",
            CategoryColorHex = "#3366cc",
            CompanyAddress = s.City,
            FitWhyLine = s.FitWhy
        }).ToList();

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

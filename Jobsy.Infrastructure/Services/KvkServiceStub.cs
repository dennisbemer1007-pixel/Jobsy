using System.Text.RegularExpressions;
using Jobsy.Core.Interfaces;
using Jobsy.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Jobsy.Infrastructure.Services;

/// <summary>
/// Demo KvK-handelsregister zonder live API. Gebruik de catalogus-nummers in Admin/Branches.
/// </summary>
public sealed class KvkServiceStub : IKvkService
{
    private readonly JobsyDbContext _db;

    /// <summary>Juridische handelsnaam per KVK-nummer (hoofdvestiging / org).</summary>
    private static readonly IReadOnlyDictionary<string, string> LegalNames =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["12345678"] = "Westland Fresh Logistics B.V.",
            ["87654321"] = "Boutique Café De Stad B.V.",
            ["11223344"] = "Supermarkt De Fred B.V.",
            ["55667788"] = "Demo Intermediair Flex B.V.",
            ["33445566"] = "Zorggroep Duinzicht B.V.",
            ["44556677"] = "Bouwbedrijf Van der Plas B.V.",
            ["66778899"] = "Horeca Groep Scheveningen B.V.",
            ["77889900"] = "TechHub Den Haag B.V.",
            ["88990011"] = "Bloemenveiling Westland Coöperatie U.A.",
            ["99001122"] = "Transport & Koel BV"
        };

    /// <summary>Primary SBI codes per KVK (78* = uitzend/arbeidsbemiddeling).</summary>
    private static readonly IReadOnlyDictionary<string, string[]> SbiCodesByKvk =
        new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
        {
            ["12345678"] = ["5229"], // Overige dienstverlening voor vervoer
            ["87654321"] = ["5610"], // Restaurants
            ["11223344"] = ["4711"], // Supermarkten
            ["55667788"] = ["7820"], // Uitzendbureaus (SBI 78 → Intermediair)
            ["33445566"] = ["8710"], // Verpleeghuizen
            ["44556677"] = ["4120"], // Algemene burgerlijke en utiliteitsbouw
            ["66778899"] = ["5610"], // Restaurants
            ["77889900"] = ["6201"], // Ontwikkelen van software
            ["88990011"] = ["4622"], // Groothandel in bloemen en planten
            ["99001122"] = ["4941"]  // Goederenvervoer over de weg
        };

    private static readonly IReadOnlyList<KvkEstablishmentResult> Catalog =
    [
        // Westland Fresh Logistics — multi-site werkgever (bestaand demo)
        new("12345678", "0001", "12345678_0001", "Westland Fresh Logistics HQ",
            "'s-Gravenzandseweg 10, Honselersdijk", 51.9812, 4.2235, false),
        new("12345678", "0002", "12345678_0002", "Westland Fresh — Naaldwijk",
            "Dijkweg 2, Naaldwijk", 51.9930, 4.2080, false),
        new("12345678", "0003", "12345678_0003", "Westland Fresh — Poeldijk",
            "Wateringseweg 44, Poeldijk", 52.0215, 4.2200, false),
        new("12345678", "0004", "12345678_0004", "Westland Fresh — Kwintsheul",
            "Herculesweg 8, Kwintsheul", 52.0050, 4.2450, false),

        // Boutique Café De Stad
        new("87654321", "0001", "87654321_0001", "Boutique Café De Stad",
            "Grote Markt 14, Den Haag Centrum", 52.0735, 4.3120, false),
        new("87654321", "0002", "87654321_0002", "Boutique Café — Spuiplein",
            "Spuiplein 150, Den Haag", 52.0770, 4.3175, false),

        // Supermarkt De Fred — keten
        new("11223344", "0001", "11223344_0001", "Supermarkt De Fred — Statenkwartier",
            "Frederik Hendriklaan 88, Den Haag", 52.0910, 4.2815, false),
        new("11223344", "0002", "11223344_0002", "Supermarkt De Fred — Scheveningen",
            "Keizerstraat 12, Scheveningen", 52.1045, 4.2750, false),
        new("11223344", "0003", "11223344_0003", "Supermarkt De Fred — Ypenburg",
            "Laan van Ypenburg 120, Den Haag", 52.0405, 4.3700, false),
        new("11223344", "0004", "11223344_0004", "Supermarkt De Fred — Delft",
            "Brabanstraat 5, Delft", 51.9990, 4.3590, false),

        // Demo Intermediair — meerdere vestigingen (SBI 7820)
        new("55667788", "0001", "55667788_0001", "Demo Intermediair Flex — Binckhorst",
            "Binckhorstlaan 36, Den Haag", 52.0680, 4.3350, false),
        new("55667788", "0002", "55667788_0002", "Demo Intermediair Flex — Rotterdam",
            "Coolsingel 105, Rotterdam", 51.9210, 4.4790, false),
        new("55667788", "0003", "55667788_0003", "Demo Intermediair Flex — Leiden",
            "Stationsplein 1, Leiden", 52.1660, 4.4820, false),

        // Zorggroep Duinzicht
        new("33445566", "0001", "33445566_0001", "Zorggroep Duinzicht — Hoofdlocatie",
            "Sportlaan 600, Den Haag", 52.0900, 4.2800, false),
        new("33445566", "0002", "33445566_0002", "Zorggroep Duinzicht — Loosduinen",
            "Loosduinseweg 700, Den Haag", 52.0550, 4.2450, false),
        new("33445566", "0003", "33445566_0003", "Zorggroep Duinzicht — Wassenaar",
            "Van Oldenbarneveltlaan 20, Wassenaar", 52.1450, 4.4000, false),

        // Bouwbedrijf Van der Plas
        new("44556677", "0001", "44556677_0001", "Bouwbedrijf Van der Plas — Kantoor",
            "Vlietweg 12, Rijswijk", 52.0370, 4.3250, false),
        new("44556677", "0002", "44556677_0002", "Bouwbedrijf Van der Plas — Magazijn",
            "Industrieweg 88, Wateringen", 52.0250, 4.2750, false),

        // Horeca Groep Scheveningen
        new("66778899", "0001", "66778899_0001", "Strandpaviljoen Noord",
            "Strandweg 1, Scheveningen", 52.1130, 4.2800, false),
        new("66778899", "0002", "66778899_0002", "Strandpaviljoen Zuid",
            "Strandweg 80, Scheveningen", 52.1010, 4.2680, false),
        new("66778899", "0003", "66778899_0003", "Brasserie Kurhaus",
            "Gevers Deynootplein 30, Scheveningen", 52.1125, 4.2825, false),

        // TechHub Den Haag — single site
        new("77889900", "0001", "77889900_0001", "TechHub Den Haag",
            "Wilhelmina van Pruisenweg 104, Den Haag", 52.0685, 4.3400, false),

        // Bloemenveiling Westland
        new("88990011", "0001", "88990011_0001", "Bloemenveiling Westland — Veiling",
            "Middel Broekweg 29, Honselersdijk", 51.9950, 4.2250, false),
        new("88990011", "0002", "88990011_0002", "Bloemenveiling Westland — Logistiek",
            "ABC Westland 555, Poeldijk", 52.0180, 4.2150, false),

        // Transport & Koel
        new("99001122", "0001", "99001122_0001", "Transport & Koel — Depot Westland",
            "Nieuw Oranjekanaal 10, 's-Gravenzande", 51.9770, 4.1650, false),
        new("99001122", "0002", "99001122_0002", "Transport & Koel — Depot Rotterdam",
            "Waalhaven Z.z. 20, Rotterdam", 51.8800, 4.4500, false)
    ];

    public KvkServiceStub(JobsyDbContext db)
    {
        _db = db;
    }

    /// <summary>Bekende demo-KVK-nummers (voor UI-hints).</summary>
    public static IReadOnlyList<string> DemoKvkNumbers { get; } =
        LegalNames.Keys.OrderBy(k => k, StringComparer.Ordinal).ToList();

    public Task<KvkCompanyResult?> GetByKvkNumberAsync(
        string kvkNumber,
        CancellationToken cancellationToken = default)
    {
        var normalized = NormalizeKvkNumber(kvkNumber);
        if (normalized is null)
        {
            return Task.FromResult<KvkCompanyResult?>(null);
        }

        var match = Catalog.FirstOrDefault(c =>
            c.KvkNumber.Equals(normalized, StringComparison.OrdinalIgnoreCase));
        if (match is null)
        {
            return Task.FromResult<KvkCompanyResult?>(null);
        }

        var legalName = LegalNames.TryGetValue(normalized, out var name)
            ? name
            : StripBranchSuffix(match.Name);

        return Task.FromResult<KvkCompanyResult?>(
            new KvkCompanyResult(normalized, legalName, match.Address, SbiCodesFor(normalized)));
    }

    public async Task<IReadOnlyList<KvkEstablishmentResult>> GetEstablishmentsAsync(
        string kvkNumber,
        CancellationToken cancellationToken = default)
    {
        var normalized = NormalizeKvkNumber(kvkNumber);
        if (normalized is null)
        {
            return Array.Empty<KvkEstablishmentResult>();
        }

        var inUse = await _db.Companies
            .AsNoTracking()
            .Where(c => c.KvkNumber == normalized && c.KvkEstablishmentId != null)
            .Select(c => c.KvkEstablishmentId!)
            .ToListAsync(cancellationToken);

        var sbi = SbiCodesFor(normalized);
        return Catalog
            .Where(c => c.KvkNumber.Equals(normalized, StringComparison.OrdinalIgnoreCase))
            .Select(c => c with { IsInUse = inUse.Contains(c.KvkEstablishmentId), SbiCodes = sbi })
            .OrderBy(c => c.EstablishmentNumber, StringComparer.Ordinal)
            .ToList();
    }

    /// <summary>Accepteert spaties/streepjes; verwacht 8 cijfers.</summary>
    internal static string? NormalizeKvkNumber(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return null;
        }

        var digits = Regex.Replace(raw.Trim(), @"\D", string.Empty);
        return digits.Length == 8 ? digits : null;
    }

    private static string[] SbiCodesFor(string kvkNumber)
        => SbiCodesByKvk.TryGetValue(kvkNumber, out var codes) ? codes : [];

    private static string StripBranchSuffix(string name)
    {
        var parts = name.Split(['—', '-'], 2, StringSplitOptions.TrimEntries);
        return parts[0];
    }
}

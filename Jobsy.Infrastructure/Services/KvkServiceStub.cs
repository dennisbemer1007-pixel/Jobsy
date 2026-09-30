using Jobsy.Core.Enums;
using Jobsy.Core.Interfaces;
using Jobsy.Core.Rules;
using Jobsy.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Jobsy.Infrastructure.Services;

/// <summary>
/// Demo KvK-handelsregister zonder live API. Gebruik de catalogus-nummers in Admin/Branches.
/// </summary>
public sealed class KvkServiceStub : IKvkService
{
    /// <summary>Zeist vestiging of Groen &amp; Zorg — permanently "in use" for access-request demos (07).</summary>
    public const string ForcedInUseEstablishmentId = "90123456_000045678923";

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
            ["77889900"] = "Groenzorg Hoveniers V.O.F.",
            ["88990011"] = "Bloemenveiling Westland Coöperatie U.A.",
            ["99001122"] = "Transport & Koel BV",
            ["90123456"] = "Groen & Zorg Thuiszorg B.V.",
            ["81234567"] = "Stichting Groen en Zorgboerderij De Hoef",
            ["66554433"] = "Zorgschoon Groen B.V."
        };

    private static readonly IReadOnlyDictionary<string, string> LegalForms =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["12345678"] = "Besloten Vennootschap",
            ["87654321"] = "Besloten Vennootschap",
            ["11223344"] = "Besloten Vennootschap",
            ["55667788"] = "Besloten Vennootschap",
            ["33445566"] = "Besloten Vennootschap",
            ["44556677"] = "Besloten Vennootschap",
            ["66778899"] = "Besloten Vennootschap",
            ["77889900"] = "Vennootschap Onder Firma",
            ["88990011"] = "Coöperatie",
            ["99001122"] = "Besloten Vennootschap",
            ["90123456"] = "Besloten Vennootschap",
            ["81234567"] = "Stichting",
            ["66554433"] = "Besloten Vennootschap"
        };

    /// <summary>Primary SBI codes per KVK (78* = uitzend/arbeidsbemiddeling).</summary>
    private static readonly IReadOnlyDictionary<string, string[]> SbiCodesByKvk =
        new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
        {
            ["12345678"] = ["5229"],
            ["87654321"] = ["5610"],
            ["11223344"] = ["4711"],
            ["55667788"] = ["7820"],
            ["33445566"] = ["8710"],
            ["44556677"] = ["4120"],
            ["66778899"] = ["5610"],
            ["77889900"] = ["8130"],
            ["88990011"] = ["4622"],
            ["99001122"] = ["4941"],
            ["90123456"] = ["88101", "88102"],
            ["81234567"] = ["8899"],
            ["66554433"] = ["81210"]
        };

    private static readonly IReadOnlyDictionary<string, string[]> WebsitesByKvk =
        new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
        {
            ["90123456"] = ["groenenzorg.nl"]
        };

    private static readonly IReadOnlyList<StubEstablishment> Catalog =
    [
        // Westland Fresh Logistics — multi-site werkgever (bestaand demo)
        new("12345678", "0001", "Westland Fresh Logistics HQ",
            "'s-Gravenzandseweg 10, Honselersdijk", "Honselersdijk", 51.9812, 4.2235),
        new("12345678", "0002", "Westland Fresh — Naaldwijk",
            "Dijkweg 2, Naaldwijk", "Naaldwijk", 51.9930, 4.2080),
        new("12345678", "0003", "Westland Fresh — Poeldijk",
            "Wateringseweg 44, Poeldijk", "Poeldijk", 52.0215, 4.2200),
        new("12345678", "0004", "Westland Fresh — Kwintsheul",
            "Herculesweg 8, Kwintsheul", "Kwintsheul", 52.0050, 4.2450),

        new("87654321", "0001", "Boutique Café De Stad",
            "Grote Markt 14, Den Haag Centrum", "Den Haag", 52.0735, 4.3120),
        new("87654321", "0002", "Boutique Café — Spuiplein",
            "Spuiplein 150, Den Haag", "Den Haag", 52.0770, 4.3175),

        new("11223344", "0001", "Supermarkt De Fred — Statenkwartier",
            "Frederik Hendriklaan 88, Den Haag", "Den Haag", 52.0910, 4.2815),
        new("11223344", "0002", "Supermarkt De Fred — Scheveningen",
            "Keizerstraat 12, Scheveningen", "Scheveningen", 52.1045, 4.2750),
        new("11223344", "0003", "Supermarkt De Fred — Ypenburg",
            "Laan van Ypenburg 120, Den Haag", "Den Haag", 52.0405, 4.3700),
        new("11223344", "0004", "Supermarkt De Fred — Delft",
            "Brabanstraat 5, Delft", "Delft", 51.9990, 4.3590),

        new("55667788", "0001", "Demo Intermediair Flex — Binckhorst",
            "Binckhorstlaan 36, Den Haag", "Den Haag", 52.0680, 4.3350),
        new("55667788", "0002", "Demo Intermediair Flex — Rotterdam",
            "Coolsingel 105, Rotterdam", "Rotterdam", 51.9210, 4.4790),
        new("55667788", "0003", "Demo Intermediair Flex — Leiden",
            "Stationsplein 1, Leiden", "Leiden", 52.1660, 4.4820),

        new("33445566", "0001", "Zorggroep Duinzicht — Hoofdlocatie",
            "Sportlaan 600, Den Haag", "Den Haag", 52.0900, 4.2800),
        new("33445566", "0002", "Zorggroep Duinzicht — Loosduinen",
            "Loosduinseweg 700, Den Haag", "Den Haag", 52.0550, 4.2450),
        new("33445566", "0003", "Zorggroep Duinzicht — Wassenaar",
            "Van Oldenbarneveltlaan 20, Wassenaar", "Wassenaar", 52.1450, 4.4000),

        new("44556677", "0001", "Bouwbedrijf Van der Plas — Kantoor",
            "Vlietweg 12, Rijswijk", "Rijswijk", 52.0370, 4.3250),
        new("44556677", "0002", "Bouwbedrijf Van der Plas — Magazijn",
            "Industrieweg 88, Wateringen", "Wateringen", 52.0250, 4.2750),

        new("66778899", "0001", "Strandpaviljoen Noord",
            "Strandweg 1, Scheveningen", "Scheveningen", 52.1130, 4.2800),
        new("66778899", "0002", "Strandpaviljoen Zuid",
            "Strandweg 80, Scheveningen", "Scheveningen", 52.1010, 4.2680),
        new("66778899", "0003", "Brasserie Kurhaus",
            "Gevers Deynootplein 30, Scheveningen", "Scheveningen", 52.1125, 4.2825),

        // Groenzorg Hoveniers (was TechHub demo number — reused for wr-d2 name search)
        new("77889900", "0001", "Groenzorg Hoveniers V.O.F.",
            "Parklaan 3, Nieuwegein", "Nieuwegein", 52.0280, 5.0910),

        new("88990011", "0001", "Bloemenveiling Westland — Veiling",
            "Middel Broekweg 29, Honselersdijk", "Honselersdijk", 51.9950, 4.2250),
        new("88990011", "0002", "Bloemenveiling Westland — Logistiek",
            "ABC Westland 555, Poeldijk", "Poeldijk", 52.0180, 4.2150),

        new("99001122", "0001", "Transport & Koel — Depot Westland",
            "Nieuw Oranjekanaal 10, 's-Gravenzande", "'s-Gravenzande", 51.9770, 4.1650),
        new("99001122", "0002", "Transport & Koel — Depot Rotterdam",
            "Waalhaven Z.z. 20, Rotterdam", "Rotterdam", 51.8800, 4.4500),

        // Groen & Zorg Thuiszorg (wizard voorbeelddata)
        new("90123456", "000045678901", "Hoofdvestiging Utrecht",
            "Vondellaan 12, 3521 GE Utrecht", "Utrecht", 52.0780, 5.1250,
            Visiting: new KvkAddressLine("Vondellaan", "12", null, "3521GE", "Utrecht"),
            Postal: new KvkAddressLine("Postbus", "100", null, "3500AA", "Utrecht"),
            Type: "Hoofdvestiging"),
        new("90123456", "000045678912", "Amersfoort",
            "Stationsplein 4, 3818 LE Amersfoort", "Amersfoort", 52.1530, 5.3730,
            Visiting: new KvkAddressLine("Stationsplein", "4", null, "3818LE", "Amersfoort"),
            Type: "Nevenvestiging"),
        new("90123456", "000045678923", "Zeist",
            "Slotlaan 88, 3701 GP Zeist", "Zeist", 52.0880, 5.2320,
            Visiting: new KvkAddressLine("Slotlaan", "88", null, "3701GP", "Zeist"),
            Type: "Nevenvestiging",
            ForceInUse: true),

        new("81234567", "0001", "Stichting Groen en Zorgboerderij De Hoef",
            "Hoefseweg 2, Houten", "Houten", 52.0285, 5.1680,
            Type: "Rechtspersoon"),

        new("66554433", "0001", "Zorgschoon Groen — Utrecht",
            "Croeselaan 15, Utrecht", "Utrecht", 52.0850, 5.1120),
        new("66554433", "0002", "Zorgschoon Groen — Nieuwegein",
            "Passage 8, Nieuwegein", "Nieuwegein", 52.0295, 5.0800)
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
        var normalized = CompanyPublicPaths.NormalizeKvkNumber(kvkNumber);
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
        var lookup = await LookupEstablishmentsAsync(kvkNumber, cancellationToken);
        if (lookup.Status == KvkLookupStatus.Unavailable)
        {
            throw new Jobsy.Core.Exceptions.KvkServiceUnavailableException(lookup.Message ?? "KVK unavailable");
        }

        return lookup.Establishments;
    }

    public async Task<KvkEstablishmentsLookup> LookupEstablishmentsAsync(
        string kvkNumber,
        CancellationToken cancellationToken = default)
    {
        if (string.Equals(
                Environment.GetEnvironmentVariable("JOBSY_KVK_FORCE_UNAVAILABLE"),
                "1",
                StringComparison.Ordinal))
        {
            return KvkEstablishmentsLookup.Unavailable();
        }

        var normalized = CompanyPublicPaths.NormalizeKvkNumber(kvkNumber);
        if (normalized is null)
        {
            return KvkEstablishmentsLookup.NotFound();
        }

        var inUse = await LoadInUseIdsAsync(normalized, cancellationToken);
        var sbi = SbiCodesFor(normalized);
        var items = Catalog
            .Where(c => c.KvkNumber.Equals(normalized, StringComparison.OrdinalIgnoreCase))
            .Select(c => ToEstablishmentResult(c, inUse, sbi))
            .OrderBy(c => c.EstablishmentNumber, StringComparer.Ordinal)
            .ToList();

        return items.Count == 0
            ? KvkEstablishmentsLookup.NotFound()
            : KvkEstablishmentsLookup.Ok(items);
    }

    public async Task<KvkSearchResult> SearchAsync(
        KvkSearchQuery query,
        CancellationToken cancellationToken = default)
    {
        if (string.Equals(
                Environment.GetEnvironmentVariable("JOBSY_KVK_FORCE_UNAVAILABLE"),
                "1",
                StringComparison.Ordinal))
        {
            return KvkSearchResult.Unavailable();
        }

        var text = (query.Text ?? string.Empty).Trim();
        var place = string.IsNullOrWhiteSpace(query.Place) ? null : query.Place.Trim();
        var page = query.Page < 1 ? 1 : query.Page;
        var digits = new string(text.Where(char.IsDigit).ToArray());
        var isNumberSearch = digits.Length == 8
                             && text.All(c => char.IsDigit(c) || char.IsWhiteSpace(c) || c is '.' or '-');

        if (!isNumberSearch && text.Length < 3)
        {
            throw new ArgumentException("Zoekterm moet minimaal 3 tekens zijn.", nameof(query));
        }

        IEnumerable<IGrouping<string, StubEstablishment>> groups;
        if (isNumberSearch)
        {
            groups = Catalog
                .Where(c => c.KvkNumber.Equals(digits, StringComparison.Ordinal))
                .GroupBy(c => c.KvkNumber, StringComparer.Ordinal);
        }
        else
        {
            var tokens = NormalizeSearch(text)
                .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            groups = Catalog
                .Where(c =>
                {
                    var name = LegalNames.TryGetValue(c.KvkNumber, out var legal) ? legal : c.Name;
                    var hay = NormalizeSearch(name + " " + c.Name);
                    // All query tokens must appear (so "groen en zorg" matches Groenzorg / Zorgschoon Groen).
                    return tokens.All(t => t is "en" or "de" or "het" or "van"
                        || hay.Contains(t, StringComparison.Ordinal));
                })
                .GroupBy(c => c.KvkNumber, StringComparer.Ordinal);
        }

        // Prefer HQ place; group to one row per KvK as in wr-d2.
        var allHits = new List<KvkSearchHit>();
        foreach (var group in groups)
        {
            var kvk = group.Key;
            var legal = LegalNames.TryGetValue(kvk, out var n) ? n : group.First().Name;
            var hq = group.FirstOrDefault(g => g.Type == "Hoofdvestiging") ?? group.First();
            // Soft place preference: keep all name hits; boost matching plaats to the top.
            var type = group.Count() > 1
                ? "Hoofdvestiging"
                : (hq.Type ?? "Rechtspersoon");
            var displayPlace = place is not null
                && group.Any(g => g.Place.Contains(place, StringComparison.OrdinalIgnoreCase))
                ? group.First(g => g.Place.Contains(place, StringComparison.OrdinalIgnoreCase)).Place
                : hq.Place;
            allHits.Add(new KvkSearchHit(
                kvk,
                legal,
                displayPlace,
                type,
                group.Count(),
                false));
        }

        // Sort: Utrecht matches first when place filter, then by name.
        allHits = allHits
            .OrderByDescending(h => place is not null
                && h.Place.Contains(place, StringComparison.OrdinalIgnoreCase))
            .ThenBy(h => h.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();

        var total = allHits.Count;
        var pageHits = allHits.Skip((page - 1) * 10).Take(10).ToList();
        var onLobsy = await LoadOnLobsyAsync(pageHits.Select(h => h.KvkNumber), cancellationToken);
        var marked = pageHits
            .Select(h => h with { IsOnLobsy = onLobsy.Contains(h.KvkNumber) })
            .ToList();

        return KvkSearchResult.Ok(marked, total);
    }

    public async Task<KvkCompanyProfile> GetProfileAsync(
        string kvkNumber,
        CancellationToken cancellationToken = default)
    {
        if (string.Equals(
                Environment.GetEnvironmentVariable("JOBSY_KVK_FORCE_UNAVAILABLE"),
                "1",
                StringComparison.Ordinal))
        {
            return KvkCompanyProfile.Unavailable(kvkNumber ?? "");
        }

        var normalized = CompanyPublicPaths.NormalizeKvkNumber(kvkNumber);
        if (normalized is null)
        {
            return KvkCompanyProfile.NotFound(kvkNumber ?? "");
        }

        var rows = Catalog
            .Where(c => c.KvkNumber.Equals(normalized, StringComparison.OrdinalIgnoreCase))
            .OrderBy(c => c.EstablishmentNumber, StringComparer.Ordinal)
            .ToList();
        if (rows.Count == 0)
        {
            return KvkCompanyProfile.NotFound(normalized);
        }

        var inUse = await LoadInUseIdsAsync(normalized, cancellationToken);
        var sbi = SbiCodesFor(normalized);
        var legal = LegalNames.TryGetValue(normalized, out var name) ? name : rows[0].Name;
        var websites = WebsitesByKvk.TryGetValue(normalized, out var sites) ? sites : [];
        var legalForm = LegalForms.TryGetValue(normalized, out var form) ? form : null;
        var establishments = rows
            .Select(r => ToEstablishmentProfile(r, inUse, sbi))
            .ToList();

        return new KvkCompanyProfile(
            KvkLookupStatus.Ok,
            normalized,
            legal,
            rows[0].Address,
            legalForm,
            sbi,
            websites,
            establishments);
    }

    private async Task<HashSet<string>> LoadInUseIdsAsync(string kvkNumber, CancellationToken cancellationToken)
    {
        var managedCompanyIds = await CompanyOccupancy.LoadManagedCompanyIdsAsync(_db, cancellationToken);

        var fromDb = managedCompanyIds.Count == 0
            ? new List<string>()
            : await _db.Companies
                .AsNoTracking()
                .Where(c => c.KvkNumber == kvkNumber
                            && c.KvkEstablishmentId != null
                            && managedCompanyIds.Contains(c.Id))
                .Select(c => c.KvkEstablishmentId!)
                .ToListAsync(cancellationToken);

        var set = new HashSet<string>(fromDb, StringComparer.OrdinalIgnoreCase) { ForcedInUseEstablishmentId };
        return set;
    }

    private async Task<HashSet<string>> LoadOnLobsyAsync(
        IEnumerable<string> kvkNumbers,
        CancellationToken cancellationToken)
    {
        var list = kvkNumbers.Distinct(StringComparer.Ordinal).ToList();
        if (list.Count == 0)
        {
            return new HashSet<string>(StringComparer.Ordinal);
        }

        var managed = await CompanyOccupancy.LoadManagedCompanyIdsAsync(_db, cancellationToken);
        if (managed.Count == 0)
        {
            return new HashSet<string>(StringComparer.Ordinal);
        }

        var matches = await _db.Companies.AsNoTracking()
            .Where(c => list.Contains(c.KvkNumber) && managed.Contains(c.Id))
            .Select(c => c.KvkNumber)
            .Distinct()
            .ToListAsync(cancellationToken);

        return new HashSet<string>(matches, StringComparer.Ordinal);
    }

    private static KvkEstablishmentResult ToEstablishmentResult(
        StubEstablishment c,
        HashSet<string> inUse,
        IReadOnlyList<string> sbi)
    {
        var id = CompanyPublicPaths.BuildEstablishmentId(c.KvkNumber, c.EstablishmentNumber);
        return new KvkEstablishmentResult(
            c.KvkNumber,
            c.EstablishmentNumber,
            id,
            c.Name,
            c.Address,
            c.Latitude,
            c.Longitude,
            inUse.Contains(id) || c.ForceInUse,
            sbi);
    }

    private static KvkEstablishmentProfile ToEstablishmentProfile(
        StubEstablishment c,
        HashSet<string> inUse,
        IReadOnlyList<string> sbi)
    {
        var id = CompanyPublicPaths.BuildEstablishmentId(c.KvkNumber, c.EstablishmentNumber);
        return new KvkEstablishmentProfile(
            c.KvkNumber,
            c.EstablishmentNumber,
            id,
            c.Name,
            c.Address,
            c.Latitude,
            c.Longitude,
            inUse.Contains(id) || c.ForceInUse,
            sbi,
            c.Visiting,
            c.Postal);
    }

    private static string[] SbiCodesFor(string kvkNumber)
        => SbiCodesByKvk.TryGetValue(kvkNumber, out var codes) ? codes : [];

    private static string StripBranchSuffix(string name)
    {
        var parts = name.Split(['—', '-'], 2, StringSplitOptions.TrimEntries);
        return parts[0];
    }

    private static string NormalizeSearch(string value)
    {
        var lower = value.Trim().ToLowerInvariant()
            .Replace("&", " en ", StringComparison.Ordinal);
        while (lower.Contains("  ", StringComparison.Ordinal))
        {
            lower = lower.Replace("  ", " ", StringComparison.Ordinal);
        }

        return lower;
    }

    private sealed record StubEstablishment(
        string KvkNumber,
        string EstablishmentNumber,
        string Name,
        string Address,
        string Place,
        double Latitude,
        double Longitude,
        KvkAddressLine? Visiting = null,
        KvkAddressLine? Postal = null,
        string? Type = null,
        bool ForceInUse = false);
}

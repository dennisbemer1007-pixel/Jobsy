using Jobsy.Core.Interfaces;
using Jobsy.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Jobsy.Infrastructure.Services;

public sealed class KvkServiceStub : IKvkService
{
    private readonly JobsyDbContext _db;

    private static readonly IReadOnlyList<KvkEstablishmentResult> Catalog =
    [
        new("12345678", "0001", "12345678_0001", "Westland Fresh Logistics HQ",
            "'s-Gravenzandseweg 10, Honselersdijk", 51.9812, 4.2235, false),
        new("12345678", "0002", "12345678_0002", "Westland Fresh — Naaldwijk",
            "Dijkweg 2, Naaldwijk", 51.9930, 4.2080, false),
        new("87654321", "0001", "87654321_0001", "Boutique Café De Stad",
            "Grote Markt 14, Den Haag Centrum", 52.0735, 4.3120, false),
        new("11223344", "0001", "11223344_0001", "Supermarkt De Fred — Statenkwartier",
            "Frederik Hendriklaan 88, Den Haag", 52.0910, 4.2815, false),
        new("11223344", "0002", "11223344_0002", "Supermarkt De Fred — Scheveningen",
            "Keizerstraat 12, Scheveningen", 52.1045, 4.2750, false),
        new("55667788", "0001", "55667788_0001", "Demo Intermediair Flex BV",
            "Binckhorstlaan 36, Den Haag", 52.0680, 4.3350, false)
    ];

    public KvkServiceStub(JobsyDbContext db)
    {
        _db = db;
    }

    public Task<KvkCompanyResult?> GetByKvkNumberAsync(
        string kvkNumber,
        CancellationToken cancellationToken = default)
    {
        var match = Catalog.FirstOrDefault(c =>
            c.KvkNumber.Equals(kvkNumber, StringComparison.OrdinalIgnoreCase));
        if (match is null)
        {
            return Task.FromResult<KvkCompanyResult?>(null);
        }

        return Task.FromResult<KvkCompanyResult?>(
            new KvkCompanyResult(match.KvkNumber, match.Name.Split('—')[0].Trim(), match.Address));
    }

    public async Task<IReadOnlyList<KvkEstablishmentResult>> GetEstablishmentsAsync(
        string kvkNumber,
        CancellationToken cancellationToken = default)
    {
        var inUse = await _db.Companies
            .AsNoTracking()
            .Where(c => c.KvkNumber == kvkNumber && c.KvkEstablishmentId != null)
            .Select(c => c.KvkEstablishmentId!)
            .ToListAsync(cancellationToken);

        return Catalog
            .Where(c => c.KvkNumber.Equals(kvkNumber, StringComparison.OrdinalIgnoreCase))
            .Select(c => c with { IsInUse = inUse.Contains(c.KvkEstablishmentId) })
            .ToList();
    }
}

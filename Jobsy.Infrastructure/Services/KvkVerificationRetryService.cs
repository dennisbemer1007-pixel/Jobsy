using Jobsy.Core.Enums;
using Jobsy.Core.Interfaces;
using Jobsy.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Jobsy.Infrastructure.Services;

public sealed class KvkVerificationRetryService : IKvkVerificationRetryService
{
    public const int MaxAttempts = 48; // ~2 days at hourly cadence

    private readonly JobsyDbContext _db;
    private readonly IKvkService _kvk;
    private readonly ILogger<KvkVerificationRetryService> _logger;

    public KvkVerificationRetryService(
        JobsyDbContext db,
        IKvkService kvk,
        ILogger<KvkVerificationRetryService> logger)
    {
        _db = db;
        _kvk = kvk;
        _logger = logger;
    }

    public async Task<int> RetryPendingAsync(CancellationToken cancellationToken = default)
    {
        var pending = await _db.Companies
            .Where(c => c.KvkVerificationStatus == KvkVerificationStatus.Pending
                        && c.KvkEstablishmentId != null)
            .OrderBy(c => c.KvkLastVerificationAttemptAtUtc ?? c.KvkVerifiedAtUtc)
            .Take(100)
            .ToListAsync(cancellationToken);

        if (pending.Count == 0)
        {
            return 0;
        }

        var verified = 0;
        foreach (var company in pending)
        {
            company.KvkLastVerificationAttemptAtUtc = DateTime.UtcNow;
            company.KvkVerificationAttempts += 1;

            var lookup = await _kvk.LookupEstablishmentsAsync(company.KvkNumber, cancellationToken);
            if (lookup.Status == KvkLookupStatus.Unavailable)
            {
                if (company.KvkVerificationAttempts >= MaxAttempts)
                {
                    company.KvkVerificationStatus = KvkVerificationStatus.Failed;
                    _logger.LogWarning(
                        "KVK verification failed after {Attempts} attempts for company {CompanyId}",
                        company.KvkVerificationAttempts, company.Id);
                }

                continue;
            }

            if (lookup.Status == KvkLookupStatus.NotFound)
            {
                company.KvkVerificationStatus = KvkVerificationStatus.Failed;
                continue;
            }

            var match = lookup.Establishments.FirstOrDefault(e =>
                e.KvkEstablishmentId.Equals(company.KvkEstablishmentId, StringComparison.OrdinalIgnoreCase));
            if (match is null)
            {
                company.KvkVerificationStatus = KvkVerificationStatus.Failed;
                continue;
            }

            company.Name = match.Name;
            company.Address = match.Address;
            company.Location = new Core.ValueObjects.GeoPoint(match.Latitude, match.Longitude);
            company.KvkVerificationStatus = KvkVerificationStatus.Verified;
            company.KvkVerifiedAtUtc = DateTime.UtcNow;
            verified++;

            _logger.LogInformation(
                "KVK verification succeeded for company {CompanyId} ({Establishment})",
                company.Id, company.KvkEstablishmentId);
        }

        await _db.SaveChangesAsync(cancellationToken);
        return verified;
    }
}

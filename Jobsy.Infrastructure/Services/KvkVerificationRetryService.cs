using Jobsy.Core.Entities;
using Jobsy.Core.Enums;
using Jobsy.Core.Interfaces;
using Jobsy.Core.Rules;
using Jobsy.Core.ValueObjects;
using Jobsy.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Jobsy.Infrastructure.Services;

public sealed class KvkVerificationRetryService : IKvkVerificationRetryService
{
    public const int MaxAttempts = 48; // ~2 days at hourly cadence

    private readonly JobsyDbContext _db;
    private readonly IKvkService _kvk;
    private readonly CompanyRegistrationService _registration;
    private readonly IPartnerAffiliateService _partnerAffiliates;
    private readonly ILogger<KvkVerificationRetryService> _logger;

    public KvkVerificationRetryService(
        JobsyDbContext db,
        IKvkService kvk,
        CompanyRegistrationService registration,
        ILogger<KvkVerificationRetryService> logger)
        : this(
            db,
            kvk,
            registration,
            new PartnerAffiliateService(
                db,
                new TokenLedgerService(db),
                new PlatformFeatureService(
                    db,
                    Microsoft.Extensions.Options.Options.Create(new Jobsy.Core.Options.JobsyFeatureOptions()),
                    new Microsoft.Extensions.Configuration.ConfigurationBuilder().Build())),
            logger)
    {
    }

    public KvkVerificationRetryService(
        JobsyDbContext db,
        IKvkService kvk,
        CompanyRegistrationService registration,
        IPartnerAffiliateService partnerAffiliates,
        ILogger<KvkVerificationRetryService> logger)
    {
        _db = db;
        _kvk = kvk;
        _registration = registration;
        _partnerAffiliates = partnerAffiliates;
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

            // Ownership: if another company already owns this establishment, do not auto-verify.
            var occupiedByOther = await _db.Companies.AsNoTracking()
                .AnyAsync(
                    c => c.Id != company.Id
                         && c.KvkEstablishmentId == company.KvkEstablishmentId
                         && c.KvkVerificationStatus == KvkVerificationStatus.Verified,
                    cancellationToken);
            if (occupiedByOther || match.IsInUse)
            {
                // IsInUse may include this company itself — re-check excluding self.
                var otherOwner = await _db.Companies.AsNoTracking()
                    .Where(c => c.KvkEstablishmentId == company.KvkEstablishmentId && c.Id != company.Id)
                    .Select(c => c.Id)
                    .FirstOrDefaultAsync(cancellationToken);
                if (otherOwner != Guid.Empty)
                {
                    company.KvkVerificationStatus = KvkVerificationStatus.Failed;
                    _logger.LogWarning(
                        "KVK verification rejected for company {CompanyId}: establishment {Establishment} already owned",
                        company.Id, company.KvkEstablishmentId);
                    continue;
                }
            }

            company.Name = match.Name;
            company.Address = match.Address;
            company.Location = new GeoPoint(match.Latitude, match.Longitude);
            company.KvkVerificationStatus = KvkVerificationStatus.Verified;
            company.KvkVerifiedAtUtc = DateTime.UtcNow;

            var kvkCompany = await _kvk.GetByKvkNumberAsync(company.KvkNumber, cancellationToken);
            var sbiCodes = kvkCompany?.EffectiveSbiCodes.Count > 0
                ? kvkCompany.EffectiveSbiCodes
                : match.EffectiveSbiCodes;
            await ApplyVerifiedSbiClassificationAsync(company, sbiCodes, cancellationToken);

            if (company.ParentCompanyId is Guid orgId)
            {
                var org = await _db.Companies.FirstOrDefaultAsync(c => c.Id == orgId, cancellationToken);
                if (org is not null)
                {
                    org.KvkVerificationStatus = KvkVerificationStatus.Verified;
                    org.KvkVerifiedAtUtc ??= DateTime.UtcNow;
                    if (kvkCompany is not null)
                    {
                        org.Name = kvkCompany.Name;
                        org.Address = kvkCompany.Address;
                    }

                    await _registration.ClaimSiblingEstablishmentsForOrgAsync(
                        company.KvkNumber, orgId, company.Id, cancellationToken);

                    // Membership for newly claimed siblings for the org's enterprise managers.
                    await EnsureOrgMembershipsForSiblingsAsync(orgId, cancellationToken);
                }
            }

            verified++;
            _logger.LogInformation(
                "KVK verification succeeded for company {CompanyId} ({Establishment})",
                company.Id, company.KvkEstablishmentId);
        }

        await _db.SaveChangesAsync(cancellationToken);
        return verified;
    }

    private async Task ApplyVerifiedSbiClassificationAsync(
        Company company,
        IReadOnlyList<string> sbiCodes,
        CancellationToken cancellationToken)
    {
        var isIntermediary = KvkSbiClassification.IsIntermediary(sbiCodes);
        if (!isIntermediary)
        {
            return;
        }

        // Promote pending employer → intermediary only after KVK confirms SBI 78*.
        if (company.Type != CompanyType.Intermediary)
        {
            company.Type = CompanyType.Intermediary;
            company.ParentCompanyId = null;
        }

        var users = await _db.Users
            .Where(u => u.CompanyId == company.Id
                        && (u.Role == UserRole.BranchManager
                            || u.Role == UserRole.EnterpriseManager
                            || u.Role == UserRole.RegionalManager))
            .ToListAsync(cancellationToken);
        foreach (var user in users)
        {
            user.Role = UserRole.Intermediary;
        }

        await _db.SaveChangesAsync(cancellationToken);
        foreach (var user in users)
        {
            await _partnerAffiliates.EnsureProfileAsync(user.Id, cancellationToken);
        }
    }

    private async Task EnsureOrgMembershipsForSiblingsAsync(
        Guid orgId,
        CancellationToken cancellationToken)
    {
        var managers = await _db.Users
            .Where(u => u.Role == UserRole.EnterpriseManager && u.CompanyId == orgId)
            .Select(u => u.Id)
            .ToListAsync(cancellationToken);
        if (managers.Count == 0)
        {
            return;
        }

        var childIds = await _db.Companies
            .Where(c => c.ParentCompanyId == orgId)
            .Select(c => c.Id)
            .ToListAsync(cancellationToken);
        childIds.AddRange(_db.Companies.Local
            .Where(c => c.ParentCompanyId == orgId)
            .Select(c => c.Id));

        foreach (var managerId in managers.Distinct())
        {
            foreach (var childId in childIds.Distinct())
            {
                var exists = await _db.UserCompanies
                    .AnyAsync(m => m.UserId == managerId && m.CompanyId == childId, cancellationToken);
                if (!exists && !_db.UserCompanies.Local.Any(m => m.UserId == managerId && m.CompanyId == childId))
                {
                    _db.UserCompanies.Add(new UserCompany
                    {
                        UserId = managerId,
                        CompanyId = childId
                    });
                }
            }
        }
    }
}

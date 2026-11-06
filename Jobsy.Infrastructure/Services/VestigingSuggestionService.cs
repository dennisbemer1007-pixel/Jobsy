using Jobsy.Core.Entities;
using Jobsy.Core.Enums;
using Jobsy.Core.Interfaces;
using Jobsy.Core.ValueObjects;
using Jobsy.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace Jobsy.Infrastructure.Services;

public sealed class VestigingSuggestionService : IVestigingSuggestionService
{
    public static readonly TimeSpan DismissDuration = TimeSpan.FromDays(90);
    private static readonly TimeSpan CacheTtl = TimeSpan.FromHours(24);

    private readonly JobsyDbContext _db;
    private readonly IKvkService _kvk;
    private readonly IKvkUsageCounter _usage;
    private readonly IMemoryCache _cache;
    private readonly ILogger<VestigingSuggestionService> _logger;

    public VestigingSuggestionService(
        JobsyDbContext db,
        IKvkService kvk,
        IKvkUsageCounter usage,
        IMemoryCache cache,
        ILogger<VestigingSuggestionService> logger)
    {
        _db = db;
        _kvk = kvk;
        _usage = usage;
        _cache = cache;
        _logger = logger;
    }

    public async Task<int> RefreshSuggestionsAsync(CancellationToken cancellationToken = default)
    {
        var usage = await _usage.GetSummaryAsync(cancellationToken);
        if (usage.BudgetWarning
            || usage.ProfileCallsThisMonth >= usage.MonthlyProfileBudget)
        {
            _logger.LogInformation(
                "Skipping vestiging suggestion scan — KVK budget {Used}/{Budget} (warning={Warning}).",
                usage.ProfileCallsThisMonth,
                usage.MonthlyProfileBudget,
                usage.BudgetWarning);
            return 0;
        }

        var orgRoots = await _db.CompanyRegistrations.AsNoTracking()
            .Where(r => r.Status == CompanyRegistrationStatus.Activated
                        && r.Scope == RegistrationScope.Organization
                        && r.CreatedOrganizationCompanyId != null)
            .Select(r => r.CreatedOrganizationCompanyId!.Value)
            .Distinct()
            .ToListAsync(cancellationToken);

        var verifiedRoots = await _db.Companies.AsNoTracking()
            .Where(c => orgRoots.Contains(c.Id)
                        && c.ParentCompanyId == null
                        && c.VerificationStatus == CompanyVerificationStatus.Verified)
            .Select(c => new { c.Id, c.KvkNumber })
            .ToListAsync(cancellationToken);

        var found = 0;
        foreach (var root in verifiedRoots)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                break;
            }

            usage = await _usage.GetSummaryAsync(cancellationToken);
            if (usage.BudgetWarning
                || usage.ProfileCallsThisMonth >= usage.MonthlyProfileBudget)
            {
                break;
            }

            var list = await ComputeOpenAsync(root.Id, root.KvkNumber, cancellationToken);
            _cache.Set(CacheKey(root.Id), list, CacheTtl);
            found += list.Count;
        }

        return found;
    }

    public async Task<IReadOnlyList<EmployerVestigingSuggestionDto>> ListOpenAsync(
        Guid rootCompanyId,
        CancellationToken cancellationToken = default)
    {
        if (_cache.TryGetValue(CacheKey(rootCompanyId), out IReadOnlyList<EmployerVestigingSuggestionDto>? cached)
            && cached is not null)
        {
            return cached;
        }

        var root = await _db.Companies.AsNoTracking()
            .Where(c => c.Id == rootCompanyId)
            .Select(c => new { c.Id, c.KvkNumber, c.VerificationStatus, c.ParentCompanyId })
            .FirstOrDefaultAsync(cancellationToken);
        if (root is null
            || root.ParentCompanyId is not null
            || root.VerificationStatus != CompanyVerificationStatus.Verified)
        {
            return [];
        }

        var isOrgScope = await _db.CompanyRegistrations.AsNoTracking()
            .AnyAsync(
                r => r.CreatedOrganizationCompanyId == rootCompanyId
                     && r.Scope == RegistrationScope.Organization
                     && r.Status == CompanyRegistrationStatus.Activated,
                cancellationToken);
        if (!isOrgScope)
        {
            return [];
        }

        var usage = await _usage.GetSummaryAsync(cancellationToken);
        if (usage.BudgetWarning
            || usage.ProfileCallsThisMonth >= usage.MonthlyProfileBudget)
        {
            return [];
        }

        var list = await ComputeOpenAsync(root.Id, root.KvkNumber, cancellationToken);
        _cache.Set(CacheKey(root.Id), list, CacheTtl);
        return list;
    }

    public async Task DismissAsync(
        Guid rootCompanyId,
        string kvkEstablishmentId,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var id = (kvkEstablishmentId ?? "").Trim();
        if (string.IsNullOrWhiteSpace(id))
        {
            return;
        }

        var now = DateTime.UtcNow;
        var existing = await _db.DismissedVestigingSuggestions
            .FirstOrDefaultAsync(
                d => d.CompanyId == rootCompanyId && d.KvkEstablishmentId == id,
                cancellationToken);
        if (existing is null)
        {
            _db.DismissedVestigingSuggestions.Add(new DismissedVestigingSuggestion
            {
                Id = Guid.NewGuid(),
                CompanyId = rootCompanyId,
                KvkEstablishmentId = id,
                HiddenUntilUtc = now.Add(DismissDuration),
                CreatedAtUtc = now,
                DismissedByUserId = userId
            });
        }
        else
        {
            existing.HiddenUntilUtc = now.Add(DismissDuration);
            existing.DismissedByUserId = userId;
        }

        await _db.SaveChangesAsync(cancellationToken);
        _cache.Remove(CacheKey(rootCompanyId));
    }

    public async Task AcceptAsync(
        Guid rootCompanyId,
        string kvkEstablishmentId,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var root = await _db.Companies.FirstOrDefaultAsync(c => c.Id == rootCompanyId, cancellationToken)
            ?? throw new KeyNotFoundException("Company not found.");

        var id = (kvkEstablishmentId ?? "").Trim();
        var establishments = await _kvk.GetEstablishmentsAsync(root.KvkNumber, cancellationToken);
        var match = establishments.FirstOrDefault(e =>
            e.KvkEstablishmentId.Equals(id, StringComparison.OrdinalIgnoreCase));
        if (match is null)
        {
            throw new InvalidOperationException("Vestiging niet gevonden in KVK.");
        }

        if (match.IsInUse
            || await _db.Companies.AnyAsync(c => c.KvkEstablishmentId == match.KvkEstablishmentId, cancellationToken))
        {
            throw new InvalidOperationException("Deze vestiging is al geregistreerd.");
        }

        var company = new Company
        {
            Id = Guid.NewGuid(),
            Name = match.Name,
            KvkNumber = match.KvkNumber,
            KvkEstablishmentId = match.KvkEstablishmentId,
            Address = match.Address,
            Location = new GeoPoint(match.Latitude, match.Longitude),
            Type = CompanyType.Employer,
            ParentCompanyId = root.Id,
            LocationSource = CompanyLocationSource.Kvk,
            VerificationStatus = root.VerificationStatus == CompanyVerificationStatus.Verified
                ? CompanyVerificationStatus.Verified
                : root.VerificationStatus,
            VerificationMethod = CompanyVerificationMethod.InheritedFromOrganization,
            VerifiedAtUtc = root.VerificationStatus == CompanyVerificationStatus.Verified
                ? (root.VerifiedAtUtc ?? DateTime.UtcNow)
                : null,
            VerificationUpdatedAtUtc = DateTime.UtcNow
        };

        _db.Companies.Add(company);
        if (!await _db.UserCompanies.AnyAsync(uc => uc.UserId == userId && uc.CompanyId == company.Id, cancellationToken))
        {
            _db.UserCompanies.Add(new UserCompany { UserId = userId, CompanyId = company.Id });
        }

        // Accepting removes any dismiss row.
        var dismiss = await _db.DismissedVestigingSuggestions
            .Where(d => d.CompanyId == rootCompanyId && d.KvkEstablishmentId == match.KvkEstablishmentId)
            .ToListAsync(cancellationToken);
        _db.DismissedVestigingSuggestions.RemoveRange(dismiss);

        await _db.SaveChangesAsync(cancellationToken);
        _cache.Remove(CacheKey(rootCompanyId));
    }

    private async Task<IReadOnlyList<EmployerVestigingSuggestionDto>> ComputeOpenAsync(
        Guid rootCompanyId,
        string kvkNumber,
        CancellationToken cancellationToken)
    {
        var lookup = await _kvk.LookupEstablishmentsAsync(kvkNumber, cancellationToken);
        if (lookup.Status != KvkLookupStatus.Ok)
        {
            return [];
        }

        var existingIds = await _db.Companies.AsNoTracking()
            .Where(c => c.KvkNumber == kvkNumber && c.KvkEstablishmentId != null)
            .Select(c => c.KvkEstablishmentId!)
            .ToListAsync(cancellationToken);
        var existing = new HashSet<string>(existingIds, StringComparer.OrdinalIgnoreCase);

        var now = DateTime.UtcNow;
        var dismissed = await _db.DismissedVestigingSuggestions.AsNoTracking()
            .Where(d => d.CompanyId == rootCompanyId && d.HiddenUntilUtc > now)
            .Select(d => d.KvkEstablishmentId)
            .ToListAsync(cancellationToken);
        var dismissedSet = new HashSet<string>(dismissed, StringComparer.OrdinalIgnoreCase);

        return lookup.Establishments
            .Where(e => !e.IsInUse
                        && !existing.Contains(e.KvkEstablishmentId)
                        && !dismissedSet.Contains(e.KvkEstablishmentId))
            .Select(e => new EmployerVestigingSuggestionDto(e.KvkEstablishmentId, e.Name, e.Address))
            .ToList();
    }

    private static string CacheKey(Guid rootCompanyId) => $"wa:vestiging-suggestions:{rootCompanyId:D}";
}

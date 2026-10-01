using Jobsy.Core.Contracts;
using Jobsy.Core.Enums;
using Jobsy.Core.Interfaces;
using Jobsy.Core.Rules;
using Jobsy.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Jobsy.Infrastructure.Services;

/// <summary>
/// Public /{kvk} visibility: KVK verified + platform verified + ≥1 publicly visible vacancy,
/// and not blocked by a moderation decision (<c>PublicPageBlockedAtUtc</c>).
/// </summary>
public interface IPublicCompanyQuery
{
    Task<IReadOnlyList<PublicCompanyRow>> GetByKvkAsync(string kvk, CancellationToken cancellationToken = default);

    Task<PublicCompanyRow?> GetVestigingAsync(
        string kvk,
        string vestigingsnummer,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<string>> GetSitemapCompanyPathsAsync(CancellationToken cancellationToken = default);
}

public sealed record PublicCompanyRow(
    Guid Id,
    string Name,
    string? City,
    string? LogoUrl,
    string? KvkEstablishmentId,
    Guid? ParentCompanyId,
    int PublicVacancyCount);

public sealed class PublicCompanyQuery : IPublicCompanyQuery
{
    private readonly JobsyDbContext _db;
    private readonly IVacancyDiscoveryIndex _discovery;

    public PublicCompanyQuery(JobsyDbContext db, IVacancyDiscoveryIndex discovery)
    {
        _db = db;
        _discovery = discovery;
    }

    public async Task<IReadOnlyList<PublicCompanyRow>> GetByKvkAsync(
        string kvk,
        CancellationToken cancellationToken = default)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var records = await _discovery.GetActiveAsync(cancellationToken);
        var publicByCompany = records
            .Where(r => VacancyVisibilityRules.IsPubliclyVisible(r, today)
                        && string.Equals(r.KvkNumber, kvk, StringComparison.Ordinal))
            .GroupBy(r => r.CompanyId)
            .ToDictionary(g => g.Key, g => g.Count());

        if (publicByCompany.Count == 0)
        {
            return [];
        }

        var companies = await _db.Companies.AsNoTracking()
            .Where(PublicVisibility.CompanyIsPublic)
            .Where(c => c.KvkNumber == kvk
                        && c.KvkVerificationStatus == KvkVerificationStatus.Verified
                        // A "Verwijderen" decision on a content report takes the page offline (06).
                        && c.PublicPageBlockedAtUtc == null)
            .Where(c => publicByCompany.Keys.Contains(c.Id))
            .Select(c => new
            {
                c.Id,
                c.Name,
                c.Address,
                c.LogoUrl,
                c.KvkEstablishmentId,
                c.ParentCompanyId
            })
            .ToListAsync(cancellationToken);

        return companies
            .Select(c => new PublicCompanyRow(
                c.Id,
                c.Name,
                ResolveCity(c.Address),
                c.LogoUrl,
                c.KvkEstablishmentId,
                c.ParentCompanyId,
                publicByCompany.GetValueOrDefault(c.Id)))
            .Where(r => r.PublicVacancyCount > 0)
            .ToList();
    }

    public async Task<PublicCompanyRow?> GetVestigingAsync(
        string kvk,
        string vestigingsnummer,
        CancellationToken cancellationToken = default)
    {
        var rows = await GetByKvkAsync(kvk, cancellationToken);
        var trimmed = vestigingsnummer.Trim();
        return rows.FirstOrDefault(c =>
            string.Equals(
                CompanyPublicPaths.TryParseVestigingsnummer(c.KvkEstablishmentId, kvk),
                trimmed,
                StringComparison.Ordinal)
            || string.Equals(c.KvkEstablishmentId, trimmed, StringComparison.Ordinal)
            || string.Equals(
                c.KvkEstablishmentId,
                CompanyPublicPaths.BuildEstablishmentId(kvk, trimmed),
                StringComparison.Ordinal));
    }

    public async Task<IReadOnlyList<string>> GetSitemapCompanyPathsAsync(
        CancellationToken cancellationToken = default)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var records = await _discovery.GetActiveAsync(cancellationToken);
        var kvks = records
            .Where(r => VacancyVisibilityRules.IsPubliclyVisible(r, today))
            .Select(r => r.KvkNumber)
            .Where(k => !string.IsNullOrWhiteSpace(k))
            .Distinct(StringComparer.Ordinal)
            .ToList();

        if (kvks.Count == 0)
        {
            return [];
        }

        var paths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var kvk in kvks)
        {
            var normalized = CompanyPublicPaths.NormalizeKvkNumber(kvk);
            if (normalized is null)
            {
                continue;
            }

            var rows = await GetByKvkAsync(normalized, cancellationToken);
            if (rows.Count == 0)
            {
                continue;
            }

            if (rows.Any(r => r.ParentCompanyId is null))
            {
                var kvkPath = CompanyPublicPaths.TryBuildKvkPath(normalized);
                if (kvkPath is not null)
                {
                    paths.Add(kvkPath);
                }
            }

            foreach (var branch in rows)
            {
                var vestigingPath = CompanyPublicPaths.TryBuildPath(normalized, branch.KvkEstablishmentId);
                if (vestigingPath is not null)
                {
                    paths.Add(vestigingPath);
                }
            }
        }

        return paths.OrderBy(p => p, StringComparer.Ordinal).ToList();
    }

    private static string? ResolveCity(string? address)
        => Jobsy.Core.Contracts.LobsyCvModelFactory.ExtractCity(address);
}

using Jobsy.Core.Entities;
using Jobsy.Core.Interfaces;
using Jobsy.Core.Options;
using Jobsy.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Jobsy.Infrastructure.Services;

public interface IKvkUsageCounter
{
    Task IncrementAsync(string callType, CancellationToken cancellationToken = default);

    Task<KvkUsageSummary> GetSummaryAsync(CancellationToken cancellationToken = default);
}

public sealed record KvkUsageSummary(
    int ZoekenToday,
    int BasisprofielToday,
    int VestigingenToday,
    int ProfileCallsThisMonth,
    int MonthlyProfileBudget,
    bool BudgetWarning);

public sealed class KvkUsageCounter : IKvkUsageCounter
{
    private readonly JobsyDbContext _db;
    private readonly KvkOptions _options;

    public KvkUsageCounter(JobsyDbContext db, IOptions<KvkOptions> options)
    {
        _db = db;
        _options = options.Value ?? new KvkOptions();
    }

    public async Task IncrementAsync(string callType, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(callType))
        {
            return;
        }

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var type = callType.Trim().ToLowerInvariant();
        var row = await _db.KvkUsageDaily
            .FirstOrDefaultAsync(r => r.Date == today && r.CallType == type, cancellationToken);

        if (row is null)
        {
            _db.KvkUsageDaily.Add(new KvkUsageDaily
            {
                Id = Guid.NewGuid(),
                Date = today,
                CallType = type,
                Count = 1
            });
        }
        else
        {
            row.Count += 1;
        }

        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task<KvkUsageSummary> GetSummaryAsync(CancellationToken cancellationToken = default)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var monthStart = new DateOnly(today.Year, today.Month, 1);
        var rows = await _db.KvkUsageDaily
            .AsNoTracking()
            .Where(r => r.Date >= monthStart)
            .ToListAsync(cancellationToken);

        var zoeken = rows.Where(r => r.Date == today && r.CallType == KvkUsageCallTypes.Zoeken).Sum(r => r.Count);
        var basis = rows.Where(r => r.Date == today && r.CallType == KvkUsageCallTypes.Basisprofiel).Sum(r => r.Count);
        var vest = rows.Where(r => r.Date == today && r.CallType == KvkUsageCallTypes.Vestigingen).Sum(r => r.Count);
        var monthProfiles = rows
            .Where(r => r.CallType is KvkUsageCallTypes.Basisprofiel or KvkUsageCallTypes.Vestigingen)
            .Sum(r => r.Count);
        var budget = _options.MonthlyProfileBudget <= 0 ? 50_000 : _options.MonthlyProfileBudget;
        var warning = monthProfiles >= (int)(budget * 0.8);

        return new KvkUsageSummary(zoeken, basis, vest, monthProfiles, budget, warning);
    }
}

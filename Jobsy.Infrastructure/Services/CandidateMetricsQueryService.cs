using Jobsy.Core.Contracts;
using Jobsy.Core.Enums;
using Jobsy.Core.Interfaces;
using Jobsy.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Jobsy.Infrastructure.Services;

public sealed class CandidateMetricsQueryService : ICandidateMetricsQueryService
{
    private readonly JobsyDbContext _db;

    public CandidateMetricsQueryService(JobsyDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<MetricCountDto>> GetSummaryAsync(
        Guid candidateUserId,
        string period,
        CancellationToken cancellationToken = default)
    {
        var metricsPeriod = MetricsPeriodParser.Parse(period);
        var (from, to) = MetricsPeriodParser.ResolveRange(metricsPeriod);
        var periodKey = metricsPeriod.ToString().ToLowerInvariant();

        var applications = await _db.Applications.AsNoTracking()
            .CountAsync(a => a.CandidateUserId == candidateUserId
                             && a.EmailVerifiedAt != null
                             && a.CreatedAt >= from && a.CreatedAt <= to, cancellationToken);

        var shares = await _db.VacancyShares.AsNoTracking()
            .CountAsync(s => s.UserId == candidateUserId && s.CreatedAt >= from && s.CreatedAt <= to, cancellationToken);

        var likes = await _db.VacancyLikes.AsNoTracking()
            .CountAsync(l => l.UserId == candidateUserId && l.CreatedAt >= from && l.CreatedAt <= to, cancellationToken);

        // "Reacties" = vacatureclicks door deze kandidaat.
        var reactions = await _db.VacancyClicks.AsNoTracking()
            .CountAsync(c => c.UserId == candidateUserId && c.CreatedAt >= from && c.CreatedAt <= to, cancellationToken);

        var bucketCount = metricsPeriod switch
        {
            MetricsPeriod.Day => 12,
            MetricsPeriod.Week => 7,
            MetricsPeriod.Month => 10,
            MetricsPeriod.Quarter => 12,
            MetricsPeriod.Year => 12,
            _ => 7
        };

        async Task<IReadOnlyList<decimal>> SparkAsync(string key)
        {
            List<DateTime> stamps = key switch
            {
                "applications" => await _db.Applications.AsNoTracking()
                    .Where(a => a.CandidateUserId == candidateUserId
                                && a.EmailVerifiedAt != null
                                && a.CreatedAt >= from && a.CreatedAt <= to)
                    .Select(a => a.CreatedAt)
                    .ToListAsync(cancellationToken),
                "shares" => await _db.VacancyShares.AsNoTracking()
                    .Where(s => s.UserId == candidateUserId && s.CreatedAt >= from && s.CreatedAt <= to)
                    .Select(s => s.CreatedAt)
                    .ToListAsync(cancellationToken),
                "likes" => await _db.VacancyLikes.AsNoTracking()
                    .Where(l => l.UserId == candidateUserId && l.CreatedAt >= from && l.CreatedAt <= to)
                    .Select(l => l.CreatedAt)
                    .ToListAsync(cancellationToken),
                "reactions" => await _db.VacancyClicks.AsNoTracking()
                    .Where(c => c.UserId == candidateUserId && c.CreatedAt >= from && c.CreatedAt <= to)
                    .Select(c => c.CreatedAt)
                    .ToListAsync(cancellationToken),
                _ => []
            };

            return BucketTimestamps(stamps, from, to, bucketCount);
        }

        return
        [
            new MetricCountDto("applications", "Sollicitaties", periodKey, applications, await SparkAsync("applications")),
            new MetricCountDto("shares", "Gedeeld", periodKey, shares, await SparkAsync("shares")),
            new MetricCountDto("likes", "Geliked", periodKey, likes, await SparkAsync("likes")),
            new MetricCountDto("reactions", "Reacties", periodKey, reactions, await SparkAsync("reactions"))
        ];
    }

    private static IReadOnlyList<decimal> BucketTimestamps(
        IReadOnlyList<DateTime> stamps,
        DateTime from,
        DateTime to,
        int bucketCount)
    {
        var buckets = new decimal[bucketCount];
        if (bucketCount <= 0)
        {
            return buckets;
        }

        var spanTicks = Math.Max((to - from).Ticks, TimeSpan.FromHours(1).Ticks);
        var bucketTicks = spanTicks / bucketCount;

        foreach (var stamp in stamps)
        {
            var offset = (stamp - from).Ticks;
            var index = (int)(offset / bucketTicks);
            if (index < 0) index = 0;
            else if (index >= bucketCount) index = bucketCount - 1;
            buckets[index] += 1;
        }

        return buckets;
    }

    public async Task<IReadOnlyList<MetricDrilldownItemDto>> GetDrilldownAsync(
        Guid candidateUserId,
        string key,
        string period,
        CancellationToken cancellationToken = default)
    {
        var metricsPeriod = MetricsPeriodParser.Parse(period);
        var (from, to) = MetricsPeriodParser.ResolveRange(metricsPeriod);

        return key.ToLowerInvariant() switch
        {
            "applications" => await _db.Applications.AsNoTracking()
                .Where(a => a.CandidateUserId == candidateUserId
                            && a.EmailVerifiedAt != null
                            && a.CreatedAt >= from && a.CreatedAt <= to)
                .OrderByDescending(a => a.CreatedAt)
                .Select(a => new MetricDrilldownItemDto(
                    a.Id, a.Vacancy.Title, a.Vacancy.Company.Name, a.CreatedAt, null))
                .ToListAsync(cancellationToken),
            "shares" => await _db.VacancyShares.AsNoTracking()
                .Where(s => s.UserId == candidateUserId && s.CreatedAt >= from && s.CreatedAt <= to)
                .OrderByDescending(s => s.CreatedAt)
                .Select(s => new MetricDrilldownItemDto(
                    s.Id, s.Vacancy.Title, s.Channel.ToString(), s.CreatedAt, null))
                .ToListAsync(cancellationToken),
            "likes" => await _db.VacancyLikes.AsNoTracking()
                .Where(l => l.UserId == candidateUserId && l.CreatedAt >= from && l.CreatedAt <= to)
                .OrderByDescending(l => l.CreatedAt)
                .Select(l => new MetricDrilldownItemDto(
                    l.Id, l.Vacancy.Title, l.Vacancy.Company.Name, l.CreatedAt, null))
                .ToListAsync(cancellationToken),
            "reactions" or "clicks" => await _db.VacancyClicks.AsNoTracking()
                .Where(c => c.UserId == candidateUserId && c.CreatedAt >= from && c.CreatedAt <= to)
                .OrderByDescending(c => c.CreatedAt)
                .Select(c => new MetricDrilldownItemDto(
                    c.Id, c.Vacancy.Title, c.Vacancy.Company.Name, c.CreatedAt, null))
                .ToListAsync(cancellationToken),
            _ => Array.Empty<MetricDrilldownItemDto>()
        };
    }
}

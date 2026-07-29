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

        return
        [
            new MetricCountDto("applications", "Sollicitaties", periodKey, applications),
            new MetricCountDto("shares", "Gedeeld", periodKey, shares),
            new MetricCountDto("likes", "Geliked", periodKey, likes),
            new MetricCountDto("reactions", "Reacties", periodKey, reactions)
        ];
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

using System.Globalization;
using System.Text.Json;
using Jobsy.Core.Entities;
using Jobsy.Core.Enums;
using Jobsy.Core.Interfaces;
using Jobsy.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Jobsy.Infrastructure.Services.EmployerPhase2;

public sealed class MaqqieHoursOptions
{
    public bool ExportEnabled { get; set; }
}

public sealed class MaqqieHoursService : IMaqqieHoursService
{
    private readonly JobsyDbContext _db;
    private readonly IEmployerPhase2Service _phase2;
    private readonly IMaqqieHoursExporter _exporter;
    private readonly IOptions<MaqqieHoursOptions> _options;

    public MaqqieHoursService(
        JobsyDbContext db,
        IEmployerPhase2Service phase2,
        IMaqqieHoursExporter exporter,
        IOptions<MaqqieHoursOptions> options)
    {
        _db = db;
        _phase2 = phase2;
        _exporter = exporter;
        _options = options;
    }

    public async Task<bool> CandidateHasActiveMaqqieContractAsync(
        Guid candidateUserId,
        CancellationToken cancellationToken = default)
    {
        if (!await _phase2.IsEnabledAsync(cancellationToken))
        {
            return false;
        }

        return await (
            from p in _db.ApplicationPlacements.AsNoTracking()
            join a in _db.Applications.AsNoTracking() on p.ApplicationId equals a.Id
            where p.EmploymentMode == PlacementEmploymentMode.Maqqie
                  && a.CandidateUserId == candidateUserId
                  && a.Status != ApplicationStatus.Rejected
                  && a.Status != ApplicationStatus.Withdrawn
            select p).AnyAsync(cancellationToken);
    }

    public async Task<MaqqieHoursOverviewDto?> GetOverviewForCandidateAsync(
        Guid candidateUserId,
        CancellationToken cancellationToken = default)
    {
        if (!await CandidateHasActiveMaqqieContractAsync(candidateUserId, cancellationToken))
        {
            return null;
        }

        var placement = await _db.ApplicationPlacements
            .AsNoTracking()
            .Include(p => p.Application).ThenInclude(a => a.Vacancy).ThenInclude(v => v.Company)
            .Where(p => p.EmploymentMode == PlacementEmploymentMode.Maqqie
                        && p.Application.CandidateUserId == candidateUserId)
            .OrderByDescending(p => p.EmploymentModeChosenAtUtc)
            .FirstOrDefaultAsync(cancellationToken);
        if (placement is null)
        {
            return null;
        }

        var applicationId = placement.ApplicationId;
        var weeks = await _db.MaqqieHoursWeeks
            .AsNoTracking()
            .Where(w => w.ApplicationId == applicationId)
            .OrderByDescending(w => w.WeekStart)
            .ToListAsync(cancellationToken);

        var currentStart = StartOfWeek(DateOnly.FromDateTime(DateTime.UtcNow));
        var current = weeks.FirstOrDefault(w => w.WeekStart == currentStart);
        MaqqieHoursWeekDto? currentDto = current is null
            ? null
            : MapWeek(current);

        var previous = weeks
            .Where(w => w.WeekStart != currentStart)
            .Take(8)
            .Select(w => new MaqqieHoursWeekSummaryDto(w.Id, w.WeekStart, w.TotalHours, w.Status))
            .ToList();

        return new MaqqieHoursOverviewDto(
            placement.Application.Vacancy.Company.Name,
            placement.Application.Vacancy.Title,
            currentDto,
            previous);
    }

    public async Task<MaqqieHoursWeekDto?> UpsertDraftWeekAsync(
        Guid candidateUserId,
        DateOnly weekStart,
        IReadOnlyDictionary<int, decimal> dailyHours,
        CancellationToken cancellationToken = default)
    {
        if (!await CandidateHasActiveMaqqieContractAsync(candidateUserId, cancellationToken))
        {
            return null;
        }

        var applicationId = await ResolveApplicationIdAsync(candidateUserId, cancellationToken);
        if (applicationId is null)
        {
            return null;
        }

        weekStart = StartOfWeek(weekStart);
        var total = dailyHours.Values.Sum();
        var json = JsonSerializer.Serialize(dailyHours);

        var week = await _db.MaqqieHoursWeeks
            .FirstOrDefaultAsync(w => w.ApplicationId == applicationId && w.WeekStart == weekStart, cancellationToken);
        if (week is null)
        {
            week = new MaqqieHoursWeek
            {
                Id = Guid.NewGuid(),
                ApplicationId = applicationId.Value,
                WeekStart = weekStart
            };
            _db.MaqqieHoursWeeks.Add(week);
        }
        else if (week.Status != MaqqieHoursWeekStatus.Draft)
        {
            return MapWeek(week);
        }

        week.DailyHoursJson = json;
        week.TotalHours = total;
        week.UpdatedAtUtc = DateTime.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);
        return MapWeek(week);
    }

    public async Task<bool> SubmitWeekAsync(Guid candidateUserId, Guid weekId, CancellationToken cancellationToken = default)
    {
        var week = await LoadOwnedWeekAsync(candidateUserId, weekId, cancellationToken);
        if (week is null || week.Status != MaqqieHoursWeekStatus.Draft)
        {
            return false;
        }

        week.Status = MaqqieHoursWeekStatus.Submitted;
        week.SubmittedAtUtc = DateTime.UtcNow;
        week.UpdatedAtUtc = DateTime.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<bool> EmployerApproveWeekAsync(
        Guid applicationId,
        Guid weekId,
        Guid actorUserId,
        CancellationToken cancellationToken = default)
    {
        if (!await _phase2.IsEnabledAsync(cancellationToken))
        {
            return false;
        }

        var week = await _db.MaqqieHoursWeeks
            .Include(w => w.Application).ThenInclude(a => a.Vacancy)
            .FirstOrDefaultAsync(w => w.Id == weekId && w.ApplicationId == applicationId, cancellationToken);
        if (week is null || week.Status != MaqqieHoursWeekStatus.Submitted)
        {
            return false;
        }

        week.Status = MaqqieHoursWeekStatus.EmployerApproved;
        week.EmployerApprovedAtUtc = DateTime.UtcNow;
        week.UpdatedAtUtc = DateTime.UtcNow;

        if (_options.Value.ExportEnabled)
        {
            week.Status = MaqqieHoursWeekStatus.SentToMaqqie;
            week.SentToMaqqieAtUtc = DateTime.UtcNow;
            await _exporter.ExportWeekAsync(week, week.Application, cancellationToken);
        }

        await _db.SaveChangesAsync(cancellationToken);

        var approvedCount = await _db.MaqqieHoursWeeks
            .CountAsync(
                w => w.ApplicationId == applicationId
                     && w.Status >= MaqqieHoursWeekStatus.EmployerApproved,
                cancellationToken);
        if (approvedCount == 1)
        {
            await _phase2.TryCreditMaqqieWeekOneAsync(applicationId, cancellationToken);
        }

        return true;
    }

    private async Task<MaqqieHoursWeek?> LoadOwnedWeekAsync(
        Guid candidateUserId,
        Guid weekId,
        CancellationToken cancellationToken)
    {
        return await _db.MaqqieHoursWeeks
            .Include(w => w.Application)
            .FirstOrDefaultAsync(
                w => w.Id == weekId && w.Application.CandidateUserId == candidateUserId,
                cancellationToken);
    }

    private async Task<Guid?> ResolveApplicationIdAsync(Guid candidateUserId, CancellationToken cancellationToken)
    {
        return await _db.ApplicationPlacements
            .AsNoTracking()
            .Where(p => p.EmploymentMode == PlacementEmploymentMode.Maqqie
                        && p.Application.CandidateUserId == candidateUserId)
            .OrderByDescending(p => p.EmploymentModeChosenAtUtc)
            .Select(p => p.ApplicationId)
            .FirstOrDefaultAsync(cancellationToken);
    }

    private static MaqqieHoursWeekDto MapWeek(MaqqieHoursWeek week)
    {
        var hours = JsonSerializer.Deserialize<Dictionary<int, decimal>>(week.DailyHoursJson)
                    ?? new Dictionary<int, decimal>();
        return new MaqqieHoursWeekDto(week.Id, week.WeekStart, week.Status, hours, week.TotalHours);
    }

    private static DateOnly StartOfWeek(DateOnly date)
    {
        var dow = (int)date.DayOfWeek;
        var mondayOffset = dow == 0 ? 6 : dow - 1;
        return date.AddDays(-mondayOffset);
    }
}

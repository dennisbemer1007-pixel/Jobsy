using System.Globalization;
using System.Text.Json;
using Jobsy.Core.Entities;
using Jobsy.Core.Enums;
using Jobsy.Core.Interfaces;
using Jobsy.Core.Rules;
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
            join v in _db.Vacancies.AsNoTracking() on a.VacancyId equals v.Id
            where p.EmploymentMode == PlacementEmploymentMode.Maqqie
                  && AcceptCandidateVacancyRules.SupportsEmploymentModeChoice(v.Kind)
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
                        && AcceptCandidateVacancyRules.SupportsEmploymentModeChoice(p.Application.Vacancy.Kind)
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
        else if (week.Status is not MaqqieHoursWeekStatus.Draft
                 and not MaqqieHoursWeekStatus.ReturnedToCandidate)
        {
            return MapWeek(week);
        }

        if (week.Status == MaqqieHoursWeekStatus.ReturnedToCandidate)
        {
            week.Status = MaqqieHoursWeekStatus.Draft;
            week.EmployerReturnNote = null;
            week.EmployerReturnedAtUtc = null;
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
        if (week is null
            || week.Status is not MaqqieHoursWeekStatus.Draft
                and not MaqqieHoursWeekStatus.ReturnedToCandidate)
        {
            return false;
        }

        week.Status = MaqqieHoursWeekStatus.Submitted;
        week.SubmittedAtUtc = DateTime.UtcNow;
        week.EmployerReturnNote = null;
        week.EmployerReturnedAtUtc = null;
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
        if (approvedCount == 1
            && AcceptCandidateVacancyRules.SupportsEmploymentModeChoice(week.Application.Vacancy.Kind))
        {
            await _phase2.TryCreditMaqqieWeekOneAsync(applicationId, cancellationToken);
        }

        return true;
    }

    public async Task<bool> EmployerReturnWeekAsync(
        Guid applicationId,
        Guid weekId,
        Guid actorUserId,
        string note,
        CancellationToken cancellationToken = default)
    {
        if (!await _phase2.IsEnabledAsync(cancellationToken))
        {
            return false;
        }

        note = (note ?? "").Trim();
        if (note.Length == 0 || note.Length > 280)
        {
            return false;
        }

        var week = await _db.MaqqieHoursWeeks
            .FirstOrDefaultAsync(w => w.Id == weekId && w.ApplicationId == applicationId, cancellationToken);
        if (week is null || week.Status != MaqqieHoursWeekStatus.Submitted)
        {
            return false;
        }

        if (!await IsMaqqieApplicationAsync(applicationId, cancellationToken))
        {
            return false;
        }

        week.Status = MaqqieHoursWeekStatus.ReturnedToCandidate;
        week.EmployerReturnNote = note;
        week.EmployerReturnedAtUtc = DateTime.UtcNow;
        week.UpdatedAtUtc = DateTime.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<bool> EmployerHasMaqqiePlacementsAsync(
        Guid employerUserId,
        IReadOnlySet<Guid>? accessibleCompanyIds,
        CancellationToken cancellationToken = default)
    {
        if (!await _phase2.IsEnabledAsync(cancellationToken))
        {
            return false;
        }

        return await MaqqiePlacementQuery(accessibleCompanyIds).AnyAsync(cancellationToken);
    }

    public async Task<MaqqieHoursEmployerOverviewDto?> GetEmployerOverviewAsync(
        Guid employerUserId,
        IReadOnlySet<Guid>? accessibleCompanyIds,
        CancellationToken cancellationToken = default)
    {
        if (!await _phase2.IsEnabledAsync(cancellationToken))
        {
            return null;
        }

        var rows = await (
            from w in _db.MaqqieHoursWeeks.AsNoTracking()
            join a in _db.Applications.AsNoTracking() on w.ApplicationId equals a.Id
            join p in _db.ApplicationPlacements.AsNoTracking() on a.Id equals p.ApplicationId
            join v in _db.Vacancies.AsNoTracking() on a.VacancyId equals v.Id
            where p.EmploymentMode == PlacementEmploymentMode.Maqqie
                  && AcceptCandidateVacancyRules.SupportsEmploymentModeChoice(v.Kind)
                  && w.Status == MaqqieHoursWeekStatus.Submitted
                  && ScopeMatches(accessibleCompanyIds, v.CompanyId, v.IntermediaryCompanyId)
            orderby w.WeekStart descending, w.SubmittedAtUtc descending
            select new MaqqieHoursEmployerWeekRowDto(
                a.Id,
                w.Id,
                w.WeekStart,
                w.TotalHours,
                a.CandidateName,
                v.Title,
                w.Status))
            .Take(50)
            .ToListAsync(cancellationToken);

        return new MaqqieHoursEmployerOverviewDto(rows);
    }

    private async Task<bool> IsMaqqieApplicationAsync(Guid applicationId, CancellationToken cancellationToken)
        => await (
            from p in _db.ApplicationPlacements.AsNoTracking()
            join a in _db.Applications.AsNoTracking() on p.ApplicationId equals a.Id
            join v in _db.Vacancies.AsNoTracking() on a.VacancyId equals v.Id
            where p.ApplicationId == applicationId
                  && p.EmploymentMode == PlacementEmploymentMode.Maqqie
                  && AcceptCandidateVacancyRules.SupportsEmploymentModeChoice(v.Kind)
            select p).AnyAsync(cancellationToken);

    private IQueryable<ApplicationPlacement> MaqqiePlacementQuery(IReadOnlySet<Guid>? accessibleCompanyIds)
        => from p in _db.ApplicationPlacements.AsNoTracking()
           join a in _db.Applications.AsNoTracking() on p.ApplicationId equals a.Id
           join v in _db.Vacancies.AsNoTracking() on a.VacancyId equals v.Id
           where p.EmploymentMode == PlacementEmploymentMode.Maqqie
                 && AcceptCandidateVacancyRules.SupportsEmploymentModeChoice(v.Kind)
                 && ScopeMatches(accessibleCompanyIds, v.CompanyId, v.IntermediaryCompanyId)
           select p;

    private static bool ScopeMatches(
        IReadOnlySet<Guid>? accessibleCompanyIds,
        Guid companyId,
        Guid? intermediaryCompanyId)
    {
        if (accessibleCompanyIds is null)
        {
            return true;
        }

        return accessibleCompanyIds.Contains(companyId)
               || (intermediaryCompanyId is Guid i && accessibleCompanyIds.Contains(i));
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
            .Include(p => p.Application).ThenInclude(a => a.Vacancy)
            .Where(p => p.EmploymentMode == PlacementEmploymentMode.Maqqie
                        && AcceptCandidateVacancyRules.SupportsEmploymentModeChoice(p.Application.Vacancy.Kind)
                        && p.Application.CandidateUserId == candidateUserId)
            .OrderByDescending(p => p.EmploymentModeChosenAtUtc)
            .Select(p => p.ApplicationId)
            .FirstOrDefaultAsync(cancellationToken);
    }

    private static MaqqieHoursWeekDto MapWeek(MaqqieHoursWeek week)
    {
        var hours = JsonSerializer.Deserialize<Dictionary<int, decimal>>(week.DailyHoursJson)
                    ?? new Dictionary<int, decimal>();
        return new MaqqieHoursWeekDto(
            week.Id,
            week.WeekStart,
            week.Status,
            hours,
            week.TotalHours,
            week.EmployerReturnNote);
    }

    private static DateOnly StartOfWeek(DateOnly date)
    {
        var dow = (int)date.DayOfWeek;
        var mondayOffset = dow == 0 ? 6 : dow - 1;
        return date.AddDays(-mondayOffset);
    }
}

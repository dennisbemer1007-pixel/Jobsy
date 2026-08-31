using Jobsy.Core.Contracts;
using Jobsy.Core.Entities;
using Jobsy.Core.Enums;
using Jobsy.Core.Interfaces;
using Jobsy.Core.Media;
using Jobsy.Core.Rules;
using Jobsy.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Jobsy.Infrastructure.Services;

/// <summary>
/// Process-wide banenkaart snapshot. Rebuilt on a short timer and immediately after writes.
/// </summary>
public sealed class VacancyDiscoveryIndex : IVacancyDiscoveryIndex
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<VacancyDiscoveryIndex> _logger;
    private readonly SemaphoreSlim _refreshLock = new(1, 1);

    private volatile IReadOnlyList<VacancyDiscoveryRecord>? _snapshot;
    private volatile VacancyMapView _mapView = VacancyMapViewCalculator.Fallback;
    private volatile bool _dirty = true;
    private DateOnly _indexedForDate;

    public VacancyDiscoveryIndex(
        IServiceScopeFactory scopeFactory,
        ILogger<VacancyDiscoveryIndex> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    public void Invalidate() => _dirty = true;

    public async Task<IReadOnlyList<VacancyDiscoveryRecord>> GetActiveAsync(
        CancellationToken cancellationToken = default)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var snap = _snapshot;
        if (snap is null || _dirty || _indexedForDate != today)
        {
            await RefreshCoreAsync(force: false, cancellationToken);
            snap = _snapshot ?? [];
        }

        return VisibleToday(snap, today);
    }

    public async Task<VacancyMapView> GetMapViewAsync(CancellationToken cancellationToken = default)
    {
        await GetActiveAsync(cancellationToken);
        return _mapView;
    }

    public Task RefreshAsync(CancellationToken cancellationToken = default)
        => RefreshCoreAsync(force: true, cancellationToken);

    private async Task RefreshCoreAsync(bool force, CancellationToken cancellationToken)
    {
        await _refreshLock.WaitAsync(cancellationToken);
        try
        {
            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            if (!force && _snapshot is not null && !_dirty && _indexedForDate == today)
            {
                return;
            }

            _dirty = false;
            var records = await LoadActiveAsync(cancellationToken);
            var todayAfterLoad = DateOnly.FromDateTime(DateTime.UtcNow);
            _snapshot = records;
            _mapView = VacancyMapViewCalculator.FromRecords(VisibleToday(records, todayAfterLoad));
            _indexedForDate = todayAfterLoad;
            _logger.LogInformation("Banenkaart index refreshed with {Count} public vacancies.", records.Count);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _dirty = true;
            _logger.LogWarning(ex, "Banenkaart index refresh failed; discover will retry.");
            if (_snapshot is null)
            {
                throw;
            }
        }
        finally
        {
            _refreshLock.Release();
        }
    }

    private async Task<IReadOnlyList<VacancyDiscoveryRecord>> LoadActiveAsync(
        CancellationToken cancellationToken)
    {
        await using var scope = _scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<JobsyDbContext>();
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        var vacancies = await db.Vacancies
            .AsNoTracking()
            .AsSplitQuery()
            .Include(v => v.Company)
            .Include(v => v.IntermediaryCompany)
            .Include(v => v.Category)
            .Include(v => v.ExclusivitySetting!)
            .Include(v => v.SalaryTable!)
                .ThenInclude(t => t.Rates)
            .Where(v =>
                v.Status == VacancyStatus.Active
                && v.StartDate <= today
                && v.EndDate >= today)
            .OrderBy(v => v.Title)
            .ToListAsync(cancellationToken);

        var records = new List<VacancyDiscoveryRecord>(vacancies.Count);
        foreach (var vacancy in vacancies)
        {
            if (vacancy.Location is null)
            {
                continue;
            }

            records.Add(ToRecord(vacancy));
        }

        return records;
    }

    internal static VacancyDiscoveryRecord ToRecord(Vacancy vacancy)
    {
        var display = IntermediaryVacancyRules.ResolvePublicDisplay(
            vacancy,
            vacancy.Company,
            vacancy.IntermediaryCompany);
        var kvk = CompanyPublicPaths.NormalizeKvkNumber(vacancy.Company?.KvkNumber);
        var rates = vacancy.SalaryTable is { IsActive: true }
            ? vacancy.SalaryTable.Rates
                .OrderBy(r => r.AgeYears)
                .Select(r => new WageAgeBand(
                    r.AgeYears,
                    r.HourlyRate,
                    string.IsNullOrWhiteSpace(r.Label) ? r.AgeYears.ToString() : r.Label))
                .ToList()
            : [];

        return new VacancyDiscoveryRecord(
            vacancy.Id,
            vacancy.Title,
            vacancy.Description ?? string.Empty,
            vacancy.HourlyWage,
            vacancy.StartDate,
            vacancy.EndDate,
            vacancy.Status,
            vacancy.CompanyId,
            display.DisplayName,
            display.DisplayAddress,
            VacancyImageUrls.Normalize(display.DisplayLogoUrl),
            VacancyImageUrls.Normalize(vacancy.ImageUrl),
            vacancy.VideoUrl,
            display.Latitude,
            display.Longitude,
            vacancy.RequiredTransport,
            TransportLabels.Expand(vacancy.RequiredTransport),
            vacancy.WorkTypes,
            vacancy.WorkTypeLabels,
            WorkTypeLabels.ResolveLabels(vacancy.WorkTypes, vacancy.WorkTypeLabels) ?? [],
            vacancy.IsHighlighted,
            vacancy.HighlightedUntil,
            vacancy.ExtensionCount,
            vacancy.SalaryTableId,
            rates,
            vacancy.RequiredDrivingLicense,
            vacancy.RequiredEducation,
            vacancy.MinimumEmployers,
            vacancy.FulfilledByApplicationId,
            vacancy.CreatedVia,
            vacancy.MinHoursPerWeek,
            vacancy.MaxHoursPerWeek,
            vacancy.FlexibleTimes,
            vacancy.ScheduleJson,
            vacancy.LegalWorksAfter19,
            vacancy.LegalNightShift23To06,
            vacancy.LegalAdultSupervisorPresent,
            vacancy.LegalHandlesMoneyOrClosing,
            vacancy.LegalHeavyOrHazardousWork,
            display.OfferedByLabel,
            vacancy.ShowClientAddressOnMap,
            vacancy.IntermediaryCompanyId,
            vacancy.Kind,
            vacancy.ExclusivitySettingId,
            vacancy.ExclusivitySetting?.Name,
            vacancy.ExclusivitySetting?.IsOpenOption ?? true,
            vacancy.ExclusivitySetting?.SchoolDomain,
            [],
            vacancy.CategoryId,
            vacancy.Category?.Name,
            vacancy.Category?.ColorHex,
            vacancy.SuitableFor65Plus,
            kvk,
            CompanyPublicPaths.TryParseVestigingsnummer(
                vacancy.Company?.KvkEstablishmentId,
                kvk),
            vacancy.ContentModerationPassed,
            vacancy.RequireEmailVerification,
            vacancy.MinimumReferences);
    }

    private static IReadOnlyList<VacancyDiscoveryRecord> VisibleToday(
        IReadOnlyList<VacancyDiscoveryRecord> records,
        DateOnly today)
    {
        if (records.Count == 0 || records.All(r => VacancyVisibilityRules.IsPubliclyVisible(r, today)))
        {
            return records;
        }

        return records.Where(r => VacancyVisibilityRules.IsPubliclyVisible(r, today)).ToList();
    }
}

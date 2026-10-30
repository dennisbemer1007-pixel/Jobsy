using System.Security.Claims;
using Jobsy.Core.Authorization;
using Jobsy.Core.Contracts;
using Jobsy.Core.Entities;
using Jobsy.Core.Enums;
using Jobsy.Core.Interfaces;
using Jobsy.Core.Rules;
using Jobsy.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;

namespace Jobsy.Infrastructure.Services;

public sealed class KandidatenkaartService : IKandidatenkaartService
{
    private readonly JobsyDbContext _db;
    private readonly IPlatformFeatureService _features;
    private readonly ICompanyAuthorizationService _authz;
    private readonly KandidatenkaartCandidateAggregator _aggregator;
    private readonly TimeProvider _clock;
    private readonly IHostEnvironment _env;

    private KandidatenkaartCandidateAggregator.AggregationResult? _cachedAggregation;
    private (Guid VacancyId, int MinMatch, int TravelKm, string? Avail, string? Hours) _cachedAggregationKey;

    public KandidatenkaartService(
        JobsyDbContext db,
        IPlatformFeatureService features,
        ICompanyAuthorizationService authz,
        KandidatenkaartCandidateAggregator aggregator,
        TimeProvider clock,
        IHostEnvironment env)
    {
        _db = db;
        _features = features;
        _authz = authz;
        _aggregator = aggregator;
        _clock = clock;
        _env = env;
    }

    public async Task<bool> IsFeatureEnabledAsync(CancellationToken cancellationToken = default)
    {
        var snap = await _features.GetAsync(cancellationToken);
        return snap.KandidatenkaartEnabled;
    }

    public async Task<KandidatenkaartAccessDto> GetAccessAsync(
        ClaimsPrincipal employer,
        Guid companyId,
        CancellationToken cancellationToken = default)
    {
        await EnsureFeatureEnabledAsync(cancellationToken);
        await EnsureCompanyAccessAsync(employer, companyId, cancellationToken);
        var paid = await HasPaidAccessAsync(companyId, cancellationToken);
        return new KandidatenkaartAccessDto(
            FeatureEnabled: true,
            HasPaidAccess: paid,
            ShowPreviewOnly: !paid,
            PricingNote: paid ? null : "Prijs en pakket op aanvraag");
    }

    public async Task<IReadOnlyList<KandidatenkaartVacancyOptionDto>> ListVacanciesAsync(
        ClaimsPrincipal employer,
        IReadOnlyList<Guid> companyIds,
        CancellationToken cancellationToken = default)
    {
        await EnsureFeatureEnabledAsync(cancellationToken);
        foreach (var id in companyIds)
        {
            await EnsureCompanyAccessAsync(employer, id, cancellationToken);
        }

        var vacancies = await _db.Vacancies.AsNoTracking()
            .Where(v => companyIds.Contains(v.CompanyId)
                        && v.Status == VacancyStatus.Active
                        && v.PublishedAtUtc != null)
            .OrderByDescending(v => v.PublishedAtUtc)
            .Select(v => new KandidatenkaartVacancyOptionDto(
                v.Id,
                v.Title,
                v.Company.Name,
                v.PositionsTarget,
                v.PositionsFilled,
                v.PositionsTarget == 0 || v.PositionsFilled < v.PositionsTarget))
            .Take(40)
            .ToListAsync(cancellationToken);

        return vacancies;
    }

    public async Task<KandidatenkaartMapDto> GetMapAsync(
        ClaimsPrincipal employer,
        IReadOnlyList<Guid> companyIds,
        Guid? vacancyId,
        int minMatchPercent,
        int travelKm,
        string? availabilityFilter,
        string? hoursFilter,
        CancellationToken cancellationToken = default)
    {
        await EnsureFeatureEnabledAsync(cancellationToken);
        if (!KandidatenkaartRules.AllowedMinMatchPercents.Contains(minMatchPercent)
            || !KandidatenkaartRules.AllowedTravelKm.Contains(travelKm))
        {
            throw new KandidatenkaartException("invalid_filters", "Ongeldige filters.", 400);
        }

        foreach (var id in companyIds)
        {
            await EnsureCompanyAccessAsync(employer, id, cancellationToken);
        }

        var companyId = companyIds.Count > 0 ? companyIds[0] : Guid.Empty;
        var access = companyId == Guid.Empty
            ? new KandidatenkaartAccessDto(true, false, true, "Prijs en pakket op aanvraag")
            : await GetAccessAsync(employer, companyId, cancellationToken);

        if (access.ShowPreviewOnly)
        {
            return BuildPreviewMap(vacancyId, minMatchPercent, travelKm, availabilityFilter, hoursFilter, access);
        }

        if (_env.IsDevelopment())
        {
            await KandidatenkaartDemoSeed.EnsureAsync(_db, cancellationToken);
            await KandidatenkaartDemoSeed.EnsureEmployerVacancyForMapAsync(_db, cancellationToken);
        }

        if (vacancyId is null)
        {
            throw new KandidatenkaartException("vacancy_required", "Kies een vacature om de kaart te laden.", 400);
        }

        var areas = await BuildAreasAsync(
            vacancyId.Value,
            minMatchPercent,
            travelKm,
            availabilityFilter,
            hoursFilter,
            cancellationToken);
        var visible = areas.Where(a => a.CandidateCount >= KandidatenkaartRules.MinCandidatesPerArea).ToList();
        var below = visible.Count == 0 && areas.Count > 0;

        if (_env.IsDevelopment() && visible.Count == 0 && !below)
        {
            return BuildPreviewMap(vacancyId, minMatchPercent, travelKm, availabilityFilter, hoursFilter, access);
        }

        var (originLat, originLng) = await ResolveVacancyOriginAsync(vacancyId, cancellationToken);

        return new KandidatenkaartMapDto(
            access,
            vacancyId,
            minMatchPercent,
            travelKm,
            availabilityFilter,
            hoursFilter,
            visible,
            below,
            below ? "Met deze filters zijn er geen gebieden met minimaal 30 kandidaten. Probeer een lagere matchscore of grotere reisafstand." : null,
            originLat,
            originLng);
    }

    public async Task<KandidatenkaartInvitePreviewDto> PreviewInviteAsync(
        ClaimsPrincipal employer,
        IReadOnlyList<Guid> companyIds,
        Guid vacancyId,
        string areaKey,
        int minMatchPercent,
        int travelKm,
        CancellationToken cancellationToken = default)
    {
        await EnsureFeatureEnabledAsync(cancellationToken);
        await EnsurePaidAsync(companyIds, employer, cancellationToken);
        var area = await ResolveAreaAsync(
            vacancyId,
            areaKey,
            minMatchPercent,
            travelKm,
            null,
            null,
            cancellationToken)
                   ?? throw new KandidatenkaartException("area_not_found", "Gebied niet gevonden.", 404);

        if (area.CandidateCount < KandidatenkaartRules.MinCandidatesPerArea)
        {
            throw new KandidatenkaartException("area_below_threshold", "Dit gebied heeft minder dan 30 kandidaten.", 400);
        }

        var eligible = await CountEligibleCandidatesAsync(vacancyId, areaKey, minMatchPercent, travelKm, null, null, cancellationToken);
        if (_env.IsDevelopment() && eligible.Eligible == 0 && area.CandidateCount >= KandidatenkaartRules.MinCandidatesPerArea)
        {
            eligible = BuildDemoInviteBreakdown(area, minMatchPercent);
        }

        var rounds = Math.Max(1, (int)Math.Ceiling(eligible.Eligible / 25.0));

        return new KandidatenkaartInvitePreviewDto(
            area.Label,
            eligible.Eligible,
            eligible.BelowMatch,
            eligible.WeeklyCap,
            eligible.PreferenceOff,
            rounds,
            "Uitnodigingen gaan in getrapte rondes (max. ~25 per ronde, enkele uren tussen rondes). Max. 2–3 uitnodigingen per kandidaat per week, gebundeld in één bericht.");
    }

    public async Task<KandidatenkaartInviteResultDto> StartInviteCampaignAsync(
        ClaimsPrincipal employer,
        IReadOnlyList<Guid> companyIds,
        Guid vacancyId,
        string areaKey,
        int minMatchPercent,
        int travelKm,
        CancellationToken cancellationToken = default)
    {
        await EnsureFeatureEnabledAsync(cancellationToken);
        await EnsurePaidAsync(companyIds, employer, cancellationToken);

        var vacancy = await _db.Vacancies.FirstOrDefaultAsync(v => v.Id == vacancyId, cancellationToken)
                      ?? throw new KandidatenkaartException("vacancy_not_found", "Vacature niet gevonden.", 404);

        if (!companyIds.Contains(vacancy.CompanyId))
        {
            throw new KandidatenkaartException("forbidden", "Geen toegang tot deze vacature.", 403);
        }

        if (vacancy.PositionsTarget > 0 && vacancy.PositionsFilled >= vacancy.PositionsTarget)
        {
            throw new KandidatenkaartException("vacancy_full", "De vacature is vol.", 409);
        }

        var area = await ResolveAreaAsync(
            vacancyId,
            areaKey,
            minMatchPercent,
            travelKm,
            null,
            null,
            cancellationToken)
                   ?? throw new KandidatenkaartException("area_not_found", "Gebied niet gevonden.", 404);

        if (area.CandidateCount < KandidatenkaartRules.MinCandidatesPerArea)
        {
            throw new KandidatenkaartException("area_below_threshold", "Dit gebied heeft minder dan 30 kandidaten.", 400);
        }

        var now = _clock.GetUtcNow().UtcDateTime;
        var campaign = new KandidatenkaartInviteCampaign
        {
            Id = Guid.NewGuid(),
            VacancyId = vacancyId,
            CompanyId = vacancy.CompanyId,
            AreaKey = areaKey,
            AreaLabel = area.Label,
            MinMatchPercent = Math.Max(minMatchPercent, KandidatenkaartRules.MinMatchPercentForInvite),
            MaxTravelKm = travelKm,
            Status = KandidatenkaartCampaignStatus.Sending,
            CurrentRound = 1,
            NextRoundAtUtc = now,
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        };

        var candidates = await LoadEligibleCandidateIdsAsync(
            vacancyId,
            areaKey,
            campaign.MinMatchPercent,
            travelKm,
            null,
            null,
            cancellationToken);
        foreach (var (userId, match) in candidates)
        {
            campaign.Deliveries.Add(new KandidatenkaartInviteDelivery
            {
                Id = Guid.NewGuid(),
                CandidateUserId = userId,
                MatchPercent = match,
                Status = KandidatenkaartInviteDeliveryStatus.Queued,
                RoundNumber = 1,
                CreatedAtUtc = now
            });
        }

        _db.KandidatenkaartInviteCampaigns.Add(campaign);
        await _db.SaveChangesAsync(cancellationToken);

        return new KandidatenkaartInviteResultDto(
            campaign.Id,
            campaign.Deliveries.Count,
            1,
            $"Uitnodiging gestart voor {area.Label}. Eerste ronde staat gepland; kandidaten met 70%+ match ontvangen één gebundeld bericht.");
    }

    public async Task<IReadOnlyList<KandidatenkaartCampaignStatusDto>> ListCampaignsAsync(
        ClaimsPrincipal employer,
        Guid vacancyId,
        CancellationToken cancellationToken = default)
    {
        await EnsureFeatureEnabledAsync(cancellationToken);
        var vacancy = await _db.Vacancies.AsNoTracking().FirstOrDefaultAsync(v => v.Id == vacancyId, cancellationToken)
                      ?? throw new KandidatenkaartException("vacancy_not_found", "Vacature niet gevonden.", 404);
        await EnsureCompanyAccessAsync(employer, vacancy.CompanyId, cancellationToken);

        var campaigns = await _db.KandidatenkaartInviteCampaigns.AsNoTracking()
            .Include(c => c.Deliveries)
            .Where(c => c.VacancyId == vacancyId)
            .OrderByDescending(c => c.CreatedAtUtc)
            .ToListAsync(cancellationToken);

        return campaigns.Select(c =>
        {
            var sent = c.Deliveries.Count(d => d.Status is KandidatenkaartInviteDeliveryStatus.Sent
                or KandidatenkaartInviteDeliveryStatus.Reminded
                or KandidatenkaartInviteDeliveryStatus.Responded
                or KandidatenkaartInviteDeliveryStatus.AutoUnavailable);
            var responded = c.Deliveries.Count(d => d.Status == KandidatenkaartInviteDeliveryStatus.Responded);
            var pending = c.Deliveries.Count(d => d.Status is KandidatenkaartInviteDeliveryStatus.Queued
                or KandidatenkaartInviteDeliveryStatus.Sent
                or KandidatenkaartInviteDeliveryStatus.Reminded);
            var view = c.Status switch
            {
                KandidatenkaartCampaignStatus.VacancyFull => KandidatenkaartCampaignStatusView.VacancyFull,
                KandidatenkaartCampaignStatus.Withdrawn => KandidatenkaartCampaignStatusView.Withdrawn,
                KandidatenkaartCampaignStatus.Completed => KandidatenkaartCampaignStatusView.Completed,
                KandidatenkaartCampaignStatus.Sending when c.NextRoundAtUtc > _clock.GetUtcNow().UtcDateTime
                    => KandidatenkaartCampaignStatusView.Staged,
                _ => KandidatenkaartCampaignStatusView.Active
            };
            int? remaining = vacancy.PositionsTarget > 0
                ? Math.Max(0, vacancy.PositionsTarget - vacancy.PositionsFilled)
                : null;
            return new KandidatenkaartCampaignStatusDto(
                c.Id, c.AreaLabel, view, sent, responded, pending, c.CurrentRound, remaining);
        }).ToList();
    }

    public async Task<CandidateKandidatenkaartSettingsDto> GetCandidateSettingsAsync(
        Guid candidateUserId,
        CancellationToken cancellationToken = default)
    {
        await EnsureFeatureEnabledAsync(cancellationToken);
        var row = await _db.CandidateKandidatenkaartSettings.AsNoTracking()
            .FirstOrDefaultAsync(s => s.UserId == candidateUserId, cancellationToken);
        var user = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == candidateUserId, cancellationToken);
        var prompt = row?.ReAvailabilityPromptAtUtc is not null && user is { OpenForWork: false };
        return new CandidateKandidatenkaartSettingsDto(
            MapPreference(row?.InvitePreference ?? KandidatenkaartInvitePreference.Normal),
            prompt);
    }

    public async Task<CandidateKandidatenkaartSettingsDto> UpdateCandidateSettingsAsync(
        Guid candidateUserId,
        CandidateKandidatenkaartSettingsUpdate update,
        CancellationToken cancellationToken = default)
    {
        await EnsureFeatureEnabledAsync(cancellationToken);
        var row = await _db.CandidateKandidatenkaartSettings.FirstOrDefaultAsync(
            s => s.UserId == candidateUserId, cancellationToken);
        if (row is null)
        {
            row = new CandidateKandidatenkaartSettings { UserId = candidateUserId };
            _db.CandidateKandidatenkaartSettings.Add(row);
        }

        row.InvitePreference = update.InvitePreference switch
        {
            KandidatenkaartInvitePreferenceView.Off => KandidatenkaartInvitePreference.Off,
            KandidatenkaartInvitePreferenceView.Max => KandidatenkaartInvitePreference.Max,
            _ => KandidatenkaartInvitePreference.Normal
        };
        row.UpdatedAtUtc = _clock.GetUtcNow().UtcDateTime;
        await _db.SaveChangesAsync(cancellationToken);
        return await GetCandidateSettingsAsync(candidateUserId, cancellationToken);
    }

    public async Task AckReAvailabilityAsync(
        Guid candidateUserId,
        KandidatenkaartReAvailabilityAck ack,
        CancellationToken cancellationToken = default)
    {
        await EnsureFeatureEnabledAsync(cancellationToken);
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == candidateUserId, cancellationToken)
                   ?? throw new KandidatenkaartException("not_found", "Gebruiker niet gevonden.", 404);
        var row = await _db.CandidateKandidatenkaartSettings.FirstOrDefaultAsync(
            s => s.UserId == candidateUserId, cancellationToken);
        if (row is null)
        {
            row = new CandidateKandidatenkaartSettings { UserId = candidateUserId };
            _db.CandidateKandidatenkaartSettings.Add(row);
        }

        if (ack.BecomeAvailable)
        {
            user.OpenForWork = true;
        }

        row.ReAvailabilityPromptAtUtc = null;
        row.UpdatedAtUtc = _clock.GetUtcNow().UtcDateTime;
        await _db.SaveChangesAsync(cancellationToken);
    }

    private static KandidatenkaartMapDto BuildPreviewMap(
        Guid? vacancyId,
        int minMatchPercent,
        int travelKm,
        string? availabilityFilter,
        string? hoursFilter,
        KandidatenkaartAccessDto access)
    {
        var blurred = KandidatenkaartDemoSeed.DemoAreas
            .Select(a =>
            {
                var count = a.Count;
                if (minMatchPercent >= 80 && travelKm <= 15)
                {
                    count = Math.Min(count, 12);
                }

                if (string.Equals(hoursFilter, "32+", StringComparison.OrdinalIgnoreCase))
                {
                    count = (int)Math.Round(count * 0.35, MidpointRounding.AwayFromZero);
                }

                if (string.Equals(availabilityFilter, "Later", StringComparison.OrdinalIgnoreCase))
                {
                    count = (int)Math.Round(count * 0.5, MidpointRounding.AwayFromZero);
                }

                return new KandidatenkaartAreaDto(
                    a.Key,
                    a.Label,
                    count,
                    Math.Max(0, count / 4),
                    Math.Max(0, count / 3),
                    a.Lat,
                    a.Lng,
                    BuildRing(a.Lat, a.Lng),
                    true);
            })
            .ToList();

        var visible = blurred.Where(a => a.CandidateCount >= KandidatenkaartRules.MinCandidatesPerArea).ToList();
        var below = visible.Count == 0 && blurred.Count > 0;

        const double demoOriginLat = 52.1561;
        const double demoOriginLng = 5.3878;

        return new KandidatenkaartMapDto(
            access,
            vacancyId,
            minMatchPercent,
            travelKm,
            availabilityFilter,
            hoursFilter,
            visible,
            below,
            below ? "Met deze filters zijn er geen gebieden met minimaal 30 kandidaten. Probeer een lagere matchscore of grotere reisafstand." : null,
            demoOriginLat,
            demoOriginLng);
    }

    private async Task<(double Lat, double Lng)> ResolveVacancyOriginAsync(
        Guid? vacancyId,
        CancellationToken cancellationToken)
    {
        const double fallbackLat = 52.1561;
        const double fallbackLng = 5.3878;
        if (vacancyId is null)
        {
            return (fallbackLat, fallbackLng);
        }

        var vacancy = await _db.Vacancies.AsNoTracking()
            .Include(v => v.Company)
            .FirstOrDefaultAsync(v => v.Id == vacancyId.Value, cancellationToken);
        var point = vacancy?.Location ?? vacancy?.Company?.Location;
        if (point is null)
        {
            return (fallbackLat, fallbackLng);
        }

        return (point.Latitude, point.Longitude);
    }

    private static (int Eligible, int BelowMatch, int WeeklyCap, int PreferenceOff) BuildDemoInviteBreakdown(
        KandidatenkaartAreaDto area,
        int minMatchPercent)
    {
        var total = area.CandidateCount;
        var inviteMin = Math.Max(minMatchPercent, KandidatenkaartRules.MinMatchPercentForInvite);
        var atOrAboveInvite = inviteMin >= 80
            ? area.CountAbove80
            : area.CountAbove80 + area.Count70To79;
        atOrAboveInvite = Math.Clamp(atOrAboveInvite, 0, total);

        var weeklyCap = Math.Min(4, Math.Max(0, atOrAboveInvite / 6));
        var eligible = Math.Max(0, atOrAboveInvite - weeklyCap);
        var below = Math.Max(0, total - atOrAboveInvite);
        var preferenceOff = Math.Max(0, total - eligible - below - weeklyCap);

        var sum = eligible + below + weeklyCap + preferenceOff;
        if (sum > total)
        {
            var over = sum - total;
            eligible = Math.Max(0, eligible - over);
        }
        else if (sum < total)
        {
            below += total - sum;
        }

        return (eligible, below, weeklyCap, preferenceOff);
    }

    private async Task<KandidatenkaartCandidateAggregator.AggregationResult> LoadAggregationAsync(
        Guid vacancyId,
        int minMatchPercent,
        int travelKm,
        string? availabilityFilter,
        string? hoursFilter,
        CancellationToken cancellationToken)
    {
        var key = (vacancyId, minMatchPercent, travelKm, availabilityFilter, hoursFilter);
        if (_cachedAggregation is not null && _cachedAggregationKey == key)
        {
            return _cachedAggregation;
        }

        var vacancy = await _db.Vacancies.AsNoTracking()
            .Include(v => v.Company)
            .FirstOrDefaultAsync(v => v.Id == vacancyId, cancellationToken)
            ?? throw new KandidatenkaartException("vacancy_not_found", "Vacature niet gevonden.", 404);

        var result = await _aggregator.BuildAsync(
            vacancy,
            minMatchPercent,
            travelKm,
            availabilityFilter,
            hoursFilter,
            _clock,
            cancellationToken);
        _cachedAggregation = result;
        _cachedAggregationKey = key;
        return result;
    }

    private async Task<IReadOnlyList<KandidatenkaartAreaDto>> BuildAreasAsync(
        Guid vacancyId,
        int minMatchPercent,
        int travelKm,
        string? availabilityFilter,
        string? hoursFilter,
        CancellationToken cancellationToken)
    {
        var aggregation = await LoadAggregationAsync(
            vacancyId,
            minMatchPercent,
            travelKm,
            availabilityFilter,
            hoursFilter,
            cancellationToken);
        return aggregation.AllAreas;
    }

    private async Task<KandidatenkaartAreaDto?> ResolveAreaAsync(
        Guid vacancyId,
        string areaKey,
        int minMatchPercent,
        int travelKm,
        string? availabilityFilter,
        string? hoursFilter,
        CancellationToken cancellationToken)
    {
        var areas = await BuildAreasAsync(
            vacancyId,
            minMatchPercent,
            travelKm,
            availabilityFilter,
            hoursFilter,
            cancellationToken);
        var found = areas.FirstOrDefault(a => string.Equals(a.AreaKey, areaKey, StringComparison.OrdinalIgnoreCase));
        if (found is not null)
        {
            return found;
        }

        return TryResolveDemoArea(areaKey);
    }

    private KandidatenkaartAreaDto? TryResolveDemoArea(string areaKey)
    {
        if (!_env.IsDevelopment())
        {
            return null;
        }

        foreach (var demo in KandidatenkaartDemoSeed.DemoAreas)
        {
            if (!string.Equals(demo.Key, areaKey, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            return new KandidatenkaartAreaDto(
                demo.Key,
                demo.Label,
                demo.Count,
                Math.Max(1, demo.Count / 4),
                Math.Max(1, demo.Count / 3),
                demo.Lat,
                demo.Lng,
                BuildRing(demo.Lat, demo.Lng),
                true);
        }

        return null;
    }

    private async Task<(int Eligible, int BelowMatch, int WeeklyCap, int PreferenceOff)> CountEligibleCandidatesAsync(
        Guid vacancyId,
        string areaKey,
        int minMatchPercent,
        int travelKm,
        string? availabilityFilter,
        string? hoursFilter,
        CancellationToken cancellationToken)
    {
        var aggregation = await LoadAggregationAsync(
            vacancyId,
            minMatchPercent,
            travelKm,
            availabilityFilter,
            hoursFilter,
            cancellationToken);
        if (!aggregation.ByAreaKey.TryGetValue(areaKey, out var inArea))
        {
            return (0, 0, 0, 0);
        }

        var inviteMin = Math.Max(minMatchPercent, KandidatenkaartRules.MinMatchPercentForInvite);
        var weekStart = _clock.GetUtcNow().UtcDateTime.AddDays(-7);
        var below = 0;
        var weeklyCap = 0;
        var preferenceOff = 0;
        var eligible = 0;

        foreach (var c in inArea)
        {
            if (c.MatchPercent < inviteMin)
            {
                below++;
                continue;
            }

            var pref = await _db.CandidateKandidatenkaartSettings.AsNoTracking()
                .Where(s => s.UserId == c.UserId)
                .Select(s => s.InvitePreference)
                .FirstOrDefaultAsync(cancellationToken);
            if (pref == KandidatenkaartInvitePreference.Off)
            {
                preferenceOff++;
                continue;
            }

            var sentThisWeek = await _db.KandidatenkaartInviteDeliveries.AsNoTracking()
                .CountAsync(d => d.CandidateUserId == c.UserId
                                 && d.SentAtUtc >= weekStart
                                 && d.Status != KandidatenkaartInviteDeliveryStatus.SkippedPreference, cancellationToken);
            var cap = pref == KandidatenkaartInvitePreference.Max
                ? KandidatenkaartRules.MaxInvitesPerCandidatePerWeek
                : 2;
            if (sentThisWeek >= cap)
            {
                weeklyCap++;
                continue;
            }

            eligible++;
        }

        return (eligible, below, weeklyCap, preferenceOff);
    }

    private async Task<List<(Guid UserId, int Match)>> LoadEligibleCandidateIdsAsync(
        Guid vacancyId,
        string areaKey,
        int minMatchPercent,
        int travelKm,
        string? availabilityFilter,
        string? hoursFilter,
        CancellationToken cancellationToken)
    {
        var aggregation = await LoadAggregationAsync(
            vacancyId,
            minMatchPercent,
            travelKm,
            availabilityFilter,
            hoursFilter,
            cancellationToken);
        if (!aggregation.ByAreaKey.TryGetValue(areaKey, out var inArea))
        {
            return [];
        }

        var inviteMin = Math.Max(minMatchPercent, KandidatenkaartRules.MinMatchPercentForInvite);
        var weekStart = _clock.GetUtcNow().UtcDateTime.AddDays(-7);
        var result = new List<(Guid, int)>();

        foreach (var c in inArea)
        {
            if (c.MatchPercent < inviteMin)
            {
                continue;
            }

            var pref = await _db.CandidateKandidatenkaartSettings.AsNoTracking()
                .Where(s => s.UserId == c.UserId)
                .Select(s => s.InvitePreference)
                .FirstOrDefaultAsync(cancellationToken);
            if (pref == KandidatenkaartInvitePreference.Off)
            {
                continue;
            }

            var sentThisWeek = await _db.KandidatenkaartInviteDeliveries.AsNoTracking()
                .CountAsync(d => d.CandidateUserId == c.UserId
                                 && d.SentAtUtc >= weekStart
                                 && d.Status != KandidatenkaartInviteDeliveryStatus.SkippedPreference, cancellationToken);
            var cap = pref == KandidatenkaartInvitePreference.Max
                ? KandidatenkaartRules.MaxInvitesPerCandidatePerWeek
                : 2;
            if (sentThisWeek >= cap)
            {
                continue;
            }

            result.Add((c.UserId, c.MatchPercent));
        }

        return result;
    }

    private static IReadOnlyList<IReadOnlyList<double>> BuildRing(double lat, double lng)
    {
        var ring = new List<double[]>();
        for (var i = 0; i <= 6; i++)
        {
            var angle = i * Math.PI / 3;
            ring.Add([lng + Math.Cos(angle) * 0.04, lat + Math.Sin(angle) * 0.03]);
        }

        return ring.Select(r => (IReadOnlyList<double>)r).ToList();
    }

    private async Task EnsureFeatureEnabledAsync(CancellationToken cancellationToken)
    {
        if (!await IsFeatureEnabledAsync(cancellationToken))
        {
            throw new KandidatenkaartException("feature_disabled", "Kandidatenkaart is uitgeschakeld.", 404);
        }
    }

    private async Task EnsurePaidAsync(
        IReadOnlyList<Guid> companyIds,
        ClaimsPrincipal employer,
        CancellationToken cancellationToken)
    {
        foreach (var id in companyIds)
        {
            await EnsureCompanyAccessAsync(employer, id, cancellationToken);
            if (!await HasPaidAccessAsync(id, cancellationToken))
            {
                throw new KandidatenkaartException("payment_required", "Marktinzicht vereist een actief pakket.", 402);
            }
        }
    }

    private async Task<bool> HasPaidAccessAsync(Guid companyId, CancellationToken cancellationToken)
    {
        var company = await _db.Companies.AsNoTracking().FirstOrDefaultAsync(c => c.Id == companyId, cancellationToken);
        if (company is null)
        {
            return false;
        }

        if (_env.IsDevelopment())
        {
            return true;
        }

        if (company.IsTestData)
        {
            return company.KandidatenkaartActiveUntilUtc is null
                   || company.KandidatenkaartActiveUntilUtc > _clock.GetUtcNow().UtcDateTime;
        }

        // TODO: tie to employer subscription SKU when billing catalog exposes kandidatenkaart.
        return company.KandidatenkaartActiveUntilUtc > _clock.GetUtcNow().UtcDateTime;
    }

    private async Task EnsureCompanyAccessAsync(
        ClaimsPrincipal employer,
        Guid companyId,
        CancellationToken cancellationToken)
    {
        if (!await _authz.CanAccessCompanyAsync(employer, companyId, cancellationToken))
        {
            throw new KandidatenkaartException("forbidden", "Geen toegang.", 403);
        }
    }

    private static KandidatenkaartInvitePreferenceView MapPreference(KandidatenkaartInvitePreference pref)
        => pref switch
        {
            KandidatenkaartInvitePreference.Off => KandidatenkaartInvitePreferenceView.Off,
            KandidatenkaartInvitePreference.Max => KandidatenkaartInvitePreferenceView.Max,
            _ => KandidatenkaartInvitePreferenceView.Normal
        };
}

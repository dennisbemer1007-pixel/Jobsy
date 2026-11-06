using System.Text.Json;
using Jobsy.Core.Contracts;
using Jobsy.Core.Entities;
using Jobsy.Core.Enums;
using Jobsy.Core.Interfaces;
using Jobsy.Core.Rules;
using Jobsy.Core.ValueObjects;
using Jobsy.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Jobsy.Infrastructure.Services;

/// <summary>
/// Scores candidates against a vacancy, applies kandidatenkaart filters, and clusters by home location.
/// </summary>
public sealed class KandidatenkaartCandidateAggregator
{
    private const double GridStepDegrees = 0.06;
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    private readonly JobsyDbContext _db;
    private readonly IProfileVacancyMatchService _matches;
    private readonly IRoutingService _routing;

    public KandidatenkaartCandidateAggregator(
        JobsyDbContext db,
        IProfileVacancyMatchService matches,
        IRoutingService routing)
    {
        _db = db;
        _matches = matches;
        _routing = routing;
    }

    public sealed record ScoredCandidate(
        Guid UserId,
        int MatchPercent,
        GeoPoint Home,
        string AreaKey,
        bool ActiveThisWeek);

    public sealed record AggregationResult(
        IReadOnlyList<KandidatenkaartAreaDto> AllAreas,
        IReadOnlyDictionary<string, List<ScoredCandidate>> ByAreaKey);

    public async Task<AggregationResult> BuildAsync(
        Vacancy vacancy,
        int minMatchPercent,
        int travelKm,
        string? availabilityFilter,
        string? hoursFilter,
        TimeProvider clock,
        CancellationToken cancellationToken)
    {
        var destination = vacancy.Location ?? vacancy.Company.Location
                          ?? throw new InvalidOperationException("Vacancy has no location.");

        var record = VacancyDiscoveryIndex.ToRecord(vacancy);
        var transport = vacancy.RequiredTransport;

        var candidates = await _db.Users.AsNoTracking()
            .Where(u => u.Role == UserRole.Candidate && u.IsActive && u.OpenForWork && u.HomeLocation != null
                        && u.TalentPoolConsentAt != null)
            .Take(2500)
            .ToListAsync(cancellationToken);

        candidates = ProactiveTalentAvailabilityRules.FilterEligible(candidates).ToList();
        if (candidates.Count == 0)
        {
            return new AggregationResult([], new Dictionary<string, List<ScoredCandidate>>());
        }

        var userIds = candidates.Select(c => c.Id).ToList();
        var prefsOff = await _db.CandidateKandidatenkaartSettings.AsNoTracking()
            .Where(s => userIds.Contains(s.UserId) && s.InvitePreference == KandidatenkaartInvitePreference.Off)
            .Select(s => s.UserId)
            .ToListAsync(cancellationToken);
        var offSet = prefsOff.ToHashSet();

        var competencyRows = await _db.CandidateCompetencies.AsNoTracking()
            .Where(c => userIds.Contains(c.UserId) && c.Status == CandidateCompetencyStatuses.Completed)
            .ToListAsync(cancellationToken);
        var competencies = competencyRows.GroupBy(c => c.UserId).ToDictionary(g => g.Key, g => g.First());
        var careerRows = await _db.CandidateCareerInterests.AsNoTracking()
            .Where(c => userIds.Contains(c.UserId) && c.Status == CandidateCompetencyStatuses.Completed)
            .ToListAsync(cancellationToken);
        var careers = careerRows.GroupBy(c => c.UserId).ToDictionary(g => g.Key, g => g.First());

        var weekAgo = clock.GetUtcNow().UtcDateTime.AddDays(-7);
        var scored = new List<ScoredCandidate>();

        foreach (var user in candidates)
        {
            if (offSet.Contains(user.Id))
            {
                continue;
            }

            if (!competencies.ContainsKey(user.Id) && !careers.ContainsKey(user.Id))
            {
                continue;
            }

            if (!PassesAvailabilityFilter(user, availabilityFilter))
            {
                continue;
            }

            var prefs = DeserializePrefs(user.PreferencesJson);
            if (!PassesHoursFilter(prefs, hoursFilter))
            {
                continue;
            }

            if (!GeoDistance.IsWithinKm(destination, user.HomeLocation!, travelKm))
            {
                continue;
            }

            var context = await _matches.TryLoadForUserIdAsync(user.Id, cancellationToken);
            if (context is null)
            {
                continue;
            }

            int? travelMinutes = null;
            var route = await _routing.GetRouteAsync(
                destination.Latitude,
                destination.Longitude,
                user.HomeLocation!.Latitude,
                user.HomeLocation.Longitude,
                transport,
                cancellationToken);
            travelMinutes = (int)Math.Round(route.DurationSeconds / 60.0, MidpointRounding.AwayFromZero);

            var batch = new List<(VacancyDiscoveryRecord Record, int? TravelMinutes)> { (record, travelMinutes) };
            var scores = await _matches.ScoreAsync(context, batch, cancellationToken);
            if (!scores.TryGetValue(vacancy.Id, out var match)
                || !match.Core.LegalEligible
                || match.TotalPercent < minMatchPercent)
            {
                continue;
            }

            var areaKey = GridAreaKey(user.HomeLocation!);
            scored.Add(new ScoredCandidate(
                user.Id,
                match.TotalPercent,
                user.HomeLocation!,
                areaKey,
                user.LastLoginAtUtc >= weekAgo));
        }

        var grouped = scored.GroupBy(s => s.AreaKey, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.ToList(), StringComparer.OrdinalIgnoreCase);

        var areas = new List<KandidatenkaartAreaDto>();
        foreach (var (key, list) in grouped)
        {
            var centerLat = list.Average(x => x.Home.Latitude);
            var centerLng = list.Average(x => x.Home.Longitude);
            areas.Add(new KandidatenkaartAreaDto(
                key,
                LabelFor(centerLat, centerLng),
                list.Count,
                list.Count(x => x.MatchPercent >= 80),
                list.Count(x => x.MatchPercent is >= 70 and < 80),
                centerLat,
                centerLng,
                BuildRing(centerLat, centerLng, list),
                list.Any(x => x.ActiveThisWeek)));
        }

        areas.Sort((a, b) => b.CandidateCount.CompareTo(a.CandidateCount));
        return new AggregationResult(areas, grouped);
    }

    public static string GridAreaKey(GeoPoint home)
    {
        var latIdx = (int)Math.Round(home.Latitude / GridStepDegrees, MidpointRounding.AwayFromZero);
        var lngIdx = (int)Math.Round(home.Longitude / GridStepDegrees, MidpointRounding.AwayFromZero);
        return $"kk-{latIdx}-{lngIdx}";
    }

    private static string LabelFor(double lat, double lng)
    {
        var best = KandidatenkaartDemoSeed.DemoAreas
            .Select(a => (a.Label, Km: HaversineKm(lat, lng, a.Lat, a.Lng)))
            .OrderBy(x => x.Km)
            .FirstOrDefault();
        if (best.Label is not null && best.Km <= 22)
        {
            return best.Label;
        }

        return $"Regio ({lat:F2}, {lng:F2})";
    }

    private static double HaversineKm(double lat1, double lng1, double lat2, double lng2)
    {
        var a = new GeoPoint(lat1, lng1);
        var b = new GeoPoint(lat2, lng2);
        return GeoDistance.HaversineKm(a, b);
    }

    private static bool PassesAvailabilityFilter(User user, string? filter)
    {
        if (string.IsNullOrWhiteSpace(filter))
        {
            return true;
        }

        var immediate = AvailabilityStartRules.IsImmediate(user.AvailableFromDate);
        if (filter.Equals("Per direct", StringComparison.OrdinalIgnoreCase))
        {
            return immediate;
        }

        if (filter.Equals("Binnen 1 maand", StringComparison.OrdinalIgnoreCase))
        {
            if (immediate)
            {
                return false;
            }

            var start = user.AvailableFromDate ?? AvailabilityStartRules.TodayUtc;
            return start <= AvailabilityStartRules.TodayUtc.AddDays(30);
        }

        if (filter.Equals("Later", StringComparison.OrdinalIgnoreCase))
        {
            if (immediate)
            {
                return false;
            }

            var start = user.AvailableFromDate ?? AvailabilityStartRules.TodayUtc;
            return start > AvailabilityStartRules.TodayUtc.AddDays(30);
        }

        return true;
    }

    private static bool PassesHoursFilter(CandidatePreferencesDto prefs, string? filter)
    {
        if (string.IsNullOrWhiteSpace(filter))
        {
            return true;
        }

        var min = prefs.MinHoursPerWeek ?? 0;
        var max = prefs.MaxHoursPerWeek ?? 40;
        return filter switch
        {
            "<16" => max < 16 || (min < 16 && max <= 16),
            "16–24" or "16-24" => max >= 16 && min <= 24,
            "24–32" or "24-32" => max >= 24 && min <= 32,
            "32+" => min >= 32 || max >= 32,
            _ => true
        };
    }

    private static CandidatePreferencesDto DeserializePrefs(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return new CandidatePreferencesDto([], null, null);
        }

        try
        {
            return JsonSerializer.Deserialize<CandidatePreferencesDto>(json, JsonOptions)
                   ?? new CandidatePreferencesDto([], null, null);
        }
        catch (JsonException)
        {
            return new CandidatePreferencesDto([], null, null);
        }
    }

    private static IReadOnlyList<IReadOnlyList<double>> BuildRing(
        double centerLat,
        double centerLng,
        List<ScoredCandidate> members)
    {
        var maxKm = members.Max(m => GeoDistance.HaversineKm(
            new GeoPoint(centerLat, centerLng),
            m.Home));
        var radiusDeg = Math.Max(0.025, Math.Min(0.12, maxKm / 111.0));
        var ring = new List<double[]>();
        for (var i = 0; i <= 8; i++)
        {
            var angle = i * Math.PI / 4;
            ring.Add([centerLng + Math.Cos(angle) * radiusDeg, centerLat + Math.Sin(angle) * radiusDeg * 0.85]);
        }

        return ring.Select(r => (IReadOnlyList<double>)r).ToList();
    }
}

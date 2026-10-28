using Jobsy.Core.Rules;
using Jobsy.Web.Models;

namespace Jobsy.Web.Services;

/// <summary>
/// Loads and relevance-filters vacancies for the Match swipe stack (education + travel/transport).
/// Banenkaart discovery stays separate and unfiltered by this service.
/// </summary>
public sealed class MatchVacancyService
{
    private readonly JobsyApiClient _api;

    public MatchVacancyService(JobsyApiClient api)
    {
        _api = api;
    }

    public async Task<IReadOnlyList<VacancyListItem>> GetRelevantSwipeVacanciesAsync(
        MatchProfileGateViewModel gate,
        int take = 24,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(gate);
        if (!gate.IsProfileComplete)
        {
            return [];
        }

        var transport = string.IsNullOrWhiteSpace(gate.PreferredTransport)
            ? "Auto"
            : gate.PreferredTransport!;
        var maxMinutes = gate.MaxTravelMinutes is int m && m > 0 ? m : 45;

        VacancyListPage page;
        try
        {
            page = await _api.DiscoverVacanciesPageAsync(
                originLat: gate.HomeLatitude,
                originLng: gate.HomeLongitude,
                transport: transport,
                maxMinutes: maxMinutes,
                radiusKm: null,
                take: Math.Max(take * 2, 24),
                ct: ct);
        }
        catch
        {
            return [];
        }

        return FilterRelevant(page.Items, gate, take, page.OutsideTravelFallback);
    }

    public static IReadOnlyList<VacancyListItem> FilterRelevant(
        IEnumerable<VacancyListItem> vacancies,
        MatchProfileGateViewModel gate,
        int take = 24,
        bool outsideTravelFallback = false)
    {
        ArgumentNullException.ThrowIfNull(vacancies);
        ArgumentNullException.ThrowIfNull(gate);

        var list = vacancies.ToList();
        var filtered = list
            .Where(v => MatchVacancyRelevance.IsRelevant(
                gate.Educations,
                gate.MaxTravelMinutes,
                gate.PreferredTransport,
                v.RequiredEducation,
                v.TravelMinutes,
                v.RequiredTransport))
            .Take(take)
            .ToList();

        if (filtered.Count > 0 || !outsideTravelFallback || list.Count == 0)
        {
            return filtered;
        }

        // Discovery already widened to nearest reachable pins; keep education/transport fit only.
        return list
            .Where(v => MatchVacancyRelevance.IsRelevant(
                gate.Educations,
                candidateMaxTravelMinutes: null,
                gate.PreferredTransport,
                v.RequiredEducation,
                v.TravelMinutes,
                v.RequiredTransport))
            .Take(take)
            .ToList();
    }
}

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

        IReadOnlyList<VacancyListItem> raw;
        try
        {
            raw = await _api.DiscoverVacanciesAsync(
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

        return FilterRelevant(raw, gate, take);
    }

    public static IReadOnlyList<VacancyListItem> FilterRelevant(
        IEnumerable<VacancyListItem> vacancies,
        MatchProfileGateViewModel gate,
        int take = 24)
    {
        ArgumentNullException.ThrowIfNull(vacancies);
        ArgumentNullException.ThrowIfNull(gate);

        return vacancies
            .Where(v => MatchVacancyRelevance.IsRelevant(
                gate.Educations,
                gate.MaxTravelMinutes,
                gate.PreferredTransport,
                v.RequiredEducation,
                v.TravelMinutes,
                v.RequiredTransport))
            .Take(take)
            .ToList();
    }
}

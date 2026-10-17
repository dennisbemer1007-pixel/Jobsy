using Jobsy.Api.Security;
using Jobsy.Core.Contracts;

namespace Jobsy.Api.Vacancies;

/// <summary>
/// Bounds how many vacancies receive per-request match scoring (pins + discover).
/// Match math is unchanged for scored rows; only out-of-cap rows are omitted from the response.
/// </summary>
public static class VacancyMatchScoreCap
{
    public static List<T> LimitCandidates<T>(
        IReadOnlyList<T> candidates,
        GeoBbox? bbox,
        int? zoom,
        Func<T, VacancyDiscoveryRecord> recordSelector)
    {
        if (candidates.Count == 0)
        {
            return [];
        }

        var pinCap = bbox is { IsValid: true }
            ? PublicApiLimits.MaxPinsPerBbox
            : PublicApiLimits.MaxPinsWithoutBbox;
        if (candidates.Count <= pinCap)
        {
            return candidates.ToList();
        }

        var capped = VacancyDiscoveryBbox.CapPins(
            candidates.Select(recordSelector).ToList(),
            bbox,
            pinCap,
            zoom);
        var allowed = capped.Select(r => r.Id).ToHashSet();
        return candidates.Where(c => allowed.Contains(recordSelector(c).Id)).ToList();
    }
}

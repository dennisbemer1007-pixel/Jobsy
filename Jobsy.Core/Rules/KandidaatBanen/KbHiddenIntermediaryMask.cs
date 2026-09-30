using Jobsy.Core.Entities;
using Jobsy.Core.ValueObjects;

namespace Jobsy.Core.Rules.KandidaatBanen;

/// <summary>
/// Candidate-side mask for uitzendbureau hidden mode when intermediair 03 is absent.
/// Pin / mini map / travel use the bureau vestiging; Route/Street View and end-client
/// identity never reach the candidate client.
/// </summary>
/// <remarks>
/// KB-FALLBACK(A): superseded by intermediair 03 (<c>IntermediaryPublicIdentity</c>).
/// Delete this type when that stack lands.
/// </remarks>
public static class KbHiddenIntermediaryMask
{
    public static bool IsHidden(Vacancy vacancy) =>
        !vacancy.ShowClientAddressOnMap
        && (vacancy.IntermediaryCompanyId is not null || vacancy.IntermediaryCompany is not null);

    public static bool IsHidden(Guid? intermediaryCompanyId, bool showClientAddressOnMap) =>
        intermediaryCompanyId is not null && !showClientAddressOnMap;

    /// <summary>
    /// Hidden when an intermediary company object is present and the map flag is masked
    /// (used by <see cref="IntermediaryVacancyRules.ResolvePublicDisplay"/> when FK may be unset in tests).
    /// </summary>
    public static bool IsMaskedByIntermediary(Company? intermediary, bool showClientAddressOnMap) =>
        intermediary is not null && !showClientAddressOnMap;

    /// <summary>
    /// Bureau organisation used for public pin/travel: walk to the loaded root parent when present.
    /// </summary>
    public static Company? ResolveBureauOrganisation(Company? intermediary)
    {
        if (intermediary is null)
        {
            return null;
        }

        var current = intermediary;
        var guard = 0;
        while (current.ParentCompany is not null && guard++ < 8)
        {
            current = current.ParentCompany;
        }

        return current;
    }

    /// <summary>
    /// Bureau pin coordinates. Null when the bureau has no location — no pin, no travel
    /// (never fall back to the end-client workplace).
    /// </summary>
    public static GeoPoint? ResolveBureauLocation(Company? intermediary)
    {
        var org = ResolveBureauOrganisation(intermediary) ?? intermediary;
        var loc = org?.Location;
        if (loc is null)
        {
            return null;
        }

        if (!double.IsFinite(loc.Latitude) || !double.IsFinite(loc.Longitude))
        {
            return null;
        }

        if (loc.Latitude == 0 && loc.Longitude == 0)
        {
            return null;
        }

        return loc;
    }

    /// <summary>
    /// True when candidate payloads must omit end-client KvK / vestiging / identity fields.
    /// </summary>
    public static bool RedactClientPublicPaths(Guid? intermediaryCompanyId, bool showClientAddressOnMap) =>
        IsHidden(intermediaryCompanyId, showClientAddressOnMap);

    /// <summary>Route and Street View must not be offered in hidden mode.</summary>
    public static bool HideRouteAndStreetView(Guid? intermediaryCompanyId, bool showClientAddressOnMap) =>
        IsHidden(intermediaryCompanyId, showClientAddressOnMap);
}

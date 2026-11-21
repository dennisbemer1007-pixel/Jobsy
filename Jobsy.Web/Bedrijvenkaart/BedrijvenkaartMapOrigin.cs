using Jobsy.Core.Authorization;
using Jobsy.Web.Auth;
using Jobsy.Web.Models;
using Jobsy.Web.Services;
using Microsoft.AspNetCore.Http;

namespace Jobsy.Web.Bedrijvenkaart;

internal static class BedrijvenkaartMapOrigin
{
    internal sealed record Resolved(double Lat, double Lng, bool FromProfile);

    internal static async Task<Resolved?> TryResolveAsync(
        HttpContext? http,
        JobsyApiClient api,
        CancellationToken ct)
    {
        if (http is not null
            && TryParseQueryOrigin(http.Request, out var qLat, out var qLng))
        {
            return new Resolved(qLat, qLng, false);
        }

        if (http?.User?.Identity?.IsAuthenticated == true)
        {
            if (RoleClaimMatching.HasAnyRole(http.User, JobsyRoles.EmployerRoles))
            {
                try
                {
                    var companies = await api.GetMyCompaniesAsync(ct);
                    CompanySummary? branch = null;
                    foreach (var c in companies)
                    {
                        if (c.Latitude is double la && c.Longitude is double lo && IsValidCoord(la, lo))
                        {
                            branch = c;
                            break;
                        }
                    }

                    branch ??= companies.Count > 0 ? companies[0] : null;
                    if (branch?.Latitude is double bLat
                        && branch.Longitude is double bLng
                        && IsValidCoord(bLat, bLng))
                    {
                        return new Resolved(bLat, bLng, true);
                    }
                }
                catch
                {
                    // optional
                }
            }

            try
            {
                var profile = await api.GetMyProfileAsync(ct);
                if (profile?.HomeLatitude is double hLat
                    && profile.HomeLongitude is double hLng
                    && IsValidCoord(hLat, hLng))
                {
                    return new Resolved(hLat, hLng, true);
                }
            }
            catch
            {
                // optional
            }
        }

        return null;
    }

    internal static bool TryParseQueryOrigin(HttpRequest request, out double lat, out double lng)
    {
        lat = 0;
        lng = 0;
        if (!request.Query.TryGetValue("originLat", out var latRaw)
            || !request.Query.TryGetValue("originLng", out var lngRaw))
        {
            return false;
        }

        if (!double.TryParse(latRaw.ToString(), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out lat)
            || !double.TryParse(lngRaw.ToString(), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out lng))
        {
            return false;
        }

        return IsValidCoord(lat, lng);
    }

    internal static bool IsValidCoord(double lat, double lng)
        => lat >= -90 && lat <= 90 && lng >= -180 && lng <= 180
           && double.IsFinite(lat) && double.IsFinite(lng);
}

using Jobsy.Web.Services;
using Microsoft.AspNetCore.Http;

namespace Jobsy.Web.Bedrijvenkaart;

internal static class BedrijvenkaartMapOrigin
{
    internal const double FallbackLat = 52.15;
    internal const double FallbackLng = 5.2913;

    internal sealed record Resolved(double Lat, double Lng, bool FromProfile);

    internal static async Task<Resolved> ResolveAsync(
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

        return new Resolved(FallbackLat, FallbackLng, false);
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
        => lat is >= -90 and <= 90 && lng is >= -180 and <= 180
           && double.IsFinite(lat) && double.IsFinite(lng);
}

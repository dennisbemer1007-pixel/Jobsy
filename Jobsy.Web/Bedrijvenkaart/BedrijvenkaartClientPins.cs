using Jobsy.Web.Services;

namespace Jobsy.Web.Bedrijvenkaart;

internal static class BedrijvenkaartClientPins
{
    internal const string Path = "/api/bedrijvenkaart-client/pins";

    internal static async Task<IResult> GetAsync(
        HttpContext http,
        JobsyApiClient api,
        CancellationToken cancellationToken)
    {
        var radiusKm = 25;
        if (int.TryParse(http.Request.Query["radiusKm"], out var parsed) && parsed is 10 or 25 or 40)
        {
            radiusKm = parsed;
        }

        double lat;
        double lng;
        if (double.TryParse(http.Request.Query["lat"], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var qLat)
            && double.TryParse(http.Request.Query["lng"], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var qLng)
            && BedrijvenkaartMapOrigin.IsValidCoord(qLat, qLng))
        {
            lat = qLat;
            lng = qLng;
        }
        else
        {
            var origin = await BedrijvenkaartMapOrigin.ResolveAsync(http, api, cancellationToken);
            lat = origin.Lat;
            lng = origin.Lng;
        }

        try
        {
            var pins = await api.GetCompanyMapPinsAsync(lat, lng, radiusKm, cancellationToken);
            return TypedResults.Ok(pins);
        }
        catch
        {
            return TypedResults.StatusCode(StatusCodes.Status502BadGateway);
        }
    }
}

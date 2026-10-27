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

        try
        {
            var pins = await api.GetCompanyMapPinsAsync(null, null, radiusKm, cancellationToken);
            return TypedResults.Ok(pins);
        }
        catch
        {
            return TypedResults.StatusCode(StatusCodes.Status502BadGateway);
        }
    }
}

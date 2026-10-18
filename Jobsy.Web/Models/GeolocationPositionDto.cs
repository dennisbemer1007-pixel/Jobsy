namespace Jobsy.Web.Models;

/// <summary>Browser geolocation payload from <c>jobsyGeo.requestLocation</c>.</summary>
public sealed class GeolocationPositionDto
{
    public double lat { get; set; }
    public double lng { get; set; }
    public double? accuracyMeters { get; set; }
}

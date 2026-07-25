namespace Jobsy.Core.ValueObjects;

/// <summary>
/// WGS84 geographic point (latitude/longitude). Persistence maps this to PostGIS via Infrastructure.
/// </summary>
public sealed class GeoPoint : IEquatable<GeoPoint>
{
    public double Latitude { get; init; }
    public double Longitude { get; init; }

    public GeoPoint()
    {
    }

    public GeoPoint(double latitude, double longitude)
    {
        Latitude = latitude;
        Longitude = longitude;
    }

    public bool Equals(GeoPoint? other) =>
        other is not null
        && Latitude.Equals(other.Latitude)
        && Longitude.Equals(other.Longitude);

    public override bool Equals(object? obj) => obj is GeoPoint other && Equals(other);

    public override int GetHashCode() => HashCode.Combine(Latitude, Longitude);
}

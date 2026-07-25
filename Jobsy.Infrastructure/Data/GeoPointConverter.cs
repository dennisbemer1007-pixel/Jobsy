using Jobsy.Core.ValueObjects;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using NetTopologySuite;
using NetTopologySuite.Geometries;

namespace Jobsy.Infrastructure.Data;

internal sealed class GeoPointConverter : ValueConverter<GeoPoint, Point>
{
    private static readonly GeometryFactory Factory =
        NtsGeometryServices.Instance.CreateGeometryFactory(srid: 4326);

    public GeoPointConverter()
        : base(
            geo => Factory.CreatePoint(new Coordinate(geo.Longitude, geo.Latitude)),
            point => new GeoPoint(point.Y, point.X))
    {
    }
}

/// <summary>Nullable variant for optional candidate home locations.</summary>
internal sealed class NullableGeoPointConverter : ValueConverter<GeoPoint?, Point?>
{
    private static readonly GeometryFactory Factory =
        NtsGeometryServices.Instance.CreateGeometryFactory(srid: 4326);

    public NullableGeoPointConverter()
        : base(
            geo => geo == null ? null : Factory.CreatePoint(new Coordinate(geo.Longitude, geo.Latitude)),
            point => point == null ? null : new GeoPoint(point.Y, point.X))
    {
    }
}

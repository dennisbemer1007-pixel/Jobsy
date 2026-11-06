namespace Jobsy.Core.Interfaces;

/// <summary>Server-side address → coordinates (PDOK / Nominatim).</summary>
public interface IGeocodingService
{
    /// <summary>
    /// Geocode a free-text Dutch address. Returns null when no confident result is found.
    /// </summary>
    Task<GeocodingResult?> GeocodeAsync(
        string addressQuery,
        CancellationToken cancellationToken = default);
}

public sealed record GeocodingResult(
    double Latitude,
    double Longitude,
    string? Label = null);

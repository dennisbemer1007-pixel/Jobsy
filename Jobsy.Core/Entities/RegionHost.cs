namespace Jobsy.Core.Entities;

/// <summary>
/// Public regional site bound to a CNAME hostname (e.g. westland.lobsy.nl).
/// Drives branding, map focus and campaign analytics scope.
/// </summary>
public class RegionHost
{
    public Guid Id { get; set; }

    /// <summary>Normalized hostname without scheme/port, lowercase (e.g. westland.lobsy.nl).</summary>
    public string Hostname { get; set; } = string.Empty;

    /// <summary>Friendly region label shown in UI (e.g. Westland).</summary>
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>Optional tagline / slogan for the regional landing.</summary>
    public string? Slogan { get; set; }

    /// <summary>Human-readable address used for map focus (from autocomplete).</summary>
    public string? AddressLabel { get; set; }

    public double? Latitude { get; set; }
    public double? Longitude { get; set; }

    /// <summary>Optional background image URL or path for regional hero branding.</summary>
    public string? BackgroundImageUrl { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
}

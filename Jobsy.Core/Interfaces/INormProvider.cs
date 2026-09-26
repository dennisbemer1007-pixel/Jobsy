using System.Text.Json.Serialization;

namespace Jobsy.Core.Interfaces;

/// <summary>
/// Provides Big Five research norm data (Johnson, 2014) so a candidate's own score can be
/// compared to a large reference group. Purely additive: when norms are unavailable the
/// caller must hide the comparison instead of guessing.
/// </summary>
public interface INormProvider
{
    /// <summary>True when the embedded norm dataset loaded successfully and is marked available.</summary>
    bool IsAvailable { get; }

    /// <summary>The parsed norm dataset, or null when unavailable.</summary>
    BigFiveNormSet? Norms { get; }

    /// <summary>
    /// Dutch band label for <paramref name="score"/> (0-100) against the p25/p75 of the given
    /// trait or facet key (e.g. "Conscientiousness" or "C1"). Returns null when norms are
    /// unavailable or the key is unknown.
    /// </summary>
    string? BandLabel(double score, string traitOrFacetKey);

    /// <summary>Mean (0-100) for the given trait or facet key, or null when unavailable/unknown.</summary>
    double? Mean(string traitOrFacetKey);

    /// <summary>
    /// One-line Dutch disclaimer explaining the source and limits of the comparison. Always
    /// available regardless of <see cref="IsAvailable"/>, so callers may show it once norms
    /// are confirmed available.
    /// </summary>
    string SourceDisclaimer { get; }
}

/// <summary>Root of the embedded Johnson (2014) IPIP-NEO-120 norm dataset.</summary>
public sealed class BigFiveNormSet
{
    [JsonPropertyName("available")]
    public bool Available { get; set; }

    [JsonPropertyName("instrument")]
    public string? Instrument { get; set; }

    [JsonPropertyName("source")]
    public string? Source { get; set; }

    [JsonPropertyName("doi")]
    public string? Doi { get; set; }

    [JsonPropertyName("data")]
    public string? Data { get; set; }

    [JsonPropertyName("country")]
    public string? Country { get; set; }

    [JsonPropertyName("group")]
    public string? Group { get; set; }

    [JsonPropertyName("n")]
    public int N { get; set; }

    [JsonPropertyName("collected")]
    public string? Collected { get; set; }

    [JsonPropertyName("scale")]
    public string? Scale { get; set; }

    [JsonPropertyName("traits")]
    public Dictionary<string, BigFiveTraitNorm> Traits { get; set; } = new(StringComparer.OrdinalIgnoreCase);
}

/// <summary>Norm statistics (0-100 scale) for one Big Five trait plus its six facets.</summary>
public sealed class BigFiveTraitNorm
{
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("mean")]
    public double Mean { get; set; }

    [JsonPropertyName("sd")]
    public double Sd { get; set; }

    [JsonPropertyName("p25")]
    public double P25 { get; set; }

    [JsonPropertyName("p50")]
    public double P50 { get; set; }

    [JsonPropertyName("p75")]
    public double P75 { get; set; }

    [JsonPropertyName("facets")]
    public Dictionary<string, BigFiveFacetNorm> Facets { get; set; } = new(StringComparer.OrdinalIgnoreCase);
}

/// <summary>Norm statistics (0-100 scale) for one IPIP-NEO facet.</summary>
public sealed class BigFiveFacetNorm
{
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("mean")]
    public double Mean { get; set; }

    [JsonPropertyName("sd")]
    public double Sd { get; set; }

    [JsonPropertyName("p25")]
    public double P25 { get; set; }

    [JsonPropertyName("p50")]
    public double P50 { get; set; }

    [JsonPropertyName("p75")]
    public double P75 { get; set; }
}

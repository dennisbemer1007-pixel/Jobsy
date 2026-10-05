namespace Jobsy.Core.Entities;

/// <summary>
/// One stored typical workday for an ESCO occupation. Generated once and reused.
/// No candidate data.
/// </summary>
public class OccupationDayInLife
{
    public Guid Id { get; set; }

    /// <summary>ESCO occupation id (concept UUID), the stable lookup key.</summary>
    public string EscoId { get; set; } = "";

    /// <summary>ESCO concept URI.</summary>
    public string Uri { get; set; } = "";

    public string TitleNl { get; set; } = "";
    public string Morning { get; set; } = "";
    public string Midday { get; set; } = "";
    public string Afternoon { get; set; } = "";

    /// <summary>JSON array of short highlight lines.</summary>
    public string HighlightsJson { get; set; } = "[]";

    /// <summary>What can differ between employers. Required so the day stays typical, not specific.</summary>
    public string VariesNote { get; set; } = "";

    public string SourceModel { get; set; } = "";
    public DateTime GeneratedAtUtc { get; set; }

    /// <summary>SHA-256 of the stored Dutch text. Import rejects a row when this does not match.</summary>
    public string ContentHash { get; set; } = "";

    public string Locale { get; set; } = "nl";

    /// <summary>True when the ESCO source was thin and the stored day is intentionally short.</summary>
    public bool ThinSource { get; set; }
}

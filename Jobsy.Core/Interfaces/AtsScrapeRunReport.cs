namespace Jobsy.Core.Interfaces;

/// <summary>Diagnostic summary of one ATS scrape run (all sources or a single source).</summary>
public sealed class AtsScrapeRunReport
{
    public DateTime StartedAtUtc { get; set; }
    public DateTime FinishedAtUtc { get; set; }
    public int SourceCount { get; set; }
    public int Upserted { get; set; }
    public int Inserted { get; set; }
    public int Updated { get; set; }
    public int SkippedDuplicateHash { get; set; }
    public int SkippedBlacklist { get; set; }
    public int SkippedParse { get; set; }
    public int HttpErrors { get; set; }
    public IList<AtsScrapeSourceReport> Sources { get; set; } = new List<AtsScrapeSourceReport>();
    /// <summary>Human-readable diagnostic lines for the admin UI.</summary>
    public IList<string> Lines { get; set; } = new List<string>();
}

public sealed class AtsScrapeSourceReport
{
    public Guid SourceId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Domain { get; set; } = string.Empty;
    public string ListUrl { get; set; } = string.Empty;
    public int? ListHttpStatus { get; set; }
    public int RawAnchorCount { get; set; }
    public int VacancyLinkCount { get; set; }
    public int DetailPagesFetched { get; set; }
    public int Upserted { get; set; }
    public int Inserted { get; set; }
    public int Updated { get; set; }
    public int SkippedDuplicateHash { get; set; }
    public int SkippedBlacklist { get; set; }
    public int SkippedParse { get; set; }
    public int HttpErrors { get; set; }
    public string? Error { get; set; }
    public IList<string> Lines { get; set; } = new List<string>();
}

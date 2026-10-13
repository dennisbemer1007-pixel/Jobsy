namespace Jobsy.Core.Entities.Scholen;

/// <summary>Completed scoring snapshot for a pupil code. Rendered text regenerated from templates.</summary>
public class PupilResult
{
    public Guid PupilCodeId { get; set; }
    public PupilCode? PupilCode { get; set; }
    public Guid SchoolClassId { get; set; }
    public DateTime CompletedAtUtc { get; set; }
    public string CompetenceScoresJson { get; set; } = "{}";
    public string RiasecScoresJson { get; set; } = "{}";
    public string? HollandCode { get; set; }
    public string ValuesScoresJson { get; set; } = "{}";
    public string? TopValue { get; set; }
    public string CultureScoresJson { get; set; } = "{}";
    public string? TopCulture { get; set; }
    public string ScoringVersion { get; set; } = string.Empty;
    public string StoryTemplateVersion { get; set; } = string.Empty;
    /// <summary>Template keys, not rendered text.</summary>
    public string StoryKeysJson { get; set; } = "[]";
    public string? DreamJobKey { get; set; }
    public string? FitSnapshotJson { get; set; }
}

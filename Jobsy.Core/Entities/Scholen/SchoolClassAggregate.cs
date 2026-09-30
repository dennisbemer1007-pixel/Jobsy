using Jobsy.Core.Enums;

namespace Jobsy.Core.Entities.Scholen;

/// <summary>
/// Anonymous class aggregate (k ≥ 5). No FK to SchoolClass/PupilCode so it survives retention deletion.
/// </summary>
public class SchoolClassAggregate
{
    public Guid Id { get; set; }
    public Guid? SchoolId { get; set; }
    public int SchoolYearStart { get; set; }
    public string ClassLabel { get; set; } = string.Empty;
    public SchoolLevel Level { get; set; }
    public int Year { get; set; }
    public int PupilCount { get; set; }
    public int StartedCount { get; set; }
    public int CompletedCount { get; set; }
    public string RiasecTop3CountsJson { get; set; } = "{}";
    public string TopValueCountsJson { get; set; } = "{}";
    public string TopCultureCountsJson { get; set; } = "{}";
    public string CompetenceBandCountsJson { get; set; } = "{}";
    public string DreamJobCountsJson { get; set; } = "{}";
    public DateTime SnapshotAtUtc { get; set; }
}

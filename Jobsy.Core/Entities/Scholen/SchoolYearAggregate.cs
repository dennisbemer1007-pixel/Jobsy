using Jobsy.Core.Enums;

namespace Jobsy.Core.Entities.Scholen;

/// <summary>
/// School-year aggregate (and optional platform-wide row with SchoolId null),
/// always scoped to a single <see cref="PupilQuestionSet"/>. Survives class deletion.
/// </summary>
public class SchoolYearAggregate
{
    public Guid Id { get; set; }
    public Guid? SchoolId { get; set; }
    public int SchoolYearStart { get; set; }
    /// <summary>Which pupil test this year/platform aggregate belongs to (never mixed).</summary>
    public PupilQuestionSet QuestionSet { get; set; }
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

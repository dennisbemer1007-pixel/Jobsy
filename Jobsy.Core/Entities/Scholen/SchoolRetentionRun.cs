namespace Jobsy.Core.Entities.Scholen;

/// <summary>Retention job run log for admin reporting (file 07).</summary>
public class SchoolRetentionRun
{
    public Guid Id { get; set; }
    public DateTime RanAtUtc { get; set; }
    public DateOnly CutoffDate { get; set; }
    public int ClassesDeleted { get; set; }
    public int CodesDeleted { get; set; }
    public int ResultsDeleted { get; set; }
    public int AggregatesWritten { get; set; }
    public string Outcome { get; set; } = string.Empty;
}

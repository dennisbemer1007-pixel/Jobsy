namespace Jobsy.Core.Entities;

public class CandidateOutsideWorkExperience
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;

    public string ActivityTitle { get; set; } = "";
    public string Description { get; set; } = "";
    public int? HoursPerWeek { get; set; }
    public int SortOrder { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
}

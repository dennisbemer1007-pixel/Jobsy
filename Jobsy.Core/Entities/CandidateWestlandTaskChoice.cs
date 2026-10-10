namespace Jobsy.Core.Entities;

public class CandidateWestlandTaskChoice
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;
    public Guid TaskId { get; set; }
    public WestlandOccupationTask Task { get; set; } = null!;
    public DateTime ChosenAtUtc { get; set; }
}

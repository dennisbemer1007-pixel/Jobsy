namespace Jobsy.Core.Entities;

public class WestlandPilotEnrollment
{
    public Guid Id { get; set; }
    public Guid CohortId { get; set; }
    public WestlandPilotCohort Cohort { get; set; } = null!;
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;
    public DateTime EnrolledAtUtc { get; set; }
}

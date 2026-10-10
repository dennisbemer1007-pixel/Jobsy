namespace Jobsy.Core.Entities;

public class WestlandPilotCohort
{
    public Guid Id { get; set; }
    public string Key { get; set; } = "";
    public string Name { get; set; } = "";
    public DateTime? OpensAtUtc { get; set; }
    public DateTime? ClosesAtUtc { get; set; }
    public bool IsActive { get; set; }

    public List<WestlandPilotEnrollment> Enrollments { get; set; } = [];
}

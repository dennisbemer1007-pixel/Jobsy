namespace Jobsy.Core.Entities.Scholen;

public class TeacherClassAssignment
{
    public Guid TeacherUserId { get; set; }
    public Guid SchoolClassId { get; set; }
    public SchoolClass? SchoolClass { get; set; }
    public DateTime CreatedAtUtc { get; set; }
}

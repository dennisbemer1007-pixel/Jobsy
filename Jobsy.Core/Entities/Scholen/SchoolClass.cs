using Jobsy.Core.Enums;

namespace Jobsy.Core.Entities.Scholen;

public class SchoolClass
{
    public Guid Id { get; set; }
    public Guid SchoolId { get; set; }
    public School? School { get; set; }
    /// <summary>Class label, e.g. "2B". Max 12; pattern letters/digits/dash/space.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>True only for acceptatie CLI-seeded sample classes.</summary>
    public bool IsTestData { get; set; }
    public SchoolLevel Level { get; set; }
    /// <summary>School year number 1–6.</summary>
    public int Year { get; set; }
    /// <summary>Calendar year the school year starts (2026 = "2026–2027").</summary>
    public int SchoolYearStart { get; set; }
    /// <summary>Planned pupil count 1–40 (codes generated to match).</summary>
    public int PupilCount { get; set; }
    public TestWindowState TestWindow { get; set; } = TestWindowState.NotOpen;
    public DateOnly? TestWindowClosesOn { get; set; }
    public DateTime? ParentalInfoConfirmedAtUtc { get; set; }
    public Guid? ParentalInfoConfirmedByUserId { get; set; }
    public string? ParentalInfoTextVersion { get; set; }
    public DateTime? LoginPausedUntilUtc { get; set; }
    public DateTime CreatedAtUtc { get; set; }

    public ICollection<PupilCode> PupilCodes { get; set; } = new List<PupilCode>();
    public ICollection<TeacherClassAssignment> TeacherAssignments { get; set; } = new List<TeacherClassAssignment>();
}

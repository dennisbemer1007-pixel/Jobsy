using Jobsy.Core.Entities.Scholen;
using Jobsy.Core.Enums;

namespace Jobsy.Core.Scholen;

/// <summary>Class year/level/set for dream-job "Nu: …" labels and VO copy.</summary>
public sealed record PupilClassContext(SchoolLevel Level, int Year, PupilQuestionSet Set)
{
    public static PupilClassContext From(SchoolClass schoolClass)
    {
        ArgumentNullException.ThrowIfNull(schoolClass);
        return new(schoolClass.Level, schoolClass.Year, schoolClass.QuestionSet);
    }
}

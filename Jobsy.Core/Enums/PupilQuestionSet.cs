namespace Jobsy.Core.Enums;

/// <summary>
/// Which pupil test a school class uses. Derived only from <see cref="SchoolLevel"/>
/// via <c>SchoolLevelRules.QuestionSetFor</c> — never stored per pupil code.
/// See scholen-vragensets README §S.
/// </summary>
public enum PupilQuestionSet
{
    /// <summary>Basisschool groep 7/8 test (60 items, today's bank).</summary>
    Groep78 = 1,

    /// <summary>Middelbare school (VO) test.</summary>
    Vo = 2
}

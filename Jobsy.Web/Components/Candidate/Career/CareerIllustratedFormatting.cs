using Jobsy.Web.Localization;

namespace Jobsy.Web.Components.Candidate.Career;

internal static class CareerIllustratedFormatting
{
    public static string ProgressEyebrow(CultureState culture, int currentStep, int totalSteps, string dreamTitle)
        => culture.Format("Career.Illustrated.ProgressDream", currentStep, totalSteps, dreamTitle);

    public static string CoursesHint(CultureState culture, int count)
        => count == 1
            ? culture["Career.Illustrated.OneCourse"]
            : culture.Format("Career.Illustrated.ManyCourses", count);

    public static string SkillsHint(CultureState culture, int count)
        => count == 1
            ? culture["Career.Illustrated.OneSkill"]
            : culture.Format("Career.Illustrated.ManySkills", count);

    public static string BothHint(CultureState culture, int skills, int courses)
        => $"{CoursesHint(culture, courses)} · {SkillsHint(culture, skills)}";
}

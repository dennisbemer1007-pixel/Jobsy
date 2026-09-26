namespace Jobsy.Core.Rules;

/// <summary>Simple 0–100 profile completeness for the Kompas header.</summary>
public static class KompasProfileCompleteness
{
    public const int StepCount = 6;

    public static int Percent(
        bool profileBasicsFilled,
        bool competencyCompleted,
        bool careerCompleted,
        bool cultureCompleted,
        bool valuesCompleted,
        bool hasEducationOrBackground)
    {
        var done = (profileBasicsFilled ? 1 : 0)
                   + (hasEducationOrBackground ? 1 : 0)
                   + (competencyCompleted ? 1 : 0)
                   + (careerCompleted ? 1 : 0)
                   + (cultureCompleted ? 1 : 0)
                   + (valuesCompleted ? 1 : 0);
        return (int)Math.Round(100.0 * done / StepCount);
    }
}

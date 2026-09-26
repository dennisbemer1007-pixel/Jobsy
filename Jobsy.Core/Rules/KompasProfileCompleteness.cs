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
        => Percent(
            profileBasicsFilled,
            competencyCompleted,
            careerCompleted,
            cultureCompleted,
            valuesCompleted,
            hasEducationOrBackground,
            competencyProvisional: false,
            careerProvisional: false,
            cultureProvisional: false,
            valuesProvisional: false);

    /// <summary>
    /// Completeness with provisional (wizard draft) tests counting as half a step.
    /// Completed tests always count as a full step.
    /// </summary>
    public static int Percent(
        bool profileBasicsFilled,
        bool competencyCompleted,
        bool careerCompleted,
        bool cultureCompleted,
        bool valuesCompleted,
        bool hasEducationOrBackground,
        bool competencyProvisional,
        bool careerProvisional,
        bool cultureProvisional,
        bool valuesProvisional)
    {
        double done = (profileBasicsFilled ? 1 : 0)
                      + (hasEducationOrBackground ? 1 : 0)
                      + StepWeight(competencyCompleted, competencyProvisional)
                      + StepWeight(careerCompleted, careerProvisional)
                      + StepWeight(cultureCompleted, cultureProvisional)
                      + StepWeight(valuesCompleted, valuesProvisional);
        return (int)Math.Round(100.0 * done / StepCount);
    }

    private static double StepWeight(bool completed, bool provisional)
        => completed ? 1.0 : provisional ? 0.5 : 0.0;
}

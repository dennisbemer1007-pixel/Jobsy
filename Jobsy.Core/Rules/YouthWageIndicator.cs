namespace Jobsy.Core.Rules;

/// <summary>
/// Employer-facing youth-wage flag. Replaces an exact age on employer screens (decision 22).
/// The cut-off is <see cref="AgeRules.AdultAgeYears"/> (21), the same constant the wage tables use.
/// </summary>
public static class YouthWageIndicator
{
    /// <summary>
    /// True when <paramref name="ageYears"/> is below the adult youth-wage cut-off.
    /// Null when the age is unknown. Never returns the age itself.
    /// </summary>
    public static bool? Applies(int? ageYears)
    {
        if (ageYears is not int age)
        {
            return null;
        }

        return age < AgeRules.AdultAgeYears;
    }
}

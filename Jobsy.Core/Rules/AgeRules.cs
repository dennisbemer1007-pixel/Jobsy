namespace Jobsy.Core.Rules;

/// <summary>Canonical age bounds for filters, wages and youth-labor checks.</summary>
public static class AgeRules
{
    public const int MinWorkingAgeYears = YouthLaborRules.MinWorkingAgeYears;
    public const int AdultAgeYears = 21;
    public const int MaxFilterAgeYears = 67;
    public const int MaxWageBandAgeYears = 99;

    public static int ClampWorkingAge(int ageYears)
        => Math.Clamp(ageYears, MinWorkingAgeYears, MaxWageBandAgeYears);

    public static int ClampFilterAge(int ageYears)
        => Math.Clamp(ageYears, MinWorkingAgeYears, MaxFilterAgeYears);

    public static int? AgeYearsFromDateOfBirth(DateOnly? dateOfBirth, DateOnly? today = null)
    {
        if (dateOfBirth is null)
        {
            return null;
        }

        var on = today ?? DateOnly.FromDateTime(DateTime.UtcNow);
        var age = on.Year - dateOfBirth.Value.Year;
        if (dateOfBirth.Value > on.AddYears(-age))
        {
            age--;
        }

        return age < 0 ? null : age;
    }
}

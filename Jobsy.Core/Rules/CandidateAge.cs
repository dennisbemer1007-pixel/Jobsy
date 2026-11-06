namespace Jobsy.Core.Rules;

/// <summary>Age gates for candidate UI. Unknown birth date is treated as an adult.</summary>
public static class CandidateAge
{
    public const int AdultFromYears = 18;

    public static bool IsYoung(DateOnly? birthDate, DateOnly today)
    {
        if (birthDate is not { } born || born > today)
        {
            return false;
        }

        var age = today.Year - born.Year;
        if (born > today.AddYears(-age))
        {
            age--;
        }

        return age < AdultFromYears;
    }
}

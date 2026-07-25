namespace Jobsy.Core.Rules;

/// <summary>
/// Anonymous visitors see wage. Candidates must have a date of birth on file.
/// Non-candidate authenticated users (employers/admin) also see wage.
/// </summary>
public static class WageVisibilityRules
{
    public const string MissingDateOfBirthMessage = "Vul geboortedatum in";

    public static bool CanShowWage(bool isAuthenticated, bool isCandidate, bool hasDateOfBirth)
    {
        if (!isAuthenticated || !isCandidate)
        {
            return true;
        }

        return hasDateOfBirth;
    }
}

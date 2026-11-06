using System.Text.RegularExpressions;

namespace Jobsy.Core.Rules;

/// <summary>Drops coach replies that invent test names or deny tests the facts already list.</summary>
public static partial class AssistantReplyGuard
{
    public static bool Accepts(string? reply, string? facts)
    {
        if (string.IsNullOrWhiteSpace(reply))
        {
            return false;
        }

        if (Forbidden().IsMatch(reply))
        {
            return false;
        }

        if (!string.IsNullOrWhiteSpace(facts)
            && facts.Contains("completedTests=", StringComparison.Ordinal)
            && !facts.Contains("completedTests=none", StringComparison.Ordinal)
            && DeniesTests().IsMatch(reply))
        {
            return false;
        }

        return true;
    }

    [GeneratedRegex(@"\b(riasec|career-test|holland-code|holland code|werksterkte)\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex Forbidden();

    [GeneratedRegex(@"geen tests|no tests yet|have not done any tests|haven't done any tests|nie masz test|nu ai făcut niciun test|لم تجرِ أي اختبار", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex DeniesTests();
}

namespace Jobsy.Core.Rules;

/// <summary>Light guardrails so rejection reasons stay warm and readable.</summary>
public static class AfterMatchOutcomeToneRules
{
    private static readonly string[] BlockedFragments =
    [
        " idioot",
        " sukkel",
        " dom ",
        " gek ",
        " rot ",
        " kut",
        " fuck",
        " shit",
        " idiot",
        " stupid",
        " dumb "
    ];

    public static bool IsAcceptable(string? reason)
    {
        if (string.IsNullOrWhiteSpace(reason))
        {
            return false;
        }

        var trimmed = reason.Trim();
        if (trimmed.Length < 12)
        {
            return false;
        }

        var lower = trimmed.ToLowerInvariant();
        foreach (var fragment in BlockedFragments)
        {
            if (lower.Contains(fragment, StringComparison.Ordinal))
            {
                return false;
            }
        }

        return true;
    }
}

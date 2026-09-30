namespace Jobsy.Core.Rules;

/// <summary>
/// Classifies Dutch Chamber of Commerce (KVK) SBI codes for automated role assignment.
/// SBI codes starting with <c>78</c> are employment/recruitment agencies (intermediairs).
/// </summary>
public static class KvkSbiClassification
{
    public const string IntermediaryPrefix = "78";

    public static bool IsIntermediarySbi(string? sbiCode)
    {
        if (string.IsNullOrWhiteSpace(sbiCode))
        {
            return false;
        }

        var digits = new string(sbiCode.Where(char.IsDigit).ToArray());
        return digits.StartsWith(IntermediaryPrefix, StringComparison.Ordinal);
    }

    public static bool IsIntermediary(IEnumerable<string>? sbiCodes)
        => sbiCodes?.Any(IsIntermediarySbi) == true;

    public static bool HasNonIntermediary(IEnumerable<string>? sbiCodes)
        => sbiCodes?.Any(s => !string.IsNullOrWhiteSpace(s) && !IsIntermediarySbi(s)) == true;

    /// <summary>SBI 78 plus at least one non-78 code → wizard asks "als werkgever / als intermediair".</summary>
    public static bool IsMixedIntermediary(IEnumerable<string>? sbiCodes)
        => IsIntermediary(sbiCodes) && HasNonIntermediary(sbiCodes);

    /// <summary>True when the first (main) SBI activity is 78*.</summary>
    public static bool IsMainActivityIntermediary(IEnumerable<string>? sbiCodes)
        => IsIntermediarySbi(PrimarySbiCode(sbiCodes));

    public static string? PrimarySbiCode(IEnumerable<string>? sbiCodes)
        => sbiCodes?.FirstOrDefault(s => !string.IsNullOrWhiteSpace(s))?.Trim();
}

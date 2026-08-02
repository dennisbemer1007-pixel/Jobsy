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

    public static string? PrimarySbiCode(IEnumerable<string>? sbiCodes)
        => sbiCodes?.FirstOrDefault(s => !string.IsNullOrWhiteSpace(s))?.Trim();
}

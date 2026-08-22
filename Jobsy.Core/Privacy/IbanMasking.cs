using Jobsy.Core.Interfaces;

namespace Jobsy.Core.Privacy;

/// <summary>API/UI must never echo a full IBAN. Empty or masked input means “keep stored value”.</summary>
public static class IbanMasking
{
    public static string ForApi(string? iban) => ISalesManagerPayoutService.MaskIban(iban);

    public static bool IsFullIbanInput(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        if (value.Contains('*', StringComparison.Ordinal) || value.Contains('—', StringComparison.Ordinal))
        {
            return false;
        }

        var compact = value.Replace(" ", "", StringComparison.Ordinal);
        return compact.Length >= 8 && compact.All(char.IsLetterOrDigit);
    }

    public static string? ResolveStoredIban(string? requested, string? currentStored)
        => IsFullIbanInput(requested) ? requested : currentStored;
}

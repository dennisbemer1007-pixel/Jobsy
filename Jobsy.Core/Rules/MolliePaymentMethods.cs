namespace Jobsy.Core.Rules;

/// <summary>
/// Supported Mollie payment methods for prepaid token purchases.
/// Values match Mollie Payments API method identifiers.
/// </summary>
public static class MolliePaymentMethods
{
    public const string Ideal = "ideal";
    public const string CreditCard = "creditcard";

    /// <summary>Primary instant methods offered in Lobsy checkout UI.</summary>
    public static readonly IReadOnlyList<string> PrimaryMethods = [Ideal, CreditCard];

    public static bool IsSupported(string? method)
    {
        if (string.IsNullOrWhiteSpace(method))
        {
            return false;
        }

        var normalized = Normalize(method);
        return normalized is Ideal or CreditCard;
    }

    /// <summary>
    /// Returns a supported method id, or null when empty/unsupported.
    /// </summary>
    public static string? NormalizeOrNull(string? method)
    {
        if (string.IsNullOrWhiteSpace(method))
        {
            return null;
        }

        var normalized = Normalize(method);
        return IsSupported(normalized) ? normalized : null;
    }

    public static string Normalize(string method) => method.Trim().ToLowerInvariant();

    public static string DisplayName(string? method) => NormalizeOrNull(method) switch
    {
        Ideal => "iDEAL",
        CreditCard => "Creditcard",
        _ => "Mollie"
    };
}

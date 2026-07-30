namespace Jobsy.Core.Rules;

/// <summary>
/// Token pack prices are charged incl. 21% BTW. All monetary amounts are stored as whole cents.
/// </summary>
public static class TokenVatPricing
{
    public const decimal VatRate = 0.21m;
    public const decimal VatDivisor = 1.21m;

    /// <summary>
    /// Splits an incl.-VAT euro amount into integer cents (ex-VAT, VAT, total).
    /// VAT = total − ex-VAT so the three parts always reconcile.
    /// </summary>
    public static (int ExVatCents, int VatCents, int TotalCents) SplitInclVatEuros(decimal totalInclVatEuro)
    {
        var totalCents = ToCents(totalInclVatEuro);
        return SplitInclVatCents(totalCents);
    }

    public static (int ExVatCents, int VatCents, int TotalCents) SplitInclVatCents(int totalCents)
    {
        if (totalCents < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(totalCents));
        }

        if (totalCents == 0)
        {
            return (0, 0, 0);
        }

        var exVatCents = (int)Math.Round(totalCents / VatDivisor, MidpointRounding.AwayFromZero);
        var vatCents = totalCents - exVatCents;
        return (exVatCents, vatCents, totalCents);
    }

    public static int ToCents(decimal euros) =>
        (int)Math.Round(euros * 100m, MidpointRounding.AwayFromZero);

    public static decimal FromCents(int cents) => cents / 100m;

    public static string FormatEuro(int cents) =>
        FromCents(cents).ToString("0.00", System.Globalization.CultureInfo.GetCultureInfo("nl-NL"));
}

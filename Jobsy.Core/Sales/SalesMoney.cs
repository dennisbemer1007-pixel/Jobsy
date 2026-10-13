using System.Globalization;

namespace Jobsy.Core.Sales;

public static class SalesMoney
{
    private static readonly CultureInfo Nl = CultureInfo.GetCultureInfo("nl-NL");

    public static string Format(decimal amount, Enums.SalesMoneyKind kind)
    {
        var body = FormatPlain(amount);
        return kind switch
        {
            Enums.SalesMoneyKind.ExVat => $"{body} excl. btw",
            Enums.SalesMoneyKind.InclVat => $"{body} incl. btw",
            _ => body
        };
    }

    public static string FormatPlain(decimal amount)
    {
        // € + NBSP + nl-NL number
        var number = amount.ToString("N2", Nl);
        return $"€\u00A0{number}";
    }

    public static string FormatSigned(decimal amount)
    {
        if (amount > 0)
        {
            return $"+ {FormatPlain(amount)}";
        }

        if (amount < 0)
        {
            return $"– {FormatPlain(Math.Abs(amount))}";
        }

        return FormatPlain(0m);
    }
}

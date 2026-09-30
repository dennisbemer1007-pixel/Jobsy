using System.Globalization;

namespace Jobsy.Core.Email;

/// <summary>Culture-aware number/money helpers for mail copy (nl until file 04).</summary>
public static class EmailFormat
{
    private static readonly CultureInfo Nl = CultureInfo.GetCultureInfo("nl-NL");

    public static string FormatEuro(decimal amount)
        => amount.ToString("C", Nl);

    public static string FormatKm(double km)
        => km.ToString("0.0", Nl) + " km";
}

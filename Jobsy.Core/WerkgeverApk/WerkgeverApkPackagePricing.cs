namespace Jobsy.Core.WerkgeverApk;

public static class WerkgeverApkPackagePricing
{
    public const string OnRequestLabel = "Prijs op aanvraag";

    public static string FormatPrice(string? amount, string? unit)
    {
        if (string.IsNullOrWhiteSpace(amount))
        {
            return OnRequestLabel;
        }

        var trimmed = amount.Trim();
        if (string.IsNullOrWhiteSpace(unit))
        {
            return trimmed;
        }

        return $"{trimmed} {unit.Trim()}";
    }

    public static string PackageLabel(WerkgeverApkPackageKind package)
        => package switch
        {
            WerkgeverApkPackageKind.Uitgebreid => "Uitgebreid",
            _ => "Basis",
        };
}
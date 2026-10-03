namespace Jobsy.Core.Rules;

public static class PassportPartnerLogoRules
{
    public const int MaxBytes = 512 * 1024;
    public const int MinWidthPx = 200;

    public static string? RejectReason(string? fileName, string? contentType, long size)
    {
        if (size > MaxBytes)
        {
            return "logo_too_large";
        }

        var name = fileName ?? "";
        var type = contentType ?? "";
        if (name.EndsWith(".svg", StringComparison.OrdinalIgnoreCase)
            || type.Contains("svg", StringComparison.OrdinalIgnoreCase))
        {
            return "logo_svg";
        }

        var png = type.Contains("png", StringComparison.OrdinalIgnoreCase)
            || name.EndsWith(".png", StringComparison.OrdinalIgnoreCase);
        var jpeg = type.Contains("jpeg", StringComparison.OrdinalIgnoreCase)
            || type.Contains("jpg", StringComparison.OrdinalIgnoreCase)
            || name.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase)
            || name.EndsWith(".jpeg", StringComparison.OrdinalIgnoreCase);
        if (!png && !jpeg)
        {
            return "logo_type";
        }

        return null;
    }
}

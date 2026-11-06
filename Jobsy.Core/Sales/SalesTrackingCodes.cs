using System.Text.RegularExpressions;
using Jobsy.Core.Enums;
using Jobsy.Core.Rules;

namespace Jobsy.Core.Sales;

/// <summary>Normalize and classify public sales/partner/ambassadeur tracking codes.</summary>
public static partial class SalesTrackingCodes
{
    public static string? Normalize(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return null;
        }

        var code = raw.Trim().ToUpperInvariant();
        return WellFormedRegex().IsMatch(code) ? code : null;
    }

    public static bool IsWellFormed(string? code)
        => Normalize(code) is not null;

    public static bool IsSalesManager(string code)
        => code.StartsWith("SM-", StringComparison.Ordinal);

    public static bool IsPartner(string code)
        => code.StartsWith("BM-", StringComparison.Ordinal)
           || code.StartsWith("IM-", StringComparison.Ordinal);

    public static bool IsAmbassadeur(string code)
        => AmbassadeurCommissionRules.IsAmbassadeurTrackingCode(code)
           || code.StartsWith(AmbassadeurCommissionRules.TrackingCodePrefix, StringComparison.Ordinal);

    public static SalesLinkChannel ChannelFromQuery(string? b)
    {
        if (string.IsNullOrWhiteSpace(b))
        {
            return SalesLinkChannel.Other;
        }

        return b.Trim().ToLowerInvariant() switch
        {
            "qr" => SalesLinkChannel.Qr,
            "flyer" => SalesLinkChannel.Flyer,
            "link" => SalesLinkChannel.Link,
            _ => SalesLinkChannel.Other
        };
    }

    [GeneratedRegex(@"^(SM|BM|IM|AM)-[A-Z0-9]{4,12}$", RegexOptions.CultureInvariant)]
    private static partial Regex WellFormedRegex();
}

using System.Text.RegularExpressions;

namespace Jobsy.Web.Auth;

/// <summary>
/// Safe, log-friendly formatting for Microsoft Entra / OIDC failures (no secrets).
/// </summary>
public static partial class EntraAuthFailureFormatter
{
    [GeneratedRegex(@"AADSTS\d+", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex AadstsCodePattern();

    public static string? TryGetAadstsCode(Exception? failure)
    {
        for (var ex = failure; ex is not null; ex = ex.InnerException)
        {
            var match = AadstsCodePattern().Match(ex.Message);
            if (match.Success)
            {
                return match.Value;
            }
        }

        return null;
    }

    public static string Describe(Exception? failure)
    {
        if (failure is null)
        {
            return "(geen exception)";
        }

        var aadsts = TryGetAadstsCode(failure);
        var message = failure.Message;
        if (message.Length > 500)
        {
            message = message[..500] + "…";
        }

        return aadsts is null ? message : $"{aadsts}: {message}";
    }
}

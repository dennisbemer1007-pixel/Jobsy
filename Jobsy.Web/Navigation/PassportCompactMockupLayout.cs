using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Configuration;

namespace Jobsy.Web.Navigation;

/// <summary>
/// Step-1 compact paspoort mockups: layout variants via query string when explicitly enabled (Development / screenshot host).
/// </summary>
public static class PassportCompactMockupLayout
{
    public const string QueryKey = "compactLayout";

    public static bool IsEnabled(IConfiguration configuration)
        => configuration.GetValue("Jobsy:PassportCompactMockups:Enabled", false);

    /// <summary>Layout id: a, b, or c; null when disabled or invalid.</summary>
    public static string? TryParse(string? absoluteOrRelativeUri, IConfiguration configuration)
    {
        if (!IsEnabled(configuration) || string.IsNullOrWhiteSpace(absoluteOrRelativeUri))
        {
            return null;
        }

        var uri = absoluteOrRelativeUri.Contains("://", StringComparison.Ordinal)
            ? new Uri(absoluteOrRelativeUri)
            : new Uri("https://local" + (absoluteOrRelativeUri.StartsWith('/') ? absoluteOrRelativeUri : "/" + absoluteOrRelativeUri));
        var query = QueryHelpers.ParseQuery(uri.Query);
        if (!query.TryGetValue(QueryKey, out var values))
        {
            return null;
        }

        var raw = values.FirstOrDefault()?.Trim().ToLowerInvariant();
        return raw is "a" or "b" or "c" ? raw : null;
    }

    public static string? PageModifierClass(string? layout)
        => layout switch
        {
            "a" => "passport-page--compact-a",
            "b" => "passport-page--compact-b",
            "c" => "passport-page--compact-c",
            _ => null
        };
}

namespace Jobsy.Core;

/// <summary>
/// Normalizes public URLs from host-only env values (e.g. Render <c>fromService.host</c>).
/// </summary>
public static class JobsyPublicUrl
{
    public static string NormalizeOrigin(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var trimmed = value.Trim().TrimEnd('/');
        if (trimmed.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
            || trimmed.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            return trimmed;
        }

        return "https://" + trimmed.TrimStart('/');
    }

    public static string NormalizeBaseUrl(string? value, string fallback)
    {
        var origin = NormalizeOrigin(string.IsNullOrWhiteSpace(value) ? fallback : value);
        return string.IsNullOrEmpty(origin) ? fallback.TrimEnd('/') + "/" : origin + "/";
    }
}

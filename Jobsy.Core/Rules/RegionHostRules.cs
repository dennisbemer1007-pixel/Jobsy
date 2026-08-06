namespace Jobsy.Core.Rules;

/// <summary>Normalize and validate public hostnames for regional CNAME sites.</summary>
public static class RegionHostRules
{
    public const int MaxHostnameLength = 253;
    public const int MaxDisplayNameLength = 128;
    public const int MaxSloganLength = 256;
    public const int MaxAddressLength = 512;
    public const int MaxBackgroundUrlLength = 1024;

    public static string? NormalizeHostname(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return null;
        }

        var value = raw.Trim().ToLowerInvariant();
        if (value.StartsWith("https://", StringComparison.Ordinal))
        {
            value = value["https://".Length..];
        }
        else if (value.StartsWith("http://", StringComparison.Ordinal))
        {
            value = value["http://".Length..];
        }

        var slash = value.IndexOf('/');
        if (slash >= 0)
        {
            value = value[..slash];
        }

        var colon = value.IndexOf(':');
        if (colon >= 0)
        {
            value = value[..colon];
        }

        value = value.Trim().Trim('.');
        if (value.StartsWith("www.", StringComparison.Ordinal))
        {
            // Keep www if it's the apex branding host; regional CNAMEs usually omit www.
        }

        return string.IsNullOrWhiteSpace(value) ? null : value;
    }

    public static bool IsValidHostname(string? hostname)
    {
        var normalized = NormalizeHostname(hostname);
        if (normalized is null || normalized.Length > MaxHostnameLength)
        {
            return false;
        }

        // Basic DNS label check: labels of 1–63 chars, alphanumeric + hyphen, at least one dot for public hosts
        // Allow localhost and *.local for Development.
        if (normalized is "localhost" || normalized.EndsWith(".local", StringComparison.Ordinal))
        {
            return true;
        }

        if (!normalized.Contains('.'))
        {
            return false;
        }

        var labels = normalized.Split('.');
        if (labels.Length < 2)
        {
            return false;
        }

        foreach (var label in labels)
        {
            if (label.Length is < 1 or > 63)
            {
                return false;
            }

            if (label.StartsWith('-') || label.EndsWith('-'))
            {
                return false;
            }

            foreach (var ch in label)
            {
                if (!char.IsAsciiLetterOrDigit(ch) && ch != '-')
                {
                    return false;
                }
            }
        }

        return true;
    }
}

namespace Jobsy.Core.Rules;

/// <summary>Normalizes KVK website values to a registrable domain (no scheme/path/www).</summary>
public static class KvkWebsiteDomain
{
    public static string? Normalize(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return null;
        }

        var value = raw.Trim();
        if (value.StartsWith("http://", StringComparison.OrdinalIgnoreCase))
        {
            value = value["http://".Length..];
        }
        else if (value.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            value = value["https://".Length..];
        }

        var slash = value.IndexOfAny(['/', '?', '#']);
        if (slash >= 0)
        {
            value = value[..slash];
        }

        value = value.Trim().TrimEnd('.').ToLowerInvariant();
        if (value.StartsWith("www.", StringComparison.Ordinal))
        {
            value = value["www.".Length..];
        }

        return string.IsNullOrWhiteSpace(value) ? null : value;
    }

    public static IReadOnlyList<string> NormalizeMany(IEnumerable<string?>? raw)
    {
        if (raw is null)
        {
            return [];
        }

        var list = new List<string>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var item in raw)
        {
            var domain = Normalize(item);
            if (domain is null || !seen.Add(domain))
            {
                continue;
            }

            list.Add(domain);
        }

        return list;
    }
}

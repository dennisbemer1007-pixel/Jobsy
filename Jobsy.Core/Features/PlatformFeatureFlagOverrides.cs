using System.Text.Json;

namespace Jobsy.Core.Features;

/// <summary>
/// Tracks which platform feature bools were explicitly set in admin (override env/appsettings default).
/// </summary>
public static class PlatformFeatureFlagOverrides
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public static IReadOnlySet<string> Parse(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return Empty;
        }

        try
        {
            var list = JsonSerializer.Deserialize<List<string>>(json, JsonOptions);
            if (list is null || list.Count == 0)
            {
                return Empty;
            }

            return new HashSet<string>(list.Where(s => !string.IsNullOrWhiteSpace(s)), StringComparer.Ordinal);
        }
        catch
        {
            return Empty;
        }
    }

    public static string Serialize(IEnumerable<string> keys)
    {
        var list = keys
            .Where(k => !string.IsNullOrWhiteSpace(k))
            .Distinct(StringComparer.Ordinal)
            .OrderBy(k => k, StringComparer.Ordinal)
            .ToList();
        return JsonSerializer.Serialize(list, JsonOptions);
    }

    public static string AddKey(string? json, string key)
    {
        var set = new HashSet<string>(Parse(json), StringComparer.Ordinal);
        set.Add(key);
        return Serialize(set);
    }

    public static string RemoveKey(string? json, string key)
    {
        var set = new HashSet<string>(Parse(json), StringComparer.Ordinal);
        set.Remove(key);
        return Serialize(set);
    }

    private static readonly IReadOnlySet<string> Empty = new HashSet<string>(StringComparer.Ordinal);
}

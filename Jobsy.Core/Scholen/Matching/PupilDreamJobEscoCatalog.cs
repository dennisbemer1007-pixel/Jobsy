using System.Reflection;
using System.Text.Json;

namespace Jobsy.Core.Scholen.Matching;

/// <summary>Dream-job catalog keys → ESCO URI (API search, nov 2026). See pupil-dream-job-esco.json.</summary>
public static class PupilDreamJobEscoCatalog
{
    private sealed record Entry(string Uri, string? EscoTitle, IReadOnlyList<string> IscedCodes);

    private static readonly Lazy<IReadOnlyDictionary<string, Entry>> Map = new(Load);
    private static readonly Lazy<IReadOnlyList<string>> Unmatched = new(LoadUnmatched);

    public static string? TryGetUri(string? jobKey)
    {
        if (string.IsNullOrWhiteSpace(jobKey))
        {
            return null;
        }

        return Map.Value.TryGetValue(jobKey.Trim(), out var entry) ? entry.Uri : null;
    }

    public static IReadOnlyList<string> IscedCodesForJob(string? jobKey)
    {
        if (string.IsNullOrWhiteSpace(jobKey))
        {
            return [];
        }

        if (!Map.Value.TryGetValue(jobKey.Trim(), out var entry))
        {
            return [];
        }

        if (entry.IscedCodes.Count > 0)
        {
            return entry.IscedCodes;
        }

        return PupilEscoIscedIndex.IscedCodesForUri(entry.Uri);
    }

    public static bool HasEscoMatch(string? jobKey)
        => !string.IsNullOrWhiteSpace(TryGetUri(jobKey));

    public static IReadOnlyList<string> UnmatchedJobKeys => Unmatched.Value;

    private static IReadOnlyDictionary<string, Entry> Load()
    {
        using var stream = OpenJson();
        using var doc = JsonDocument.Parse(stream);
        var jobs = doc.RootElement.GetProperty("jobs");
        var dict = new Dictionary<string, Entry>(StringComparer.OrdinalIgnoreCase);
        foreach (var prop in jobs.EnumerateObject())
        {
            var uri = prop.Value.GetProperty("uri").GetString() ?? "";
            var title = prop.Value.TryGetProperty("escoTitle", out var t) ? t.GetString() : null;
            var codes = new List<string>();
            if (prop.Value.TryGetProperty("iscedCodes", out var arr) && arr.ValueKind == JsonValueKind.Array)
            {
                foreach (var c in arr.EnumerateArray())
                {
                    var code = c.GetString();
                    if (!string.IsNullOrWhiteSpace(code))
                    {
                        codes.Add(code);
                    }
                }
            }

            dict[prop.Name] = new Entry(uri, title, codes);
        }

        return dict;
    }

    private static IReadOnlyList<string> LoadUnmatched()
    {
        using var stream = OpenJson();
        using var doc = JsonDocument.Parse(stream);
        if (!doc.RootElement.TryGetProperty("unmatched", out var arr))
        {
            return [];
        }

        return arr.EnumerateArray()
            .Select(e => e.GetString() ?? "")
            .Where(s => s.Length > 0)
            .ToList();
    }

    private static Stream OpenJson()
    {
        var asm = typeof(PupilDreamJobEscoCatalog).Assembly;
        var name = asm.GetManifestResourceNames()
            .First(n => n.EndsWith("pupil-dream-job-esco.json", StringComparison.OrdinalIgnoreCase));
        return asm.GetManifestResourceStream(name)
               ?? throw new InvalidOperationException("pupil-dream-job-esco.json missing");
    }
}

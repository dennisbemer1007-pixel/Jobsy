using System.Reflection;
using System.Text.Json;

namespace Jobsy.Core.Scholen.Matching;

/// <summary>ESCO occupation URI → ISCED-F veldcodes (from embedded occ_isced.json + dream-job enrichment).</summary>
public static class PupilEscoIscedIndex
{
    private static readonly Lazy<IReadOnlyDictionary<string, IReadOnlyList<string>>> ByUri = new(Load);

    public static IReadOnlyList<string> IscedCodesForUri(string? escoUri)
    {
        if (string.IsNullOrWhiteSpace(escoUri))
        {
            return [];
        }

        return ByUri.Value.TryGetValue(escoUri.Trim(), out var codes) ? codes : [];
    }

    public static bool HasIscedField(string? escoUri) => IscedCodesForUri(escoUri).Count > 0;

    private static IReadOnlyDictionary<string, IReadOnlyList<string>> Load()
    {
        var map = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);

        void AddFromIscedObject(string uri, JsonElement iscedObj)
        {
            if (!map.TryGetValue(uri, out var set))
            {
                set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                map[uri] = set;
            }

            foreach (var prop in iscedObj.EnumerateObject())
            {
                if (prop.Name.Length is >= 3 and <= 4 && prop.Name.All(char.IsDigit))
                {
                    set.Add(prop.Name);
                }
            }
        }

        foreach (var file in new[] { "occ_isced.json", "pupil-dream-job-esco.json" })
        {
            using var stream = OpenEmbedded($"Jobsy.Core.Data.Scholen.Esco.{file}")
                               ?? OpenEmbeddedBySuffix(file);
            if (stream is null)
            {
                continue;
            }

            using var doc = JsonDocument.Parse(stream);
            if (file.StartsWith("occ", StringComparison.Ordinal))
            {
                foreach (var row in doc.RootElement.EnumerateArray())
                {
                    if (!row.TryGetProperty("uri", out var uriEl))
                    {
                        continue;
                    }

                    var uri = uriEl.GetString();
                    if (string.IsNullOrWhiteSpace(uri))
                    {
                        continue;
                    }

                    if (row.TryGetProperty("isced", out var isced) && isced.ValueKind == JsonValueKind.Object)
                    {
                        AddFromIscedObject(uri, isced);
                    }
                }
            }
            else if (doc.RootElement.TryGetProperty("jobs", out var jobs))
            {
                foreach (var job in jobs.EnumerateObject())
                {
                    if (!job.Value.TryGetProperty("uri", out var uriEl))
                    {
                        continue;
                    }

                    var uri = uriEl.GetString();
                    if (string.IsNullOrWhiteSpace(uri))
                    {
                        continue;
                    }

                    if (job.Value.TryGetProperty("isced", out var isced) && isced.ValueKind == JsonValueKind.Object)
                    {
                        AddFromIscedObject(uri, isced);
                    }
                    else if (job.Value.TryGetProperty("iscedCodes", out var codes)
                             && codes.ValueKind == JsonValueKind.Array)
                    {
                        if (!map.TryGetValue(uri, out var set))
                        {
                            set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                            map[uri] = set;
                        }

                        foreach (var c in codes.EnumerateArray())
                        {
                            var code = c.GetString();
                            if (!string.IsNullOrWhiteSpace(code))
                            {
                                set.Add(code);
                            }
                        }
                    }
                }
            }
        }

        return map.ToDictionary(
            kv => kv.Key,
            kv => (IReadOnlyList<string>)kv.Value.OrderBy(x => x, StringComparer.Ordinal).ToList(),
            StringComparer.OrdinalIgnoreCase);
    }

    private static Stream? OpenEmbedded(string logicalName)
    {
        var asm = typeof(PupilEscoIscedIndex).Assembly;
        return asm.GetManifestResourceStream(logicalName);
    }

    private static Stream? OpenEmbeddedBySuffix(string fileName)
    {
        var asm = typeof(PupilEscoIscedIndex).Assembly;
        var name = asm.GetManifestResourceNames()
            .FirstOrDefault(n => n.EndsWith(fileName, StringComparison.OrdinalIgnoreCase));
        return name is null ? null : asm.GetManifestResourceStream(name);
    }
}

using System.Text.Json;

namespace Jobsy.Core.Rules;

public static class ApplicationPreferenceRedaction
{
    /// <summary>
    /// Pending applications expose only non-PII preference hints (roles / travel), never raw JSON.
    /// </summary>
    public static string? RedactForEmployer(string? preferencesJson, bool piiRevealed)
    {
        if (piiRevealed)
        {
            return preferencesJson;
        }

        if (string.IsNullOrWhiteSpace(preferencesJson))
        {
            return null;
        }

        try
        {
            using var doc = JsonDocument.Parse(preferencesJson);
            var root = doc.RootElement;
            var parts = new List<string>();

            if (root.TryGetProperty("roles", out var roles) && roles.ValueKind == JsonValueKind.Array)
            {
                var list = roles.EnumerateArray()
                    .Select(r => r.GetString())
                    .Where(s => !string.IsNullOrWhiteSpace(s))
                    .Take(5);
                var joined = string.Join("/", list!);
                if (!string.IsNullOrWhiteSpace(joined))
                {
                    parts.Add(joined);
                }
            }

            if (root.TryGetProperty("maxTravelMinutes", out var m) && m.TryGetInt32(out var mins))
            {
                parts.Add($"max {mins} min");
            }

            if (root.TryGetProperty("preferredTransport", out var t))
            {
                var transport = t.GetString();
                if (!string.IsNullOrWhiteSpace(transport))
                {
                    parts.Add(transport);
                }
            }

            return parts.Count == 0 ? null : string.Join(" · ", parts);
        }
        catch (JsonException)
        {
            return null;
        }
    }
}

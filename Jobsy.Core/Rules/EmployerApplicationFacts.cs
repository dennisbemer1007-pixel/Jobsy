using System.Text.Json;
using Jobsy.Core.Contracts;
using Jobsy.Core.Entities;

namespace Jobsy.Core.Rules;

/// <summary>
/// AI Act: employers see checklist facts only (no scores or personality output).
/// </summary>
public static class EmployerApplicationFacts
{
    public sealed record Row(string Label, string Value, bool FitsWell);

    public static IReadOnlyList<Row> Build(Application application)
    {
        var rows = new List<Row>();

        if (!string.IsNullOrWhiteSpace(application.PreferencesSummary))
        {
            rows.AddRange(SplitPreferenceFacts(application.PreferencesSummary));
        }

        if (!string.IsNullOrWhiteSpace(application.SnapshotAvailabilityJson))
        {
            var availability = LobsyCvModelFactory.ParseAvailabilityPayload(application.SnapshotAvailabilityJson);
            if (availability.FlexibleTimes)
            {
                rows.Add(new Row("Beschikbaar", "Flexibel", true));
            }
            else if (availability.Slots.Count > 0)
            {
                rows.Add(new Row("Beschikbaar", FormatSlots(availability.Slots), true));
            }
        }

        if (rows.Count == 0 && !string.IsNullOrWhiteSpace(application.Motivation))
        {
            rows.Add(new Row("Motivatie", Truncate(application.Motivation, 120), true));
        }

        return rows;
    }

    private static List<Row> SplitPreferenceFacts(string summary)
    {
        var rows = new List<Row>();
        if (LooksLikeJson(summary))
        {
            try
            {
                using var doc = JsonDocument.Parse(summary);
                if (doc.RootElement.ValueKind == JsonValueKind.Array)
                {
                    foreach (var el in doc.RootElement.EnumerateArray())
                    {
                        if (el.ValueKind != JsonValueKind.Object)
                        {
                            continue;
                        }

                        var label = el.TryGetProperty("label", out var l) ? l.GetString() : null;
                        var value = el.TryGetProperty("value", out var v) ? v.GetString() : null;
                        var fits = el.TryGetProperty("fits", out var f) && f.ValueKind == JsonValueKind.True;
                        if (!string.IsNullOrWhiteSpace(label) && !string.IsNullOrWhiteSpace(value))
                        {
                            rows.Add(new Row(label!, value!, fits));
                        }
                    }

                    return rows;
                }

                if (doc.RootElement.ValueKind == JsonValueKind.Object)
                {
                    rows.AddRange(ParsePreferenceObject(doc.RootElement));
                    return rows;
                }
            }
            catch (JsonException)
            {
                // fall through to plain text
            }
        }

        foreach (var line in summary.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var idx = line.IndexOf(':');
            if (idx > 0)
            {
                rows.Add(new Row(line[..idx].Trim(), line[(idx + 1)..].Trim(), true));
            }
        }

        return rows;
    }

    private static IEnumerable<Row> ParsePreferenceObject(JsonElement root)
    {
        var rows = new List<Row>();

        if (root.TryGetProperty("roles", out var roles) && roles.ValueKind == JsonValueKind.Array)
        {
            var list = roles.EnumerateArray()
                .Select(r => r.GetString())
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .Take(5)
                .ToArray();
            if (list.Length > 0)
            {
                rows.Add(new Row("Rollen", string.Join(", ", list), true));
            }
        }

        var hours = ApplicationPreferenceRedaction.ToHoursReadable(root.GetRawText());
        if (!string.IsNullOrWhiteSpace(hours) && hours != "—")
        {
            rows.Add(new Row("Uren per week", hours, true));
        }

        if (root.TryGetProperty("maxTravelMinutes", out var m)
            && m.ValueKind == JsonValueKind.Number
            && m.TryGetInt32(out var mins)
            && mins > 0)
        {
            rows.Add(new Row("Reistijd", $"max {mins} min", true));
        }

        if (root.TryGetProperty("preferredTransport", out var t))
        {
            var transport = t.GetString()?.Trim();
            if (!string.IsNullOrWhiteSpace(transport))
            {
                rows.Add(new Row("Vervoer", transport, true));
            }
        }

        if (rows.Count == 0)
        {
            var readable = ApplicationPreferenceRedaction.ToHumanReadable(root.GetRawText());
            if (!string.IsNullOrWhiteSpace(readable))
            {
                rows.Add(new Row("Voorkeur", readable, true));
            }
        }

        return rows;
    }

    private static bool LooksLikeJson(string s)
    {
        s = s.TrimStart();
        return s.StartsWith('{') || s.StartsWith('[');
    }

    private static string FormatSlots(IReadOnlyDictionary<string, string[]> slots)
    {
        if (slots.Count == 0)
        {
            return "—";
        }

        return string.Join(" · ", slots.Take(3).Select(kv => $"{kv.Key} {string.Join("/", kv.Value)}"));
    }

    private static string Truncate(string s, int max)
        => s.Length <= max ? s : s[..(max - 1)] + "…";
}

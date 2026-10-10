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

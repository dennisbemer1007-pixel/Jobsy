using System.Globalization;
using System.Text.Json;

namespace Jobsy.Core.Rules;

public static class ApplicationPreferenceRedaction
{
    private static readonly HashSet<string> TechnicalKeys = new(StringComparer.OrdinalIgnoreCase)
    {
        "email", "emailhint", "phone", "phonenumber", "address", "homeaddress",
        "iban", "studentnumber", "schoolemail", "snapshot", "json"
    };

    /// <summary>
    /// Employer UI never receives raw JSON. Pending rows omit city/PII; accepted rows may include city.
    /// </summary>
    public static string? RedactForEmployer(string? preferencesJson, bool piiRevealed)
        => ToHumanReadable(preferencesJson, includeCity: piiRevealed);

    /// <summary>
    /// Converts preference JSON, JSON arrays, or already-human text into a short Dutch summary.
    /// Returns null when the value is empty or only technical metadata.
    /// </summary>
    public static string? ToHumanReadable(string? value, bool includeCity = false)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var trimmed = value.Trim();
        if (LooksLikeJson(trimmed))
        {
            try
            {
                using var doc = JsonDocument.Parse(trimmed);
                return FormatElement(doc.RootElement, includeCity);
            }
            catch (JsonException)
            {
                return null;
            }
        }

        return trimmed;
    }

    public static bool LooksLikeJson(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        var trimmed = value.TrimStart();
        return trimmed.StartsWith('{') || trimmed.StartsWith('[');
    }

    private static string? FormatElement(JsonElement element, bool includeCity)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                return FormatObject(element, includeCity);
            case JsonValueKind.Array:
                return FormatArray(element);
            case JsonValueKind.String:
                var text = element.GetString();
                return string.IsNullOrWhiteSpace(text) ? null : HumanizeRole(text);
            case JsonValueKind.Number:
                return element.ToString();
            case JsonValueKind.True:
                return "ja";
            case JsonValueKind.False:
                return "nee";
            default:
                return null;
        }
    }

    private static string? FormatObject(JsonElement root, bool includeCity)
    {
        var parts = new List<string>();

        if (root.TryGetProperty("roles", out var roles) && roles.ValueKind == JsonValueKind.Array)
        {
            var list = roles.EnumerateArray()
                .Select(r => HumanizeRole(r.GetString()))
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .Take(5)
                .ToArray();
            if (list.Length > 0)
            {
                parts.Add(string.Join(", ", list));
            }
        }

        if (root.TryGetProperty("maxTravelMinutes", out var m)
            && m.ValueKind == JsonValueKind.Number
            && m.TryGetInt32(out var mins)
            && mins > 0)
        {
            parts.Add($"max {mins} min");
        }

        if (root.TryGetProperty("preferredTransport", out var t))
        {
            var transport = t.GetString();
            if (!string.IsNullOrWhiteSpace(transport))
            {
                parts.Add(transport.Trim());
            }
        }

        if (includeCity && root.TryGetProperty("city", out var cityEl))
        {
            var city = cityEl.GetString();
            if (!string.IsNullOrWhiteSpace(city))
            {
                parts.Add(city.Trim());
            }
        }

        if (parts.Count == 0)
        {
            foreach (var prop in root.EnumerateObject())
            {
                if (TechnicalKeys.Contains(prop.Name) ||
                    string.Equals(prop.Name, "roles", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(prop.Name, "city", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                var formatted = FormatElement(prop.Value, includeCity);
                if (!string.IsNullOrWhiteSpace(formatted))
                {
                    parts.Add(formatted);
                }
            }
        }

        return parts.Count == 0 ? null : string.Join(" · ", parts);
    }

    private static string? FormatArray(JsonElement array)
    {
        var items = array.EnumerateArray()
            .Select(item => item.ValueKind == JsonValueKind.String
                ? HumanizeRole(item.GetString())
                : FormatElement(item, includeCity: false))
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .Take(8)
            .ToArray();

        return items.Length == 0 ? null : string.Join(", ", items);
    }

    private static string? HumanizeRole(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return null;
        }

        var known = WorkTypeLabels.Expand(WorkTypeLabels.Parse(raw)).FirstOrDefault();
        if (!string.IsNullOrWhiteSpace(known))
        {
            return known;
        }

        var text = raw.Trim();
        if (text.Length == 1)
        {
            return text.ToUpperInvariant();
        }

        return char.ToUpper(text[0], CultureInfo.GetCultureInfo("nl-NL")) + text[1..];
    }
}

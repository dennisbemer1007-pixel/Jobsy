using System.Text.Json;
using Jobsy.Core.Localization;

namespace Jobsy.Core.Careers;

/// <summary>
/// Stored translations of one Dutch workday. Dutch stays the source of truth.
/// Target languages are every supported UI language except Dutch.
/// </summary>
public static class OccupationDayTranslations
{
    public static readonly IReadOnlyList<string> TargetLanguages =
        JobsyLanguages.All
            .Select(language => language.Code)
            .Where(code => !string.Equals(code, JobsyLanguages.Default, StringComparison.OrdinalIgnoreCase))
            .ToArray();

    public static bool IsComplete(string? translationsJson, string? contentHash)
    {
        if (string.IsNullOrWhiteSpace(contentHash))
        {
            return false;
        }

        var map = Parse(translationsJson);
        foreach (var language in TargetLanguages)
        {
            if (!TryRead(map, language, contentHash, out _))
            {
                return false;
            }
        }

        return true;
    }

    public static bool TryGet(
        string? translationsJson,
        string? language,
        string? contentHash,
        out OccupationDayStoredTranslation translation)
    {
        translation = new OccupationDayStoredTranslation();
        var code = JobsyLanguages.Normalize(language);
        if (string.Equals(code, JobsyLanguages.Default, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        return TryRead(Parse(translationsJson), code, contentHash, out translation);
    }

    public static Dictionary<string, OccupationDayStoredTranslation> Parse(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return new Dictionary<string, OccupationDayStoredTranslation>(StringComparer.OrdinalIgnoreCase);
        }

        try
        {
            var map = JsonSerializer.Deserialize<Dictionary<string, OccupationDayStoredTranslation>>(json, OccupationDayJson.Options);
            return map is null
                ? new Dictionary<string, OccupationDayStoredTranslation>(StringComparer.OrdinalIgnoreCase)
                : new Dictionary<string, OccupationDayStoredTranslation>(map, StringComparer.OrdinalIgnoreCase);
        }
        catch (JsonException)
        {
            return new Dictionary<string, OccupationDayStoredTranslation>(StringComparer.OrdinalIgnoreCase);
        }
    }

    public static string Serialize(IReadOnlyDictionary<string, OccupationDayStoredTranslation> map)
        => JsonSerializer.Serialize(map, OccupationDayJson.Options);

    public static OccupationDayStoredTranslation FromDraft(OccupationDayDraft draft, string sourceHash, string? model)
        => new()
        {
            Title = (draft.TitleNl ?? "").Trim(),
            Morning = (draft.Morning ?? "").Trim(),
            Midday = (draft.Midday ?? "").Trim(),
            Afternoon = (draft.Afternoon ?? "").Trim(),
            Closing = (draft.Closing ?? "").Trim(),
            Highlights = draft.Highlights.Where(line => !string.IsNullOrWhiteSpace(line)).Select(line => line.Trim()).Take(4).ToList(),
            Varies = (draft.VariesNote ?? "").Trim(),
            SourceHash = (sourceHash ?? "").Trim().ToLowerInvariant(),
            Model = (model ?? "").Trim()
        };

    public static OccupationDayDraft ToDraft(OccupationDayStoredTranslation translation, string fallbackTitle)
        => new(
            string.IsNullOrWhiteSpace(translation.Title) ? fallbackTitle : translation.Title.Trim(),
            translation.Morning ?? "",
            translation.Midday ?? "",
            translation.Afternoon ?? "",
            translation.Closing ?? "",
            translation.Highlights ?? [],
            translation.Varies ?? "");

    private static bool TryRead(
        IReadOnlyDictionary<string, OccupationDayStoredTranslation> map,
        string language,
        string? contentHash,
        out OccupationDayStoredTranslation translation)
    {
        translation = new OccupationDayStoredTranslation();
        if (!map.TryGetValue(language, out var stored) || stored is null)
        {
            return false;
        }

        if (string.IsNullOrWhiteSpace(stored.Morning)
            || string.IsNullOrWhiteSpace(stored.Midday)
            || string.IsNullOrWhiteSpace(stored.Afternoon)
            || string.IsNullOrWhiteSpace(stored.Closing))
        {
            return false;
        }

        if (!string.Equals((stored.SourceHash ?? "").Trim(), (contentHash ?? "").Trim(), StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        translation = stored;
        return true;
    }
}

public sealed class OccupationDayStoredTranslation
{
    public string Title { get; set; } = "";
    public string Morning { get; set; } = "";
    public string Midday { get; set; } = "";
    public string Afternoon { get; set; } = "";
    public string Closing { get; set; } = "";
    public List<string> Highlights { get; set; } = [];
    public string Varies { get; set; } = "";
    public string SourceHash { get; set; } = "";
    public string Model { get; set; } = "";
}

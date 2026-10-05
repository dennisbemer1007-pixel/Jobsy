using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Jobsy.Core.Careers;

/// <summary>
/// One stored honest-advice line per ESCO occupation. Loaded from embedded JSON.
/// Never calls a model at runtime.
/// </summary>
public sealed class HonestAdviceService
{
    public const string FileName = "honest_advice.nl.json";

    private static readonly JsonSerializerOptions Json = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true
    };

    private static HonestAdviceService? _shared;

    private readonly Dictionary<string, HonestAdviceEntry> _entries;

    private HonestAdviceService(Dictionary<string, HonestAdviceEntry> entries)
        => _entries = entries;

    public static HonestAdviceService Shared => _shared ??= LoadEmbedded();

    public static HonestAdviceService LoadEmbedded()
    {
        using var stream = Open(FileName);
        return Load(stream);
    }

    public static HonestAdviceService Load(Stream stream)
    {
        var file = JsonSerializer.Deserialize<AdviceFile>(stream, Json)
                   ?? throw new InvalidOperationException("honest_advice.nl.json is empty.");
        var entries = new Dictionary<string, HonestAdviceEntry>(StringComparer.OrdinalIgnoreCase);
        foreach (var pair in file.Entries ?? [])
        {
            var id = (pair.Key ?? "").Trim();
            var row = pair.Value;
            var text = (row?.Text ?? "").Trim();
            if (id.Length == 0)
            {
                throw new InvalidOperationException("honest_advice.nl.json has an empty occupation id.");
            }

            var reason = HonestAdviceValidator.StructuralReason(text);
            if (reason is not null)
            {
                throw new InvalidOperationException($"Eerlijk advies {id} is ongeldig ({reason}).");
            }

            var translations = new Dictionary<string, HonestAdviceTranslation>(StringComparer.OrdinalIgnoreCase);
            foreach (var locale in row?.Translations ?? [])
            {
                var code = Jobsy.Core.Localization.JobsyLanguages.Normalize(locale.Key);
                if (code == Jobsy.Core.Localization.JobsyLanguages.Default
                    || !Jobsy.Core.Localization.JobsyLanguages.IsSupported(locale.Key))
                {
                    continue;
                }

                var translated = (locale.Value?.Text ?? "").Trim();
                var localeReason = HonestAdviceValidator.StructuralReason(translated, code);
                if (localeReason is not null)
                {
                    throw new InvalidOperationException($"Eerlijk advies {id} ({code}) is ongeldig ({localeReason}).");
                }

                translations[code] = new HonestAdviceTranslation(
                    translated,
                    (locale.Value?.GeneratedAt ?? "").Trim(),
                    (locale.Value?.Model ?? "").Trim(),
                    (locale.Value?.SourceHash ?? "").Trim());
            }

            entries[id] = new HonestAdviceEntry(
                text,
                (row?.GeneratedAt ?? "").Trim(),
                (row?.Model ?? "").Trim(),
                (row?.Peildatum ?? "").Trim(),
                (row?.SourceHash ?? "").Trim(),
                translations);
        }

        return new HonestAdviceService(entries);
    }

    public IReadOnlyCollection<string> Ids => _entries.Keys;

    public HonestAdviceEntry? Find(string? escoId)
        => !string.IsNullOrWhiteSpace(escoId) && _entries.TryGetValue(escoId.Trim(), out var entry)
            ? entry
            : null;

    /// <summary>
    /// Stored advice for one occupation, or null when there is none or it no longer matches the facts.
    /// <paramref name="language"/> serves a stored translation. Missing or stale translations stay Dutch.
    /// </summary>
    public HonestAdviceResult? Get(string? escoId, string? language = null)
    {
        var entry = Find(escoId);
        if (entry is null)
        {
            return null;
        }

        var facts = HonestAdviceFacts.TryFor(escoId);
        if (facts is null || HonestAdviceValidator.RejectionReason(entry.Text, facts) is not null)
        {
            return null;
        }

        var lang = Jobsy.Core.Localization.JobsyLanguages.Normalize(language);
        var text = entry.Text;
        if (lang != Jobsy.Core.Localization.JobsyLanguages.Default
            && entry.Translations.TryGetValue(lang, out var translated)
            && string.Equals(translated.SourceHash, HonestAdviceFacts.Hash(entry.Text), StringComparison.Ordinal)
            && HonestAdviceValidator.RejectionReason(translated.Text, facts, lang) is null)
        {
            text = translated.Text;
        }
        else
        {
            lang = Jobsy.Core.Localization.JobsyLanguages.Default;
        }

        var outlook = OccupationOutlook.Shared.Get(facts.EscoId);
        var source = outlook.Sources.Count > 0 ? outlook.Sources[0] : "";
        var peildatum = string.IsNullOrWhiteSpace(outlook.Peildatum) ? entry.Peildatum : outlook.Peildatum;
        return new HonestAdviceResult(text, peildatum, source, facts.EscoId, lang);
    }

    private static Stream Open(string fileName)
    {
        var assembly = typeof(HonestAdviceService).Assembly;
        var name = assembly.GetManifestResourceNames().FirstOrDefault(resource =>
            resource.EndsWith(fileName, StringComparison.OrdinalIgnoreCase));
        if (name is null)
        {
            throw new InvalidOperationException($"Embedded occupation resource {fileName} is missing.");
        }

        return assembly.GetManifestResourceStream(name)
               ?? throw new InvalidOperationException($"Could not open {name}.");
    }

    private sealed class AdviceFile
    {
        public Dictionary<string, AdviceRow>? Entries { get; set; }
    }

    private sealed class AdviceRow
    {
        public string? Text { get; set; }

        public string? GeneratedAt { get; set; }

        public string? Model { get; set; }

        public string? Peildatum { get; set; }

        public string? SourceHash { get; set; }

        public Dictionary<string, AdviceLocale>? Translations { get; set; }
    }

    private sealed class AdviceLocale
    {
        public string? Text { get; set; }

        public string? GeneratedAt { get; set; }

        public string? Model { get; set; }

        public string? SourceHash { get; set; }
    }
}

public sealed record HonestAdviceTranslation(
    string Text,
    string GeneratedAt,
    string Model,
    string SourceHash);

public sealed record HonestAdviceEntry(
    string Text,
    string GeneratedAt,
    string Model,
    string Peildatum,
    string SourceHash,
    IReadOnlyDictionary<string, HonestAdviceTranslation> Translations)
{
    public HonestAdviceEntry(
        string Text,
        string GeneratedAt,
        string Model,
        string Peildatum,
        string SourceHash)
        : this(Text, GeneratedAt, Model, Peildatum, SourceHash, new Dictionary<string, HonestAdviceTranslation>(StringComparer.OrdinalIgnoreCase))
    {
    }
}

public sealed record HonestAdviceResult(
    string Text,
    string? Peildatum,
    string Source,
    string EscoId,
    string Language);

/// <summary>File shape written by tools/occupations/HonestAdviceGen.</summary>
public sealed class HonestAdviceFile
{
    [JsonPropertyName("entries")]
    public Dictionary<string, HonestAdviceFileEntry> Entries { get; set; } = new(StringComparer.OrdinalIgnoreCase);
}

public sealed class HonestAdviceFileEntry
{
    [JsonPropertyName("text")]
    public string Text { get; set; } = "";

    [JsonPropertyName("generatedAt")]
    public string GeneratedAt { get; set; } = "";

    [JsonPropertyName("model")]
    public string Model { get; set; } = "";

    [JsonPropertyName("peildatum")]
    public string Peildatum { get; set; } = "";

    [JsonPropertyName("sourceHash")]
    public string SourceHash { get; set; } = "";

    [JsonPropertyName("translations")]
    public Dictionary<string, HonestAdviceLocaleFile>? Translations { get; set; }
}

public sealed class HonestAdviceLocaleFile
{
    [JsonPropertyName("text")]
    public string Text { get; set; } = "";

    [JsonPropertyName("generatedAt")]
    public string GeneratedAt { get; set; } = "";

    [JsonPropertyName("model")]
    public string Model { get; set; } = "";

    [JsonPropertyName("sourceHash")]
    public string SourceHash { get; set; } = "";
}

using System.Collections.Concurrent;
using System.Reflection;
using System.Text.Json;
using Jobsy.Core.Enums;
using Microsoft.Extensions.Logging;

namespace Jobsy.Core.Rules;

/// <summary>
/// Localized prompts/examples for uitgebreide-test items (JSON embedded resources).
/// Fallback to Dutch is explicit and counted — never silent.
/// </summary>
public static class DeepItemLocalizations
{
    private static readonly ConcurrentDictionary<string, IReadOnlyDictionary<int, DeepItemText>> Cache = new(StringComparer.OrdinalIgnoreCase);
    private static readonly ConcurrentDictionary<string, int> FallbackCounts = new(StringComparer.OrdinalIgnoreCase);
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public static DeepItemText Resolve(
        AssessmentKind kind,
        int id,
        string promptNl,
        string exampleNl,
        string lang,
        ILogger? logger = null)
    {
        var normalized = NormalizeLang(lang);
        if (normalized is "nl")
        {
            return new DeepItemText(promptNl, exampleNl);
        }

        var map = Load(kind, normalized);
        if (map.TryGetValue(id, out var text)
            && !string.IsNullOrWhiteSpace(text.Prompt)
            && !string.IsNullOrWhiteSpace(text.Example))
        {
            return text;
        }

        var key = $"{kind}:{normalized}";
        FallbackCounts.AddOrUpdate(key, 1, static (_, n) => n + 1);
        logger?.LogWarning(
            "Deep item fallback to nl for {Kind}#{Id} lang={Lang}",
            kind, id, normalized);
        return new DeepItemText(promptNl, exampleNl);
    }

    public static int FallbackCount(AssessmentKind kind, string lang)
        => FallbackCounts.TryGetValue($"{kind}:{NormalizeLang(lang)}", out var n) ? n : 0;

    public static void ResetFallbackCountsForTests() => FallbackCounts.Clear();

    public static IReadOnlyDictionary<int, DeepItemText> Load(AssessmentKind kind, string lang)
    {
        var slug = kind switch
        {
            AssessmentKind.Career => "career",
            AssessmentKind.Culture => "culture",
            AssessmentKind.Values => "values",
            _ => "competence"
        };
        var cacheKey = $"{slug}-{NormalizeLang(lang)}";
        return Cache.GetOrAdd(cacheKey, static key =>
        {
            var asm = typeof(DeepItemLocalizations).Assembly;
            var resource = asm.GetManifestResourceNames()
                .FirstOrDefault(n => n.EndsWith($"Data.DeepItems.{key}.json", StringComparison.OrdinalIgnoreCase));
            if (resource is null)
            {
                return new Dictionary<int, DeepItemText>();
            }

            using var stream = asm.GetManifestResourceStream(resource)!;
            using var reader = new StreamReader(stream);
            var json = reader.ReadToEnd();
            var raw = JsonSerializer.Deserialize<Dictionary<string, DeepItemDto>>(json, JsonOptions)
                      ?? new Dictionary<string, DeepItemDto>();
            var map = new Dictionary<int, DeepItemText>();
            foreach (var (idText, dto) in raw)
            {
                if (!int.TryParse(idText, out var id)) continue;
                if (string.IsNullOrWhiteSpace(dto.Prompt)) continue;
                map[id] = new DeepItemText(dto.Prompt, dto.Example ?? "");
            }

            return map;
        });
    }

    public static string NormalizeLang(string? lang)
    {
        var l = (lang ?? "nl").Trim().ToLowerInvariant();
        return l switch
        {
            "en" or "en-gb" or "en-us" => "en",
            "pl" or "pl-pl" => "pl",
            "ro" or "ro-ro" => "ro",
            "ar" or "ar-sa" or "ar-ae" => "ar",
            _ => "nl"
        };
    }

    private sealed class DeepItemDto
    {
        public string Prompt { get; set; } = "";
        public string? Example { get; set; }
        public bool Reverse { get; set; }
        public string? Domain { get; set; }
    }
}

public sealed record DeepItemText(string Prompt, string Example);

public static class DeepAnalysisQuestionLocales
{
    public static string Prompt(this DeepAnalysisQuestion question, string lang, ILogger? logger = null)
    {
        var kind = FamilyToKind(question.Family);
        var exampleNl = DeepAnalysisQuestionHelp.ExampleFor(question);
        return DeepItemLocalizations.Resolve(kind, question.Id, question.PromptNl, exampleNl, lang, logger).Prompt;
    }

    public static string Example(this DeepAnalysisQuestion question, string lang, ILogger? logger = null)
    {
        var kind = FamilyToKind(question.Family);
        var exampleNl = DeepAnalysisQuestionHelp.ExampleFor(question);
        return DeepItemLocalizations.Resolve(kind, question.Id, question.PromptNl, exampleNl, lang, logger).Example;
    }

    private static AssessmentKind FamilyToKind(string family)
        => family.ToLowerInvariant() switch
        {
            "riasec" or "career" => AssessmentKind.Career,
            "culture" or "disc" => AssessmentKind.Culture,
            "schwartz" or "values" => AssessmentKind.Values,
            _ => AssessmentKind.Competence // BigFive
        };
}

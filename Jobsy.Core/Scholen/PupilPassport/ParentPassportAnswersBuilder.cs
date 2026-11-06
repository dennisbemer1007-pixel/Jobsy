using System.Text.Json;
using Jobsy.Core.Rules;
using Jobsy.Core.Scholen.QuestionSets;

namespace Jobsy.Core.Scholen.PupilPassport;

/// <summary>
/// Read-only Q&amp;A for the parent portal: question and chosen answer label keys only (no Likert values or scores).
/// </summary>
public static class ParentPassportAnswersBuilder
{
    public static ParentPassportAnswersWebDto Build(PupilQuestionSetDef def, string? answersJson)
    {
        var answers = ParseAnswers(answersJson);
        var sections = new List<ParentPassportAnswerSectionDto>();

        foreach (var world in def.Worlds)
        {
            var meta = PupilWorldCatalog.Find(world.WorldKey);
            if (meta is null)
            {
                continue;
            }

            var items = new List<ParentPassportAnswerItemDto>();
            foreach (var item in def.Bank.AllItems)
            {
                if (!string.Equals(item.WorldKey, world.WorldKey, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                if (!answers.TryGetValue(item.Id, out var value) || !LikertAnswerJson.IsValidAnswer(value))
                {
                    continue;
                }

                var labelKey = LabelKeyFor(def, value);
                if (labelKey is null)
                {
                    continue;
                }

                items.Add(new ParentPassportAnswerItemDto(item.TextKey, labelKey));
            }

            if (items.Count > 0)
            {
                sections.Add(new ParentPassportAnswerSectionDto(meta.TitleKey, meta.SubtitleKey, items));
            }
        }

        return new ParentPassportAnswersWebDto(sections.Count > 0, sections);
    }

    private static string? LabelKeyFor(PupilQuestionSetDef def, int value)
    {
        foreach (var (v, key) in def.AnswerLabels)
        {
            if (v == value)
            {
                return key;
            }
        }

        return null;
    }

    private static Dictionary<string, int> ParseAnswers(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        }

        try
        {
            return JsonSerializer.Deserialize<Dictionary<string, int>>(json)
                   ?? new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        }
        catch (JsonException)
        {
            return new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        }
    }
}

public sealed record ParentPassportAnswerItemDto(string QuestionTextKey, string AnswerLabelKey);

public sealed record ParentPassportAnswerSectionDto(
    string TitleKey,
    string SubtitleKey,
    IReadOnlyList<ParentPassportAnswerItemDto> Items);

public sealed record ParentPassportAnswersWebDto(
    bool Enabled,
    IReadOnlyList<ParentPassportAnswerSectionDto> Sections);

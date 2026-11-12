using System.Text.Json;
using Jobsy.Core.Entities.Scholen;
using Jobsy.Core.Enums;
using Jobsy.Core.Rules;
using Jobsy.Core.Scholen.PupilPassport;

namespace Jobsy.Core.Scholen;

public enum G78StoryBeat
{
    Doen,
    Samen,
    Waarden,
    SchoolLater
}

public sealed record G78AnswerClause(
    int ItemId,
    string Category,
    AssessmentKind Model,
    G78StoryBeat Beat,
    string Text,
    int EffectiveScore);

public sealed record PupilG78PreferencesCompact(
    IReadOnlyList<string> SchoolLeuk,
    IReadOnlyList<string> HobbyLeuk,
    IReadOnlyList<string> BelangrijkItems);

public sealed record PupilG78StoryPackage(
    string Body,
    IReadOnlyList<int> UsedItemIds,
    PupilG78PreferencesCompact Preferences,
    CandidateFactSheet FactSheet);

/// <summary>Fact-only G78 "Dit ben jij" story from Likert answers (deterministic, no AI).</summary>
public static class PupilG78FactStory
{
    public const string ParagraphBreak = "\n\n";
    private static readonly PupilQuestionBank Bank = new();

    private static readonly JsonSerializerOptions JsonOpts = new() { PropertyNameCaseInsensitive = true };

    public static bool IsGroep78(PupilResult result)
        => string.Equals(result.ScoringVersion, "g78-1", StringComparison.OrdinalIgnoreCase);

    public static PupilG78StoryPackage Build(PupilResult result, PupilProgress? progress, string language = "nl")
    {
        var answers = ParseAnswers(progress?.AnswersJson);
        var clauses = CollectClauses(answers, language);
        var vak = PupilVakScores.Parse(progress?.VakScoresJson);
        var paragraphs = PupilG78StoryComposer.ComposeParagraphs(
            clauses, result, progress, vak, language, out var usedIds);
        var body = string.Join(ParagraphBreak, paragraphs);
        var preferences = BuildPreferences(clauses, answers, usedIds, progress, vak, language);
        var sheet = BuildFactSheet(clauses, progress, vak, language);
        return new PupilG78StoryPackage(body, usedIds, preferences, sheet);
    }

    public static IReadOnlyList<string> SplitParagraphs(string body)
    {
        if (string.IsNullOrWhiteSpace(body))
        {
            return [];
        }

        return body.Split(ParagraphBreak, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(p => p.Length > 0)
            .ToList();
    }

    public static string ForSpeakAloud(string body)
        => string.Join(' ', SplitParagraphs(body));

    private static List<G78AnswerClause> CollectClauses(IReadOnlyDictionary<string, int> answers, string language)
    {
        var list = new List<G78AnswerClause>();
        foreach (var item in Bank.AllItems)
        {
            if (!answers.TryGetValue(item.Id, out var raw))
            {
                continue;
            }

            var q = Bank.Questions.FirstOrDefault(x => x.Id == item.NumericId);
            if (q is null)
            {
                continue;
            }

            var eff = PupilQuestionStoryParaphrase.EffectiveScore(raw, q.Reverse);
            var text = eff >= 4
                ? PupilQuestionStoryParaphrase.StrongAgreeClause(q.Id, q.Reverse, raw, language)
                : eff <= 2
                    ? PupilQuestionStoryParaphrase.StrongDisagreeClause(q.Id, q.Reverse, raw, language)
                    : null;
            if (string.IsNullOrWhiteSpace(text))
            {
                continue;
            }

            list.Add(new G78AnswerClause(
                q.Id,
                q.Category,
                q.Model,
                BeatFor(q),
                PupilQuestionStoryParaphrase.PolishJeForm(text),
                eff));
        }

        return list;
    }

    private static G78StoryBeat BeatFor(PupilQuestion q)
    {
        if (q.Model == AssessmentKind.Values)
        {
            return G78StoryBeat.Waarden;
        }

        if (q.Model == AssessmentKind.Culture
            && q.Category is Rules.CulturePersonalityCatalog.Collaboration
                or Rules.CulturePersonalityCatalog.PeopleFirst)
        {
            return G78StoryBeat.Samen;
        }

        if (q.Model == AssessmentKind.Culture)
        {
            return G78StoryBeat.SchoolLater;
        }

        if (q.Model == AssessmentKind.Career && q.Category is Rules.CareerTestCatalog.Social)
        {
            return G78StoryBeat.Samen;
        }

        if (q.Model == AssessmentKind.Career)
        {
            return G78StoryBeat.Doen;
        }

        if (q.Category is Rules.CompetencyTestCatalog.Samenwerken or Rules.CompetencyTestCatalog.Extraversie)
        {
            return G78StoryBeat.Samen;
        }

        return G78StoryBeat.Doen;
    }

    private static PupilG78PreferencesCompact BuildPreferences(
        IReadOnlyList<G78AnswerClause> clauses,
        IReadOnlyDictionary<string, int> answers,
        IReadOnlyList<int> usedStoryIds,
        PupilProgress? progress,
        IReadOnlyList<PupilVakScoreEntry> vak,
        string language)
    {
        var school = vak
            .Where(e => e.Leuk >= 4)
            .OrderByDescending(e => e.Leuk + e.Goed)
            .Select(e => PupilVakSubjectLabels.Nl(e.SubjectKey))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(5)
            .ToList();

        var hobbies = ParseChips(progress?.LikesJson)
            .Select(k => PupilPortalChipLabels.Label(k, language))
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        if (!string.IsNullOrWhiteSpace(progress?.LikeOtherWord))
        {
            hobbies.Add(progress.LikeOtherWord.Trim());
        }

        hobbies = DedupeHobbyVsSchool(hobbies, school).Take(6).ToList();

        var belangrijk = new List<string>();
        foreach (var item in Bank.AllItems.OrderBy(i => i.NumericId))
        {
            if (item.NumericId is < 9031 or > 9045)
            {
                continue;
            }

            if (!answers.TryGetValue(item.Id, out var raw))
            {
                continue;
            }

            var q = Bank.Questions.First(x => x.Id == item.NumericId);
            var eff = PupilQuestionStoryParaphrase.EffectiveScore(raw, q.Reverse);
            if (eff < 4)
            {
                continue;
            }

            var shortLabel = PupilG78ValueShortLabels.ForItem(item.NumericId);
            if (!string.IsNullOrWhiteSpace(shortLabel))
            {
                belangrijk.Add(shortLabel);
            }
        }

        belangrijk = belangrijk.Distinct(StringComparer.OrdinalIgnoreCase).Take(5).ToList();
        if (belangrijk.Count == 0)
        {
            belangrijk = clauses
                .Where(c => c.Model == AssessmentKind.Values && c.EffectiveScore >= 4)
                .OrderByDescending(c => c.EffectiveScore)
                .Select(c => PupilG78ValueShortLabels.FromClause(c.Text))
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Cast<string>()
                .Take(4)
                .ToList();
        }

        _ = usedStoryIds;
        return new PupilG78PreferencesCompact(school, hobbies, belangrijk);
    }

    private static List<string> DedupeHobbyVsSchool(List<string> hobbies, List<string> school)
    {
        var result = new List<string>();
        foreach (var h in hobbies)
        {
            if (string.Equals(h, "Sport", StringComparison.OrdinalIgnoreCase)
                && school.Any(s => s.Contains("gym", StringComparison.OrdinalIgnoreCase)))
            {
                result.Add(h);
                continue;
            }

            if (string.Equals(h, "Natuur", StringComparison.OrdinalIgnoreCase)
                && school.Any(s => s.Contains("natuur", StringComparison.OrdinalIgnoreCase)))
            {
                result.Add(h);
                continue;
            }

            if (school.Any(s => string.Equals(s, h, StringComparison.OrdinalIgnoreCase)))
            {
                continue;
            }

            result.Add(h);
        }

        return result;
    }

    private static CandidateFactSheet BuildFactSheet(
        IReadOnlyList<G78AnswerClause> clauses,
        PupilProgress? progress,
        IReadOnlyList<PupilVakScoreEntry> vak,
        string language)
    {
        var confirmed = new List<string>();
        foreach (var c in clauses)
        {
            confirmed.Add(c.Text);
        }

        foreach (var chip in ParseChips(progress?.LikesJson))
        {
            confirmed.Add(PupilPortalChipLabels.Label(chip, language));
        }

        foreach (var v in vak)
        {
            confirmed.Add(PupilVakSubjectLabels.Nl(v.SubjectKey));
        }

        return CandidateFactSheet.Personal([], [], [], confirmedItems: confirmed);
    }

    private static Dictionary<string, int> ParseAnswers(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return new Dictionary<string, int>(StringComparer.Ordinal);
        }

        try
        {
            return JsonSerializer.Deserialize<Dictionary<string, int>>(json, JsonOpts)
                   ?? new Dictionary<string, int>(StringComparer.Ordinal);
        }
        catch (JsonException)
        {
            return new Dictionary<string, int>(StringComparer.Ordinal);
        }
    }

    private static IReadOnlyList<string> ParseChips(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return [];
        }

        try
        {
            return JsonSerializer.Deserialize<List<string>>(json, JsonOpts) ?? [];
        }
        catch (JsonException)
        {
            return [];
        }
    }

}

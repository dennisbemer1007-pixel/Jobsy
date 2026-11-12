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
    IReadOnlyList<string> LeukItems,
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
        var usedIds = new List<int>();
        var usedCategories = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var paragraphs = new List<string>();

        var usedItemIds = new HashSet<int>();
        AddParagraph(paragraphs, usedIds, usedCategories, usedItemIds, Pick(clauses, G78StoryBeat.Doen, usedCategories, usedItemIds, 2));
        AddParagraph(paragraphs, usedIds, usedCategories, usedItemIds, Pick(clauses, G78StoryBeat.Samen, usedCategories, usedItemIds, 2));
        AddParagraph(paragraphs, usedIds, usedCategories, usedItemIds, Pick(clauses, G78StoryBeat.Waarden, usedCategories, usedItemIds, 2));
        var schoolBits = new List<string>();
        schoolBits.AddRange(SchoolVakLines(vak));
        schoolBits.AddRange(LikeLines(progress, language));
        var later = LaterLine(result, progress, language);
        if (!string.IsNullOrWhiteSpace(later))
        {
            schoolBits.Add(later);
        }

        if (schoolBits.Count > 0)
        {
            paragraphs.Add(string.Join(' ', schoolBits));
        }

        if (paragraphs.Count == 0)
        {
            paragraphs.Add(PupilVerhaalLocalized.IsEnglish(language)
                ? "You answered the questions yourself. There is no right or wrong."
                : "Je gaf zelf antwoord op de vragen. Er is geen goed of fout.");
        }

        while (paragraphs.Count > 4)
        {
            paragraphs.RemoveAt(paragraphs.Count - 2);
        }

        var body = string.Join(ParagraphBreak, paragraphs);
        var preferences = BuildPreferences(clauses, usedIds, progress, vak, language);
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

    private static void AddParagraph(
        List<string> paragraphs,
        List<int> usedIds,
        HashSet<string> usedCategories,
        HashSet<int> usedItemIds,
        IReadOnlyList<G78AnswerClause> picked)
    {
        if (picked.Count == 0)
        {
            return;
        }

        foreach (var c in picked)
        {
            usedIds.Add(c.ItemId);
            usedCategories.Add(c.Category);
            usedItemIds.Add(c.ItemId);
        }

        paragraphs.Add(JoinFlowing(picked.Select(p => p.Text)));
    }

    private static string JoinFlowing(IEnumerable<string> parts)
    {
        var list = parts.Where(p => !string.IsNullOrWhiteSpace(p)).Select(p => p.Trim()).ToList();
        if (list.Count == 0)
        {
            return "";
        }

        if (list.Count == 1)
        {
            return list[0];
        }

        var sb = new System.Text.StringBuilder();
        sb.Append(list[0].TrimEnd('.')).Append('.');
        for (var i = 1; i < list.Count; i++)
        {
            var next = list[i].Trim();
            var conn = i == list.Count - 1 ? " Ook" : " Daarnaast";
            if (next.StartsWith("In je vrije tijd", StringComparison.Ordinal)
                || next.StartsWith("Op school", StringComparison.Ordinal)
                || next.StartsWith("Later", StringComparison.Ordinal))
            {
                conn = " ";
            }

            if (conn is " Ook" or " Daarnaast" && next.StartsWith("Je ", StringComparison.Ordinal))
            {
                next = "je " + next[3..];
            }

            sb.Append(conn).Append(' ');
            sb.Append(next.TrimEnd('.')).Append('.');
        }

        return sb.ToString();
    }

    private static IReadOnlyList<G78AnswerClause> Pick(
        IReadOnlyList<G78AnswerClause> all,
        G78StoryBeat beat,
        HashSet<string> usedCategories,
        HashSet<int> usedItemIds,
        int max)
        => all
            .Where(c => c.Beat == beat
                        && !usedCategories.Contains(c.Category)
                        && !usedItemIds.Contains(c.ItemId))
            .OrderByDescending(c => c.EffectiveScore)
            .ThenBy(c => c.ItemId)
            .Take(max)
            .ToList();

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
                text,
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

        if (q.Model == AssessmentKind.Culture)
        {
            return G78StoryBeat.SchoolLater;
        }

        if (q.Model == AssessmentKind.Career)
        {
            return G78StoryBeat.Doen;
        }

        if (q.Category is Rules.CompetencyTestCatalog.Samenwerken or Rules.CompetencyTestCatalog.Extraversie)
        {
            return G78StoryBeat.Samen;
        }

        if (q.Model == AssessmentKind.Career && q.Category is Rules.CareerTestCatalog.Social)
        {
            return G78StoryBeat.Samen;
        }

        if (q.Model == AssessmentKind.Culture
            && q.Category is Rules.CulturePersonalityCatalog.Collaboration
                or Rules.CulturePersonalityCatalog.PeopleFirst)
        {
            return G78StoryBeat.Samen;
        }

        return G78StoryBeat.Doen;
    }

    private static IEnumerable<string> SchoolVakLines(IReadOnlyList<PupilVakScoreEntry> vak)
    {
        var fun = vak
            .Where(e => e.Leuk >= 4)
            .OrderByDescending(e => e.Leuk + e.Goed)
            .Take(3)
            .Select(e => PupilVakSubjectLabels.Nl(e.SubjectKey))
            .ToList();
        if (fun.Count == 0)
        {
            yield break;
        }

        var joined = JoinDutchList(fun);
        yield return $"Op school vind je {joined} leuk om te doen.";
    }

    private static IEnumerable<string> LikeLines(PupilProgress? progress, string language)
    {
        var likes = ParseChips(progress?.LikesJson);
        var labels = likes
            .Select(k => PupilPortalChipLabels.Label(k, language))
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .Take(3)
            .ToList();
        if (!string.IsNullOrWhiteSpace(progress?.LikeOtherWord))
        {
            labels.Add(progress.LikeOtherWord.Trim());
        }

        if (labels.Count == 0)
        {
            yield break;
        }

        yield return "In je vrije tijd vind je " + JoinDutchList(labels) + " leuk.";
    }

    private static string? LaterLine(PupilResult result, PupilProgress? progress, string language)
    {
        var key = progress?.DreamJobKey ?? result.DreamJobKey;
        if (string.IsNullOrWhiteSpace(key)
            || string.Equals(key, PupilDreamJobFit.UndecidedKey, StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        var title = PupilPassportLocalizedCopy.JobTitle(key.Trim(), language);
        return $"Later wil je misschien iets proberen als {title.ToLowerInvariant()}";
    }

    private static PupilG78PreferencesCompact BuildPreferences(
        IReadOnlyList<G78AnswerClause> clauses,
        IReadOnlyList<int> usedStoryIds,
        PupilProgress? progress,
        IReadOnlyList<PupilVakScoreEntry> vak,
        string language)
    {
        var used = usedStoryIds.ToHashSet();
        var leuk = new List<string>();
        leuk.AddRange(ParseChips(progress?.LikesJson)
            .Select(k => PupilPortalChipLabels.Label(k, language))
            .Where(s => !string.IsNullOrWhiteSpace(s)));
        if (!string.IsNullOrWhiteSpace(progress?.LikeOtherWord))
        {
            leuk.Add(progress.LikeOtherWord.Trim());
        }

        leuk.AddRange(vak
            .Where(e => e.Leuk >= 4)
            .OrderByDescending(e => e.Leuk + e.Goed)
            .Select(e => PupilVakSubjectLabels.Nl(e.SubjectKey)));

        var belangrijk = clauses
            .Where(c => c.Model == AssessmentKind.Values && !used.Contains(c.ItemId))
            .OrderByDescending(c => c.EffectiveScore)
            .ThenBy(c => c.ItemId)
            .Take(4)
            .Select(c => TrimForList(c.Text))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (belangrijk.Count == 0)
        {
            belangrijk = clauses
                .Where(c => c.Model == AssessmentKind.Culture && !used.Contains(c.ItemId))
                .OrderByDescending(c => c.EffectiveScore)
                .Take(2)
                .Select(c => TrimForList(c.Text))
                .ToList();
        }

        return new PupilG78PreferencesCompact(
            leuk.Distinct(StringComparer.OrdinalIgnoreCase).Take(8).ToList(),
            belangrijk.Take(6).ToList());
    }

    private static string TrimForList(string text)
    {
        var t = text.Trim().TrimEnd('.');
        if (t.StartsWith("Dit zei je minder vaak", StringComparison.Ordinal))
        {
            return t;
        }

        return t.Length > 72 ? t[..69] + "…" : t;
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

    private static string JoinDutchList(IReadOnlyList<string> items) => items.Count switch
    {
        0 => "",
        1 => items[0],
        2 => $"{items[0]} en {items[1]}",
        _ => string.Join(", ", items.Take(items.Count - 1)) + " en " + items[^1]
    };
}

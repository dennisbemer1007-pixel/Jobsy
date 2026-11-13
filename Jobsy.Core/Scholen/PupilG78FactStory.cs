using System.Text.Json;
using Jobsy.Core.Contracts.Scholen;
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

public sealed record PupilG78CompetencyExampleAnchor(string Category, int ItemId);

public sealed record PupilG78StoryPackage(
    string Body,
    IReadOnlyList<int> UsedItemIds,
    IReadOnlyList<int> StoryItemIds,
    IReadOnlyList<PupilStoryTileDto> Tiles,
    PupilG78PreferencesCompact Preferences,
    CandidateFactSheet FactSheet,
    IReadOnlyList<PupilG78GrowthCompetencyDto> GrowthCompetencies,
    IReadOnlyList<PupilG78CompetencyExampleAnchor> CompetencyExamples);

/// <summary>Fact-only G78 "Dit ben jij" story from Likert answers (deterministic, no AI).</summary>
public static class PupilG78FactStory
{
    public const string ParagraphBreak = "\n\n";
    private static readonly PupilQuestionBank BankV1 = new();
    private static readonly PupilQuestionBankG78V2 BankV2 = new();

    private static readonly JsonSerializerOptions JsonOpts = new() { PropertyNameCaseInsensitive = true };

    public static bool IsGroep78(PupilResult result)
        => PupilG78ScoringVersions.IsKnown(result.ScoringVersion);

    private static bool IsContrastV2(PupilResult result)
        => string.Equals(result.ScoringVersion, PupilG78ScoringVersions.V2, StringComparison.Ordinal);

    private static IPupilQuestionBank BankFor(PupilResult result)
        => IsContrastV2(result) ? BankV2 : BankV1;

    public static PupilG78StoryPackage Build(PupilResult result, PupilProgress? progress, string language = "nl")
    {
        var answers = ParseAnswers(progress?.AnswersJson);
        var contrast = IsContrastV2(result);
        var clauses = CollectClauses(answers, language, BankFor(result), contrast);
        var vak = PupilVakScores.Parse(progress?.VakScoresJson);
        var growthCategories = PupilG78GrowthCompetencies.SelectGrowthCategories(answers, maxCount: 4, contrast).ToList();
        var growthCategorySet = growthCategories.ToHashSet(StringComparer.OrdinalIgnoreCase);
        var storyStrongCategories = PupilG78ContentAllocation.StoryStrongCategories(
            answers,
            growthCategorySet,
            clauses: clauses,
            result: result,
            contrastV2: contrast);
        var competencyExampleIds = PupilG78ContentAllocation.ReserveCompetencyExamples(
            clauses,
            storyStrongCategories,
            growthCategorySet,
            excludeIds: null);
        var storyOpeningIds = PupilG78ContentAllocation.ReserveStorySocialOpenings(
            result,
            clauses,
            growthCategorySet);
        var competencyReserved = competencyExampleIds.ToHashSet();
        var storyReservedForTiles = competencyReserved
            .Union(storyOpeningIds)
            .ToHashSet();
        var tilePlan = PupilG78ContentAllocation.PlanTileItems(
            clauses,
            growthCategorySet,
            blockedIds: storyReservedForTiles,
            maxPerTile: 3);
        var reservedForStory = competencyExampleIds.ToHashSet();
        var tilePlanIds = tilePlan.Values.SelectMany(v => v).ToHashSet();
        IReadOnlyList<string> paragraphs = [];
        IReadOnlyList<int> storyIds = [];
        IReadOnlyList<PupilG78CompetencyExampleAnchor> competencyExamples = [];
        HashSet<int> fullStoryIds = [];
        for (var pass = 0; pass < 3; pass++)
        {
            tilePlanIds = tilePlan.Values.SelectMany(v => v).ToHashSet();
            var tilePreviewTexts = PreviewTileJeSentences(clauses, tilePlan);
            paragraphs = PupilG78StoryComposer.ComposeOverview(
                clauses,
                result,
                progress,
                vak,
                language,
                answers,
                tilePlanIds,
                tilePreviewTexts,
                growthCategorySet,
                storyStrongCategories,
                competencyExampleIds,
                contrastV2: contrast,
                out storyIds,
                out competencyExamples);
            fullStoryIds = storyIds.Union(storyOpeningIds).ToHashSet();
            var reconciled = PupilG78ContentAllocation.ReconcileTilePlanWithStory(
                tilePlan,
                clauses,
                fullStoryIds,
                growthCategorySet,
                storyReservedForTiles,
                maxPerTile: 3);
            if (PlansEqual(tilePlan, reconciled))
            {
                break;
            }

            tilePlan = reconciled;
        }

        tilePlanIds = tilePlan.Values.SelectMany(v => v).ToHashSet();
        fullStoryIds = storyIds.Union(storyOpeningIds).ToHashSet();
        var blockedForStory = reservedForStory.Union(tilePlanIds).ToHashSet();
        var storyReserved = blockedForStory.Union(fullStoryIds).ToHashSet();
        var (tiles, tileIds) = PupilG78FactTileComposer.BuildFromPlan(
            clauses,
            language,
            tilePlan,
            growthCategorySet,
            storyReservedForTiles,
            fullStoryIds,
            answers,
            contrast);
        var body = string.Join(ParagraphBreak, paragraphs);
        body = PupilG78PageTextDedupe.ScrubStoryAgainstTileExplanations(body, tiles);
        var tileIdSet = tileIds.ToHashSet();
        var pageItemIds = storyReserved.Union(tileIdSet).ToHashSet();
        var growth = PupilG78GrowthCompetencies.BuildForCategories(
            answers,
            growthCategories,
            pageItemIds,
            language,
            contrast);
        var allUsed = pageItemIds.ToList();
        var preferences = BuildPreferences(clauses, answers, allUsed, progress, vak, language);
        var sheet = BuildFactSheet(clauses, progress, vak, language);
        return new PupilG78StoryPackage(body, allUsed, fullStoryIds.ToList(), tiles, preferences, sheet, growth, competencyExamples);
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

    private static IReadOnlyList<string> PreviewTileJeSentences(
        IReadOnlyList<G78AnswerClause> clauses,
        IReadOnlyDictionary<AssessmentKind, IReadOnlyList<int>> tilePlan)
    {
        var byId = clauses.ToDictionary(c => c.ItemId);
        var texts = new List<string>();
        foreach (var ids in tilePlan.Values)
        {
            foreach (var id in ids)
            {
                if (!byId.TryGetValue(id, out var clause) || clause.EffectiveScore < 4)
                {
                    continue;
                }

                texts.Add(clause.Text);
            }
        }

        return texts;
    }

    private static List<G78AnswerClause> CollectClauses(
        IReadOnlyDictionary<string, int> answers,
        string language,
        IPupilQuestionBank bank,
        bool contrast)
    {
        var list = new List<G78AnswerClause>();
        foreach (var item in bank.AllItems)
        {
            if (!answers.TryGetValue(item.Id, out var raw))
            {
                continue;
            }

            var q = bank.Questions.FirstOrDefault(x => x.Id == item.NumericId);
            if (q is null)
            {
                continue;
            }

            string? text;
            int eff;
            string category;
            if (contrast && q.IsContrast)
            {
                var fact = PupilQuestionStoryParaphrase.ContrastChosenFact(q, raw, language);
                if (fact is null)
                {
                    continue;
                }

                (text, eff, category) = fact.Value;
            }
            else
            {
                var likert = raw;
                eff = PupilQuestionStoryParaphrase.EffectiveScore(raw, q.Reverse);
                text = eff >= 4
                    ? PupilQuestionStoryParaphrase.StrongAgreeClause(q.Id, false, likert, language)
                    : eff <= 2
                        ? PupilQuestionStoryParaphrase.StrongDisagreeClause(q.Id, false, likert, language)
                        : null;
                category = q.Category;
            }

            if (string.IsNullOrWhiteSpace(text))
            {
                continue;
            }

            var beatQ = category == q.Category ? q : q with { Category = category };
            list.Add(new G78AnswerClause(
                q.Id,
                category,
                q.Model,
                BeatFor(beatQ),
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
        var school = DedupePreferenceSubjectLabels(vak
            .Where(e => e.Leuk >= 4)
            .OrderByDescending(e => e.Leuk + e.Goed)
            .Select(e => PupilVakSubjectLabels.Nl(e.SubjectKey))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(6)
            .ToList())
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

        var belangrijkRanked = new List<(int Eff, string Label)>();
        var bank = BankV1;
        foreach (var item in bank.AllItems)
        {
            if (item.NumericId is < 9031 or > 9045)
            {
                continue;
            }

            if (!answers.TryGetValue(item.Id, out var raw))
            {
                continue;
            }

            var q = bank.Questions.First(x => x.Id == item.NumericId);
            var eff = PupilQuestionStoryParaphrase.EffectiveScore(raw, q.Reverse);
            if (eff < 4)
            {
                continue;
            }

            var shortLabel = PupilG78ValueShortLabels.ForItem(item.NumericId);
            if (!string.IsNullOrWhiteSpace(shortLabel))
            {
                belangrijkRanked.Add((eff, shortLabel));
            }
        }

        var belangrijk = belangrijkRanked
            .OrderByDescending(x => x.Eff)
            .ThenBy(x => x.Label, StringComparer.OrdinalIgnoreCase)
            .Select(x => x.Label)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(4)
            .ToList();
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

    private static bool PlansEqual(
        IReadOnlyDictionary<AssessmentKind, IReadOnlyList<int>> a,
        IReadOnlyDictionary<AssessmentKind, IReadOnlyList<int>> b)
    {
        if (a.Count != b.Count)
        {
            return false;
        }

        foreach (var (kind, ids) in a)
        {
            if (!b.TryGetValue(kind, out var other))
            {
                return false;
            }

            if (!ids.SequenceEqual(other))
            {
                return false;
            }
        }

        return true;
    }

    internal static List<string> DedupePreferenceSubjectLabels(IReadOnlyList<string> labels)
    {
        var ordered = labels
            .Select(CapitalizePreferenceLabel)
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .ToList();
        var kept = new List<string>();
        foreach (var label in ordered)
        {
            if (IsSubsumedByAnotherLabel(label, kept))
            {
                continue;
            }

            kept.RemoveAll(existing => IsSubsumedByAnotherLabel(existing, [label]));
            kept.Add(label);
        }

        return kept;
    }

    private static string CapitalizePreferenceLabel(string label)
    {
        var t = label.Trim();
        if (t.Length == 0)
        {
            return t;
        }

        return char.ToUpperInvariant(t[0]) + t[1..];
    }

    private static bool IsSubsumedByAnotherLabel(string label, IReadOnlyList<string> others)
    {
        foreach (var other in others)
        {
            if (string.Equals(other, label, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            if (other.Contains(label, StringComparison.OrdinalIgnoreCase) && label.Length < other.Length)
            {
                return true;
            }

            if (label.Contains(other, StringComparison.OrdinalIgnoreCase) && other.Length < label.Length)
            {
                return false;
            }
        }

        return false;
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

using System.Text.Json;
using Jobsy.Core.Entities.Scholen;
using Jobsy.Core.Enums;
using Jobsy.Core.Rules;
using Jobsy.Core.Scholen.Matching;
using Jobsy.Core.Scholen.PupilPassport;

namespace Jobsy.Core.Scholen;

/// <summary>Deterministic G78 job info panel from mapped data only (no AI).</summary>
public static class PupilG78JobMatchExplanation
{
    public const int MinSentences = 3;
    public const int MaxSentences = 5;

    private const string GenericBlurbKey = "LeerlingPassport.Card.Blurb.Generic.G78";

    private static readonly JsonSerializerOptions JsonOpts = new() { PropertyNameCaseInsensitive = true };
    private static readonly PupilQuestionBank G78Bank = new();

    private static readonly Dictionary<string, char> CategoryToLetter = new(StringComparer.OrdinalIgnoreCase)
    {
        [CareerTestCatalog.Realistic] = 'R',
        [CareerTestCatalog.Investigative] = 'I',
        [CareerTestCatalog.Artistic] = 'A',
        [CareerTestCatalog.Social] = 'S',
        [CareerTestCatalog.Enterprising] = 'E',
        [CareerTestCatalog.Conventional] = 'C',
    };

    private static readonly Dictionary<string, string> LikeToSubjectKey = new(StringComparer.OrdinalIgnoreCase)
    {
        ["tekenen"] = "tekenen",
        ["muziek"] = "muziek",
        ["sport"] = "gym",
        ["natuur"] = "natuur-techniek",
        ["techniek"] = "natuur-techniek",
        ["rekenen"] = "rekenen",
        ["computers"] = "natuur-techniek",
        ["programmeren"] = "natuur-techniek",
        ["bouwen"] = "natuur-techniek",
        ["fietsen-repareren"] = "natuur-techniek",
    };

    private static readonly string[] BannedPhrases =
    [
        "past bij dit beroep",
        "Toch past het",
        "Hier had je misschien zelf niet aan gedacht",
        "In de vragen koos je vaak",
        "sterk in bent",
        "scoorde",
        "scoor je",
    ];

    private sealed record AnswerPhrase(int ItemId, string Subordinate);

    public static string Build(
        string jobKey,
        PupilResult result,
        PupilProgress? progress,
        string language = "nl")
    {
        var panel = BuildContent(jobKey, result, progress, language);
        return PupilVerhaalLocalized.IsEnglish(language)
            ? panel.FormatCompactPlain()
            : panel.FormatCompactPlain();
    }

    public static string BuildHtml(
        string jobKey,
        PupilResult result,
        PupilProgress? progress,
        string language = "nl")
        => BuildContent(jobKey, result, progress, language).FormatHtml();

    public static PupilG78JobWhyContent BuildContent(
        string jobKey,
        PupilResult result,
        PupilProgress? progress,
        string language = "nl")
    {
        if (PupilVerhaalLocalized.IsEnglish(language))
        {
            return ComposeDutch(jobKey, result, progress);
        }

        return ComposeDutch(jobKey, result, progress);
    }

    public static IReadOnlyList<string> BuildSentences(
        string jobKey,
        PupilResult result,
        PupilProgress? progress,
        string language = "nl")
    {
        var content = BuildContent(jobKey, result, progress, language);
        return content.FormatFullPlain()
            .Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .ToList();
    }

    public static bool IsPlaceholderJobBlurb(string blurb)
    {
        if (string.IsNullOrWhiteSpace(blurb))
        {
            return true;
        }

        var generic = PupilPassportCopy.Get(GenericBlurbKey);
        return string.Equals(blurb.Trim(), generic, StringComparison.Ordinal)
               || blurb.Contains("beroep om over na te denken", StringComparison.OrdinalIgnoreCase);
    }

    public static bool ContainsBannedPhrase(string text)
        => BannedPhrases.Any(p => text.Contains(p, StringComparison.OrdinalIgnoreCase));

    private static PupilG78JobWhyContent ComposeDutch(string jobKey, PupilResult result, PupilProgress? progress)
    {
        var vak = PupilVakScores.Parse(progress?.VakScoresJson);
        var factors = PupilG78CareerScoring.ScoreFactors(jobKey, result, progress, vak);
        var likes = ParseChips(progress?.LikesJson);
        var unexpected = !JobInLikesOrDream(likes, progress, jobKey);

        var work = JobDescriptionSentences(jobKey);
        var (competencies, evidenceUsedInCompetencies) = CompetencyLines(jobKey, progress);
        var answers = AnswerReflectionParagraphs(
            jobKey,
            result,
            progress,
            vak,
            likes,
            factors.PrimaryRiasecLetter,
            unexpected,
            evidenceUsedInCompetencies);

        return new PupilG78JobWhyContent(work, competencies, answers);
    }

    private static IReadOnlyList<string> JobDescriptionSentences(string jobKey)
    {
        var blurb = PupilPassportLocalizedCopy.JobBlurb(jobKey, PupilQuestionSet.Groep78, "nl");
        if (IsPlaceholderJobBlurb(blurb))
        {
            return [];
        }

        var title = PupilPassportLocalizedCopy.JobTitle(jobKey, "nl").Trim().ToLowerInvariant();
        var lines = new List<string>();
        foreach (var part in blurb.Split('.', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).Take(3))
        {
            if (!part.StartsWith("Je ", StringComparison.Ordinal))
            {
                continue;
            }

            if (part.StartsWith("Je bent ", StringComparison.Ordinal))
            {
                lines.Add($"Een {title} is {part[8..].ToLowerInvariant()}.");
                continue;
            }

            var rest = part[3..].TrimEnd('.');
            rest = rest.Replace(" je ", " de ", StringComparison.Ordinal);
            lines.Add($"Een {title} {rest.ToLowerInvariant()}.");
        }

        return lines;
    }

    private static (IReadOnlyList<PupilG78JobCompetencyLine> Lines, HashSet<int> UsedEvidenceItemIds) CompetencyLines(
        string jobKey,
        PupilProgress? progress)
    {
        var reqs = PupilG78JobCompetencyMapping.ForJob(jobKey);
        if (reqs.Count == 0)
        {
            return ([], []);
        }

        var lines = new List<PupilG78JobCompetencyLine>();
        var usedItems = new HashSet<int>();
        foreach (var req in reqs)
        {
            var (reflection, itemId) = PupilG78CompetenceFromAnswers.PupilReflectionForEvidence(
                req.SlotKey,
                jobKey,
                req.EvidenceItemIds,
                progress?.AnswersJson,
                progress);
            if (itemId is > 0)
            {
                usedItems.Add(itemId.Value);
            }

            lines.Add(new PupilG78JobCompetencyLine(req.JobAsksLabel, reflection));
        }

        return (lines, usedItems);
    }

    private static List<string> AnswerReflectionParagraphs(
        string jobKey,
        PupilResult result,
        PupilProgress? progress,
        IReadOnlyList<PupilVakScoreEntry> vakScores,
        IReadOnlySet<string> likes,
        char jobLetter,
        bool unexpected,
        IReadOnlySet<int> reservedEvidenceItemIds)
    {
        var sentences = new List<string>(5);
        var usedSemantic = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var id in reservedEvidenceItemIds)
        {
            usedSemantic.Add(PupilG78HighAnswerParaphrases.SemanticKey(id));
        }

        if (unexpected)
        {
            var topic = SurpriseTopic(progress, jobLetter);
            sentences.Add(string.IsNullOrWhiteSpace(topic)
                ? "Dit beroep kwam misschien onverwacht naar voren. Het kwam uit je antwoorden in de vragenlijsten."
                : $"Dit beroep kwam misschien onverwacht naar voren. Het kwam uit je antwoorden over {topic}.");
        }
        else
        {
            var likeLabel = LikeLabel(jobKey, likes);
            if (likeLabel is not null)
            {
                sentences.Add($"Dit beroep kwam naar voren omdat je {likeLabel} leuk vindt.");
                if (LikeToSubjectKey.Values.Any(sk => likes.Any(l => LikeToSubjectKey.GetValueOrDefault(l) == sk)))
                {
                    foreach (var like in likes)
                    {
                        if (LikeToSubjectKey.TryGetValue(like, out var sk))
                        {
                            usedSemantic.Add($"vak-{sk}");
                        }
                    }
                }
            }
        }

        var answers = CollectAnswers(jobLetter, progress, usedSemantic, reservedEvidenceItemIds, max: 3);
        if (answers.Count > 0)
        {
            var joined = string.Join(", en dat ", answers.Select(a => a.Subordinate));
            if (sentences.Count > 0)
            {
                sentences.Add($"Je antwoorden laten ook zien dat {joined}.");
            }
            else
            {
                sentences.Add($"Je antwoorden laten zien dat {joined}.");
            }
        }

        var subject = SubjectLabel(jobKey, vakScores, likes, usedSemantic);
        if (subject is not null)
        {
            sentences.Add($"Op school gaf je aan dat je goed bent in {subject}.");
        }

        var riasec = RiasecDriverSentence(jobLetter, result);
        if (riasec is not null)
        {
            sentences.Add(riasec);
        }

        var contrast = NeutralContrastSentence(jobLetter, result);
        if (contrast is not null)
        {
            sentences.Add(contrast);
        }

        var distinct = sentences
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .Distinct(StringComparer.Ordinal)
            .Take(MaxSentences)
            .ToList();
        return FlowAnswerParagraphs(distinct);
    }

    private static List<string> FlowAnswerParagraphs(IReadOnlyList<string> sentences)
    {
        if (sentences.Count == 0)
        {
            return [];
        }

        if (sentences.Count <= 2)
        {
            return [string.Join(' ', sentences)];
        }

        var mid = (sentences.Count + 1) / 2;
        var p1 = string.Join(' ', sentences.Take(mid));
        var p2 = string.Join(' ', sentences.Skip(mid));
        return string.IsNullOrWhiteSpace(p2) ? [p1] : [p1, p2];
    }

    private static string? RiasecDriverSentence(char jobLetter, PupilResult result)
    {
        var scores = ParseRiasecScores(result.RiasecScoresJson);
        if (scores is null || !scores.TryGetValue(jobLetter, out var jobScore) || jobScore < 55)
        {
            return null;
        }

        var label = RiasecLetterLabel(jobLetter);
        return $"In de beroepentest kwam {label} bij jou naar voren.";
    }

    private static string? NeutralContrastSentence(char jobLetter, PupilResult result)
    {
        var scores = ParseRiasecScores(result.RiasecScoresJson);
        if (scores is null || scores.Count < 2)
        {
            return null;
        }

        var other = scores
            .Where(kv => char.ToUpperInvariant(kv.Key) != char.ToUpperInvariant(jobLetter))
            .OrderByDescending(kv => kv.Value)
            .FirstOrDefault();
        if (other.Key == default || other.Value < 50)
        {
            return null;
        }

        var label = RiasecLetterLabel(other.Key);
        return $"Sommige antwoorden laten ook ruimte zien voor werk met meer {label}.";
    }

    private static Dictionary<char, int>? ParseRiasecScores(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return null;
        }

        try
        {
            var doc = JsonSerializer.Deserialize<Dictionary<string, int>>(json, JsonOpts);
            if (doc is null || doc.Count == 0)
            {
                return null;
            }

            var map = new Dictionary<char, int>();
            foreach (var (key, value) in doc)
            {
                var letter = key.Trim().ToLowerInvariant() switch
                {
                    "realistic" or "r" => 'R',
                    "investigative" or "i" => 'I',
                    "artistic" or "a" => 'A',
                    "social" or "s" => 'S',
                    "enterprising" or "e" => 'E',
                    "conventional" or "c" => 'C',
                    _ => '\0'
                };
                if (letter != '\0')
                {
                    map[letter] = value;
                }
            }

            return map.Count > 0 ? map : null;
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static string FormatSubjectForPanel(string subjectKey)
    {
        if (string.Equals(subjectKey, "tekenen", StringComparison.OrdinalIgnoreCase))
        {
            return "tekenen en handvaardigheid";
        }

        return PupilVakSubjectLabels.Nl(subjectKey).ToLowerInvariant();
    }

    private static string RiasecLetterLabel(char letter) => letter switch
    {
        'R' => "praktisch werken en bouwen",
        'I' => "uitzoeken en begrijpen",
        'A' => "creatief bezig zijn",
        'S' => "mensen helpen",
        'E' => "leiding nemen",
        'C' => "ordelijk werken",
        _ => "andere interesses"
    };

    private static string? SurpriseTopic(PupilProgress? progress, char jobLetter)
    {
        var contrastV2 = PupilG78ContrastAnswerPhrases.IsContrastSession(progress);
        var answers = ParseAnswers(progress?.AnswersJson, contrastV2);
        var careerItems = contrastV2
            ? new PupilQuestionBankG78V2().AsCareerItems()
            : G78Bank.AsCareerItems();
        var item = careerItems
            .Where(q =>
            {
                if (!answers.TryGetValue(q.Id, out var value))
                {
                    return false;
                }

                if (contrastV2)
                {
                    var qFull = new PupilQuestionBankG78V2().Questions.First(x => x.Id == q.Id);
                    return PupilG78ContrastAnswerPhrases.TryGetCareerLetter(qFull, value, out var contrastLetter)
                           && contrastLetter == jobLetter;
                }

                return value >= 4
                       && CategoryToLetter.TryGetValue(q.Category, out var categoryLetter)
                       && categoryLetter == jobLetter;
            })
            .OrderByDescending(q => answers[q.Id])
            .FirstOrDefault();

        if (item is null)
        {
            return null;
        }

        string sub;
        if (contrastV2)
        {
            var qFull = new PupilQuestionBankG78V2().Questions.First(x => x.Id == item.Id);
            if (!answers.TryGetValue(item.Id, out var raw)
                || !PupilG78ContrastAnswerPhrases.TryGetSubordinate(qFull, raw, out sub))
            {
                return null;
            }
        }
        else if (!PupilG78HighAnswerParaphrases.TryGetSubordinateClause(item.Id, out sub))
        {
            return null;
        }

        if (sub.Contains("buiten", StringComparison.Ordinal))
        {
            return "buiten werken";
        }

        if (sub.Contains("maakt", StringComparison.Ordinal) || sub.Contains("repareert", StringComparison.Ordinal))
        {
            return "maken en repareren";
        }

        if (sub.Contains("uitzoekt", StringComparison.Ordinal))
        {
            return "uitzoeken hoe dingen werken";
        }

        if (sub.Contains("moois maakt", StringComparison.Ordinal))
        {
            return "creatief bezig zijn";
        }

        return "wat je energie geeft";
    }

    private static List<AnswerPhrase> CollectAnswers(
        char jobLetter,
        PupilProgress? progress,
        HashSet<string> usedSemantic,
        IReadOnlySet<int> reservedItemIds,
        int max)
    {
        var contrastV2 = PupilG78ContrastAnswerPhrases.IsContrastSession(progress);
        var answers = ParseAnswers(progress?.AnswersJson, contrastV2);
        var list = new List<AnswerPhrase>();
        var careerItems = contrastV2
            ? new PupilQuestionBankG78V2().AsCareerItems()
            : G78Bank.AsCareerItems();
        foreach (var item in careerItems
                     .Where(q =>
                     {
                         if (reservedItemIds.Contains(q.Id))
                         {
                             return false;
                         }

                         if (!answers.TryGetValue(q.Id, out var value))
                         {
                             return false;
                         }

                         if (contrastV2)
                         {
                             var qFull = new PupilQuestionBankG78V2().Questions.First(x => x.Id == q.Id);
                             return PupilG78ContrastAnswerPhrases.TryGetCareerLetter(qFull, value, out var contrastLetter)
                                    && contrastLetter == jobLetter;
                         }

                         return value >= 4
                                && CategoryToLetter.TryGetValue(q.Category, out var categoryLetter)
                                && categoryLetter == jobLetter;
                     })
                     .OrderByDescending(q => answers[q.Id])
                     .ThenBy(q => q.Id))
        {
            var key = PupilG78HighAnswerParaphrases.SemanticKey(item.Id);
            if (!usedSemantic.Add(key))
            {
                continue;
            }

            if (!answers.TryGetValue(item.Id, out var raw))
            {
                continue;
            }

            string sub;
            if (contrastV2)
            {
                var qFull = new PupilQuestionBankG78V2().Questions.First(x => x.Id == item.Id);
                if (!PupilG78ContrastAnswerPhrases.TryGetSubordinate(qFull, raw, out sub))
                {
                    continue;
                }
            }
            else if (!PupilG78HighAnswerParaphrases.TryGetSubordinateClause(item.Id, out sub))
            {
                continue;
            }

            list.Add(new AnswerPhrase(item.Id, sub));
            if (list.Count >= max)
            {
                break;
            }
        }

        return list;
    }

    private static string? LikeLabel(string jobKey, IReadOnlySet<string> likes)
    {
        foreach (var like in likes)
        {
            if (!PupilRiasecJobIdeas.LikeJobsForScoring.TryGetValue(like, out var jobs))
            {
                continue;
            }

            if (!jobs.Any(j => string.Equals(j, jobKey, StringComparison.OrdinalIgnoreCase)))
            {
                continue;
            }

            var label = PupilPortalChipLabels.Dutch(like);
            return string.IsNullOrWhiteSpace(label) ? null : label.ToLowerInvariant();
        }

        return null;
    }

    private static string? SubjectLabel(
        string jobKey,
        IReadOnlyList<PupilVakScoreEntry> vakScores,
        IReadOnlySet<string> likes,
        HashSet<string> usedSemantic)
    {
        var best = vakScores
            .Select(v =>
            {
                if (!SubjectContributesToJob(jobKey, v))
                {
                    return (Entry: v, Score: 0.0);
                }

                if (likes.Any(like =>
                        LikeToSubjectKey.TryGetValue(like, out var sk)
                        && string.Equals(sk, v.SubjectKey, StringComparison.OrdinalIgnoreCase)))
                {
                    return (Entry: v, Score: 0.0);
                }

                var key = $"vak-{v.SubjectKey}";
                if (usedSemantic.Contains(key))
                {
                    return (Entry: v, Score: 0.0);
                }

                return (Entry: v, Score: PupilVakScores.SubjectWeight(v));
            })
            .Where(x => x.Score > 0.4)
            .OrderByDescending(x => x.Score)
            .FirstOrDefault();

        if (best.Score <= 0)
        {
            return null;
        }

        return FormatSubjectForPanel(best.Entry.SubjectKey);
    }

    private static bool SubjectContributesToJob(string jobKey, PupilVakScoreEntry entry)
    {
        var row = PupilSubjectIscedCatalog.TryGetG78(entry.SubjectKey);
        if (row is { CountsInMatch: false })
        {
            return false;
        }

        if (row?.LinkedOccupationJobKeys?.Any(j =>
                string.Equals(j, jobKey, StringComparison.OrdinalIgnoreCase)) == true)
        {
            return PupilVakScores.SubjectWeight(entry) >= 0.45;
        }

        var occCodes = PupilDreamJobEscoCatalog.IscedCodesForJob(jobKey);
        if (occCodes.Count > 0
            && row?.IscedCodes.Any(c => occCodes.Any(o => string.Equals(o, c, StringComparison.OrdinalIgnoreCase))) == true)
        {
            return PupilVakScores.SubjectWeight(entry) >= 0.55;
        }

        return false;
    }

    private static bool JobInLikesOrDream(IReadOnlySet<string> likes, PupilProgress? progress, string jobKey)
    {
        foreach (var like in likes)
        {
            if (PupilRiasecJobIdeas.LikeJobsForScoring.TryGetValue(like, out var jobs)
                && jobs.Any(j => string.Equals(j, jobKey, StringComparison.OrdinalIgnoreCase)))
            {
                return true;
            }
        }

        var dream = (progress?.DreamJobKey ?? "").Trim();
        return string.Equals(dream, jobKey, StringComparison.OrdinalIgnoreCase);
    }

    private static Dictionary<int, int> ParseAnswers(string? json, bool contrastV2 = false)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return [];
        }

        try
        {
            var byString = JsonSerializer.Deserialize<Dictionary<string, int>>(json, JsonOpts) ?? [];
            var map = new Dictionary<int, int>();
            var items = contrastV2 ? new PupilQuestionBankG78V2().AllItems : G78Bank.AllItems;
            foreach (var item in items)
            {
                if (byString.TryGetValue(item.Id, out var value) && LikertAnswerJson.IsValidAnswer(value))
                {
                    map[item.NumericId] = value;
                }
            }

            return map;
        }
        catch (JsonException)
        {
            return [];
        }
    }

    private static HashSet<string> ParseChips(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        }

        try
        {
            var list = JsonSerializer.Deserialize<List<string>>(json) ?? [];
            return new HashSet<string>(list, StringComparer.OrdinalIgnoreCase);
        }
        catch (JsonException)
        {
            return new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        }
    }
}

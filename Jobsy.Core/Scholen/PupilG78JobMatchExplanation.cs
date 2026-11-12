using System.Text.Json;
using Jobsy.Core.Entities.Scholen;
using Jobsy.Core.Enums;
using Jobsy.Core.Rules;
using Jobsy.Core.Scholen.Matching;
using Jobsy.Core.Scholen.PupilPassport;

namespace Jobsy.Core.Scholen;

/// <summary>Deterministic "Waarom past dit bij jou?" copy from score drivers and pupil answers (no AI, no invented facts).</summary>
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

    private sealed record AnswerPhrase(int ItemId, string Subordinate, string MainAfterWant);

    public static string Build(
        string jobKey,
        PupilResult result,
        PupilProgress? progress,
        string language = "nl")
    {
        var sentences = BuildSentences(jobKey, result, progress, language);
        return string.Join(' ', sentences);
    }

    public static IReadOnlyList<string> BuildSentences(
        string jobKey,
        PupilResult result,
        PupilProgress? progress,
        string language = "nl")
    {
        if (PupilVerhaalLocalized.IsEnglish(language))
        {
            return BuildEnglishSentences(jobKey, result, progress);
        }

        return ComposeDutch(jobKey, result, progress);
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

    private static List<string> ComposeDutch(string jobKey, PupilResult result, PupilProgress? progress)
    {
        var vak = PupilVakScores.Parse(progress?.VakScoresJson);
        var factors = PupilG78CareerScoring.ScoreFactors(jobKey, result, progress, vak);
        var likes = ParseChips(progress?.LikesJson);
        var surprise = !JobInLikesOrDream(likes, progress, jobKey);

        var usedSemantic = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var likeLabel = LikeLabel(jobKey, likes);
        if (likeLabel is not null)
        {
            foreach (var like in likes)
            {
                if (LikeToSubjectKey.TryGetValue(like, out var sk))
                {
                    usedSemantic.Add($"vak-{sk}");
                }
            }
        }

        var answers = new List<AnswerPhrase>();
        foreach (var phrase in AnswerPhrases(factors.PrimaryRiasecLetter, progress))
        {
            var key = PupilG78HighAnswerParaphrases.SemanticKey(phrase.ItemId);
            if (!usedSemantic.Add(key))
            {
                continue;
            }

            answers.Add(phrase);
            if (answers.Count >= 2)
            {
                break;
            }
        }

        var subjectLabel = SubjectLabel(jobKey, vak, likes, usedSemantic);
        var jobSentences = JobDescriptionNl(jobKey);

        return StitchSentences(likeLabel, answers, subjectLabel, jobSentences, surprise);
    }

    private static List<string> StitchSentences(
        string? likeLabel,
        IReadOnlyList<AnswerPhrase> answers,
        string? subjectLabel,
        IReadOnlyList<string> jobSentences,
        bool surprise)
    {
        var reserveForJob = Math.Min(jobSentences.Count, 2);
        var sentences = new List<string>(MaxSentences);

        var usedFirstAnswerInWant = false;
        if (surprise && answers.Count > 0)
        {
            sentences.Add(
                $"Hier had je misschien zelf niet aan gedacht. Toch past het, want {answers[0].MainAfterWant}.");
            usedFirstAnswerInWant = true;
        }
        else if (surprise)
        {
            sentences.Add("Hier had je misschien zelf niet aan gedacht. Toch laten je antwoorden zien dat dit beroep bij je kan passen.");
        }
        else if (answers.Count > 0 && likeLabel is null)
        {
            sentences.Add($"Dit past bij jou, want {answers[0].MainAfterWant}.");
            usedFirstAnswerInWant = true;
        }

        if (!string.IsNullOrWhiteSpace(likeLabel))
        {
            sentences.Add($"Je vindt {likeLabel} leuk, en dat past bij dit beroep.");
        }

        var answerOffset = usedFirstAnswerInWant ? 1 : 0;
        for (var i = answerOffset; i < answers.Count; i++)
        {
            if (sentences.Count >= MaxSentences - reserveForJob)
            {
                break;
            }

            var useBijVragen = !surprise
                               && likeLabel is not null
                               && i == answerOffset
                               && answerOffset < answers.Count;
            var prefix = useBijVragen ? "Bij de vragen zei je dat" : "Je gaf ook aan dat";
            sentences.Add($"{prefix} {answers[i].Subordinate}.");
        }

        if (!string.IsNullOrWhiteSpace(subjectLabel)
            && sentences.Count < MaxSentences - reserveForJob)
        {
            sentences.Add($"Op school ben je goed in {subjectLabel}, en dat helpt je hier.");
        }

        foreach (var jobLine in jobSentences)
        {
            if (sentences.Count >= MaxSentences)
            {
                break;
            }

            sentences.Add(jobLine);
        }

        return sentences
            .Select(EnsurePeriod)
            .Where(s => s.Length > 10)
            .Take(MaxSentences)
            .ToList();
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

    private static IEnumerable<AnswerPhrase> AnswerPhrases(char jobLetter, PupilProgress? progress)
    {
        var answers = ParseAnswers(progress?.AnswersJson);
        if (answers.Count == 0)
        {
            yield break;
        }

        var items = G78Bank.AsCareerItems()
            .Where(q =>
            {
                if (!answers.TryGetValue(q.Id, out var value) || value < 4)
                {
                    return false;
                }

                return CategoryToLetter.TryGetValue(q.Category, out var letter) && letter == jobLetter;
            })
            .OrderByDescending(q => answers[q.Id])
            .ThenBy(q => q.Id);

        foreach (var item in items)
        {
            if (!PupilG78HighAnswerParaphrases.TryGetSubordinateClause(item.Id, out var sub)
                || !PupilG78HighAnswerParaphrases.TryGetMainClauseAfterWant(item.Id, out var main))
            {
                continue;
            }

            yield return new AnswerPhrase(item.Id, sub, main);
        }
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

        usedSemantic.Add($"vak-{best.Entry.SubjectKey}");
        return PupilVakSubjectLabels.Nl(best.Entry.SubjectKey).ToLowerInvariant();
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

    private static IReadOnlyList<string> JobDescriptionNl(string jobKey)
    {
        var blurb = PupilPassportLocalizedCopy.JobBlurb(jobKey, PupilQuestionSet.Groep78, "nl");
        if (IsPlaceholderJobBlurb(blurb))
        {
            return [];
        }

        var title = PupilPassportLocalizedCopy.JobTitle(jobKey, "nl").Trim().ToLowerInvariant();
        var lines = new List<string>(2);
        foreach (var part in blurb.Split('.', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).Take(2))
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

    private static List<string> BuildEnglishSentences(string jobKey, PupilResult result, PupilProgress? progress)
        => ComposeDutch(jobKey, result, progress);

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

    private static string EnsurePeriod(string sentence)
        => sentence.EndsWith('.') ? sentence : sentence + ".";

    private static Dictionary<int, int> ParseAnswers(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return [];
        }

        try
        {
            var byString = JsonSerializer.Deserialize<Dictionary<string, int>>(json, JsonOpts) ?? [];
            var map = new Dictionary<int, int>();
            foreach (var item in G78Bank.AllItems)
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

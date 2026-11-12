using System.Text.Json;
using System.Text.RegularExpressions;
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
        var clauses = new List<(double Weight, string Clause)>();

        var likeClause = LikeClause(jobKey, likes);
        if (likeClause is not null)
        {
            clauses.Add((factors.ChipFit * PupilCareerMatchWeights.Overige, likeClause.Value.Clause));
            if (LikeToSubjectKey.TryGetValue(likeClause.Value.LikeKey, out var subj))
            {
                usedSemantic.Add($"vak-{subj}");
            }
        }

        var answerCount = 0;
        foreach (var (weight, clause, key) in AnswerClauses(factors.PrimaryRiasecLetter, progress))
        {
            if (!usedSemantic.Add(key))
            {
                continue;
            }

            if (answerCount >= 2)
            {
                break;
            }

            clauses.Add((weight, clause));
            answerCount++;
        }

        var subjectClause = SubjectClause(jobKey, vakScores: vak, likes, usedSemantic);
        if (subjectClause is not null)
        {
            clauses.Add(subjectClause.Value);
        }

        var answerAndSubject = clauses
            .Where(c => !c.Clause.Contains("leuk vindt", StringComparison.Ordinal))
            .OrderByDescending(c => c.Weight)
            .Select(c => c.Clause)
            .ToList();
        var likeOnly = likeClause?.Clause;
        var ordered = new List<string>();
        if (!string.IsNullOrWhiteSpace(likeOnly))
        {
            ordered.Add(likeOnly);
        }

        ordered.AddRange(answerAndSubject);

        var jobClose = JobClosingNl(jobKey);
        return StitchSentences(ordered, surprise, jobClose);
    }

    private static List<string> StitchSentences(IReadOnlyList<string> clauses, bool surprise, string? jobClose)
    {
        if (clauses.Count == 0 && jobClose is null)
        {
            return ["Je antwoorden passen bij dit beroep."];
        }

        var sentences = new List<string>(MaxSentences);
        var idx = 0;

        if (surprise && clauses.Count > 0)
        {
            sentences.Add($"Hier had je misschien zelf niet aan gedacht. Toch past het, want {clauses[0]}.");
            idx = 1;
        }
        else if (surprise)
        {
            sentences.Add("Hier had je misschien zelf niet aan gedacht. Toch laten je antwoorden zien dat dit beroep bij je kan passen.");
        }

        while (idx < clauses.Count && sentences.Count < (jobClose is null ? MaxSentences : MaxSentences - 1))
        {
            sentences.Add(ClauseToSentence(clauses[idx], sentences.Count, surprise));
            idx++;
        }

        if (jobClose is not null && sentences.Count < MaxSentences)
        {
            sentences.Add(jobClose);
        }

        return sentences
            .Select(EnsurePeriod)
            .Where(s => s.Length > 10)
            .Take(MaxSentences)
            .ToList();
    }

    private static string ClauseToSentence(string clause, int sentenceIndex, bool surprise)
    {
        if (clause.Contains("leuk vindt", StringComparison.Ordinal) && !clause.Contains("graag", StringComparison.Ordinal))
        {
            return $"Je vindt {clause.Replace("je ", "", StringComparison.Ordinal).Replace(" leuk vindt", "", StringComparison.Ordinal)} leuk, en dat past bij dit beroep.";
        }

        if (clause.Contains("op school", StringComparison.Ordinal))
        {
            return $"Op school ben je goed in {clause.Replace("je op school goed bent in ", "", StringComparison.Ordinal)}, en dat helpt je hier.";
        }

        return sentenceIndex switch
        {
            0 when !surprise => $"Dit past bij jou, want {clause}.",
            1 => $"In de vragen koos je vaak voor iets waarbij {clause}.",
            _ => $"Daarbij {clause}.",
        };
    }

    private static (string LikeKey, string Clause)? LikeClause(string jobKey, IReadOnlySet<string> likes)
    {
        string? bestLike = null;
        foreach (var like in likes)
        {
            if (!PupilRiasecJobIdeas.LikeJobsForScoring.TryGetValue(like, out var jobs))
            {
                continue;
            }

            if (jobs.Any(j => string.Equals(j, jobKey, StringComparison.OrdinalIgnoreCase)))
            {
                bestLike = like;
                break;
            }
        }

        if (bestLike is null)
        {
            return null;
        }

        var label = PupilPortalChipLabels.Dutch(bestLike);
        if (string.IsNullOrWhiteSpace(label))
        {
            return null;
        }

        return (bestLike, $"je {label.ToLowerInvariant()} leuk vindt");
    }

    private static IEnumerable<(double Weight, string Clause, string SemanticKey)> AnswerClauses(
        char jobLetter,
        PupilProgress? progress)
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
            if (!PupilG78HighAnswerParaphrases.TryGet(item.Id, out var paraphrase))
            {
                continue;
            }

            var semantic = PupilG78HighAnswerParaphrases.SemanticKey(item.Id);
            var weight = answers[item.Id] / 5.0;
            yield return (weight, paraphrase, semantic);
        }
    }

    private static (double Weight, string Clause)? SubjectClause(
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

        var label = PupilVakSubjectLabels.Nl(best.Entry.SubjectKey).ToLowerInvariant();
        usedSemantic.Add($"vak-{best.Entry.SubjectKey}");
        return (best.Score * PupilCareerMatchWeights.Vakken,
            $"je op school goed bent in {label}");
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

    private static string? JobClosingNl(string jobKey)
    {
        var blurb = PupilPassportLocalizedCopy.JobBlurb(jobKey, PupilQuestionSet.Groep78, "nl");
        if (IsPlaceholderJobBlurb(blurb))
        {
            return null;
        }

        var first = blurb.Split('.', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .FirstOrDefault();
        if (string.IsNullOrWhiteSpace(first))
        {
            return null;
        }

        var title = PupilPassportLocalizedCopy.JobTitle(jobKey, "nl").Trim().ToLowerInvariant();
        var match = Regex.Match(first, @"^Je\s+(\S+)\s+(.*)$", RegexOptions.CultureInvariant);
        if (!match.Success)
        {
            return null;
        }

        var verb = match.Groups[1].Value;
        var tail = match.Groups[2].Value.TrimEnd('.');
        var stem = verb.Length > 2 && verb.EndsWith('t') ? verb[..^1] : verb;
        return $"Als {title} {stem} je {tail.ToLowerInvariant()}.";
    }

    private static List<string> BuildEnglishSentences(string jobKey, PupilResult result, PupilProgress? progress)
    {
        var dutch = ComposeDutch(jobKey, result, progress);
        return dutch;
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

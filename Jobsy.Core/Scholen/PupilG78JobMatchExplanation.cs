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
        var english = PupilVerhaalLocalized.IsEnglish(language);
        var vak = PupilVakScores.Parse(progress?.VakScoresJson);
        var factors = PupilG78CareerScoring.ScoreFactors(jobKey, result, progress, vak);
        var likes = ParseChips(progress?.LikesJson);
        var sentences = new List<string>(MaxSentences);

        if (!english && !JobInLikesOrDream(likes, progress, jobKey))
        {
            sentences.Add("Misschien had je hier zelf niet aan gedacht, maar je antwoorden laten wel zien dat het past.");
        }
        else if (english && !JobInLikesOrDream(likes, progress, jobKey))
        {
            sentences.Add("You may not have thought of this yourself, but your answers show it could fit you.");
        }

        foreach (var line in HighAnswerParaphrases(factors.PrimaryRiasecLetter, progress, english))
        {
            sentences.Add(line);
            if (sentences.Count >= MaxSentences - 1)
            {
                break;
            }
        }

        foreach (var line in TopDriverSentences(jobKey, factors, vak, likes, english))
        {
            if (sentences.Contains(line, StringComparer.Ordinal))
            {
                continue;
            }

            sentences.Add(line);
            if (sentences.Count >= MaxSentences - 1)
            {
                break;
            }
        }

        var jobLine = JobActivitySentence(jobKey, english);
        if (!string.IsNullOrWhiteSpace(jobLine))
        {
            sentences.Add(jobLine);
        }

        return sentences
            .Select(EnsurePeriod)
            .Where(s => s.Length > 0)
            .Distinct(StringComparer.Ordinal)
            .Take(MaxSentences)
            .ToList();
    }

    private static IEnumerable<string> TopDriverSentences(
        string jobKey,
        PupilG78JobScoreFactors factors,
        IReadOnlyList<PupilVakScoreEntry> vakScores,
        IReadOnlySet<string> likes,
        bool english)
    {
        var riasecW = factors.RiasecFit * PupilCareerMatchWeights.Riasec;
        var subjectW = factors.SubjectFit * PupilCareerMatchWeights.Vakken;
        var chipW = factors.ChipFit * PupilCareerMatchWeights.Overige;

        var drivers = new List<(double Weight, string Sentence)>
        {
            (chipW, LikeDriverSentence(jobKey, likes, english) ?? ""),
            (subjectW, SubjectDriverSentence(jobKey, factors.PrimaryRiasecLetter, vakScores, english) ?? ""),
            (riasecW, RiasecDriverSentence(factors.PrimaryRiasecLetter, english)),
        };

        foreach (var (_, sentence) in drivers
                     .Where(d => !string.IsNullOrWhiteSpace(d.Sentence))
                     .OrderByDescending(d => d.Weight))
        {
            yield return sentence!;
        }
    }

    private static string? LikeDriverSentence(string jobKey, IReadOnlySet<string> likes, bool english)
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

            var label = english
                ? PupilPortalChipLabels.English(like)
                : PupilPortalChipLabels.Dutch(like);
            if (string.IsNullOrWhiteSpace(label))
            {
                continue;
            }

            return english
                ? $"You said you enjoy {label.ToLowerInvariant()}, and that fits this job."
                : $"Je gaf aan dat je {label.ToLowerInvariant()} leuk vindt, en dat sluit aan bij dit beroep.";
        }

        return null;
    }

    private static string? SubjectDriverSentence(
        string jobKey,
        char jobLetter,
        IReadOnlyList<PupilVakScoreEntry> vakScores,
        bool english)
    {
        var ranked = vakScores
            .Select(v =>
            {
                var map = PupilSubjectMap.TryGet(v.SubjectKey);
                var letterBoost = map?.RiasecWeights.GetValueOrDefault(jobLetter) ?? 0;
                var row = PupilSubjectIscedCatalog.TryGetG78(v.SubjectKey);
                var linked = row?.LinkedOccupationJobKeys?.Any(j =>
                                 string.Equals(j, jobKey, StringComparison.OrdinalIgnoreCase)) == true;
                var score = PupilVakScores.SubjectWeight(v);
                var weight = score * (0.5 + letterBoost) + (linked ? 0.35 : 0);
                return (Entry: v, Weight: weight, Linked: linked);
            })
            .Where(x => x.Weight > 0.35)
            .OrderByDescending(x => x.Weight)
            .Take(2)
            .ToList();

        if (ranked.Count == 0)
        {
            return null;
        }

        var names = ranked
            .Select(r => PupilVakSubjectLabels.Nl(r.Entry.SubjectKey))
            .Where(n => !string.IsNullOrWhiteSpace(n))
            .ToList();
        if (names.Count == 0)
        {
            return null;
        }

        var joined = JoinList(names, english);
        return english
            ? $"At school you like {joined}, and that helps in this job."
            : $"Op school vind je {joined} leuk, en dat komt terug in dit beroep.";
    }

    private static string RiasecDriverSentence(char letter, bool english)
    {
        var key = $"LeerlingPassport.G78.Riasec.{letter}.Desc";
        var text = PupilPassportLocalizedCopy.Get(key, english ? "en" : "nl").Trim().TrimEnd('.');
        if (string.IsNullOrWhiteSpace(text) || string.Equals(text, key, StringComparison.Ordinal))
        {
            return english
                ? "Your career test answers point in this direction."
                : "Je antwoorden op de beroepentest wijzen deze kant op.";
        }

        return english
            ? $"Your test shows: {text.ToLowerInvariant()}."
            : $"Uit je test blijkt: {text.ToLowerInvariant()}.";
    }

    private static IEnumerable<string> HighAnswerParaphrases(
        char jobLetter,
        PupilProgress? progress,
        bool english)
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
            .ThenBy(q => q.Id)
            .Take(2);

        foreach (var item in items)
        {
            if (!PupilG78HighAnswerParaphrases.TryGet(item.Id, out var paraphrase))
            {
                continue;
            }

            yield return english
                ? $"You said that {paraphrase}."
                : $"Je gaf aan dat {paraphrase}.";
        }
    }

    private static string JobActivitySentence(string jobKey, bool english)
    {
        var blurb = PupilPassportLocalizedCopy.JobBlurb(jobKey, PupilQuestionSet.Groep78, english ? "en" : "nl")
            .Trim();
        if (string.IsNullOrWhiteSpace(blurb))
        {
            return "";
        }

        var first = blurb.Split('.', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .FirstOrDefault();
        if (string.IsNullOrWhiteSpace(first))
        {
            return "";
        }

        return english
            ? $"In this job you often {first.Trim().ToLowerInvariant()}."
            : $"In dit beroep doe je vaak dingen zoals: {first.Trim().ToLowerInvariant()}.";
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

    private static string JoinList(IReadOnlyList<string> items, bool english) => items.Count switch
    {
        0 => "",
        1 => items[0],
        2 => english ? $"{items[0]} and {items[1]}" : $"{items[0]} en {items[1]}",
        _ => english
            ? string.Join(", ", items.Take(items.Count - 1)) + " and " + items[^1]
            : string.Join(", ", items.Take(items.Count - 1)) + " en " + items[^1]
    };

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

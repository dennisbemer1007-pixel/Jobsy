using System.Text.Json;
using Jobsy.Core.Entities.Scholen;
using Jobsy.Core.Enums;
using Jobsy.Core.Rules;

namespace Jobsy.Core.Scholen;

public sealed record PupilG78BeroepssoortRank(string Key, double Score);

public sealed record PupilG78SchoolDirectionRank(string Key, double Score);

/// <summary>Deterministic G78 job and beroepssoort ranking (no AI).</summary>
public static class PupilG78CareerScoring
{
    private static readonly JsonSerializerOptions JsonOpts = new() { PropertyNameCaseInsensitive = true };

    public static IReadOnlyList<string> TopJobKeys(
        PupilResult result,
        PupilProgress? progress,
        IReadOnlyList<PupilVakScoreEntry> vakScores,
        int take = 5)
    {
        var riasec = Deserialize<RiasecScores>(result.RiasecScoresJson);
        var likes = ParseChips(progress?.LikesJson);
        var candidates = CandidateJobKeys(likes);
        var ranked = candidates
            .Select(key => (Key: key, Score: ScoreJob(key, riasec, likes, vakScores)))
            .OrderByDescending(x => x.Score)
            .ThenBy(x => x.Key, StringComparer.OrdinalIgnoreCase)
            .Take(take)
            .Select(x => x.Key)
            .ToList();

        if (ranked.Count >= take)
        {
            return ranked;
        }

        foreach (var fallback in DefaultJobKeys(result))
        {
            if (ranked.Count >= take)
            {
                break;
            }

            if (!ranked.Contains(fallback, StringComparer.OrdinalIgnoreCase))
            {
                ranked.Add(fallback);
            }
        }

        return ranked;
    }

    public static IReadOnlyList<PupilG78BeroepssoortRank> TopBeroepssoorten(
        PupilResult result,
        IReadOnlyList<PupilVakScoreEntry> vakScores,
        int take = 10)
    {
        var riasec = Deserialize<RiasecScores>(result.RiasecScoresJson);
        var scores = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);
        foreach (var entry in PupilG78BeroepssoortCatalog.All)
        {
            scores[entry.Key] = 0;
        }

        foreach (var vak in vakScores)
        {
            var weight = PupilVakScores.SubjectWeight(vak);
            if (weight <= 0)
            {
                continue;
            }

            var map = PupilSubjectMap.TryGet(vak.SubjectKey);
            if (map is null)
            {
                continue;
            }

            foreach (var bKey in map.BeroepssoortKeys)
            {
                scores[bKey] = scores.GetValueOrDefault(bKey) + weight * RiasecAffinity(bKey, riasec);
            }
        }

        foreach (var entry in PupilG78BeroepssoortCatalog.All)
        {
            scores[entry.Key] += 0.15 * RiasecAffinity(entry.Key, riasec);
        }

        return scores
            .OrderByDescending(x => x.Value)
            .ThenBy(x => x.Key, StringComparer.OrdinalIgnoreCase)
            .Take(take)
            .Select(x => new PupilG78BeroepssoortRank(x.Key, x.Value))
            .ToList();
    }

    public static IReadOnlyList<PupilG78SchoolDirectionRank> TopSchoolDirections(
        PupilResult result,
        IReadOnlyList<PupilVakScoreEntry> vakScores,
        int take = 3)
    {
        var riasec = Deserialize<RiasecScores>(result.RiasecScoresJson);
        var tech = SubjectClusterScore(vakScores, "natuur-techniek", "rekenen", "tekenen");
        var care = SubjectClusterScore(vakScores, "gym", "taal", "geschiedenis");
        var language = SubjectClusterScore(vakScores, "lezen", "taal", "engels", "schrijven");
        var exact = SubjectClusterScore(vakScores, "rekenen", "natuur-techniek", "aardrijkskunde");

        var map = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase)
        {
            ["vmbo-basis"] = 0.4 * care + 0.2,
            ["vmbo-kader"] = 0.35 * care + 0.25 * tech,
            ["vmbo-techniek"] = 0.55 * tech + 0.2 * (riasec?.Get(CareerTestCatalog.Realistic) ?? 0) / 100.0,
            ["vmbo-zorg"] = 0.55 * care + 0.2 * (riasec?.Get(CareerTestCatalog.Social) ?? 0) / 100.0,
            ["havo"] = 0.45 * language + 0.35 * exact,
            ["vwo"] = 0.5 * exact + 0.3 * language,
            ["praktijk-mbo"] = 0.5 * tech + 0.2 * care,
            ["gemengd-profiel"] = CountStrongSubjects(vakScores) >= 3 ? 0.85 : 0.35,
        };

        return map
            .OrderByDescending(x => x.Value)
            .ThenBy(x => x.Key, StringComparer.OrdinalIgnoreCase)
            .Take(take)
            .Select(x => new PupilG78SchoolDirectionRank(x.Key, x.Value))
            .ToList();
    }

    private static double ScoreJob(
        string jobKey,
        RiasecScores? riasec,
        IReadOnlySet<string> likes,
        IReadOnlyList<PupilVakScoreEntry> vakScores)
    {
        var riasecFit = JobRiasecFit(jobKey, riasec);
        var subjectFit = JobSubjectFit(jobKey, vakScores);
        var chipFit = JobChipFit(jobKey, likes);
        return 0.5 * riasecFit + 0.3 * subjectFit + 0.2 * chipFit;
    }

    private static double JobRiasecFit(string jobKey, RiasecScores? riasec)
    {
        if (riasec is null)
        {
            return 0.35;
        }

        var letters = JobPrimaryLetters(jobKey);
        if (letters.Count == 0)
        {
            return 0.35;
        }

        var sum = 0.0;
        foreach (var letter in letters)
        {
            var code = letter switch
            {
                'R' => CareerTestCatalog.Realistic,
                'I' => CareerTestCatalog.Investigative,
                'A' => CareerTestCatalog.Artistic,
                'S' => CareerTestCatalog.Social,
                'E' => CareerTestCatalog.Enterprising,
                'C' => CareerTestCatalog.Conventional,
                _ => CareerTestCatalog.Social
            };
            sum += riasec.Get(code) / 100.0;
        }

        return Math.Clamp(sum / letters.Count, 0, 1);
    }

    private static double JobSubjectFit(string jobKey, IReadOnlyList<PupilVakScoreEntry> vakScores)
    {
        if (vakScores.Count == 0)
        {
            return 0;
        }

        var sum = 0.0;
        var weightSum = 0.0;
        foreach (var vak in vakScores)
        {
            var w = PupilVakScores.SubjectWeight(vak);
            if (w <= 0)
            {
                continue;
            }

            var map = PupilSubjectMap.TryGet(vak.SubjectKey);
            if (map is null)
            {
                continue;
            }

            weightSum += w;
            if (JobMatchesBeroepssoort(jobKey, map.BeroepssoortKeys))
            {
                sum += w;
            }
        }

        if (weightSum <= 0)
        {
            return 0;
        }

        return Math.Clamp(sum / weightSum, 0, 1);
    }

    private static bool JobMatchesBeroepssoort(string jobKey, IReadOnlyList<string> beroepen)
    {
        foreach (var b in beroepen)
        {
            if (BeroepJobHints(b).Any(h =>
                    jobKey.Contains(h, StringComparison.OrdinalIgnoreCase)
                    || string.Equals(jobKey, h, StringComparison.OrdinalIgnoreCase)))
            {
                return true;
            }
        }

        return false;
    }

    private static IEnumerable<string> BeroepJobHints(string beroepKey) => beroepKey switch
    {
        "techniek-maken" => ["timmerman", "elektricien", "automonteur", "ingenieur", "bouwkundige", "schilder"],
        "exact-wetenschap" => ["bioloog", "wetenschapper", "doktersassistent", "ingenieur"],
        "creatief-design" => ["grafisch-ontwerper", "fotograaf", "muzikant", "acteur", "contentmaker", "kok"],
        "taal-verhalen" => ["journalist", "leraar", "advocaat"],
        "zorg-mensen" => ["leraar", "verpleegkundige", "pedagogisch", "dierenverzorger", "fysiotherapeut"],
        "natuur-buiten" => ["hovenier", "bioloog", "brandweer"],
        "sport-bewegen" => ["personal-trainer", "profsporter", "fysiotherapeut"],
        "handel-organiseren" => ["verkoper", "logistiek", "accountant", "kapper"],
        "media-digital" => ["programmeur", "contentmaker", "game-developer", "ict-medewerker"],
        "dieren-leven" => ["dierenarts", "dierenverzorger", "bioloog"],
        "veiligheid-hulp" => ["politie", "brandweer", "verpleegkundige"],
        "ondernemen-leiden" => ["ondernemer", "verkoper", "evenementenorganisator"],
        _ => []
    };

    private static double JobChipFit(string jobKey, IReadOnlySet<string> likes)
    {
        if (likes.Count == 0)
        {
            return 0.25;
        }

        foreach (var like in likes)
        {
            if (!PupilRiasecJobIdeas.LikeJobsForScoring.TryGetValue(like, out var jobs))
            {
                continue;
            }

            if (jobs.Any(j => string.Equals(j, jobKey, StringComparison.OrdinalIgnoreCase)))
            {
                return 1;
            }
        }

        return 0.15;
    }

    private static double RiasecAffinity(string beroepKey, RiasecScores? riasec)
    {
        if (riasec is null)
        {
            return 0.3;
        }

        var letters = beroepKey switch
        {
            "techniek-maken" or "exact-wetenschap" => "RI",
            "creatief-design" or "taal-verhalen" or "media-digital" => "AS",
            "zorg-mensen" or "sport-bewegen" or "veiligheid-hulp" => "S",
            "natuur-buiten" or "dieren-leven" => "IR",
            "handel-organiseren" or "ondernemen-leiden" => "EC",
            _ => "S"
        };

        var sum = 0.0;
        foreach (var c in letters)
        {
            var code = c switch
            {
                'R' => CareerTestCatalog.Realistic,
                'I' => CareerTestCatalog.Investigative,
                'A' => CareerTestCatalog.Artistic,
                'S' => CareerTestCatalog.Social,
                'E' => CareerTestCatalog.Enterprising,
                'C' => CareerTestCatalog.Conventional,
                _ => CareerTestCatalog.Social
            };
            sum += riasec.Get(code) / 100.0;
        }

        return Math.Clamp(sum / letters.Length, 0, 1);
    }

    private static double SubjectClusterScore(IReadOnlyList<PupilVakScoreEntry> vakScores, params string[] keys)
    {
        var sum = 0.0;
        var n = 0;
        foreach (var key in keys)
        {
            var entry = vakScores.FirstOrDefault(v =>
                string.Equals(v.SubjectKey, key, StringComparison.OrdinalIgnoreCase));
            if (entry is null)
            {
                continue;
            }

            n++;
            sum += PupilVakScores.SubjectWeight(entry);
        }

        return n == 0 ? 0 : sum / n;
    }

    private static int CountStrongSubjects(IReadOnlyList<PupilVakScoreEntry> vakScores)
        => vakScores.Count(PupilVakScores.IsStrong);

    private static HashSet<string> CandidateJobKeys(IReadOnlySet<string> likes)
    {
        var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var list in PupilRiasecJobIdeas.Groep78LetterPoolForScoring.Values)
        {
            foreach (var key in list)
            {
                set.Add(key);
            }
        }

        foreach (var like in likes)
        {
            if (PupilRiasecJobIdeas.LikeJobsForScoring.TryGetValue(like, out var jobs))
            {
                foreach (var key in jobs)
                {
                    set.Add(key);
                }
            }
        }

        foreach (var key in DreamJobCatalog.All.Select(j => j.Key))
        {
            set.Add(key);
        }

        return set;
    }

    private static IReadOnlyList<string> DefaultJobKeys(PupilResult result)
    {
        var holland = (result.HollandCode ?? "S").Trim().ToUpperInvariant();
        var letters = holland.Where(c => "RIASEC".Contains(c)).Take(2).ToList();
        if (letters.Count == 0)
        {
            letters = ['S'];
        }

        return PupilRiasecJobIdeas.ForLetters(letters, PupilQuestionSet.Groep78, null);
    }

    private static IReadOnlyList<char> JobPrimaryLetters(string jobKey)
    {
        foreach (var (letter, jobs) in PupilRiasecJobIdeas.Groep78LetterPoolForScoring)
        {
            if (jobs.Any(j => string.Equals(j, jobKey, StringComparison.OrdinalIgnoreCase)))
            {
                return [letter];
            }
        }

        return ['S'];
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

    private static T? Deserialize<T>(string? json) where T : class
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return null;
        }

        try
        {
            return JsonSerializer.Deserialize<T>(json, JsonOpts);
        }
        catch (JsonException)
        {
            return null;
        }
    }
}

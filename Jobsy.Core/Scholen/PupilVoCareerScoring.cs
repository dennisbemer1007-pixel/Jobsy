using System.Text.Json;
using Jobsy.Core.Entities.Scholen;
using Jobsy.Core.Rules;
using Jobsy.Core.Scholen.Matching;
using Jobsy.Core.Scholen.PupilPassport;

namespace Jobsy.Core.Scholen;

/// <summary>VO beroepensuggesties (volgorde, geen percentage in UI).</summary>
public static class PupilVoCareerScoring
{
    private static readonly JsonSerializerOptions JsonOpts = new() { PropertyNameCaseInsensitive = true };

    public sealed record VoJobRank(string JobKey, double Score);

    public static IReadOnlyList<string> TopJobKeys(
        PupilResult result,
        PupilProgress? progress,
        int take = 3)
        => TopJobs(result, progress, take).Select(j => j.JobKey).ToList();

    public static IReadOnlyList<VoJobRank> TopJobs(
        PupilResult result,
        PupilProgress? progress,
        int take = 3)
    {
        var riasec = DeserializeRiasec(result.RiasecScoresJson);
        var likes = ParseChips(progress?.LikesJson);
        var favKeys = ParseTagList(progress?.VoFavoriteSubjectsJson);
        var holland = result.HollandCode ?? "";
        var candidates = CandidateJobKeys(likes);
        var ranked = candidates
            .Select(key => new VoJobRank(key, ScoreJob(key, riasec, likes, favKeys, holland)))
            .OrderByDescending(x => x.Score)
            .ThenBy(x => x.JobKey, StringComparer.OrdinalIgnoreCase)
            .ToList();

        return PupilCareerMatchSelection.SelectTop(
            ranked,
            j => j.Score,
            j => PupilG78CareerScoring.PrimaryBeroepssoortKey(j.JobKey) ?? "",
            PupilCareerMatchWeights.DefaultMinMatch,
            take,
            PupilCareerMatchWeights.MaxPerGroup);
    }

    private static double ScoreJob(
        string jobKey,
        RiasecScores? riasec,
        IReadOnlySet<string> likes,
        IReadOnlyList<string> favoriteSubjects,
        string hollandCode)
    {
        var riasecFit = JobRiasecFit(jobKey, riasec);
        var subjectFit = PupilSubjectOccupationFit.FitForVoFavorites(jobKey, favoriteSubjects);
        var chipFit = ChipFit(jobKey, likes);
        var sectorFit = SectorFit(jobKey, hollandCode, likes);
        var otherFit = 0.5 * chipFit + 0.5 * sectorFit;
        return PupilCareerMatchWeights.Riasec * riasecFit
               + PupilCareerMatchWeights.Vakken * subjectFit
               + PupilCareerMatchWeights.Overige * otherFit;
    }

    private static double JobRiasecFit(string jobKey, RiasecScores? riasec)
    {
        if (riasec is null)
        {
            return 0.35;
        }

        var letters = JobPrimaryLetters(jobKey);
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

    private static IReadOnlyList<char> JobPrimaryLetters(string jobKey)
    {
        foreach (var (letter, jobs) in PupilRiasecJobIdeas.ByLetter)
        {
            if (jobs.Any(j => string.Equals(j, jobKey, StringComparison.OrdinalIgnoreCase)))
            {
                return [letter];
            }
        }

        return ['S'];
    }

    private static double ChipFit(string jobKey, IReadOnlySet<string> likes)
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

    private static double SectorFit(string jobKey, string hollandCode, IReadOnlySet<string> likes)
    {
        var sectors = PupilVoSectorFits.Build(hollandCode, likes.ToList());
        if (sectors.Count == 0)
        {
            return 0.35;
        }

        var title = PupilPassportLocalizedCopy.JobTitle(jobKey, "nl");
        foreach (var sector in sectors.Take(2))
        {
            if (title.Contains(sector.Title, StringComparison.OrdinalIgnoreCase))
            {
                return 0.85;
            }
        }

        return 0.45;
    }

    private static HashSet<string> CandidateJobKeys(IReadOnlySet<string> likes)
    {
        var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var list in PupilRiasecJobIdeas.ByLetter.Values)
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

    private static List<string> ParseTagList(string? json)
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

    private static RiasecScores? DeserializeRiasec(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return null;
        }

        try
        {
            return JsonSerializer.Deserialize<RiasecScores>(json, JsonOpts);
        }
        catch (JsonException)
        {
            return null;
        }
    }
}

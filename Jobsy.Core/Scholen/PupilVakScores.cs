using System.Text.Json;

namespace Jobsy.Core.Scholen;

public sealed record PupilVakScoreEntry(string SubjectKey, int Leuk, int Goed);

/// <summary>G78 subject ratings (1–5) stored on <see cref="Entities.Scholen.PupilProgress.VakScoresJson"/>.</summary>
public static class PupilVakScores
{
    public const int PageSize = 6;

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    public static readonly IReadOnlyList<string> AllSubjectKeys =
    [
        "rekenen",
        "lezen",
        "spelling",
        "taal",
        "engels",
        "schrijven",
        "aardrijkskunde",
        "geschiedenis",
        "natuur-techniek",
        "tekenen",
        "muziek",
        "gym"
    ];

    public static int PageFor(string subjectKey)
    {
        var idx = IndexOf(subjectKey);
        return idx < PageSize ? 1 : 2;
    }

    public static IReadOnlyList<string> KeysForPage(int page)
        => page switch
        {
            1 => AllSubjectKeys.Take(PageSize).ToList(),
            2 => AllSubjectKeys.Skip(PageSize).ToList(),
            _ => []
        };

    public static IReadOnlyList<PupilVakScoreEntry> Parse(string? json)
    {
        if (string.IsNullOrWhiteSpace(json) || json.Trim() is "{}" or "[]")
        {
            return [];
        }

        try
        {
            var map = JsonSerializer.Deserialize<Dictionary<string, PupilVakScoreDto>>(json, JsonOpts);
            if (map is null || map.Count == 0)
            {
                return [];
            }

            var list = new List<PupilVakScoreEntry>(AllSubjectKeys.Count);
            foreach (var key in AllSubjectKeys)
            {
                if (!map.TryGetValue(key, out var dto))
                {
                    continue;
                }

                if (IsValidScore(dto.Leuk) && IsValidScore(dto.Goed))
                {
                    list.Add(new PupilVakScoreEntry(key, dto.Leuk, dto.Goed));
                }
            }

            return list;
        }
        catch (JsonException)
        {
            return [];
        }
    }

    public static string Serialize(IReadOnlyDictionary<string, (int Leuk, int Goed)> scores)
    {
        var map = new Dictionary<string, PupilVakScoreDto>(StringComparer.OrdinalIgnoreCase);
        foreach (var key in AllSubjectKeys)
        {
            if (scores.TryGetValue(key, out var pair)
                && IsValidScore(pair.Leuk)
                && IsValidScore(pair.Goed))
            {
                map[key] = new PupilVakScoreDto(pair.Leuk, pair.Goed);
            }
        }

        return JsonSerializer.Serialize(map, JsonOpts);
    }

    public static bool IsComplete(string? json)
    {
        var parsed = Parse(json);
        if (parsed.Count != AllSubjectKeys.Count)
        {
            return false;
        }

        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var entry in parsed)
        {
            if (!AllSubjectKeys.Contains(entry.SubjectKey, StringComparer.OrdinalIgnoreCase))
            {
                return false;
            }

            seen.Add(entry.SubjectKey);
        }

        return seen.Count == AllSubjectKeys.Count;
    }

    /// <summary>0.6 × leuk + 0.4 × goed (beide 1–5 → 0–1).</summary>
    public static double SubjectWeight(int leuk, int goed)
    {
        return SubjectScore(leuk, goed);
    }

    public static double SubjectWeight(PupilVakScoreEntry entry)
        => SubjectScore(entry.Leuk, entry.Goed);

    /// <summary>Combined vak rating 0–1 (Dennis optie A).</summary>
    public static double SubjectScore(int leuk, int goed)
    {
        if (!IsValidScore(leuk) || !IsValidScore(goed))
        {
            return 0;
        }

        var leukNorm = (leuk - 1) / 4.0;
        var goedNorm = (goed - 1) / 4.0;
        return Math.Clamp(0.6 * leukNorm + 0.4 * goedNorm, 0, 1);
    }

    /// <summary>True when the vak contributes signal (any valid leuk/goed pair).</summary>
    public static bool HasRating(PupilVakScoreEntry entry)
        => IsValidScore(entry.Leuk) && IsValidScore(entry.Goed);

    public static bool IsStrong(PupilVakScoreEntry entry)
        => entry.Leuk >= 4 && entry.Goed >= 4;

    public static bool IsLeukGrowth(PupilVakScoreEntry entry)
        => entry.Leuk >= 4 && entry.Goed < 4;

    public static bool IsGoedOnly(PupilVakScoreEntry entry)
        => entry.Leuk < 4 && entry.Goed >= 4;

    public static bool ShouldHide(PupilVakScoreEntry entry)
        => entry.Leuk < 4 && entry.Goed < 4;

    private static int IndexOf(string subjectKey)
    {
        for (var i = 0; i < AllSubjectKeys.Count; i++)
        {
            if (string.Equals(AllSubjectKeys[i], subjectKey, StringComparison.OrdinalIgnoreCase))
            {
                return i;
            }
        }

        return -1;
    }

    private static bool IsValidScore(int value) => value is >= 1 and <= 5;

    private sealed record PupilVakScoreDto(int Leuk, int Goed);
}

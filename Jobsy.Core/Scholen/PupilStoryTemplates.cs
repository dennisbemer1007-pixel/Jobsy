using System.Text.Json;
using Jobsy.Core.Entities.Scholen;
using Jobsy.Core.Enums;
using Jobsy.Core.Rules;

namespace Jobsy.Core.Scholen;

/// <summary>
/// Deterministic "Dit ben jij" template key selection (Version 1). Texts live in <see cref="PupilVerhaalCopy"/>.
/// </summary>
public static class PupilStoryTemplates
{
    public const int Version = 1;
    public const int BalancedSpreadMax = 10;

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private static readonly string[] CompetenceOrder = CompetencyTestCatalog.CategoryCodes;
    private static readonly string[] RiasecOrder =
    [
        CareerTestCatalog.Realistic,
        CareerTestCatalog.Investigative,
        CareerTestCatalog.Artistic,
        CareerTestCatalog.Social,
        CareerTestCatalog.Enterprising,
        CareerTestCatalog.Conventional
    ];

    private static readonly Dictionary<string, char> RiasecLetter = new(StringComparer.OrdinalIgnoreCase)
    {
        [CareerTestCatalog.Realistic] = 'R',
        [CareerTestCatalog.Investigative] = 'I',
        [CareerTestCatalog.Artistic] = 'A',
        [CareerTestCatalog.Social] = 'S',
        [CareerTestCatalog.Enterprising] = 'E',
        [CareerTestCatalog.Conventional] = 'C'
    };

    /// <summary>Selects story keys from scored result + like chip keys (excludes "Iets anders" free text).</summary>
    public static PupilStoryKeySet SelectKeys(
        PupilResult result,
        IReadOnlyList<string>? likeChipKeys,
        PupilQuestionSet set = PupilQuestionSet.Vo)
    {
        ArgumentNullException.ThrowIfNull(result);

        var competence = Deserialize<CompetencyScores>(result.CompetenceScoresJson);
        var riasec = Deserialize<RiasecScores>(result.RiasecScoresJson);
        var values = Deserialize<SchwartzValuesScores>(result.ValuesScoresJson);
        var culture = Deserialize<CulturePersonalityScores>(result.CultureScoresJson);

        var (bfCode, balanced) = TopCompetence(competence);
        var bfKey = balanced
            ? "LeerlingStory.Bf.Balanced"
            : $"LeerlingStory.Bf.{bfCode}";

        var top2 = TopRiasecLetters(riasec, result.HollandCode);
        var riasecKey = RiasecSentenceKey(top2);

        var topValue = string.IsNullOrWhiteSpace(result.TopValue)
            ? TopByOrder(SchwartzValuesCatalog.CategoryCodes, c => values?.Get(c) ?? 0)
            : result.TopValue.Trim();
        var schwartzKey = $"LeerlingStory.Val.{topValue}";

        var topCulture = string.IsNullOrWhiteSpace(result.TopCulture)
            ? TopByOrder(CulturePersonalityCatalog.CultureDimensionCodes, c => culture?.Get(c) ?? 0)
            : result.TopCulture.Trim();
        var cultureKey = $"LeerlingStory.Cult.{topCulture}";

        var likes = (likeChipKeys ?? [])
            .Where(k => !string.IsNullOrWhiteSpace(k)
                        && !string.Equals(k, PupilInterestChipCatalog.OtherChipKey, StringComparison.Ordinal))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(2)
            .ToList();

        var jobIdeas = PupilRiasecJobIdeas.ForLetters(top2, set, likes);

        return new PupilStoryKeySet(
            Version: Version,
            BigFiveKey: bfKey,
            RiasecKey: riasecKey,
            SchwartzKey: schwartzKey,
            CultureKey: cultureKey,
            LikeChipKeys: likes,
            TileCompetenceKey: $"LeerlingStory.Tile.Bf.{(balanced ? "Balanced" : bfCode)}",
            TileRiasecKey: TileRiasecKey(top2),
            TileValueKey: $"LeerlingStory.Tile.Val.{topValue}",
            TileCultureKey: $"LeerlingStory.Tile.Cult.{topCulture}",
            JobIdeaKeys: jobIdeas);
    }

    public static string Serialize(PupilStoryKeySet keys)
        => JsonSerializer.Serialize(keys, JsonOpts);

    public static PupilStoryKeySet? TryParse(string? json)
    {
        if (string.IsNullOrWhiteSpace(json) || json.Trim() is "[]" or "{}")
        {
            return null;
        }

        try
        {
            return JsonSerializer.Deserialize<PupilStoryKeySet>(json, JsonOpts);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    public static IReadOnlyList<string> ConversationStarterKeys(PupilResult result)
    {
        var letter = (result.HollandCode ?? "S").Trim().ToUpperInvariant();
        if (letter.Length == 0)
        {
            letter = "S";
        }

        var top = letter[0];
        if ("RIASEC".IndexOf(top) < 0)
        {
            top = 'S';
        }

        var value = string.IsNullOrWhiteSpace(result.TopValue) ? "Connection" : result.TopValue.Trim();
        return
        [
            $"LeerlingStory.Talk.{top}.{value}.1",
            $"LeerlingStory.Talk.{top}.{value}.2",
            $"LeerlingStory.Talk.{top}.{value}.3"
        ];
    }

    public static IReadOnlyList<string> ClassDiscussionPromptKeys(string? topLetter1, string? topLetter2)
    {
        var a = NormalizeLetter(topLetter1);
        var b = NormalizeLetter(topLetter2);
        var pair = string.Equals(a, b, StringComparison.Ordinal)
            ? a
            : CanonicalRiasecPair(a[0], b[0]);
        return
        [
            $"LeerlingStory.Class.{pair}.1",
            $"LeerlingStory.Class.{pair}.2",
            $"LeerlingStory.Class.{pair}.3"
        ];
    }

    private static string NormalizeLetter(string? letter)
    {
        var c = (letter ?? "S").Trim().ToUpperInvariant();
        if (c.Length == 0 || "RIASEC".IndexOf(c[0]) < 0)
        {
            return "S";
        }

        return c[0].ToString();
    }

    private static (string Code, bool Balanced) TopCompetence(CompetencyScores? scores)
    {
        if (scores is null)
        {
            return (CompetencyTestCatalog.Samenwerken, true);
        }

        var ranked = CompetenceOrder
            .Select(c => (Code: c, Score: scores.Get(c)))
            .OrderByDescending(x => x.Score)
            .ThenBy(x => Array.IndexOf(CompetenceOrder, x.Code))
            .ToList();
        var top = ranked[0];
        var spread = top.Score - ranked[^1].Score;
        return (top.Code, spread < BalancedSpreadMax);
    }

    private static IReadOnlyList<char> TopRiasecLetters(RiasecScores? scores, string? hollandCode)
    {
        if (!string.IsNullOrWhiteSpace(hollandCode))
        {
            var letters = hollandCode.Trim().ToUpperInvariant()
                .Where(c => "RIASEC".Contains(c))
                .Distinct()
                .Take(2)
                .ToList();
            if (letters.Count >= 1)
            {
                return letters;
            }
        }

        if (scores is null)
        {
            return ['S'];
        }

        return RiasecOrder
            .Select(c => (Letter: RiasecLetter[c], Score: scores.Get(c)))
            .OrderByDescending(x => x.Score)
            .ThenBy(x => "RIASEC".IndexOf(x.Letter))
            .Select(x => x.Letter)
            .Take(2)
            .ToList();
    }

    /// <summary>Alphabetical pair so CS and SC share one key.</summary>
    public static string CanonicalRiasecPair(char first, char second)
    {
        var a = char.ToUpperInvariant(first);
        var b = char.ToUpperInvariant(second);
        if (a > b)
        {
            (a, b) = (b, a);
        }

        return string.Concat(a, b);
    }

    /// <summary>Sorted pair so CA and AC share <c>LeerlingStory.Tile.Riasec.AC</c>.</summary>
    public static string TileRiasecKey(IReadOnlyList<char> top2)
        => "LeerlingStory.Tile.Riasec." + PairSuffix(top2);

    /// <summary>Repairs a stored unsorted pair such as <c>LeerlingStory.Tile.Riasec.CA</c>.</summary>
    public static string NormalizeTileRiasecKey(string key)
    {
        const string prefix = "LeerlingStory.Tile.Riasec.";
        if (string.IsNullOrWhiteSpace(key) || !key.StartsWith(prefix, StringComparison.Ordinal))
        {
            return key;
        }

        var rest = key[prefix.Length..];
        if (rest.Length == 2)
        {
            return prefix + CanonicalRiasecPair(rest[0], rest[1]);
        }

        return key;
    }

    /// <summary>Same letter order as <see cref="TileRiasecKey"/>.</summary>
    public static string NormalizeRiasecSentenceKey(string key)
    {
        const string prefix = "LeerlingStory.Riasec.";
        if (string.IsNullOrWhiteSpace(key) || !key.StartsWith(prefix, StringComparison.Ordinal))
        {
            return key;
        }

        var rest = key[prefix.Length..];
        if (rest.Length == 2)
        {
            return prefix + CanonicalRiasecPair(rest[0], rest[1]);
        }

        return key;
    }

    private static string RiasecSentenceKey(IReadOnlyList<char> top2)
        => "LeerlingStory.Riasec." + PairSuffix(top2);

    private static string PairSuffix(IReadOnlyList<char> top2)
    {
        if (top2.Count == 0)
        {
            return "S";
        }

        if (top2.Count == 1)
        {
            return top2[0].ToString();
        }

        return CanonicalRiasecPair(top2[0], top2[1]);
    }

    private static string TopByOrder(IReadOnlyList<string> order, Func<string, int> getter)
    {
        string best = order[0];
        var bestVal = int.MinValue;
        foreach (var code in order)
        {
            var v = getter(code);
            if (v > bestVal)
            {
                bestVal = v;
                best = code;
            }
        }

        return best;
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

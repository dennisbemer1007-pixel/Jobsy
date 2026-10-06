using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using Jobsy.Core.Rules;

namespace Jobsy.Core.Careers;

/// <summary>
/// ESCO v1.2.1 occupations with sourced O*NET interest profiles. Loaded once from embedded JSON.
/// A correction changes a profile only when its status is goedgekeurd, unless preview is on outside Production.
/// </summary>
public sealed class OccupationCatalog
{
    public const string PreviewConfigKey = "Occupations:PreviewConceptCorrections";
    public const string ApprovedStatus = "goedgekeurd";
    public const string ConceptStatus = "concept, wacht op akkoord";

    private static readonly JsonSerializerOptions Json = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private static OccupationCatalog? _shared;

    private readonly Dictionary<string, Occupation> _byId;
    private readonly Dictionary<string, List<Occupation>> _byPreferred;
    private readonly Dictionary<string, List<Occupation>> _byAlt;
    private readonly Dictionary<string, List<Occupation>> _byIsco;
    private readonly List<(string Fold, string Isco)> _cbsTitles;
    private readonly HashSet<string> _cbsFolds;
    private readonly HashSet<string> _cbsTitleByIsco;
    private readonly List<SearchRule> _searchRules;
    private readonly List<LevelSwap> _swaps;

    private OccupationCatalog(
        IReadOnlyList<Occupation> all,
        IReadOnlyList<CbsTitle> cbs,
        IReadOnlyList<SearchRule> searchRules,
        IReadOnlyList<LevelSwap> swaps,
        bool previewActive)
    {
        All = all;
        PreviewActive = previewActive;
        _byId = all.ToDictionary(item => item.Id, StringComparer.OrdinalIgnoreCase);
        _byPreferred = Group(all, item => item.Nl);
        _byAlt = new Dictionary<string, List<Occupation>>(StringComparer.Ordinal);
        foreach (var item in all)
        {
            foreach (var alt in item.Alt)
            {
                AddFold(_byAlt, alt, item);
            }
        }

        _byIsco = all.GroupBy(item => item.Isco, StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.ToList(), StringComparer.Ordinal);
        _cbsTitles = cbs.Select(item => (CareerOccupationKeys.Fold(item.Title), item.Isco)).ToList();
        _cbsFolds = _cbsTitles
            .Select(item => item.Fold)
            .Where(fold => fold.Length > 0)
            .ToHashSet(StringComparer.Ordinal);
        _cbsTitleByIsco = _cbsTitles
            .Where(item => item.Fold.Length > 0 && item.Isco.Length > 0)
            .Select(item => item.Isco + "\u001f" + item.Fold)
            .ToHashSet(StringComparer.Ordinal);
        _searchRules = searchRules.ToList();
        _swaps = swaps.ToList();
        Listable = all.Where(item => item.IsListable).ToList();
    }

    public static OccupationCatalog Shared => _shared ??= LoadEmbedded(applyConcept: false);

    public IReadOnlyList<Occupation> All { get; }

    public IReadOnlyList<Occupation> Listable { get; }

    public bool PreviewActive { get; }

    public static void Configure(bool previewRequested, bool isProduction)
        => _shared = LoadEmbedded(ApplyConcept(previewRequested, isProduction));

    /// <summary>Production never applies concept corrections, even when the flag is on.</summary>
    public static bool ApplyConcept(bool previewRequested, bool isProduction)
        => previewRequested && !isProduction;

    public static OccupationCatalog LoadEmbedded(bool applyConcept)
    {
        using var occupations = Open("occupations.nl.json");
        using var onet = Open("onet-oi.json");
        using var corrections = Open("corrections.json");
        using var cbs = Open("cbs-title-index.json");
        return Load(occupations, onet, corrections, cbs, applyConcept);
    }

    public static OccupationCatalog Load(
        Stream occupations,
        Stream onet,
        Stream corrections,
        Stream cbs,
        bool applyConcept)
    {
        var rows = JsonSerializer.Deserialize<List<OccupationDto>>(occupations, Json)
                   ?? throw new InvalidOperationException("Occupation catalogue JSON is empty.");
        var profiles = JsonSerializer.Deserialize<Dictionary<string, OnetDto>>(onet, Json)
                       ?? throw new InvalidOperationException("O*NET OI JSON is empty.");
        var correctionFile = JsonSerializer.Deserialize<CorrectionsDto>(corrections, Json)
                             ?? throw new InvalidOperationException("corrections.json is empty.");
        var titles = JsonSerializer.Deserialize<List<CbsTitle>>(cbs, Json) ?? [];
        var oi = profiles.ToDictionary(
            pair => pair.Key,
            pair => pair.Value.Oi ?? throw new InvalidOperationException($"O*NET {pair.Key} has no OI."),
            StringComparer.Ordinal);

        var search = new List<SearchRule>();
        var swaps = new List<LevelSwap>();
        var byId = new Dictionary<string, Occupation>(StringComparer.OrdinalIgnoreCase);
        foreach (var row in rows)
        {
            var officialOi = row.OfficialOi ?? row.Oi;
            var occupation = new Occupation
            {
                Id = row.Id,
                Uri = row.Uri,
                Nl = row.Nl,
                Alt = row.Alt ?? [],
                Desc = row.Desc ?? "",
                Isco = row.Isco,
                IscoLevel = row.IscoLevel,
                Brc = row.Brc,
                Oi = row.Oi,
                Tier = row.Tier,
                Confidence = row.Confidence,
                Onet = row.Onet ?? [],
                OfficialOi = officialOi,
                OfficialTier = row.OfficialTier ?? row.Tier,
                OfficialConfidence = row.OfficialConfidence ?? row.Confidence,
                OfficialOnet = row.OfficialOnet ?? row.Onet ?? []
            };
            byId[occupation.Id] = occupation;
        }

        foreach (var item in correctionFile.Items ?? [])
        {
            if (!byId.TryGetValue(item.EscoId, out var occupation))
            {
                throw new InvalidOperationException($"Correction {item.EscoId} is not an ESCO occupation.");
            }

            if (item.Kind == "change")
            {
                foreach (var code in item.Onet ?? [])
                {
                    if (!oi.ContainsKey(code))
                    {
                        throw new InvalidOperationException($"O*NET code {code} has no OI profile.");
                    }
                }
            }

            var active = item.Status == ApprovedStatus || (applyConcept && item.Status == ConceptStatus);
            if (!active || item.Kind == "search")
            {
                if (active && item.Kind == "search")
                {
                    foreach (var hint in item.Search ?? [])
                    {
                        search.Add(new SearchRule(
                            hint.Query ?? "",
                            hint.AlsoShow ?? [],
                            hint.ShowFirst ?? []));
                    }
                }

                continue;
            }

            if (item.Kind == "change")
            {
                var codes = item.Onet ?? [];
                occupation.Oi = Mean(codes, oi);
                occupation.Onet = codes;
                occupation.Tier = "lobsy";
                occupation.Confidence = item.Confidence ?? "";
            }
            else if (item.Kind == "confirm")
            {
                occupation.Tier = "lobsy";
                occupation.Confidence = item.Confidence ?? occupation.Confidence;
            }
        }

        foreach (var swap in correctionFile.LevelSubstitutions ?? [])
        {
            if (swap.Status == ApprovedStatus)
            {
                swaps.Add(new LevelSwap(swap.FromEscoId, swap.ToEscoId));
            }
        }

        return new OccupationCatalog(byId.Values.OrderBy(item => item.Uri, StringComparer.Ordinal).ToList(), titles, search, swaps, applyConcept);
    }

    public Occupation? Get(string? escoId)
        => !string.IsNullOrWhiteSpace(escoId) && _byId.TryGetValue(escoId.Trim(), out var occupation)
            ? occupation
            : null;

    /// <summary>Exact Dutch preferred label, or a unique alternative label. Ambiguous titles do not resolve.</summary>
    public Occupation? Resolve(string? title)
    {
        var fold = CareerOccupationKeys.Fold(title ?? "");
        if (fold.Length == 0)
        {
            return null;
        }

        if (_byPreferred.TryGetValue(fold, out var preferred))
        {
            return preferred.Count == 1 ? preferred[0] : null;
        }

        return _byAlt.TryGetValue(fold, out var alts) && alts.Count == 1 ? alts[0] : null;
    }

    /// <summary>
    /// True when the official Dutch title is in the CBS occupation index.
    /// Alternate names are ignored, so a niche job is not marked common via a generic alias.
    /// </summary>
    public bool IsCommonDutchTitle(Occupation occupation)
    {
        var fold = CareerOccupationKeys.Fold(occupation.Nl);
        return fold.Length > 0 && _cbsFolds.Contains(fold);
    }

    /// <summary>
    /// True when this occupation's Dutch name is a CBS title filed under the same ISCO code.
    /// That is the recognisable name of the group, not a niche ESCO variant.
    /// </summary>
    public bool IsCbsTitleForItsIsco(Occupation occupation)
    {
        var fold = CareerOccupationKeys.Fold(occupation.Nl);
        return fold.Length > 0
               && occupation.Isco.Length > 0
               && _cbsTitleByIsco.Contains(occupation.Isco + "\u001f" + fold);
    }

    public static bool IsLeadership(Occupation occupation)
    {
        if (occupation.Isco.Length > 0 && occupation.Isco[0] == '1')
        {
            return true;
        }

        if (occupation.Isco is "3121" or "3122" or "3123" or "3341" or "5151" or "5222")
        {
            return true;
        }

        if (CareerCompassBuilder.IsLeadershipTitle(occupation.Nl))
        {
            return true;
        }

        return occupation.Alt.Any(CareerCompassBuilder.IsLeadershipTitle);
    }

    public IReadOnlyList<OccupationSearchHit> Search(string? query, int max, string? education = null)
    {
        var fold = CareerOccupationKeys.Fold(query ?? "");
        if (fold.Length == 0)
        {
            return [];
        }

        var limit = Math.Clamp(max, 1, 40);
        var tokens = fold.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var scores = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        void Keep(string escoId, int score)
        {
            if (score <= 0 || !_byId.ContainsKey(escoId))
            {
                return;
            }

            if (!scores.TryGetValue(escoId, out var current) || score > current)
            {
                scores[escoId] = score;
            }
        }

        foreach (var occupation in All)
        {
            var preferred = MatchRank(fold, tokens, CareerOccupationKeys.Fold(occupation.Nl));
            if (preferred > 0)
            {
                Keep(occupation.Id, 300 + preferred);
            }

            foreach (var alt in occupation.Alt)
            {
                var altRank = MatchRank(fold, tokens, CareerOccupationKeys.Fold(alt));
                if (altRank > 0)
                {
                    Keep(occupation.Id, 200 + altRank);
                }
            }
        }

        foreach (var (titleFold, isco) in _cbsTitles)
        {
            var rank = MatchRank(fold, tokens, titleFold);
            if (rank == 0 || !_byIsco.TryGetValue(isco, out var group))
            {
                continue;
            }

            foreach (var occupation in group)
            {
                Keep(occupation.Id, 100 + rank);
            }
        }

        for (var i = 0; i < _searchRules.Count; i++)
        {
            var rule = _searchRules[i];
            if (!QueryHits(fold, CareerOccupationKeys.Fold(rule.Query)))
            {
                continue;
            }

            for (var n = 0; n < rule.ShowFirst.Count; n++)
            {
                Keep(rule.ShowFirst[n], 10_000 - n);
            }

            foreach (var id in rule.AlsoShow)
            {
                if (!scores.ContainsKey(id))
                {
                    Keep(id, 50);
                }
            }
        }

        var gate = CareerEducationGate.MaxIscoLevel(education);
        if (gate.SubstituteLowerOffice)
        {
            foreach (var swap in _swaps)
            {
                if (scores.TryGetValue(swap.FromId, out var fromScore))
                {
                    // The lower office job sorts first even when the query only hit the higher title.
                    Keep(swap.ToId, fromScore + 1);
                }
            }
        }

        return scores
            .OrderByDescending(pair => pair.Value)
            .ThenBy(pair => _byId[pair.Key].Nl, StringComparer.OrdinalIgnoreCase)
            .Take(limit)
            .Select(pair =>
            {
                var occupation = _byId[pair.Key];
                var levelNote = CareerEducationGate.NeedsExtraTraining(occupation.IscoLevel, gate)
                                || (gate.SubstituteLowerOffice && _swaps.Any(swap => swap.FromId == occupation.Id));
                return new OccupationSearchHit(
                    occupation.Id,
                    occupation.Nl,
                    Short(occupation.Desc),
                    occupation.NoScore,
                    levelNote,
                    occupation.Isco,
                    occupation.IscoLevel);
            })
            .ToList();
    }

    public IReadOnlyList<LevelSwap> LevelSwaps => _swaps;

    public static string Short(string? description)
    {
        var text = (description ?? "").Trim();
        if (text.Length <= 180)
        {
            return text;
        }

        var cut = text.LastIndexOf(' ', 180);
        if (cut < 80)
        {
            cut = 180;
        }

        return text[..cut].TrimEnd() + "…";
    }

    private static int MatchRank(string query, string[] tokens, string label)
    {
        if (label.Length == 0)
        {
            return 0;
        }

        if (label == query)
        {
            return 40;
        }

        if (label.StartsWith(query, StringComparison.Ordinal))
        {
            return 30;
        }

        if (tokens.Length > 0 && tokens.All(token => CareerOccupationKeys.Hits(label, token) || label.StartsWith(token, StringComparison.Ordinal)))
        {
            return 20;
        }

        return 0;
    }

    private static bool QueryHits(string user, string rule)
    {
        if (user.Length == 0 || rule.Length == 0)
        {
            return false;
        }

        return user == rule
               || rule.StartsWith(user, StringComparison.Ordinal)
               || user.StartsWith(rule, StringComparison.Ordinal);
    }

    private static double[] Mean(IReadOnlyList<string> codes, IReadOnlyDictionary<string, double[]> oi)
    {
        if (codes.Count == 0)
        {
            throw new InvalidOperationException("A change correction needs at least one O*NET code.");
        }

        var rows = new List<double[]>(codes.Count);
        foreach (var code in codes)
        {
            if (!oi.TryGetValue(code, out var profile) || profile.Length != 6)
            {
                throw new InvalidOperationException($"O*NET code {code} has no OI profile.");
            }

            rows.Add(profile);
        }

        var mean = new double[6];
        for (var i = 0; i < 6; i++)
        {
            var sum = 0d;
            foreach (var row in rows)
            {
                sum += row[i];
            }

            mean[i] = Math.Round(sum / rows.Count, 2, MidpointRounding.AwayFromZero);
        }

        return mean;
    }

    private static Dictionary<string, List<Occupation>> Group(IEnumerable<Occupation> all, Func<Occupation, string> label)
    {
        var map = new Dictionary<string, List<Occupation>>(StringComparer.Ordinal);
        foreach (var item in all)
        {
            AddFold(map, label(item), item);
        }

        return map;
    }

    private static void AddFold(Dictionary<string, List<Occupation>> map, string label, Occupation occupation)
    {
        var fold = CareerOccupationKeys.Fold(label);
        if (fold.Length == 0)
        {
            return;
        }

        if (!map.TryGetValue(fold, out var list))
        {
            list = [];
            map[fold] = list;
        }

        if (!list.Contains(occupation))
        {
            list.Add(occupation);
        }
    }

    private static Stream Open(string fileName)
    {
        var assembly = typeof(OccupationCatalog).Assembly;
        var name = assembly.GetManifestResourceNames().FirstOrDefault(resource =>
            resource.EndsWith(fileName, StringComparison.Ordinal));
        if (name is null)
        {
            throw new InvalidOperationException($"Embedded occupation resource {fileName} is missing.");
        }

        return assembly.GetManifestResourceStream(name)
               ?? throw new InvalidOperationException($"Could not open {name}.");
    }

    private sealed class OccupationDto
    {
        public string Id { get; set; } = "";
        public string Uri { get; set; } = "";
        public string Nl { get; set; } = "";
        public List<string>? Alt { get; set; }
        public string? Desc { get; set; }
        public string Isco { get; set; } = "";
        public int? IscoLevel { get; set; }
        public string? Brc { get; set; }
        public double[]? Oi { get; set; }
        public string Tier { get; set; } = "";
        public string Confidence { get; set; } = "";
        public List<string>? Onet { get; set; }
        public double[]? OfficialOi { get; set; }
        public string? OfficialTier { get; set; }
        public string? OfficialConfidence { get; set; }
        public List<string>? OfficialOnet { get; set; }
    }

    private sealed class OnetDto
    {
        public double[]? Oi { get; set; }
    }

    private sealed class CorrectionsDto
    {
        public List<CorrectionItem>? Items { get; set; }
        public List<LevelSwapDto>? LevelSubstitutions { get; set; }
    }

    private sealed class CorrectionItem
    {
        public string EscoId { get; set; } = "";
        public string? Kind { get; set; }
        public string? Status { get; set; }
        public string? Confidence { get; set; }
        public List<string>? Onet { get; set; }
        public List<SearchHintDto>? Search { get; set; }
    }

    private sealed class SearchHintDto
    {
        public string? Query { get; set; }
        public List<string>? AlsoShow { get; set; }
        public List<string>? ShowFirst { get; set; }
    }

    private sealed class LevelSwapDto
    {
        public string FromEscoId { get; set; } = "";
        public string ToEscoId { get; set; } = "";
        public string? Status { get; set; }
    }
}

public sealed class Occupation
{
    public string Id { get; init; } = "";
    public string Uri { get; init; } = "";
    public string Nl { get; init; } = "";
    public IReadOnlyList<string> Alt { get; init; } = [];
    public string Desc { get; init; } = "";
    public string Isco { get; init; } = "";
    public int? IscoLevel { get; init; }
    public string? Brc { get; init; }
    public IReadOnlyList<double>? Oi { get; set; }
    public string Tier { get; set; } = "";
    public string Confidence { get; set; } = "";
    public IReadOnlyList<string> Onet { get; set; } = [];
    public IReadOnlyList<double>? OfficialOi { get; init; }
    public string OfficialTier { get; init; } = "";
    public string OfficialConfidence { get; init; } = "";
    public IReadOnlyList<string> OfficialOnet { get; init; } = [];

    public bool IsListable
        => Oi is { Count: 6 } && Confidence is "high" or "medium";

    public bool NoScore
        => Oi is null || Confidence is "low" or "none" or "";
}

public sealed record CbsTitle(
    [property: JsonPropertyName("title")] string Title,
    [property: JsonPropertyName("isco")] string Isco);

public sealed record OccupationSearchHit(
    string EscoId,
    string Title,
    string Description,
    bool NoScore,
    bool LevelNote,
    string Isco,
    int? IscoLevel);

public sealed record SearchRule(string Query, IReadOnlyList<string> AlsoShow, IReadOnlyList<string> ShowFirst);

public sealed record LevelSwap(string FromId, string ToId);

using System.Reflection;
using System.Text.Json;

namespace Jobsy.Core.Careers;

/// <summary>ESCO essential and optional skills, Dutch labels, loaded from the generated catalogue.</summary>
public sealed class OccupationSkills
{
    private static readonly JsonSerializerOptions Json = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private static OccupationSkills? _shared;

    private static readonly IReadOnlySet<int> Empty = new HashSet<int>();

    private readonly Dictionary<string, SkillLists> _byOccupation;
    private readonly Dictionary<string, IReadOnlySet<int>> _allOf = new(StringComparer.OrdinalIgnoreCase);
    private readonly IReadOnlyList<SkillLabel> _labels;

    private OccupationSkills(Dictionary<string, SkillLists> byOccupation, IReadOnlyList<SkillLabel> labels)
    {
        _byOccupation = byOccupation;
        _labels = labels;
    }

    public static OccupationSkills Shared => _shared ??= LoadEmbedded();

    public static OccupationSkills LoadEmbedded()
    {
        using var occupations = Open("occupation-skills.json");
        using var labels = Open("skills.nl.json");
        return Load(occupations, labels);
    }

    public static OccupationSkills Load(Stream occupations, Stream labels)
    {
        var map = JsonSerializer.Deserialize<Dictionary<string, SkillLists>>(occupations, Json)
                  ?? new Dictionary<string, SkillLists>(StringComparer.OrdinalIgnoreCase);
        var rows = JsonSerializer.Deserialize<List<SkillLabel>>(labels, Json) ?? [];
        return new OccupationSkills(
            new Dictionary<string, SkillLists>(map, StringComparer.OrdinalIgnoreCase),
            rows);
    }

    public IReadOnlyList<int> Essential(string? escoId) => Lists(escoId)?.Essential ?? [];

    public IReadOnlyList<int> Optional(string? escoId) => Lists(escoId)?.Optional ?? [];

    public string? Label(int index)
        => index >= 0 && index < _labels.Count ? _labels[index].Nl : null;

    public IReadOnlySet<int> AllOf(string? escoId)
    {
        if (string.IsNullOrWhiteSpace(escoId))
        {
            return Empty;
        }

        var key = escoId.Trim();
        if (_allOf.TryGetValue(key, out var cached))
        {
            return cached;
        }

        var lists = Lists(key);
        if (lists is null)
        {
            _allOf[key] = Empty;
            return Empty;
        }

        var set = new HashSet<int>(lists.Essential ?? []);
        if (lists.Optional is not null)
        {
            set.UnionWith(lists.Optional);
        }

        _allOf[key] = set;
        return set;
    }

    private SkillLists? Lists(string? escoId)
        => !string.IsNullOrWhiteSpace(escoId) && _byOccupation.TryGetValue(escoId.Trim(), out var lists)
            ? lists
            : null;

    private static Stream Open(string fileName)
    {
        var assembly = typeof(OccupationSkills).Assembly;
        var name = assembly.GetManifestResourceNames().FirstOrDefault(resource =>
            resource.EndsWith(fileName, StringComparison.OrdinalIgnoreCase));
        if (name is null)
        {
            throw new InvalidOperationException($"Embedded occupation resource {fileName} is missing.");
        }

        return assembly.GetManifestResourceStream(name)
               ?? throw new InvalidOperationException($"Could not open {name}.");
    }

    private sealed class SkillLists
    {
        public List<int>? Essential { get; set; }
        public List<int>? Optional { get; set; }
    }

    private sealed class SkillLabel
    {
        public string? Uri { get; set; }
        public string? Nl { get; set; }
    }
}

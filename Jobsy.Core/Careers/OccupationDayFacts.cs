using System.Reflection;
using System.Text;
using System.Text.Json;

namespace Jobsy.Core.Careers;

/// <summary>
/// Grounding for one typical day: ESCO title, description, skills and ILO task lines already in the product.
/// Nothing here is about a candidate, an employer, a city or a wage.
/// </summary>
public sealed class OccupationDayFacts
{
    public const int MaxDescriptionChars = 900;
    public const int MaxSkills = 12;
    public const int MaxTasks = 8;

    private static readonly JsonSerializerOptions Json = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private static readonly Lazy<IReadOnlyDictionary<string, string[]>> TasksByIsco = new(LoadTasks);

    private OccupationDayFacts(
        string escoId,
        string uri,
        string titleNl,
        string description,
        IReadOnlyList<string> altNames,
        IReadOnlyList<string> skills,
        IReadOnlyList<string> tasks)
    {
        EscoId = escoId;
        Uri = uri;
        TitleNl = titleNl;
        Description = description;
        AltNames = altNames;
        Skills = skills;
        Tasks = tasks;
        IsThin = description.Length < 120 && skills.Count < 2 && tasks.Count < 2;
    }

    public string EscoId { get; }
    public string Uri { get; }
    public string TitleNl { get; }
    public string Description { get; }
    public IReadOnlyList<string> AltNames { get; }
    public IReadOnlyList<string> Skills { get; }
    public IReadOnlyList<string> Tasks { get; }
    public bool IsThin { get; }

    public string SourceText
        => string.Join(
            '\n',
            new[] { TitleNl, Description }
                .Concat(AltNames)
                .Concat(Skills)
                .Concat(Tasks));

    public static OccupationDayFacts? For(string? escoId)
    {
        var job = OccupationCatalog.Shared.Get(escoId);
        if (job is null || string.IsNullOrWhiteSpace(job.Nl))
        {
            return null;
        }

        var skills = new List<string>();
        foreach (var index in OccupationSkills.Shared.Essential(job.Id).Take(8))
        {
            AddSkill(skills, OccupationSkills.Shared.Label(index));
        }

        foreach (var index in OccupationSkills.Shared.Optional(job.Id))
        {
            if (skills.Count >= MaxSkills)
            {
                break;
            }

            AddSkill(skills, OccupationSkills.Shared.Label(index));
        }

        var tasks = TasksByIsco.Value.TryGetValue(job.Isco, out var lines)
            ? lines.Take(MaxTasks).ToList()
            : [];
        var description = TrimDescription(job.Desc);
        var alt = job.Alt
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .Select(name => name.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(4)
            .ToList();
        return new OccupationDayFacts(job.Id, job.Uri, job.Nl.Trim(), description, alt, skills, tasks);
    }

    /// <summary>Test and import helper. Does not invent fields that were not passed in.</summary>
    public static OccupationDayFacts Create(
        string escoId,
        string uri,
        string title,
        string description,
        IReadOnlyList<string>? skills = null,
        IReadOnlyList<string>? tasks = null,
        IReadOnlyList<string>? altNames = null)
        => new(
            escoId.Trim(),
            uri.Trim(),
            title.Trim(),
            TrimDescription(description),
            (altNames ?? []).Where(s => !string.IsNullOrWhiteSpace(s)).Select(s => s.Trim()).Take(4).ToList(),
            (skills ?? []).Where(s => !string.IsNullOrWhiteSpace(s)).Select(s => s.Trim()).Take(MaxSkills).ToList(),
            (tasks ?? []).Where(s => !string.IsNullOrWhiteSpace(s)).Select(s => s.Trim()).Take(MaxTasks).ToList());

    public string ToPrompt()
    {
        var sb = new StringBuilder();
        sb.Append("Beroep: ").AppendLine(TitleNl);
        sb.Append("Andere namen in de bron: ");
        sb.AppendLine(AltNames.Count == 0 ? "geen" : string.Join(", ", AltNames));
        sb.AppendLine("Beschrijving:");
        sb.AppendLine(Description.Length == 0 ? "geen" : Description);
        sb.AppendLine("Vaardigheden uit de bron:");
        AppendLines(sb, Skills);
        sb.AppendLine("Taken uit de bron:");
        AppendLines(sb, Tasks);
        sb.Append("Bron is dun: ").AppendLine(IsThin ? "ja" : "nee");
        return sb.ToString().Trim();
    }

    private static void AppendLines(StringBuilder sb, IReadOnlyList<string> lines)
    {
        if (lines.Count == 0)
        {
            sb.AppendLine("- geen");
            return;
        }

        foreach (var line in lines)
        {
            sb.Append("- ").AppendLine(line);
        }
    }

    private static void AddSkill(List<string> skills, string? label)
    {
        var text = (label ?? "").Trim();
        if (text.Length == 0 || skills.Contains(text, StringComparer.OrdinalIgnoreCase))
        {
            return;
        }

        skills.Add(text);
    }

    private static string TrimDescription(string? description)
    {
        var text = (description ?? "").Trim().Replace("\r\n", "\n", StringComparison.Ordinal);
        if (text.Length <= MaxDescriptionChars)
        {
            return text;
        }

        var cut = text.LastIndexOf('.', MaxDescriptionChars - 1);
        if (cut < 200)
        {
            cut = MaxDescriptionChars;
        }

        return text[..cut].Trim();
    }

    private static IReadOnlyDictionary<string, string[]> LoadTasks()
    {
        using var stream = Open("ilo_tasks_nl.json");
        var rows = JsonSerializer.Deserialize<List<TaskLine>>(stream, Json) ?? [];
        var grouped = new Dictionary<string, List<string>>(StringComparer.Ordinal);
        foreach (var row in rows)
        {
            var isco = (row.Isco ?? "").Trim();
            var nl = (row.Nl ?? "").Trim();
            var en = (row.En ?? "").Trim();
            var line = nl.Length > 0 ? nl : en;
            if (isco.Length == 0 || line.Length == 0)
            {
                continue;
            }

            if (!grouped.TryGetValue(isco, out var list))
            {
                list = [];
                grouped[isco] = list;
            }

            if (list.Contains(line, StringComparer.OrdinalIgnoreCase) || list.Count >= MaxTasks)
            {
                continue;
            }

            list.Add(line);
        }

        return grouped.ToDictionary(
            pair => pair.Key,
            pair => pair.Value.ToArray(),
            StringComparer.Ordinal);
    }

    private static Stream Open(string fileName)
    {
        var assembly = typeof(OccupationDayFacts).Assembly;
        var name = assembly.GetManifestResourceNames().FirstOrDefault(resource =>
            resource.EndsWith(fileName, StringComparison.OrdinalIgnoreCase));
        if (name is null)
        {
            throw new InvalidOperationException($"Embedded occupation resource {fileName} is missing.");
        }

        return assembly.GetManifestResourceStream(name)
               ?? throw new InvalidOperationException($"Could not open {name}.");
    }

    private sealed class TaskLine
    {
        public string? Isco { get; set; }

        public string? En { get; set; }
        public string? Nl { get; set; }
    }
}

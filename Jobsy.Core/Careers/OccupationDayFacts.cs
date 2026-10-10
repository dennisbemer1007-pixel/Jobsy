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

    private static readonly Lazy<IReadOnlyDictionary<string, TaskLine[]>> TasksByIsco = new(LoadTasks);

    private OccupationDayFacts(
        string escoId,
        string uri,
        string titleNl,
        string description,
        IReadOnlyList<string> altNames,
        IReadOnlyList<string> skills,
        IReadOnlyList<string> tasks,
        OccupationDaySourceRewrite.WorkplaceKind workplace)
    {
        EscoId = escoId;
        Uri = uri;
        TitleNl = titleNl;
        Description = description;
        AltNames = altNames;
        Skills = skills;
        Tasks = tasks;
        Workplace = workplace;
        IsThin = description.Length < 120 && skills.Count < 2 && tasks.Count < 2;
    }

    public string EscoId { get; }
    public string Uri { get; }
    public string TitleNl { get; }
    public string Description { get; }
    public IReadOnlyList<string> AltNames { get; }
    public IReadOnlyList<string> Skills { get; }
    public IReadOnlyList<string> Tasks { get; }
    public OccupationDaySourceRewrite.WorkplaceKind Workplace { get; }
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

        var alt = job.Alt
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .Select(name => name.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(4)
            .ToList();
        var description = OccupationDaySourceRewrite.RewriteDescription(TrimDescription(job.Desc));
        var workplace = OccupationDaySourceRewrite.DetectWorkplace(job.Nl.Trim(), description, alt);
        var tasks = TasksByIsco.Value.TryGetValue(job.Isco, out var lines)
            ? lines
                .Take(MaxTasks)
                .Select(line => OccupationDaySourceRewrite.RewriteTask(line.Nl, line.En, workplace))
                .Where(line => line.Length > 0)
                .ToList()
            : [];
        return new OccupationDayFacts(job.Id, job.Uri, job.Nl.Trim(), description, alt, skills, tasks, workplace);
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
    {
        var alt = (altNames ?? []).Where(s => !string.IsNullOrWhiteSpace(s)).Select(s => s.Trim()).Take(4).ToList();
        var desc = OccupationDaySourceRewrite.RewriteDescription(TrimDescription(description));
        var workplace = OccupationDaySourceRewrite.DetectWorkplace(title.Trim(), desc, alt);
        var taskLines = (tasks ?? []).Where(s => !string.IsNullOrWhiteSpace(s)).Select(s => s.Trim()).Take(MaxTasks).ToList();
        return new OccupationDayFacts(
            escoId.Trim(),
            uri.Trim(),
            title.Trim(),
            desc,
            alt,
            (skills ?? []).Where(s => !string.IsNullOrWhiteSpace(s)).Select(s => s.Trim()).Take(MaxSkills).ToList(),
            taskLines,
            workplace);
    }

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
        sb.AppendLine("Taken uit de bron (al in gewoon Nederlands, B1):");
        AppendLines(sb, Tasks);
        sb.Append("Werkplek: ").AppendLine(WorkplaceHint());
        sb.Append("Bron is dun: ").AppendLine(IsThin ? "ja" : "nee");
        sb.AppendLine();
        sb.AppendLine(OccupationDayInLifePrompt.GenerationRules);
        return sb.ToString().Trim();
    }

    private string WorkplaceHint() => Workplace switch
    {
        OccupationDaySourceRewrite.WorkplaceKind.Greenhouse =>
            "kwekerij of kas (geen hovenier of particuliere tuin)",
        OccupationDaySourceRewrite.WorkplaceKind.Kitchen => "keuken of institutionele catering",
        OccupationDaySourceRewrite.WorkplaceKind.RoadTransport => "wegtransport met vrachtwagen",
        _ => "algemeen"
    };

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

    private static IReadOnlyDictionary<string, TaskLine[]> LoadTasks()
    {
        using var stream = Open("ilo_tasks_nl.json");
        var rows = JsonSerializer.Deserialize<List<TaskLine>>(stream, Json) ?? [];
        var grouped = new Dictionary<string, List<TaskLine>>(StringComparer.Ordinal);
        foreach (var row in rows)
        {
            var isco = (row.Isco ?? "").Trim();
            var nl = (row.Nl ?? "").Trim();
            var en = (row.En ?? "").Trim();
            if (isco.Length == 0 || (nl.Length == 0 && en.Length == 0))
            {
                continue;
            }

            if (!grouped.TryGetValue(isco, out var list))
            {
                list = [];
                grouped[isco] = list;
            }

            if (list.Any(item => string.Equals(item.Nl, nl, StringComparison.OrdinalIgnoreCase)
                                 && string.Equals(item.En, en, StringComparison.OrdinalIgnoreCase))
                || list.Count >= MaxTasks)
            {
                continue;
            }

            list.Add(new TaskLine { Isco = isco, Nl = nl, En = en });
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

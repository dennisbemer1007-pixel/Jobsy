using System.Globalization;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Jobsy.Core.Careers;

/// <summary>
/// Sourced demand and task-exposure lines. Templates only: no generated numbers and no generated sentences.
/// Approved Dutch task lines are the only task text that may be shown.
/// </summary>
public sealed class OccupationOutlook
{
    public const string ApprovedStatus = "goedgekeurd";

    public const string SourceLine =
        "Bron: ROA, AIS tot 2030 (editie 2026, peildatum {0}); ILO Working Paper 140 (2025), door Lobsy vertaald en ingekort.";

    public const string MissingDemand = "Dat weten we niet: er zijn geen cijfers over de vraag naar dit werk.";
    public const string MissingIlo = "Dat weten we niet: de ILO heeft geen cijfers voor dit beroep.";

    private static readonly JsonSerializerOptions Json = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private static readonly string[] ForbiddenWords =
    [
        "verdwijnt",
        "verdwijnen",
        "geen toekomst",
        "overbodig",
        "ontslag",
        "werkloos"
    ];

    private static OccupationOutlook? _shared;

    private readonly Dictionary<string, RoaGroup> _roa;
    private readonly Dictionary<string, IloGroup> _ilo;
    private readonly Dictionary<string, string> _approvedTasks;

    private OccupationOutlook(
        string peildatum,
        IReadOnlyDictionary<string, RoaGroup> roa,
        IReadOnlyDictionary<string, IloGroup> ilo,
        IReadOnlyDictionary<string, string> approvedTasks)
    {
        Peildatum = peildatum;
        _roa = new Dictionary<string, RoaGroup>(roa, StringComparer.Ordinal);
        _ilo = new Dictionary<string, IloGroup>(ilo, StringComparer.Ordinal);
        _approvedTasks = new Dictionary<string, string>(approvedTasks, StringComparer.Ordinal);
    }

    public static OccupationOutlook Shared => _shared ??= LoadEmbedded();

    public string Peildatum { get; }

    public static OccupationOutlook LoadEmbedded()
    {
        using var outlook = Open("outlook.json");
        using var tasks = Open("ilo_tasks_nl.json");
        return Load(outlook, tasks);
    }

    public static OccupationOutlook Load(Stream outlook, Stream tasks)
    {
        var file = JsonSerializer.Deserialize<OutlookFile>(outlook, Json)
                   ?? throw new InvalidOperationException("outlook.json is empty.");
        var lines = JsonSerializer.Deserialize<List<TaskLine>>(tasks, Json) ?? [];
        var approved = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var line in lines)
        {
            var status = (line.Status ?? "").Trim();
            var nl = (line.Nl ?? "").Trim();
            if (status == ApprovedStatus && nl.Length == 0)
            {
                throw new InvalidOperationException(
                    $"Goedgekeurde taak {line.Isco} {line.TaskId} heeft geen Nederlandse tekst.");
            }

            if (status == ApprovedStatus && !string.IsNullOrWhiteSpace(line.Isco) && !string.IsNullOrWhiteSpace(line.TaskId))
            {
                approved[TaskKey(line.Isco, line.TaskId)] = nl;
            }
        }

        return new OccupationOutlook(
            (file.Peildatum ?? "").Trim(),
            file.RoaByBrc ?? new Dictionary<string, RoaGroup>(StringComparer.Ordinal),
            file.IloByIsco ?? new Dictionary<string, IloGroup>(StringComparer.Ordinal),
            approved);
    }

    public OccupationOutlookResult Get(string? escoId)
    {
        var occupation = OccupationCatalog.Shared.Get(escoId);
        var source = string.Format(CultureInfo.InvariantCulture, SourceLine, Peildatum);
        if (occupation is null)
        {
            return new OccupationOutlookResult(
                MissingDemand,
                MissingIlo,
                [],
                [],
                [source],
                Peildatum,
                ["geen-beroep", "geen-vraag", "geen-ilo"]);
        }

        _roa.TryGetValue(occupation.Brc ?? "", out var roa);
        _ilo.TryGetValue(occupation.Isco, out var ilo);
        var missing = new List<string>();
        if (roa?.Itkb?.Typering is null)
        {
            missing.Add("geen-vraag");
        }

        if (string.IsNullOrWhiteSpace(ilo?.Potential25))
        {
            missing.Add("geen-ilo");
        }

        var high = Approved(occupation.Isco, ilo?.High);
        var low = Approved(occupation.Isco, ilo?.Low);
        var demand = Demand(roa);
        var ai = Ai(ilo?.Potential25, high.FirstOrDefault(), low.FirstOrDefault());
        return new OccupationOutlookResult(
            demand,
            ai,
            high,
            low,
            [source],
            Peildatum,
            missing);
    }

    /// <summary>Demand sentence for one BRC group. Null group means the figures are missing.</summary>
    public static string Demand(RoaGroup? group)
    {
        var typering = (group?.Itkb?.Typering ?? "").Trim().ToLowerInvariant();
        var lead = typering switch
        {
            "zeer groot" or "groot" =>
                "Tot 2030 hebben werkgevers naar verwachting moeite om genoeg mensen te vinden voor dit soort werk.",
            "enige" =>
                "Tot 2030 is er naar verwachting redelijk wat vraag naar mensen voor dit soort werk.",
            "vrijwel geen" or "geen" =>
                "Tot 2030 zijn er naar verwachting genoeg mensen voor dit soort werk. Een baan vinden kan meer moeite kosten.",
            _ => MissingDemand
        };

        if (lead == MissingDemand || group?.Baanopeningen?.Totaal6jrperc is not double openings)
        {
            return lead;
        }

        var replacement = group.Vervanging?.Totaal6jrperc;
        var expansion = group.Uitbreiding?.Totaal6jrperc;
        if (replacement is null || expansion is null)
        {
            return lead;
        }

        var reason = replacement > expansion
            ? "mensen met pensioen gaan of ander werk kiezen"
            : "er meer werk bijkomt";
        return lead
               + " Tot 2030 komen naar schatting "
               + FormatCount(openings)
               + " van elke 100 banen vrij, vooral omdat "
               + reason
               + ".";
    }

    /// <summary>AI sentence for one ILO label. Example fragments are left out when the task is not approved.</summary>
    public static string Ai(string? potential25, string? highestApproved, string? lowestApproved)
    {
        var label = (potential25 ?? "").Trim();
        var high = string.IsNullOrWhiteSpace(highestApproved) ? null : highestApproved.Trim();
        var low = string.IsNullOrWhiteSpace(lowestApproved) ? null : lowestApproved.Trim();
        if (high is not null && low is not null && string.Equals(high, low, StringComparison.Ordinal))
        {
            low = null;
        }

        return label switch
        {
            "Not Exposed" => "Volgens de ILO verandert AI weinig aan de taken in dit werk.",
            "Minimal Exposure" => high is null
                ? "AI raakt maar een klein deel van de taken."
                : $"AI raakt maar een klein deel van de taken, bijvoorbeeld {high}.",
            "Exposed: Gradient 1" => Gradient1(high, low),
            "Exposed: Gradient 2" => Gradient2(high, low),
            "Exposed: Gradient 3" => high is null
                ? "Veel taken kunnen door AI veranderen. Het werk gaat er waarschijnlijk anders uitzien."
                : $"Veel taken kunnen door AI veranderen, zoals {high}. Het werk gaat er waarschijnlijk anders uitzien.",
            "Exposed: Gradient 4" => high is null
                ? "De meeste taken kunnen door AI veranderen. Het is slim om nu iets extra's te leren."
                : $"De meeste taken kunnen door AI veranderen, zoals {high}. Het is slim om nu iets extra's te leren.",
            _ => MissingIlo
        };
    }

    public static bool ContainsForbiddenWord(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return false;
        }

        var fold = text.ToLowerInvariant();
        foreach (var word in ForbiddenWords)
        {
            if (fold.Contains(word, StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }

    public static int IloRank(string? potential25) => (potential25 ?? "").Trim() switch
    {
        "Not Exposed" => 0,
        "Minimal Exposure" => 1,
        "Exposed: Gradient 1" => 2,
        "Exposed: Gradient 2" => 3,
        "Exposed: Gradient 3" => 4,
        "Exposed: Gradient 4" => 5,
        _ => -1
    };

    public static int ItkbRank(string? typering) => (typering ?? "").Trim().ToLowerInvariant() switch
    {
        "geen" => 0,
        "vrijwel geen" => 1,
        "enige" => 2,
        "groot" => 3,
        "zeer groot" => 4,
        _ => -1
    };

    public bool TryGetComparison(string? escoId, out int iloRank, out int itkbRank)
    {
        iloRank = -1;
        itkbRank = -1;
        var occupation = OccupationCatalog.Shared.Get(escoId);
        if (occupation is null)
        {
            return false;
        }

        if (_ilo.TryGetValue(occupation.Isco, out var ilo))
        {
            iloRank = IloRank(ilo.Potential25);
        }

        if (!string.IsNullOrWhiteSpace(occupation.Brc)
            && _roa.TryGetValue(occupation.Brc, out var roa))
        {
            itkbRank = ItkbRank(roa.Itkb?.Typering);
        }

        return iloRank >= 0 && itkbRank >= 0;
    }

    /// <summary>
    /// Demand facts for one occupation. False when the ROA typering or the openings
    /// percentage is missing. The AI line is null when the ILO has no label for this job.
    /// </summary>
    public bool TryGetSourcedDemand(string? escoId, out SourcedDemand? demand)
    {
        demand = null;
        var occupation = OccupationCatalog.Shared.Get(escoId);
        if (occupation is null || string.IsNullOrWhiteSpace(occupation.Brc))
        {
            return false;
        }

        if (!_roa.TryGetValue(occupation.Brc, out var roa))
        {
            return false;
        }

        var rank = ItkbRank(roa.Itkb?.Typering);
        if (rank < 0 || roa.Baanopeningen?.Totaal6jrperc is not double openings)
        {
            return false;
        }

        var outlook = Get(occupation.Id);
        var ai = outlook.MissingReasons.Contains("geen-ilo") || outlook.AiLine == MissingIlo
            ? null
            : outlook.AiLine;
        if (ContainsForbiddenWord(ai))
        {
            ai = null;
        }

        demand = new SourcedDemand(
            rank,
            (roa.Itkb?.Typering ?? "").Trim().ToLowerInvariant(),
            openings,
            string.IsNullOrWhiteSpace(ai) ? null : ai.Trim(),
            (roa.Baanopeningen?.Typering ?? "").Trim().ToLowerInvariant());
        return true;
    }

    private List<string> Approved(string isco, IReadOnlyList<IloTask>? tasks)
    {
        var lines = new List<string>();
        if (tasks is null)
        {
            return lines;
        }

        foreach (var task in tasks)
        {
            if (string.IsNullOrWhiteSpace(task.TaskId))
            {
                continue;
            }

            if (_approvedTasks.TryGetValue(TaskKey(isco, task.TaskId), out var nl) && lines.Contains(nl) is false)
            {
                lines.Add(nl);
            }
        }

        return lines;
    }

    private static string Gradient1(string? high, string? low)
    {
        var first = high is null
            ? "Een paar taken kunnen door computers of AI veranderen."
            : $"Een paar taken kunnen door computers of AI veranderen, zoals {high}.";
        var second = low is null
            ? "Het meeste werk blijft mensenwerk."
            : $"Het meeste werk blijft mensenwerk, zoals {low}.";
        return first + " " + second;
    }

    private static string Gradient2(string? high, string? low)
    {
        var first = high is null
            ? "Een deel van de taken kan door AI veranderen."
            : $"Een deel van de taken kan door AI veranderen, zoals {high}.";
        return low is null ? first : first + $" {low} blijft mensenwerk.";
    }

    private static string FormatCount(double value)
    {
        var whole = Math.Round(value, 0, MidpointRounding.AwayFromZero);
        if (Math.Abs(value - whole) < 0.001)
        {
            return whole.ToString("0", CultureInfo.InvariantCulture);
        }

        return value.ToString("0.##", CultureInfo.InvariantCulture);
    }

    private static string TaskKey(string isco, string taskId) => isco.Trim() + "\u001f" + taskId.Trim();

    private static Stream Open(string fileName)
    {
        var assembly = typeof(OccupationOutlook).Assembly;
        var name = assembly.GetManifestResourceNames().FirstOrDefault(resource =>
            resource.EndsWith(fileName, StringComparison.OrdinalIgnoreCase));
        if (name is null)
        {
            throw new InvalidOperationException($"Embedded occupation resource {fileName} is missing.");
        }

        return assembly.GetManifestResourceStream(name)
               ?? throw new InvalidOperationException($"Could not open {name}.");
    }

    private sealed class OutlookFile
    {
        public string? Peildatum { get; set; }
        public Dictionary<string, RoaGroup>? RoaByBrc { get; set; }
        public Dictionary<string, IloGroup>? IloByIsco { get; set; }
    }

    private sealed class TaskLine
    {
        public string? Isco { get; set; }

        [JsonPropertyName("taskID")]
        public string? TaskId { get; set; }

        public string? Nl { get; set; }
        public string? Status { get; set; }
    }
}

public sealed class RoaGroup
{
    public RoaMeasure? Itkb { get; set; }
    public RoaMeasure? Baanopeningen { get; set; }
    public RoaMeasure? Uitbreiding { get; set; }
    public RoaMeasure? Vervanging { get; set; }
    public RoaMeasure? Richting { get; set; }
    public RoaMeasure? Huidige { get; set; }
    public List<string>? Vergelijkbaar { get; set; }
}

public sealed class RoaMeasure
{
    public string? Typering { get; set; }
    public string? Indicator { get; set; }
    public double? Aantal { get; set; }
    public double? Totaal6jrperc { get; set; }
}

public sealed class IloGroup
{
    public string? Potential25 { get; set; }
    public double? MeanScore2025 { get; set; }
    public double? Sd2025 { get; set; }
    public List<IloTask>? High { get; set; }
    public List<IloTask>? Low { get; set; }
}

public sealed class IloTask
{
    [JsonPropertyName("taskID")]
    public string? TaskId { get; set; }

    public string? En { get; set; }
}

public sealed record SourcedDemand(
    int ItkbRank,
    string Typering,
    double OpeningsPer100,
    string? AiLine,
    string OpeningsTypering);

public sealed record OccupationOutlookResult(
    string? DemandLine,
    string? AiLine,
    IReadOnlyList<string> ChangeTasks,
    IReadOnlyList<string> HumanTasks,
    IReadOnlyList<string> Sources,
    string? Peildatum,
    IReadOnlyList<string> MissingReasons);

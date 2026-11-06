using System.Text.Json;
using Jobsy.Core.Careers;
using Jobsy.Core.Rules;

if (args.Length > 0 && args[0] == "--print-facts")
{
    var t = args.Length > 1 ? args[1] : "dierenverzorger";
    var occ = ResolveOccupation(t);
    if (occ is null)
    {
        return 1;
    }

    var f = OccupationDayFacts.For(occ.Id)!;
    Console.WriteLine("esco=" + f.EscoId);
    Console.WriteLine("title=" + f.TitleNl);
    foreach (var task in f.Tasks)
    {
        Console.WriteLine("task: " + task);
    }

    foreach (var skill in f.Skills)
    {
        Console.WriteLine("skill: " + skill);
    }

    return 0;
}

var dreamKey = args.Length > 0 ? args[0] : "dierenverzorger";
var dataDir = DataDir();
var outPath = args.Length > 1
    ? args[1]
    : Path.Combine(dataDir, $"occupation-day-{dreamKey}.json");
outPath = Path.GetFullPath(outPath);

var title = DreamJobCatalog.All.FirstOrDefault(j =>
    string.Equals(j.Key, dreamKey, StringComparison.OrdinalIgnoreCase))?.TitleNl ?? dreamKey;

var occupation = ResolveOccupation(title);
if (occupation is null)
{
    Console.Error.WriteLine("unknown occupation for " + title);
    return 1;
}

var facts = OccupationDayFacts.For(occupation.Id);
if (facts is null)
{
    Console.Error.WriteLine("no facts for " + occupation.Id);
    return 1;
}

var exportPath = Path.Combine(dataDir, "occupation-day-in-life.nl.json");
string source;
List<OccupationDayBlock> blocks;
string varies;
List<string> highlights;
List<string> tasks;
List<string> skills;

if (TryLoadStoredExport(exportPath, facts.EscoId, out var stored))
{
    source = "occupation-day-in-life-export";
    blocks = stored.Blocks;
    varies = stored.VariesNote;
    highlights = stored.Highlights;
    tasks = stored.Tasks;
    skills = stored.Skills;
}
else
{
    var draft = BuildDraft(facts);
    if (!OccupationDayInLifeValidator.TryValidate(draft, facts, out var reasons))
    {
        Console.Error.WriteLine("validator failed: " + string.Join("; ", reasons));
        return 2;
    }

    blocks = draft.Blocks!.ToList();
    varies = draft.VariesNote;
    highlights = draft.Highlights.ToList();
    tasks = draft.Tasks!.ToList();
    skills = draft.Skills!.ToList();
    source = "occupation-day-facts-grounded";
}

var payload = new
{
    source,
    dreamJobKey = dreamKey,
    escoId = facts.EscoId,
    titleNl = facts.TitleNl,
    dreamTitle = title,
    thinSource = facts.IsThin,
    variesNote = varies,
    highlights,
    tasks,
    skills,
    blocks = blocks.Select(b => new { key = b.Key, label = b.Label, text = b.Text }).ToList()
};

Directory.CreateDirectory(Path.GetDirectoryName(outPath)!);
await File.WriteAllTextAsync(outPath, JsonSerializer.Serialize(payload, new JsonSerializerOptions { WriteIndented = true }));
Console.WriteLine(outPath);
return 0;

static string DataDir()
    => Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "data"));

static Occupation? ResolveOccupation(string title)
{
    var occupation = OccupationCatalog.Shared.Resolve(title);
    if (occupation is not null)
    {
        return occupation;
    }

    var hits = OccupationCatalog.Shared.Search(title, 1);
    return hits.Count > 0 ? OccupationCatalog.Shared.Get(hits[0].EscoId) : null;
}

static bool TryLoadStoredExport(
    string path,
    string escoId,
    out (List<OccupationDayBlock> Blocks, string VariesNote, List<string> Highlights, List<string> Tasks, List<string> Skills) row)
{
    row = default!;
    if (!File.Exists(path))
    {
        return false;
    }

    var doc = JsonSerializer.Deserialize<OccupationDayExportDocument>(File.ReadAllText(path), OccupationDayJson.Options);
    var found = doc?.Rows?.FirstOrDefault(r => string.Equals(r.EscoId, escoId, StringComparison.OrdinalIgnoreCase));
    if (found?.Blocks is not { Count: > 0 })
    {
        return false;
    }

    row = (
        OccupationDayBlocks.Normalize(found.Blocks).ToList(),
        found.VariesNote,
        found.Highlights,
        found.Tasks ?? [],
        found.Skills ?? []);
    return true;
}

static OccupationDayDraft BuildDraft(OccupationDayFacts facts)
{
    var lines = facts.Tasks
        .Where(t => !t.Contains("Cleaning, organizing", StringComparison.Ordinal))
        .Where(t => !t.Contains("geneesmiddelen", StringComparison.OrdinalIgnoreCase))
        .Where(t => !LooksEnglishOnly(t))
        .Select(ShortSentence)
        .Where(t => t.Length > 0)
        .ToList();

    var intro = ShortSentence(facts.Description);
    if (intro.Length > 0)
    {
        lines.Insert(0, intro);
    }

    while (lines.Count < 5 && facts.Skills.Count > 0)
    {
        var skill = facts.Skills[lines.Count % facts.Skills.Count];
        lines.Add(ShortSentence("Je let op " + skill + "."));
    }

    var specs = new (string Key, string Label, int TaskIndex)[]
    {
        ("start", "07:00 Start", 0),
        ("morning", "09:00 Ochtend", 1),
        ("pause", "12:00 Pauze", 2),
        ("afternoon", "14:00 Middag", 3),
        ("close", "17:00 Afronden", 4)
    };

    var blocks = new List<OccupationDayBlock>();
    foreach (var (key, label, idx) in specs)
    {
        blocks.Add(new OccupationDayBlock(key, label, lines[Math.Min(idx, lines.Count - 1)]));
    }

    blocks = OccupationDayBlocks.Normalize(blocks).ToList();
    var highlights = PickHighlights(facts);
    return OccupationDayBlocks.WithDerived(new OccupationDayDraft(
        facts.TitleNl,
        "",
        "",
        "",
        "",
        highlights,
        "De volgorde kan per werkgever anders zijn.",
        blocks,
        facts.Tasks.Where(t => !LooksEnglishOnly(t)).Take(6).ToList(),
        facts.Skills.Take(6).ToList()));
}

static bool LooksEnglishOnly(string text)
{
    var t = text.Trim();
    if (t.Length == 0)
    {
        return true;
    }

    return t.Contains(" and ", StringComparison.OrdinalIgnoreCase)
        || t.Contains(" such as ", StringComparison.OrdinalIgnoreCase);
}

static List<string> PickHighlights(OccupationDayFacts facts)
{
    var list = new List<string>();
    foreach (var skill in facts.Skills)
    {
        if (!skill.Contains("dieren", StringComparison.OrdinalIgnoreCase) || list.Count >= 2)
        {
            continue;
        }

        var line = skill.Trim();
        if (WordCount(line) < 2)
        {
            line = "Letten op " + line;
        }

        list.Add(char.ToUpper(line[0]) + line[1..]);
    }

    if (list.Count < 2)
    {
        list.Add("Dieren verzorgen");
    }

    if (list.Count < 2)
    {
        list.Add("Kijken naar dierenwelzijn");
    }

    return list.Take(4).ToList();
}

static int WordCount(string text)
    => text.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length;

static string ShortSentence(string text)
{
    var t = text.Trim().TrimEnd('.');
    if (t.Length == 0)
    {
        return "";
    }

    var words = t.Split(' ', StringSplitOptions.RemoveEmptyEntries);
    if (words.Length > 18)
    {
        t = string.Join(' ', words.Take(18));
    }

    return t.EndsWith('.') ? t : t + ".";
}

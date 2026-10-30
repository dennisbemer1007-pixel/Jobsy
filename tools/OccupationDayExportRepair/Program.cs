using System.Text.Json;
using Jobsy.Core.Careers;

var root = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));
var exportPath = Path.Combine(root, "docs/mockups/leerling/werelden/data/occupation-day-in-life.nl.json");
var manifestPath = Path.Combine(root, "docs/mockups/leerling/werelden/data/trial-20-manifest.json");

if (!File.Exists(exportPath) || !File.Exists(manifestPath))
{
    Console.Error.WriteLine("Export or manifest missing.");
    return 1;
}

var manifest = JsonSerializer.Deserialize<Manifest>(
    await File.ReadAllTextAsync(manifestPath),
    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
var doc = JsonSerializer.Deserialize<OccupationDayExportDocument>(
    await File.ReadAllTextAsync(exportPath),
    OccupationDayJson.Options);
if (manifest?.Occupations is null || doc?.Rows is null)
{
    Console.Error.WriteLine("Empty manifest or export.");
    return 1;
}

var trial = manifest.Occupations.Select(o => o.EscoId).ToHashSet(StringComparer.OrdinalIgnoreCase);
var byId = doc.Rows.ToDictionary(r => r.EscoId, StringComparer.OrdinalIgnoreCase);

foreach (var occ in manifest.Occupations)
{
    if (byId.ContainsKey(occ.EscoId))
    {
        continue;
    }

    var facts = OccupationDayFacts.For(occ.EscoId);
    var grounded = facts is null ? null : OccupationDayGroundedDraftBuilder.TryBuild(facts);
    if (grounded is null)
    {
        Console.Error.WriteLine("skip grounded " + occ.EscoId);
        continue;
    }

    var row = new OccupationDayExportRow
    {
        EscoId = occ.EscoId,
        Uri = occ.EscoId,
        TitleNl = facts!.TitleNl,
        Locale = "nl",
        SourceModel = "gpt-4o-grounded-bridge",
        GeneratedAtUtc = DateTime.UtcNow,
        ContentHash = "trial-bridge",
        ThinSource = facts.IsThin,
    };
    ApplyDraft(row, grounded);
    doc.Rows.Add(row);
    byId[occ.EscoId] = row;
    Console.WriteLine("grounded " + row.TitleNl);
}

var repaired = 0;
foreach (var row in doc.Rows)
{
    if (!trial.Contains(row.EscoId))
    {
        continue;
    }

    var facts = OccupationDayFacts.For(row.EscoId);
    if (facts is null)
    {
        continue;
    }

    var blocks = OccupationDayBlocks.Normalize(row.Blocks ?? []);
    if (blocks.Count == 0)
    {
        blocks = OccupationDayBlocks.FromLegacy(row.Morning, row.Midday, row.Afternoon, row.Closing).ToList();
    }

    blocks = blocks
        .Select(block => block with { Text = OccupationDayChildLanguage.SimplifyKnownJargon(block.Text) })
        .ToList();

    var draft = OccupationDayBlocks.WithDerived(new OccupationDayDraft(
        row.TitleNl,
        row.Morning ?? "",
        row.Midday ?? "",
        row.Afternoon ?? "",
        row.Closing ?? "",
        row.Highlights ?? [],
        row.VariesNote ?? "",
        blocks,
        row.Tasks ?? [],
        row.Skills ?? []));
    draft = OccupationDayDraftRepair.Apply(draft, facts);
    ApplyDraft(row, draft);
    repaired++;
    Console.WriteLine("repaired " + row.TitleNl);
}

static void ApplyDraft(OccupationDayExportRow row, OccupationDayDraft draft)
{
    row.Blocks = draft.Blocks?.ToList() ?? [];
    row.Morning = draft.Morning;
    row.Midday = draft.Midday;
    row.Afternoon = draft.Afternoon;
    row.Closing = draft.Closing;
    row.Skills = draft.Skills?.ToList() ?? [];
    row.Highlights = draft.Highlights.ToList();
    row.VariesNote = draft.VariesNote;
}

await File.WriteAllTextAsync(exportPath, JsonSerializer.Serialize(doc, OccupationDayJson.Options));
Console.WriteLine($"Done. {repaired} rows.");
return 0;

file sealed class Manifest
{
    public List<ManifestOcc> Occupations { get; set; } = [];
}

file sealed class ManifestOcc
{
    public string EscoId { get; set; } = "";
}

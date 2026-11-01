using System.Globalization;
using System.Reflection;
using System.Text;
using Jobsy.Core.Enums;

namespace Jobsy.Core.Scholen.MboHbo;

/// <summary>Loads Dennis-approved v2 CSV banks (embedded resources, UTF-8 BOM).</summary>
public sealed class PupilMboHboCatalog
{
    public const string ScoringVersion = "mbo-hbo-1";

    private static readonly Lazy<PupilMboHboCatalog> Instance = new(() => new PupilMboHboCatalog());

    private readonly IReadOnlyList<PupilMboHboQuestion> _vakkenMbo;
    private readonly IReadOnlyList<PupilMboHboQuestion> _vakkenHbo;
    private readonly IReadOnlyList<PupilMboHboQuestion> _interesse;
    private readonly IReadOnlyList<PupilMboHboQuestion> _competentie;
    private readonly IReadOnlyList<PupilMboHboQuestion> _beroepen;
    private readonly IReadOnlyList<PupilMboHboQuestion> _werkwaarden;

    private PupilMboHboCatalog()
    {
        _vakkenMbo = LoadBlock(PupilMboHboBlock.Vakken, "1a-vakkenpakket-mbo.csv", "v");
        _vakkenHbo = LoadBlock(PupilMboHboBlock.Vakken, "1b-vakkenpakket-hbo.csv", "v");
        _interesse = LoadBlock(PupilMboHboBlock.Interesse, "2-interessetest-riasec.csv", "i");
        _competentie = LoadBlock(PupilMboHboBlock.Competentie, "3-competentietest.csv", "c");
        _beroepen = LoadBlock(PupilMboHboBlock.Beroepen, "4-beroepentest.csv", "b");
        _werkwaarden = LoadBlock(PupilMboHboBlock.Werkwaarden, "5-werkwaarden-werkcultuur.csv", "w");
    }

    public static PupilMboHboCatalog Default => Instance.Value;

    public static IReadOnlyList<PupilMboHboBlock> AllBlocks { get; } =
    [
        PupilMboHboBlock.Vakken,
        PupilMboHboBlock.Interesse,
        PupilMboHboBlock.Competentie,
        PupilMboHboBlock.Beroepen,
        PupilMboHboBlock.Werkwaarden
    ];

    public IReadOnlyList<PupilMboHboQuestion> QuestionsFor(PupilMboHboBlock block, SchoolLevel level)
        => block switch
        {
            PupilMboHboBlock.Vakken => SchoolLevelRules.IsHbo(level) ? _vakkenHbo : _vakkenMbo,
            PupilMboHboBlock.Interesse => _interesse,
            PupilMboHboBlock.Competentie => _competentie,
            PupilMboHboBlock.Beroepen => _beroepen,
            PupilMboHboBlock.Werkwaarden => _werkwaarden,
            _ => throw new ArgumentOutOfRangeException(nameof(block))
        };

    public PupilMboHboQuestion? Find(PupilMboHboBlock block, SchoolLevel level, string id)
        => QuestionsFor(block, level).FirstOrDefault(q => string.Equals(q.Id, id, StringComparison.Ordinal));

    public int QuestionCount(PupilMboHboBlock block, SchoolLevel level)
        => QuestionsFor(block, level).Count;

    private static IReadOnlyList<PupilMboHboQuestion> LoadBlock(
        PupilMboHboBlock block,
        string fileName,
        string idPrefix)
    {
        var asm = typeof(PupilMboHboCatalog).Assembly;
        var resourceName = asm.GetManifestResourceNames()
            .FirstOrDefault(n => n.EndsWith(fileName, StringComparison.OrdinalIgnoreCase));
        if (resourceName is null)
        {
            throw new InvalidOperationException($"Embedded MBO/HBO CSV missing: {fileName}");
        }

        using var stream = asm.GetManifestResourceStream(resourceName)
                           ?? throw new InvalidOperationException($"Cannot open {resourceName}");
        using var reader = new StreamReader(stream, Encoding.UTF8);
        var lines = new List<string>();
        while (!reader.EndOfStream)
        {
            lines.Add(reader.ReadLine() ?? string.Empty);
        }

        if (lines.Count < 2)
        {
            return [];
        }

        var header = SplitCsvLine(lines[0]);
        var idx = header.ToDictionary(h => h.Trim(), h => header.IndexOf(h), StringComparer.OrdinalIgnoreCase);
        var list = new List<PupilMboHboQuestion>();
        foreach (var line in lines.Skip(1))
        {
            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            var cols = SplitCsvLine(line);
            string Col(string name)
            {
                if (!idx.TryGetValue(name, out var i) || i < 0 || i >= cols.Count)
                {
                    return string.Empty;
                }

                return cols[i].Trim();
            }

            if (!int.TryParse(Col("nr"), NumberStyles.Integer, CultureInfo.InvariantCulture, out var nr))
            {
                continue;
            }

            ParseEsco(Col("esco"), out var escoName, out var escoUri);
            var id = $"{idPrefix}{nr:D2}";
            list.Add(new PupilMboHboQuestion(
                block,
                nr,
                id,
                Col("dimensie"),
                Col("vraag"),
                Col("voorbeeldscenario"),
                escoName,
                escoUri,
                string.IsNullOrWhiteSpace(Col("esco_match")) ? null : Col("esco_match")));
        }

        return list;
    }

    private static void ParseEsco(string raw, out string? name, out string? uri)
    {
        name = null;
        uri = null;
        if (string.IsNullOrWhiteSpace(raw))
        {
            return;
        }

        var parts = raw.Split('|', StringSplitOptions.TrimEntries);
        if (parts.Length > 0)
        {
            name = parts[0];
        }

        foreach (var part in parts)
        {
            if (part.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
                || part.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            {
                uri = part;
                break;
            }
        }
    }

    /// <summary>Minimal CSV line split (semicolon, quoted fields).</summary>
    internal static List<string> SplitCsvLine(string line)
    {
        var result = new List<string>();
        var sb = new StringBuilder();
        var inQuotes = false;
        for (var i = 0; i < line.Length; i++)
        {
            var c = line[i];
            if (c == '"')
            {
                if (inQuotes && i + 1 < line.Length && line[i + 1] == '"')
                {
                    sb.Append('"');
                    i++;
                }
                else
                {
                    inQuotes = !inQuotes;
                }

                continue;
            }

            if (c == ';' && !inQuotes)
            {
                result.Add(sb.ToString());
                sb.Clear();
                continue;
            }

            sb.Append(c);
        }

        result.Add(sb.ToString());
        return result;
    }
}

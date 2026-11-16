using System.Globalization;
using System.Reflection;
using System.Text;
using Jobsy.Core.Enums;
using Jobsy.Core.Scholen.MboHbo;

namespace Jobsy.Core.WerkgeverApk;

/// <summary>Loads CONCEPT A/B banks (Basis ~40, Uitgebreid ~200).</summary>
public sealed class WerkgeverApkEmployeeAbCatalog
{
    public const string ScoringVersion = "werkgever-apk-ab-concept-1";

    private static readonly Lazy<Dictionary<WerkgeverApkPackageKind, WerkgeverApkEmployeeAbCatalog>> Cache = new(BuildAll);

    private readonly WerkgeverApkPackageKind _package;

    private WerkgeverApkEmployeeAbCatalog(WerkgeverApkPackageKind package, IReadOnlyList<WerkgeverApkAbItem> items)
    {
        _package = package;
        Items = items;
    }

    public WerkgeverApkPackageKind Package => _package;

    public IReadOnlyList<WerkgeverApkAbItem> Items { get; }

    public int QuestionCount => Items.Count;

    public int DisplayBlockCount => _package == WerkgeverApkPackageKind.Basis ? 3 : 6;

    public static WerkgeverApkEmployeeAbCatalog ForPackage(WerkgeverApkPackageKind package)
        => Cache.Value.TryGetValue(package, out var cat)
            ? cat
            : Cache.Value[WerkgeverApkPackageKind.Basis];

    public WerkgeverApkAbItem? Find(string id)
        => Items.FirstOrDefault(i => string.Equals(i.Id, id, StringComparison.OrdinalIgnoreCase));

    public IReadOnlyList<WerkgeverApkAbItem> ItemsInDisplayBlock(int displayBlock)
        => Items.Where(i => i.DisplayBlock == displayBlock).ToList();

    private static Dictionary<WerkgeverApkPackageKind, WerkgeverApkEmployeeAbCatalog> BuildAll()
    {
        return new Dictionary<WerkgeverApkPackageKind, WerkgeverApkEmployeeAbCatalog>
        {
            [WerkgeverApkPackageKind.Basis] = Load(WerkgeverApkPackageKind.Basis, "ab-basis-CONCEPT.csv", "b"),
            [WerkgeverApkPackageKind.Uitgebreid] = Load(WerkgeverApkPackageKind.Uitgebreid, "ab-uitgebreid-CONCEPT.csv", "u"),
        };
    }

    private static WerkgeverApkEmployeeAbCatalog Load(
        WerkgeverApkPackageKind package,
        string fileName,
        string idPrefix)
    {
        var asm = typeof(WerkgeverApkEmployeeAbCatalog).Assembly;
        var resourceName = asm.GetManifestResourceNames()
            .FirstOrDefault(n => n.EndsWith(fileName, StringComparison.OrdinalIgnoreCase))
            ?? throw new InvalidOperationException($"Missing embedded {fileName}");

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
            return new WerkgeverApkEmployeeAbCatalog(package, []);
        }

        var header = PupilMboHboCatalog.SplitCsvLine(lines[0]);
        var idx = header.ToDictionary(h => h.Trim(), h => header.IndexOf(h), StringComparer.OrdinalIgnoreCase);
        var list = new List<WerkgeverApkAbItem>();

        foreach (var line in lines.Skip(1))
        {
            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            var cols = PupilMboHboCatalog.SplitCsvLine(line);
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

            var blok = Col("blok");
            var block = ParseBlock(blok);
            var displayBlock = int.TryParse(Col("display_blok"), NumberStyles.Integer, CultureInfo.InvariantCulture, out var db)
                ? db
                : 1;
            var soort = Col("soort");
            var kind = string.Equals(soort, "check", StringComparison.OrdinalIgnoreCase)
                ? WerkgeverApkAbItemKind.Check
                : WerkgeverApkAbItemKind.Ab;

            list.Add(new WerkgeverApkAbItem(
                $"{idPrefix}{nr:D3}",
                nr,
                block,
                displayBlock,
                kind,
                Col("situatie"),
                Col("kop"),
                NullIfEmpty(Col("optie_a")),
                Col("dimensie_a"),
                NullIfEmpty(Col("optie_b")),
                NullIfEmpty(Col("dimensie_b"))));
        }

        return new WerkgeverApkEmployeeAbCatalog(package, list);
    }

    private static PupilMboHboBlock ParseBlock(string blok)
        => blok.ToLowerInvariant() switch
        {
            "interesse" => PupilMboHboBlock.Interesse,
            "competentie" => PupilMboHboBlock.Competentie,
            "werkwaarden" => PupilMboHboBlock.Werkwaarden,
            _ => PupilMboHboBlock.Competentie,
        };

    private static string? NullIfEmpty(string s) => string.IsNullOrWhiteSpace(s) ? null : s;
}

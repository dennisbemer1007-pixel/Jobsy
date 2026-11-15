using System.Globalization;
using System.Reflection;
using System.Text;
using Jobsy.Core.Enums;
using Jobsy.Core.Scholen.MboHbo;

namespace Jobsy.Core.WerkgeverApk;

/// <summary>Work-focused B1 question bank for Loopbaan-APK medewerkers (not pupil/MBO copy).</summary>
public sealed class WerkgeverApkEmployeeCatalog
{
    public const string ScoringVersion = "werkgever-apk-employee-1";

    private static readonly Lazy<WerkgeverApkEmployeeCatalog> Instance = new(() => new WerkgeverApkEmployeeCatalog());

    private readonly IReadOnlyList<PupilMboHboQuestion> _interesse;
    private readonly IReadOnlyList<PupilMboHboQuestion> _competentie;
    private readonly IReadOnlyList<PupilMboHboQuestion> _werkwaarden;

    private WerkgeverApkEmployeeCatalog()
    {
        _interesse = LoadBlock(PupilMboHboBlock.Interesse, "ea-interesse.csv", "i");
        _competentie = LoadBlock(PupilMboHboBlock.Competentie, "ea-competentie.csv", "c");
        _werkwaarden = LoadBlock(PupilMboHboBlock.Werkwaarden, "ea-werkwaarden.csv", "w");
    }

    public static WerkgeverApkEmployeeCatalog Default => Instance.Value;

    public IReadOnlyList<PupilMboHboQuestion> QuestionsFor(PupilMboHboBlock block, SchoolLevel level = SchoolLevel.Mbo3)
    {
        _ = level;
        return QuestionsFor(block);
    }

    public IReadOnlyList<PupilMboHboQuestion> QuestionsFor(PupilMboHboBlock block)
        => block switch
        {
            PupilMboHboBlock.Interesse => _interesse,
            PupilMboHboBlock.Competentie => _competentie,
            PupilMboHboBlock.Werkwaarden => _werkwaarden,
            _ => throw new ArgumentOutOfRangeException(nameof(block))
        };

    public PupilMboHboQuestion? Find(PupilMboHboBlock block, SchoolLevel level, string id)
        => QuestionsFor(block, level).FirstOrDefault(q => string.Equals(q.Id, id, StringComparison.Ordinal));

    public int QuestionCount(PupilMboHboBlock block, SchoolLevel level = SchoolLevel.Mbo3)
        => QuestionsFor(block, level).Count;

    private static IReadOnlyList<PupilMboHboQuestion> LoadBlock(
        PupilMboHboBlock block,
        string fileName,
        string idPrefix)
    {
        var asm = typeof(WerkgeverApkEmployeeCatalog).Assembly;
        var resourceName = asm.GetManifestResourceNames()
            .FirstOrDefault(n => n.EndsWith(fileName, StringComparison.OrdinalIgnoreCase));
        if (resourceName is null)
        {
            throw new InvalidOperationException($"Embedded Werkgever-APK CSV missing: {fileName}");
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

        var header = PupilMboHboCatalog.SplitCsvLine(lines[0]);
        var idx = header.ToDictionary(h => h.Trim(), h => header.IndexOf(h), StringComparer.OrdinalIgnoreCase);
        var list = new List<PupilMboHboQuestion>();
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

            var id = $"{idPrefix}{nr:D2}";
            list.Add(new PupilMboHboQuestion(
                block,
                nr,
                id,
                Col("dimensie"),
                Col("vraag"),
                Col("voorbeeldscenario"),
                null,
                null,
                null));
        }

        return list;
    }
}

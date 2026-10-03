using System.Globalization;
using System.Text;
using Jobsy.Core.Enums;
using Jobsy.Core.Scholen;
using Jobsy.Core.Scholen.QuestionSets;
using Jobsy.Web.Localization;

namespace Jobsy.Tests.Scholen;

/// <summary>
/// Keeps generated vragenbank docs in sync. Regenerate:
/// JOBSY_UPDATE_PUPIL_QBANK_DOC=1 dotnet test --filter FullyQualifiedName~PupilQuestionBankDocFreshness
/// </summary>
public class PupilQuestionBankDocFreshnessTests
{
    public const string G78RelativeDocPath = "docs/scholen/vragenbank-leerlingen.md";
    public const string VoRelativeDocPath = "docs/scholen/vragenbank-leerlingen-vo.md";

    [Fact]
    public void Vragenbank_docs_match_catalog_and_strings()
    {
        AssertDoc(G78RelativeDocPath, GenerateG78);
        AssertDoc(VoRelativeDocPath, GenerateVo);
    }

    private static void AssertDoc(string relative, Func<string> generate)
    {
        var generated = Normalize(generate());
        var root = FindRepoRoot();
        var path = Path.Combine(root, relative);
        var update = string.Equals(
            Environment.GetEnvironmentVariable("JOBSY_UPDATE_PUPIL_QBANK_DOC"),
            "1",
            StringComparison.Ordinal);

        if (update || !File.Exists(path))
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            File.WriteAllText(path, generated);
        }

        Assert.True(File.Exists(path), $"Missing {relative}");
        var onDisk = Normalize(File.ReadAllText(path));
        Assert.True(
            string.Equals(generated, onDisk, StringComparison.Ordinal),
            $"{relative} is outdated. Regenerate with:\n" +
            "JOBSY_UPDATE_PUPIL_QBANK_DOC=1 dotnet test Jobsy.Tests/Jobsy.Tests.csproj " +
            "--filter \"FullyQualifiedName~PupilQuestionBankDocFreshness\"");
    }

    public static string GenerateG78()
        => Generate(
            new PupilQuestionBank(),
            strings => UiStringsLeerlingVragen.MergeNl(strings),
            "# Vragenbank leerlingen groep 7/8 (review)",
            "`PupilQuestionBank` + `UiStringsLeerlingVragen`");

    public static string GenerateVo()
        => Generate(
            new PupilQuestionBankVo(),
            strings => UiStringsLeerlingVragenVo.MergeNl(strings),
            "# Vragenbank leerlingen VO (review)",
            "`PupilQuestionBankVo` + `UiStringsLeerlingVragenVo`");

    private static string Generate(
        PupilQuestionBankBase bank,
        Action<Dictionary<string, string>> merge,
        string title,
        string sources)
    {
        var strings = new Dictionary<string, string>(StringComparer.Ordinal);
        merge(strings);
        var sb = new StringBuilder();
        sb.AppendLine(title);
        sb.AppendLine();
        sb.AppendLine($"Gegenereerd uit {sources}. Wijzig de bronnen, niet dit bestand met de hand.");
        sb.AppendLine();
        sb.AppendLine("| # | Wereld | Model | Categorie | Omgekeerd | Vraag | Stel je voor… |");
        sb.AppendLine("|---|---|---|---|---|---|---|");
        var n = 1;
        foreach (var q in bank.Questions)
        {
            var text = Esc(strings.GetValueOrDefault(q.TextKey, ""));
            var example = Esc(strings.GetValueOrDefault(q.ExampleKey, ""));
            sb.AppendLine(
                $"| {n} | {PupilQuestionBankBase.WorldTitle(q.World)} | {ModelLabel(q.Model)} | {q.Category} | {(q.Reverse ? "ja" : "nee")} | {text} | {example} |");
            n++;
        }

        sb.AppendLine();
        var report = DutchReadability.Analyze(bank.Questions.SelectMany(q =>
            new[] { strings.GetValueOrDefault(q.TextKey, ""), strings.GetValueOrDefault(q.ExampleKey, "") }));
        sb.AppendLine(string.Create(
            CultureInfo.InvariantCulture,
            $"Readability: gemiddelde zinslengte **{report.AverageWordsPerSentence:0.00}** woorden; " +
            $"aandeel lange woorden (>3 lettergrepen) **{DutchReadability.FormatPercent(report.LongWordShare)}%**."));
        sb.AppendLine();
        return sb.ToString();
    }

    private static string ModelLabel(AssessmentKind kind) => kind switch
    {
        AssessmentKind.Competence => "Competentietest",
        AssessmentKind.Career => "Beroepentest",
        AssessmentKind.Values => "Waarden",
        AssessmentKind.Culture => "Cultuur",
        _ => kind.ToString()
    };

    private static string Esc(string s) => s.Replace("|", "\\|", StringComparison.Ordinal);

    private static string Normalize(string text)
        => text.Replace("\r\n", "\n", StringComparison.Ordinal).TrimEnd() + "\n";

    private static string FindRepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "Jobsy.sln")))
            {
                return dir.FullName;
            }

            dir = dir.Parent;
        }

        throw new InvalidOperationException("Jobsy.sln not found");
    }
}

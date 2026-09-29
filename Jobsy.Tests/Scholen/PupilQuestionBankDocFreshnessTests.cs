using System.Text;
using Jobsy.Core.Enums;
using Jobsy.Core.Scholen;
using Jobsy.Web.Localization;

namespace Jobsy.Tests.Scholen;

/// <summary>
/// Keeps <c>docs/scholen/vragenbank-leerlingen.md</c> in sync with the bank + strings.
/// Regenerate: JOBSY_UPDATE_PUPIL_QBANK_DOC=1 dotnet test --filter FullyQualifiedName~PupilQuestionBankDocFreshness
/// </summary>
public class PupilQuestionBankDocFreshnessTests
{
    public const string RelativeDocPath = "docs/scholen/vragenbank-leerlingen.md";

    [Fact]
    public void Vragenbank_doc_matches_catalog_and_strings()
    {
        var generated = Normalize(Generate());
        var root = FindRepoRoot();
        var path = Path.Combine(root, RelativeDocPath);
        var update = string.Equals(
            Environment.GetEnvironmentVariable("JOBSY_UPDATE_PUPIL_QBANK_DOC"),
            "1",
            StringComparison.Ordinal);

        if (update || !File.Exists(path))
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            File.WriteAllText(path, generated);
        }

        Assert.True(File.Exists(path), $"Missing {RelativeDocPath}");
        var onDisk = Normalize(File.ReadAllText(path));
        Assert.True(
            string.Equals(generated, onDisk, StringComparison.Ordinal),
            $"{RelativeDocPath} is outdated. Regenerate with:\n" +
            "JOBSY_UPDATE_PUPIL_QBANK_DOC=1 dotnet test Jobsy.Tests/Jobsy.Tests.csproj " +
            "--filter \"FullyQualifiedName~PupilQuestionBankDocFreshness\"");
    }

    public static string Generate()
    {
        var bank = new PupilQuestionBank();
        var strings = new Dictionary<string, string>(StringComparer.Ordinal);
        UiStringsLeerlingVragen.MergeNl(strings);
        var sb = new StringBuilder();
        sb.AppendLine("# Vragenbank leerlingen (review)");
        sb.AppendLine();
        sb.AppendLine("Gegenereerd uit `PupilQuestionBank` + `UiStringsLeerlingVragen`. Wijzig de bronnen, niet dit bestand met de hand.");
        sb.AppendLine();
        sb.AppendLine("| # | Wereld | Model | Categorie | Omgekeerd | Vraag | Stel je voor… |");
        sb.AppendLine("|---|---|---|---|---|---|---|");
        var n = 1;
        foreach (var q in bank.Questions)
        {
            var text = Esc(strings.GetValueOrDefault(q.TextKey, ""));
            var example = Esc(strings.GetValueOrDefault(q.ExampleKey, ""));
            sb.AppendLine(
                $"| {n} | {PupilQuestionBank.WorldTitle(q.World)} | {ModelLabel(q.Model)} | {q.Category} | {(q.Reverse ? "ja" : "nee")} | {text} | {example} |");
            n++;
        }

        sb.AppendLine();
        var report = DutchReadability.Analyze(bank.Questions.SelectMany(q =>
            new[] { strings.GetValueOrDefault(q.TextKey, ""), strings.GetValueOrDefault(q.ExampleKey, "") }));
        sb.AppendLine(
            $"Readability: gemiddelde zinslengte **{report.AverageWordsPerSentence:0.00}** woorden; " +
            $"aandeel lange woorden (>3 lettergrepen) **{DutchReadability.FormatPercent(report.LongWordShare)}%**.");
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

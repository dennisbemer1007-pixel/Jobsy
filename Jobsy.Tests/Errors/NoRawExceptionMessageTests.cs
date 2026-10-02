using System.Text;
using System.Text.RegularExpressions;

namespace Jobsy.Tests.Errors;

/// <summary>
/// Ratchet for E7: a visitor never reads <c>ex.Message</c>. Counting is per file and may only go
/// down. A file that is not in the baseline must be at 0, so new code cannot add one.
/// Regenerate with <c>JOBSY_UPDATE_EXMESSAGE_BASELINE=1 dotnet test --filter ExMessage</c> — and
/// only ever to record a decrease.
/// </summary>
public class NoRawExceptionMessageTests
{
    public const string BaselineEnvVar = "JOBSY_UPDATE_EXMESSAGE_BASELINE";

    private const string BaselineRelativePath = "docs/errors/ex-message-baseline.txt";

    /// <summary>The helper that is allowed to touch the message: it logs it, never shows it.</summary>
    private static readonly string[] ExemptFiles =
    [
        "Jobsy.Web/Services/UserFacingError.cs"
    ];

    /// <summary>
    /// Pages and layouts that must stay at 0: every error/status page, the public pages, the
    /// chrome around every page and everything a visitor can reach without signing in.
    /// </summary>
    private static readonly string[] MustBeZeroPrefixes =
    [
        "Jobsy.Web/Components/Errors/",
        "Jobsy.Web/Components/Layout/",
        "Jobsy.Web/Components/Pages/Status/",
        "Jobsy.Web/Components/Pages/Public/",
        "Jobsy.Web/Hosting/",
        "Jobsy.Web/Localization/"
    ];

    private static readonly string[] MustBeZeroFiles =
    [
        "Jobsy.Web/Components/App.razor",
        "Jobsy.Web/Components/AccessDeniedView.razor",
        "Jobsy.Web/Components/Pages/Error.razor",
        "Jobsy.Web/Components/Pages/CompanyPublicPage.razor",
        "Jobsy.Web/Components/Pages/VacancyDetail.razor",
        "Jobsy.Web/Components/Pages/Partner/PartnerSales.razor",
        "Jobsy.Web/Components/Pages/RegisterToegang.razor",
        "Jobsy.Web/Components/Pages/Sales/RecommendObject.razor",
        "Jobsy.Web/Components/Pages/Legal/PrivacyData.razor",
        "Jobsy.Web/Components/Pages/Candidate/HowLobsyWorks.razor"
    ];

    private static readonly Regex MessagePattern = new(
        @"\b(?:ex|e|exception|exc)\.Message\b|\.InnerException\.Message\b",
        RegexOptions.Compiled);

    /// <summary>Logging the message is fine — only showing it is not.</summary>
    private static readonly Regex LoggingCall = new(
        @"\b(?:Log(?:Error|Warning|Information|Debug|Trace|Critical)|Logger\.Log|logger\.Log)\s*\(",
        RegexOptions.Compiled);

    [Fact]
    public void ExMessage_count_never_grows_per_file()
    {
        var root = RepoRoot();
        var counts = CountAll(root);

        if (Environment.GetEnvironmentVariable(BaselineEnvVar) == "1")
        {
            WriteBaseline(root, counts);
        }

        var baseline = ReadBaseline(root);
        var problems = new List<string>();

        foreach (var (path, count) in counts.OrderBy(c => c.Key, StringComparer.Ordinal))
        {
            var allowed = baseline.TryGetValue(path, out var fromBaseline) ? fromBaseline : 0;
            if (count > allowed)
            {
                problems.Add($"{path}: {count} > {allowed} allowed");
            }
        }

        Assert.True(
            problems.Count == 0,
            "New ex.Message in user-facing code (use UserFacingError instead):"
            + Environment.NewLine
            + string.Join(Environment.NewLine, problems)
            + Environment.NewLine
            + $"If this is a decrease, regenerate with {BaselineEnvVar}=1.");
    }

    [Fact]
    public void ExMessage_is_zero_on_error_public_pages_and_layouts()
    {
        var root = RepoRoot();
        var counts = CountAll(root);

        var offenders = counts
            .Where(entry =>
                MustBeZeroFiles.Contains(entry.Key, StringComparer.Ordinal)
                || MustBeZeroPrefixes.Any(prefix => entry.Key.StartsWith(prefix, StringComparison.Ordinal)))
            .Select(entry => $"{entry.Key}: {entry.Value}")
            .OrderBy(line => line, StringComparer.Ordinal)
            .ToList();

        Assert.True(
            offenders.Count == 0,
            "These must stay at 0 (E7):" + Environment.NewLine + string.Join(Environment.NewLine, offenders));
    }

    [Fact]
    public void ExMessage_baseline_file_explains_how_to_regenerate()
    {
        var text = File.ReadAllText(Path.Combine(RepoRoot(), BaselineRelativePath));
        Assert.Contains(BaselineEnvVar, text, StringComparison.Ordinal);
        Assert.Contains("UserFacingError", text, StringComparison.Ordinal);
    }

    internal static Dictionary<string, int> CountAll(string root)
    {
        var results = new Dictionary<string, int>(StringComparer.Ordinal);
        var web = Path.Combine(root, "Jobsy.Web");

        foreach (var file in Directory.EnumerateFiles(web, "*.*", SearchOption.AllDirectories))
        {
            var extension = Path.GetExtension(file);
            if (extension is not (".cs" or ".razor"))
            {
                continue;
            }

            var relative = Relative(root, file);
            if (relative.Contains("/obj/", StringComparison.Ordinal)
                || relative.Contains("/bin/", StringComparison.Ordinal)
                || ExemptFiles.Contains(relative, StringComparer.Ordinal))
            {
                continue;
            }

            var count = Count(File.ReadAllText(file));
            if (count > 0)
            {
                results[relative] = count;
            }
        }

        return results;
    }

    /// <summary>Matches outside logging calls and outside comment lines.</summary>
    internal static int Count(string text)
    {
        var count = 0;
        foreach (var match in MessagePattern.Matches(text).Cast<Match>())
        {
            var statement = text[StatementStart(text, match.Index)..match.Index];
            if (LoggingCall.IsMatch(statement))
            {
                continue;
            }

            if (IsCommentLine(text, match.Index))
            {
                continue;
            }

            count++;
        }

        return count;
    }

    /// <summary>Start of the statement the match sits in, so multi-line log calls are recognised.</summary>
    private static int StatementStart(string text, int index)
    {
        for (var i = index; i > 0; i--)
        {
            if (text[i - 1] is ';' or '{' or '}')
            {
                return i;
            }
        }

        return 0;
    }

    private static bool IsCommentLine(string text, int index)
    {
        var lineStart = text.LastIndexOf('\n', Math.Max(0, index - 1)) + 1;
        var prefix = text[lineStart..index].TrimStart();
        return prefix.StartsWith("//", StringComparison.Ordinal)
               || prefix.StartsWith('*');
    }

    private static Dictionary<string, int> ReadBaseline(string root)
    {
        var result = new Dictionary<string, int>(StringComparer.Ordinal);
        foreach (var line in File.ReadAllLines(Path.Combine(root, BaselineRelativePath)))
        {
            if (line.Length == 0 || line[0] == '#')
            {
                continue;
            }

            var parts = line.Split('\t', 2, StringSplitOptions.TrimEntries);
            if (parts.Length == 2 && int.TryParse(parts[0], out var count))
            {
                result[parts[1]] = count;
            }
        }

        return result;
    }

    private static void WriteBaseline(string root, Dictionary<string, int> counts)
    {
        var sb = new StringBuilder();
        sb.AppendLine("# ex.Message ratchet baseline (errors 04, E7).");
        sb.AppendLine("# One line per file: <count><TAB><path>. Counts may only go down.");
        sb.AppendLine("# Replace the use with UserFacingError (Jobsy.Web/Services/UserFacingError.cs),");
        sb.AppendLine("# then regenerate: JOBSY_UPDATE_EXMESSAGE_BASELINE=1 dotnet test --filter ExMessage");
        sb.AppendLine("# A file missing from this list must be at 0. This list is also the to-do list.");
        sb.AppendLine(
            "# Total: "
            + counts.Values.Sum().ToString(System.Globalization.CultureInfo.InvariantCulture)
            + " in "
            + counts.Count.ToString(System.Globalization.CultureInfo.InvariantCulture)
            + " files.");

        foreach (var (path, count) in counts.OrderBy(entry => entry.Key, StringComparer.Ordinal))
        {
            sb.Append(count.ToString(System.Globalization.CultureInfo.InvariantCulture))
                .Append('\t')
                .AppendLine(path);
        }

        var target = Path.Combine(root, BaselineRelativePath);
        Directory.CreateDirectory(Path.GetDirectoryName(target)!);
        File.WriteAllText(target, sb.ToString());
    }

    private static string Relative(string root, string file)
        => Path.GetRelativePath(root, file).Replace('\\', '/');

    private static string RepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "Jobsy.sln")))
        {
            dir = dir.Parent;
        }

        return dir?.FullName ?? throw new InvalidOperationException("Jobsy.sln not found");
    }
}

using System.Diagnostics;
using System.Xml.Linq;
using Xunit;

namespace Jobsy.Tests;

/// <summary>
/// Ensures PR unit-test failures are a subset of acceptatie baseline (same catalog, same base sha policy).
/// Set JOBSY_ACCEPTATIE_TRX and JOBSY_PR_TRX to TRX paths (e.g. from GitHub Actions artifacts).
/// </summary>
public sealed class AcceptatiePrFailureSubsetTests
{
    private static readonly XNamespace Tns = "http://microsoft.com/schemas/VisualStudio/TeamTest/2010";

    [Fact]
    public void Pr_failures_are_subset_of_acceptatie_when_trx_paths_configured()
    {
        var accPath = Environment.GetEnvironmentVariable("JOBSY_ACCEPTATIE_TRX");
        var prPath = Environment.GetEnvironmentVariable("JOBSY_PR_TRX");
        if (string.IsNullOrWhiteSpace(accPath) || string.IsNullOrWhiteSpace(prPath))
        {
            // CI: workflow should set both on merge gates; local runs skip unless TRX exported.
            return;
        }

        Assert.True(File.Exists(accPath), $"Missing acceptatie TRX: {accPath}");
        Assert.True(File.Exists(prPath), $"Missing PR TRX: {prPath}");

        var accFailed = FailedTestNames(accPath);
        var prFailed = FailedTestNames(prPath);
        var accCatalog = DefinedTestNames(accPath);

        var newOnPr = prFailed.Except(accFailed).Intersect(accCatalog).OrderBy(n => n, StringComparer.Ordinal).ToList();
        Assert.True(
            newOnPr.Count == 0,
            "New failures on PR vs acceptatie baseline (same catalog):\n" + string.Join("\n", newOnPr.Select(n => " + " + n)));
    }

    [Fact]
    public void Compare_script_matches_inline_subset_check()
    {
        var accPath = Environment.GetEnvironmentVariable("JOBSY_ACCEPTATIE_TRX");
        var prPath = Environment.GetEnvironmentVariable("JOBSY_PR_TRX");
        if (string.IsNullOrWhiteSpace(accPath) || string.IsNullOrWhiteSpace(prPath))
        {
            return;
        }

        var script = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "compare-failures-with-acceptatie.py"));
        if (!File.Exists(script))
        {
            script = Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), "compare-failures-with-acceptatie.py"));
        }

        Assert.True(File.Exists(script), $"Missing {script}");
        using var proc = Process.Start(new ProcessStartInfo
        {
            FileName = "python3",
            ArgumentList = { script, prPath, accPath },
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
        });
        Assert.NotNull(proc);
        var stdout = proc.StandardOutput.ReadToEnd();
        var stderr = proc.StandardError.ReadToEnd();
        proc.WaitForExit(60_000);
        Assert.True(proc.ExitCode == 0, $"compare-failures failed (exit {proc.ExitCode}): {stderr}\n{stdout}");
    }

    private static HashSet<string> FailedTestNames(string trxPath)
    {
        var doc = XDocument.Load(trxPath);
        return doc.Descendants(Tns + "UnitTestResult")
            .Where(r => string.Equals(r.Attribute("outcome")?.Value, "Failed", StringComparison.OrdinalIgnoreCase))
            .Select(r => r.Attribute("testName")?.Value ?? "")
            .Where(n => n.Length > 0)
            .ToHashSet(StringComparer.Ordinal);
    }

    private static HashSet<string> DefinedTestNames(string trxPath)
    {
        var doc = XDocument.Load(trxPath);
        return doc.Descendants(Tns + "UnitTest")
            .Select(u => u.Attribute("name")?.Value ?? "")
            .Where(n => n.Length > 0)
            .ToHashSet(StringComparer.Ordinal);
    }
}

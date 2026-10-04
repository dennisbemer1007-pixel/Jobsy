using System.Text.RegularExpressions;
using Jobsy.Core.Email;
using Jobsy.Core.Legal;

namespace Jobsy.Tests;

public class LegalProcessorCatalogTests
{
    private const string FingerprintAt20261004 = "8c50d3463cae283f8316cd4400525152f3e4cd0e8e9847dabb80e72a8c5ea1da";

    [Fact]
    public void Changing_the_processor_list_requires_a_new_privacy_version()
    {
        var actual = LegalProcessorFingerprint.Compute(LegalProcessors.All);
        var snapshots = LegalDocumentVersions.PrivacyCatalogSnapshots;
        var current = LegalDocumentVersions.Privacy.Version;

        Assert.Equal(current, snapshots[^1].Version);
        Assert.Equal(snapshots[^1].Fingerprint, actual);
        Assert.Equal("2026-10-04", snapshots[0].Version);
        Assert.Equal(FingerprintAt20261004, snapshots[0].Fingerprint);
        Assert.NotEqual(FingerprintAt20261004, actual);
        Assert.NotEqual("2026-10-04", current);
        Assert.Equal(snapshots.Count, snapshots.Select(snapshot => snapshot.Version).Distinct(StringComparer.Ordinal).Count());
        Assert.Equal(snapshots.Count, snapshots.Select(snapshot => snapshot.Fingerprint).Distinct(StringComparer.Ordinal).Count());
        Assert.Contains(LegalDocumentVersions.PrivacyHistory, entry => entry.Version == current);
    }

    [Fact]
    public void Every_processor_has_a_company_region_and_a_data_region()
    {
        Assert.All(LegalProcessors.All, processor =>
        {
            Assert.True(Enum.IsDefined(processor.CompanyHq), processor.Id);
            Assert.True(Enum.IsDefined(processor.DataRegion), processor.Id);
            Assert.Contains(ProcessorRegionText.Dutch(processor.CompanyHq), processor.Region, StringComparison.Ordinal);
            Assert.Contains(ProcessorRegionText.Dutch(processor.DataRegion), processor.Region, StringComparison.Ordinal);
        });
    }

    [Fact]
    public void Resend_drops_out_when_lettermint_is_the_active_mail_provider()
    {
        var rows = LegalProcessorSelection.Resolve("lettermint", lettermintApiKeyConfigured: true);

        Assert.Contains(rows, row => row.Id == "lettermint");
        Assert.DoesNotContain(rows, row => row.Id == "resend");
        Assert.DoesNotContain("Resend", DataLocationText.AmericanCompanyNames(rows));
        Assert.Contains("Lettermint", rows.Select(row => row.Name));
        Assert.False(LegalProcessors.ById("lettermint").IsAmericanCompany);
    }

    [Fact]
    public void Missing_lettermint_key_keeps_resend()
    {
        var rows = LegalProcessorSelection.Resolve(MailProviderNames.Lettermint, lettermintApiKeyConfigured: false);

        Assert.Contains(rows, row => row.Id == "resend");
        Assert.DoesNotContain(rows, row => row.Id == "lettermint");
    }

    [Theory]
    [InlineData("nl", "Render, Cloudflare en Resend")]
    [InlineData("en", "Render, Cloudflare and Resend")]
    [InlineData("ar", "Render، Cloudflare و Resend")]
    public void Names_are_joined_in_the_reader_language(string language, string expected)
        => Assert.Equal(expected, DataLocationText.JoinNames(["Render", "Cloudflare", "Resend"], language));
}

public class ThirdPartyProcessorGuardTests
{
    private static readonly HashSet<string> SkipDirectories = new(StringComparer.OrdinalIgnoreCase)
    {
        "bin", "obj", "node_modules", ".git", "artifacts", "TestResults", ".cursor"
    };

    private static readonly string[] ScannedExtensions = [".cs", ".csproj", ".json", ".yml", ".yaml", ".razor", ".html"];

    [Fact]
    public void A_watched_host_without_a_processor_is_a_finding()
    {
        var ids = LegalThirdPartyWatch.ProcessorIdsForHost("api.scaleway.com").ToList();
        Assert.Equal("scaleway", Assert.Single(ids));
        Assert.DoesNotContain(LegalProcessors.All, processor => processor.Id == "scaleway");
    }

    [Fact]
    public void Package_segments_map_sentry_without_matching_unrelated_names()
    {
        Assert.Equal("sentry", Assert.Single(LegalThirdPartyWatch.ProcessorIdsForPackage("Sentry.AspNetCore")));
        Assert.Empty(LegalThirdPartyWatch.ProcessorIdsForPackage("Microsoft.AspNetCore.Authentication.OpenIdConnect"));
    }

    [Fact]
    public void Code_and_packages_only_reference_processors_that_exist()
    {
        var root = FindRepoRoot();
        var known = LegalProcessors.All.Select(processor => processor.Id).ToHashSet(StringComparer.Ordinal);
        var findings = new List<string>();
        foreach (var file in Directory.EnumerateFiles(root, "*", SearchOption.AllDirectories))
        {
            if (Skip(file))
            {
                continue;
            }

            var extension = Path.GetExtension(file);
            if (!ScannedExtensions.Contains(extension, StringComparer.OrdinalIgnoreCase))
            {
                continue;
            }

            var info = new FileInfo(file);
            if (info.Length > 2_000_000)
            {
                continue;
            }

            var text = File.ReadAllText(file);
            var relative = Path.GetRelativePath(root, file);
            if (extension.Equals(".csproj", StringComparison.OrdinalIgnoreCase))
            {
                foreach (Match match in Regex.Matches(text, "Include=\"([^\"]+)\""))
                {
                    Note(findings, known, relative, "package " + match.Groups[1].Value,
                        LegalThirdPartyWatch.ProcessorIdsForPackage(match.Groups[1].Value));
                }
            }

            foreach (Match match in Regex.Matches(text, @"https?://([A-Za-z0-9._\-]+)"))
            {
                Note(findings, known, relative, "host " + match.Groups[1].Value,
                    LegalThirdPartyWatch.ProcessorIdsForHost(match.Groups[1].Value));
            }
        }

        Assert.True(
            findings.Count == 0,
            "A third-party host or package has no LegalProcessors row:" + Environment.NewLine
            + string.Join(Environment.NewLine, findings.Distinct(StringComparer.Ordinal)));
    }

    private static void Note(
        List<string> findings,
        HashSet<string> known,
        string relative,
        string reference,
        IEnumerable<string> processorIds)
    {
        foreach (var processorId in processorIds)
        {
            if (!known.Contains(processorId))
            {
                findings.Add($"{relative}: {reference} → {processorId}");
            }
        }
    }

    private static bool Skip(string file)
    {
        var parts = file.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        return parts.Any(part => SkipDirectories.Contains(part));
    }

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

        throw new InvalidOperationException("Jobsy.sln not found.");
    }
}

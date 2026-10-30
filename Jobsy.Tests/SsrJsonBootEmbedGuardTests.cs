namespace Jobsy.Tests;

/// <summary>Boot JSON in SSR must use <see cref="Jobsy.Web.Hosting.JsonScriptEmbed"/> (not HtmlEncode), same as Match/r7.</summary>
public sealed class SsrJsonBootEmbedGuardTests
{
    [Fact]
    public void Uwv_and_match_ssr_shells_use_json_script_embed_for_boot_payloads()
    {
        var root = TestRepo.FindRoot();
        var web = Path.Combine(root, "Jobsy.Web");
        var offenders = new List<string>();
        foreach (var file in Directory.EnumerateFiles(web, "*SsrShells.cs", SearchOption.AllDirectories))
        {
            var text = File.ReadAllText(file);
            if (!text.Contains("application/json", StringComparison.Ordinal))
            {
                continue;
            }

            if (text.Contains("HtmlEncode(boot)", StringComparison.Ordinal)
                || text.Contains("HtmlEncode(JsonSerializer.Serialize", StringComparison.Ordinal))
            {
                offenders.Add(Path.GetRelativePath(root, file));
            }
        }

        Assert.True(offenders.Count == 0, "Use JsonScriptEmbed.Append for boot JSON: " + string.Join(", ", offenders));
    }

    [Fact]
    public void Adviseur_and_ondernemer_shells_call_json_script_embed()
    {
        var root = TestRepo.FindRoot();
        var adviseur = File.ReadAllText(Path.Combine(root, "Jobsy.Web", "UwvMockups", "AdviseurWerkSsrShells.cs"));
        var ondernemer = File.ReadAllText(Path.Combine(root, "Jobsy.Web", "UwvMockups", "OndernemersCheckSsrShells.cs"));
        Assert.Contains("JsonScriptEmbed.Append(sb, \"uwv-adviseur-boot\"", adviseur, StringComparison.Ordinal);
        Assert.Contains("JsonScriptEmbed.Append(sb, \"uwv-ondernemer-boot\"", ondernemer, StringComparison.Ordinal);
    }
}

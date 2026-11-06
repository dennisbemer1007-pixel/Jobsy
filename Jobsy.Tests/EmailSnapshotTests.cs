using AngleSharp.Html.Parser;
using Jobsy.Core.Email;
using Jobsy.Core.Email.Model;
using System.Text;

namespace Jobsy.Tests;

public class EmailSnapshotTests
{
    private static bool UpdateSnapshots =>
        string.Equals(Environment.GetEnvironmentVariable("JOBSY_UPDATE_EMAIL_SNAPSHOTS"), "1", StringComparison.Ordinal);

    [Fact]
    public async Task Structural_snapshots_for_every_registry_key()
    {
        var root = FindRepoRoot();
        var dir = Path.Combine(root, "Jobsy.Tests", "EmailSnapshots");
        Directory.CreateDirectory(dir);
        var ctx = EmailSampleContext.ForPreview("https://lobsy.nl");
        var missing = new List<string>();
        var mismatches = new List<string>();

        foreach (var def in EmailTemplateRegistry.All)
        {
            foreach (var lang in new[] { "nl", "en", "ar" })
            {
                var mail = TransactionalEmails.Compose(def.Key, ctx, EmailCulture.ForLanguage(lang));
                var snapshot = await BuildSnapshotAsync(mail);
                var path = Path.Combine(dir, $"{def.Key}.{lang}.txt");
                if (UpdateSnapshots || !File.Exists(path))
                {
                    await File.WriteAllTextAsync(path, snapshot);
                    if (!UpdateSnapshots)
                    {
                        missing.Add($"{def.Key}.{lang}");
                    }

                    continue;
                }

                var existing = await File.ReadAllTextAsync(path);
                if (!string.Equals(Normalize(existing), Normalize(snapshot), StringComparison.Ordinal))
                {
                    mismatches.Add($"{def.Key}.{lang}");
                }
            }
        }

        if (UpdateSnapshots)
        {
            return;
        }

        Assert.True(missing.Count == 0, "Missing snapshots (regenerate with JOBSY_UPDATE_EMAIL_SNAPSHOTS=1): " + string.Join(", ", missing));
        Assert.True(mismatches.Count == 0, "Snapshot drift (regenerate with JOBSY_UPDATE_EMAIL_SNAPSHOTS=1): " + string.Join(", ", mismatches));
    }

    private static async Task<string> BuildSnapshotAsync(ComposedEmail mail)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"KEY: {mail.Key}");
        sb.AppendLine($"SUBJECT: {mail.Subject}");
        sb.AppendLine($"PREHEADER: {mail.Preheader}");
        sb.AppendLine($"KIND: {mail.Kind}");
        sb.AppendLine("--- TEXT ---");
        sb.AppendLine(mail.Text.TrimEnd());
        sb.AppendLine("--- STRUCTURE ---");

        var parser = new HtmlParser();
        var doc = await parser.ParseDocumentAsync(mail.Html);
        foreach (var h1 in doc.QuerySelectorAll("h1"))
        {
            sb.AppendLine($"h1: {Collapse(h1.TextContent)}");
        }

        foreach (var el in doc.QuerySelectorAll("[data-lobsy-layout],[data-lobsy-cta],[data-lobsy-block],[data-lobsy-otp]"))
        {
            var markers = string.Join(" ", el.Attributes
                .Where(a => a.Name.StartsWith("data-lobsy-", StringComparison.Ordinal))
                .Select(a => $"{a.Name}={a.Value}"));
            sb.AppendLine($"marker: {el.TagName.ToLowerInvariant()} {markers}");
        }

        foreach (var a in doc.QuerySelectorAll("a[href]"))
        {
            sb.AppendLine($"a: {Collapse(a.TextContent)} -> {a.GetAttribute("href")}");
        }

        foreach (var img in doc.QuerySelectorAll("img"))
        {
            sb.AppendLine(
                $"img: src={img.GetAttribute("src")} alt={img.GetAttribute("alt")} width={img.GetAttribute("width")} height={img.GetAttribute("height")}");
        }

        // Ensure no style attributes leaked into the snapshot file itself beyond intentional omission.
        var snapshot = sb.ToString();
        Assert.DoesNotContain("style=", snapshot);
        return snapshot;
    }

    private static string Collapse(string? value)
        => string.Join(' ', (value ?? string.Empty).Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));

    private static string Normalize(string value)
    {
        var normalized = value.Replace("\r\n", "\n", StringComparison.Ordinal);
        // Invite / takeover sample composers still stamp "today+7" into notes — collapse for stability.
        normalized = System.Text.RegularExpressions.Regex.Replace(
            normalized,
            @"\d{1,2} (januari|februari|maart|april|mei|juni|juli|augustus|september|oktober|november|december|January|February|March|April|May|June|July|August|September|October|November|December|يناير|فبراير|مارس|أبريل|مايو|يونيو|يوليو|أغسطس|سبتمبر|أكتوبر|نوفمبر|ديسمبر) \d{4}(?:, \d{2}:\d{2})?",
            "<DATE>",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        return normalized.TrimEnd() + "\n";
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

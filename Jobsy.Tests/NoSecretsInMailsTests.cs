using Jobsy.Core.Email;
using Jobsy.Core.Time;

namespace Jobsy.Tests;

public class NoSecretsInMailsTests
{
    [Fact]
    public void Catalog_samples_have_no_password_box_or_api_key()
    {
        var ctx = EmailSampleContext.ForPreview("https://lobsy.nl");
        foreach (var template in TransactionalEmails.Templates)
        {
            var mail = TransactionalEmails.Compose(template.Key, ctx);
            Assert.DoesNotContain("tijdelijke wachtwoord", mail.Html, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("eenmalige tijdelijke", mail.Html, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("lobsy_test_", mail.Html, StringComparison.OrdinalIgnoreCase);
        }
    }

    [Fact]
    public void Source_has_no_GenerateTemporaryPassword_or_TemporaryPassword_in_hotspots()
    {
        var roots = new[]
        {
            Path.Combine(FindRepoRoot(), "Jobsy.Api"),
            Path.Combine(FindRepoRoot(), "Jobsy.Core", "Interfaces"),
            Path.Combine(FindRepoRoot(), "Jobsy.Web"),
            Path.Combine(FindRepoRoot(), "Jobsy.Infrastructure", "Services")
        };

        foreach (var root in roots)
        {
            foreach (var file in Directory.EnumerateFiles(root, "*.cs", SearchOption.AllDirectories)
                         .Concat(Directory.EnumerateFiles(root, "*.razor", SearchOption.AllDirectories)))
            {
                if (file.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
                    || file.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
                {
                    continue;
                }

                var text = File.ReadAllText(file);
                Assert.DoesNotContain("GenerateTemporaryPassword", text);
                Assert.DoesNotContain("TemporaryPassword", text);
            }
        }
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

        throw new InvalidOperationException("Repo root not found.");
    }
}

public class SupportAccessMailTests
{
    [Fact]
    public void Reason_is_escaped_and_expiry_uses_amsterdam()
    {
        var utc = new DateTime(2026, 7, 1, 12, 0, 0, DateTimeKind.Utc); // summer → 14:00
        var mail = TransactionalEmails.SupportAccessRequested(
            "https://lobsy.nl",
            "a***@lobsy.nl",
            "<img src=x onerror=alert(1)>",
            utc,
            "PersonalData");

        Assert.Equal("SupportAccessRequested", mail.Category);
        Assert.Contains("&lt;img src=x onerror=alert(1)&gt;", mail.Html);
        Assert.DoesNotContain("<img src=x", mail.Html);
        Assert.Contains(AmsterdamTime.FormatDateTime(utc), mail.Html);
        Assert.Contains("14:00", mail.Html);
    }
}

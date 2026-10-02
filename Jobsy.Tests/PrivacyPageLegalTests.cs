using Jobsy.Web.Services;

namespace Jobsy.Tests;

public class PrivacyPageLegalTests
{
    [Fact]
    public void Privacy_razor_has_no_placeholders_or_empty_mailto()
    {
        var root = FindRepoRoot();
        var page = File.ReadAllText(Path.Combine(root, "Jobsy.Web", "Components", "Pages", "Legal", "Privacy.razor"));
        Assert.DoesNotContain(string.Concat("[", "BEDRIJFS", "NAAM]"), page, StringComparison.Ordinal);
        Assert.DoesNotContain(string.Concat("[", "KVK-", "NUMMER]"), page, StringComparison.Ordinal);
        Assert.DoesNotContain(string.Concat("[", "AD", "RES]"), page, StringComparison.Ordinal);
        Assert.DoesNotContain(string.Concat("[", "CONTACT", " E-MAIL ", "PRIVACY]"), page, StringComparison.Ordinal);
        Assert.DoesNotContain(string.Concat("Platform", "Legal", "Identity"), page, StringComparison.Ordinal);
        Assert.DoesNotContain("mailto:\"", page, StringComparison.Ordinal);
        Assert.DoesNotContain("mailto:@\"\"", page, StringComparison.Ordinal);
        Assert.Contains("LegalIdentityProvider", page, StringComparison.Ordinal);
        Assert.Contains("!string.IsNullOrWhiteSpace(_privacyContact)", page, StringComparison.Ordinal);
    }

    [Fact]
    public void Empty_identity_line_omits_kvk_label_and_has_no_placeholders()
    {
        var empty = LegalIdentityDto.Empty;
        var parts = new List<string> { empty.DisplayName };
        if (!string.IsNullOrWhiteSpace(empty.KvkNumber))
        {
            parts.Add("KvK " + empty.KvkNumber);
        }

        if (!string.IsNullOrWhiteSpace(empty.AddressLine))
        {
            parts.Add(empty.AddressLine);
        }

        var line = string.Join(", ", parts) + ".";
        Assert.DoesNotContain("KvK", line, StringComparison.Ordinal);
        Assert.DoesNotContain("[", line, StringComparison.Ordinal);
        Assert.Equal("Lobsy.", line);
        Assert.Equal("support@lobsy.nl", empty.PrivacyContact);
    }

    [Fact]
    public void Identity_with_values_shows_kvk_and_address()
    {
        var dto = new LegalIdentityDto
        {
            Name = "Voorbeeld BV",
            TradeName = "Lobsy",
            Street = "Straat 1",
            PostalCode = "1234 AB",
            City = "Delft",
            KvkNumber = "12345678",
            SupportEmail = "support@lobsy.nl",
            PrivacyEmail = "privacy@lobsy.nl"
        };
        var parts = new List<string> { dto.DisplayName };
        if (!string.IsNullOrWhiteSpace(dto.KvkNumber))
        {
            parts.Add("KvK " + dto.KvkNumber);
        }

        if (!string.IsNullOrWhiteSpace(dto.AddressLine))
        {
            parts.Add(dto.AddressLine!);
        }

        var line = string.Join(", ", parts) + ".";
        Assert.Contains("Voorbeeld BV", line, StringComparison.Ordinal);
        Assert.Contains("KvK 12345678", line, StringComparison.Ordinal);
        Assert.Contains("Straat 1", line, StringComparison.Ordinal);
        Assert.Equal("privacy@lobsy.nl", dto.PrivacyContact);
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

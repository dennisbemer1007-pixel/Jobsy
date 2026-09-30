using System.Text.RegularExpressions;

namespace Jobsy.Tests;

public class MailSettingsPageTests
{
    [Fact]
    public void Unsubscribe_and_settings_pages_use_post_forms_and_no_token_links_outside_form()
    {
        var root = FindRepoRoot();
        var unsub = File.ReadAllText(Path.Combine(root, "Jobsy.Web/Components/Pages/Public/MailUnsubscribe.razor"));
        var settings = File.ReadAllText(Path.Combine(root, "Jobsy.Web/Components/Pages/Account/MailSettings.razor"));

        Assert.Contains("method=\"post\"", unsub, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("action=\"/mail/afmelden\"", unsub, StringComparison.Ordinal);
        Assert.Contains("method=\"post\"", settings, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("action=\"/account/mail-instellingen\"", settings, StringComparison.Ordinal);

        // Token only inside hidden form fields, not as GET navigation links.
        Assert.DoesNotContain("href=\"/mail/afmelden?t=", unsub, StringComparison.Ordinal);
        Assert.DoesNotContain("href=\"/account/mail-instellingen?t=", settings, StringComparison.Ordinal);
    }

    [Fact]
    public void Seo_catalog_marks_new_mail_pages_private_noindex()
    {
        var root = FindRepoRoot();
        var seo = File.ReadAllText(Path.Combine(root, "Jobsy.Web/Seo/PageSeoCatalog.cs"));
        Assert.Contains("[\"/mail/afmelden\"]", seo, StringComparison.Ordinal);
        Assert.Contains("[\"/account/mail-instellingen\"]", seo, StringComparison.Ordinal);
        Assert.Contains("MailUnsub.Seo.Title", seo, StringComparison.Ordinal);
        Assert.Contains("MailSettings.Seo.Title", seo, StringComparison.Ordinal);
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

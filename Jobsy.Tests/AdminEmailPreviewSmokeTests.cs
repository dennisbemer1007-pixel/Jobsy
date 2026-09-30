namespace Jobsy.Tests;

/// <summary>
/// Smoke checks for the admin email preview page wiring (no live browser — markup + API client).
/// </summary>
public class AdminEmailPreviewSmokeTests
{
    [Fact]
    public void Mail_test_page_uses_sandboxed_iframe_preview()
    {
        var root = FindRepoRoot();
        var page = File.ReadAllText(Path.Combine(root, "Jobsy.Web", "Components", "Pages", "Admin", "MailTestAdmin.razor"));
        var client = File.ReadAllText(Path.Combine(root, "Jobsy.Web", "Services", "ApiClient", "JobsyApiClient.Admin.cs"));

        Assert.Contains("@page \"/admin/content/emails\"", page);
        Assert.Contains("[Authorize(Roles = \"Admin\")]", page);
        Assert.Contains("sandbox=\"\"", page);
        Assert.Contains("srcdoc=", page);
        Assert.Contains("GetEmailTemplatePreviewAsync", page);
        Assert.Contains("ar (RTL)", page);
        Assert.Contains("GetEmailTemplatePreviewAsync", client);
        Assert.Contains("StartSendAllEmailTemplatesAsync", client);
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

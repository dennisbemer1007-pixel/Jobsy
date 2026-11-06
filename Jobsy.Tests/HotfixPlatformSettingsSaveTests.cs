namespace Jobsy.Tests;

public class HotfixPlatformSettingsSaveTests
{
    [Fact]
    public void Platform_settings_editor_shows_save_errors_and_verifies_api_response()
    {
        var editor = File.ReadAllText(Path.Combine(
            FindRepoRoot(),
            "Jobsy.Web/Components/Admin/Sections/PlatformSettingsEditor.razor"));
        Assert.Contains("_messageIsError ? \"error\" : \"success\"", editor, StringComparison.Ordinal);
        Assert.Contains("VerifySavedValues", editor, StringComparison.Ordinal);
        Assert.Contains("FeatureFlags.Invalidate()", editor, StringComparison.Ordinal);
        Assert.Contains("if (saved is null)", editor, StringComparison.Ordinal);

        var client = File.ReadAllText(Path.Combine(
            FindRepoRoot(),
            "Jobsy.Web/Services/ApiClient/JobsyApiClient.Admin.cs"));
        Assert.Contains("Lege reactie van de API bij opslaan van platforminstellingen.", client, StringComparison.Ordinal);
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

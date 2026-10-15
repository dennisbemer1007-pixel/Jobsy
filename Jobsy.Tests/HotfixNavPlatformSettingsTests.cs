namespace Jobsy.Tests;

public class HotfixNavPlatformSettingsTests
{
    [Fact]
    public void Desktop_candidate_main_nav_is_a_left_sidebar_not_hidden()
    {
        var css = File.ReadAllText(Path.Combine(FindRepoRoot(), "Jobsy.Web/wwwroot/css/app.css"));
        Assert.Contains("@media (min-width: 1024px)", css, StringComparison.Ordinal);
        Assert.Contains(".app-shell:has(.bottom-nav) .bottom-nav {", css, StringComparison.Ordinal);
        Assert.Contains("flex-direction: column;", css, StringComparison.Ordinal);
        Assert.Contains("padding-inline-start: 13.5rem;", css, StringComparison.Ordinal);
        Assert.DoesNotContain(
            ".app-shell .bottom-nav {\n        display: none;",
            css,
            StringComparison.Ordinal);
    }

    [Fact]
    public void Main_layout_refreshes_candidate_shell_when_auth_state_changes()
    {
        var layout = File.ReadAllText(Path.Combine(
            FindRepoRoot(),
            "Jobsy.Web/Components/Layout/MainLayout.razor"));
        Assert.Contains("AuthenticationStateChanged += OnAuthenticationStateChanged", layout, StringComparison.Ordinal);
        Assert.Contains("app-shell--candidate", layout, StringComparison.Ordinal);
    }

    [Fact]
    public void Platform_settings_editor_shows_save_errors_and_verifies_api_response()
    {
        var editor = File.ReadAllText(Path.Combine(
            FindRepoRoot(),
            "Jobsy.Web/Components/Admin/Sections/PlatformSettingsEditor.razor"));
        Assert.Contains("state-message @(_messageIsError ? \"error\" : null)", editor, StringComparison.Ordinal);
        Assert.Contains("VerifySavedValues", editor, StringComparison.Ordinal);
        Assert.Contains("FeatureFlags.Invalidate()", editor, StringComparison.Ordinal);
        Assert.Contains("if (saved is null)", editor, StringComparison.Ordinal);

        var client = File.ReadAllText(Path.Combine(
            FindRepoRoot(),
            "Jobsy.Web/Services/ApiClient/JobsyApiClient.Admin.cs"));
        Assert.Contains("Lege reactie van de API bij opslaan van platforminstellingen.", client, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(true, true, 5)]
    [InlineData(true, false, 3)]
    [InlineData(false, true, 5)]
    [InlineData(false, false, 2)]
    public void Candidate_nav_catalog_never_empty_for_common_flag_combos(bool passportOn, bool employersOn, int expectedCount)
    {
        var flags = new Jobsy.Core.Features.FeatureFlagSnapshot(
            EmployersEnabled: employersOn,
            CandidatePassportEnabled: passportOn);
        var items = Jobsy.Web.Navigation.RoleNavCatalog.CandidateItems(flags);
        Assert.Equal(expectedCount, items.Count);
        Assert.True(items.Count > 0);
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

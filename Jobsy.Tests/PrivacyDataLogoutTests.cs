namespace Jobsy.Tests;

/// <summary>
/// <c>/privacy/data</c> (public-pages 07): after a completed account deletion the page signs
/// the visitor out via a hidden, antiforgery-protected POST form to <c>/account/logout</c> —
/// never the shared <see cref="Jobsy.Web.Components.UnsubscribeDialog"/>'s own GET navigation.
/// </summary>
public class PrivacyDataLogoutTests
{
    [Fact]
    public void Page_renders_a_hidden_post_logout_form_with_an_antiforgery_token()
    {
        var markup = ReadPrivacyData();

        Assert.Contains(
            "<form id=\"pp-logout-form\" class=\"pp-mydata__logout-form\" method=\"post\" action=\"/account/logout\">",
            markup,
            StringComparison.Ordinal);
        Assert.Contains("<AntiforgeryToken", markup, StringComparison.Ordinal);
    }

    [Fact]
    public void Page_disables_the_dialogs_own_get_navigation_and_submits_the_form_via_js_interop()
    {
        var markup = ReadPrivacyData();

        Assert.Contains("SkipLogoutNavigation=\"true\"", markup, StringComparison.Ordinal);
        Assert.Contains(
            "CircuitInterop.InvokeVoidIgnoredAsync(Js, \"lobsyPublicPages.submitForm\", \"pp-logout-form\")",
            markup,
            StringComparison.Ordinal);
        Assert.DoesNotContain("NavigateTo(\"/account/logout\"", markup, StringComparison.Ordinal);
    }

    [Fact]
    public void Shared_dialog_only_navigates_when_the_caller_did_not_opt_out()
    {
        var dialog = ReadRepoFile("Jobsy.Web", "Components", "UnsubscribeDialog.razor");

        Assert.Contains("[Parameter] public bool SkipLogoutNavigation { get; set; }", dialog, StringComparison.Ordinal);
        Assert.Contains("if (!SkipLogoutNavigation)", dialog, StringComparison.Ordinal);
        Assert.Contains("Navigation.NavigateTo(\"/account/logout\", forceLoad: true)", dialog, StringComparison.Ordinal);
    }

    [Fact]
    public void Form_submit_helper_calls_the_native_html_form_submit()
    {
        var js = ReadRepoFile("Jobsy.Web", "wwwroot", "js", "public-pages.js");

        Assert.Contains("submitForm", js, StringComparison.Ordinal);
        Assert.Contains(".submit()", js, StringComparison.Ordinal);
    }

    private static string ReadPrivacyData()
        => ReadRepoFile("Jobsy.Web", "Components", "Pages", "Legal", "PrivacyData.razor");

    private static string ReadRepoFile(params string[] relativeParts)
        => File.ReadAllText(Path.Combine(FindRepoRoot(), Path.Combine(relativeParts)));

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

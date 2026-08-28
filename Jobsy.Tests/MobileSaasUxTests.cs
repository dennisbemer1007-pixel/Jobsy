namespace Jobsy.Tests;

public class MobileSaasUxTests
{
    [Fact]
    public void Bottom_nav_is_fixed_and_pages_clear_it_with_pb28()
    {
        var css = File.ReadAllText(Path.Combine(FindRepoRoot(), "Jobsy.Web/wwwroot/css/app.css"));
        Assert.Contains(".bottom-nav {\n    position: fixed;\n    bottom: 0;\n    left: 0;\n    right: 0;\n    z-index: 50;", css);
        Assert.Contains("--bottom-nav-clearance: 7rem;", css);
        Assert.Contains("padding-bottom: calc(var(--bottom-nav-clearance) + env(safe-area-inset-bottom, 0px));", css);

        var layout = File.ReadAllText(Path.Combine(FindRepoRoot(), "Jobsy.Web/Components/Layout/MainLayout.razor"));
        Assert.Contains("<BottomNav", layout);
        Assert.Contains("<AppFooter", layout);
    }

    [Fact]
    public void Mobile_footer_is_a_text_line_and_legal_links_live_in_the_account_menu()
    {
        var css = File.ReadAllText(Path.Combine(FindRepoRoot(), "Jobsy.Web/wwwroot/css/app.css"));
        Assert.DoesNotContain(".app-footer__links {\n        display: grid;\n        grid-template-columns: repeat(2, minmax(0, 1fr));", css);
        Assert.Contains(".account-menu__panel", css);

        var footer = File.ReadAllText(Path.Combine(FindRepoRoot(), "Jobsy.Web/Components/Layout/AppFooter.razor"));
        Assert.Contains("href=\"/privacy\"", footer);
        Assert.Contains("href=\"/algemene-voorwaarden\"", footer);
        Assert.Contains("href=\"/gebruiksvoorwaarden\"", footer);
        Assert.Contains("href=\"/wie-zijn-wij\"", footer);
        Assert.Contains("href=\"/westland\"", footer);

        var header = File.ReadAllText(Path.Combine(FindRepoRoot(), "Jobsy.Web/Components/Layout/AuthHeader.razor"));
        Assert.Contains("account-menu", header);
        Assert.Contains("href=\"/privacy\"", header);
        Assert.Contains("aria-expanded=\"@(_open ? \"true\" : \"false\")\"", header);
        Assert.DoesNotContain("aria-expanded=\"@_open\"", header);
    }

    [Fact]
    public void Tabs_scroll_horizontally_as_pills_with_brand_active_state()
    {
        var css = File.ReadAllText(Path.Combine(FindRepoRoot(), "Jobsy.Web/wwwroot/css/app.css"));
        Assert.Contains(".admin-sublinks {\n    display: flex;\n    flex-direction: row;\n    flex-wrap: nowrap;", css);
        Assert.Contains("overflow-x: auto", css);
        Assert.Contains(".admin-sublink--active {\n    background: var(--accent);\n    color: #fff;", css);
        Assert.Contains(".applicants-filters__btn.is-active {\n    border-color: var(--accent);\n    background: var(--accent);\n    color: #fff;", css);
        Assert.Contains("min-height: 40px", css);
        Assert.Contains(".pill-scroller", css);
        Assert.Contains("scrollbar-width: none", css);
    }

    [Fact]
    public void Applicants_page_uses_cards_and_never_renders_raw_json()
    {
        var razor = File.ReadAllText(Path.Combine(FindRepoRoot(), "Jobsy.Web/Components/Pages/Branch/Applicants.razor"));
        Assert.Contains("class=\"applicants-list\"", razor);
        Assert.Contains("class=\"applicant-card", razor);
        Assert.Contains("applicant-card__section", razor);
        Assert.Contains("Employer.FactMotivation", razor);
        Assert.Contains("Employer.FactAvailability", razor);
        Assert.Contains("Employer.FactProfile", razor);
        Assert.Contains("Employer.FactContact", razor);
        Assert.Contains("HumanText(a.PreferencesSummary)", razor);
        Assert.DoesNotContain("@a.PreferencesSummary", razor);
        Assert.DoesNotContain("applicants-grid__table", razor);
        Assert.DoesNotContain("<table", razor);
    }

    [Fact]
    public void Token_purchase_uses_a_two_column_pack_grid()
    {
        var tokens = File.ReadAllText(Path.Combine(FindRepoRoot(), "Jobsy.Web/Components/Pages/Employer/Tokens.razor"));
        Assert.Contains("class=\"token-pack-options\"", tokens);
        Assert.DoesNotContain("token-pack-options--vertical", tokens);
        Assert.DoesNotContain("max-width:32rem", tokens);

        var css = File.ReadAllText(Path.Combine(FindRepoRoot(), "Jobsy.Web/wwwroot/css/app.css"));
        Assert.Contains(".token-pack-options {\n    display: grid;\n    grid-template-columns: repeat(2, minmax(0, 1fr));", css);
        Assert.Contains(".token-pack-options--vertical {\n    grid-template-columns: repeat(2, minmax(0, 1fr));", css);
        Assert.Contains(".token-buy .login-submit {\n    width: 100%;", css);
    }

    [Fact]
    public void Users_and_team_pages_use_cards_not_tables()
    {
        var users = File.ReadAllText(Path.Combine(FindRepoRoot(), "Jobsy.Web/Components/Pages/Employer/Users.razor"));
        Assert.Contains("class=\"user-card-list\"", users);
        Assert.Contains("class=\"user-card\"", users);
        Assert.Contains("user-card__menu-toggle", users);
        Assert.Contains("aria-expanded=\"@(menuOpen ? \"true\" : \"false\")\"", users);
        Assert.Contains("class=\"users-toolbar__filters\"", users);
        Assert.Contains("login-form invite-form", users);
        Assert.Contains("invite-form__row", users);
        Assert.Contains("Uitnodigen", users);
        Assert.DoesNotContain("users-table", users);
        Assert.DoesNotContain("<table", users);

        var team = File.ReadAllText(Path.Combine(FindRepoRoot(), "Jobsy.Web/Components/Pages/Intermediary/Team.razor"));
        Assert.Contains("class=\"user-card-list\"", team);
        Assert.Contains("class=\"user-card\"", team);
        Assert.Contains("login-form invite-form", team);
        Assert.DoesNotContain("users-table", team);
        Assert.DoesNotContain("<table", team);

        var css = File.ReadAllText(Path.Combine(FindRepoRoot(), "Jobsy.Web/wwwroot/css/app.css"));
        Assert.Contains(".user-card-list {\n    display: flex;\n    flex-direction: column;", css);
        Assert.Contains(".users-toolbar {\n    display: flex;\n    flex-direction: column;", css);
        Assert.Contains(".users-toolbar__filters {\n    display: grid;\n    grid-template-columns: minmax(0, 1fr) minmax(0, 1fr);", css);
        Assert.Contains(".invite-form__row {\n    display: grid;\n    grid-template-columns: minmax(0, 1fr) minmax(0, 1fr);", css);
        Assert.Contains(".app-shell.has-bottom-nav .app-footer,\n    .app-shell:has(.bottom-nav) .app-footer {\n        display: none;", css);
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

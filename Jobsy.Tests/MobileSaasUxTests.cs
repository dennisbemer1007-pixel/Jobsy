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
    public void Mobile_shell_locks_horizontal_overflow_and_moves_logout_into_the_account_menu()
    {
        var css = File.ReadAllText(Path.Combine(FindRepoRoot(), "Jobsy.Web/wwwroot/css/app.css"));
        Assert.Contains("html, body {\n    margin: 0;\n    height: 100%;\n    max-width: 100%;\n    overflow-x: hidden;", css);
        Assert.Contains(".app-shell {\n    display: flex;\n    flex-direction: column;\n    height: 100vh;\n    min-height: 100vh;\n    max-width: 100%;\n    min-width: 0;\n    overflow-x: hidden;", css);
        Assert.Contains(".app-header__actions {\n    display: flex;\n    align-items: center;\n    gap: 0.75rem;\n    flex: 1 1 auto;\n    min-width: 0;", css);
        Assert.Contains(".auth-logout-form--chrome {\n        display: none;", css);

        var header = File.ReadAllText(Path.Combine(FindRepoRoot(), "Jobsy.Web/Components/Layout/AuthHeader.razor"));
        Assert.Contains("id=\"auth-logout\"", header);
        Assert.Contains("form=\"auth-logout\"", header);
        Assert.Contains("account-menu__link--logout", header);
        Assert.Contains("Auth.Logout", header);
        Assert.Contains("auth-logout-form--chrome", header);

        var app = File.ReadAllText(Path.Combine(FindRepoRoot(), "Jobsy.Web/Components/App.razor"));
        Assert.Contains("max-width: 100%; overflow-x: hidden;", app);
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
        Assert.DoesNotContain(".app-main {\n        padding-bottom: var(--bottom-nav-clearance);", css);
        Assert.Contains("RequestDeactivate", users);
        Assert.Contains("Bevestigen", users);
    }

    [Fact]
    public void Candidate_applications_use_cards_with_current_status_and_bar_stepper()
    {
        var razor = File.ReadAllText(Path.Combine(FindRepoRoot(), "Jobsy.Web/Components/Pages/Candidate/Applications.razor"));
        Assert.Contains("class=\"panel-page apps-page\"", razor);
        Assert.Contains("apps-tabs", razor);
        Assert.Contains("class=\"application-card-list\"", razor);
        Assert.Contains("class=\"application-card\"", razor);
        Assert.Contains("application-card__title", razor);
        Assert.Contains("application-card__meta", razor);
        Assert.Contains("application-card__company", razor);
        Assert.Contains("Apps.StatusNow", razor);
        Assert.Contains("application-stepper", razor);
        Assert.Contains("visually-hidden", razor);
        Assert.Contains("application-card__btn", razor);
        Assert.DoesNotContain("class=\"table-list\"", razor);
        Assert.DoesNotContain("<table", razor);
        Assert.DoesNotContain("<span>@steps[i]</span>", razor);

        var css = File.ReadAllText(Path.Combine(FindRepoRoot(), "Jobsy.Web/wwwroot/css/app.css"));
        Assert.Contains(".application-card-list {\n    display: flex;\n    flex-direction: column;\n    gap: 1rem;", css);
        Assert.Contains(".apps-tabs.admin-sublinks {\n    position: sticky;\n    top: 0;", css);
        Assert.Contains(".application-card__actions {\n    display: flex;\n    flex-wrap: wrap;", css);
        Assert.Contains(".application-card__actions .application-card__btn {\n    flex: 1 1 8.5rem;\n    min-height: 2.6rem;\n    border-radius: 10px;", css);
        Assert.Contains(".application-stepper__step.is-done .application-stepper__bar,\n.application-stepper__step.is-current .application-stepper__bar {\n    background: var(--brand);", css);
    }

    [Fact]
    public void Candidate_profile_uses_accordions_compact_availability_and_sticky_save()
    {
        var razor = File.ReadAllText(Path.Combine(FindRepoRoot(), "Jobsy.Web/Components/Pages/Candidate/Profile.razor"));
        Assert.Contains("profile-page--candidate", razor);
        Assert.Contains("profile-accordion", razor);
        Assert.Contains("ToggleSection(\"personal\")", razor);
        Assert.Contains("ToggleSection(\"preferences\")", razor);
        Assert.Contains("ToggleSection(\"availability\")", razor);
        Assert.Contains("ToggleSection(\"experience\")", razor);
        Assert.Contains("profile-check-grid", razor);
        Assert.Contains("availability-matrix", razor);
        Assert.Contains("availability-presets", razor);
        Assert.Contains("profile-save-bar", razor);
        Assert.Contains("aria-expanded=\"@(IsSectionOpen(\"personal\") ? \"true\" : \"false\")\"", razor);
        Assert.DoesNotContain("vacancy-schedule__table", razor);

        var css = File.ReadAllText(Path.Combine(FindRepoRoot(), "Jobsy.Web/wwwroot/css/app.css"));
        Assert.Contains(".profile-accordion {\n    display: flex;\n    flex-direction: column;", css);
        Assert.Contains(".profile-page--candidate .profile-check-grid,\n.profile-page--candidate .profile-roles.profile-check-grid {\n    grid-template-columns: repeat(2, minmax(0, 1fr));", css);
        Assert.Contains(".availability-matrix {\n    display: grid;\n    grid-template-columns: 2.35rem repeat(4, minmax(0, 1fr));", css);
        Assert.Contains("bottom: calc(4.75rem + env(safe-area-inset-bottom, 0px));", css);
        Assert.Contains(".profile-save-bar .login-submit {\n    width: 100%;", css);
        Assert.Contains(".profile-page--candidate .profile-contact__names {\n    grid-template-columns: repeat(2, minmax(0, 1fr));", css);
        Assert.Contains(".profile-page--candidate .profile-page__header {\n        display: none;", css);
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

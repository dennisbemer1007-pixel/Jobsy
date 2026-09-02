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
        Assert.Contains("HowLobsyHrefFor", header);
        Assert.Contains("Nav.HowLobsyWorks", header);
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
        Assert.Contains(".auth-logout-form--chrome {\n    display: none;", css);

        var header = File.ReadAllText(Path.Combine(FindRepoRoot(), "Jobsy.Web/Components/Layout/AuthHeader.razor"));
        Assert.Contains("id=\"auth-logout\"", header);
        Assert.Contains("form=\"auth-logout\"", header);
        Assert.Contains("account-menu__link--logout", header);
        Assert.Contains("Auth.Logout", header);
        Assert.Contains("auth-logout-form--chrome", header);
        Assert.DoesNotContain("auth-icon-btn", header);
        Assert.DoesNotContain("NavIcons.Logout", header);

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
        Assert.Contains("membership-grid", users);
        Assert.Contains("pb-28", users);
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
        Assert.Contains("Profile.ReturnHint", razor);
        Assert.Contains("profile-return-hint", razor);
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

    [Fact]
    public void Guest_discovery_omits_save_search_button()
    {
        var discovery = File.ReadAllText(Path.Combine(FindRepoRoot(), "Jobsy.Web/Components/VacancyDiscovery.razor"));
        Assert.Contains("AuthorizeView", discovery);
        Assert.Contains("jobsy-action--save", discovery);
        Assert.Contains("href=\"/candidate/liked\"", discovery);
        Assert.Contains("Discovery.SaveSearch", discovery);
        Assert.DoesNotContain("LikedLoginUrl", discovery);
        Assert.DoesNotContain("<NotAuthorized>", discovery);

        var authStart = discovery.IndexOf("<Authorized>", StringComparison.Ordinal);
        Assert.True(authStart > 0);
        var authBlock = discovery[authStart..Math.Min(discovery.Length, authStart + 450)];
        Assert.Contains("href=\"/candidate/liked\"", authBlock);
        Assert.Contains("Discovery.SaveSearch", authBlock);
    }

    [Fact]
    public void Employer_vacancies_use_mgmt_cards_with_stat_mini_grid()
    {
        var razor = File.ReadAllText(Path.Combine(FindRepoRoot(), "Jobsy.Web/Components/Pages/Employer/Vacancies.razor"));
        Assert.Contains("vacancy-card-list", razor);
        Assert.Contains("vacancy-mgmt-card", razor);
        Assert.Contains("vacancy-mgmt-card__stats", razor);
        Assert.Contains("vacancy-mgmt-card__status", razor);
        Assert.DoesNotContain("table-scroll vacancy-grid", razor);
        Assert.DoesNotContain("<table class=\"data-table\">", razor);

        var css = File.ReadAllText(Path.Combine(FindRepoRoot(), "Jobsy.Web/wwwroot/css/app.css"));
        Assert.Contains(".vacancy-mgmt-card__stats {\n    display: grid;\n    grid-template-columns: repeat(3, minmax(0, 1fr));", css);
        Assert.Contains(".bento-cell--category {\n        background: transparent;\n        border: none;", css);
    }

    [Fact]
    public void Applicants_availability_renders_a_readonly_matrix_not_raw_day_text()
    {
        var razor = File.ReadAllText(Path.Combine(FindRepoRoot(), "Jobsy.Web/Components/Pages/Branch/Applicants.razor"));
        Assert.Contains("availability-matrix--readonly", razor);
        Assert.Contains("ParseAvailabilityPayload", razor);
        Assert.Contains("a.PiiRevealed", razor);
        Assert.Contains("Common.Yes", razor);
        Assert.Contains("EmployerDisplayDayPartCodes", razor);
        Assert.Contains("DayPartMatrix.NightDayPart", razor);
        Assert.Contains("availability-matrix__night-note", razor);
        Assert.Contains("Profile.Slot.Night", razor);
        Assert.DoesNotContain("DayPartMatrix.DayPartCodes", razor);
        Assert.DoesNotContain("aria-label=\"@UiLabels.Weekday(Culture, day) @UiLabels.AvailabilitySlot(Culture, slot): @(on ? \"ja\" : \"nee\")\"", razor);
    }

    [Fact]
    public void Token_logs_hide_technical_ids_and_show_explicit_token_amounts()
    {
        var tokens = File.ReadAllText(Path.Combine(FindRepoRoot(), "Jobsy.Web/Components/Pages/Employer/Tokens.razor"));
        Assert.Contains("token-log-list", tokens);
        Assert.Contains("TokenLogPresentation.FormatAmount", tokens);
        Assert.Contains("TokenLogPresentation.FormatWhen", tokens);
        Assert.Contains("TokenLogPresentation.Describe", tokens);
        Assert.DoesNotContain("@log.Kind / @log.Reason", tokens);
        Assert.DoesNotContain("dd-MM HH:mm", tokens);
        Assert.Contains("pill-scroller token-tabs", tokens);
        Assert.Contains("pb-28", tokens);

        var css = File.ReadAllText(Path.Combine(FindRepoRoot(), "Jobsy.Web/wwwroot/css/app.css"));
        Assert.Contains(".token-log__amount--in {\n    color: var(--success);", css);
        Assert.Contains(".token-log__amount--out {\n    color: var(--danger);", css);
    }

    [Fact]
    public void Header_popups_stay_in_viewport_and_dashboard_moves_raamflyer_off_home()
    {
        var css = File.ReadAllText(Path.Combine(FindRepoRoot(), "Jobsy.Web/wwwroot/css/app.css"));
        Assert.Contains(".notification-dropdown", css);
        Assert.Contains(".info-dropdown", css);
        Assert.Contains(".header-dropdown-backdrop", css);
        Assert.Contains("z-index: 55", css);
        Assert.Contains(".notification-dropdown.z-50", css);
        Assert.Contains("max-width: 90vw", css);
        Assert.Contains(".z-50 { z-index: 50; }", css);
        Assert.Contains(".pb-28 { padding-bottom: 7rem; }", css);
        Assert.Contains(".grid-cols-2 { grid-template-columns: repeat(2, minmax(0, 1fr));", css);
        Assert.Contains(".availability-matrix--readonly {\n    grid-template-columns: 2.35rem repeat(3, minmax(0, 1fr));", css);
        Assert.Contains(".dash-refresh-btn {\n    display: inline-flex;", css);

        var help = File.ReadAllText(Path.Combine(FindRepoRoot(), "Jobsy.Web/Components/Layout/PageHelp.razor"));
        Assert.Contains("info-dropdown", help);
        Assert.Contains("header-dropdown-backdrop", help);
        Assert.Contains("right-0 max-w-[90vw] mx-auto z-50", help);

        var bell = File.ReadAllText(Path.Combine(FindRepoRoot(), "Jobsy.Web/Components/Layout/NotificationBell.razor"));
        Assert.Contains("notification-dropdown", bell);
        Assert.Contains("header-dropdown-backdrop", bell);
        Assert.Contains("right-0 max-w-[90vw] mx-auto z-50", bell);

        var home = File.ReadAllText(Path.Combine(FindRepoRoot(), "Jobsy.Web/Components/Pages/EmployerHomePanel.razor"));
        Assert.DoesNotContain("Download Raamflyer", home);
        Assert.DoesNotContain("Per vestiging", home);
        Assert.DoesNotContain("raamflyer-scope", home);
        Assert.Contains("dashboard-secondary", home);
        Assert.Contains("RaamflyerTools", home);
        Assert.Contains("panel-header__title-row", home);
        Assert.Contains("DashboardRefreshButton", home);

        var company = File.ReadAllText(Path.Combine(FindRepoRoot(), "Jobsy.Web/Components/Pages/Employer/CompanyDetails.razor"));
        Assert.Contains("RaamflyerTools", company);
        Assert.Contains("Wervingsmateriaal", company);

        var branches = File.ReadAllText(Path.Combine(FindRepoRoot(), "Jobsy.Web/Components/Pages/Employer/Branches.razor"));
        Assert.Contains("RaamflyerTools", branches);

        var refresh = File.ReadAllText(Path.Combine(FindRepoRoot(), "Jobsy.Web/Components/Shared/DashboardRefreshButton.razor"));
        Assert.Contains("dash-refresh-btn", refresh);
        Assert.Contains("aria-label=\"Ververs\"", refresh);
    }

    [Fact]
    public void Login_is_compact_modern_and_honors_return_aliases()
    {
        var login = File.ReadAllText(Path.Combine(FindRepoRoot(), "Jobsy.Web/Components/Pages/Login.razor"));
        Assert.Contains("login-modal--compact", login);
        Assert.Contains("AuthRedirects.ResolveRequestedReturnUrl", login);
        Assert.Contains("QueryValue(query, \"returnTo\")", login);
        Assert.Contains("QueryValue(query, \"redirect\")", login);
        Assert.Contains("name=\"returnUrl\"", login);
        Assert.Contains("/account/external/entra?returnUrl=", login);
        Assert.Contains("/account/external/google?returnUrl=", login);
        Assert.DoesNotContain("login-brand", login);
        Assert.DoesNotContain("<LobsyLogo", login);
        Assert.DoesNotContain("login-register__actions", login);
        Assert.Contains("login-register__cta", login);
        Assert.Contains("login-register__back", login);
        Assert.Contains("NavigateTo(_returnUrl", login);
        Assert.Contains("class=\"w-full px-4 py-3\"", login);

        var css = File.ReadAllText(Path.Combine(FindRepoRoot(), "Jobsy.Web/wwwroot/css/app.css"));
        Assert.Contains(".login-modal .login-form {\n    display: grid;\n    grid-template-columns: minmax(0, 1fr);", css);
        Assert.Contains("width: 100% !important;", css);
        Assert.Contains("min-height: 3rem;", css);
        Assert.Contains("border-radius: 14px;", css);
        Assert.Contains(".w-full { width: 100%; }", css);
        Assert.Contains(".px-4 { padding-left: 1rem; padding-right: 1rem; }", css);
        Assert.Contains(".py-3 { padding-top: 0.75rem; padding-bottom: 0.75rem; }", css);
        Assert.Contains(".login-modal .login-form label {\n    text-transform: none;", css);
        Assert.Contains(".login-modal__dialog .login-lead {\n        display: none;", css);
        Assert.Contains(".login-modal .provider-btn {\n    min-height: 2.1rem;", css);

        var app = File.ReadAllText(Path.Combine(FindRepoRoot(), "Jobsy.Web/Components/App.razor"));
        Assert.Contains(".login-modal .login-form input:not([type=\"checkbox\"]):not([type=\"radio\"]) { display: block; width: 100%;", app);

        var header = File.ReadAllText(Path.Combine(FindRepoRoot(), "Jobsy.Web/Components/Layout/AuthHeader.razor"));
        Assert.Contains("IsLoginRoute", header);
        Assert.Contains("href=\"/login\"", header);

        var idle = File.ReadAllText(Path.Combine(FindRepoRoot(), "Jobsy.Web/wwwroot/js/sessionIdle.js"));
        Assert.Contains("sessionReturnUrl", idle);
        Assert.Contains("&returnUrl=", idle);
        Assert.Contains("return path;", idle);
        Assert.DoesNotContain("path + (window.location.search", idle);

        var extras = File.ReadAllText(Path.Combine(FindRepoRoot(), "Jobsy.Web/wwwroot/js/app-extras.js"));
        Assert.Contains("sessionReturnUrl", extras);
        Assert.Contains("&returnUrl=", extras);
        Assert.Contains("return path;", extras);
        Assert.DoesNotContain("path + (window.location.search", extras);
    }

    [Fact]
    public void Assistant_and_feedback_are_right_edge_tabs_and_how_lobsy_lives_in_the_account_menu()
    {
        var assistant = File.ReadAllText(Path.Combine(FindRepoRoot(), "Jobsy.Web/Components/LobsyAssistantChat.razor"));
        Assert.DoesNotContain("lobsy-assistant__fab", assistant);
        Assert.Contains("lobsy-assistant-tab", assistant);
        Assert.Contains("lobsy-assistant-tab__btn", assistant);
        Assert.Contains("AssistantChatHost", assistant);
        Assert.Contains("ChatHost.ToggleRequested", assistant);
        Assert.Contains("UseMascot=\"true\"", assistant);
        Assert.Contains("aria-expanded=\"@(_open ? \"true\" : \"false\")\"", assistant);
        Assert.DoesNotContain("aria-expanded=\"@_open\"", assistant);

        var nav = File.ReadAllText(Path.Combine(FindRepoRoot(), "Jobsy.Web/Components/Layout/BottomNav.razor"));
        Assert.DoesNotContain("bottom-nav__item--assistant", nav);
        Assert.DoesNotContain("NavIcons.Assistant", nav);
        Assert.DoesNotContain("AssistantChatHost", nav);
        Assert.DoesNotContain("Nav.HowLobsyWorks", nav);

        var header = File.ReadAllText(Path.Combine(FindRepoRoot(), "Jobsy.Web/Components/Layout/AuthHeader.razor"));
        Assert.Contains("HowLobsyHrefFor", header);
        Assert.Contains("Nav.HowLobsyWorks", header);

        var feedback = File.ReadAllText(Path.Combine(FindRepoRoot(), "Jobsy.Web/Components/Feedback/FeedbackWidget.razor"));
        Assert.Contains("feedback-widget--tab", feedback);
        Assert.Contains("feedback-widget__tab", feedback);
        Assert.DoesNotContain("Feedback.CaptureFailed", feedback);

        var css = File.ReadAllText(Path.Combine(FindRepoRoot(), "Jobsy.Web/wwwroot/css/app.css"));
        Assert.Contains(".lobsy-assistant-tab {\n    position: fixed;\n    top: 26%;\n    right: 0;", css);
        Assert.Contains(".lobsy-assistant-tab__btn {\n    display: inline-flex;\n    align-items: center;\n    gap: 0.35rem;\n    writing-mode: vertical-rl;", css);
        Assert.Contains(".feedback-widget {\n    position: fixed;\n    top: 46%;\n    right: 0;", css);
        Assert.Contains(".feedback-widget__tab {\n    writing-mode: vertical-rl;", css);
        Assert.Contains(".lobsy-assistant__fab {\n    display: none !important;", css);
        Assert.Contains("button.bottom-nav__item {", css);
        Assert.DoesNotContain(".bottom-nav__item--assistant {", css);
        Assert.Contains(".pb-28 { padding-bottom: 7rem; }", css);
        Assert.Contains(".overflow-x-hidden { overflow-x: hidden; }", css);

        var program = File.ReadAllText(Path.Combine(FindRepoRoot(), "Jobsy.Web/Program.cs"));
        Assert.Contains("AddScoped<Jobsy.Web.Navigation.AssistantChatHost>()", program);
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

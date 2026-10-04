namespace Jobsy.Tests;

public class MobileSaasUxTests
{
    [Fact]
    public void Bottom_nav_is_fixed_and_pages_clear_it_with_pb28()
    {
        var css = File.ReadAllText(Path.Combine(FindRepoRoot(), "Jobsy.Web/wwwroot/css/app.css"));
        Assert.Contains(".bottom-nav {\n    position: fixed;\n    bottom: 0;\n    left: 0;\n    right: 0;\n    width: 100%;\n    max-width: 100%;\n    box-sizing: border-box;\n    z-index: 50;", css);
        Assert.Contains("--bottom-nav-clearance: 7rem;", css);
        Assert.Contains("padding-bottom: calc(var(--bottom-nav-clearance) + env(safe-area-inset-bottom, 0px));", css);

        var layout = File.ReadAllText(Path.Combine(FindRepoRoot(), "Jobsy.Web/Components/Layout/MainLayout.razor"));
        Assert.Contains("<BottomNav", layout);
        Assert.Contains("<AppFooter", layout);
        Assert.DoesNotContain("lobsy-watermarks", layout);
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
        Assert.Contains(".applicants-filters__btn.is-active {\n    border-color: var(--accent);\n    background: var(--accent);\n    color: #fff;", css);
        Assert.Contains("min-height: 40px", css);
        Assert.Contains(".pill-scroller", css);
        Assert.Contains("scrollbar-width: none", css);

        // Admin uses grouped sidebar (AdminLayout) instead of pill settings subnav.
        var adminCss = File.ReadAllText(Path.Combine(FindRepoRoot(), "Jobsy.Web/wwwroot/css/features/admin.css"));
        Assert.Contains(".admin-sidebar__link.is-active", adminCss);
        Assert.Contains("AdminSidebar", File.ReadAllText(Path.Combine(FindRepoRoot(), "Jobsy.Web/Components/Layout/AdminLayout.razor")));
        Assert.False(File.Exists(Path.Combine(FindRepoRoot(), "Jobsy.Web/Components/Admin/AdminSettingsSubnav.razor")));
    }

    [Fact]
    public void Applicants_page_uses_cards_and_never_renders_raw_json()
    {
        var razor = File.ReadAllText(Path.Combine(FindRepoRoot(), "Jobsy.Web/Components/Pages/Werkgever/Applicants.razor"));
        Assert.Contains("wg-app__pipeline", razor);
        Assert.Contains("class=\"wg-app-card", razor);
        Assert.Contains("ApplicationCandidateDetail", razor);
        Assert.Contains("CardFacts", razor);
        Assert.Contains("ApplicationPreferenceRedaction.ToHumanReadable", razor);
        Assert.DoesNotContain("@a.PreferencesSummary", razor);
        Assert.DoesNotContain("applicants-grid__table", razor);
        Assert.Contains("EntDataTable", razor);

        var detail = File.ReadAllText(Path.Combine(FindRepoRoot(), "Jobsy.Web/Components/Werkgever/Applications/ApplicationCandidateDetail.razor"));
        Assert.Contains("WgApp.Tab.Motivation", detail);
    }

    [Fact]
    public void Token_purchase_uses_pack_grid_on_overview()
    {
        var tokens = File.ReadAllText(Path.Combine(FindRepoRoot(), "Jobsy.Web/Components/Pages/Werkgever/Tokens.razor"));
        Assert.Contains("wg-tok-packs", tokens);
        Assert.Contains("WgTok.BuyButton", tokens);
        Assert.DoesNotContain("token-pack-options--vertical", tokens);

        var css = File.ReadAllText(Path.Combine(FindRepoRoot(), "Jobsy.Web/wwwroot/css/features/werkgever.css"));
        Assert.Contains(".wg-tok-packs", css);
        Assert.Contains("grid-template-columns: repeat(3, minmax(0, 1fr))", css);
    }

    [Fact]
    public void Mobile_shell_locks_horizontal_overflow_and_moves_logout_into_the_account_menu()
    {
        var css = File.ReadAllText(Path.Combine(FindRepoRoot(), "Jobsy.Web/wwwroot/css/app.css"));
        Assert.Contains("html, body {\n    margin: 0;\n    min-height: 100%;\n    max-width: 100%;\n    overflow-x: clip;", css);
        Assert.Contains(".app-shell {\n    display: flex;\n    flex-direction: column;\n    min-height: 100dvh;\n    max-width: 100%;\n    min-width: 0;\n    overflow-x: clip;", css);
        Assert.Contains(".app-header__actions {\n    display: flex;\n    align-items: center;\n    gap: 0.75rem;\n    flex: 1 1 auto;\n    min-width: 0;", css);

        var header = File.ReadAllText(Path.Combine(FindRepoRoot(), "Jobsy.Web/Components/Layout/AuthHeader.razor"));
        Assert.Contains("account-menu__link--logout", header);
        Assert.Contains("Auth.Logout", header);
        Assert.Contains("href=\"/logout\"", header);
        Assert.Contains("NavigateTo(\"/account/logout\"", header);
        Assert.DoesNotContain("Auth.Profile", header);
        Assert.DoesNotContain("Auth.Settings", header);
        Assert.DoesNotContain("Footer.Westland", header);
        Assert.DoesNotContain("href=\"/westland\"", header);
        Assert.DoesNotContain("auth-icon-btn", header);
        Assert.DoesNotContain("NavIcons.Logout", header);

        var app = File.ReadAllText(Path.Combine(FindRepoRoot(), "Jobsy.Web/Components/App.razor"));
        Assert.Contains("max-width: 100%; overflow-x: clip;", app);
    }

    [Fact]
    public void Users_and_team_pages_use_enterprise_table_and_invite_drawer()
    {
        var users = File.ReadAllText(Path.Combine(FindRepoRoot(), "Jobsy.Web/Components/Pages/Werkgever/Users.razor"));
        Assert.Contains("WgTeamTable", users);
        Assert.Contains("WgInviteDrawer", users);
        Assert.Contains("EntFilterBar", users);
        Assert.Contains("ConfirmDeactivate", users);
        Assert.Contains("LobsyFriendlyDialog", users);
        Assert.Contains("WgTeam.Action.Deactivate", users);
        Assert.Contains("WgInvite.Title", users);
        Assert.DoesNotContain("user-card-list", users);
        Assert.DoesNotContain("invite-form__row", users);
        Assert.DoesNotContain("users-table", users);

        var team = File.ReadAllText(Path.Combine(FindRepoRoot(), "Jobsy.Web/Components/Pages/Intermediary/Team.razor"));
        Assert.Contains("class=\"user-card-list\"", team);
        Assert.Contains("class=\"user-card\"", team);
        Assert.Contains("WgInviteDrawer", team);
        Assert.Contains("WgInvite.Title", team);
        Assert.DoesNotContain("login-form invite-form", team);
        Assert.DoesNotContain("users-table", team);
        Assert.DoesNotContain("<table", team);

        var css = File.ReadAllText(Path.Combine(FindRepoRoot(), "Jobsy.Web/wwwroot/css/app.css"));
        Assert.Contains(".user-card-list {\n    display: flex;\n    flex-direction: column;", css);
        Assert.Contains(".users-toolbar {\n    display: flex;\n    flex-direction: column;", css);
        Assert.Contains(".users-toolbar__filters {\n    display: grid;\n    grid-template-columns: minmax(0, 1fr) minmax(0, 1fr);", css);
        Assert.Contains(".invite-form__row {\n    display: grid;\n    grid-template-columns: minmax(0, 1fr) minmax(0, 1fr);", css);
        Assert.Contains(".app-shell.has-bottom-nav .app-footer,\n    .app-shell:has(.bottom-nav) .app-footer {\n        display: none;", css);
        Assert.DoesNotContain(".app-main {\n        padding-bottom: var(--bottom-nav-clearance);", css);
    }

    [Fact]
    public void Candidate_applications_use_cards_with_current_status_and_bar_stepper()
    {
        var razor = File.ReadAllText(Path.Combine(FindRepoRoot(), "Jobsy.Web/Components/Pages/Candidate/Applications.razor"));
        Assert.Contains("class=\"panel-page apps-page kb-apps\"", razor);
        Assert.Contains("application-counters", razor);
        Assert.Contains("application-card-list", razor);
        Assert.Contains("application-card", razor);
        Assert.Contains("application-card__title", razor);
        Assert.Contains("application-card__img", razor);
        Assert.Contains("application-card__progress", razor);
        Assert.Contains("kb-timeline", razor);
        Assert.Contains("application-card__company", razor);
        Assert.Contains("application-card--hired", razor);
        Assert.Contains("Kb.Apps.FilterRunning", razor);
        Assert.Contains("Apps.WithdrawConfirm", razor);
        Assert.Contains("LobsyFriendlyDialog", razor);
        Assert.Contains("aria-haspopup=\"menu\"", razor);
        Assert.DoesNotContain("apps-tabs", razor);
        Assert.DoesNotContain("application-stepper", razor);
        Assert.DoesNotContain("class=\"table-list\"", razor);
        Assert.DoesNotContain("<table", razor);

        var css = File.ReadAllText(Path.Combine(FindRepoRoot(), "Jobsy.Web/wwwroot/css/features/applications.css"));
        Assert.Contains(".application-counters", css);
        Assert.Contains(".application-card__img", css);
        Assert.Contains(".application-card__progress", css);
        Assert.Contains(".application-card--hired", css);
        // Mobile truncation: title up to 2 lines; status chip never ellipsized; company 1 line.
        Assert.Contains("line-clamp: 2", css);
        Assert.Contains("-webkit-line-clamp: 2", css);
        Assert.Contains("flex-shrink: 0", css);
        Assert.Contains("flex-wrap: wrap", css);
        Assert.Contains(".application-card__status", css);
        Assert.Contains("@media (max-width: 430px)", css);
        Assert.Contains(".application-card__company", css);
        Assert.Contains(
            ".application-card__company {\n    margin: 0;\n    font-size: var(--text-sm);\n    color: var(--muted);\n    white-space: nowrap;\n    overflow: hidden;\n    text-overflow: ellipsis;",
            css);
        Assert.DoesNotContain(
            ".application-card__title {\n    margin: 0;\n    font-size: var(--text-md);\n    font-weight: 600;\n    color: var(--text);\n    line-height: 1.25;\n    white-space: nowrap;",
            css);
        Assert.DoesNotContain(
            ".application-card__pill {\n    display: inline-flex;\n    align-items: center;\n    gap: 5px;\n    block-size: 24px;\n    padding-inline: 8px;\n    border-radius: var(--radius-pill);\n    font-size: var(--text-xs);\n    font-weight: 600;\n    white-space: nowrap;\n    flex: 0 1 auto;\n    min-inline-size: 0;\n    max-inline-size: 100%;\n    overflow: hidden;\n    text-overflow: ellipsis;",
            css);
        Assert.Contains("aria-haspopup", razor);
        Assert.DoesNotContain("application-stepper", css);
        Assert.DoesNotContain("application-card__actions", css);

        var app = File.ReadAllText(Path.Combine(FindRepoRoot(), "Jobsy.Web/Components/App.razor"));
        AssetVersions.AssertVersionedRefMatchesManifest(app, "css/features/applications.css");

        var appCss = File.ReadAllText(Path.Combine(FindRepoRoot(), "Jobsy.Web/wwwroot/css/app.css"));
        Assert.DoesNotContain(".apps-tabs.admin-sublinks", appCss);
        Assert.DoesNotContain(".application-stepper", appCss);
    }

    [Fact]
    public void Match_mobile_card_fills_height_and_clears_bottom_nav()
    {
        var css = File.ReadAllText(Path.Combine(FindRepoRoot(), "Jobsy.Web/wwwroot/css/app.css"));
        // Beat has-bottom-nav 7rem so actions sit ~14px above the nav on mobile only.
        Assert.Contains(
            ".app-shell--match.has-bottom-nav .app-main,\n    .app-shell--match:has(.bottom-nav) .app-main {\n        padding-bottom: calc(4.75rem + 14px + env(safe-area-inset-bottom, 0px));",
            css);
        Assert.Contains(".swipe-card__why {\n        flex: 1 1 auto;", css);
        Assert.Contains(".swipe-card__more {\n        position: static;", css);
        Assert.Contains(".swipe-card__match-hero {\n        width: 100%;", css);
        Assert.Contains("-webkit-line-clamp: 2;", css);
        // Desktop clearance unchanged.
        Assert.Contains(
            ".app-shell--match.has-bottom-nav .app-main,\n    .app-shell--match:has(.bottom-nav) .app-main {\n        padding-bottom: calc(var(--bottom-nav-clearance) + env(safe-area-inset-bottom, 0px));",
            css);
    }

    [Fact]
    public void Candidate_profile_uses_accordions_compact_availability_and_sticky_save()
    {
        var root = FindRepoRoot();
        var razor = File.ReadAllText(Path.Combine(root, "Jobsy.Web/Components/Pages/Candidate/Profile.razor"));
        var sectionsDir = Path.Combine(root, "Jobsy.Web/Components/Candidate/ProfileSections");
        var sections = string.Concat(Directory.EnumerateFiles(sectionsDir, "*.razor").Select(File.ReadAllText));
        var blob = razor + sections;
        Assert.Contains("profile-page--candidate", razor);
        Assert.Contains("profile-accordion", razor);
        Assert.Contains("ToggleSection(\"personal\")", razor);
        Assert.Contains("ToggleSection(\"preferences\")", razor);
        Assert.Contains("ToggleSection(\"availability\")", razor);
        Assert.Contains("ToggleSection(\"experience\")", razor);
        Assert.Contains("competency-profile-card", razor);
        Assert.Contains("CandidateKompas", razor);
        Assert.Contains("profile-layout__matches", razor);
        Assert.DoesNotContain("competency.Scores ?? competency.PreviewScores", razor);
        Assert.Contains("profile-check-grid", blob);
        Assert.Contains("availability-matrix", blob);
        Assert.Contains("availability-presets", blob);
        Assert.Contains("profile-save-bar", blob);
        Assert.Contains("Profile.ReturnHint", razor);
        Assert.Contains("profile-return-hint", razor);
        Assert.Contains("aria-expanded=\"@(IsSectionOpen(\"personal\") ? \"true\" : \"false\")\"", razor);
        Assert.DoesNotContain("vacancy-schedule__table", razor);

        var css = File.ReadAllText(Path.Combine(root, "Jobsy.Web/wwwroot/css/app.css"));
        Assert.Contains(".profile-accordion {\n    display: flex;\n    flex-direction: column;", css);
        Assert.Contains(".profile-page--candidate .profile-check-grid,\n.profile-page--candidate .profile-roles.profile-check-grid {\n    grid-template-columns: repeat(2, minmax(0, 1fr));", css);
        Assert.Contains(".availability-matrix {\n    display: grid;\n    grid-template-columns: 2.35rem repeat(4, minmax(0, 1fr));", css);
        Assert.Contains("bottom: calc(4.75rem + env(safe-area-inset-bottom, 0px));", css);
        Assert.Contains("--profile-save-bar-h: 4.25rem;", css);
        Assert.Contains("4.75rem + var(--profile-save-bar-h)", css);
        Assert.Contains(".profile-save-bar .login-submit {\n    width: 100%;", css);
        Assert.Contains(".profile-page--candidate .profile-contact__names {\n    grid-template-columns: repeat(2, minmax(0, 1fr));", css);
        Assert.Contains(".profile-page--candidate .profile-page__header {\n        display: none;", css);
        // page-in must not leave a transform (fill-mode:both) or fixed Opslaan sticks near the top
        Assert.Contains(
            "animation: lobsy-page-in 220ms cubic-bezier(0.2, 0.8, 0.2, 1) backwards;",
            css);
        Assert.DoesNotContain(
            "animation: lobsy-page-in 220ms cubic-bezier(0.2, 0.8, 0.2, 1) both;",
            css);
    }

    [Fact]
    public void Competency_test_discloses_privacy_and_blocks_save_after_load_failure()
    {
        var test = File.ReadAllText(Path.Combine(FindRepoRoot(), "Jobsy.Web/Components/Pages/Candidate/CompetencyTest.razor"));
        Assert.Contains("Competency.PrivacyNote", test);
        Assert.Contains("PrivacyHref=\"/privacy\"", test);
        Assert.Contains("_loadFailed", test);
        Assert.Contains("QuestionnaireAutosave", test);
        Assert.Contains("LoadFailed=\"_loadFailed\"", test);
        Assert.DoesNotContain("Competency.SaveDraft", test);

        var privacy = File.ReadAllText(Path.Combine(FindRepoRoot(), "Jobsy.Web/Components/Pages/Legal/Privacy.razor"));
        Assert.Contains("5b. Competentietest, beroepentest, diepte-analyse en talentpool", privacy);
        Assert.Contains("niet</strong> aan werkgevers getoond", privacy);
        Assert.Contains("anonieme talentpool", privacy);
        Assert.Contains("Mijn Beroepen-kompas", privacy);
        Assert.Contains("Likert-antwoorden", privacy);
        Assert.Contains("26 september 2026", privacy);
        Assert.Contains("volledige CV-tekst", privacy);
        Assert.Contains("Cursor", privacy);
        Assert.Equal("2026-09-26", Jobsy.Core.Privacy.PrivacyConstants.CurrentConsentVersion);
    }

    [Fact]
    public void Guest_discovery_shows_match_with_login_return_url()
    {
        var discovery = File.ReadAllText(Path.Combine(FindRepoRoot(), "Jobsy.Web/Components/VacancyDiscovery.razor"));
        Assert.Contains("kb-match-button", discovery);
        Assert.Contains("jobsy-action--match", discovery);
        Assert.Contains("MatchButtonHref", discovery);
        Assert.Contains("MatchLoginHref", discovery);
        Assert.Contains("/candidate/match", discovery);
        Assert.Contains("returnUrl=", discovery);
        Assert.Contains("Nav.Match", discovery);
        Assert.DoesNotContain("jobsy-action--save", discovery);
        Assert.DoesNotContain("LikedLoginUrl", discovery);
    }

    [Fact]
    public void Employer_vacancies_use_ent_data_table_with_density()
    {
        var razor = File.ReadAllText(Path.Combine(FindRepoRoot(), "Jobsy.Web/Components/Pages/Werkgever/Vacancies.razor"));
        Assert.Contains("EntDataTable", razor);
        Assert.Contains("wg-vac--dense", razor);
        Assert.Contains("EntBulkBar", razor);
        Assert.Contains("EntTabs", razor);
        Assert.DoesNotContain("vacancy-card-list", razor);
        Assert.DoesNotContain("vacancy-mgmt-card", razor);

        var css = File.ReadAllText(Path.Combine(FindRepoRoot(), "Jobsy.Web/wwwroot/css/features/werkgever.css"));
        Assert.Contains(".wg-vac--dense", css);
        Assert.Contains("height: 44px", css);
    }

    [Fact]
    public void Applicants_availability_renders_a_readonly_matrix_not_raw_day_text()
    {
        var detail = File.ReadAllText(Path.Combine(FindRepoRoot(), "Jobsy.Web/Components/Werkgever/Applications/ApplicationCandidateDetail.razor"));
        Assert.Contains("ParseAvailabilityPayload(item.SnapshotAvailabilityJson)", detail);
        Assert.Contains("WgApp.See.Title", detail);
        Assert.Contains("IsPiiStage", detail);
        Assert.Contains("wg-app-contact", detail);
        Assert.DoesNotContain("contact-icon", detail);

        var rules = File.ReadAllText(Path.Combine(FindRepoRoot(), "Jobsy.Web/Werkgever/ApplicationPipelineRules.cs"));
        Assert.Contains("LobsyCvAccessRules.IsPiiRevealed", rules);
        Assert.Contains("LobsyCvAccessRules.IsDirectContactRevealed", rules);

        var contactModal = File.ReadAllText(Path.Combine(FindRepoRoot(), "Jobsy.Web/Components/DirectContactModal.razor"));
        Assert.Contains("direct-contact-modal__details", contactModal);
        Assert.Contains("FormatDisplayPhone", contactModal);
        Assert.Contains("WhatsAppLabel", contactModal);
    }

    [Fact]
    public void Token_logs_hide_technical_ids_and_show_explicit_token_amounts()
    {
        var mutaties = File.ReadAllText(Path.Combine(FindRepoRoot(), "Jobsy.Web/Components/Pages/Werkgever/TokensMutaties.razor"));
        Assert.Contains("TokenLogPresentation.FormatAmount", mutaties);
        Assert.Contains("TokenLogPresentation.FormatDateShort", mutaties);
        Assert.Contains("TokenLogPresentation.Describe", mutaties);
        Assert.DoesNotContain("@log.Kind / @log.Reason", mutaties);

        var presentation = File.ReadAllText(Path.Combine(FindRepoRoot(), "Jobsy.Web/Tokens/TokenLogPresentation.cs"));
        Assert.Contains("AmountToneClass", presentation);
        Assert.Contains("token-log__amount--in", presentation);
        Assert.Contains("token-log__amount--out", presentation);

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
        Assert.Contains("notification-bell__toggle--unread", bell);
        Assert.Contains(".notification-bell__toggle--unread {\n    color: #c9a227;", css);

        var home = File.ReadAllText(Path.Combine(FindRepoRoot(), "Jobsy.Web/Components/Home/EmployerHomePanel.razor"));
        Assert.DoesNotContain("Download Raamflyer", home);
        Assert.DoesNotContain("Per vestiging", home);
        Assert.DoesNotContain("raamflyer-scope", home);
        Assert.Contains("dashboard-secondary", home);
        Assert.Contains("RaamflyerTools", home);
        Assert.Contains("panel-header__title-row", home);
        Assert.Contains("DashboardRefreshButton", home);

        var company = File.ReadAllText(Path.Combine(FindRepoRoot(), "Jobsy.Web/Components/Pages/Werkgever/Wervingsmateriaal.razor"));
        Assert.Contains("RaamflyerTools", company);
        Assert.Contains("WgNav.RecruitmentMaterials", company);
        Assert.Contains("WgMaterials.Raamflyer", company);

        var branches = File.ReadAllText(Path.Combine(FindRepoRoot(), "Jobsy.Web/Components/Werkgever/Sections/BranchesSection.razor"));
        Assert.Contains("RaamflyerTools", branches);

        var refresh = File.ReadAllText(Path.Combine(FindRepoRoot(), "Jobsy.Web/Components/Shared/DashboardRefreshButton.razor"));
        Assert.Contains("dash-refresh-btn", refresh);
        Assert.Contains("aria-label=\"Ververs\"", refresh);
    }

    [Fact]
    public void Login_is_compact_modern_and_honors_return_aliases()
    {
        var login = File.ReadAllText(Path.Combine(FindRepoRoot(), "Jobsy.Web/Components/Pages/Login.razor"));
        Assert.Contains("ExcludeFromInteractiveRouting", login);
        Assert.Contains("au-card", login);
        Assert.Contains("AuthRedirects.ResolveRequestedReturnUrl", login);
        Assert.Contains("QueryValue(query, \"returnTo\")", login);
        Assert.Contains("QueryValue(query, \"redirect\")", login);
        Assert.Contains("name=\"returnUrl\"", login);
        Assert.Contains("/account/external/entra?returnUrl=", login);
        Assert.Contains("/account/external/google?returnUrl=", login);
        Assert.DoesNotContain("role=\"dialog\"", login);
        Assert.DoesNotContain("login-modal--compact", login);
        Assert.Contains("Login.CreateAccountCta", login);
        Assert.Contains("NavigateTo(_returnUrl", login);
        Assert.Contains("PublicLayout", login);

        var css = File.ReadAllText(Path.Combine(FindRepoRoot(), "Jobsy.Web/wwwroot/css/features/auth.css"));
        Assert.Contains(".pub-theme .au-card", css);
        Assert.Contains("max-width: 460px", css);

        var header = File.ReadAllText(Path.Combine(FindRepoRoot(), "Jobsy.Web/Components/Layout/AuthHeader.razor"));
        Assert.Contains("IsLoginRoute", header);
        Assert.Contains("href=\"/login\"", header);

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
        Assert.Contains("FeedbackHost", header);
        Assert.Contains("Feedback.Button", header);
        Assert.Contains("OpenFeedbackAsync", header);

        var feedback = File.ReadAllText(Path.Combine(FindRepoRoot(), "Jobsy.Web/Components/Feedback/FeedbackWidget.razor"));
        Assert.Contains("feedback-widget--tab", feedback);
        Assert.Contains("feedback-widget__tab", feedback);
        Assert.Contains("feedback-widget__tab--edge", feedback);
        Assert.Contains("FeedbackHost", feedback);
        Assert.DoesNotContain("Feedback.CaptureFailed", feedback);

        var css = File.ReadAllText(Path.Combine(FindRepoRoot(), "Jobsy.Web/wwwroot/css/app.css"));
        Assert.Contains(".lobsy-assistant-tab {\n    position: fixed;\n    top: 50%;\n    right: 0;", css);
        Assert.Contains(".lobsy-assistant-tab__btn {\n    display: inline-flex;\n    align-items: center;\n    gap: 0.35rem;\n    writing-mode: vertical-rl;", css);
        Assert.Contains(".feedback-widget {\n    position: fixed;\n    top: 46%;\n    right: 0;", css);
        Assert.Contains(".feedback-widget__tab {\n    writing-mode: vertical-rl;", css);
        Assert.Contains(".lobsy-assistant-tab.has-bottom-nav {\n        top: auto;\n        bottom: calc(4.75rem + env(safe-area-inset-bottom, 0px));", css);
        Assert.Contains(".feedback-widget__tab--edge {\n        display: none !important;", css);
        Assert.DoesNotContain(".lobsy-assistant-tab--edge,\n    .feedback-widget__tab--edge {\n        display: none !important;", css);
        Assert.Contains("button.bottom-nav__item {", css);
        Assert.DoesNotContain(".bottom-nav__item--assistant {", css);
        Assert.Contains(".pb-28 { padding-bottom: 7rem; }", css);
        Assert.Contains(".overflow-x-hidden { overflow-x: hidden; }", css);
        Assert.Contains("flex: 1 1 0;", css);
        Assert.Contains(".bottom-nav {\n    position: fixed;\n    bottom: 0;\n    left: 0;\n    right: 0;\n    width: 100%;", css);

        var program = File.ReadAllText(Path.Combine(FindRepoRoot(), "Jobsy.Web/Program.cs"));
        Assert.Contains("AddScoped<Jobsy.Web.Navigation.AssistantChatHost>()", program);
        Assert.Contains("AddScoped<Jobsy.Web.Navigation.FeedbackHost>()", program);
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

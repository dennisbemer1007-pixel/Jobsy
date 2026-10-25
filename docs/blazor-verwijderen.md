# Blazor verwijderen — inventaris

Doel: `blazor.web.js`, SignalR-circuits en enhanced navigation uitfaseren ten gunste van SSR + gewone JS (in-page wissels zonder volledige reload). Uiterlijk blijft gelijk.

## Status werkgever na-de-match (wave 0 — afgerond op acceptatie)

| Item | Detail |
|------|--------|
| Acceptatie SHA | `a55a6ee09` (merge #1051) |
| PR's | #1045 (enhanced-nav fix), #1046–#1051 (demo-seed + ensure BFF) |
| Gedrag | Geen `blazor.web.js` op `/werkgever/sollicitaties/*/after-match*`; `data-enhance-nav="false"`; panel-JS met `enhancedload`-herstel |
| Acc-test | `NdmEmployerPanelPlaywrightTests` met `JOBSY_NDM_EMPLOYER_ACC=1` en `JOBSY_E2E_BASE_URL=https://acceptatie.lobsy.nl` |

## Globale infrastructuur (nog Blazor)

| Locatie | Wat |
|---------|-----|
| `Jobsy.Web/Components/App.razor` | `LoadBlazorRuntime` → preload + `blazor.web.js` + `blazor-boot.js`; `PageRenderMode` → `InteractiveServerRenderMode`; enhanced-nav inline guard |
| `Jobsy.Web/Program.cs` | `AddInteractiveServerRenderMode`, circuit / SignalR |
| `Jobsy.Web/wwwroot/js/blazor-boot.js` | Handmatige `Blazor.start`, reconnect, circuit lifecycle |
| `Jobsy.Web/Components/Layout/MainLayout.razor` | `_noBlazorRuntime` gate; anders volledige app-shell met interactieve chrome |
| `Jobsy.Web/Components/Layout/CircuitErrorBoundary.razor` | Circuit error UI |
| `Jobsy.Web/Applications/AfterMatchWerkgeverSsrPaths.cs` | **Al** Blazor uit voor werkgever after-match SSR-paden |

**Telling routes:** 308 `@page`-routes totaal — **44** met `[NoBlazorRuntime]`, **264** laden nog `blazor.web.js` (tenzij crawler).

**Bestanden met expliciet `@rendermode InteractiveServer`:** 176 (zie appendix).

## Al SSR / geen Blazor-runtime (`[NoBlazorRuntime]`)

| Route | Bestand |
|-------|---------|
| `/` | `Jobsy.Web/Components/Pages/Landing.razor` |
| `/Error` | `Jobsy.Web/Components/Pages/Error.razor` |
| `/access-denied` | `Jobsy.Web/Components/Pages/AccessDenied.razor` |
| `/account-maken` | `Jobsy.Web/Components/Pages/Public/AccountMaken.razor` |
| `/account-maken/code` | `Jobsy.Web/Components/Pages/Public/AccountMakenCode.razor` |
| `/algemene-voorwaarden` | `Jobsy.Web/Components/Pages/Legal/AlgemeneVoorwaarden.razor` |
| `/banenkaart` | `Jobsy.Web/Components/Pages/BanenkaartSsr.razor` |
| `/bronnen` | `Jobsy.Web/Components/Pages/Legal/Bronnen.razor` |
| `/candidate/applications` | `Jobsy.Web/Components/Pages/Candidate/CandidateApplicationsSsr.razor` |
| `/candidate/applications/{ApplicationId:guid}/after-match/bevestiging-ssr` | `Jobsy.Web/Components/Pages/Candidate/CandidateAfterMatchBevestigingSsr.razor` |
| `/candidate/applications/{ApplicationId:guid}/after-match/confirmed-ssr` | `Jobsy.Web/Components/Pages/Candidate/CandidateAfterMatchConfirmedSsr.razor` |
| `/candidate/applications/{ApplicationId:guid}/after-match/herinneringen-ssr` | `Jobsy.Web/Components/Pages/Candidate/CandidateAfterMatchHerinneringenSsr.razor` |
| `/candidate/applications/{ApplicationId:guid}/after-match/messages-ssr` | `Jobsy.Web/Components/Pages/Candidate/CandidateAfterMatchMessagesSsr.razor` |
| `/candidate/applications/{ApplicationId:guid}/after-match/outcome-ssr` | `Jobsy.Web/Components/Pages/Candidate/CandidateAfterMatchPanelSsr.razor` |
| `/candidate/applications/{ApplicationId:guid}/after-match/panel-ssr` | `Jobsy.Web/Components/Pages/Candidate/CandidateAfterMatchPanelSsr.razor` |
| `/candidate/applications/{ApplicationId:guid}/after-match/pick-slot-ssr` | `Jobsy.Web/Components/Pages/Candidate/CandidateAfterMatchPickSlotSsr.razor` |
| `/candidate/applications/{ApplicationId:guid}/after-match/reschedule-ssr` | `Jobsy.Web/Components/Pages/Candidate/CandidateAfterMatchRescheduleSsr.razor` |
| `/candidate/kompas` | `Jobsy.Web/Components/Pages/Candidate/CandidateKompasSsr.razor` |
| `/candidate/liked` | `Jobsy.Web/Components/Pages/Candidate/CandidateLikedSsr.razor` |
| `/candidate/ontdekkingsreis` | `Jobsy.Web/Components/Pages/Candidate/CandidateDiscoverySsr.razor` |
| `/candidate/paspoort` | `Jobsy.Web/Components/Pages/Candidate/CandidatePassportSsr.razor` |
| `/candidate/start` | `Jobsy.Web/Components/Pages/Candidate/OnboardingStartSsr.razor` |
| `/candidate/start/kompas` | `Jobsy.Web/Components/Pages/Candidate/OnboardingKompasReadySsr.razor` |
| `/carriere` | `Jobsy.Web/Components/Pages/Candidate/CandidateCareerSsr.razor` |
| `/contact` | `Jobsy.Web/Components/Pages/Legal/Contact.razor` |
| `/gebruiksvoorwaarden` | `Jobsy.Web/Components/Pages/Legal/Gebruiksvoorwaarden.razor` |
| `/hoe-werkt-lobsy` | `Jobsy.Web/Components/Pages/HowLobsyWorks.razor` |
| `/info/onderzoek` | `Jobsy.Web/Components/Pages/Legal/Onderzoek.razor` |
| `/login` | `Jobsy.Web/Components/Pages/Login.razor` |
| `/partner` | `Jobsy.Web/Components/Pages/Partner/PartnerSales.razor` |
| `/partner/{TrackingCode?}` | `Jobsy.Web/Components/Pages/Partner/PartnerSales.razor` |
| `/preview/na-de-match/{ScreenId}` | `Jobsy.Web/Components/Pages/__Preview/NaDeMatchAccPreviewScreens.razor` |
| `/privacy` | `Jobsy.Web/Components/Pages/Legal/Privacy.razor` |
| `/scholen` | `Jobsy.Web/Components/Pages/ScholenPublic.razor` |
| `/status/{Code:int}` | `Jobsy.Web/Components/Pages/Status/StatusPage.razor` |
| `/toegankelijkheid` | `Jobsy.Web/Components/Pages/Legal/Toegankelijkheid.razor` |
| `/werkgever/sollicitaties/{ApplicationId:guid}/after-match/cancel-ssr` | `Jobsy.Web/Components/Pages/Werkgever/WerkgeverAfterMatchCancelSsr.razor` |
| `/werkgever/sollicitaties/{ApplicationId:guid}/after-match/messages-ssr` | `Jobsy.Web/Components/Pages/Werkgever/WerkgeverAfterMatchMessagesSsr.razor` |
| `/werkgever/sollicitaties/{ApplicationId:guid}/after-match/outcome-ssr` | `Jobsy.Web/Components/Pages/Werkgever/WerkgeverAfterMatchOutcomeSsr.razor` |
| `/werkgever/sollicitaties/{ApplicationId:guid}/after-match/panel-ssr` | `Jobsy.Web/Components/Pages/Werkgever/WerkgeverAfterMatchPanelSsr.razor` |
| `/werkgever/sollicitaties/{ApplicationId:guid}/after-match/propose-ssr` | `Jobsy.Web/Components/Pages/Werkgever/WerkgeverAfterMatchProposeSsr.razor` |
| `/werkgever/sollicitaties/{ApplicationId:guid}/after-match/reschedule-ssr` | `Jobsy.Web/Components/Pages/Werkgever/WerkgeverAfterMatchRescheduleSsr.razor` |
| `/werkgevers/binnenkort` | `Jobsy.Web/Components/Pages/WerkgeversBinnenkort.razor` |
| `/wie-zijn-wij` | `Jobsy.Web/Components/Pages/Legal/WieZijnWij.razor` |

## Nog Blazor — per gebied (routes)

Kolommen: `@onclick` / `EditForm` tellen server-event handlers in de page-razor (niet meegetelde child components).

### `__test` (2 routes)

| Route | onclick | EditForm | rendermode | Bestand |
|-------|---------|----------|------------|---------|
| `/__test/capture-test-block` | 0 | — | ja | `Jobsy.Web/Components/Pages/__Test/CaptureTestBlockPreview.razor` |
| `/__test/pwa-vacancy-apply-success` | 0 | — | ja | `Jobsy.Web/Components/Pages/__Test/PwaVacancyApplySuccessPreview.razor` |

### `account` (6 routes)

| Route | onclick | EditForm | rendermode | Bestand |
|-------|---------|----------|------------|---------|
| `/account/mail-instellingen` | 0 | — | ja | `Jobsy.Web/Components/Pages/Account/MailSettings.razor` |
| `/account/mfa` | 0 | — | — | `Jobsy.Web/Components/Pages/Account/MfaPrompt.razor` |
| `/account/mfa/herstelcodes-vernieuwen` | 0 | — | — | `Jobsy.Web/Components/Pages/Account/MfaRegenerateRecoveryCodes.razor` |
| `/account/mfa/recovery-codes` | 0 | — | — | `Jobsy.Web/Components/Pages/Account/MfaRecoveryCodes.razor` |
| `/account/mfa/setup` | 0 | — | — | `Jobsy.Web/Components/Pages/Account/MfaSetup.razor` |
| `/account/wachtwoord-instellen` | 0 | — | — | `Jobsy.Web/Components/Pages/Account/SetPassword.razor` |

### `admin` (68 routes)

| Route | onclick | EditForm | rendermode | Bestand |
|-------|---------|----------|------------|---------|
| `/admin` | 0 | — | ja | `Jobsy.Web/Components/Pages/Admin/AdminDashboard.razor` |
| `/admin/about` | 0 | — | ja | `Jobsy.Web/Components/Pages/Admin/AdminLegacyRedirect.razor` |
| `/admin/ambassadeurs` | 0 | — | ja | `Jobsy.Web/Components/Pages/Admin/AdminLegacyRedirect.razor` |
| `/admin/api-keys` | 0 | — | ja | `Jobsy.Web/Components/Pages/Admin/AdminLegacyRedirect.razor` |
| `/admin/ats-vacancies` | 0 | — | ja | `Jobsy.Web/Components/Pages/Admin/AdminLegacyRedirect.razor` |
| `/admin/beveiliging` | 5 | — | ja | `Jobsy.Web/Components/Pages/Admin/AuditLogAdmin.razor` |
| `/admin/beveiliging/2fa` | 1 | — | ja | `Jobsy.Web/Components/Pages/Admin/MfaSessionsAdmin.razor` |
| `/admin/beveiliging/gegevensinzage` | 3 | — | ja | `Jobsy.Web/Components/Pages/Admin/PersonalDataAccessLogAdmin.razor` |
| `/admin/beveiliging/privacy` | 0 | — | ja | `Jobsy.Web/Components/Pages/Admin/PrivacyAdmin.razor` |
| `/admin/beveiliging/referent-misbruik` | 1 | — | ja | `Jobsy.Web/Components/Pages/Admin/ReferenceMisuseAdmin.razor` |
| `/admin/beveiliging/systeemlogs` | 4 | — | ja | `Jobsy.Web/Components/Pages/Admin/LoggingAdmin.razor` |
| `/admin/cnames` | 0 | — | ja | `Jobsy.Web/Components/Pages/Admin/AdminLegacyRedirect.razor` |
| `/admin/cockpit` | 0 | — | ja | `Jobsy.Web/Components/Pages/Admin/AdminLegacyRedirect.razor` |
| `/admin/companies` | 0 | — | ja | `Jobsy.Web/Components/Pages/Admin/AdminLegacyRedirect.razor` |
| `/admin/company` | 0 | — | ja | `Jobsy.Web/Components/Pages/Admin/AdminLegacyRedirect.razor` |
| `/admin/content/dag-in-het-leven` | 6 | — | ja | `Jobsy.Web/Components/Pages/Admin/OccupationDayAdmin.razor` |
| `/admin/content/emails` | 8 | — | ja | `Jobsy.Web/Components/Pages/Admin/MailTestAdmin.razor` |
| `/admin/content/opleidingen` | 5 | — | ja | `Jobsy.Web/Components/Pages/Admin/TrainingAdmin.razor` |
| `/admin/content/paginas` | 0 | — | ja | `Jobsy.Web/Components/Pages/Admin/PaginasFlyerPage.razor` |
| `/admin/content/stamgegevens` | 0 | — | ja | `Jobsy.Web/Components/Pages/Admin/StamgegevensPage.razor` |
| `/admin/exclusivity` | 0 | — | ja | `Jobsy.Web/Components/Pages/Admin/AdminLegacyRedirect.razor` |
| `/admin/feedback` | 6 | — | ja | `Jobsy.Web/Components/Pages/Admin/FeedbackAdmin.razor` |
| `/admin/finance` | 0 | — | ja | `Jobsy.Web/Components/Pages/Admin/AdminLegacyRedirect.razor` |
| `/admin/financien` | 5 | — | ja | `Jobsy.Web/Components/Pages/Admin/FinanceAdmin.razor` |
| `/admin/financien/goodwill` | 0 | — | ja | `Jobsy.Web/Components/Pages/Admin/TokenAdmin.razor` |
| `/admin/financien/prijzen` | 0 | — | ja | `Jobsy.Web/Components/Pages/Admin/PricesPage.razor` |
| `/admin/financien/uitbetalingen` | 7 | — | ja | `Jobsy.Web/Components/Pages/Admin/TokenFinanceAdmin.razor` |
| `/admin/gebruikers` | 0 | — | ja | `Jobsy.Web/Components/Pages/Admin/UsersAdmin.razor` |
| `/admin/gebruikers/rollen` | 0 | — | ja | `Jobsy.Web/Components/Pages/Admin/RolesAdmin.razor` |
| `/admin/gebruikers/sales` | 0 | — | ja | `Jobsy.Web/Components/Pages/Admin/SalesAmbassadeursPage.razor` |
| `/admin/instellingen` | 0 | — | ja | `Jobsy.Web/Components/Pages/Admin/SettingsAdmin.razor` |
| `/admin/instellingen/algemeen` | 0 | — | ja | `Jobsy.Web/Components/Pages/Admin/CompanySettingsAdmin.razor` |
| `/admin/instellingen/integraties` | 0 | — | ja | `Jobsy.Web/Components/Pages/Admin/IntegratiesApiPage.razor` |
| `/admin/integrations` | 0 | — | ja | `Jobsy.Web/Components/Pages/Admin/AdminLegacyRedirect.razor` |
| `/admin/kandidaten` | 0 | — | ja | `Jobsy.Web/Components/Pages/Admin/CandidatesAdmin.razor` |
| `/admin/logging` | 0 | — | ja | `Jobsy.Web/Components/Pages/Admin/AdminLegacyRedirect.razor` |
| `/admin/mail-test` | 0 | — | ja | `Jobsy.Web/Components/Pages/Admin/AdminLegacyRedirect.razor` |
| `/admin/marketing-flyer` | 0 | — | ja | `Jobsy.Web/Components/Pages/Admin/AdminLegacyRedirect.razor` |
| `/admin/masterdata` | 0 | — | ja | `Jobsy.Web/Components/Pages/Admin/AdminLegacyRedirect.razor` |
| `/admin/match-calibration` | 0 | — | ja | `Jobsy.Web/Components/Pages/Admin/MatchCalibrationAdmin.razor` |
| `/admin/moderation` | 0 | — | ja | `Jobsy.Web/Components/Pages/Admin/AdminLegacyRedirect.razor` |
| `/admin/notifications` | 0 | — | ja | `Jobsy.Web/Components/Pages/Admin/AdminLegacyRedirect.razor` |
| `/admin/organisaties` | 7 | — | ja | `Jobsy.Web/Components/Pages/Admin/CompaniesAdmin.razor` |
| `/admin/organisaties/aanvragen` | 5 | — | ja | `Jobsy.Web/Components/Pages/Admin/OrganisationRequestsAdmin.razor` |
| `/admin/organisaties/regios` | 0 | — | ja | `Jobsy.Web/Components/Pages/Admin/CnamesAdmin.razor` |
| `/admin/paspoortpartners` | 5 | — | ja | `Jobsy.Web/Components/Pages/Admin/PassportPartnersAdmin.razor` |
| `/admin/personal-data-access-log` | 0 | — | ja | `Jobsy.Web/Components/Pages/Admin/AdminLegacyRedirect.razor` |
| `/admin/research` | 2 | — | ja | `Jobsy.Web/Components/Pages/Admin/ResearchAdmin.razor` |
| `/admin/sales` | 0 | — | ja | `Jobsy.Web/Components/Pages/Admin/AdminLegacyRedirect.razor` |
| `/admin/sales-managers` | 0 | — | ja | `Jobsy.Web/Components/Pages/Admin/AdminLegacyRedirect.razor` |
| `/admin/scholen` | 2 | ja | ja | `Jobsy.Web/Components/Pages/Admin/Scholen/ScholenList.razor` |
| `/admin/scholen/rapportage` | 3 | — | ja | `Jobsy.Web/Components/Pages/Admin/Scholen/ScholenRapportage.razor` |
| `/admin/scholen/{SchoolId:guid}` | 5 | — | ja | `Jobsy.Web/Components/Pages/Admin/Scholen/ScholenDetail.razor` |
| `/admin/settings` | 0 | — | ja | `Jobsy.Web/Components/Pages/Admin/AdminLegacyRedirect.razor` |
| `/admin/takenlijst` | 10 | — | ja | `Jobsy.Web/Components/Pages/Admin/BeheerTodosAdmin.razor` |
| `/admin/te-doen` | 6 | — | ja | `Jobsy.Web/Components/Pages/Admin/TodoAdmin.razor` |
| `/admin/token-finance` | 0 | — | ja | `Jobsy.Web/Components/Pages/Admin/AdminLegacyRedirect.razor` |
| `/admin/tokens` | 0 | — | ja | `Jobsy.Web/Components/Pages/Admin/AdminLegacyRedirect.razor` |
| `/admin/training` | 0 | — | ja | `Jobsy.Web/Components/Pages/Admin/AdminLegacyRedirect.razor` |
| `/admin/users` | 0 | — | ja | `Jobsy.Web/Components/Pages/Admin/AdminLegacyRedirect.razor` |
| `/admin/vacancies` | 0 | — | ja | `Jobsy.Web/Components/Pages/Admin/AdminLegacyRedirect.razor` |
| `/admin/vacancy-categories` | 0 | — | ja | `Jobsy.Web/Components/Pages/Admin/AdminLegacyRedirect.razor` |
| `/admin/vacatures` | 0 | — | ja | `Jobsy.Web/Components/Pages/Admin/VacanciesAdmin.razor` |
| `/admin/vacatures/ats` | 19 | — | ja | `Jobsy.Web/Components/Pages/Admin/AtsVacanciesAdmin.razor` |
| `/admin/vacatures/categorieen` | 0 | — | ja | `Jobsy.Web/Components/Pages/Admin/CategorieenSalarisPage.razor` |
| `/admin/vacatures/moderatie` | 2 | — | ja | `Jobsy.Web/Components/Pages/Admin/VacanciesModerationPage.razor` |
| `/admin/wages` | 0 | — | ja | `Jobsy.Web/Components/Pages/Admin/AdminLegacyRedirect.razor` |
| `/admin/werkgeververificatie` | 29 | — | ja | `Jobsy.Web/Components/Pages/Admin/WerkgeverVerificatieAdmin.razor` |

### `ambassadeur` (6 routes)

| Route | onclick | EditForm | rendermode | Bestand |
|-------|---------|----------|------------|---------|
| `/ambassadeur` | 0 | — | — | `Jobsy.Web/Components/Pages/Ambassadeur/Dashboard.razor` |
| `/ambassadeur/finance` | 4 | — | ja | `Jobsy.Web/Components/Pages/Ambassadeur/Finance.razor` |
| `/ambassadeur/onboarding` | 1 | — | ja | `Jobsy.Web/Components/Pages/Ambassadeur/Onboarding.razor` |
| `/ambassadeur/payout-checkout` | 0 | — | ja | `Jobsy.Web/Components/Pages/Ambassadeur/PayoutCheckoutStub.razor` |
| `/ambassadeur/ref/{TrackingCode}` | 0 | — | — | `Jobsy.Web/Components/Pages/Ambassadeur/Landing.razor` |
| `/ambassadeur/toolkit` | 3 | — | ja | `Jobsy.Web/Components/Pages/Ambassadeur/Toolkit.razor` |

### `bedrijvenkaart` (1 routes)

| Route | onclick | EditForm | rendermode | Bestand |
|-------|---------|----------|------------|---------|
| `/bedrijvenkaart` | 0 | — | ja | `Jobsy.Web/Components/Pages/Bedrijvenkaart.razor` |

### `branch` (6 routes)

| Route | onclick | EditForm | rendermode | Bestand |
|-------|---------|----------|------------|---------|
| `/branch` | 0 | — | — | `Jobsy.Web/Components/Pages/Werkgever/WerkgeverLegacyRedirect.razor` |
| `/branch/applicants` | 0 | — | — | `Jobsy.Web/Components/Pages/Werkgever/WerkgeverLegacyRedirect.razor` |
| `/branch/culture` | 0 | — | — | `Jobsy.Web/Components/Pages/Werkgever/WerkgeverLegacyRedirect.razor` |
| `/branch/tokens` | 0 | — | — | `Jobsy.Web/Components/Pages/Werkgever/WerkgeverLegacyRedirect.razor` |
| `/branch/vacancies` | 0 | — | — | `Jobsy.Web/Components/Pages/Werkgever/WerkgeverLegacyRedirect.razor` |
| `/branch/vacancies/new` | 0 | — | — | `Jobsy.Web/Components/Pages/Werkgever/WerkgeverLegacyRedirect.razor` |

### `candidate` (42 routes)

| Route | onclick | EditForm | rendermode | Bestand |
|-------|---------|----------|------------|---------|
| `/candidate/actions/set-unavailable` | 1 | — | ja | `Jobsy.Web/Components/Pages/Candidate/SetUnavailableAction.razor` |
| `/candidate/actions/withdraw-others` | 1 | — | ja | `Jobsy.Web/Components/Pages/Candidate/WithdrawOthersAction.razor` |
| `/candidate/adviseur-werk` | 0 | — | ja | `Jobsy.Web/Components/Pages/Candidate/AdviseurWerk.razor` |
| `/candidate/applications/{ApplicationId:guid}/after-match/bevestiging` | 0 | — | ja | `Jobsy.Web/Components/Pages/Candidate/CandidateAfterMatch.razor` |
| `/candidate/applications/{ApplicationId:guid}/after-match/blazor` | 0 | — | ja | `Jobsy.Web/Components/Pages/Candidate/CandidateAfterMatch.razor` |
| `/candidate/applications/{ApplicationId:guid}/after-match/herinneringen` | 0 | — | ja | `Jobsy.Web/Components/Pages/Candidate/CandidateAfterMatch.razor` |
| `/candidate/beroep/{EscoId:guid}/dag` | 0 | — | ja | `Jobsy.Web/Components/Pages/Candidate/OccupationDayPage.razor` |
| `/candidate/binnenkort` | 0 | — | ja | `Jobsy.Web/Components/Pages/Candidate/CandidateBinnenkort.razor` |
| `/candidate/career` | 2 | — | ja | `Jobsy.Web/Components/Pages/Candidate/CareerTest.razor` |
| `/candidate/competencies` | 2 | — | ja | `Jobsy.Web/Components/Pages/Candidate/CompetencyTest.razor` |
| `/candidate/culture` | 2 | — | ja | `Jobsy.Web/Components/Pages/Candidate/CultureScan.razor` |
| `/candidate/deep-analysis` | 3 | — | ja | `Jobsy.Web/Components/Pages/Candidate/DeepAnalysis.razor` |
| `/candidate/deep-analysis/checkout` | 1 | — | ja | `Jobsy.Web/Components/Pages/Candidate/DeepAnalysisCheckout.razor` |
| `/candidate/deep-analysis/{Kind}` | 3 | — | ja | `Jobsy.Web/Components/Pages/Candidate/DeepAnalysis.razor` |
| `/candidate/disc` | 2 | — | ja | `Jobsy.Web/Components/Pages/Candidate/CultureScan.razor` |
| `/candidate/external/add` | 1 | — | ja | `Jobsy.Web/Components/Pages/Candidate/ExternalVacancyAdd.razor` |
| `/candidate/external/{Id:guid}` | 4 | — | ja | `Jobsy.Web/Components/Pages/Candidate/ExternalVacancyDetail.razor` |
| `/candidate/external/{Id:guid}/apply` | 1 | — | ja | `Jobsy.Web/Components/Pages/Candidate/ExternalVacancyApply.razor` |
| `/candidate/gevoelig-demo` | 0 | — | ja | `Jobsy.Web/Components/Pages/Candidate/GevoeligDemo.razor` |
| `/candidate/gevoelig-demo/{Screen}` | 0 | — | ja | `Jobsy.Web/Components/Pages/Candidate/GevoeligDemo.razor` |
| `/candidate/hoe-werkt-lobsy` | 2 | — | ja | `Jobsy.Web/Components/Pages/Candidate/HowLobsyWorks.razor` |
| `/candidate/home` | 0 | — | — | `Jobsy.Web/Components/Pages/Candidate/CandidateHomeRedirect.razor` |
| `/candidate/kandidatenkaart-instellingen` | 3 | — | ja | `Jobsy.Web/Components/Pages/Candidate/CandidateKandidatenkaartSettings.razor` |
| `/candidate/match` | 1 | — | ja | `Jobsy.Web/Components/Pages/Candidate/MatchPage.razor` |
| `/candidate/ondernemerscheck` | 3 | — | ja | `Jobsy.Web/Components/Pages/Candidate/OndernemersCheck.razor` |
| `/candidate/paspoort/referentie/vraag/{EntryId:guid}` | 1 | — | ja | `Jobsy.Web/Components/Pages/Candidate/ReferenceVerificationRequestPage.razor` |
| `/candidate/paspoort/referenties/{RequestId:guid}` | 0 | — | ja | `Jobsy.Web/Components/Pages/Candidate/ReferenceVerificationDetailPage.razor` |
| `/candidate/privacy` | 0 | — | ja | `Jobsy.Web/Components/Pages/Candidate/CandidatePrivacy.razor` |
| `/candidate/proactive-matching` | 5 | — | ja | `Jobsy.Web/Components/Pages/Candidate/ProactiveMatching.razor` |
| `/candidate/profile` | 4 | — | ja | `Jobsy.Web/Components/Pages/Candidate/Profile.razor` |
| `/candidate/research-consent` | 0 | — | ja | `Jobsy.Web/Components/Pages/Candidate/ResearchConsentFinish.razor` |
| `/candidate/settings` | 2 | — | ja | `Jobsy.Web/Components/Pages/Candidate/CandidateSettings.razor` |
| `/candidate/share-in` | 0 | — | ja | `Jobsy.Web/Components/Pages/Candidate/ShareIn.razor` |
| `/candidate/shared` | 0 | — | ja | `Jobsy.Web/Components/Pages/Candidate/Shared.razor` |
| `/candidate/start/nearby` | 0 | — | — | `Jobsy.Web/Components/Pages/Candidate/OnboardingStartNearbyRedirect.razor` |
| `/candidate/start/wizard` | 17 | — | ja | `Jobsy.Web/Components/Pages/Candidate/OnboardingWizard.razor` |
| `/candidate/talent-contacts` | 0 | — | ja | `Jobsy.Web/Components/Pages/Candidate/CandidateTalentContacts.razor` |
| `/candidate/tests` | 0 | — | — | `Jobsy.Web/Components/Pages/Candidate/TestsIndexRedirect.razor` |
| `/candidate/uren` | 1 | — | ja | `Jobsy.Web/Components/Pages/Candidate/MaqqieHours.razor` |
| `/candidate/vacancies` | 3 | — | ja | `Jobsy.Web/Components/Pages/Candidate/Vacancies.razor` |
| `/candidate/values` | 2 | — | ja | `Jobsy.Web/Components/Pages/Candidate/ValuesScan.razor` |
| `/candidate/wat-drijft-jou` | 0 | — | ja | `Jobsy.Web/Components/Pages/Candidate/WatDrijftJou.razor` |

### `carriere` (1 routes)

| Route | onclick | EditForm | rendermode | Bestand |
|-------|---------|----------|------------|---------|
| `/carriere/leren` | 7 | — | ja | `Jobsy.Web/Components/Pages/Candidate/CareerLearnGrow.razor` |

### `deel` (1 routes)

| Route | onclick | EditForm | rendermode | Bestand |
|-------|---------|----------|------------|---------|
| `/deel/uwv-adviseur/{Token}` | 1 | — | — | `Jobsy.Web/Components/Pages/Public/UwvAdviseurDeel.razor` |

### `dev` (1 routes)

| Route | onclick | EditForm | rendermode | Bestand |
|-------|---------|----------|------------|---------|
| `/dev/na-de-match/{ScreenId}` | 0 | — | ja | `Jobsy.Web/Components/Pages/Dev/NaDeMatchPreview.razor` |

### `dna` (1 routes)

| Route | onclick | EditForm | rendermode | Bestand |
|-------|---------|----------|------------|---------|
| `/dna` | 9 | — | ja | `Jobsy.Web/Components/Pages/Public/GratisDna.razor` |

### `employer` (18 routes)

| Route | onclick | EditForm | rendermode | Bestand |
|-------|---------|----------|------------|---------|
| `/employer/branches` | 0 | — | — | `Jobsy.Web/Components/Pages/Werkgever/WerkgeverLegacyRedirect.razor` |
| `/employer/company` | 0 | — | — | `Jobsy.Web/Components/Pages/Werkgever/WerkgeverLegacyRedirect.razor` |
| `/employer/csv-import` | 0 | — | — | `Jobsy.Web/Components/Pages/Werkgever/WerkgeverLegacyRedirect.razor` |
| `/employer/culture` | 0 | — | — | `Jobsy.Web/Components/Pages/Werkgever/WerkgeverLegacyRedirect.razor` |
| `/employer/kandidaatinzichten` | 0 | — | — | `Jobsy.Web/Components/Pages/Werkgever/WerkgeverLegacyRedirect.razor` |
| `/employer/onboarding-checkout` | 2 | — | ja | `Jobsy.Web/Components/Pages/Employer/OnboardingCheckout.razor` |
| `/employer/organization` | 0 | — | — | `Jobsy.Web/Components/Pages/Werkgever/WerkgeverLegacyRedirect.razor` |
| `/employer/regions` | 0 | — | — | `Jobsy.Web/Components/Pages/Werkgever/WerkgeverLegacyRedirect.razor` |
| `/employer/salary-tables` | 0 | — | — | `Jobsy.Web/Components/Pages/Werkgever/WerkgeverLegacyRedirect.razor` |
| `/employer/salary-tables/{TableId:guid}` | 0 | — | — | `Jobsy.Web/Components/Pages/Werkgever/WerkgeverLegacyRedirect.razor` |
| `/employer/sales` | 0 | — | — | `Jobsy.Web/Components/Pages/Werkgever/WerkgeverLegacyRedirect.razor` |
| `/employer/sales/payout-checkout` | 0 | — | — | `Jobsy.Web/Components/Pages/Werkgever/WerkgeverLegacyRedirect.razor` |
| `/employer/takeovers` | 0 | — | — | `Jobsy.Web/Components/Pages/Werkgever/WerkgeverLegacyRedirect.razor` |
| `/employer/talent` | 0 | — | — | `Jobsy.Web/Components/Pages/Werkgever/WerkgeverLegacyRedirect.razor` |
| `/employer/talent-contacts` | 0 | — | — | `Jobsy.Web/Components/Pages/Werkgever/WerkgeverLegacyRedirect.razor` |
| `/employer/tokens` | 0 | — | — | `Jobsy.Web/Components/Pages/Werkgever/WerkgeverLegacyRedirect.razor` |
| `/employer/users` | 0 | — | — | `Jobsy.Web/Components/Pages/Werkgever/WerkgeverLegacyRedirect.razor` |
| `/employer/vacancies` | 0 | — | — | `Jobsy.Web/Components/Pages/Werkgever/WerkgeverLegacyRedirect.razor` |

### `home` (2 routes)

| Route | onclick | EditForm | rendermode | Bestand |
|-------|---------|----------|------------|---------|
| `/home` | 0 | — | ja | `Jobsy.Web/Components/Pages/RoleHome.razor` |
| `/home/metrics/{Key}` | 1 | — | ja | `Jobsy.Web/Components/Pages/MetricDrilldownPage.razor` |

### `intermediary` (2 routes)

| Route | onclick | EditForm | rendermode | Bestand |
|-------|---------|----------|------------|---------|
| `/intermediary` | 1 | — | ja | `Jobsy.Web/Components/Pages/Intermediary/IntermediaryDashboard.razor` |
| `/intermediary/team` | 1 | — | ja | `Jobsy.Web/Components/Pages/Intermediary/Team.razor` |

### `koppeling` (1 routes)

| Route | onclick | EditForm | rendermode | Bestand |
|-------|---------|----------|------------|---------|
| `/koppeling/sleutel` | 0 | — | — | `Jobsy.Web/Components/Pages/Public/ApiKeyReveal.razor` |

### `lancering` (1 routes)

| Route | onclick | EditForm | rendermode | Bestand |
|-------|---------|----------|------------|---------|
| `/lancering` | 0 | — | ja | `Jobsy.Web/Components/Pages/WestlandTeaser.razor` |

### `leerling` (8 routes)

| Route | onclick | EditForm | rendermode | Bestand |
|-------|---------|----------|------------|---------|
| `/leerling` | 0 | — | — | `Jobsy.Web/Components/Pages/Leerling/LeerlingLogin.razor` |
| `/leerling/dit-ben-jij` | 1 | — | — | `Jobsy.Web/Components/Pages/Leerling/LeerlingDitBenJij.razor` |
| `/leerling/droombaan` | 2 | — | — | `Jobsy.Web/Components/Pages/Leerling/LeerlingDroombaan.razor` |
| `/leerling/eiland` | 6 | — | — | `Jobsy.Web/Components/Pages/Leerling/LeerlingEiland.razor` |
| `/leerling/reis` | 5 | — | — | `Jobsy.Web/Components/Pages/Leerling/LeerlingReis.razor` |
| `/leerling/start` | 0 | — | — | `Jobsy.Web/Components/Pages/Leerling/LeerlingStart.razor` |
| `/leerling/stop` | 0 | — | — | `Jobsy.Web/Components/Pages/Leerling/LeerlingStop.razor` |
| `/leerling/vo-paspoort` | 1 | — | — | `Jobsy.Web/Components/Pages/Leerling/LeerlingVoPaspoort.razor` |

### `leraar` (8 routes)

| Route | onclick | EditForm | rendermode | Bestand |
|-------|---------|----------|------------|---------|
| `/leraar` | 0 | — | ja | `Jobsy.Web/Components/Pages/Leraar/LeraarDashboard.razor` |
| `/leraar/klas/{ClassId:guid}` | 3 | — | ja | `Jobsy.Web/Components/Pages/Leraar/LeraarKlasOverview.razor` |
| `/leraar/klas/{ClassId:guid}/code/{CodeId:guid}` | 1 | — | ja | `Jobsy.Web/Components/Pages/Leraar/LeraarCodeDetail.razor` |
| `/leraar/klas/{ClassId:guid}/codes` | 10 | — | ja | `Jobsy.Web/Components/Pages/Leraar/LeraarCodes.razor` |
| `/leraar/klas/{ClassId:guid}/droombanen` | 0 | — | ja | `Jobsy.Web/Components/Pages/Leraar/LeraarDreamJobs.razor` |
| `/leraar/klas/{ClassId:guid}/groep` | 0 | — | ja | `Jobsy.Web/Components/Pages/Leraar/LeraarGroup.razor` |
| `/leraar/klas/{ClassId:guid}/materiaal` | 2 | — | ja | `Jobsy.Web/Components/Pages/Leraar/LeraarMaterials.razor` |
| `/leraar/klas/{ClassId:guid}/testvenster` | 2 | — | ja | `Jobsy.Web/Components/Pages/Leraar/LeraarTestWindow.razor` |

### `mail` (1 routes)

| Route | onclick | EditForm | rendermode | Bestand |
|-------|---------|----------|------------|---------|
| `/mail/afmelden` | 0 | — | — | `Jobsy.Web/Components/Pages/Public/MailUnsubscribe.razor` |

### `melden` (1 routes)

| Route | onclick | EditForm | rendermode | Bestand |
|-------|---------|----------|------------|---------|
| `/melden` | 0 | — | — | `Jobsy.Web/Components/Pages/Public/Melden.razor` |

### `notifications` (1 routes)

| Route | onclick | EditForm | rendermode | Bestand |
|-------|---------|----------|------------|---------|
| `/notifications` | 0 | — | ja | `Jobsy.Web/Components/Pages/Notifications.razor` |

### `ontdek` (1 routes)

| Route | onclick | EditForm | rendermode | Bestand |
|-------|---------|----------|------------|---------|
| `/ontdek` | 9 | — | ja | `Jobsy.Web/Components/Pages/Public/GratisDna.razor` |

### `pilot` (1 routes)

| Route | onclick | EditForm | rendermode | Bestand |
|-------|---------|----------|------------|---------|
| `/pilot/{Code}` | 2 | — | ja | `Jobsy.Web/Components/Pages/Public/PilotTest.razor` |

### `preview` (3 routes)

| Route | onclick | EditForm | rendermode | Bestand |
|-------|---------|----------|------------|---------|
| `/preview/bedrijvenkaart/{Screen}` | 0 | — | ja | `Jobsy.Web/Components/Pages/__Preview/BedrijvenkaartScreens.razor` |
| `/preview/uwv-mockups` | 0 | — | ja | `Jobsy.Web/Components/Pages/__Preview/UwvMockupScreens.razor` |
| `/preview/uwv-mockups/{Screen}` | 0 | — | ja | `Jobsy.Web/Components/Pages/__Preview/UwvMockupScreens.razor` |

### `privacy` (1 routes)

| Route | onclick | EditForm | rendermode | Bestand |
|-------|---------|----------|------------|---------|
| `/privacy/data` | 1 | — | ja | `Jobsy.Web/Components/Pages/Legal/PrivacyData.razor` |

### `profiel` (3 routes)

| Route | onclick | EditForm | rendermode | Bestand |
|-------|---------|----------|------------|---------|
| `/profiel` | 0 | — | ja | `Jobsy.Web/Components/Pages/Candidate/CandidateProfile.razor` |
| `/profiel/tests` | 0 | — | — | `Jobsy.Web/Components/Pages/Candidate/TestsIndexRedirect.razor` |
| `/profiel/tests/{TestKey}` | 4 | — | ja | `Jobsy.Web/Components/Pages/Candidate/TestDetail.razor` |

### `referentie` (1 routes)

| Route | onclick | EditForm | rendermode | Bestand |
|-------|---------|----------|------------|---------|
| `/referentie/{Token}` | 0 | — | — | `Jobsy.Web/Components/Pages/Public/ReferenceConfirmationPage.razor` |

### `regional` (3 routes)

| Route | onclick | EditForm | rendermode | Bestand |
|-------|---------|----------|------------|---------|
| `/regional` | 0 | — | — | `Jobsy.Web/Components/Pages/Werkgever/WerkgeverLegacyRedirect.razor` |
| `/regional/branches` | 0 | — | — | `Jobsy.Web/Components/Pages/Werkgever/WerkgeverLegacyRedirect.razor` |
| `/regional/tokens` | 0 | — | — | `Jobsy.Web/Components/Pages/Werkgever/WerkgeverLegacyRedirect.razor` |

### `register` (7 routes)

| Route | onclick | EditForm | rendermode | Bestand |
|-------|---------|----------|------------|---------|
| `/register` | 0 | — | ja | `Jobsy.Web/Components/Pages/Register.razor` |
| `/register/bedrijf` | 8 | — | ja | `Jobsy.Web/Components/Pages/RegisterBedrijf.razor` |
| `/register/externe-sollicitatie` | 2 | — | ja | `Jobsy.Web/Components/Pages/ExternalEmployerRegister.razor` |
| `/register/koppelen` | 0 | — | — | `Jobsy.Web/Components/Pages/RegisterKoppelen.razor` |
| `/register/toegang` | 3 | — | ja | `Jobsy.Web/Components/Pages/RegisterToegang.razor` |
| `/register/verifieren` | 10 | — | ja | `Jobsy.Web/Components/Pages/RegisterVerifieren.razor` |
| `/register/verifieren/brief` | 2 | — | ja | `Jobsy.Web/Components/Pages/RegisterVerifierenBrief.razor` |

### `sales` (11 routes)

| Route | onclick | EditForm | rendermode | Bestand |
|-------|---------|----------|------------|---------|
| `/sales` | 5 | — | ja | `Jobsy.Web/Components/Pages/Sales/Dashboard.razor` |
| `/sales/aanbevelen` | 0 | — | ja | `Jobsy.Web/Components/Pages/Sales/Recommend.razor` |
| `/sales/aanbevelen/bezwaar` | 0 | — | — | `Jobsy.Web/Components/Pages/Sales/RecommendObject.razor` |
| `/sales/hulp` | 2 | — | ja | `Jobsy.Web/Components/Pages/Sales/Help.razor` |
| `/sales/link` | 18 | — | ja | `Jobsy.Web/Components/Pages/Sales/Link.razor` |
| `/sales/profiel` | 12 | — | ja | `Jobsy.Web/Components/Pages/Sales/Profile.razor` |
| `/sales/profiel/iban-bevestigen` | 0 | — | ja | `Jobsy.Web/Components/Pages/Sales/IbanConfirm.razor` |
| `/sales/start` | 7 | — | ja | `Jobsy.Web/Components/Pages/Sales/Start.razor` |
| `/sales/wallet` | 9 | — | ja | `Jobsy.Web/Components/Pages/Sales/Wallet.razor` |
| `/sales/wallet/uitbetalen` | 9 | — | ja | `Jobsy.Web/Components/Pages/Sales/Wallet.razor` |
| `/sales/werkgevers` | 1 | — | ja | `Jobsy.Web/Components/Pages/Sales/Employers.razor` |

### `salesmanager` (6 routes)

| Route | onclick | EditForm | rendermode | Bestand |
|-------|---------|----------|------------|---------|
| `/salesmanager` | 0 | — | — | `Jobsy.Web/Components/Pages/Sales/LegacySalesmanagerRedirect.razor` |
| `/salesmanager/invoices` | 0 | — | — | `Jobsy.Web/Components/Pages/Sales/LegacyInvoicesRedirect.razor` |
| `/salesmanager/onboarding` | 0 | — | — | `Jobsy.Web/Components/Pages/Sales/LegacyOnboardingRedirect.razor` |
| `/salesmanager/payout-checkout` | 0 | — | — | `Jobsy.Web/Components/Pages/Sales/LegacyPayoutRedirect.razor` |
| `/salesmanager/referrals` | 0 | — | — | `Jobsy.Web/Components/Pages/Sales/LegacyReferralsRedirect.razor` |
| `/salesmanager/toolkit` | 0 | — | — | `Jobsy.Web/Components/Pages/Sales/LegacyToolkitRedirect.razor` |

### `school` (9 routes)

| Route | onclick | EditForm | rendermode | Bestand |
|-------|---------|----------|------------|---------|
| `/school` | 1 | — | ja | `Jobsy.Web/Components/Pages/School/SchoolDashboard.razor` |
| `/school/gegevens` | 0 | — | ja | `Jobsy.Web/Components/Pages/School/SchoolDetails.razor` |
| `/school/klassen` | 3 | ja | ja | `Jobsy.Web/Components/Pages/School/SchoolClasses.razor` |
| `/school/klassen/{ClassId:guid}` | 11 | ja | ja | `Jobsy.Web/Components/Pages/School/SchoolClassDetail.razor` |
| `/school/leraren` | 8 | — | ja | `Jobsy.Web/Components/Pages/School/SchoolTeachers.razor` |
| `/school/materiaal` | 0 | — | ja | `Jobsy.Web/Components/Pages/School/SchoolMaterials.razor` |
| `/school/privacy` | 2 | — | ja | `Jobsy.Web/Components/Pages/School/SchoolPrivacy.razor` |
| `/school/resultaten` | 3 | — | ja | `Jobsy.Web/Components/Pages/School/SchoolResults.razor` |
| `/school/te-doen` | 0 | — | ja | `Jobsy.Web/Components/Pages/School/SchoolTodos.razor` |

### `toestemming` (1 routes)

| Route | onclick | EditForm | rendermode | Bestand |
|-------|---------|----------|------------|---------|
| `/toestemming` | 0 | — | — | `Jobsy.Web/Components/Pages/Public/ParentalConsent.razor` |

### `tokens` (2 routes)

| Route | onclick | EditForm | rendermode | Bestand |
|-------|---------|----------|------------|---------|
| `/tokens/checkout-return` | 1 | — | ja | `Jobsy.Web/Components/Pages/TokensCheckoutReturn.razor` |
| `/tokens/checkout-stub` | 1 | — | ja | `Jobsy.Web/Components/Pages/TokensCheckoutStub.razor` |

### `vacancies` (1 routes)

| Route | onclick | EditForm | rendermode | Bestand |
|-------|---------|----------|------------|---------|
| `/vacancies/{Id:guid}` | 12 | — | ja | `Jobsy.Web/Components/Pages/VacancyDetail.razor` |

### `vestiging` (1 routes)

| Route | onclick | EditForm | rendermode | Bestand |
|-------|---------|----------|------------|---------|
| `/vestiging/{CompanyId:guid}` | 0 | — | ja | `Jobsy.Web/Components/Pages/VestigingLanding.razor` |

### `wachtwoord-vergeten` (1 routes)

| Route | onclick | EditForm | rendermode | Bestand |
|-------|---------|----------|------------|---------|
| `/wachtwoord-vergeten` | 0 | — | — | `Jobsy.Web/Components/Pages/Account/ForgotPassword.razor` |

### `werk-referentie` (1 routes)

| Route | onclick | EditForm | rendermode | Bestand |
|-------|---------|----------|------------|---------|
| `/werk-referentie/{Token}` | 12 | ja | ja | `Jobsy.Web/Components/Pages/Public/WorkExperienceReferencePublicPage.razor` |

### `werkgever` (29 routes)

| Route | onclick | EditForm | rendermode | Bestand |
|-------|---------|----------|------------|---------|
| `/werkgever` | 4 | — | ja | `Jobsy.Web/Components/Pages/Werkgever/WerkgeverDashboard.razor` |
| `/werkgever/kandidaatinzichten` | 10 | — | ja | `Jobsy.Web/Components/Pages/Werkgever/CandidateInsights.razor` |
| `/werkgever/kandidatenkaart` | 20 | — | ja | `Jobsy.Web/Components/Pages/Werkgever/Kandidatenkaart.razor` |
| `/werkgever/koppelingen` | 0 | — | ja | `Jobsy.Web/Components/Pages/Werkgever/Koppelingen.razor` |
| `/werkgever/meldingen` | 0 | — | ja | `Jobsy.Web/Components/Pages/Werkgever/Meldingen.razor` |
| `/werkgever/open-voor-initiatief` | 2 | — | ja | `Jobsy.Web/Components/Pages/Werkgever/OpenForInitiative.razor` |
| `/werkgever/organisatie/profiel` | 0 | — | ja | `Jobsy.Web/Components/Pages/Werkgever/CompanyProfile.razor` |
| `/werkgever/organisatie/salaristabellen` | 9 | — | ja | `Jobsy.Web/Components/Pages/Werkgever/SalaryTables.razor` |
| `/werkgever/organisatie/salaristabellen/{TableId:guid}` | 9 | — | ja | `Jobsy.Web/Components/Pages/Werkgever/SalaryTables.razor` |
| `/werkgever/organisatie/team` | 5 | — | ja | `Jobsy.Web/Components/Pages/Werkgever/Users.razor` |
| `/werkgever/organisatie/vestigingen` | 7 | — | ja | `Jobsy.Web/Components/Pages/Werkgever/BranchesRegions.razor` |
| `/werkgever/overnames` | 4 | — | ja | `Jobsy.Web/Components/Pages/Werkgever/Takeovers.razor` |
| `/werkgever/partner` | 2 | — | ja | `Jobsy.Web/Components/Pages/Werkgever/PartnerSales.razor` |
| `/werkgever/partner/uitbetalen` | 0 | — | ja | `Jobsy.Web/Components/Pages/Werkgever/PartnerSalesPayoutCheckoutStub.razor` |
| `/werkgever/sollicitaties` | 19 | — | ja | `Jobsy.Web/Components/Pages/Werkgever/Applicants.razor` |
| `/werkgever/sollicitaties/initiatieven` | 0 | — | ja | `Jobsy.Web/Components/Pages/Werkgever/InitiativeApplications.razor` |
| `/werkgever/sollicitaties/{ApplicationId:guid}` | 4 | — | ja | `Jobsy.Web/Components/Pages/Werkgever/ApplicationCandidate.razor` |
| `/werkgever/sollicitaties/{ApplicationId:guid}/after-match/blazor` | 0 | — | ja | `Jobsy.Web/Components/Pages/Werkgever/WerkgeverAfterMatch.razor` |
| `/werkgever/talentpool` | 0 | — | ja | `Jobsy.Web/Components/Pages/Werkgever/TalentPool.razor` |
| `/werkgever/te-doen` | 2 | — | ja | `Jobsy.Web/Components/Pages/Werkgever/TeDoen.razor` |
| `/werkgever/tokens` | 11 | — | ja | `Jobsy.Web/Components/Pages/Werkgever/Tokens.razor` |
| `/werkgever/tokens/facturen` | 1 | — | ja | `Jobsy.Web/Components/Pages/Werkgever/TokensFacturen.razor` |
| `/werkgever/tokens/mutaties` | 1 | — | ja | `Jobsy.Web/Components/Pages/Werkgever/TokensMutaties.razor` |
| `/werkgever/tokens/verbruik` | 6 | — | ja | `Jobsy.Web/Components/Pages/Werkgever/TokensVerbruik.razor` |
| `/werkgever/uren` | 4 | — | ja | `Jobsy.Web/Components/Pages/Werkgever/MaqqieHoursApprove.razor` |
| `/werkgever/vacatures` | 21 | — | ja | `Jobsy.Web/Components/Pages/Werkgever/Vacancies.razor` |
| `/werkgever/vacatures/nieuw` | 0 | — | ja | `Jobsy.Web/Components/Pages/Werkgever/CreateVacancy.razor` |
| `/werkgever/vacatures/{VacancyId:guid}/proactieve-shortlist` | 0 | — | ja | `Jobsy.Web/Components/Pages/Werkgever/ProactiveVacancyShortlistPage.razor` |
| `/werkgever/wervingsmateriaal` | 1 | — | ja | `Jobsy.Web/Components/Pages/Werkgever/Wervingsmateriaal.razor` |

### `werven` (1 routes)

| Route | onclick | EditForm | rendermode | Bestand |
|-------|---------|----------|------------|---------|
| `/werven/{TrackingCode}` | 0 | — | — | `Jobsy.Web/Components/Pages/Ambassadeur/Landing.razor` |

### `westland` (1 routes)

| Route | onclick | EditForm | rendermode | Bestand |
|-------|---------|----------|------------|---------|
| `/westland` | 0 | — | ja | `Jobsy.Web/Components/Pages/WestlandTeaser.razor` |

### `{KvkNumber:regex(^\\d{{8}}$)}` (2 routes)

| Route | onclick | EditForm | rendermode | Bestand |
|-------|---------|----------|------------|---------|
| `/{KvkNumber:regex(^\\d{{8}}$)}` | 0 | — | ja | `Jobsy.Web/Components/Pages/CompanyPublicPage.razor` |
| `/{KvkNumber:regex(^\\d{{8}}$)}/{Vestigingsnummer:regex(^\\d{{1,12}}$)}` | 0 | — | ja | `Jobsy.Web/Components/Pages/CompanyPublicPage.razor` |

## Gedeelde components & layout (circuits / @onclick)

Top interactieve **niet-page** components (sorteer: onclick + forms):

| Bestand | onclick | EditForm | @bind | rendermode |
|---------|---------|----------|-------|------------|
| `Jobsy.Web/Components/VacancyDiscovery.razor` | 26 | 0 | 5 | — |
| `Jobsy.Web/Components/Admin/Sections/UsersAdminSection.razor` | 24 | 0 | 16 | — |
| `Jobsy.Web/Components/Admin/Sections/VacanciesAdminSection.razor` | 24 | 0 | 7 | — |
| `Jobsy.Web/Components/Admin/IntegrationSettingsTile.razor` | 17 | 0 | 29 | — |
| `Jobsy.Web/Components/Werkgever/VacancyCreateWizard.razor` | 17 | 0 | 39 | — |
| `Jobsy.Web/Components/Werkgever/Sections/CompanyDetailsSection.razor` | 16 | 0 | 8 | ja |
| `Jobsy.Web/Components/Candidate/Passport/PassportProofTab.razor` | 12 | 0 | 0 | — |
| `Jobsy.Web/Components/Candidate/Onboarding/DreamJobStep.razor` | 11 | 0 | 4 | — |
| `Jobsy.Web/Components/Werkgever/VacancyWizard/VacancyRichTextEditor.razor` | 11 | 0 | 0 | — |
| `Jobsy.Web/Components/Candidate/Career/CareerOverviewCard.razor` | 10 | 0 | 0 | — |
| `Jobsy.Web/Components/Layout/NotificationBell.razor` | 10 | 0 | 0 | — |
| `Jobsy.Web/Components/Admin/Sections/CnamesSection.razor` | 9 | 0 | 10 | — |
| `Jobsy.Web/Components/AfterMatch/NdMSchedulingPanel.razor` | 9 | 0 | 1 | — |
| `Jobsy.Web/Components/Candidate/Passport/PassportCard.razor` | 9 | 0 | 0 | — |
| `Jobsy.Web/Components/KandidaatBanen/KbActiveFilterTags.razor` | 9 | 0 | 0 | — |
| `Jobsy.Web/Components/Public/GratisDnaResultView.razor` | 9 | 0 | 0 | — |
| `Jobsy.Web/Components/Admin/Sections/PricingSettingsSection.razor` | 8 | 0 | 22 | — |
| `Jobsy.Web/Components/Candidate/CandidateKompas.razor` | 8 | 0 | 2 | — |
| `Jobsy.Web/Components/Candidate/Discovery/JourneyOverview.razor` | 8 | 0 | 0 | — |
| `Jobsy.Web/Components/Discovery/VacancyCard.razor` | 8 | 0 | 0 | — |
| `Jobsy.Web/Components/Admin/Sales/PayoutRunDetailDrawer.razor` | 7 | 0 | 1 | — |
| `Jobsy.Web/Components/Admin/Sections/PlatformSettingsEditor.razor` | 7 | 0 | 4 | — |
| `Jobsy.Web/Components/Candidate/ProfileSections/ConsentSection.razor` | 7 | 0 | 1 | — |
| `Jobsy.Web/Components/Candidate/ProfileSections/EducationSection.razor` | 7 | 0 | 11 | — |
| `Jobsy.Web/Components/Admin/Sections/MasterdataSection.razor` | 6 | 0 | 11 | — |
| `Jobsy.Web/Components/Admin/Sections/SalesCommercialSection.razor` | 6 | 0 | 19 | — |
| `Jobsy.Web/Components/Admin/Sections/SalesManagersSection.razor` | 6 | 0 | 2 | — |
| `Jobsy.Web/Components/Bedrijvenkaart/InitiativeEmployerSollicitatiesPanel.razor` | 6 | 0 | 0 | — |
| `Jobsy.Web/Components/Bedrijvenkaart/InitiativeKnockPanel.razor` | 6 | 0 | 2 | — |
| `Jobsy.Web/Components/Candidate/ProfileSections/AvailabilitySection.razor` | 6 | 0 | 2 | — |
| `Jobsy.Web/Components/KandidaatBanen/KbListRow.razor` | 6 | 0 | 0 | — |
| `Jobsy.Web/Components/LobsyAssistantChat.razor` | 6 | 0 | 2 | — |
| `Jobsy.Web/Components/Registration/RegisterWizard.razor` | 6 | 0 | 4 | — |
| `Jobsy.Web/Components/School/TestWindowDialog.razor` | 6 | 0 | 1 | — |
| `Jobsy.Web/Components/Admin/Sections/MarketingFlyerSection.razor` | 5 | 0 | 11 | — |
| `Jobsy.Web/Components/Admin/Sections/VacancyCategoriesSection.razor` | 5 | 0 | 19 | — |
| `Jobsy.Web/Components/AfterMatch/NdMEmployerProposePanel.razor` | 5 | 0 | 3 | — |
| `Jobsy.Web/Components/Bedrijvenkaart/CompanyMapDiscovery.razor` | 5 | 0 | 0 | — |
| `Jobsy.Web/Components/Bedrijvenkaart/InitiativeEmployerAcceptDialog.razor` | 5 | 0 | 0 | — |
| `Jobsy.Web/Components/Bedrijvenkaart/InitiativeEmployerDeclineDialog.razor` | 5 | 0 | 1 | — |
| `Jobsy.Web/Components/Bedrijvenkaart/InitiativeTeamAddDialog.razor` | 5 | 0 | 3 | — |
| `Jobsy.Web/Components/Candidate/FitPercentInfo.razor` | 5 | 0 | 0 | — |
| `Jobsy.Web/Components/Candidate/TestResult/TestResultRedoDialog.razor` | 5 | 0 | 0 | — |
| `Jobsy.Web/Components/Candidate/TestResult/TestResultSamplePdfDialog.razor` | 5 | 0 | 0 | — |
| `Jobsy.Web/Components/Layout/AuthHeader.razor` | 5 | 0 | 0 | — |
| `Jobsy.Web/Components/Layout/SalesLayout.razor` | 5 | 0 | 0 | — |
| `Jobsy.Web/Components/Registration/WaStepScope.razor` | 5 | 0 | 0 | — |
| `Jobsy.Web/Components/Shared/Questionnaire/TestQuestionFlow.razor` | 5 | 0 | 0 | — |
| `Jobsy.Web/Components/Shared/RowActionsMenu.razor` | 5 | 0 | 0 | — |
| `Jobsy.Web/Components/UnsubscribeDialog.razor` | 5 | 0 | 4 | — |
| `Jobsy.Web/Components/Admin/Sections/AmbassadeursSection.razor` | 4 | 0 | 6 | — |
| `Jobsy.Web/Components/Admin/Sections/ContentReportsAdminSection.razor` | 4 | 0 | 3 | — |
| `Jobsy.Web/Components/Admin/Sections/FinancePayoutsSection.razor` | 4 | 0 | 0 | — |
| `Jobsy.Web/Components/Admin/Sections/MaintenancePanel.razor` | 4 | 0 | 0 | — |
| `Jobsy.Web/Components/AfterMatch/NdMOutcomePanel.razor` | 4 | 0 | 1 | — |
| `Jobsy.Web/Components/AfterMatch/NdMReminderPanel.razor` | 4 | 0 | 0 | — |
| `Jobsy.Web/Components/Candidate/Career/CareerDreamPicker.razor` | 4 | 0 | 0 | — |
| `Jobsy.Web/Components/Candidate/Career/CareerStepDetailCard.razor` | 4 | 0 | 0 | — |
| `Jobsy.Web/Components/Candidate/DnaPanel.razor` | 4 | 0 | 0 | — |
| `Jobsy.Web/Components/Candidate/Passport/PassportDataTab.razor` | 4 | 0 | 0 | — |
| `Jobsy.Web/Components/Candidate/Passport/PassportDnaTab.razor` | 4 | 0 | 0 | — |
| `Jobsy.Web/Components/Candidate/Passport/SharedWithEmployersSection.razor` | 4 | 0 | 0 | — |
| `Jobsy.Web/Components/Candidate/ProfileSections/ExperienceSection.razor` | 4 | 0 | 2 | — |
| `Jobsy.Web/Components/Employer/CompanyVerificationBanner.razor` | 4 | 0 | 0 | — |
| `Jobsy.Web/Components/Feedback/FeedbackWidget.razor` | 4 | 0 | 3 | — |
| `Jobsy.Web/Components/Match/MatchDeckDialog.razor` | 4 | 0 | 0 | — |
| `Jobsy.Web/Components/Match/SwipeCard.razor` | 4 | 0 | 0 | — |
| `Jobsy.Web/Components/Registration/WaStepAccount.razor` | 4 | 0 | 9 | — |
| `Jobsy.Web/Components/Shared/Questionnaire/QuestionnaireShell.razor` | 4 | 0 | 0 | — |
| `Jobsy.Web/Components/Werkgever/Insights/WgInsightsPremium.razor` | 4 | 0 | 0 | — |
| `Jobsy.Web/Components/Werkgever/Org/WgOrgTree.razor` | 4 | 0 | 0 | — |
| `Jobsy.Web/Components/Werkgever/Sections/CsvImportSection.razor` | 4 | 0 | 15 | ja |
| `Jobsy.Web/Components/Admin/CnameSetupHelpPanel.razor` | 3 | 0 | 0 | — |
| `Jobsy.Web/Components/Admin/Sales/LedgerCorrectionDialog.razor` | 3 | 0 | 2 | — |
| `Jobsy.Web/Components/Admin/Sales/PayoutRunsSection.razor` | 3 | 0 | 0 | — |
| `Jobsy.Web/Components/Admin/Sections/ExclusivitySection.razor` | 3 | 0 | 17 | — |
| `Jobsy.Web/Components/Admin/Shell/AdminTopBar.razor` | 3 | 0 | 0 | — |
| `Jobsy.Web/Components/AfterMatch/NdMWeekCalendar.razor` | 3 | 0 | 0 | — |
| `Jobsy.Web/Components/Bedrijvenkaart/InitiativeWeekLimitDialog.razor` | 3 | 0 | 0 | — |
| `Jobsy.Web/Components/Candidate/Career/CareerArchivedPlans.razor` | 3 | 0 | 0 | — |
| … | | | | | *(+179 bestanden)* |

### Layout / chrome (impact op elke Blazor-pagina)

- `Jobsy.Web/Components/Layout/MainLayout.razor`: onclick=0, EditForm=0, rendermode=nee
- `Jobsy.Web/Components/Layout/AuthHeader.razor`: onclick=5, EditForm=0, rendermode=nee
- `Jobsy.Web/Components/Layout/NotificationBell.razor`: onclick=10, EditForm=0, rendermode=nee
- `Jobsy.Web/Components/Layout/BottomNav.razor`: onclick=0, EditForm=0, rendermode=ja
- `Jobsy.Web/Components/Layout/CandidateHeaderNav.razor`: onclick=0, EditForm=0, rendermode=ja
- `Jobsy.Web/Components/Layout/LanguageSelector.razor`: onclick=1, EditForm=0, rendermode=nee
- `Jobsy.Web/Components/Layout/SalesLayout.razor`: onclick=5, EditForm=0, rendermode=nee
- `Jobsy.Web/Components/Layout/AdminLayout.razor`: onclick=3, EditForm=0, rendermode=nee
- `Jobsy.Web/Components/Layout/NoBlazorCandidateNav.razor`: onclick=0, EditForm=0, rendermode=nee

## Voorgestelde migratie-golven (kleine PR's)

### Wave 1 — Werkgever rest
- **Scope:** /werkgever/* behalve after-match SSR
- **Notities:** Sollicitaties-lijst, kandidaat-detail (`ApplicationCandidate.razor`), dashboard, vacatures — na after-match fix

### Wave 2 — Kandidaat sollicitaties & after-match
- **Scope:** /candidate/applications*, after-match blazor routes
- **Notities:** `Applications.razor` → SSR (deels al `CandidateApplicationsSsr`); `CandidateAfterMatch.razor` / Blazor panels → SSR+JS zoals werkgever

### Wave 3 — Banenkaart & discovery
- **Scope:** banenkaart, bewaard, discovery
- **Notities:** `BanenkaartSsr` al SSR; `VacancyDiscovery`, liked/saved flows

### Wave 4 — Paspoort / profiel
- **Scope:** /candidate/paspoort SSR uitbreiden
- **Notities:** `Profile.razor`, passport tabs, tests — zware forms

### Wave 5 — Werkgever kaart & wizard
- **Scope:** MapLibre + forms
- **Notities:** `Kandidatenkaart`, `VacancyCreateWizard`, tokens

### Wave 6 — Admin
- **Scope:** /admin/*
- **Notities:** 68 admin routes, veel tabellen/forms

### Wave 7 — Sales / school / leerling
- **Scope:** apart per rol
- **Notities:** Sales 11, school 9, leerling 8 routes

### Wave 8 — Public register & auth
- **Scope:** account, register
- **Notities:** Register*, MFA SSR deels klaar

### Wave 9 — App shell zonder Blazor
- **Scope:** App.razor + Program.cs cleanup
- **Notities:** Als >90% SSR: `LoadBlazorRuntime` default false, opt-in legacy

## Appendix: alle `@rendermode InteractiveServer` bestanden

- `Jobsy.Web/Components/App.razor`
- `Jobsy.Web/Components/Layout/BottomNav.razor`
- `Jobsy.Web/Components/Layout/CandidateHeaderNav.razor`
- `Jobsy.Web/Components/Layout/CandidateNavLink.razor`
- `Jobsy.Web/Components/Pages/Admin/AdminDashboard.razor`
- `Jobsy.Web/Components/Pages/Admin/AdminLegacyRedirect.razor`
- `Jobsy.Web/Components/Pages/Admin/AtsVacanciesAdmin.razor`
- `Jobsy.Web/Components/Pages/Admin/AuditLogAdmin.razor`
- `Jobsy.Web/Components/Pages/Admin/BeheerTodosAdmin.razor`
- `Jobsy.Web/Components/Pages/Admin/CandidatesAdmin.razor`
- `Jobsy.Web/Components/Pages/Admin/CategorieenSalarisPage.razor`
- `Jobsy.Web/Components/Pages/Admin/CnamesAdmin.razor`
- `Jobsy.Web/Components/Pages/Admin/CompaniesAdmin.razor`
- `Jobsy.Web/Components/Pages/Admin/CompanySettingsAdmin.razor`
- `Jobsy.Web/Components/Pages/Admin/FeedbackAdmin.razor`
- `Jobsy.Web/Components/Pages/Admin/FinanceAdmin.razor`
- `Jobsy.Web/Components/Pages/Admin/IntegratiesApiPage.razor`
- `Jobsy.Web/Components/Pages/Admin/LoggingAdmin.razor`
- `Jobsy.Web/Components/Pages/Admin/MailTestAdmin.razor`
- `Jobsy.Web/Components/Pages/Admin/MatchCalibrationAdmin.razor`
- `Jobsy.Web/Components/Pages/Admin/MfaSessionsAdmin.razor`
- `Jobsy.Web/Components/Pages/Admin/OccupationDayAdmin.razor`
- `Jobsy.Web/Components/Pages/Admin/OrganisationRequestsAdmin.razor`
- `Jobsy.Web/Components/Pages/Admin/PaginasFlyerPage.razor`
- `Jobsy.Web/Components/Pages/Admin/PassportPartnersAdmin.razor`
- `Jobsy.Web/Components/Pages/Admin/PersonalDataAccessLogAdmin.razor`
- `Jobsy.Web/Components/Pages/Admin/PricesPage.razor`
- `Jobsy.Web/Components/Pages/Admin/PrivacyAdmin.razor`
- `Jobsy.Web/Components/Pages/Admin/ReferenceMisuseAdmin.razor`
- `Jobsy.Web/Components/Pages/Admin/ResearchAdmin.razor`
- `Jobsy.Web/Components/Pages/Admin/RolesAdmin.razor`
- `Jobsy.Web/Components/Pages/Admin/SalesAmbassadeursPage.razor`
- `Jobsy.Web/Components/Pages/Admin/Scholen/ScholenDetail.razor`
- `Jobsy.Web/Components/Pages/Admin/Scholen/ScholenList.razor`
- `Jobsy.Web/Components/Pages/Admin/Scholen/ScholenRapportage.razor`
- `Jobsy.Web/Components/Pages/Admin/SettingsAdmin.razor`
- `Jobsy.Web/Components/Pages/Admin/StamgegevensPage.razor`
- `Jobsy.Web/Components/Pages/Admin/TodoAdmin.razor`
- `Jobsy.Web/Components/Pages/Admin/TokenAdmin.razor`
- `Jobsy.Web/Components/Pages/Admin/TokenFinanceAdmin.razor`
- `Jobsy.Web/Components/Pages/Admin/TrainingAdmin.razor`
- `Jobsy.Web/Components/Pages/Admin/UsersAdmin.razor`
- `Jobsy.Web/Components/Pages/Admin/VacanciesAdmin.razor`
- `Jobsy.Web/Components/Pages/Admin/VacanciesModerationPage.razor`
- `Jobsy.Web/Components/Pages/Admin/WerkgeverVerificatieAdmin.razor`
- `Jobsy.Web/Components/Pages/Ambassadeur/Finance.razor`
- `Jobsy.Web/Components/Pages/Ambassadeur/Onboarding.razor`
- `Jobsy.Web/Components/Pages/Ambassadeur/PayoutCheckoutStub.razor`
- `Jobsy.Web/Components/Pages/Ambassadeur/Toolkit.razor`
- `Jobsy.Web/Components/Pages/Bedrijvenkaart.razor`
- `Jobsy.Web/Components/Pages/Candidate/AdviseurWerk.razor`
- `Jobsy.Web/Components/Pages/Candidate/Applications.razor`
- `Jobsy.Web/Components/Pages/Candidate/CandidateAfterMatch.razor`
- `Jobsy.Web/Components/Pages/Candidate/CandidateBinnenkort.razor`
- `Jobsy.Web/Components/Pages/Candidate/CandidateKandidatenkaartSettings.razor`
- `Jobsy.Web/Components/Pages/Candidate/CandidatePrivacy.razor`
- `Jobsy.Web/Components/Pages/Candidate/CandidateProfile.razor`
- `Jobsy.Web/Components/Pages/Candidate/CandidateSettings.razor`
- `Jobsy.Web/Components/Pages/Candidate/CandidateTalentContacts.razor`
- `Jobsy.Web/Components/Pages/Candidate/CareerLearnGrow.razor`
- `Jobsy.Web/Components/Pages/Candidate/CareerTest.razor`
- `Jobsy.Web/Components/Pages/Candidate/CompetencyTest.razor`
- `Jobsy.Web/Components/Pages/Candidate/CultureScan.razor`
- `Jobsy.Web/Components/Pages/Candidate/DeepAnalysis.razor`
- `Jobsy.Web/Components/Pages/Candidate/DeepAnalysisCheckout.razor`
- `Jobsy.Web/Components/Pages/Candidate/DiscoveryJourney.razor`
- `Jobsy.Web/Components/Pages/Candidate/ExternalVacancyAdd.razor`
- `Jobsy.Web/Components/Pages/Candidate/ExternalVacancyApply.razor`
- `Jobsy.Web/Components/Pages/Candidate/ExternalVacancyDetail.razor`
- `Jobsy.Web/Components/Pages/Candidate/GevoeligDemo.razor`
- `Jobsy.Web/Components/Pages/Candidate/HowLobsyWorks.razor`
- `Jobsy.Web/Components/Pages/Candidate/MaqqieHours.razor`
- `Jobsy.Web/Components/Pages/Candidate/MatchPage.razor`
- `Jobsy.Web/Components/Pages/Candidate/OccupationDayPage.razor`
- `Jobsy.Web/Components/Pages/Candidate/OnboardingWizard.razor`
- `Jobsy.Web/Components/Pages/Candidate/OndernemersCheck.razor`
- `Jobsy.Web/Components/Pages/Candidate/ProactiveMatching.razor`
- `Jobsy.Web/Components/Pages/Candidate/Profile.razor`
- `Jobsy.Web/Components/Pages/Candidate/ReferenceVerificationDetailPage.razor`
- `Jobsy.Web/Components/Pages/Candidate/ReferenceVerificationRequestPage.razor`
- `Jobsy.Web/Components/Pages/Candidate/ResearchConsentFinish.razor`
- `Jobsy.Web/Components/Pages/Candidate/ShareIn.razor`
- `Jobsy.Web/Components/Pages/Candidate/Shared.razor`
- `Jobsy.Web/Components/Pages/Candidate/TestDetail.razor`
- `Jobsy.Web/Components/Pages/Candidate/Vacancies.razor`
- `Jobsy.Web/Components/Pages/Candidate/ValuesScan.razor`
- `Jobsy.Web/Components/Pages/Candidate/WatDrijftJou.razor`
- `Jobsy.Web/Components/Pages/CompanyPublicPage.razor`
- `Jobsy.Web/Components/Pages/Dev/NaDeMatchPreview.razor`
- `Jobsy.Web/Components/Pages/Employer/OnboardingCheckout.razor`
- `Jobsy.Web/Components/Pages/ExternalEmployerRegister.razor`
- `Jobsy.Web/Components/Pages/Intermediary/IntermediaryDashboard.razor`
- `Jobsy.Web/Components/Pages/Intermediary/Team.razor`
- `Jobsy.Web/Components/Pages/Legal/PrivacyData.razor`
- `Jobsy.Web/Components/Pages/Leraar/LeraarCodeDetail.razor`
- `Jobsy.Web/Components/Pages/Leraar/LeraarCodes.razor`
- `Jobsy.Web/Components/Pages/Leraar/LeraarDashboard.razor`
- `Jobsy.Web/Components/Pages/Leraar/LeraarDreamJobs.razor`
- `Jobsy.Web/Components/Pages/Leraar/LeraarGroup.razor`
- `Jobsy.Web/Components/Pages/Leraar/LeraarKlasOverview.razor`
- `Jobsy.Web/Components/Pages/Leraar/LeraarMaterials.razor`
- `Jobsy.Web/Components/Pages/Leraar/LeraarTestWindow.razor`
- `Jobsy.Web/Components/Pages/MetricDrilldownPage.razor`
- `Jobsy.Web/Components/Pages/Notifications.razor`
- `Jobsy.Web/Components/Pages/Public/GratisDna.razor`
- `Jobsy.Web/Components/Pages/Public/PilotTest.razor`
- `Jobsy.Web/Components/Pages/Public/WorkExperienceReferencePublicPage.razor`
- `Jobsy.Web/Components/Pages/Register.razor`
- `Jobsy.Web/Components/Pages/RegisterBedrijf.razor`
- `Jobsy.Web/Components/Pages/RegisterToegang.razor`
- `Jobsy.Web/Components/Pages/RegisterVerifieren.razor`
- `Jobsy.Web/Components/Pages/RegisterVerifierenBrief.razor`
- `Jobsy.Web/Components/Pages/RoleHome.razor`
- `Jobsy.Web/Components/Pages/Sales/Dashboard.razor`
- `Jobsy.Web/Components/Pages/Sales/Employers.razor`
- `Jobsy.Web/Components/Pages/Sales/Help.razor`
- `Jobsy.Web/Components/Pages/Sales/IbanConfirm.razor`
- `Jobsy.Web/Components/Pages/Sales/Link.razor`
- `Jobsy.Web/Components/Pages/Sales/Profile.razor`
- `Jobsy.Web/Components/Pages/Sales/Recommend.razor`
- `Jobsy.Web/Components/Pages/Sales/Start.razor`
- `Jobsy.Web/Components/Pages/Sales/Wallet.razor`
- `Jobsy.Web/Components/Pages/School/SchoolClassDetail.razor`
- `Jobsy.Web/Components/Pages/School/SchoolClasses.razor`
- `Jobsy.Web/Components/Pages/School/SchoolDashboard.razor`
- `Jobsy.Web/Components/Pages/School/SchoolDetails.razor`
- `Jobsy.Web/Components/Pages/School/SchoolMaterials.razor`
- `Jobsy.Web/Components/Pages/School/SchoolPrivacy.razor`
- `Jobsy.Web/Components/Pages/School/SchoolResults.razor`
- `Jobsy.Web/Components/Pages/School/SchoolTeachers.razor`
- `Jobsy.Web/Components/Pages/School/SchoolTodos.razor`
- `Jobsy.Web/Components/Pages/TokensCheckoutReturn.razor`
- `Jobsy.Web/Components/Pages/TokensCheckoutStub.razor`
- `Jobsy.Web/Components/Pages/VacancyDetail.razor`
- `Jobsy.Web/Components/Pages/VestigingLanding.razor`
- `Jobsy.Web/Components/Pages/Werkgever/Applicants.razor`
- `Jobsy.Web/Components/Pages/Werkgever/ApplicationCandidate.razor`
- `Jobsy.Web/Components/Pages/Werkgever/BranchesRegions.razor`
- `Jobsy.Web/Components/Pages/Werkgever/CandidateInsights.razor`
- `Jobsy.Web/Components/Pages/Werkgever/CompanyProfile.razor`
- `Jobsy.Web/Components/Pages/Werkgever/CreateVacancy.razor`
- `Jobsy.Web/Components/Pages/Werkgever/InitiativeApplications.razor`
- `Jobsy.Web/Components/Pages/Werkgever/Kandidatenkaart.razor`
- `Jobsy.Web/Components/Pages/Werkgever/Koppelingen.razor`
- `Jobsy.Web/Components/Pages/Werkgever/MaqqieHoursApprove.razor`
- `Jobsy.Web/Components/Pages/Werkgever/Meldingen.razor`
- `Jobsy.Web/Components/Pages/Werkgever/OpenForInitiative.razor`
- `Jobsy.Web/Components/Pages/Werkgever/PartnerSales.razor`
- `Jobsy.Web/Components/Pages/Werkgever/PartnerSalesPayoutCheckoutStub.razor`
- `Jobsy.Web/Components/Pages/Werkgever/ProactiveVacancyShortlistPage.razor`
- `Jobsy.Web/Components/Pages/Werkgever/SalaryTables.razor`
- `Jobsy.Web/Components/Pages/Werkgever/Takeovers.razor`
- `Jobsy.Web/Components/Pages/Werkgever/TalentPool.razor`
- `Jobsy.Web/Components/Pages/Werkgever/TeDoen.razor`
- `Jobsy.Web/Components/Pages/Werkgever/Tokens.razor`
- `Jobsy.Web/Components/Pages/Werkgever/TokensFacturen.razor`
- `Jobsy.Web/Components/Pages/Werkgever/TokensMutaties.razor`
- `Jobsy.Web/Components/Pages/Werkgever/TokensVerbruik.razor`
- `Jobsy.Web/Components/Pages/Werkgever/Users.razor`
- `Jobsy.Web/Components/Pages/Werkgever/Vacancies.razor`
- `Jobsy.Web/Components/Pages/Werkgever/WerkgeverAfterMatch.razor`
- `Jobsy.Web/Components/Pages/Werkgever/WerkgeverDashboard.razor`
- `Jobsy.Web/Components/Pages/Werkgever/Wervingsmateriaal.razor`
- `Jobsy.Web/Components/Pages/WestlandTeaser.razor`
- `Jobsy.Web/Components/Pages/__Preview/BedrijvenkaartScreens.razor`
- `Jobsy.Web/Components/Pages/__Preview/UwvMockupScreens.razor`
- `Jobsy.Web/Components/Pages/__Test/CaptureTestBlockPreview.razor`
- `Jobsy.Web/Components/Pages/__Test/PwaVacancyApplySuccessPreview.razor`
- `Jobsy.Web/Components/Werkgever/Sections/BranchesSection.razor`
- `Jobsy.Web/Components/Werkgever/Sections/CompanyDetailsSection.razor`
- `Jobsy.Web/Components/Werkgever/Sections/CsvImportSection.razor`
- `Jobsy.Web/Components/Werkgever/Sections/CultureScanSection.razor`
- `Jobsy.Web/Components/Werkgever/Sections/EmployerFitTeamCultureSection.razor`
- `Jobsy.Web/Components/Werkgever/Sections/RegionsSection.razor`
- `Jobsy.Web/Components/Werkgever/Sections/TalentContactsSection.razor`
- `Jobsy.Web/Components/Werkgever/Sections/TalentPoolSection.razor`

---
*Gegenereerd door repo-scan (razor `@page`, `[NoBlazorRuntime]`, `@onclick`, `EditForm`, `@rendermode`). Vernieuw na grote refactors.*

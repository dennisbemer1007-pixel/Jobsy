# Blazor routes

Generated from `Jobsy.Web/Components/**/*.razor` `@page` directives and
`[Authorize]` / `[AllowAnonymous]` attributes. **Do not hand-edit the table** —
regenerate with:

```bash
JOBSY_UPDATE_ROUTES_DOC=1 dotnet test Jobsy.Tests/Jobsy.Tests.csproj \
  --filter "FullyQualifiedName~RoutesDocFreshness"
```

Guard: `Jobsy.Tests/RoutesDocFreshnessTests`.

## NL / EN mix

Routes intentionally mix Dutch and English segments (`/profiel`, `/carriere`,
`/banenkaart`, `/hoe-werkt-lobsy`, `/candidate/...`, `/employer/...`, `/vacancies/...`).
**Do not rename routes** for cosmetics — bookmarks, QR landings, and emails depend on them.
Product narrative per role: [`ROLES_AND_VIEWS.md`](../ROLES_AND_VIEWS.md).
Authorization intent: [`security/roles-matrix.md`](security/roles-matrix.md).

## Landing + banenkaart (landing 04–05)

- `/` — public landing page (static SSR, no MapLibre / no Blazor runtime; indexed). Signed-in users are **302** → role home (passport ON default: discovery/paspoort for candidates; passport OFF: `/banenkaart`). Legacy map deep-link query on `/` → **301** `/banenkaart?…`.
- `/banenkaart` — public job map (indexed).
- `/banen` — legacy; **301** → `/banenkaart` (query preserved; middleware, not a Blazor page).
- `/bewaard`, `/candidate/saved`, `/candidate/bewaard` — legacy Bewaard URLs; **302** → `/candidate/liked` (query preserved; `BewaardRedirectMiddleware`).

## Access column

| Value | Meaning |
|-------|---------|
| `anonymous` | `[AllowAnonymous]` |
| `authenticated` | `[Authorize]` without `Roles=` |
| role list | `[Authorize(Roles="…")]` (Jobsy role claim names) |
| `any (no Authorize attribute)` | No attribute — Web has no FallbackPolicy |

## Table (232 routes)

| Route | Component | Access |
|-------|-----------|--------|
| `/` | `Pages/Landing.razor` | anonymous |
| `/access-denied` | `Pages/AccessDenied.razor` | anonymous |
| `/account-maken` | `Pages/Public/AccountMaken.razor` | anonymous |
| `/account-maken/code` | `Pages/Public/AccountMakenCode.razor` | anonymous |
| `/account/mail-instellingen` | `Pages/Account/MailSettings.razor` | authenticated |
| `/account/mfa` | `Pages/Account/MfaPrompt.razor` | anonymous |
| `/account/mfa/herstelcodes-vernieuwen` | `Pages/Account/MfaRegenerateRecoveryCodes.razor` | authenticated |
| `/account/mfa/recovery-codes` | `Pages/Account/MfaRecoveryCodes.razor` | anonymous |
| `/account/mfa/setup` | `Pages/Account/MfaSetup.razor` | anonymous |
| `/account/wachtwoord-instellen` | `Pages/Account/SetPassword.razor` | anonymous |
| `/admin` | `Pages/Admin/AdminDashboard.razor` | Admin |
| `/admin/about` | `Pages/Admin/AdminLegacyRedirect.razor` | Admin |
| `/admin/ambassadeurs` | `Pages/Admin/AdminLegacyRedirect.razor` | Admin |
| `/admin/api-keys` | `Pages/Admin/AdminLegacyRedirect.razor` | Admin |
| `/admin/ats-vacancies` | `Pages/Admin/AdminLegacyRedirect.razor` | Admin |
| `/admin/beveiliging` | `Pages/Admin/AuditLogAdmin.razor` | Admin |
| `/admin/beveiliging/2fa` | `Pages/Admin/MfaSessionsAdmin.razor` | Admin |
| `/admin/beveiliging/gegevensinzage` | `Pages/Admin/PersonalDataAccessLogAdmin.razor` | Admin |
| `/admin/beveiliging/privacy` | `Pages/Admin/PrivacyAdmin.razor` | Admin |
| `/admin/beveiliging/systeemlogs` | `Pages/Admin/LoggingAdmin.razor` | Admin |
| `/admin/cnames` | `Pages/Admin/AdminLegacyRedirect.razor` | Admin |
| `/admin/cockpit` | `Pages/Admin/AdminLegacyRedirect.razor` | Admin |
| `/admin/companies` | `Pages/Admin/AdminLegacyRedirect.razor` | Admin |
| `/admin/company` | `Pages/Admin/AdminLegacyRedirect.razor` | Admin |
| `/admin/content/emails` | `Pages/Admin/MailTestAdmin.razor` | Admin |
| `/admin/content/opleidingen` | `Pages/Admin/TrainingAdmin.razor` | Admin |
| `/admin/content/paginas` | `Pages/Admin/PaginasFlyerPage.razor` | Admin |
| `/admin/content/stamgegevens` | `Pages/Admin/StamgegevensPage.razor` | Admin |
| `/admin/exclusivity` | `Pages/Admin/AdminLegacyRedirect.razor` | Admin |
| `/admin/feedback` | `Pages/Admin/FeedbackAdmin.razor` | Admin |
| `/admin/finance` | `Pages/Admin/AdminLegacyRedirect.razor` | Admin |
| `/admin/financien` | `Pages/Admin/FinanceAdmin.razor` | Admin |
| `/admin/financien/goodwill` | `Pages/Admin/TokenAdmin.razor` | Admin |
| `/admin/financien/prijzen` | `Pages/Admin/PricesPage.razor` | Admin |
| `/admin/financien/uitbetalingen` | `Pages/Admin/TokenFinanceAdmin.razor` | Admin |
| `/admin/gebruikers` | `Pages/Admin/UsersAdmin.razor` | Admin |
| `/admin/gebruikers/rollen` | `Pages/Admin/RolesAdmin.razor` | Admin |
| `/admin/gebruikers/sales` | `Pages/Admin/SalesAmbassadeursPage.razor` | Admin |
| `/admin/instellingen` | `Pages/Admin/SettingsAdmin.razor` | Admin |
| `/admin/instellingen/algemeen` | `Pages/Admin/CompanySettingsAdmin.razor` | Admin |
| `/admin/instellingen/integraties` | `Pages/Admin/IntegratiesApiPage.razor` | Admin |
| `/admin/integrations` | `Pages/Admin/AdminLegacyRedirect.razor` | Admin |
| `/admin/kandidaten` | `Pages/Admin/CandidatesAdmin.razor` | Admin |
| `/admin/logging` | `Pages/Admin/AdminLegacyRedirect.razor` | Admin |
| `/admin/mail-test` | `Pages/Admin/AdminLegacyRedirect.razor` | Admin |
| `/admin/marketing-flyer` | `Pages/Admin/AdminLegacyRedirect.razor` | Admin |
| `/admin/masterdata` | `Pages/Admin/AdminLegacyRedirect.razor` | Admin |
| `/admin/moderation` | `Pages/Admin/AdminLegacyRedirect.razor` | Admin |
| `/admin/notifications` | `Pages/Admin/AdminLegacyRedirect.razor` | Admin |
| `/admin/organisaties` | `Pages/Admin/CompaniesAdmin.razor` | Admin |
| `/admin/organisaties/aanvragen` | `Pages/Admin/OrganisationRequestsAdmin.razor` | Admin |
| `/admin/organisaties/regios` | `Pages/Admin/CnamesAdmin.razor` | Admin |
| `/admin/paspoortpartners` | `Pages/Admin/PassportPartnersAdmin.razor` | Admin |
| `/admin/personal-data-access-log` | `Pages/Admin/AdminLegacyRedirect.razor` | Admin |
| `/admin/sales` | `Pages/Admin/AdminLegacyRedirect.razor` | Admin |
| `/admin/sales-managers` | `Pages/Admin/AdminLegacyRedirect.razor` | Admin |
| `/admin/scholen` | `Pages/Admin/Scholen/ScholenList.razor` | Admin |
| `/admin/scholen/rapportage` | `Pages/Admin/Scholen/ScholenRapportage.razor` | Admin |
| `/admin/scholen/{SchoolId:guid}` | `Pages/Admin/Scholen/ScholenDetail.razor` | Admin |
| `/admin/settings` | `Pages/Admin/AdminLegacyRedirect.razor` | Admin |
| `/admin/te-doen` | `Pages/Admin/TodoAdmin.razor` | Admin |
| `/admin/token-finance` | `Pages/Admin/AdminLegacyRedirect.razor` | Admin |
| `/admin/tokens` | `Pages/Admin/AdminLegacyRedirect.razor` | Admin |
| `/admin/training` | `Pages/Admin/AdminLegacyRedirect.razor` | Admin |
| `/admin/users` | `Pages/Admin/AdminLegacyRedirect.razor` | Admin |
| `/admin/vacancies` | `Pages/Admin/AdminLegacyRedirect.razor` | Admin |
| `/admin/vacancy-categories` | `Pages/Admin/AdminLegacyRedirect.razor` | Admin |
| `/admin/vacatures` | `Pages/Admin/VacanciesAdmin.razor` | Admin |
| `/admin/vacatures/ats` | `Pages/Admin/AtsVacanciesAdmin.razor` | Admin |
| `/admin/vacatures/categorieen` | `Pages/Admin/CategorieenSalarisPage.razor` | Admin |
| `/admin/vacatures/moderatie` | `Pages/Admin/VacanciesModerationPage.razor` | Admin |
| `/admin/wages` | `Pages/Admin/AdminLegacyRedirect.razor` | Admin |
| `/admin/werkgeververificatie` | `Pages/Admin/WerkgeverVerificatieAdmin.razor` | Admin |
| `/algemene-voorwaarden` | `Pages/Legal/AlgemeneVoorwaarden.razor` | anonymous |
| `/ambassadeur` | `Pages/Ambassadeur/Dashboard.razor` | Ambassadeur |
| `/ambassadeur/finance` | `Pages/Ambassadeur/Finance.razor` | Ambassadeur |
| `/ambassadeur/onboarding` | `Pages/Ambassadeur/Onboarding.razor` | Ambassadeur |
| `/ambassadeur/payout-checkout` | `Pages/Ambassadeur/PayoutCheckoutStub.razor` | Ambassadeur |
| `/ambassadeur/ref/{TrackingCode}` | `Pages/Ambassadeur/Landing.razor` | anonymous |
| `/ambassadeur/toolkit` | `Pages/Ambassadeur/Toolkit.razor` | Ambassadeur |
| `/banenkaart` | `Pages/Banenkaart.razor` | anonymous |
| `/branch` | `Pages/Werkgever/WerkgeverLegacyRedirect.razor` | authenticated |
| `/branch/applicants` | `Pages/Werkgever/WerkgeverLegacyRedirect.razor` | authenticated |
| `/branch/culture` | `Pages/Werkgever/WerkgeverLegacyRedirect.razor` | authenticated |
| `/branch/tokens` | `Pages/Werkgever/WerkgeverLegacyRedirect.razor` | authenticated |
| `/branch/vacancies` | `Pages/Werkgever/WerkgeverLegacyRedirect.razor` | authenticated |
| `/branch/vacancies/new` | `Pages/Werkgever/WerkgeverLegacyRedirect.razor` | authenticated |
| `/candidate/actions/set-unavailable` | `Pages/Candidate/SetUnavailableAction.razor` | any (no Authorize attribute) |
| `/candidate/actions/withdraw-others` | `Pages/Candidate/WithdrawOthersAction.razor` | any (no Authorize attribute) |
| `/candidate/applications` | `Pages/Candidate/Applications.razor` | Candidate, BranchManager, RegionalManager, EnterpriseManager, Intermediary, Admin |
| `/candidate/career` | `Pages/Candidate/CareerTest.razor` | Candidate |
| `/candidate/competencies` | `Pages/Candidate/CompetencyTest.razor` | Candidate |
| `/candidate/culture` | `Pages/Candidate/CultureScan.razor` | Candidate |
| `/candidate/deep-analysis` | `Pages/Candidate/DeepAnalysis.razor` | Candidate |
| `/candidate/deep-analysis/checkout` | `Pages/Candidate/DeepAnalysisCheckout.razor` | Candidate |
| `/candidate/deep-analysis/{Kind}` | `Pages/Candidate/DeepAnalysis.razor` | Candidate |
| `/candidate/disc` | `Pages/Candidate/CultureScan.razor` | Candidate |
| `/candidate/hoe-werkt-lobsy` | `Pages/Candidate/HowLobsyWorks.razor` | Candidate |
| `/candidate/liked` | `Pages/Candidate/Liked.razor` | anonymous |
| `/candidate/match` | `Pages/Candidate/MatchPage.razor` | anonymous |
| `/candidate/ontdekkingsreis` | `Pages/Candidate/DiscoveryJourney.razor` | Candidate |
| `/candidate/paspoort` | `Pages/Candidate/Passport.razor` | Candidate |
| `/candidate/profile` | `Pages/Candidate/Profile.razor` | Candidate |
| `/candidate/shared` | `Pages/Candidate/Shared.razor` | Candidate |
| `/candidate/start` | `Pages/Candidate/OnboardingWizard.razor` | Candidate |
| `/candidate/talent-contacts` | `Pages/Candidate/CandidateTalentContacts.razor` | Candidate |
| `/candidate/vacancies` | `Pages/Candidate/Vacancies.razor` | Candidate |
| `/candidate/values` | `Pages/Candidate/ValuesScan.razor` | Candidate |
| `/carriere` | `Pages/Candidate/CareerDashboard.razor` | Candidate |
| `/dna` | `Pages/Public/GratisDna.razor` | anonymous |
| `/employer/branches` | `Pages/Werkgever/WerkgeverLegacyRedirect.razor` | authenticated |
| `/employer/company` | `Pages/Werkgever/WerkgeverLegacyRedirect.razor` | authenticated |
| `/employer/csv-import` | `Pages/Werkgever/WerkgeverLegacyRedirect.razor` | authenticated |
| `/employer/culture` | `Pages/Werkgever/WerkgeverLegacyRedirect.razor` | authenticated |
| `/employer/kandidaatinzichten` | `Pages/Werkgever/WerkgeverLegacyRedirect.razor` | authenticated |
| `/employer/onboarding-checkout` | `Pages/Employer/OnboardingCheckout.razor` | BranchManager, EnterpriseManager, Intermediary, Admin |
| `/employer/organization` | `Pages/Werkgever/WerkgeverLegacyRedirect.razor` | authenticated |
| `/employer/regions` | `Pages/Werkgever/WerkgeverLegacyRedirect.razor` | authenticated |
| `/employer/salary-tables` | `Pages/Werkgever/WerkgeverLegacyRedirect.razor` | authenticated |
| `/employer/salary-tables/{TableId:guid}` | `Pages/Werkgever/WerkgeverLegacyRedirect.razor` | authenticated |
| `/employer/sales` | `Pages/Werkgever/WerkgeverLegacyRedirect.razor` | authenticated |
| `/employer/sales/payout-checkout` | `Pages/Werkgever/WerkgeverLegacyRedirect.razor` | authenticated |
| `/employer/takeovers` | `Pages/Werkgever/WerkgeverLegacyRedirect.razor` | authenticated |
| `/employer/talent` | `Pages/Werkgever/WerkgeverLegacyRedirect.razor` | authenticated |
| `/employer/talent-contacts` | `Pages/Werkgever/WerkgeverLegacyRedirect.razor` | authenticated |
| `/employer/tokens` | `Pages/Werkgever/WerkgeverLegacyRedirect.razor` | authenticated |
| `/employer/users` | `Pages/Werkgever/WerkgeverLegacyRedirect.razor` | authenticated |
| `/employer/vacancies` | `Pages/Werkgever/WerkgeverLegacyRedirect.razor` | authenticated |
| `/Error` | `Pages/Error.razor` | anonymous |
| `/gebruiksvoorwaarden` | `Pages/Legal/Gebruiksvoorwaarden.razor` | anonymous |
| `/hoe-werkt-lobsy` | `Pages/HowLobsyWorks.razor` | anonymous |
| `/home` | `Pages/RoleHome.razor` | authenticated |
| `/home/metrics/{Key}` | `Pages/MetricDrilldownPage.razor` | BranchManager, RegionalManager, EnterpriseManager, Intermediary, Admin |
| `/intermediary` | `Pages/Intermediary/IntermediaryDashboard.razor` | Intermediary |
| `/intermediary/team` | `Pages/Intermediary/Team.razor` | Intermediary |
| `/koppeling/sleutel` | `Pages/Public/ApiKeyReveal.razor` | anonymous |
| `/lancering` | `Pages/WestlandTeaser.razor` | anonymous |
| `/leerling` | `Pages/Leerling/LeerlingLogin.razor` | anonymous |
| `/leerling/dit-ben-jij` | `Pages/Leerling/LeerlingDitBenJij.razor` | authenticated |
| `/leerling/droombaan` | `Pages/Leerling/LeerlingDroombaan.razor` | authenticated |
| `/leerling/eiland` | `Pages/Leerling/LeerlingEiland.razor` | authenticated |
| `/leerling/reis` | `Pages/Leerling/LeerlingReis.razor` | authenticated |
| `/leerling/start` | `Pages/Leerling/LeerlingStart.razor` | authenticated |
| `/leerling/stop` | `Pages/Leerling/LeerlingStop.razor` | anonymous |
| `/leraar` | `Pages/Leraar/LeraarDashboard.razor` | Teacher, SchoolAdmin |
| `/leraar/klas/{ClassId:guid}` | `Pages/Leraar/LeraarKlasOverview.razor` | Teacher, SchoolAdmin |
| `/leraar/klas/{ClassId:guid}/code/{CodeId:guid}` | `Pages/Leraar/LeraarCodeDetail.razor` | Teacher, SchoolAdmin |
| `/leraar/klas/{ClassId:guid}/codes` | `Pages/Leraar/LeraarCodes.razor` | Teacher, SchoolAdmin |
| `/leraar/klas/{ClassId:guid}/droombanen` | `Pages/Leraar/LeraarDreamJobs.razor` | Teacher, SchoolAdmin |
| `/leraar/klas/{ClassId:guid}/groep` | `Pages/Leraar/LeraarGroup.razor` | Teacher, SchoolAdmin |
| `/leraar/klas/{ClassId:guid}/materiaal` | `Pages/Leraar/LeraarMaterials.razor` | Teacher, SchoolAdmin |
| `/leraar/klas/{ClassId:guid}/testvenster` | `Pages/Leraar/LeraarTestWindow.razor` | Teacher, SchoolAdmin |
| `/login` | `Pages/Login.razor` | anonymous |
| `/mail/afmelden` | `Pages/Public/MailUnsubscribe.razor` | anonymous |
| `/melden` | `Pages/Public/Melden.razor` | anonymous |
| `/ontdek` | `Pages/Public/GratisDna.razor` | anonymous |
| `/partner` | `Pages/Partner/PartnerSales.razor` | anonymous |
| `/partner/{TrackingCode?}` | `Pages/Partner/PartnerSales.razor` | anonymous |
| `/privacy` | `Pages/Legal/Privacy.razor` | anonymous |
| `/privacy/data` | `Pages/Legal/PrivacyData.razor` | authenticated |
| `/profiel` | `Pages/Candidate/CandidateProfile.razor` | Candidate |
| `/profiel/tests/{TestKey}` | `Pages/Candidate/TestDetail.razor` | Candidate |
| `/regional` | `Pages/Werkgever/WerkgeverLegacyRedirect.razor` | authenticated |
| `/regional/branches` | `Pages/Werkgever/WerkgeverLegacyRedirect.razor` | authenticated |
| `/regional/tokens` | `Pages/Werkgever/WerkgeverLegacyRedirect.razor` | authenticated |
| `/register` | `Pages/Register.razor` | anonymous |
| `/register/bedrijf` | `Pages/RegisterBedrijf.razor` | EnterpriseManager, BranchManager, Intermediary, Admin |
| `/register/koppelen` | `Pages/RegisterKoppelen.razor` | authenticated |
| `/register/toegang` | `Pages/RegisterToegang.razor` | anonymous |
| `/register/verifieren` | `Pages/RegisterVerifieren.razor` | EnterpriseManager, BranchManager, Intermediary, Admin |
| `/register/verifieren/brief` | `Pages/RegisterVerifierenBrief.razor` | EnterpriseManager, BranchManager, Intermediary, Admin |
| `/sales` | `Pages/Sales/Dashboard.razor` | SalesManager |
| `/sales/aanbevelen` | `Pages/Sales/Recommend.razor` | SalesManager |
| `/sales/aanbevelen/bezwaar` | `Pages/Sales/RecommendObject.razor` | anonymous |
| `/sales/hulp` | `Pages/Sales/Help.razor` | SalesManager |
| `/sales/link` | `Pages/Sales/Link.razor` | SalesManager |
| `/sales/profiel` | `Pages/Sales/Profile.razor` | SalesManager |
| `/sales/profiel/iban-bevestigen` | `Pages/Sales/IbanConfirm.razor` | SalesManager |
| `/sales/start` | `Pages/Sales/Start.razor` | SalesManager |
| `/sales/wallet` | `Pages/Sales/Wallet.razor` | SalesManager |
| `/sales/wallet/uitbetalen` | `Pages/Sales/Wallet.razor` | SalesManager |
| `/sales/werkgevers` | `Pages/Sales/Employers.razor` | SalesManager |
| `/salesmanager` | `Pages/Sales/LegacySalesmanagerRedirect.razor` | SalesManager |
| `/salesmanager/invoices` | `Pages/Sales/LegacyInvoicesRedirect.razor` | SalesManager |
| `/salesmanager/onboarding` | `Pages/Sales/LegacyOnboardingRedirect.razor` | SalesManager |
| `/salesmanager/payout-checkout` | `Pages/Sales/LegacyPayoutRedirect.razor` | SalesManager |
| `/salesmanager/referrals` | `Pages/Sales/LegacyReferralsRedirect.razor` | SalesManager |
| `/salesmanager/toolkit` | `Pages/Sales/LegacyToolkitRedirect.razor` | SalesManager |
| `/school` | `Pages/School/SchoolDashboard.razor` | SchoolAdmin |
| `/school/gegevens` | `Pages/School/SchoolDetails.razor` | SchoolAdmin |
| `/school/klassen` | `Pages/School/SchoolClasses.razor` | SchoolAdmin |
| `/school/klassen/{ClassId:guid}` | `Pages/School/SchoolClassDetail.razor` | SchoolAdmin |
| `/school/leraren` | `Pages/School/SchoolTeachers.razor` | SchoolAdmin |
| `/school/materiaal` | `Pages/School/SchoolMaterials.razor` | SchoolAdmin |
| `/school/privacy` | `Pages/School/SchoolPrivacy.razor` | SchoolAdmin |
| `/school/resultaten` | `Pages/School/SchoolResults.razor` | SchoolAdmin |
| `/school/te-doen` | `Pages/School/SchoolTodos.razor` | SchoolAdmin |
| `/status/{Code:int}` | `Pages/Status/StatusPage.razor` | anonymous |
| `/toestemming` | `Pages/Public/ParentalConsent.razor` | anonymous |
| `/tokens/checkout-return` | `Pages/TokensCheckoutReturn.razor` | BranchManager, EnterpriseManager, Intermediary, Admin |
| `/tokens/checkout-stub` | `Pages/TokensCheckoutStub.razor` | BranchManager, EnterpriseManager, Intermediary, Admin |
| `/vacancies/{Id:guid}` | `Pages/VacancyDetail.razor` | anonymous |
| `/vestiging/{CompanyId:guid}` | `Pages/VestigingLanding.razor` | anonymous |
| `/wachtwoord-vergeten` | `Pages/Account/ForgotPassword.razor` | anonymous |
| `/werkgever` | `Pages/Werkgever/WerkgeverDashboard.razor` | BranchManager, RegionalManager, EnterpriseManager, Intermediary |
| `/werkgever/kandidaatinzichten` | `Pages/Werkgever/CandidateInsights.razor` | BranchManager, RegionalManager, EnterpriseManager |
| `/werkgever/koppelingen` | `Pages/Werkgever/Koppelingen.razor` | EnterpriseManager, Admin |
| `/werkgever/organisatie/profiel` | `Pages/Werkgever/CompanyProfile.razor` | BranchManager, EnterpriseManager, Admin, Intermediary |
| `/werkgever/organisatie/salaristabellen` | `Pages/Werkgever/SalaryTables.razor` | BranchManager, EnterpriseManager, Admin |
| `/werkgever/organisatie/salaristabellen/{TableId:guid}` | `Pages/Werkgever/SalaryTables.razor` | BranchManager, EnterpriseManager, Admin |
| `/werkgever/organisatie/team` | `Pages/Werkgever/Users.razor` | EnterpriseManager, Admin |
| `/werkgever/organisatie/vestigingen` | `Pages/Werkgever/BranchesRegions.razor` | RegionalManager, EnterpriseManager, Admin |
| `/werkgever/overnames` | `Pages/Werkgever/Takeovers.razor` | BranchManager, EnterpriseManager, Admin |
| `/werkgever/partner` | `Pages/Werkgever/PartnerSales.razor` | EnterpriseManager, Intermediary |
| `/werkgever/partner/uitbetalen` | `Pages/Werkgever/PartnerSalesPayoutCheckoutStub.razor` | EnterpriseManager, Intermediary |
| `/werkgever/sollicitaties` | `Pages/Werkgever/Applicants.razor` | BranchManager, RegionalManager, EnterpriseManager, Intermediary, Admin |
| `/werkgever/sollicitaties/{ApplicationId:guid}` | `Pages/Werkgever/ApplicationCandidate.razor` | BranchManager, RegionalManager, EnterpriseManager, Intermediary, Admin |
| `/werkgever/talentpool` | `Pages/Werkgever/TalentPool.razor` | BranchManager, RegionalManager, EnterpriseManager, Intermediary |
| `/werkgever/te-doen` | `Pages/Werkgever/TeDoen.razor` | BranchManager, RegionalManager, EnterpriseManager |
| `/werkgever/tokens` | `Pages/Werkgever/Tokens.razor` | BranchManager, RegionalManager, EnterpriseManager, Intermediary |
| `/werkgever/tokens/facturen` | `Pages/Werkgever/TokensFacturen.razor` | EnterpriseManager, Intermediary, Admin |
| `/werkgever/tokens/mutaties` | `Pages/Werkgever/TokensMutaties.razor` | BranchManager, RegionalManager, EnterpriseManager, Intermediary |
| `/werkgever/tokens/verbruik` | `Pages/Werkgever/TokensVerbruik.razor` | RegionalManager, EnterpriseManager, Intermediary |
| `/werkgever/vacatures` | `Pages/Werkgever/Vacancies.razor` | BranchManager, RegionalManager, EnterpriseManager, Intermediary, Admin |
| `/werkgever/vacatures/nieuw` | `Pages/Werkgever/CreateVacancy.razor` | BranchManager, EnterpriseManager, Intermediary |
| `/werkgever/wervingsmateriaal` | `Pages/Werkgever/Wervingsmateriaal.razor` | BranchManager, RegionalManager, EnterpriseManager, Admin |
| `/werkgevers/binnenkort` | `Pages/WerkgeversBinnenkort.razor` | anonymous |
| `/werven/{TrackingCode}` | `Pages/Ambassadeur/Landing.razor` | anonymous |
| `/westland` | `Pages/WestlandTeaser.razor` | anonymous |
| `/wie-zijn-wij` | `Pages/Legal/WieZijnWij.razor` | anonymous |
| `/{KvkNumber:regex(^\\d{{8}}$)}` | `Pages/CompanyPublicPage.razor` | anonymous |
| `/{KvkNumber:regex(^\\d{{8}}$)}/{Vestigingsnummer:regex(^\\d{{1,12}}$)}` | `Pages/CompanyPublicPage.razor` | anonymous |


## Werkgever legacy redirects (D2)

Old `/employer`, `/branch` and `/regional` URLs answer **301** to `/werkgever/…`
for at least one release. Source: `WerkgeverLegacyRoutes.Table`.
// Remove after {release}

| Old | New |
|-----|-----|
| `/employer/vacancies` | `/werkgever/vacatures` |
| `/branch/vacancies` | `/werkgever/vacatures` |
| `/branch/vacancies/new` | `/werkgever/vacatures/nieuw` |
| `/branch/applicants` | `/werkgever/sollicitaties` |
| `/employer/talent` | `/werkgever/talentpool` |
| `/employer/talent-contacts` | `/werkgever/talentpool?tab=contact` |
| `/employer/kandidaatinzichten` | `/werkgever/kandidaatinzichten` |
| `/employer/branches` | `/werkgever/organisatie/vestigingen` |
| `/regional/branches` | `/werkgever/organisatie/vestigingen` |
| `/employer/regions` | `/werkgever/organisatie/vestigingen?tab=regios` |
| `/employer/organization` | `/werkgever/organisatie/vestigingen` |
| `/employer/users` | `/werkgever/organisatie/team` |
| `/employer/company` | `/werkgever/organisatie/profiel` |
| `/employer/culture` | `/werkgever/organisatie/profiel?tab=cultuur` |
| `/branch/culture` | `/werkgever/organisatie/profiel?tab=cultuur` |
| `/employer/salary-tables` | `/werkgever/organisatie/salaristabellen` |
| `/employer/tokens` | `/werkgever/tokens` |
| `/branch/tokens` | `/werkgever/tokens` |
| `/regional/tokens` | `/werkgever/tokens` |
| `/employer/csv-import` | `/werkgever/koppelingen?tab=csv` |
| `/employer/takeovers` | `/werkgever/overnames` |
| `/employer/sales` | `/werkgever/partner` |
| `/employer/sales/payout-checkout` | `/werkgever/partner/uitbetalen` |
| `/branch` | `/werkgever` |
| `/regional` | `/werkgever` |

| Special | Behaviour |
|---------|------------|
| `/home` (employer roles only) | 301 → `/werkgever` |
| `/employer/onboarding-checkout`, `/tokens/checkout-return`, `/tokens/checkout-stub` | **unchanged** (payment return URLs) |

## Minimal API (public shell)

Not Blazor `@page` routes — documented here for discoverability (landing stack).

| Route | Notes |
|-------|-------|
| `/taal/{lang}` | Sets `Jobsy.Culture` cookie; 302 to local `returnUrl` only; `noindex` |
| `/account/cookie-consent/analytics-token` | POST; same-origin analytics consent token for static cookie banner |
| `/account/email-code/start` | POST; antiforgery; starts passwordless e-mail code (Web → API) |
| `/account/email-code/verify` | POST; antiforgery; verifies code and signs in |
| `/mail/afmelden` | POST; RFC 8058 one-click / form unsubscribe (no antiforgery; rate-limited) |
| `/account/mail-instellingen` | POST; antiforgery; save optional mail toggles (Web → API) |
| `/melden` | POST; antiforgery; forwards a content report to `api/reports` (rate-limited; no IP stored) |
| `/partner/flyer.pdf` | GET; anonymous; proxies the partner flyer pdf (`?code=` optional); 302 → `/` with werkgevers-actief OFF; rate-limited |
| `/register?van=ontdek` | GET; 302 → `/account-maken?van=ontdek` (legacy test CTA) |
| `/banen` | GET/HEAD; **301** → `/banenkaart` (+ query) |
| `/bewaard` | GET/HEAD; **302** → `/candidate/liked` (+ query) |
| `/candidate/saved` | GET/HEAD; **302** → `/candidate/liked` (+ query) |
| `/candidate/bewaard` | GET/HEAD; **302** → `/candidate/liked` (+ query) |

## Kandidaat banen notes

- Banenkaart list mode: query `?weergave=lijst` on `/banenkaart`. Persisted in `sessionStorage jobsy.kb.weergave`.
- Employer viewed hook: `POST api/applications/{id}/viewed` (07) records at most one `EmployerViewed` timeline event.
- Werkgevers gating (paspoort 01): when `PlatformFeature.Employers` lands, candidate job pages/APIs return the feature gate / `404 feature_disabled`. Until then KB-FALLBACK(C) comments mark the intended sites.
- Map route constant: `KbRoutes.Map` (`/banenkaart`). Saved: `KbRoutes.Saved` (`/candidate/liked`). With passport ON, Bewaard is a tab under Sollicitaties.

## Carrière notes

- `/carriere` deep-links one step with `?stap={n}`; an unknown or future step falls back to the overview.
- `/candidate/talent-contacts` only shows employer contact details after the candidate accepts; a Pending request shows the share preview first.
- `/candidate/hoe-werkt-lobsy` is the candidate how-to guide (Kandidaat only); other roles get their own guide and never see the five stones.
- Career API: `GET api/me/career-path/dream-options`, `GET api/me/career-path/archived`, `POST api/me/career-path/archived/{id}/restore`, `GET api/me/talent-contacts/{id}/share-preview`, `GET api/me/journey-summary`.
- `POST api/me/career-path/courses/claim` is a **410 Gone** stub (`use_passport_proof`); candidates prove courses through the passport. Removed after 2026-10-30.

## Notes

- **Admin redesign 06.4 must host `PayoutRunsSection` in a tab Rondes** on `/admin/financien/uitbetalingen` and keep mark-paid closing payout requests. Until then the fallback is `/admin/sales-managers?tab=uitbetalingen`.

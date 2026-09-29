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
`/banen`, `/hoe-werkt-lobsy`, `/candidate/...`, `/employer/...`, `/vacancies/...`).
**Do not rename routes** for cosmetics — bookmarks, QR landings, and emails depend on them.
Product narrative per role: [`ROLES_AND_VIEWS.md`](../ROLES_AND_VIEWS.md).
Authorization intent: [`security/roles-matrix.md`](security/roles-matrix.md).

## Access column

| Value | Meaning |
|-------|---------|
| `anonymous` | `[AllowAnonymous]` |
| `authenticated` | `[Authorize]` without `Roles=` |
| role list | `[Authorize(Roles="…")]` (Jobsy role claim names) |
| `any (no Authorize attribute)` | No attribute — Web has no FallbackPolicy |

## Table (146 routes)

| Route | Component | Access |
|-------|-----------|--------|
| `/` | `Pages/Home.razor` | anonymous |
| `/access-denied` | `Pages/AccessDenied.razor` | anonymous |
| `/account/mfa` | `Pages/Account/MfaPrompt.razor` | anonymous |
| `/account/mfa/recovery-codes` | `Pages/Account/MfaRecoveryCodes.razor` | anonymous |
| `/account/mfa/setup` | `Pages/Account/MfaSetup.razor` | anonymous |
| `/admin` | `Pages/Admin/AdminIndex.razor` | Admin |
| `/admin/about` | `Pages/Admin/AboutPageAdmin.razor` | Admin |
| `/admin/ambassadeurs` | `Pages/Admin/AmbassadeursAdmin.razor` | Admin |
| `/admin/api-keys` | `Pages/Admin/ApiKeysAdmin.razor` | Admin |
| `/admin/ats-vacancies` | `Pages/Admin/AtsVacanciesAdmin.razor` | Admin |
| `/admin/cnames` | `Pages/Admin/CnamesAdmin.razor` | Admin |
| `/admin/cockpit` | `Pages/Admin/AdminHome.razor` | Admin |
| `/admin/companies` | `Pages/Admin/CompaniesAdmin.razor` | Admin |
| `/admin/company` | `Pages/Admin/CompanySettingsAdmin.razor` | Admin |
| `/admin/exclusivity` | `Pages/Admin/ExclusivitySettingsAdmin.razor` | Admin |
| `/admin/feedback` | `Pages/Admin/FeedbackAdmin.razor` | Admin |
| `/admin/finance` | `Pages/Admin/FinanceAdmin.razor` | Admin |
| `/admin/integrations` | `Pages/Admin/IntegrationsAdmin.razor` | Admin |
| `/admin/logging` | `Pages/Admin/LoggingAdmin.razor` | Admin |
| `/admin/mail-test` | `Pages/Admin/MailTestAdmin.razor` | Admin |
| `/admin/marketing-flyer` | `Pages/Admin/MarketingFlyerAdmin.razor` | Admin |
| `/admin/masterdata` | `Pages/Admin/MasterdataAdmin.razor` | Admin |
| `/admin/moderation` | `Pages/Admin/ModerationAdmin.razor` | Admin |
| `/admin/notifications` | `Pages/Admin/NotificationsAdmin.razor` | Admin |
| `/admin/personal-data-access-log` | `Pages/Admin/PersonalDataAccessLogAdmin.razor` | Admin |
| `/admin/sales` | `Pages/Admin/SalesCommercialPage.razor` | Admin |
| `/admin/sales-managers` | `Pages/Admin/SalesManagersAdmin.razor` | Admin |
| `/admin/settings` | `Pages/Admin/SettingsAdmin.razor` | Admin |
| `/admin/token-finance` | `Pages/Admin/TokenFinanceAdmin.razor` | Admin |
| `/admin/tokens` | `Pages/Admin/TokenAdmin.razor` | Admin |
| `/admin/training` | `Pages/Admin/TrainingAdmin.razor` | Admin |
| `/admin/users` | `Pages/Admin/UsersAdmin.razor` | Admin |
| `/admin/vacancies` | `Pages/Admin/VacanciesAdmin.razor` | Admin |
| `/admin/vacancy-categories` | `Pages/Admin/VacancyCategoriesAdmin.razor` | Admin |
| `/admin/wages` | `Pages/Admin/WageAdmin.razor` | Admin |
| `/algemene-voorwaarden` | `Pages/Legal/AlgemeneVoorwaarden.razor` | anonymous |
| `/ambassadeur` | `Pages/Ambassadeur/Dashboard.razor` | Ambassadeur |
| `/ambassadeur/finance` | `Pages/Ambassadeur/Finance.razor` | Ambassadeur |
| `/ambassadeur/onboarding` | `Pages/Ambassadeur/Onboarding.razor` | Ambassadeur |
| `/ambassadeur/payout-checkout` | `Pages/Ambassadeur/PayoutCheckoutStub.razor` | Ambassadeur |
| `/ambassadeur/ref/{TrackingCode}` | `Pages/Ambassadeur/Landing.razor` | anonymous |
| `/ambassadeur/toolkit` | `Pages/Ambassadeur/Toolkit.razor` | Ambassadeur |
| `/banen` | `Pages/Banen.razor` | anonymous |
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
| `/candidate/hoe-werkt-lobsy` | `Pages/Candidate/HowLobsyWorks.razor` | Candidate, BranchManager, RegionalManager, EnterpriseManager, Intermediary, Admin |
| `/candidate/liked` | `Pages/Candidate/Liked.razor` | anonymous |
| `/candidate/match` | `Pages/Candidate/MatchPage.razor` | anonymous |
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
| `/lancering` | `Pages/WestlandTeaser.razor` | anonymous |
| `/login` | `Pages/Login.razor` | anonymous |
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
| `/register/activate` | `Pages/RegisterActivate.razor` | anonymous |
| `/salesmanager` | `Pages/SalesManager/Dashboard.razor` | SalesManager |
| `/salesmanager/invoices` | `Pages/SalesManager/Invoices.razor` | SalesManager |
| `/salesmanager/onboarding` | `Pages/SalesManager/Onboarding.razor` | SalesManager |
| `/salesmanager/payout-checkout` | `Pages/SalesManager/PayoutCheckoutStub.razor` | SalesManager |
| `/salesmanager/referrals` | `Pages/SalesManager/Referrals.razor` | SalesManager |
| `/salesmanager/toolkit` | `Pages/SalesManager/SalesToolkit.razor` | SalesManager |
| `/tokens/checkout-return` | `Pages/TokensCheckoutReturn.razor` | BranchManager, EnterpriseManager, Intermediary, Admin |
| `/tokens/checkout-stub` | `Pages/TokensCheckoutStub.razor` | BranchManager, EnterpriseManager, Intermediary, Admin |
| `/vacancies/{Id:guid}` | `Pages/VacancyDetail.razor` | anonymous |
| `/vestiging/{CompanyId:guid}` | `Pages/VestigingLanding.razor` | anonymous |
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
| `/werven/{TrackingCode}` | `Pages/Ambassadeur/Landing.razor` | anonymous |
| `/westland` | `Pages/WestlandTeaser.razor` | anonymous |
| `/wie-zijn-wij` | `Pages/Legal/WieZijnWij.razor` | anonymous |
| `/{KvkNumber:regex(^\\d{{8}}$)}` | `Pages/CompanyPublicPage.razor` | anonymous |
| `/{KvkNumber:regex(^\\d{{8}}$)}/{Vestigingsnummer:regex(^\\d{{1,12}}$)}` | `Pages/CompanyPublicPage.razor` | anonymous |

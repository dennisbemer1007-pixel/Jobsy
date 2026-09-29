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

## Table (143 routes)

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
| `/admin/scholen` | `Pages/Admin/Scholen/ScholenList.razor` | Admin |
| `/admin/scholen/{SchoolId:guid}` | `Pages/Admin/Scholen/ScholenDetail.razor` | Admin |
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
| `/branch` | `Pages/Branch/BranchDashboard.razor` | BranchManager, EnterpriseManager |
| `/branch/applicants` | `Pages/Branch/Applicants.razor` | BranchManager, RegionalManager, EnterpriseManager, Intermediary, Admin |
| `/branch/culture` | `Pages/Employer/CultureScan.razor` | BranchManager, RegionalManager, EnterpriseManager, Intermediary, Admin |
| `/branch/tokens` | `Pages/Employer/Tokens.razor` | BranchManager, RegionalManager, EnterpriseManager, Intermediary |
| `/branch/vacancies` | `Pages/Employer/Vacancies.razor` | BranchManager, RegionalManager, EnterpriseManager, Intermediary, Admin |
| `/branch/vacancies/new` | `Pages/Branch/CreateVacancy.razor` | BranchManager, EnterpriseManager, Intermediary |
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
| `/employer/branches` | `Pages/Employer/Branches.razor` | RegionalManager, EnterpriseManager |
| `/employer/company` | `Pages/Employer/CompanyDetails.razor` | BranchManager, EnterpriseManager, Admin, Intermediary |
| `/employer/csv-import` | `Pages/Employer/CsvImport.razor` | EnterpriseManager, Admin |
| `/employer/culture` | `Pages/Employer/CultureScan.razor` | BranchManager, RegionalManager, EnterpriseManager, Intermediary, Admin |
| `/employer/kandidaatinzichten` | `Pages/Employer/CandidateInsights.razor` | BranchManager, RegionalManager, EnterpriseManager |
| `/employer/onboarding-checkout` | `Pages/Employer/OnboardingCheckout.razor` | BranchManager, EnterpriseManager, Intermediary, Admin |
| `/employer/organization` | `Pages/Employer/Organization.razor` | EnterpriseManager |
| `/employer/regions` | `Pages/Employer/Regions.razor` | EnterpriseManager, Admin |
| `/employer/salary-tables` | `Pages/Employer/SalaryTables.razor` | EnterpriseManager, Admin |
| `/employer/salary-tables/{TableId:guid}` | `Pages/Employer/SalaryTables.razor` | EnterpriseManager, Admin |
| `/employer/sales` | `Pages/Employer/PartnerSales.razor` | EnterpriseManager, Intermediary |
| `/employer/sales/payout-checkout` | `Pages/Employer/PartnerSalesPayoutCheckoutStub.razor` | EnterpriseManager, Intermediary |
| `/employer/takeovers` | `Pages/Employer/Takeovers.razor` | BranchManager, EnterpriseManager, Admin |
| `/employer/talent` | `Pages/Employer/TalentPool.razor` | BranchManager, RegionalManager, EnterpriseManager, Intermediary |
| `/employer/talent-contacts` | `Pages/Employer/TalentContacts.razor` | BranchManager, RegionalManager, EnterpriseManager, Intermediary |
| `/employer/tokens` | `Pages/Employer/Tokens.razor` | BranchManager, RegionalManager, EnterpriseManager, Intermediary |
| `/employer/users` | `Pages/Employer/Users.razor` | EnterpriseManager, Admin |
| `/employer/vacancies` | `Pages/Employer/Vacancies.razor` | BranchManager, RegionalManager, EnterpriseManager, Intermediary, Admin |
| `/Error` | `Pages/Error.razor` | anonymous |
| `/gebruiksvoorwaarden` | `Pages/Legal/Gebruiksvoorwaarden.razor` | anonymous |
| `/hoe-werkt-lobsy` | `Pages/HowLobsyWorks.razor` | anonymous |
| `/home` | `Pages/RoleHome.razor` | authenticated |
| `/home/metrics/{Key}` | `Pages/MetricDrilldownPage.razor` | BranchManager, RegionalManager, EnterpriseManager, Intermediary, Admin |
| `/intermediary` | `Pages/Intermediary/IntermediaryDashboard.razor` | Intermediary |
| `/intermediary/team` | `Pages/Intermediary/Team.razor` | Intermediary |
| `/lancering` | `Pages/WestlandTeaser.razor` | anonymous |
| `/leraar` | `Pages/Leraar/LeraarDashboard.razor` | Teacher, SchoolAdmin |
| `/leraar/klas/{ClassId:guid}` | `Pages/Leraar/LeraarKlasOverview.razor` | Teacher, SchoolAdmin |
| `/leraar/klas/{ClassId:guid}/code/{CodeId:guid}` | `Pages/Leraar/LeraarCodeDetail.razor` | Teacher, SchoolAdmin |
| `/leraar/klas/{ClassId:guid}/codes` | `Pages/Leraar/LeraarCodes.razor` | Teacher, SchoolAdmin |
| `/leraar/klas/{ClassId:guid}/droombanen` | `Pages/Leraar/LeraarDreamJobs.razor` | Teacher, SchoolAdmin |
| `/leraar/klas/{ClassId:guid}/groep` | `Pages/Leraar/LeraarGroup.razor` | Teacher, SchoolAdmin |
| `/leraar/klas/{ClassId:guid}/materiaal` | `Pages/Leraar/LeraarMaterials.razor` | Teacher, SchoolAdmin |
| `/leraar/klas/{ClassId:guid}/testvenster` | `Pages/Leraar/LeraarTestWindow.razor` | Teacher, SchoolAdmin |
| `/login` | `Pages/Login.razor` | anonymous |
| `/ontdek` | `Pages/Public/GratisDna.razor` | anonymous |
| `/partner` | `Pages/Partner/PartnerSales.razor` | anonymous |
| `/partner/{TrackingCode?}` | `Pages/Partner/PartnerSales.razor` | anonymous |
| `/privacy` | `Pages/Legal/Privacy.razor` | anonymous |
| `/privacy/data` | `Pages/Legal/PrivacyData.razor` | authenticated |
| `/profiel` | `Pages/Candidate/CandidateProfile.razor` | Candidate |
| `/profiel/tests/{TestKey}` | `Pages/Candidate/TestDetail.razor` | Candidate |
| `/regional` | `Pages/Regional/RegionalDashboard.razor` | RegionalManager, EnterpriseManager |
| `/regional/branches` | `Pages/Employer/Branches.razor` | RegionalManager, EnterpriseManager |
| `/regional/tokens` | `Pages/Regional/TokenControl.razor` | RegionalManager, EnterpriseManager |
| `/register` | `Pages/Register.razor` | anonymous |
| `/register/activate` | `Pages/RegisterActivate.razor` | anonymous |
| `/salesmanager` | `Pages/SalesManager/Dashboard.razor` | SalesManager |
| `/salesmanager/invoices` | `Pages/SalesManager/Invoices.razor` | SalesManager |
| `/salesmanager/onboarding` | `Pages/SalesManager/Onboarding.razor` | SalesManager |
| `/salesmanager/payout-checkout` | `Pages/SalesManager/PayoutCheckoutStub.razor` | SalesManager |
| `/salesmanager/referrals` | `Pages/SalesManager/Referrals.razor` | SalesManager |
| `/salesmanager/toolkit` | `Pages/SalesManager/SalesToolkit.razor` | SalesManager |
| `/school` | `Pages/School/SchoolDashboard.razor` | SchoolAdmin |
| `/school/gegevens` | `Pages/School/SchoolDetails.razor` | SchoolAdmin |
| `/school/klassen` | `Pages/School/SchoolClasses.razor` | SchoolAdmin |
| `/school/klassen/{ClassId:guid}` | `Pages/School/SchoolClassDetail.razor` | SchoolAdmin |
| `/school/leraren` | `Pages/School/SchoolTeachers.razor` | SchoolAdmin |
| `/school/materiaal` | `Pages/School/SchoolMaterials.razor` | SchoolAdmin |
| `/school/privacy` | `Pages/School/SchoolPrivacy.razor` | SchoolAdmin |
| `/school/resultaten` | `Pages/School/SchoolResults.razor` | SchoolAdmin |
| `/school/te-doen` | `Pages/School/SchoolTodos.razor` | SchoolAdmin |
| `/leerling` | `Pages/Leerling/LeerlingLogin.razor` | anonymous (Pupil login) |
| `/leerling/start` | `Pages/Leerling/LeerlingStart.razor` | PupilSession |
| `/leerling/reis` | `Pages/Leerling/LeerlingReis.razor` | PupilSession |
| `/leerling/stop` | `Pages/Leerling/LeerlingStop.razor` | anonymous |
| `/leerling/dit-ben-jij` | `Pages/Leerling/LeerlingDitBenJij.razor` | PupilSession |
| `/tokens/checkout-return` | `Pages/TokensCheckoutReturn.razor` | BranchManager, EnterpriseManager, Intermediary, Admin |
| `/tokens/checkout-stub` | `Pages/TokensCheckoutStub.razor` | BranchManager, EnterpriseManager, Intermediary, Admin |
| `/vacancies/{Id:guid}` | `Pages/VacancyDetail.razor` | anonymous |
| `/vestiging/{CompanyId:guid}` | `Pages/VestigingLanding.razor` | anonymous |
| `/werven/{TrackingCode}` | `Pages/Ambassadeur/Landing.razor` | anonymous |
| `/westland` | `Pages/WestlandTeaser.razor` | anonymous |
| `/wie-zijn-wij` | `Pages/Legal/WieZijnWij.razor` | anonymous |
| `/{KvkNumber:regex(^\\d{{8}}$)}` | `Pages/CompanyPublicPage.razor` | anonymous |
| `/{KvkNumber:regex(^\\d{{8}}$)}/{Vestigingsnummer:regex(^\\d{{1,12}}$)}` | `Pages/CompanyPublicPage.razor` | anonymous |

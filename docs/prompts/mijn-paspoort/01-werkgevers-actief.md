# 01 · Werkgevers actief (employers on/off), Phase 0

> Read `00-README.md` first: §0 rules apply, plus **§F (you build it here)** and **§N (you do the refactor part)**. This is the first file.

| | |
|---|---|
| Branch | `cursor/werkgevers-actief`, created from `origin/acceptatie` |
| PR | ONE PR into `acceptatie`, title `feat(flags): admin switch Werkgevers actief`. Body starts with `Stacked on: none (first in the stack)` |
| Mockups | none |

**Goal.** One admin switch.
- **ON (default) = exactly as now.** Snapshot tests and all existing tests are unchanged.
- **OFF = a pure self-discovery platform.** Everything that needs employers or vacancies is hidden in the UI **and** blocked on the server.
- **Existing data stays untouched:** no deletes, no status changes, no emails about it.
- Switching back ON restores everything as it was.

## 01.1 Flag
`EmployersEnabled` (default **true**) plus the admin checkbox, via §F. Gate with `[RequiresFeature(PlatformFeature.Employers)]`.

## 01.2 Inventory: what is employer- or vacancy-dependent, and what happens when OFF
Found with `rg` on `origin/acceptatie` @ `20dc8d5b`. **Re-run the search** before starting, and add anything new to the tables and to `docs/feature-flags.md` (a new doc listing every gated item):

```
rg -l -i "vacanc|employer|werkgever|company|kvk|application|sollicit|talentcontact|talent-contact|takeover|pushbom|token|intermediar|salesmanager|ambassadeur|banenkaart|jobmap" Jobsy.Web/Components Jobsy.Api/Controllers Jobsy.Infrastructure/Jobs Jobsy.Core/Email
```

**a) Nav (§N)**
- **Candidate** (`RoleNavCatalog.Candidate`): hide Zoeken `/`, Bewaard `/candidate/liked` (+ `/candidate/shared`) and Sollicitaties `/candidate/applications`.
- **Employer-side catalogs** (`Admin` excluded): `Enterprise`, `Regional`, `Branch…`, `Intermediary`, `SalesManager`, `Ambassadeur` and `CsvImport` / `Organization` / `Takeovers`. When OFF, these users see no nav (see 01.3).
- **Header chips** in `Components/Layout`:
  - `TokenWalletChip`
  - `SalesWalletChip`
  - `NotificationBell`: still shows existing notifications, but hides the employer-reaction types.

**b) Blazor pages: add `[RequiresFeature(Employers)]`**
- **Public job pages:**
  - `/` `Pages/Home.razor` (banenkaart)
  - `/banen` `Pages/Banen.razor`
  - `/vacancies/{Id}` `Pages/VacancyDetail.razor`
  - `/vestiging/{CompanyId}` `Pages/VestigingLanding.razor`
  - `/{KvkNumber}` and `/{KvkNumber}/{Vestigingsnummer}` `Pages/CompanyPublicPage.razor`
  - `/westland`, `/lancering` `Pages/WestlandTeaser.razor` (vacancy marketing)
- **Candidate job flows:**
  - `/candidate/match` (Match/swipe, `MatchMobileLayout`)
  - `/candidate/vacancies`
  - `/candidate/liked`, `/candidate/shared`
  - `/candidate/applications`
  - `/candidate/actions/withdraw-others`, `/candidate/actions/set-unavailable`
  - `/candidate/talent-contacts` (employer contact requests)
- **Employer registration and login:**
  - `/register`, `/register/activate`: KvK company registration
  - `/employer/onboarding-checkout`
- **Employer, branch and regional portals:**
  - `/employer/*` (vacancies, talent, talent-contacts, kandidaatinzichten, tokens, users, organization, branches, regions, company, salary-tables, csv-import, culture, takeovers, sales, sales/payout-checkout)
  - `/branch/*` (incl. `/branch/vacancies/new`, `/branch/applicants`, `/branch/culture`, `/branch/tokens`)
  - `/regional/*`
  - `/intermediary`, `/intermediary/team`
  - `/tokens/checkout-return`, `/tokens/checkout-stub`: employer token purchases. Candidate deep-analysis checkout stays.
- **Employer-acquisition portals** (default: gate them, D2):
  - `/salesmanager/*`
  - `/ambassadeur/*` + `/ambassadeur/ref/{code}`
  - `/werven/{code}`
  - `/partner`, `/partner/{code}`
- **Stays available:**
  - `/ontdek` / `/dna` (Gratis DNA)
  - `/login`
  - all `/candidate/*` test, deep-analysis, career and profile pages
  - `/carriere`
  - `/profiel*`, `/privacy*` and the legal pages
  - `/wie-zijn-wij`
  - `/hoe-werkt-lobsy`: employer guides hidden via `HowLobsyGuidePanel`
  - `/home`
  - `/admin/*`

**c) API controllers: add `[RequiresFeature(Employers)]`** (whole controller unless noted)
- **Vacancies and discovery:**
  - `VacanciesController`: public list, pins, cards, detail, CRUD
  - `VacancyCategoriesController`: non-admin actions only
  - `VacancyEngagementController`: likes/shares/views
  - `ExternalVacanciesController`: API-key vacancy push/import (D3)
  - `VacancyCsvImportController`
  - `AdminAtsController`: stays for admin (read), but its scrape triggers are gated
- **Applications:**
  - `ApplicationsController`
  - `CandidateActionsController`: withdraw / set-unavailable
- **Talent pool and employer contact:**
  - `TalentPoolController`: employer contact requests and candidate talent-contacts
  - `CandidateInsightsController`: employer-side kandidaatinzichten
  - `CompanyCultureController`
- **Companies and KvK:**
  - `CompaniesController`, `CompanyUsersController`, `CompanyApiKeysController`, `PublicCompaniesController`, `SupplierOnboardingController`
  - `KvkController`
  - `RegistrationController` (KvK registration, takeovers)
  - `RegionsController`, `RegionHostsController`: non-admin actions
- **Tokens and payments:**
  - `TokensController`, `TokenLogsController`, `SalaryTablesController`, `WagesController` (public wage lookups for vacancies)
  - `EmployerFlyersController`
  - `DashboardController` / `MetricsController`: employer and regional dashboards. Candidate metrics are in `CandidateMetricsController`, see d.
- **Employer acquisition:** `SalesManagersController`, `SalesCommercialController`, `AmbassadeursController`, `PartnerAffiliateController` (D2).
- **Not gated:**
  - `AuthController`, `MeController`, `MfaController`
  - `Candidate*Controller`s (see d)
  - `DeepAnalysisController`, `AssessmentAdjustmentsController`, `CandidateCareerPathController`, `RoleFitCheckController` (see d)
  - `TrainingUpskillController`, `MockInterviewController` (see d)
  - `AssistantController` (see d)
  - `PrivacyController`, `ParentalConsentController`, `FeedbackController`, `NotificationsController` (read), `WebPushController` (subscription only)
  - `MollieWebhooksController`: **must keep working** for payments already in flight
  - `SiteController`, `SettingsController`
  - all admin controllers (incl. `ExclusivitySettingsController`, `IntegrationsController`)
  - `TravelController`, `AnalyticsController`, `MasterdataController`: generic
  - `TokenFinanceController`, `VatDeclarationsController`: admin finance stays

**d) Mixed responses: strip the vacancy parts, keep the rest** (server-side, so the UI can't leak them)
- **`CandidateKompasController`:** `TopMatches` is an empty list when OFF.
- **`RoleFitCheckController` / `RoleFitCheckBuilder`:** when OFF, `DirectVacancies = []`, `MapHref = null` and no vacancy counts.
  - `SimilarRoles`, `Strengths`/`Gaps`, `CultureFit*` and `CareerPath` stay. They're about occupations, not vacancies.
- **`CandidateCareerPathController`:** remove "vacatures voor deze stap" links and counts when OFF.
- **`CandidateMetricsController`:** return only the non-vacancy metrics (the `/home` bento tiles about views/applications are hidden).
- **`MockInterviewController`:** the vacancy-based mode returns 404 when OFF; the generic practice mode stays.
- **`AssistantController` / `AssistantChatService`:** disable the vacancy-search tool and its prompt hints when OFF.
- **`TrainingUpskillController`:** unchanged. Courses aren't employers.
- **Candidate onboarding** (`CandidateOnboardingController`, `OnboardingWizard.razor` ~3 vacancy mentions): skip or hide the job-search steps and the "zoek werk" copy when OFF.

**e) Components and sections inside pages that stay** (hide when OFF, using one helper, `<FeatureVisible Feature="Employers">`, not raw `if`s)
- **Vacancy UI:**
  - `HighlightVacancyCarousel`, `VacancyDiscovery`, `DirectContactModal`
  - `PushBomConfirmDialog`, `PublishOptionsDialog`, `TokenTopUpDialog`
  - `ShareModal`: only its vacancy usage
  - `PushPermissionBanner`: the vacancy-push copy
- **Kompas side column** `CompetencyMatchPanel` ("Vacatures die bij je passen"), both in the classic profile and in the paspoort.
- **Classic profile:**
  - the "Beschikbaar voor werk" toggle (`_openForWork`)
  - talent-pool consent
  - `CandidateOnboardingResumeCard`: only if it points to vacancies
- **Paspoort:**
  - "Beschikbaar voor werk" pill
  - vacancies list, employer contact requests and "Vacatures voor deze stap"
  - Top-10 link
  - talent-pool toggle
- **Header, footer and marketing:**
  - footer link `/westland` (`AppFooter.razor`)
  - `/register` links in `Login.razor` (~L130), `TeaserLayout.razor`, `WestlandTeaser.razor`, `GratisDnaUnder16View.razor`, `RegisterActivate.razor`
  - `Register.razor` breadcrumb to the banenkaart
- **SEO:**
  - `/sitemap.xml` and `/robots.txt` drop vacancy and company URLs.
  - `JobPosting` JSON-LD is not emitted.
  - `PageSeoCatalog` marks gated pages as non-indexable.
  - The home canonical points to `/ontdek`.

**f) Background jobs** (`Infrastructure/Jobs`, `Api/Jobs`): each job asks `IFeatureFlags` at the **start of every tick**.
- **Paused when OFF** (tick is skipped, one info log per state change):
  - `AtsScrapeHostedService` (vacancy imports)
  - `AtsVacancyHealthHostedService`
  - `VacancyDiscoveryIndexHostedService`
  - `VacancyEngagementReminderHostedService` (emails)
  - `DraftVacancyCleanupHostedService`: **deletes drafts**, so it must pause ("data untouched")
  - `CompanyReengagementHostedService` (emails)
  - `KvkVerificationRetryHostedService`
  - `UnconfirmedRegistrationCleanupHostedService`: **deletes pending company registrations**, so it must pause
  - `CultureFitRefineWorker` (candidate × vacancy fit)
  - `CandidateInsightsWorker`: employer-facing output
  - `DashboardCacheRefreshHostedService`: employer/regional dashboards only; the candidate part continues
- **Keeps running** (money or legal):
  - `TalentContactRefundHostedService`: pending refunds must still be paid back
  - `TokenCheckoutReconcileHostedService`
  - `VatBufferTransferHostedService`
  - `DataRetentionHostedService`: legal retention, unchanged
  - `MinimumWageUpdateHostedService`: masterdata
  - `AssessmentNormSnapshotHostedService`
  - `FeedbackAutomationPollHostedService`
  - `IbanEncryptionMigrationHostedService`
  - `DatabaseSeedHostedService` (unchanged)
- **No catch-up bursts.** When the flag goes back ON, reminder and re-engagement jobs count from "now". They never mail a backlog for the OFF period. Add a test for this.

**g) Emails** (`Core/Email/TransactionalEmails.cs`, 28 templates)
- Add `bool RequiresEmployers` to the template metadata. The central send path (the service behind `TransactionalEmails.Compose` / the mail sender) **drops** those when OFF and logs "suppressed: employers disabled".
- **Employer-dependent:**
  - ApplicationConfirmation, ApplicationVerificationCode, ApplicationHired, ApplicationFilledElsewhere
  - EmployerReactionAccepted, EmployerReactionRejected, EmployerContacting, EmployerNewApplication
  - CandidateWithdrawn, CandidateWithdrawnOtherJob
  - PushBom, PendingApproval, VacancyEngagementReminder, DraftVacancyCleanupWarning, CompanyReEngagement
  - RegistrationActivation, RegistrationCredentials
  - TakeoverEmailVerification, TakeoverRequest, TakeoverSubmitted, TakeoverApproved, TakeoverRejected
  - UserInvite, SalesManagerInvite, AmbassadeurInvite, CompanyApiKeyCredentials
- **Not dependent:** MailTest, AccountUnsubscribeVerification, and the non-catalog candidate mails (parental consent, login/MFA). `EmailCatalogController` still previews all of them for admin.

**h) Web push:** `WebPushNotificationService` sends no new-vacancy or employer-reaction pushes when OFF. Subscribing stays possible.

## 01.3 Landing, login and employer users when OFF
- **Home path.** `FeatureRoutes.HomeFor(user, flags)`, a pure function, is used by `FeatureRouteGate`, `AuthRedirects` and the logo link:
  - anonymous → `/ontdek`
  - candidate → `/candidate/profile` (file 02 changes this to `/candidate/paspoort` when the paspoort flag is ON)
  - admin → `/home`
  - employer-side roles → `/access-denied?reason=employers-off`
- **`AuthRedirects`:** `BanenkaartPath` usages (`CandidatePostLoginUrl`, `IsGenericPostLoginLanding`, `ResolveCandidateReturnUrl`) become flag-aware. With OFF, a vacancy `returnUrl` falls back to the home path.
- **Login page:**
  - Hides "Registreer je bedrijf".
  - Email/password, Google, Microsoft and MFA stay, because candidates and admins need them.
  - A user whose roles are **only** employer-side (EnterpriseManager, BranchManager, RegionalManager, Intermediary, plus SalesManager/Ambassadeur per D2) can log in but lands on `/access-denied?reason=employers-off`. Text (new key): "Lobsy staat even alleen open voor kandidaten. Je gegevens blijven bewaard. We laten het je weten als werkgevers weer welkom zijn." Plus a logout button.
  - Users with Candidate or Admin roles are unaffected.
- **Admin:** unchanged, sees everything. A calm `status-pill` in `SettingsAdmin` says "Werkgevers staan uit".

## Also in this file
- **Build §F completely:** the entity field `EmployersEnabled`, `IFeatureFlags`, `RequiresFeatureAttribute` and the three gates. File 02 adds only its own field.
- **§N refactor:** `CandidateItems(flags)` as a pure function with **today's order** and the employer items hidden when OFF. The paspoort-ON order is added in file 02.
- **New `docs/feature-flags.md`** listing every gated page, API, job and email (the expected list of the reflection test).

## Tests
- **Flag default** (migration and service): `EmployersEnabled == true`; the admin update round-trip works.
- **OFF, candidate nav:** `CandidateItems` has no Zoeken/Bewaard/Sollicitaties, in both passport states (§N).
- **OFF, pages:** a reflection test that every page in 01.2b carries the attribute. A Web test that `/`, `/vacancies/{id}`, `/candidate/match`, `/candidate/applications`, `/register` and `/employer/vacancies` redirect to the home path for anonymous, candidate and employer users.
- **OFF, API:** `GET api/vacancies`, `…/pins`, `POST api/applications`, `api/talent-pool/…`, `api/kvk/…` and `api/registration/…` return **404** `feature_disabled`. Admin endpoints still return 200. `api/mollie/webhook` still works.
- **OFF, mixed responses:**
  - Kompas `TopMatches` is empty.
  - RoleFit has `DirectVacancies` empty and `MapHref` null, but still has `Strengths`/`Gaps`.
  - The metrics response has no vacancy tiles.
- **OFF, jobs:** each paused job's tick does nothing (fake clock); the refund job still runs; there is no backlog mail after switching ON.
- **OFF, emails:** the employer templates are suppressed and AccountUnsubscribeVerification is still sent.
- **OFF, SEO:** the sitemap has no `/vacancies/` URLs.
- **ON (default):** the nav equals today's exactly; all existing tests are green; no gate triggers.
- **Playwright** (new `WerkgeversUitPlaywrightTests.cs`): admin switches OFF, then as anonymous `/` lands on `/ontdek`; the candidate nav shows only the remaining items; switch back ON.

## Success criteria (all must hold before you open the PR)
- With Werkgevers actief **ON** (default), nothing changes: the nav, the routes and all existing tests are identical.
- With **OFF**:
  - the candidate nav has no Zoeken, Bewaard or Sollicitaties
  - every page in 01.2b redirects to the home path
  - every API in 01.2c returns 404 `feature_disabled`
  - the mixed responses in 01.2d carry no vacancy data
  - the paused jobs skip their ticks and the refund job still runs
  - the employer emails are suppressed
  - the sitemap has no vacancy URLs
  - employer-only users land on `/access-denied?reason=employers-off`
- Switching back ON restores everything, and no data was changed or deleted.
- Admin still sees and uses everything; `api/mollie/webhook` keeps working.
- `dotnet build` and `dotnet test` are green. Any existing test you changed has its reason in the PR body.
- The PR body has: stacked-on line, what and why, screenshots (where there is UI), test list, "Out of scope / deferred".

## Done → next
Push, open the PR, note its number. Then continue with **`01b-opleidingen-seed-opruimen.md`**. If anything above is red, stop and report (see 00-README "How to run" step 3).

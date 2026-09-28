# Lobsy code review ("grote schoonmaak")

- **Repo:** `dennisbemer1007-pixel/Jobsy`, branch `origin/acceptatie` at `ccf6976` (merge "gratis-dna", 28-09-2026 02:22 CEST).
- **Reviewed:** 28-09-2026, 06:20–08:30 CEST. Read-only. No application code was changed.
- **Related:** the performance findings live in `/workspace/jobsy-design/review/performance-audit.md` (called **PERF-#** below). They are referenced here, not repeated.
- **Raw data** is in [`docs/review/data/`](data/):
  - `analyzer-counts.txt`, `analyzer-unused-and-bugs.txt`: Roslyn analyzers.
  - `jscpd-top-clones.tsv`: clone detector.
  - `dead-css-rules.tsv`: CSS usage scan.
  - `unused-members-heuristic.txt`: cross-reference scan.
  - `uistrings-keys-without-literal-reference.txt`: localisation scan.
- **Cleanup prompts:** [`docs/prompts/cleanup/`](../prompts/cleanup/README.md).

---

## Samenvatting voor Dennis (Nederlands)

**Kort oordeel:** Lobsy is functioneel rijk en verrassend goed getest: 2.383 automatische tests, allemaal groen in 11 seconden. De code bouwt vrijwel zonder waarschuwingen. Beveiliging is op veel plekken doordacht (CSP met nonce, rate limits, fallback "altijd inloggen" op de API). Voor een derde partij is de code wel lastig over te nemen, om vier redenen:

1. **Rollen: twee echte lekken.** Een **regiomanager**, die alleen mag kijken, kan via de API toch dingen doen:
   - tokens uitgeven om contactgegevens van kandidaten te ontgrendelen (talentpool "unlock"), en zo'n aanvraag intrekken;
   - de bedrijfscultuurscan opslaan;
   - een onboarding-betaling starten.

   Er is geen test die "regiomanager = alleen lezen" bewaakt. Isolatie tussen bedrijven en vestigingen is wél centraal geregeld en ziet er goed uit.
2. **Privacy bij admin.** Admin ziet standaard álle persoonsgegevens:
   - de volledige gebruikerslijst met naam en e-mail, zonder paginering;
   - alle sollicitaties met naam, e-mail, adres, telefoon en leeftijd;
   - cv-downloads.

   Er wordt niet gelogd wie welke persoonsgegevens heeft bekeken, en IBANs staan onversleuteld in de database. Advies: standaard geanonimiseerde admin-schermen, tijdelijke toegang met reden, en een audit-log (prompts 05 en 06).
3. **Opruimwerk.**
   - Circa 1.500 regels CSS worden nergens gebruikt.
   - 4 componenten en ruim 10 afbeeldingen zijn ongebruikt.
   - 8 losse JS-bestanden zijn verouderde kopieën van gebundelde bestanden; 2 daarvan wijken al af, en tests testen die verouderde kopieën.
   - Een stuk OpenAI-code staat 11 keer gekopieerd.
4. **Grote bestanden.** Een paar bestanden zijn te groot om veilig in te werken: `app.css` (21.659 regels), de API-client (5.212), `jobMap.js` (3.836) en `VacancyDiscovery.razor` (3.466). De README beschrijft nog "Jobsy met Leaflet".

**Advies:** los eerst de rol- en privacypunten op (03–06, hoge prioriteit). Zet parallel de vangrails neer (01, ongevaarlijk; 02 na de SalesWalletChip-PR). Ruim daarna in kleine, veilige stappen op (07–15; volgorde in `docs/prompts/cleanup/README.md`). De code-namespace "Jobsy" laten we staan; dat leggen we vast in een ADR. Hernoemen kost veel en levert niets op.

---

## Metrics

### Lines of code (git-tracked, excl. `bin/obj`, `wwwroot/lib`, `*.min.*`)

| Project | Files | C# | Razor | CSS | JS | Notes |
|---|---|---|---|---|---|---|
| Jobsy.Core | 423 | 31,410 | – | – | – | domain, rules, interfaces |
| Jobsy.Infrastructure | 316 | 49,125 (+158,946 in 153 migration files) | – | – | – | EF Core, services, jobs |
| Jobsy.Api | 101 | 22,208 | – | – | – | 66 controllers, 351 actions |
| Jobsy.Web | 424 | 25,380 | 49,734 | 23,806 | 8,081 | Blazor Server UI, 208 `.razor` |
| Jobsy.Tests | 237 | 44,038 | – | – | 2 `.mjs` | 228 test files, 1,214 `[Fact]` + 78 `[Theory]` |

### Largest source files (god files)

| Lines | File |
|---|---|
| 21,659 | `Jobsy.Web/wwwroot/css/app.css` (+ hand-synced `app.min.css`, 338 KB) |
| 5,212 | `Jobsy.Web/Services/JobsyApiClient.cs` (303 async methods + 68 DTO types in one file) |
| 3,836 | `Jobsy.Web/wwwroot/js/jobMap.js` |
| 3,466 | `Jobsy.Web/Components/VacancyDiscovery.razor` |
| 3,394 / 2,262 / 1,018 | `Jobsy.Web/Localization/UiStrings.cs` / `UiStringsExtras.cs` / `UiStringsCompetencies.cs` |
| 2,641 | `Jobsy.Api/Controllers/VacanciesController.cs` |
| 2,000 | `Jobsy.Web/Components/Pages/VacancyDetail.razor` |
| 1,921 | `Jobsy.Web/Components/Pages/Candidate/Profile.razor` |
| 1,800 | `Jobsy.Web/Components/Pages/Branch/CreateVacancy.razor` |
| 1,795 | `Jobsy.Infrastructure/Services/CompanyRegistrationService.cs` |
| 1,727 | `Jobsy.Infrastructure/Data/JobsyDbContext.cs` |
| 1,667 / 1,629 | `Jobsy.Api/Controllers/MeController.cs` / `ApplicationsController.cs` |
| 1,419 | `Jobsy.Web/Auth/AuthServiceCollectionExtensions.cs` (also holds `DemoUserStore` and `LocalApiLoginProfile`) |

### Build and warnings

The SDK used was 9.0.318 (`global.json` 9.0.100 with `rollForward latestFeature`).

- **Default build** (`dotnet build Jobsy.sln`):
  - 1 compiler warning: `Jobsy.Api/Program.cs(146,18)` CS8602.
  - 1 NuGet warning: **NU1902 AngleSharp 1.3.0 has a known moderate vulnerability** (GHSA-pgww-w46g-26qg).
  - No `.editorconfig`, no `Directory.Build.props`, no analyzers configured.
- **With `AnalysisMode=Recommended` + `EnforceCodeStyleInBuild`** (IDE0005/0051/0052/0059/0060 raised to warning): **2,193 unique warnings**.

| Rule | Count | Meaning |
|---|---|---|
| CA1707 | 1,292 | underscores in test method names (xUnit style; suppress for tests) |
| CA1848 | 235 | use LoggerMessage delegates (perf, low priority) |
| IDE0005 | 193 | unused `using` |
| CA1305 / CA1304 / CA1311 | 108 / 55 / 55 | culture-less `ToString` / `ToLower` (NL/EN/PL/RO/AR app: relevant) |
| CA1859 / CA1861 / CA1862 | 56 / 53 / 48 | perf nits |
| IDE0060 | 14 | unused parameters |
| IDE0051 / IDE0052 | 8 / 4 | unused private members (7 / 2 in production code) |
| CA2208, CA2016, CA1001, CA5350 | 4, 1, 1, 1 | see findings |

### Tests and coverage

- **Run:** `dotnet test` with Playwright classes excluded. Result: **2,383 passed, 0 failed, 11 s**.
- **Line coverage** (coverlet, same run, excl. migrations):

| Project | Line coverage |
|---|---|
| Core | 80.7% |
| Infrastructure | 64.7% |
| Api | 41.9% |
| Web | 24.3% |

- **Test style:**
  - 73 test files contain **412 `File.ReadAllText(...)` source-text assertions**. These tests grep `.razor`, `.js` and `.css` files instead of exercising behaviour.
  - 68 hard-coded `?v=` asserts in 12 files.
  - bUnit is used in **1** file (`GratisDnaBunitTests.cs`).
  - 12 Playwright classes.
  - All 228 files sit in one flat folder, many named after sprints (`Sprint1ShellTests.cs` … `Sprint8PolishTests.cs`, `Acc2709PlaywrightTests.cs`).

### Duplication (jscpd 4, min 70 tokens, excl. migrations, lib and min files)

| Format | Lines scanned | Clones | Duplicated lines | % |
|---|---|---|---|---|
| C# | 172,155 | 335 | 6,300 | 3.66% |
| Razor (as markup) | 49,796 | 50 | 984 | 1.98% |
| JS | 8,082 | 11 | 1,178 | **14.58%** |
| CSS | 28,734 | 2 | 16 | 0.06% (exact clones only; near-duplicates not measured) |
| **Total** | 258,767 | 398 | 8,478 | **3.28%** |

### Dead-code count (details in §1)

| Category | Count | Confidence |
|---|---|---|
| Unused private members / write-only fields (IDE0051/52, production code) | 7 + 2 | certain |
| Unused public/internal methods (cross-reference) | 65 candidates, ~35 production-only-dead | likely (see list) |
| Unused Razor components | 4 (457 lines) | certain (no route, no tag, no DynamicComponent) |
| Unused `wwwroot` assets | 11 files (incl. `mascot.png` 567 KB) | certain/likely |
| Stale JS source copies (not served) | 8 files, 2 already drifted | certain |
| Dead CSS rules | 286 rules / ~1,590 lines (272 "safe", 14 deferred to pending PRs) | likely (class-token scan) |
| UiStrings keys without literal reference | 238 of 2,109 | verify (dynamic keys) |
| Commented-out code blocks | 1 | good |

---

## Findings

Severity: **critical / high / medium / low**. Effort: **S** (< ½ day) / **M** (1–2 days) / **L** (> 2 days). The prompt that fixes each finding is shown as → **P##**.

### 0. Role and privacy findings (added on request; high priority)

**Role model as implemented:**
- `Jobsy.Core/Authorization/JobsyRoles.cs` defines the roles Candidate, BranchManager, RegionalManager, EnterpriseManager (= bedrijfsmanager), Intermediary, Admin, SalesManager and Ambassadeur. **There is no Decaan role yet.**
- Tenant scope is resolved centrally in `Jobsy.Infrastructure/Services/CompanyAuthorizationService.cs`:
  - Admin: `null` (= all).
  - BranchManager: only `User.CompanyId` (:143-147).
  - EnterpriseManager: `CompanyId` + memberships + all child vestigingen (:160-164).
  - RegionalManager and Intermediary: `CompanyId` + explicit `UserCompanies` memberships.
  - API keys: key company + children.
- The API has a **fallback policy "authenticated user"** (`Jobsy.Api/Authorization/AuthorizationExtensions.cs:149`). All 330 async actions take a `CancellationToken`.
- The Blazor Web project has **no database access**. Every page goes through the API with the user's token, so the API is the single enforcement point. That is a good design.

**Scan method:** a script classified all 344 actions by effective `[Authorize]` (method, then class), HTTP verb, and whether the action body performs a scope check (`EnsureCanAccessCompanyAsync`, `GetAccessibleCompanyIdsAsync`, `[RequireCompanyAccess]`, `Ensure*Access`, `RunProductAsync` and so on). The employer endpoints with an `{id}` or `companyId` that the scan flagged were then read by hand. Vacancy lifecycle, applications (`react`, `contact`, `fulfill`), company settings (`EnsureCanMutateEmployerCompanySettingsAsync`), company users (role assignment via `EmployerInviteRules.CanAssignRole`), salary tables and regions **all scope-check correctly**. No cross-company IDOR was found in the code paths reviewed.

#### 0.1 [HIGH] RegionalManager (read-only by design) can mutate data and spend tokens → P03
Endpoints use `RequireEmployer` / `RequireAdminOrEmployer`, and both policies include RegionalManager (`AuthorizationExtensions.cs:157-162`):

| Endpoint | File:line | Effect for a regiomanager |
|---|---|---|
| `POST /api/employer/talent/unlock` | `TalentPoolController.cs:72` (class policy `RequireEmployer` :13) | **Spends company tokens and gets a candidate contact (PII)** |
| `POST /api/employer/talent/{requestId}/withdraw` | `TalentPoolController.cs:99` | withdraws and refunds a request |
| `PUT /api/company/culture` | `CompanyCultureController.cs:45` (class :12) | overwrites the company culture scan |
| `POST /api/companies/{companyId}/onboarding/checkout` and `/complete` | `SupplierOnboardingController.cs:28/57` (class :10) | starts and completes a supplier onboarding payment (verify intent) |

- **UI mirrors the problem:** `Employer/TalentPool.razor:3`, `Employer/TalentContacts.razor`, `Employer/CultureScan.razor:4` and `Employer/OnboardingCheckout.razor` all allow `RegionalManager`.
- **UI shows buttons the API rejects** (API is correct, UX is confusing): `Employer/Tokens.razor` (Allocate/Checkout), `Regional/TokenControl.razor` (Allocate), `Employer/Branches.razor` (Invite).
- **No test** covers "regiomanager is read-only" (`rg RegionalManager Jobsy.Tests` shows 9 files, none about talent, culture or onboarding).

#### 0.2 [HIGH] No systematic role × endpoint × scope test net → P04
- Scope rules are correct today, but they live in ~60 hand-written checks spread over the controllers.
- A new endpoint that forgets `EnsureCanAccessCompanyAsync` would compile and pass all tests.
- Recommend a reflection-based "authorization matrix" test that fails on any mutating action reachable by RegionalManager (outside an allow-list: `me/*`, notifications, device-sessions, privacy, push, likes, assistant, dashboard refresh). Pair it with cross-tenant integration tests: BranchManager A hits company B's vacancy/application/settings and gets 403/404; RegionalManager hits a vestiging outside its memberships and gets 403.

#### 0.3 [MEDIUM] RegionalManager scope is membership-based, not region-based (verify) → P04 (test) / backlog
- `CompanyAuthorizationService` never reads `Region`. A regiomanager sees exactly the `UserCompanies` rows written at invite time (`CompanyUsersController.cs:321-341`).
- If an enterprise manager moves a vestiging to another region (`RegionsController` PUT), the old regiomanager keeps access and the new one does not get it.
- Decide whether "own region" should be computed from `Region` membership. Until then, document it in the roles matrix.

#### 0.4 [LOW] Side effect before authorization in a GET → P03
`SalaryTablesController.cs:131-146` (`GET /api/salary-tables/{id}`) runs `WmlSalaryTableService.FillEmptySalaryTablesAsync` (a **write**) for the table's organisation **before** `EnsureCanManageOrganizationTablesAsync` (:163). Any employer who knows a table id can trigger writes on another organisation's tables. The data written is deterministic WML fill, so impact is low, but this is the wrong order.

#### 0.5 [HIGH] Admin sees all personal data by default, and there is no access audit trail → P05, P06

**Personal data admins can reach:**

| Surface | Personal data exposed | Evidence |
|---|---|---|
| `GET /api/admin/users` → `Admin/UsersAdmin.razor` | email, full name, role, company, active flag, memberships of **every** user (candidates incl. minors), **unpaginated** | `AdminController.cs:214-232`, DTO `Jobsy.Api/Models/Sprint6Dtos.cs:21` |
| `GET /api/applications` (admin = no scope filter) → `Branch/Applicants.razor` | candidate name, email, **address**, city, **phone** (`SnapshotPhoneNumber`), **age**, employer count for **all** applications, **unpaginated** (no `Take`) | `ApplicationsController.cs:67-125` (fields at ~:98-122) |
| `GET /api/applications/{id}/lobsy-cv.pdf`, `/uploaded-cv` | full CV (admin allowed) | `ApplicationsController.cs:188/263` (`IsAdmin` branch) |
| `GET /api/sales-managers`, `/applications` → `Admin/SalesManagersAdmin.razor` | email, name, tracking code, applicant emails (`:58`, `:87`); IBAN stored on the profile | `SalesManagersController.cs`, `Core/Entities/SalesManagerProfile.cs:19` |
| `GET /api/ambassadeurs` → `Admin/AmbassadeursAdmin.razor` | email, name, IBAN on the profile | `AmbassadeurProfile.cs:20` |
| `GET /api/feedback/{id}`, `/screenshot` → `Admin/FeedbackAdmin.razor` | free text + **screenshots of user screens** (may show any PII) | `FeedbackController.cs` |
| `GET /api/platform-logs` → `Admin/LoggingAdmin.razor` | log messages / `DetailsJson` (content depends on writers) | `Core/Entities/PlatformLog.cs` |
| `GET /api/tokens/finance/*` (+ CSV export) | buyer company, invoice data, possibly contact names | `TokenFinanceController.cs` |
| `GET /api/settings/email-templates` + send | sends mails to arbitrary addresses | `EmailCatalogController.cs` |

**Gaps:**
- No "who viewed what" log exists. `PlatformLog` has no actor or subject column (`PlatformLog.cs:5-13`), and there is no audit entity in `JobsyDbContext`.
- **IBANs are stored in plain text** (`JobsyDbContext.cs:1402/1430/1455`, `HasMaxLength(34)`, no value converter or protector). Only `SalesManagerPayoutCheckout.MaskedIban` is masked.
- The admin role is all-or-nothing. There is no "support" role and no time-limited, reasoned access.
- **Proposal (P05):** aggregated/masked admin views by default (counts per role/region; `j***@gmail.com`, initials, age band instead of age; no address or phone), pagination, a `PersonalDataAccessLog` (actor, subject user, purpose, endpoint, timestamp, IP hash), and IBAN encryption at rest.
- **Proposal (P06):** "break-glass" support access, where an admin states a reason and gets full data for **one** subject for 60 minutes; the grant is logged and visible to other admins.

#### 0.6 [MEDIUM] Candidate data goes to OpenAI from 11 services (verify DPA/DPIA)
- 11 services call `chat/completions`: `WhoAmIGenerationService`, `CvExtractionService`, `AssistantChatService`, `MockInterviewService`, `CareerCompassGenerationService`, `CareerPathPlanGenerationService`, `RoleFitCheckService`, `CultureFitAiService`, `OpenAiTranslationService`, `OpenAiCompetenceDeepReportAiService`, `VacancyContentModerationService`.
- No age or minor check was found in the four candidate-facing ones (`rg IsMinor|ParentalConsent` → 0).
- This is a legal/DPIA check (processor agreement, minors, data minimisation), not a code bug. Document it in `SECURITY.md` / a privacy register.

### 1. Dead and unused code

| # | Item | Evidence | Confidence | Sev / Effort |
|---|---|---|---|---|
| 1.1 | 7 **unused private members** + 2 write-only fields (production code) | `ApplicationsController.Html` (:1628), `VacanciesController.ResolveIntermediaryOrganizationIdAsync` (:1610), `DemoUsersSeeder.EnsureDemoPasswordAsync` (:257), `CandidateInsightsComputer.ReadValues` (:350), `PrivacyDataService.Html` (:1339), `TrainingUpskillService.CombineUrl` (:384), `AuthServiceCollectionExtensions.TryLocalApiLoginAsync` (:782); write-only fields (IDE0052): `DeviceSessionsController._db` (:19), `LobsyCvPdfService.CellOff` (:26) | certain (Roslyn) | low / S → P08 |
| 1.2 | **Unused Razor components** (no `@page`, no tag usage, no `DynamicComponent`/`typeof` anywhere) | `Components/Layout/MatchMobileLayout.razor` (26 lines; `design-system.mdc` still lists it as a layout), `Components/Pages/Candidate/KompasTabBar.razor` (22), `ValuesScorePanel.razor` (47), `WhoAmIPanel.razor` (362). **`WhoAmITests.cs:83` and `Uat/UatScriptRunner.cs:521` assert on the source of the unused `WhoAmIPanel.razor`**, which gives false assurance. | certain | medium / S → P08 |
| 1.3 | **Unused public API-client methods** | `JobsyApiClient.GetMyKompasDnaAsync` (:1362), `UpsertTrainingProviderAsync` (:1442), `DeleteTrainingProviderAsync` (:1455), `UnsubscribeWebPushAsync` (:2343), `GetPublicBrandingAsync` (:3878), `CreateMySelfBillingInvoiceAsync` (:4205) | likely (no caller in Web; API endpoints stay) | low / S → P08 (after the API-client PR) |
| 1.4 | Other unused public helpers (production callers = 0, some used only in tests) | e.g. `JobsyRoles.CanCreateVacancies`/`RequiresCompanyLink`, `CircuitInterop.InvokeVoidIgnoredAsync`/`InvokeIgnoredAsync`/`CreateLinkedCts`, `SwipeViewModel.CreateDemoDeck`, `ApiCallTracker.GetCount`, `DreamJobCatalog.FindByKey/FindByTitle`, `DashboardMemoryCache.ScopePrefix`, `VacancyWageResolver.ResolveAdultHourlyWage`, `JobsyAccessToken.GenerateDevelopmentKeyPair`. Full list with test-reference counts: `data/unused-members-heuristic.txt` (65) | likely / verify (list includes test-only helpers: keep if the test documents a rule) | low / S → P08 |
| 1.5 | **Stale JS source files (not served)** | `app-core.js` is a hand concatenation of `geo.js` + `culture.js` + `cookieConsent.js` + `maps-loader.js` + `extras-loader.js` (+ inline `lobsyPwaInstall`), and `app-extras.js` = `sessionIdle.js` + `download.js` + `richtext.js` (see file headers). Only the bundles are loaded (`App.razor:251`; `extras-loader` loads `app-extras`). **`cookieConsent.js` has drifted (52 vs 117 lines in the bundle: `watchKompasWide`, `jobsyList.observeMore`) and so has `extras-loader.js` (42 vs 81 lines: `jobsyPageVisible`, `jobsyQuestionnaire`).** 20+ tests read the stale split files (`CookieConsentAnalyticsTests.cs:12`, `HomepagePerformanceGuardTests.cs:30/94/139`, `FeedbackWidgetGuardTests.cs:81`, `Jobsy.Tests/js/jobMap-pins-304-reuse.test.mjs:13`, …). | certain | **high** / M → P09 |
| 1.6 | **Unused `wwwroot` assets** | `images/flags/{gb,nl,pl,ro,sa}.svg` (no reference), `images/brand/mascot.png` (567 KB), `mascot-180.png/.webp`, `mascot-256.png`, `lobsy-180.webp`, `lobsy-256.png` (not in `Media/BrandImages.cs`), `images/maps/README.md`, `images/brand/README-dennis.md` (says `dennis.jpg` is missing, but it exists), `image-cache-sw.js` (only in middleware allow-lists `SessionInactivityMiddleware.cs:112`, never registered) | certain (images) / verify (`image-cache-sw.js`: old installs may still have it registered) | low / S → P09 |
| 1.7 | **Dead CSS** | 286 rules (~1,590 lines) whose every selector contains a class with zero references in `.razor/.cs/.js/.html` (dynamic prefixes such as `foo--@x` or `` `foo-${}` `` excluded). Largest groups: `profile-hub*` (35 classes), `applicants-grid__*`, `whoami-disc*`, `share-modal__*` children, `login-demo*`, `mock-interview-*`, `competency-radar*`, `ats-*`, `client-perf-*`; plus `questionnaire.css` `q-likert__opt--*` / `questionnaire__status-row…` (25 rules, 114 lines). 41 of these rules name classes that tests assert on. List: `data/dead-css-rules.tsv` (SAFE vs DEFER). | likely | medium / M → P10 |
| 1.8 | Redundant NuGet references | `Jobsy.Infrastructure.csproj`: `Microsoft.AspNetCore.DataProtection.Abstractions` (transitive of `DataProtection`); `Jobsy.Web.csproj`: `System.IdentityModel.Tokens.Jwt` (no direct usage in Web; Core already references it) | certain / likely | low / S → P02 |
| 1.9 | Legacy redirect-only pages | `/admin/cockpit` (`Admin/AdminHome.razor`, redirects to `/home`), `/lancering`, `/werven/{code}` + `/ambassadeur/ref/{code}` aliases, `/candidate/vacancies` (tests only) | verify (bookmarks and printed flyers) | low → keep, document in the routes list (P15) |
| 1.10 | Root tooling leftovers | `package.json` (only `pptxgenjs`, used for `docs/demo/Jobsy-Presentatie.pptx`, 3 MB), `WebAssemblyMarker.cs` (misleading name in a Blazor Server app; only a test host marker) | certain | low / S → P15 |
| 1.11 | Config keys, commented-out code, feature flags | all `appsettings*.json` keys are referenced; 1 commented block (`DeepAnalysisCatalog.cs:6`); feature flags are DB-driven (`IPlatformFeatureService`), so whether a flag is always on/off **cannot be judged from code** | – | good / verify flags in the prod admin |

### 2. Duplication

| # | Clone | Evidence | Proposal | Sev / Effort |
|---|---|---|---|---|
| 2.1 | **OpenAI settings resolution copied 11×** | `ResolveApiKeyAsync` / `ResolveModelAsync` / `ResolveBaseUrlAsync` (~45 lines each, identical: DB credential first, then options, then the `https://api.openai.com/v1/` fallback) in the 11 services of 0.6. jscpd: `WhoAmIGenerationService.cs:169-216` ↔ 8 files. | New `Jobsy.Infrastructure/Services/OpenAi/OpenAiEndpointResolver` (`ResolveAsync(ct)` → `OpenAiEndpoint(ApiKey?, Model, BaseUrl)`), registered once; services keep their own prompts. ≈ 450 lines removed. | medium / M → P11 |
| 2.2 | **JS bundles duplicate their sources** | jscpd 1,178 duplicated JS lines = 14.6% (`app-extras.js:4-363` ↔ `sessionIdle.js`, `app-core.js:4-333` ↔ `geo.js`, `:479-742` ↔ `maps-loader.js`, …) | see 1.5 | high → P09 |
| 2.3 | Questionnaire pages | `Candidate/CultureScan.razor:152-241` ≡ `ValuesScan.razor:151-240` ≡ `CareerTest.razor:211-300` (90 lines), `CompetencyTest.razor:201-236`, `CultureScan.razor:61-101` ≡ `ValuesScan.razor:60-100` | `Components/Shared/Questionnaire/QuestionnairePageShell.razor` (header, progress, pager, finish), next to the existing `QuestionnaireFlow.cs` | medium / M → P12 |
| 2.4 | Payout checkout stubs | `Ambassadeur/PayoutCheckoutStub.razor` ≡ `SalesManager/PayoutCheckoutStub.razor` (differ only in 12 lines), plus `Employer/PartnerSalesPayoutCheckoutStub.razor` | `Components/Shared/PayoutCheckoutStubView.razor` with parameters (role label, API call) | low / S → P12 |
| 2.5 | Ambassadeur ↔ SalesManager backend | `AmbassadeurOnboardingService` ↔ `SalesManagerOnboardingService` (89 lines), `AmbassadeursController` ↔ `SalesManagersController` (77), `AmbassadeurFlyerPdfService` ↔ `PartnerFlyerPdfService` (71) | Defer: Ambassadeur is unfinished. When finishing it, build on a shared "partner affiliate" base (`PartnerAffiliateService` already exists) | low / L → backlog |
| 2.6 | Web ↔ API DTO mirrors | ≈ 55–62 name-mirrored pairs (`ApplicationDto`/`ApplicationItem`, `TokenBalanceDto`/`TokenBalance`, `SalaryTableDto`/`SalaryTableItem`, `AtsListingDto`/`AtsListingItem`, …). 209 records in Api, 220 model types in Web; `Jobsy.Web` references only `Jobsy.Core`. | Longer term, a `Jobsy.Contracts` project (DTOs only), migrated area by area; short term, JSON contract tests for the busiest pairs. Not a first-wave cleanup. | medium / L → backlog (ADR in P15) |
| 2.7 | Security plumbing duplicated in Api and Web | `Jobsy.Api/Security/CloudflareOriginMiddleware.cs` ↔ `Jobsy.Web/Security/CloudflareOriginMiddleware.cs` (72 lines); `Infrastructure/Security/DataProtectionExtensions.cs:67-153` ↔ `Web/Security/DataProtectionSetup.cs:122-208` (87 lines; Web re-implements key loading with raw `Npgsql` because it cannot reference Infrastructure) | Small `Jobsy.Hosting` class library (ASP.NET-only, no EF) referenced by both hosts. Security-sensitive: do it with tests, later. | low / M → backlog |
| 2.8 | Culture-change boilerplate | `Culture.Changed +=` / `-=` + `InvokeAsync(StateHasChanged)` in **83** components | `CultureAwareComponentBase`; migrate gradually (touching 83 files at once would conflict with every UI PR) | low / M → P12 (base class + 5 files) |
| 2.9 | JS-interop "ignore disconnect" | **166 empty `catch {}` blocks** (18 in `VacancyDiscovery.razor`, 7 in `VacancyDetail.razor`), while `Hosting/CircuitInterop.cs` already offers `InvokeVoidIgnoredAsync` and it is **unused** | use `CircuitInterop` (catches only `JSDisconnectedException`/`TaskCanceledException`) | medium / M → P12 (non-pending files) |
| 2.10 | `UiStrings.cs` internal clones | `:1973-2038`, `:2642-2707`, `:3311-3376` ≡ `:1304-1369` (66 lines: pl/ro/ar blocks copied from nl) | see 3.6 | medium → P14 |
| 2.11 | Tests | Playwright bootstrapping copied (`BanenkaartMapReusePlaywrightTests.cs:181-259` ↔ `BlazorReconnectAndHealthzTests.cs:208-297`, …); API test setup copied (`VacancyCultureFitTranslationApiTests.cs:224-291` ↔ 8 files) | `Jobsy.Tests/Infrastructure/PlaywrightFixture.cs`, `ApiTestHost.cs` | low / M → backlog |

### 3. Architecture and structure

| # | Finding | Evidence | Recommendation | Sev / Effort |
|---|---|---|---|---|
| 3.1 | **Layering is sound** | Core has no dependencies (only `IdentityModel` for tokens); Infrastructure → Core; Api → Core + Infrastructure; Web → Core only (HTTP to the API). | Keep it. Write it down (ARCHITECTURE.md is 18 lines). | good → P15 |
| 3.2 | Controllers are thick | 27 of 66 controllers inject `JobsyDbContext` directly; **371 `_db.` calls in controllers** (MeController 55, Applications 37, Vacancies 32, Companies 30). Business rules mix with HTTP. | New endpoints go through services; extract when touching. No big-bang. | medium / L → ADR (P15) |
| 3.3 | God files | see Metrics. `JobsyApiClient.cs` has 303 methods + 68 DTOs in one file; `app.css` is 21,659 lines with a **hand-synced** `app.min.css` (no minifier in the repo: `rg minif` finds nothing in csproj/yml/json). | Split `JobsyApiClient` into `partial` files per area + `Models/Api/*.cs` (pure moves) → P13. Generate `app.min.css` with a pinned minifier + a CI diff check → P10. Split the CSS per area → PERF-8. | high / M |
| 3.4 | Component folders mixed | 22 `.razor` files under `Components/Pages/**` have no `@page` (panels such as `DnaPanel`, `*HomePanel`, `KompasTabBar`, `ScoreRadarChart`); about 40 loose components in `Components/` root; `Components/Employer` vs `Components/Pages/Employer`, `Components/Public` vs `Pages/Public`. List: `rg -L '^@page' Components/Pages`. | `Pages/` = routable only; panels move to `Components/<Area>/`. Pure moves → P13 (after UI PRs). | low / M |
| 3.5 | Error handling | 390 `catch (Exception …)`, 225 bare `catch`, **166 empty catch blocks** (mostly Web). **9 `async void` handlers**, of which `VacancyDetail.razor:1133` (`OnCultureChanged` → `LoadAsync` has `try/finally` but no `catch`) and `BottomNav.razor:62` have **no try/catch**: an API failure during a language switch or navigation throws on the renderer and can kill the circuit. | Wrap in try/catch + log; use `CircuitInterop`. → P07 (Liked/Shared/Applications/Discovery already catch) | medium / S |
| 3.6 | Localisation | Dictionary-based catalog: 2,109 keys × 5 languages. All languages have every key, but **pl/ro/ar each contain 683 values identical to nl** (strings > 12 chars), because 6 of 9 `UiStrings*.cs` partials only define `Nl()`/`En()` and merge `Nl()` into pl/ro/ar (e.g. `UiStringsCompetencies.cs:14-18`). 238 keys have no literal reference (`data/uistrings-keys-…txt`, verify dynamic keys). About 241 Razor text nodes look like hard-coded Dutch (mostly `Legal/*`, `CnameSetupHelpPanel`, `Employer/Tokens.razor`, `CreateVacancy.razor`), plus Dutch API error messages. | Add a parity/"untranslated" report test (warn, not fail) and a translation backlog. Legal pages may stay NL-only (document it). → P14 | medium / M |
| 3.7 | Configuration and secrets | Only dev placeholders are committed (`appsettings.Development.json`: `local-dev-*` secrets, demo passwords `Jobsy123!`). Render secrets use `sync: false`. Sentry `SendDefaultPii=false` in both hosts. Integration credentials are stored in the DB via the admin. | Good. Add a gitleaks step in CI (P01). Document secret sources in ONBOARDING (P15). | low |
| 3.8 | Background jobs | 18 hosted services in the API process on a 0.5-CPU starter plan (see PERF-10) | Document them. Consider a worker service later. | low |
| 3.9 | DI | `Infrastructure/DependencyInjection.cs` 455 lines, 132 registrations, 13 near-identical `ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler …)` blocks | helper `AddJobsyHttpClient<T>(name, timeout)`; the Web API client pooling is PERF-2 | low / S → backlog |
| 3.10 | CI | `pr-tests.yml` builds, runs tests with Postgres and runs a Playwright smoke; `acceptatie-smoke.yml` every 30 min; Dependabot config exists **only on `acceptatie`** (added 26-09). GitHub reads it from the default branch `main`, so it is **not active yet**, and 0 Dependabot PRs have ever been opened. No format/analyzer gate, no secret scan, no vulnerability gate (NU1902 goes unnoticed). The Playwright test filter list is duplicated 3× in the workflows. | P01 (quality workflow), P02 (packages). Dependabot starts working after the next release to `main`. | medium / S |
| 3.11 | Repo hygiene | 387 remote branches (377 `cursor/*`); 12 open PRs from #131 up to #303, several stale; 18 MB of PNG/PPTX under `docs/`; `.git` is 40 MB | branch cleanup and closing stale PRs are Dennis's call (not done here) | low |

### 4. Naming

| # | Finding | Evidence | Recommendation |
|---|---|---|---|
| 4.1 | **Jobsy vs Lobsy** | about 6,800 "jobsy" occurrences in C#/Razor vs about 2,050 "lobsy"; namespaces `Jobsy.*`; DB `JobsyDb`; headers `X-Jobsy-*` (11); 23 `jobsy.*` localStorage/cookie keys (e.g. `jobsy.origin`, `Jobsy.CookieConsent`); CSS `.jobsy-*` (20 classes) vs `.lobsy-*` (60); JS `window.jobsy*` (14) vs `window.lobsy*` (5); user-visible "Jobsy" in UiStrings: 23 lines to check | **Keep code identifiers `Jobsy`** (renaming namespaces, DB, headers, storage keys and cookies = migrations, logged-out users, lost consent, broken Render/GitHub links, for no user value). **Do** fix user-visible text (UiStrings, emails, README, page titles). Record it in `docs/adr/0001-keep-jobsy-code-name.md`. Keep the repo name (GitHub redirects exist, but Render blueprints, webhooks and bookmarks point at `Jobsy`). → P15 (+ P14 for visible strings) |
| 4.2 | C# conventions | Generally .NET-conform (PascalCase, `_camel` fields, async suffix). Exceptions: `WebAssemblyMarker` (misleading), `Sprint6Dtos.cs` (sprint name as file name), DTO suffixes mixed (`…Dto` in Api, `…Item`/`…Result`/`…Wire` in Web), CA1068 ×6 (`CancellationToken` not last: `ICandidateCareerInterestService.GetAsync`, `IDeviceSessionService.CreateAsync`, …) | `.editorconfig` naming rules (P01); rename on touch |
| 4.3 | Routes | mixed NL/EN: `/banen`, `/carriere`, `/profiel`, `/hoe-werkt-lobsy`, `/werven` vs `/candidate/*`, `/employer/*`, `/branch/*`; `/branch/*` pages are for EnterpriseManager too | Do not rename (links, SEO, flyers). Document a routes table (P15). |
| 4.4 | CSS | BEM-ish (`block__elem--mod`) is mostly followed; `is-*` state classes; 163 `!important`, 102 raw `z-index` numbers, 154 distinct hex colours, 227 font-weights outside the design system (650–900) against `design-system.mdc` | design-token cleanup = separate design pass (not a code-cleanup prompt) |
| 4.5 | DB | PascalCase tables/columns (EF default), 39 explicit `ToTable`; consistent | keep |

### 5. Peer review: correctness, security, privacy, tests

| # | Sev | Finding | Evidence | → |
|---|---|---|---|---|
| 5.1 | high | Roles and privacy (see §0) | – | P03–P06 |
| 5.2 | medium | **`eval` via JS interop + CSP `'unsafe-eval'`** | `Candidate/Applications.razor:321, 447, 459` call `Js.InvokeVoidAsync("eval", …)` with interpolated ids for menu focus. The CSP keeps `'unsafe-eval'` (`JobsyContentSecurityPolicy.cs:30`); its doc comment says "Blazor Server still needs 'unsafe-eval'", which is not true for Blazor Server (only WASM needs `wasm-unsafe-eval`). Tests pin it: `ContentSecurityPolicyTests.cs:18`, `ZapFindingsTests.cs:63`. | P07 (use `ElementReference.FocusAsync`; drop `unsafe-eval` only after a CSP-violation smoke on all main pages, verifying MapLibre and html2canvas) |
| 5.3 | medium | **N+1 in talent pool** | `TalentPoolService.ListForEmployerAsync` (`:418-437`): up to 100 ids, then per id `FirstAsync` + `ToDtoAsync` (2–3 more queries), about 300–400 queries per call | P07 |
| 5.4 | medium | Unpaginated admin/employer lists | `GET /api/admin/users` (`AdminController.cs:214`), `GET /api/applications` (`ApplicationsController.cs:67`, no `Take`) | P05 |
| 5.5 | medium | `async void` without catch | `VacancyDetail.razor:1133`, `BottomNav.razor:62` (+7 guarded ones) | P07 (after the VacancyDetail and bottom-nav PRs) |
| 5.6 | medium | Cancellation in UI | 457 `await Api.*` calls in `.razor`, 14 pass a token; 9 of 208 components own a `CancellationTokenSource`. Navigating away keeps requests running on the circuit. | backlog (pattern via `CultureAwareComponentBase`/`OwningComponentBase`) |
| 5.7 | medium | Vulnerable package | AngleSharp 1.3.0 NU1902 | P02 |
| 5.8 | low | HTML sanitiser is regex-based | `Core/Rules/HtmlSanitize.cs` (admin-edited about/legal pages rendered via `MarkupString` in `Legal/WieZijnWij.razor:43`, `Admin/AboutPageAdmin.razor:98`). Admin-only input limits the risk. | backlog: use AngleSharp (already referenced) or `HtmlSanitizer` |
| 5.9 | low | `MarkupString` usage is otherwise safe | 59 uses: static SVG icons, JSON-LD/boot JSON with `<` → `\u003c` escaping (`StructuredData.cs:291`, `VacancyDiscovery.razor:2339`), encoded SVG text (`ScoreRadarSvg.cs:21-29`), encoded help links (`HowLobsyGuidePanel.razor:71-85`) | none |
| 5.10 | low | Analyzer bugs | CA2016 `WebPushNotificationService.cs:110` (token not forwarded), CA1001 `VacancyDiscoveryIndex` (`SemaphoreSlim` not disposed; singleton, harmless), CA2208 ×4 `FlexCommercialService.cs:49-64` (wrong `paramName`), CA5350 `TotpAuthenticator` HMAC-SHA1 (**required by RFC 6238, suppress with a comment**) | P08 |
| 5.11 | low | Hand-maintained `?v=` asset versions | covered by PERF-6 and the pending asset-versions guard PR. Tests hard-code 68 `?v=` strings (`RegisterWizardUiTests.cs:58-65` …), so every bump means editing tests. | after the guard PR: replace with the guard (P09 note) |
| 5.12 | medium | **Test quality** | 412 source-text assertions pin implementation details (they break on refactors and pass on dead code, see 1.2/1.5); only 1 bUnit file; Web line coverage 24%; test files are named by sprint; the Playwright filter list is repeated in 3 workflow steps | Prefer bUnit/behaviour tests for new work; add a `Jobsy.Tests/README` on test types; foldering later |
| 5.13 | low | Privacy positives | `PrivacyDataService` (export/delete), `ParentalConsentController`, retention jobs (`DraftVacancyCleanupHostedService`), redacted email in logs (`WebPushNotificationService.cs:80`), Sentry no PII | keep |
| 5.14 | medium | IBAN in plain text | see 0.5 | P05 |

### 6. What is missing to make it "strak" (small-team fit)

| Item | Status | Proposal |
|---|---|---|
| `.editorconfig` with naming and style rules | missing | P01 (matches current style; no mass reformat) |
| `Directory.Build.props` (shared `Nullable`, `ImplicitUsings`, `AnalysisLevel`, `EnforceCodeStyleInBuild`) | missing | P01 |
| Analyzer gate / warning budget in CI | missing | P01 (fail only if the warning count rises; `TreatWarningsAsErrors` later, per project, starting with Core) |
| `dotnet format` check | missing | P01: `dotnet format whitespace --verify-no-changes` on **changed files only** |
| Central package management (`Directory.Packages.props`) | missing; EF/ASP.NET pinned at 9.0.0 in 4 csproj | P02 |
| Vulnerability gate (`dotnet list package --vulnerable`) | missing | P01 |
| Secret scan (gitleaks) | missing | P01 |
| Dependabot | configured on `acceptatie` only | active after the next release to `main` (nothing to do) |
| README / ARCHITECTURE / CONTRIBUTING / ONBOARDING / ADRs / roles matrix | README stale ("Jobsy", "Leaflet-kaart", "self-hosted OSRM", 04-09); ARCHITECTURE.md 18 lines; no CONTRIBUTING, ONBOARDING or ADRs | P15 |
| Error monitoring | Sentry wired (DSN via env); verify it is set on Render prod/acc | verify |
| Asset versioning | hand `?v`; guard PR pending | pending PR (PERF-6) |
| Minified asset build | hand-synced `app.min.css`, `jobMap.min.js`, `jobsyMapLibre.min.js`, `vacancyDetailMap.min.js` | P10 (CSS), backlog for JS |
| Test pyramid guidance | implicit | `Jobsy.Tests/README.md` in P15 |
| Pre-commit hooks | – | **not recommended** (Cursor agents commit; CI gates are enough) |

---

## Top 10 (by risk × value)

1. **[high] RegionalManager can spend tokens and unlock candidate PII, save the culture scan and start onboarding payments** (`TalentPoolController.cs:72/99`, `CompanyCultureController.cs:45`, `SupplierOnboardingController.cs:28/57`) → P03.
2. **[high] Admin sees all personal data by default, unpaginated, with no access audit log; IBANs in plain text** (`AdminController.cs:214`, `ApplicationsController.cs:67`, `JobsyDbContext.cs:1402`) → P05/P06.
3. **[high] No role × scope test net**. Correct today, but one forgotten check is enough → P04.
4. **[high] 8 stale JS source files that tests read instead of the shipped bundles, 2 already drifted** (`cookieConsent.js`, `extras-loader.js`) → P09.
5. **[medium] `eval` interop + CSP `'unsafe-eval'`** (`Applications.razor:321/447/459`, `JobsyContentSecurityPolicy.cs:30`) → P07.
6. **[medium] No guardrails**: no `.editorconfig`, analyzers, format, vulnerability or secret checks. AngleSharp is vulnerable → P01/P02.
7. **[medium] OpenAI settings code copied 11×** (~450 lines) → P11.
8. **[medium] ~1,590 lines of dead CSS in a hand-synced 21.7k-line `app.css` + `app.min.css`** → P10.
9. **[medium] God files**: `JobsyApiClient.cs` 5,212 lines, `VacancyDiscovery.razor` 3,466 → P13 (+ PERF-1).
10. **[medium] Localisation**: 683 of 2,109 strings in pl/ro/ar are untranslated Dutch copies; 238 keys possibly unused → P14.

---

## Not verified / limits

- **Runtime role tests:** I read the code and ran the unit tests; I did not log in as each role on Acceptatie (employer and admin accounts require MFA, see PERF caveats). P04 adds those tests.
- **Dynamic usage:**
  - The dead-CSS, dead-key and dead-member scans are token based. Dynamic strings (for example `$"{prefix}-{x}"`) can hide usage. Everything marked *likely/verify* must be grepped once more before deletion; the prompts require a Playwright smoke.
  - Feature flags are DB-driven, so "always on/off" needs the production admin screen.
- **Operations:**
  - `image-cache-sw.js` may still be registered on old devices (verify before deleting).
  - Whether Sentry DSN, vulnerability alerts (the GitHub API returned 404 = disabled or no access) and Render disk encryption are enabled was not verified.
- **Coverage** excludes Playwright and Postgres-only paths.

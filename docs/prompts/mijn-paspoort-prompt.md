# Cursor prompt: Mijn Paspoort (new candidate profile) + "Werkgevers actief" switch

This spec has **five PRs**. Each section below is one PR into `acceptatie`:

| # | Section | PR (branch) | Depends on |
|---|---|---|---|
| 01 | Phase 0 · Werkgevers actief (employers on/off switch) | `cursor/werkgevers-actief` | §F (builds it if not there yet) |
| 02 | Phase 1 · Flag, nav, route + redirects, top overview, tab shell, tab **Mijn DNA** | `cursor/mijn-paspoort-1` | §F (builds it if not there yet) |
| 03 | Phase 2 · Tab **Mijn tests** (+ course data model, "Groei verder") | `cursor/mijn-paspoort-2` | 02 merged |
| 04 | Phase 3 · Tabs **Past deze baan?** + **Carrière** (+ course blocks) | `cursor/mijn-paspoort-3` | 03 merged |
| 05 | Phase 4 · Tabs **Bewijzen** + **Mijn gegevens** (all forms, consent, delete account) | `cursor/mijn-paspoort-4` | 04 merged |

01 is independent of 02–05. Whichever of 01 and 02 starts first builds the shared foundation §F. The other one rebases onto it and only adds its own flag. Items numbered 06 and up come later (for example the "De ontdekkingsreis" nav item). They are **not** part of this spec, but §N reserves their place.

Only read the sections your pointer prompt names: §0 always, §F always, §N whenever you touch the nav, and then your own section.

---

## 0. Hard rules (every PR)
- **Branch:** `git fetch origin && git checkout -b <branch from the table> origin/acceptatie`.
  - Phases 2–4: branch from the latest `origin/acceptatie` after the previous phase has merged. If it hasn't merged yet, stop and say so.
- **ONE PR into `acceptatie`** per section. Do **not** merge, do **not** deploy, do **not** use rule `123` (`.cursor/rules/shortcut-123.mdc`), and **never** push to `main` or `acceptatie`. See `docs/release-flow.md`.
- Code references are from `origin/acceptatie` @ `20dc8d5b` (2026-09-29 08:56 CEST). Re-check line numbers before editing.
- **Mockups:** on branch `docs/mijn-paspoort`, folder `docs/mockups/mijn-paspoort/`. Read them with `git show origin/docs/mijn-paspoort:docs/mockups/mijn-paspoort/<file>`.
  - Desktop 1440×900, one screen per tab:
    - `pp-d1-mijn-dna.png`
    - `pp-d2-mijn-tests.png`
    - `pp-d3-past-deze-baan.png`
    - `pp-d4-carriere.png`
    - `pp-d5-bewijzen.png`
    - `pp-d6-mijn-gegevens.png`
  - Mobile 390×844:
    - `pp-m1-overzicht.png`
    - `pp-m2-mijn-tests.png`
    - `pp-m3-past-deze-baan.png`
    - `pp-m4-mijn-gegevens.png`
  - The mockups are a **layout and copy reference**. All names, numbers, employers, vacancies, courses and providers in them are **Voorbeelddata**. Production shows the candidate's real data or an honest empty state. It **never** shows fake providers, fake vacancies or fake stamps.
  - The "Voorbeelddata" labels are mockup-only. The **one exception** is the locked report cards in Mijn tests, which follow the testresultaten spec (fake client-side placeholders with a stamp).
- **Design system.** Follow `.cursor/rules/design-system.mdc` plus these rules, which win when they conflict:
  - **Colours:** tokens only, no new hex values, no inline `style=""` in `.razor`. The mockup HTML uses inline styles and a few gradients; rebuild everything with classes.
    - **No gradients** except the gold exception (gold buttons and gold bars). Where the mockup shows a gradient (depth strip, shell layers), use **stepped fills from existing tokens**, for example `--accent-soft` → `--accent` → `--brand`.
    - `--coral` is used at most once per screen (the small shell icon next to the passport tagline).
    - The Waarden colour is `--warn`, not coral.
  - **Type:** weights 400/600 only (700 only for the page `h1`), and only the type scale. Card padding and gaps use the `--space-*` scale.
    - If `--space-*` / `--text-*` / `--radius-pill` / `--z-*` are missing from `:root` in `app.css`, the **first** PR that needs them adds them exactly as listed in the design system. That PR syncs `app.min.css` and bumps its `?v=`.
  - **Layout:**
    - Mobile first.
    - Breakpoints 640/900/1024 only.
    - Tap targets ≥ 44 px.
    - Logical properties for RTL (`ar`); chevrons flip under `[dir="rtl"]`.
    - `prefers-reduced-motion` fallbacks.
  - **Calm UI:**
    - One primary action per card.
    - Max 2 badges per card.
    - No decorative emoji.
  - **Icons:** only in the tab bar, the stat cards, the course "waarom" line and primary actions. Never in `h2`/`h3`.
    - New line icons (antenna, claw, stone, wave, shell) go into the existing icon helper (`Navigation/NavIcons.cs` or the equivalent SVG helper). Don't add a new icon system.
  - **Mascot:** reuse `wwwroot/images/brand/mascot-{64,128,256}.{webp,png}`.
  - **Reuse existing components:**
    - `detail-card`, `status-pill`, `kompas-chip`, `btn-compact` / `btn-compact--primary`
    - `lobsy-dialog` / `LobsyFriendlyDialog`, `ScoreRadarChart`, the charts in `Components/Shared/Charts`
    - `PanelErrorBoundary`, `PageContentSkeleton`
- **CSS:**
  - New styles go in `wwwroot/css/features/mijn-paspoort.css` (01 uses `features/werkgevers.css` only if it needs any).
  - Link each sheet in `Components/App.razor`, both in the normal list and in `<noscript>`, with its own `?v=YYYYMMDD-<slug>`.
  - Add it to `Jobsy.Tests/asset-versions.json` (`AssetVersionGuardTests`).
  - BEM block `passport-…`.
  - Don't append to `app.css`.
- **Strings:**
  - All new UI text goes through `@Culture["…"]` in **nl/en/pl/ro/ar**, in a new `Localization/UiStringsPassport.cs` (and `UiStringsFeatureFlags.cs` for 01). Follow the `UiStringsMatch.MergeAll` pattern and register it in `UiStrings.cs`.
  - The Dutch copy in this spec is final (warm B1, "je/jij", never recruiter language). Translate it naturally; don't translate word for word.
  - `LocalizationParityReportTests` and `LocalizationTests` must stay green.
  - Hardcoded Dutch that you move (for example the consent texts in `Profile.razor` ~L585–630) gets localized on the way.
- **No duplicate data or logic.** The paspoort **reuses** the existing APIs, services and components. Where a piece of logic sits inside a component's `@code`, **extract** it into a small pure C# builder or a child component so both the classic profile and the paspoort use the same code. Then change the old component to use the extracted piece. **No new endpoints for data that an existing endpoint already returns.**
- **Keep the classic profile working.** With the paspoort flag OFF, every existing test (Playwright included) must pass unchanged.
- **Docs and guards to update when routes change:**
  - `docs/ROUTES.md` (`RoutesDocFreshnessTests`)
  - `Seo/PageSeoCatalog.cs` (private entries; `PageSeoTests.Catalog_resolves_every_razor_page_route`)
  - `Help/PageHelpDocs.cs`
  - `BlazorPageRoleAttributesTests`: new pages carry `[Authorize(Roles = "Candidate")]`
  - `CHANGELOG.md`
- **Migrations:** EF migration via `dotnet ef migrations add …` in `Jobsy.Infrastructure`, and update the snapshot. `EfModelSnapshotTests`, `PendingModelChangesTests` and `EfMigrationDiscoveryTests` must stay green.
- **Must NOT touch:**
  - `features/questionnaire.css`, `QuestionnaireShell.razor`, the banenkaart CSS/JS, `app-core.js`
  - the cookie banner
  - Mollie/checkout flows (`DeepAnalysisCheckout*`, `MolliePaymentService`) and prices
  - the testresultaten done page (`TestDetail.razor`), except for reusing its extracted parts as described in 03
- **PR description:**
  - What changed and why.
  - Screenshots desktop 1440 and mobile 390 of each new screen (flag ON), plus one of the unchanged classic profile (flag OFF).
  - The test list.
  - An "Out of scope / deferred" list.
- **Run** `dotnet build` and `dotnet test` (unit + bUnit). Run the Playwright suites you touched locally if you can, and mention it if you couldn't.

---

## F. Shared foundation: feature flags (built by the first of 01/02, reused by the other)
Goal: **one** small mechanism, no scattered `if` checks.

### F.1 Storage: reuse the existing admin settings
Admin toggles live in the singleton row `Jobsy.Core/Entities/PlatformFeatureSettings.cs`. Follow the full chain of the most recent boolean, `SupportAccessNotifySubject` (migration `20260928055611_AddSupportAccessGrant`):
- **Entity `PlatformFeatureSettings`:**
  - add `bool EmployersEnabled { get; set; } = true;` (01)
  - add `bool CandidatePassportEnabled { get; set; }` (02, default false)
- **Migration:**
  - `EmployersEnabled` needs `defaultValue: true` in the migration, so the **existing row reads true**.
  - `CandidatePassportEnabled` has `defaultValue: false`.
  - Add a test that a DB migrated from the previous snapshot has `EmployersEnabled == true`.
- **`Core/Interfaces/IPlatformFeatureService.cs`:**
  - `PlatformFeatureSnapshot` gets new trailing parameters with defaults (`EmployersEnabled = true`, `CandidatePassportEnabled = false`).
  - `PlatformFeatureUpdate` gets new trailing nullable `bool?` parameters (null = keep the current value, same as `SupportAccessNotifySubject`).
- **`Infrastructure/Services/PlatformFeatureService.cs`:** map both fields in `GetAsync` and `UpdateAsync`.
- **API DTOs:** `Api/Models/Sprint6Dtos.cs` (`UpdatePlatformFeatureRequest`, `PlatformFeatureDto`) and `SettingsController.UpdatePlatformFeatures` / `ToFeatureDto`.
- **Web:**
  - `Services/ApiClient/Models/Admin.cs` (`PlatformFeatureItem`)
  - `Components/Pages/Admin/SettingsAdmin.razor`: add a checkbox next to the support-access checkboxes (~L102–109), with a one-line muted help text under each:
    - "Werkgevers actief" (`Admin.EmployersEnabled`).
      - Help: "Uit = alleen zelfontdekking. Banenkaart, vacatures, sollicitaties en werkgeversportalen zijn dan verborgen en geblokkeerd. Er wordt niets verwijderd."
    - "Mijn Paspoort (nieuw profiel)" (`Admin.CandidatePassportEnabled`).
      - Help: "Aan = kandidaten zien ‘Mijn Paspoort’ in plaats van ‘Profiel’. Uit = alles zoals nu."
  - After a successful save, call `IFeatureFlags.Invalidate()` (F.2) on the Web side.

### F.2 One service: `IFeatureFlags`
- **Core:**
  - `Jobsy.Core/Features/PlatformFeature.cs`: `enum PlatformFeature { Employers, CandidatePassport }`.
  - `IFeatureFlags` with:
    - `ValueTask<FeatureFlagSnapshot> GetAsync(CancellationToken)`
    - `ValueTask<bool> IsEnabledAsync(PlatformFeature, CancellationToken)`
    - `void Invalidate()`
  - `sealed record FeatureFlagSnapshot(bool EmployersEnabled, bool CandidatePassportEnabled)` with `bool IsEnabled(PlatformFeature)`.
- **API (Infrastructure):** `FeatureFlags : IFeatureFlags` reads `IPlatformFeatureService`.
  - It caches in `IMemoryCache` for 30 s.
  - `PlatformFeatureService.UpdateAsync` invalidates the cache.
  - If the DB is unavailable, it falls back to the **defaults** (Employers on, Passport off) and logs a warning.
- **API endpoint:** `GET api/settings/feature-flags`, `[AllowAnonymous]` like `free-publish` / `session-security`.
  - Returns `{ employersEnabled, candidatePassportEnabled }`.
  - These are not secrets. Public pages (`/`, `/register`) need them before login.
- **Web:**
  - `Jobsy.Web/Services/WebFeatureFlags : IFeatureFlags`, a singleton that calls the endpoint.
  - 30 s `IMemoryCache`; `Invalidate()` clears it.
  - On error it returns the last known snapshot, else the defaults.
  - Register it in `Program.cs`.
- Everything else asks **only** `IFeatureFlags`. No component calls the settings API directly.

### F.3 One attribute, three gates
- **`Jobsy.Core/Features/RequiresFeatureAttribute`** (`[AttributeUsage(Class | Method, AllowMultiple = true)]`):
  - `RequiresFeature(PlatformFeature feature, bool whenEnabled = true)`
  - optional `FallbackPath`
- **API gate:** a global MVC filter `FeatureGateFilter` (`IAsyncActionFilter`, registered in `Api/Program.cs`).
  - It reads the attribute on the controller **and** the action.
  - When the requirement isn't met it returns **404** `ProblemDetails` with `type = "feature_disabled"`, so a paused feature looks like it doesn't exist.
  - Admin-only controllers are never gated. Admin sees everything.
- **Web page gate:** a `FeatureRouteGate` component in `Components/Routes.razor` around `AuthorizeRouteView`.
  - It reads `RequiresFeatureAttribute` from `routeData.PageType`.
  - When the requirement isn't met it calls `Navigation.NavigateTo(fallback, replace: true)`. During prerender this becomes a server redirect.
  - The fallback comes from the attribute; the default is `FeatureRoutes.HomeFor(user, flags)` (§01.3).
  - The same check runs for the first request, prerender and interactive navigation, so there's no flash of the blocked page.
- **Web minimal APIs** (`Hosting/VacancyMapProxyEndpoints.cs`, `/sitemap.xml`, `/robots.txt`, etc.): an endpoint filter `.RequireFeature(PlatformFeature.Employers)` that returns 404.
- **Tests (in whichever PR builds §F):**
  - `FeatureGateFilter` returns 404 for a gated controller and 200 when the flag is on.
  - The Web gate redirects a gated page.
  - A reflection test lists every page and controller that carries `RequiresFeature`. The expected list lives in the test, so nothing gets gated or un-gated by accident.

---

## N. Candidate nav: order and slots (applies to 01 and 02)
Dennis's order for the candidate nav, from left to right. There is one `BottomNav`; on desktop it is the main nav too, so this covers both:

| Slot | Item | Href | Visible when |
|---|---|---|---|
| 1 | **De ontdekkingsreis** | added in **06+**, not in this spec | **reserved:** no item yet, keep the slot in code |
| 2 | **Mijn Paspoort** (`Nav.Passport`) *or* **Profiel** (`Nav.Profile`) | `/candidate/paspoort` or `/candidate/profile` | always (Paspoort when the passport flag is ON) |
| 3 | Zoeken (`Nav.Search`) | `/` | Werkgevers actief ON |
| 4 | Bewaard (`Nav.Saved`) | `/candidate/liked` | Werkgevers actief ON |
| 5 | Sollicitaties (`Nav.Applications`) | `/candidate/applications` | Werkgevers actief ON |
| 6 | Carrière (`Nav.CareerPath`) | `/carriere` | always, **always last** |

- **Implementation:**
  - `RoleNavCatalog.Candidate` becomes a **pure function** `RoleNavCatalog.CandidateItems(FeatureFlagSnapshot flags)`.
  - It uses an ordered slot list with a named reserved slot `CandidateNavSlot.Discovery` (no item until 06+), so 06 only has to add one entry.
  - `ForUser(user, flags)` passes the flags through. `BottomNav.razor` gets them from `IFeatureFlags`: refresh on init and on `NavRefreshRequested`, **not** on every location change.
  - Active-state aliases stay: Paspoort also matches `/candidate/profile`, `/profiel` and `/home`.
- **Passport OFF keeps today's order exactly** (Zoeken, Bewaard, Sollicitaties, Carrière, Profiel). That is Dennis's "OFF = alles zoals nu" rule; the Werkgevers switch still hides the employer items.
  - The new order starts when the passport flag is ON: Mijn Paspoort, Zoeken, Bewaard, Sollicitaties, Carrière.
  - Open decision (O1): should the new order also apply with the passport flag OFF?
- **Heads-up for 06+:** with Ontdekkingsreis added and employers ON, the candidate nav has 6 items. The design system allows max 5 in the bottom nav. 06 must solve that (for example Bewaard inside Zoeken). Don't solve it here.
- **Tests:** a unit test of `CandidateItems` for all 4 flag combinations (exact order and hrefs), plus a bUnit test that `BottomNav` renders that order.

---

## 01 · Phase 0: "Werkgevers actief" (employers on/off)
**Goal.** One admin switch.
- **ON (default) = exactly as now.** Snapshot tests and all existing tests are unchanged.
- **OFF = a pure self-discovery platform.** Everything that needs employers or vacancies is hidden in the UI **and** blocked on the server.
- **Existing data stays untouched:** no deletes, no status changes, no emails about it.
- Switching back ON restores everything as it was.

### 01.1 Flag
`EmployersEnabled` (default **true**) plus the admin checkbox, via §F. Gate with `[RequiresFeature(PlatformFeature.Employers)]`.

### 01.2 Inventory: what is employer- or vacancy-dependent, and what happens when OFF
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
- **Employer-acquisition portals** (default: gate them, see open decision O2):
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
  - `ExternalVacanciesController`: API-key vacancy push/import (O3)
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
- **Employer acquisition:** `SalesManagersController`, `SalesCommercialController`, `AmbassadeursController`, `PartnerAffiliateController` (O2).
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

### 01.3 Landing, login and employer users when OFF
- **Home path.** `FeatureRoutes.HomeFor(user, flags)`, a pure function, is used by `FeatureRouteGate`, `AuthRedirects` and the logo link:
  - anonymous → `/ontdek`
  - candidate → `/candidate/paspoort` if the passport flag is ON, else `/candidate/profile`
  - admin → `/home`
  - employer-side roles → `/access-denied?reason=employers-off`
- **`AuthRedirects`:** `BanenkaartPath` usages (`CandidatePostLoginUrl`, `IsGenericPostLoginLanding`, `ResolveCandidateReturnUrl`) become flag-aware. With OFF, a vacancy `returnUrl` falls back to the home path.
- **Login page:**
  - Hides "Registreer je bedrijf".
  - Email/password, Google, Microsoft and MFA stay, because candidates and admins need them.
  - A user whose roles are **only** employer-side (EnterpriseManager, BranchManager, RegionalManager, Intermediary, plus SalesManager/Ambassadeur per O2) can log in but lands on `/access-denied?reason=employers-off`. Text (new key): "Lobsy staat even alleen open voor kandidaten. Je gegevens blijven bewaard. We laten het je weten als werkgevers weer welkom zijn." Plus a logout button.
  - Users with Candidate or Admin roles are unaffected.
- **Admin:** unchanged, sees everything. A calm `status-pill` in `SettingsAdmin` says "Werkgevers staan uit".

### 01.4 Tests (01)
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

---

## 02 · Phase 1: flag, nav, route, top overview, tab shell, tab "Mijn DNA"
Mockups: `pp-d1-mijn-dna.png`, `pp-m1-overzicht.png` (and the top part of every other mockup).

### 02.1 Flag and routes
- `CandidatePassportEnabled` (default **false**) plus the admin checkbox (§F).
- **New page** `Components/Pages/Candidate/Passport.razor`:
  - `@page "/candidate/paspoort"`, `[Authorize(Roles = "Candidate")]`
  - `@rendermode InteractiveServer` with prerender, like `Profile.razor`
  - `[RequiresFeature(CandidatePassport, FallbackPath = "/candidate/profile")]`
- **Tabs** via `?tab=`, in a new `Navigation/PassportTabs.cs` modelled on `CandidateKompasTabs` (constants + `Normalize` + `Neighbor`):
  - `dna` (default), `tests`, `fit`, `career`, `proof`, `data`
  - Legacy values map too: `profile`/`profiel` → `data`, `wie-ben-ik` → `dna`, `functiefit` → `fit`, `carriere` → `career`, `bewijzen` → `proof`, `gegevens` → `data`.
- **Flag OFF:** `/candidate/paspoort` redirects to `/candidate/profile`, keeping `?tab=` mapped back to the Kompas tabs, so the paspoort isn't reachable. Everything else is unchanged.
- **Flag ON:**
  - `/candidate/profile` (`Profile.razor`) redirects to `/candidate/paspoort` with the tab mapped (`dna`→`dna`, `profile`→`data`, `tests`→`tests`, `fit`→`fit`) and `returnUrl` preserved. Do it at the top of `OnInitializedAsync`, before loading data.
  - `/profiel` already forwards to `/candidate/profile` and so follows.
  - Open decision O4 (default: yes): `/home` for candidates also redirects to the paspoort, so the old Kompas isn't a second profile.
- **Transitional rule (removed in 05).** Until Phase 4 lands, `tab=profile` on `/candidate/profile` is **not** redirected. The paspoort tabs Bewijzen and Mijn gegevens then show one line plus a link "Open je gegevens" → `/candidate/profile?tab=profile`. Keep this in one constant, `PassportRedirects.ClassicTabsUntilPhase4`, so 05 can delete it. Candidates can then always reach their forms.
- **Nav** per §N: `Nav.Passport` = "Mijn Paspoort" (reuse `NavIcons.Profile`).
- **Docs:** `ROUTES.md`, `PageSeoCatalog` (`Private("Passport.Title", …)`), `PageHelpDocs` entry for `/candidate/paspoort`.

### 02.2 Page structure (all tabs share it)
- **Desktop ≥1024:** 2 columns.
  - Left: the sticky **passport card** (≈330 px).
  - Right: the **overview row**, then the **tab bar**, then the **tab panel**.
- **Mobile:** compact passport card, then the overview card (ring + 2×2 stats), then the sticky tab bar (under the header, horizontal scroll, active tab scrolled into view, edge fade), then the panel.
- **Above the fold:** at 1440×900 the tab bar sits above the fold. The mockup puts it at ~y 322; keep it ≤ 360. On 390×844 the tab bar starts at ≤ ~400 px.
- **Each tab fits roughly one screen.** The rest goes behind "Meer" or links to the full page.
- `h1` = "Mijn Lobsy-paspoort" (visually hidden is fine; the passport card name is `h2`). One mascot bubble per tab (02.6).
- **Tab bar:** reuse the ARIA tab pattern and keyboard handling from `CandidateKompas.razor` (L56–100, `OnTabListKeyDown`).
  - Lazy-load each panel on first activation (like `Active` in `DnaPanel`).
  - Update the URL with `replace`.
  - Icons + text per tab (§0 icon rule).
  - Tab labels: "Mijn DNA", "Mijn tests" (count pill: tests with a next step), "Past deze baan?", "Carrière", "Bewijzen", "Mijn gegevens".

### 02.3 Passport card (identity column): reuse the existing profile data
Data comes from the same sources as `Profile.razor` / `CandidateKompas` (`MeProfile`, `CandidateKompasState`, `CandidateDnaSummary`). No new endpoint.
- **Banner:** "LOBSY PASPOORT" and a short member number (a stable non-PII hash of the user id, e.g. `LB-` + 5 chars; don't show the database id), plus a shell-shaped stamp "GESTART {maand ’jj}" (the account creation month).
- **Avatar:** **initials** in a `--brand` circle. Photo upload does not exist, so it's **deferred**: no edit badge, no fake photo.
- **Name and pills** (max 2 + 1):
  - "Beschikbaar voor werk" when `OpenForWork`, only if Werkgevers actief is ON.
  - Up to 2 DNA keywords (`WhoAmI.Keywords`).
- **Shell layers** instead of the % bar:
  - 5 segments filled `floor(ProfileCompletenessPercent / 20)` in stepped tokens.
  - Text: "{n} van 5 lagen · aanvullen", which opens the tab `data` (in Phase 1 the transitional link).
  - `role="progressbar"` with `aria-valuenow` = the percent. Reuse the same percent as `CandidateKompas.EffectiveCompletenessPercent`: extract that getter into the shared builder (02.5).
- **"Mijn verhaal":** the candidate's own "Over jezelf" text (`Profile.AboutMe`), clamped to 3 lines. If empty: "Schrijf in een paar zinnen wie je bent." plus a link.
- **Facts 2×2:** Ik woon in (city from the address), Reizen (max travel + transport), Uren per week, Beschikbaar (availability presets).
  - Then "Wat ik zoek" (interests chips, max 2 + "+n") and Rijbewijs (licenses).
  - An empty fact shows "—" with an "aanvullen" link. Never invent values.
- **Actions:**
  - "Lobsy-CV" (the existing Lobsy-CV download, `Profile.DownloadLobsyCv`)
  - "Aanpassen" (opens `data`)
  - **"Deel mijn paspoort" is deferred.** Sharing needs a public link and a privacy review, so don't render it.
- **Tagline** at the bottom (desktop only): shell icon (`--coral`, the only coral on the screen) + "Lobsy: ontdek wie je bent onder de schaal".
- **Mobile card:** avatar, name, one line "{stad} · {vervoer} {min} min · {uren} u", 2 pills, shell stamp, shell layers. No facts grid; the facts live in the `data` tab.
- **Spoken languages don't exist** in the model, so they're **deferred**. Don't show a languages row.

### 02.4 Overview: "Dit ben jij" (always on top)
- **DNA ring** (new `Components/Candidate/Passport/DnaRing.razor`, pure SVG, `aria-label`):
  - 4 arcs: Competenties `--brand`, Beroepen `--success`, Cultuur `--gold`, Waarden `--warn`.
  - Each arc is filled by that test's progress: completed = full, else answered / question count, from the same `DnaCard` data that `DnaPanel` uses.
  - Centre: the mascot (`mascot-128.webp`) inside a dashed `--gold-light` circle, meaning the next shell.
  - Text next to it (desktop) or under it (mobile): "Dit ben jij" / "Jouw kreeft · **fase {k} van 4**", where k = the number of completed tests (0–4), plus the legend.
  - At k = 0 the text reads "Jouw kreeft · nog in het ei" and the arcs are ghosts.
- **4 stat cards** (desktop row; mobile 2×2 inside the ring card). They reuse the existing DNA highlights (`Dna.HighlightStrongest` / `HighlightWork` / `HighlightImportant` from `DnaPanel` ~L481–491) plus the culture home label:

  | Card | Label | Value source | Status line |
  |---|---|---|---|
  | 1 (`--brand`, claw icon) | "Jouw sterkste klauw" | strongest competence | "Uitgebreid" / "Quick-Scan" / "Voorlopig" |
  | 2 (`--success`) | "Werk dat bij je past" | top RIASEC label | RIASEC code |
  | 3 (`--gold`) | "Hier voel je je thuis" | culture top pole | |
  | 4 (`--warn`) | "Dit vind je belangrijk" | top value | |

  - Status is text, not colour alone.
  - When a test isn't done, the card shows "Nog niet ontdekt" plus a quiet link "Doe de test" (to the tab `tests`).

### 02.5 Extraction (no duplicate logic)
- **Move the card/highlight/slide/detail building out of `DnaPanel.razor @code`** (~L290–905: `BuildCompetence`/`BuildCareer`/`BuildCulture`/`BuildValues`, `Paint`, `BuildSlide`, `BuildCultureDetail`, `BuildValuesDetail`, the highlights and the private records `DnaCard`/`DnaHighlight`/`DnaSlide`/`DetailBlock`) into `Jobsy.Web/Components/Candidate/Dna/CandidateDnaViewBuilder.cs`.
  - It's pure and testable, and takes `CandidateKompasState`/`CandidateDnaSummary` plus a string lookup.
  - `DnaPanel` then only renders. Its markup and classes stay identical, which existing tests like `KompasDnaLandingTests` and `MijnDnaLoadSpeedTests` check.
- The completeness getter moves into the builder too; `CandidateKompas` uses it from there.
- **Loading:** the paspoort page loads once, sharing the same state objects that `Profile.razor` passes to `CandidateKompas` (`SharedKompas`, `SharedDna`, `PersistentComponentState`), so there's no double fetch. Reuse the persist keys and pattern.

### 02.6 Mascot bubbles (one short line per tab; B1 NL is final)
One `LobsyBubble` component (mascot 40 px + `accent-soft` bubble; use the existing mascot bubble if one is already there). The bubble is the one notice per screen (§0).

| Tab | Key | NL |
|---|---|---|
| dna | `Passport.Bubble.Dna` | "Je hebt net een laag afgeworpen. Dit is wie eronder zat." (0 tests: "Hier werp je je oude schaal af. Begin met een test.") |
| tests | `Passport.Bubble.Tests` | "Niet zoeken aan de oppervlakte. Duik dieper." |
| fit | `Passport.Bubble.Fit` | "Je antennes wijzen deze kant op. Twijfel je? Typ een baan, ik kijk mee." |
| career | `Passport.Bubble.Career` | "Elke stap is een stukje nieuwe schaal dat aangroeit." |
| proof | `Passport.Bubble.Proof` | "Dit is je nieuwe, sterkere schaal. Alles wat je deed telt, ook mantelzorg." |
| data | `Passport.Bubble.Data` | "Jouw schaal, jouw regels. Jij bepaalt wie wat ziet." |

### 02.7 Tab "Mijn DNA"
3-card grid (mobile stacked), then the shells row.
- **"Wat maakt jou jou"** (eyebrow "Onder je schaal"): the 3 DNA highlights as rows (icon + bold line + one muted line) from `CandidateDnaViewBuilder`. With fewer than 3, show only what exists.
- **"Jouw verhaal":**
  - The WhoAmI story (`Dna.Story*`, same clamp/more as `DnaPanel`), keywords as `kompas-chip` (max 3).
  - Meta: "Door Lobsy geschreven uit je tests · {datum}".
  - Empty state: the existing `Dna.StoryEmpty` + CTA to `tests`.
- **"Waar voel jij je thuis":** 2 culture pole sliders (reuse `PoleSliders`) and "Waarden op werk · voorlopig|klaar", top 3 ranked (reuse the `BarList` data, shown as a numbered list). Ghost state plus a teaser when not done (existing `Dna.Teaser*`).
- **"Mijn schalen"** (the old "stempels").
  - Subtitle: "Hier werp je je oude schaal af · {n} van 6". Right side (desktop): "Groot worden doe je door je schaal af te werpen." plus "Nog {x} vragen tot je volgende schaal" (x = remaining questions of the nearest unfinished test).
  - Stamps do **not** exist yet, so they are **derived from existing data** (no storage, no dates) in `PassportShellRules.cs` (Core/Rules, unit-tested):

    | Shell | Earned when |
    |---|---|
    | Eerste test | ≥1 test completed |
    | Eerste rapport | ≥1 extended/deep analysis completed |
    | CV erbij | own CV uploaded **or** Lobsy-CV downloaded at least once, if that's tracked (else only own CV) |
    | 3 tests gedaan | ≥3 completed |
    | DNA compleet | 4 completed |
    | Eerste baan / sollicitatie | ≥1 application; **hidden** when Werkgevers actief is OFF, so the total becomes "van 5" |

  - Rendering: a shell outline per stamp (SVG, dashed stroke in the stamp's token), an icon, a 2-line label, and a slight rotation that is off under reduced motion. Unearned shells are dotted `--border` with muted text. Mobile wraps to 2 rows.
- **Tabs 2–6 in Phase 1:** render exactly what already exists, so nothing is lost:
  - `tests`: the existing `TestsOverviewPanel`
  - `fit`: the existing `RoleFitCheckPanel`
  - `career`: one card "Mijn loopbaanplan" with a link to `/carriere`
  - `proof` and `data`: the transitional link (02.1)
  - Each still gets its bubble. 03–05 replace these.

### 02.8 Strings (02)
- `Nav.Passport`
- `Passport.Title`, `Passport.Banner`, `Passport.MemberNo`, `Passport.StartedStamp`
- `Passport.Layers` ("{0} van 5 lagen"), `Passport.LayersAria`, `Passport.Fill` ("aanvullen")
- `Passport.MyStory`, `Passport.MyStoryEmpty`
- `Passport.Facts.*` (LivesIn, Travel, Hours, Available, LookingFor, License)
- `Passport.LobsyCv`, `Passport.Edit`, `Passport.Tagline`
- `Passport.Overview.Title` ("Dit ben jij"), `Passport.Overview.Stage` ("Jouw kreeft · fase {0} van 4"), `Passport.Overview.Egg`, `Passport.Legend.*`
- `Passport.Stat.*`, `Passport.Stat.NotYet`
- `Passport.Tab.*`, `Passport.Bubble.*`
- `Passport.Dna.Eyebrow`, `Passport.Dna.Home`, `Passport.Dna.ValuesProvisional`
- `Passport.Shells.Title`, `Passport.Shells.Sub`, `Passport.Shells.Tagline`, `Passport.Shells.Next`, `Passport.Shells.Item.*`
- `Passport.Transitional.OpenData`

### 02.9 Tests (02)
- **Flag and nav:**
  - flag default false; admin round-trip
  - `CandidateItems` order per §N (4 combinations)
  - bUnit `BottomNav`: "Mijn Paspoort" at slot 2 when ON, "Profiel" last when OFF
- **Redirects:**
  - OFF: `/candidate/paspoort?tab=data` → `/candidate/profile?tab=profile`
  - ON: `/candidate/profile?tab=tests` → `/candidate/paspoort?tab=tests`, `returnUrl` kept
  - ON: `/candidate/profile?tab=profile` is **not** redirected (transitional)
  - `/profiel` follows
  - O4 `/home`
- **Builder parity:** `CandidateDnaViewBuilder` gives the same highlights and cards for the fixtures that `DnaPanel` rendered before (golden test); `DnaPanel` markup is unchanged.
- **Rules:** `PassportShellRules` table test (incl. employers OFF → 5); `PassportTabs.Normalize` mappings.
- **bUnit:**
  - `DnaRing` (0/2/4 tests, labels, aria)
  - passport card: empty facts show "—", no fake photo, no share button, the "Beschikbaar" pill hidden when employers OFF
  - stat cards not-done state
  - tab keyboard navigation
- **Playwright** (`MijnPaspoortPlaywrightTests.cs`), flag ON:
  - 1440×900: the tab bar bottom is ≤ 360 px and the Mijn DNA panel is visible
  - 390×844: the tab bar is sticky under the header and scrolls horizontally
  - flag OFF: the nav equals today's

---

## 03 · Phase 2: tab "Mijn tests" + course data model + "Groei verder"
Mockups: `pp-d2-mijn-tests.png`, `pp-m2-mijn-tests.png`.

### 03.1 Test list with depth
- **Data:** extract the loading and mapping from `TestsOverviewPanel.razor` (L56–150: kompas result + 4 test states + 4 deep states) into `TestsOverviewBuilder` (pure), used by both `TestsOverviewPanel` and the new `PassportTestsTab`. No extra API calls: reuse the shared kompas state when present.
- **Card header strip:** "Oppervlakte → Diep" as 3 **stepped** segments (`--accent-soft`, `--accent`, `--brand`). No gradient.
- **Rows:** Competentie, Beroepen, Cultuur, Waarden.
  - Icon, name, one-line question ("Wat kun jij goed?" etc.).
  - Mini progress with 3 segments: Quick-Scan (light) · Uitgebreid (medium) · Rapport (dark). Done = filled; in progress = partly filled by answered / total; locked report = gold outline (`--gold-soft` / `--gold-light`).
  - **Quota line under the bars:**
    - Uses the testresultaten rules and strings `TestResult.Quota.Line` / `LineOne` / `Zero`, from `GetAssessmentAdjustmentsAsync` (`AssessmentAdjustmentState`), with 3 dots.
    - At 0 it uses the `--warn-soft` note style from testresultaten.
    - A not-finished Quick-Scan shows "Nog {x} van {y} vragen" instead.
  - **One action per row:**
    - completed extended → "Rapport" (the existing report/PDF link)
    - in progress → "Ga verder" (primary)
    - Quick-Scan done → "Uitgebreid" (secondary; it goes to the existing deep-analysis start, and price/checkout are untouched)
    - not started → "Start" (primary)
  - The selected row (the latest activity) is highlighted with `--accent-soft`.
- **"Wat kost uitgebreid?"** is a quiet link to the existing explanation. No new prices.

### 03.2 Locked report card (right column)
- Show the **testresultaten locked cards** for the test with the most relevant next step (Beroepen in the mockup). Extract the card grid + gold block from `TestDetail.razor` into a reusable `TestResultLockedPreview` if it isn't a component yet, and use it in both places.
- **All testresultaten honesty rules apply unchanged** (card gating §4.3, client-side placeholder data, the "Voorbeelddata" stamp per card, no server data, checkmarks only for cards that are shown, one gold CTA).
- If the candidate has already done every extended test, the right column shows "Download je rapporten" (the existing PDFs) instead.

### 03.3 Course data model (reused in 04)
Extend the **existing** training model (`Core/Entities/TrainingEntities.cs`, `TrainingUpskillService`, `TrainingUpskillController`, `Pages/Admin/TrainingAdmin.razor`, `TrainingOffersBlock.razor`). **Don't add a parallel model.**
- **`TrainingOffer`** gets:
  - `TrainingOfferType Type`: `Opleiding | Cursus | Workshop`
  - `int? DurationValue` + `TrainingDurationUnit? DurationUnit` (`Hours | Days | Weeks | Months | Years`), localized when rendered
  - `TrainingDeliveryMode Delivery`: `Online | OnSite | Blended` + `string? Location`
  - `bool IsFree`
  - `bool IsPartner`
  - `string? AffiliateCode`
  - `bool ShowInPassport` (curation, default **false**)
- **Migration defaults:** everything false/null for existing rows, so **nothing existing appears in the paspoort** until an admin curates it.
- **Admin:** `TrainingAdmin.razor` gets the fields. Validation: `IsPartner` requires an `AffiliateCode` and a valid course deep link (`TrainingDeepLinkRules.IsCourseDeepLink`); `IsFree` and `IsPartner` can't both be true.
- **Slot rule** `CourseSlotRules.Pick(offers, context)` (Core/Rules, pure, unit-tested):
  - Only `IsActive && Provider.IsActive && ShowInPassport` offers, matched with the existing `TrainingMatchRules` on keys/fields for the context.
  - **Slot 1 = the best free offer.** **Slot 2 = the best partner offer.** Max 2.
  - **No free match → the block is hidden** (candidate first, O5).
  - Paid non-partner offers never show in the paspoort block.
- **Links:**
  - Every click goes through the existing tracked outbound `TrackAsync`.
  - `AffiliateCode` is appended there. `TrainingTracking.AppendParameters` gets an optional code; add a test.
  - Partner links get `rel="sponsored noopener noreferrer" target="_blank"`. Free links get `rel="noopener"`.
- **Card component** `CourseSuggestionBlock.razor` (shared by 03 and 04), with an eyebrow + 2 option cards:
  - line 1: pill **"Gratis"** (`--success-soft`/`--success`, check icon) or **"Partnerlink"** (neutral outline) + title
  - line 2: "{Type} · {duur} · {online | plaats} · {aanbieder}"
  - line 3, the "waarom": the free option uses a claw icon + "Laat je klauw ‘{skill}’ groeien"; the partner option uses a check icon + a context reason
  - Block note (always visible when a partner option is shown): **"Partnerlink: Lobsy kan een vergoeding krijgen."**
- **Production data:** ship **no** seeded courses for this block. No fake providers in any environment. The example providers in the mockups (LeerPlein, ZorgStart, Voorbeeld-…) exist only in test fixtures. With no curated data the block simply doesn't render.

### 03.4 "Groei verder" (in Mijn tests)
- Under the row of the test the suggestion is based on. Default: Competentietest, when completed.
- The context is the **lowest competence** (from the existing scores) mapped to training keys through the existing `KeysCsv` matching. The `{skill}` label is the localized competence name.
- Eyebrow: "Groei verder". Max 2 options. Hidden when there is no curated free match.

### 03.5 Tests (03)
- **Builder:** `TestsOverviewBuilder` parity with the old panel.
- **Quota:** the line uses the `TestResult.Quota.*` strings with the correct counts (3/2/1/0 and the Zero state).
- **Action mapping:** the per-row action for each status.
- **Locked cards:** the testresultaten "no deep-report data for non-payers" DTO test still passes; the preview renders only gated cards.
- **Model and migration:** the new fields; existing rows stay `ShowInPassport = false`.
- **`CourseSlotRules`:**
  - free first; max 2
  - no free → empty
  - partner without affiliate code → excluded
  - paid non-partner → excluded
  - both flags true → invalid
- **Tracking:** the affiliate code is appended; `rel` attributes are correct.
- **bUnit:** `CourseSuggestionBlock` shows "Gratis" first, "Partnerlink" second and the disclosure note; nothing is rendered with an empty list.
- **Admin validation.**
- **Playwright:** the tab `tests` at 1440×900 fits the screen (panel bottom ≤ 900 with no curated courses, and with 2 fixture courses on the test DB); 390×844 row layout.

---

## 04 · Phase 3: tabs "Past deze baan?" + "Carrière"
Mockups: `pp-d3-past-deze-baan.png`, `pp-m3-past-deze-baan.png`, `pp-d4-carriere.png`.

### 04.1 "Past deze baan?"
- **Logic:** reuse `RoleFitCheckPanel` (state `GetMyRoleFitAsync`, check `EvaluateRoleFitAsync`, `RoleFitCheckResult`).
  - Extract the state handling into a small Web-side state class `RoleFitCheckSession` so both the classic panel and `PassportFitTab` use it.
  - Keep the unlock rules: Quick-Scans needed, `LockMessage`.
- **Left card "Past deze baan bij mij?":**
  - Subtitle with antenna icon: "Vind de omgeving waar jouw antennes tot rust komen."
  - Input + "Check" (primary), and "Hele uitslag ›" in the header, which goes to the existing full result view.
  - "Laatste check: {JobTitle}" + "Vergelijkbare functies ›" (`SimilarRoles`).
  - **4 results (2×2), no percentage as the hero:**
    1. "Wat je antennes zeggen": a band label from `MatchPercent`. Add `RoleFitBandRules` (≥75 "Past goed", ≥50 "Past redelijk", else "Past nog niet") if no band helper exists. The % is only in the full result.
    2. "Klauwen die je al hebt": `Strengths`, top 2.
    3. "Klauw die nog groeit": `Gaps`, first, in the `--warn` icon colour.
    4. "Wat je kunt doen": `ActionSteps`, first.
  - **Course block** "Laat je klauw groeien" (`CourseSuggestionBlock`, context = the first gap + the job title's `SearchKeys`).
- **Right column:**
  - **Culture card** (antenna icon). Show it **only** when `CultureFitLabel` exists: title "Cultuur: {CultureFitLabel}", subline "Hier komen jouw antennes tot rust", and a status pill with the existing band text (never invented). No culture scan → a quiet link "Doe de cultuurscan".
  - **"Vacatures die bij jou passen"**, subtitle with stone icon: "Niet de grootste steen, maar die bij jouw formaat past."
    - Top 3 from `CandidateKompasState.TopMatches` (the same data as `CompetencyMatchPanel`), each with a logo initial, title, company · travel time and a band pill, plus "Top 10 ›" (the existing matches view).
    - Footer row: "Werkgevers die je willen spreken" with the pill "{n} nieuw" (existing talent-contacts count) and "Bekijk" → `/candidate/talent-contacts`.
    - The whole card is hidden when **Werkgevers actief is OFF**, and the server also returns nothing (01.2d).

### 04.2 "Carrière"
- **Data:** reuse `CareerPathService` / `GetCareerPathAsync` and the `CareerDashboard.razor` mapping. Extract the plan → steps / gaps / courses mapping (`CareerDashboard.razor` ~L390–650) into `CareerPlanViewBuilder` for both views. No new endpoints.
- **Card "Mijn droombaan":**
  - Title + "Open mijn hele plan ›" → `/carriere`.
  - Header action: "Droombaan wijzigen" (secondary) → the existing dream-job flow on `/carriere`.
- **Stepper as growing shells:** the shells get bigger from "Nu" to "Doel".
  - done = solid `--brand` + check
  - current = `--accent-soft` fill + dashed `--brand`, label "Groeit nu"
  - future = dotted `--border`
  - goal = `--gold-soft` + `--gold`
  - Labels come from the plan steps. Completing and uncompleting steps stays on `/carriere`.
- **3 cards:**
  - "Wat je nog mist", subtitle with claw icon: "Welke klauwen je al hebt, en welke je nog laat groeien." Gaps have a claw icon in `--warn`; met items have a check + "heb je al".
  - "Opleiding die past": `CourseSuggestionBlock` in the same style as the mockup, with the context = the next step's course names/keys. If the plan already lists a course, reuse `CareerCourseMatcher` to mark courses that are already claimed.
  - "Match op deze stap": eyebrow "Groei eerst. Match daarna.", stone line "Deze steen past al bij jouw formaat.", a text with the band for the next step's occupation, and "Vacatures voor deze stap ›".
    - When **Werkgevers actief is OFF**, this card shows only the growth text, without the vacancy link.
- **No plan yet:** an empty state "Kies je droombaan" + CTA to `/carriere`.

### 04.3 Tests (04)
- `RoleFitCheckSession` parity with the old panel; `RoleFitBandRules` bands.
- The 4 results come from the DTO fields, with no % in the tab.
- The culture card is hidden without `CultureFitLabel`.
- The vacancies card and the "Match op deze stap" link are hidden and absent in the DOM when employers are OFF.
- `CareerPlanViewBuilder` parity with `CareerDashboard`; the shell stepper states; the no-plan empty state.
- Course blocks via the context (free first, hidden when empty).
- Playwright: both tabs fit 1440×900; the fit tab on 390×844.

---

## 05 · Phase 4: tabs "Bewijzen" + "Mijn gegevens"
Mockups: `pp-d5-bewijzen.png`, `pp-d6-mijn-gegevens.png`, `pp-m4-mijn-gegevens.png`.

> Size note: if the diff grows past ~1,500 lines, split it into 05a (the extraction + Bewijzen) and 05b (Mijn gegevens + removing the transitional rule). Each is one PR into `acceptatie` with the same rules.

### 05.1 Extract the profile form (no duplicate logic)
- **Split `Profile.razor`** (1,921 lines) into section components under `Components/Candidate/ProfileSections/`:
  - `PersonalSection` (names, phone, WhatsApp, date of birth, address + geocoding)
  - `DevicesSection`
  - `PreferencesSection` (max travel, transport, interests, licenses)
  - `AvailabilitySection` (hours + presets)
  - `ExperienceSection` (employers)
  - `EducationSection`
  - `CertificatesSection`
  - `ReferencesSection` (max 3)
  - `CvSection` (own CV upload/replace/remove/download + Lobsy-CV)
  - `MotivationSection` (about me, default motivation)
  - `ConsentSection` (tests/AI consent accept, withdraw, and withdraw + delete results; talent-pool consent; parental consent under 16)
  - `DeleteAccountSection` (the `UnsubscribeDialog` flow)
- **One shared editor state** `CandidateProfileEditor` (Web, scoped per circuit) holds the model, dirty tracking, validation, the existing save calls and the save bar.
  - Reuse `CandidateProfileService` / the existing API calls; don't add endpoints.
  - `Profile.razor` becomes a thin host of these sections with **identical markup and classes**, so existing tests keep working (`AccountUnsubscribeTests`, `CandidateProfileServiceTests`, `AccountDeleteCopyTests`, Playwright profile flows).
- **Localize** the hardcoded Dutch consent strings on the way (`Profile.razor` ~L585–630 and the toast texts ~L1607–1630).

### 05.2 Tab "Bewijzen"
- **Top strip "Je nieuwe schaal":**
  - A soft → hard segment bar: 8 segments in stepped tokens, filled = the number of proofs (employers + education + certificates + references + own CV), capped at 8.
  - Label "{n} bewijzen · steeds steviger".
  - Right side: the next missing item (for example "Nog 1 recensie en 1 certificaat, dan is je schaal hard."). This is derived in `ProofStrengthRules`. No storage.
- **3 cards:**
  - "Ervaring": timeline + "Werkgever toevoegen" → `ExperienceSection` in an inline editor or sheet
  - "Opleiding & certificaten": level pills + timeline + "Certificaat toevoegen"
  - "Recensies & CV": references (max 3, with "Recensie toevoegen (max 3)") + own CV with "Vervangen" / "Download"
  - Editing reuses the section components (05.1) in a `filter-sheet` on mobile and inline on desktop. No second form.
- **Note:** "Je Lobsy-CV (PDF) maken we automatisch van je bewijzen en je tests." + the existing download.

### 05.3 Tab "Mijn gegevens"
- **Quick switches** (row of 3):
  - "Beschikbaar voor werk" (`OpenForWork`): hidden when employers are OFF
  - "Anoniem in de talentpool" (talent-pool consent, "staat standaard uit"): hidden when employers are OFF
  - "Tests en AI-analyse": status pill "Aan · sinds {datum}" / "Uit", which opens Privacy
  - Switches save through the same editor/consent calls.
- **Accordions** (2 columns desktop, 1 mobile; one open at a time; `aria-expanded`/`aria-controls`; the row is a `<button>`):
  - Persoonlijk (incl. devices)
  - Voorkeuren & reistijd
  - Beschikbaarheid
  - Mijn motivatie
  - Privacy & toestemming (incl. parental consent)
  - Account verwijderen (`--danger` title, the existing confirm flow)
  - Each accordion hosts its section component (05.1).
- **Remove the transitional rule** (`PassportRedirects.ClassicTabsUntilPhase4`). With the flag ON, `/candidate/profile?tab=profile` now redirects to `?tab=data`, and the tabs `proof`/`data` render the real content.

### 05.4 Tests (05)
- **Extraction parity:**
  - bUnit renders `Profile.razor` sections with the same markup and classes as before (snapshot of key selectors)
  - the editor save round-trip
  - the consent actions call the same API methods
  - unsubscribe/delete still requires verification
- **`ProofStrengthRules`** table test.
- **Bewijzen:** the add flows open the shared section; references are capped at 3.
- **Mijn gegevens:**
  - the switches are hidden when employers are OFF
  - parental consent shows only under 16
  - "Account verwijderen" opens the existing dialog
- **Redirect:** with ON, `tab=profile` now goes to `data`.
- **Localization:** no hardcoded Dutch left in the moved sections (grep test on `ProfileSections/*.razor`).
- **Playwright:** both tabs at 1440×900 fit about 1 screen with the accordions closed; `m4` at 390×844; the full edit → save flow works in the paspoort and in the classic profile (flag OFF).

---

## Scope: what doesn't exist yet
| Item | Decision |
|---|---|
| Profile photo upload | **Deferred.** Initials avatar, no edit badge. |
| "Deel mijn paspoort" (sharing) | **Deferred.** Not rendered. Needs a public link + privacy design. |
| Stamps ("Mijn schalen") | **Built in 02, derived** from existing data (no storage, no dates). |
| Spoken languages | **Deferred.** Not shown (no field in the model). |
| Real course / affiliate data | **Model + admin in 03.** The list ships empty; admin curates (`ShowInPassport`). No fake providers anywhere; the mockup providers live only in test fixtures. |
| Member number | Built in 02 as a non-PII display hash. |
| "Nog {x} vragen tot je volgende schaal" | Built in 02 from existing answered/total counts. |

## Open decisions (for Dennis)
- **O1:** Should the new nav order (Paspoort left, Carrière right) also apply when the passport flag is OFF? Default: no, OFF = exactly as now.
- **O2:** When employers are OFF, are the Salesmanager, Ambassadeur and Partner portals (employer acquisition) blocked too? Default: yes.
- **O3:** When employers are OFF, `ExternalVacanciesController` (the API-key vacancy import for integrators) returns 404, so integrations get errors. Default: 404. The alternative is 503 with a message.
- **O4:** With the passport ON, does candidate `/home` redirect to the paspoort? Default: yes.
- **O5:** Course block with no free option: hide it, or show only the partner option? Default: hide (candidate first).
- **O6:** When employers are OFF, the candidate nav has only Paspoort/Profiel + Carrière until "De ontdekkingsreis" (06+) arrives. Is that acceptable?
- **O7:** Already in production today, outside this spec: the Functiefit `TrainingOffersBlock` shows seeded providers (LOI, NTI and "Praktijkacademie Haaglanden" with base URL `rocmondriaan.nl` and invented workshop paths, `TrainingUpskillService.Seeds()` / `SkillsAcademy()`). The paspoort won't show them (curation flag). Clean this up in a separate PR?
- **O8:** With 06+ (Ontdekkingsreis), the candidate nav gets 6 items while employers are ON. The design system allows max 5, so 06 has to choose which to merge.

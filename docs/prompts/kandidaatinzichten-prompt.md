> **Warnings:** deliver a Release build with 0 new warnings vs the base branch (0 warnings after code-health step 11). Run `dotnet format` and `.github/scripts/count-build-warnings.sh` before opening the PR. No blanket `<NoWarn>`.

# Cursor prompt: Kandidaatinzichten, "Wat beweegt kandidaten in jouw regio" (employer insights)

Branch from `acceptatie`. ONE PR into `acceptatie`. Do not merge, do not deploy, do not use rule 123.

## 0. Rules (read first)
- **Branch:** `git fetch origin && git checkout -b cursor/kandidaatinzichten origin/acceptatie`.
  - The code references below are from `origin/acceptatie` @ `0f5ed4fa` (2026-09-28 06:37 CEST). Re-check the line numbers, since they may have moved.
- Never push to `acceptatie` or `main`. See `docs/release-flow.md`.
- **Reference mockups** are on branch `docs/kandidaatinzichten`, folder `docs/mockups/kandidaatinzichten/`. Read them with
  `git show origin/docs/kandidaatinzichten:docs/mockups/kandidaatinzichten/<file>`, or check the folder out (`git checkout origin/docs/kandidaatinzichten -- docs/mockups/kandidaatinzichten docs/prompts/kandidaatinzichten-prompt.md`) and include it in the PR.
  - **Variant A = the main page** (build this as the page):
    - `ki-a-desktop.png` (1440×900) and `ki-a-desktop-lang.png` (full page): bedrijfsmanager with "Alle vestigingen (3)"
    - `ki-a-mobiel.png` (390×844) and `ki-a-mobiel-lang.png`: branchmanager with their own vestiging locked
    - `.html` sources: `ki-a-desktop.html`, `ki-a-mobiel.html`
  - **Variant B = "Bekijk als story"** (opened from a button on the main page):
    - `ki-b-desktop.png` / `ki-b-desktop-lang.png`: grid of insight cards on desktop
    - `ki-b-mobiel.png` / `ki-b-mobiel-lang.png`: card carousel + feed on mobile
    - `ki-b-mobiel-story.png` (story card 4, full-screen swipe viewer) and `ki-b-mobiel-story-pro.png` (the locked Pro card)
    - `.html` sources: `ki-b-desktop.html`, `ki-b-mobiel.html`, `ki-b-mobiel-story.html`, `ki-b-mobiel-story-pro.html`
  - **All numbers, names and vacancies in the mockups are example data.** Remove the "Voorbeelddata" labels, which are mockup-only; render real data only.
  - The mockup HTML uses inline styles and raw hex values for speed. Do **not** copy that. Rebuild with design-system tokens and classes (below).
- **Design system:** `.cursor/rules/design-system.mdc`.
  - Tokens only (`--brand`, `--surface`, `--muted`, `--border`, `--pearl`, `--success`, `--warn`, `--accent-soft`, …). The one exception is the map ring blue (see §5.4).
  - Font weights 400/600 (700 only for the `h1`).
  - Tap targets at least 44×44 px.
  - Logical properties (RTL for `ar`).
  - Only the breakpoints 640/900/1024. Mobile first.
  - Reuse `panel-page`, `stat-card`/`MetricTile`, `btn-compact`, `status-pill`, `lobsy-dialog`. No new card or button families. BEM classes under a new block `insights-…`.
- **Must NOT touch** (not even a formatting change):
  - banenkaart: `wwwroot/js/jobMap.js`, `jobsyMapLibre.js`, `css/features/banenkaart.css`, `VacancyDiscovery.razor`
  - `wwwroot/js/app-core.js` and its `?v=` tag in `App.razor`
  - filter-sheet CSS, bottom-nav CSS, `app.css` / `app.min.css`
  - `CookieConsentBanner`, `SalesWalletChip`
  - gratis-DNA files (`GratisDna*`, `css/features/gratis-dna.css`)
  - `TestDetail.razor`, `MatchPage.razor`
- **New CSS** goes only in a new file **`Jobsy.Web/wwwroot/css/features/kandidaatinzichten.css`**:
  - Link it in `Jobsy.Web/Components/App.razor` next to the other feature sheets (`App.razor:87-93`) **and** in the `<noscript>` block (`:95`), with its own tag, e.g. `?v=20260928-insights`.
  - Add the file to **`Jobsy.Tests/asset-versions.json`** (sha256 + v), which is checked by `Jobsy.Tests/AssetVersionGuardTests.cs:36-41`.
  - Do not change any other `?v`.
- **No new payment flow.** Locked content links to the existing tokens page (§6).

## 1. What exists today (verified in code)
| Topic | Where | Fact |
|---|---|---|
| Talentpool page | `Jobsy.Web/Components/Pages/Employer/TalentPool.razor:1-3` | `@page "/employer/talent"`, `InteractiveServer (prerender: false)`, `[Authorize(Roles = "BranchManager,RegionalManager,EnterpriseManager,Intermediary")]`, uses `panel-page`. |
| Employer nav | `Jobsy.Web/Navigation/RoleNavCatalog.cs:40-47` (Enterprise), `:76-79` (Regional), `:84-91` (Branch), `:98-104` (Intermediary), role switch `:154-168` | `Nav.Talent` → `/employer/talent` with `ExtraActivePaths ["/employer/talent-contacts"]` (`:44`, `:88`, `:101`). Regional has **no** Talent item. |
| Company scoping | `Jobsy.Core/Interfaces/ICompanyAuthorizationService.cs`; `Jobsy.Infrastructure/Services/CompanyAuthorizationService.cs:42-98` (`GetAccessibleCompanyIdsAsync`), `:144-147` (BranchManager → only `User.CompanyId`), `:149-165` (memberships; EnterpriseManager expands child vestigingen), `:109-116` (`EnsureCanAccessCompanyAsync` → `ForbiddenCompanyAccessException`) | A **vestiging is a `Company`** with `ParentCompanyId` (`Jobsy.Core/Entities/Company.cs:29,32-34`, `Location` = `GeoPoint`). |
| Controller pattern | `Jobsy.Api/Controllers/TalentPoolController.cs:11-13` (`[Route("api/employer/talent")]`, `[Authorize(Policy = JobsyPolicies.RequireEmployer)]`), `:30` (`[EnableRateLimiting("public-read")]`), `:137-156` (`ResolveCompanyAsync`) | Thin controller, service in Infrastructure, DTOs as records. |
| Company list (for the vestiging selector) | `Jobsy.Web/Services/JobsyApiClient.cs:1682-1683` `GetMyCompaniesAsync` → `api/companies/mine` | Already scoped per user. |
| Candidate data | `Jobsy.Core/Entities/User.cs:37-45` (consents), `:53` `HomeLocation` (GeoPoint), `:55` `PreferencesJson`, `:72` `AvailableFromDate`, `:75` `LastLoginAtUtc`; `JobsyDbContext.cs:139-143` (`geometry(Point, 4326)` + GIST index) | |
| Preferences | `Jobsy.Core/Contracts/CandidateContracts.cs:3-31` (`CandidatePreferencesDto`: `Roles`, `MaxTravelMinutes`, `MinHoursPerWeek`, `MaxHoursPerWeek`, `FlexibleTimes`, `AvailabilityPresets`), parser `MatchingProfileMapper.DeserializePrefs` (`Jobsy.Core/Rules/MatchingProfileMapper.cs:185`); preset codes `AvailabilityPresetRules.cs:9-16` (`direct, school, weekend, evening, office, holiday, parttime, fulltime`) | |
| Dream jobs | `Jobsy.Core/Entities/CandidateCareerPlan.cs:10-12` (`DreamTitle`, normalised `DreamKey`) | |
| Tests / DNA | `CandidateCompetencies`, `CandidateCareerInterests` (RIASEC %), `CandidateCulturePersonalityProfiles`, `CandidateValuesProfiles` (`Autonomy/Connection/Achievement/Stability/Impact`, `SchwartzValuesCatalog.cs:14-18`); status `CandidateCompetencyStatuses.IsCompleted` (`Jobsy.Core/Entities/CandidateCompetency.cs:41`) | |
| Consent rules | `Jobsy.Core/Privacy/CandidateConsentRules.cs:22-27` (`CanUseCandidateFeatures`, `HasCurrentTestAiConsent` = test/profiling consent at `PrivacyConstants.CandidateProfilingConsentVersion`) | |
| Existing matching | `Jobsy.Core/Entities/CandidateMatchSnapshot.cs:7-15` (`MatchesJson` per candidate), DTO `Jobsy.Core/Interfaces/ICandidateCompetencyService.cs:43-53` (`CandidateMatchedVacancyDto.Id` = vacancy id, `MatchPercent`); service `ICandidateMatchSnapshotService` ("read stored matches… never scores vacancies") | |
| Spatial SQL pattern | `Jobsy.Infrastructure/Services/VacancyProductService.cs:1119-1165` (`ST_DWithin` on `"HomeLocation"` via `SqlQueryRaw`, **InMemory fallback** for tests) | Reuse this pattern. **Do not** copy `TalentPoolService.cs:53-58,142-150` (loads 500 users, then one routing call per user = N+1). |
| Tokens (gate) | `Jobsy.Core/Interfaces/ITokenLedgerService.cs:8` `GetBalanceAsync(companyId)`; `Jobsy.Api/Controllers/TokensController.cs:46-70` (balances; `Company.TokensManagedByEnterprise`, `Company.cs:47`); tokens pages `/employer/tokens` and `/branch/tokens` (`RoleNavCatalog.cs:45,89`) | **No "Pro" flag/plan exists** in the codebase. |
| Cache | `Jobsy.Core/Interfaces/IDashboardCache.cs` (10-min TTL, tracked keys), `IMemoryCache` in `CandidateMatchSnapshotService.cs:21` | |
| Vacancy fields (for the tip list) | `Jobsy.Core/Entities/Vacancy.cs:11` `HourlyWage`, `:115` `SalaryTableId`, `:132` `FlexibleTimes`, `:170` `CulturePillarsJson` (+ `CulturePillarCatalog.HasProfile`, `CulturePillarCatalog.cs:70`) | |
| Privacy page | `Jobsy.Web/Components/Pages/Legal/Privacy.razor:126-137` (section 5b, tests + talentpool) | |
| Localization | `Jobsy.Web/Localization/UiStrings.cs:3376-3383` (`…MergeAll(nl, en, pl, ro, ar)`); pattern file `UiStringsGratisDna.cs` / `UiStringsOnboardingV2.cs` | Five languages. |
| Test tooling | `Jobsy.Tests/Jobsy.Tests.csproj` (xUnit, **bunit 1.36.0**, Playwright 1.49, Mvc.Testing, EF InMemory); Playwright soft-skip pattern `Jobsy.Tests/Acc2709PlaywrightTests.cs:18` (`JOBSY_E2E_BASE_URL`) | |

## 2. Routes, navigation, roles
- **Page:** new `Jobsy.Web/Components/Pages/Employer/CandidateInsights.razor`:
  - `@page "/employer/kandidaatinzichten"`, `InteractiveServer (prerender: false)` like `TalentPool.razor`
  - `[Authorize(Roles = "BranchManager,RegionalManager,EnterpriseManager")]`
  - Story mode = same page with `?weergave=story` (deep-linkable, and the back button closes the story).
- **Tabs** "Talentpool | Kandidaatinzichten" (as in the mockups):
  - Add a small shared component `EmployerTalentTabs` at the top of `TalentPool.razor` and the new page.
  - Hide the insights tab for Intermediary (Intermediary keeps Talentpool as it is today).
- **Nav active state:** add `"/employer/kandidaatinzichten"` to the `ExtraActivePaths` of `Nav.Talent` at `RoleNavCatalog.cs:44` (Enterprise) and `:88` (Branch). **Not** at `:101` (Intermediary).
  - RegionalManager has no Talent item. Add a `Nav.CandidateInsights` item to the Regional list (`:76-79`, which then has 5 items, the max) with the `NavIcons.Users` icon.
  - This is a `RoleNavCatalog` data change only; **no bottom-nav CSS change**. Update `Sprint1ShellTests` / `RoleNavCatalogTests` accordingly.
- **Authorization matrix** (enforced in the API, the page only mirrors it):

| Role | May see |
|---|---|
| EnterpriseManager (bedrijfsmanager) | all own-company vestigingen (`GetAccessibleCompanyIdsAsync`, incl. children) + "Alle vestigingen" (union) |
| BranchManager | **only** own vestiging (`User.CompanyId`, `CompanyAuthorizationService.cs:144-147`); the selector is locked (lock icon, as in `ki-a-mobiel.png`) |
| RegionalManager | **read-only**, only vestigingen in their region (their memberships from `GetAccessibleCompanyIdsAsync`); no "Vacatures verbeteren" action links |
| Admin, Intermediary, Candidate, SalesManager, Ambassadeur, anonymous, API-key clients | **403** (anonymous 401) |

- Any `branchId` outside the accessible set → **403** (never 404, never an empty 200). "Alle vestigingen" (`branchId` omitted) is allowed only for EnterpriseManager/RegionalManager with ≥2 accessible vestigingen.

## 3. API (aggregates only)
New `Jobsy.Api/Controllers/CandidateInsightsController.cs`:
- `[Route("api/employer/candidate-insights")]`, `[Authorize(Policy = JobsyPolicies.RequireEmployer)]` + an explicit role check for the matrix above.
- `[EnableRateLimiting("public-read")]`.
- The service `ICandidateInsightsService` (Core interface, Infrastructure implementation) does the work.

Endpoints:
1. `GET api/employer/candidate-insights?branchId={guid?}&radiusKm={10|20|30}&period={30|90|365}` → `CandidateInsightsDto`.
   - Validation: any other `radiusKm` or `period` → 400. Query params that look like identity/age filters (`minAge`, `age`, `userId`, …) → 400, like `TalentPoolController.cs:41-48`.
2. `GET api/employer/candidate-insights/branches` → the vestigingen the caller may pick: `{ id, name, isLocked }`. You may reuse `api/companies/mine` if it already filters correctly.

### 3.1 DTO shape (records, no PII by construction)
```
CandidateInsightsDto(
  InsightsScope Scope,            // branch ids/names shown, radiusKm, period, generatedAtUtc, isFullAccess
  InsightsKpis Kpis,              // candidatesInRadius, avgHoursPerWeek, candidates32PlusHours, active30d, matchingYourVacancies (≥70%)
  IReadOnlyList<RankedItem> DreamJobsTop,   // top 3 when !isFullAccess, top 10 when full
  InsightsDistribution? WorkFields,
  InsightsDistribution? DnaRiasec,          // null when locked
  InsightsDistribution? Competences,        // null when locked
  InsightsDistribution? Personality,        // null when locked
  InsightsDistribution? Priorities,         // reistijd / flexibiliteit / sfeer & cultuur / zekerheid (see 4.4)
  InsightsDistribution? WorkKinds,          // fulltime / parttime / bijbaan / stage / vrijwilliger
  IReadOnlyList<DensityCell> Density,       // grid cells, see 4.3
  IReadOnlyList<VacancyReach> Vacancies,    // per open vacancy of the scope
  InsightsTrend Trend,                      // status "insufficient_history" until 3 months (4.6)
  IReadOnlyList<string> LockedSections)     // e.g. ["dreamJobs4to10","dna","story5to10"]
```
- Every count-bearing value is a **`SuppressedCount`**: `{ status: "ok" | "insufficient", value: int? }`, where `value` is null when insufficient. Every percentage is `{ status, percent: int? }`.
- `RankedItem(label, SuppressedCount)`, `DensityCell(cellId, centerLat, centerLng, band: 1|2|3)`, `VacancyReach(vacancyId, title, branchName, SuppressedCount matchingCandidates, InsightsTips? tips)`.
- **Forbidden in any DTO** (the reflection test in §9 enforces this): no `UserId`/candidate ids, names, e-mails, phones, birth dates, ages, exact addresses, candidate coordinates, raw answers, free text written by candidates (dream job labels come from `DreamKey` grouping; see 4.2).

## 4. Aggregation rules (server side, in `CandidateInsightsService`)
### 4.1 Cohort (who is counted)
A candidate is counted only when **all** of these hold:
- `Role == Candidate`, `IsActive`
- `HomeLocation != null`
- `CandidateConsentRules.HasCurrentTestAiConsent(user)` (the existing profiling consent, `CandidateConsentRules.cs:25-27`)
- `CandidateConsentRules.CanUseCandidateFeatures(user)` (so under-16s without parental consent are excluded)
- `LastLoginAtUtc` within the selected period
- `HomeLocation` within `radiusKm` of the vestiging `Location` (for "Alle vestigingen": within the radius of **any** selected vestiging, each candidate counted once)

Other rules:
- Crow-flies distance only, **no routing calls**.
- Use one `ST_DWithin` query that returns ids (the pattern at `VacancyProductService.cs:1127-1150`), with an InMemory fallback for tests.
- No talent-pool consent or `OpenForWork` is required, because nobody becomes findable or contactable. Document this in the PR (§10).

### 4.2 k-anonymity (threshold 10), enforced server side
- `const int KAnonymityThreshold = 10` in a Core class `CandidateInsightsPrivacy`.
- If the cohort itself is `< 10`, return every section as `insufficient` (the KPI tile shows "Te weinig data").
- Every bucket/group/cell/vacancy count `< 10` → `status = "insufficient"`, `value = null`. **Never** return raw small counts, not even in logs.
- Percentages are computed only over sections whose denominator is ≥ 10, and each shown bucket must itself be ≥ 10; otherwise the bucket is `insufficient`.
- **Differencing protection:** round every returned count to the nearest 5 (after the ≥10 check), and percentages to whole numbers. This way 10/20/30 km or period switches cannot reveal groups under 10 by subtraction.
- **Dream jobs:** group by `CandidateCareerPlan.DreamKey`; label = the canonical title for that key (existing career-step catalog/`CareerStepKey` or the most common `DreamTitle` normalised). Keys with < 10 candidates are dropped from the ranking (not shown as "te weinig data" rows). Free-text titles never leave the server unaggregated.

### 4.3 Density map (no individual locations)
- Snap each counted `HomeLocation` to a fixed grid (≈1.5 km cells, e.g. a hex or square grid in a local metric projection anchored on a **fixed global origin**, not on the vestiging, so cells don't shift per request).
- Return only the cell centre (`cellId`, `centerLat`, `centerLng`) and a **band** 1/2/3 (terciles of the returned cells); no counts per cell.
- Cells with `< 10` candidates are **omitted**.
- Never return candidate coordinates.
- Include the vestiging location(s), which are company data and fine to show.

### 4.4 Sections and data sources (only real data; no invented proxies)
| Section | Source | Notes |
|---|---|---|
| KPIs | cohort size; `MinHoursPerWeek/MaxHoursPerWeek` midpoint average; count with `MaxHoursPerWeek ≥ 32` (or preset `fulltime`); `LastLoginAtUtc ≥ now-30d`; "match met jouw vacatures" = distinct cohort candidates with ≥1 open scope vacancy at `MatchPercent ≥ 70` in their stored `CandidateMatchSnapshot` | all `SuppressedCount` |
| Top droombanen | `CandidateCareerPlan.DreamKey` | top 3 free, top 10 full |
| Top werkvelden | `CandidatePreferencesDto.Roles` mapped to the existing vacancy categories if a mapping exists; otherwise show the top preferred roles | check the codebase first and document the choice |
| Werk-DNA (RIASEC) | completed `CandidateCareerInterests`: share per strongest type | locked |
| Competenties | completed `CandidateCompetencies`: share with that dimension ≥ 60% | locked |
| Persoonlijkheid | completed culture/personality profiles, same method | locked |
| Wat kandidaten belangrijk vinden | **reistijd** = `MaxTravelMinutes ≤ 20`; **flexibiliteit** = `FlexibleTimes == true`; **sfeer & cultuur** = values `Connection ≥ 60`; **zekerheid & duidelijke afspraken** = values `Stability ≥ 60` | There is **no candidate salary-preference field**. Do **not** show "Goed salaris" (the mockup row is illustrative). Replace it with "Zekerheid en duidelijke afspraken", or drop it, and say so in the PR. |
| Soort werk | `AvailabilityPresets`/hours: fulltime (`fulltime` or ≥32 h), parttime (`parttime` or 12–31 h), bijbaan (`school`/`weekend`/`evening`/`holiday` presets, <12 h); stage / vrijwilliger only if a real candidate preference exists | if no source exists, return the bucket `insufficient` and don't fake it |
| Jouw vacature past bij X | open vacancies of the scope vacancies × cohort snapshots (`MatchesJson` entries with `Id == vacancyId` and `MatchPercent ≥ 70`) | Use the **stored** snapshots. **Do not** call `ComputeLiveAsync` or score vacancies here (the interface says worker-only). Stale/missing snapshots simply don't count. |
| Trends | see 4.6 | placeholder |

- **Efficiency:** at most a handful of set-based queries per request:
  1. cohort ids (spatial SQL)
  2. users/prefs for those ids
  3. career plans
  4. the 4 test tables, filtered by `userIds.Contains`
  5. snapshots
  6. scope vacancies
- **No per-candidate queries** (no N+1). Parse `PreferencesJson`/`MatchesJson` in memory for the bounded cohort. Cap the cohort at e.g. 20,000 ids and log a warning above it.
- **Caching:** cache the full DTO per `(sorted branchIds, radiusKm, period, isFullAccess)` for **1 hour** (`IMemoryCache`, or `IDashboardCache` with a new `DashboardCacheKind`). The cache key contains no user id. Authorization is checked **before** the cache lookup on every request.

### 4.5 "Maak je vacature aantrekkelijker" tips and the Orderpicker list
- The tip card text is generic, chosen from the top priority buckets (e.g. reistijd high → "Noem de reistijd en OV-verbinding").
- The story card's per-vacancy checklist ("Zo scoort je vacature … hierop") is **derived from vacancy fields where available, otherwise hidden**:
  - **Salaris genoemd** = `HourlyWage > 0 || SalaryTableId != null`
  - **Flexibele uren** = `Vacancy.FlexibleTimes`
  - **Iets over sfeer en team** = `CulturePillarCatalog.HasProfile(CulturePillarsJson)`
  - **Reistijd:** Lobsy shows travel time automatically, so **hide that row** (no field to check).
- Show the checklist only for a vacancy of the selected scope (the one with the highest reach). Hide it for RegionalManager (read-only) and when there is no open vacancy.

### 4.6 Trends
- Always return `Trend.status = "insufficient_history"` until the platform has ≥ 3 months of cohort data for that scope. Detect this from the earliest `LastLoginAtUtc`/account creation in the cohort; simplest is to keep it a placeholder in this PR.
- UI: the dashed placeholder "Beschikbaar zodra er genoeg data is" (mockup A/B).
- Do not build a trend store in this PR.

## 5. UI (mobile + desktop per the mockups)
### 5.1 Main page = variant A
- Header:
  - tabs; `h1` "Wat beweegt kandidaten in jouw regio"; lead line
  - a `status-pill` "Gratis versie" when `!isFullAccess`
  - **one** primary button: "Volledige inzichten" (only when locked) **or** "Bekijk als story" (when full). When locked, "Bekijk als story" is a secondary button next to it.
- Filters row:
  - vestiging selector (locked for BranchManager)
  - radius segmented control **10/20/30 km** (default 20), period select (30/90/365 days, default 90)
  - Changing a filter reloads via the API and shows skeleton tiles, with no layout shift.
- One quiet line (not a banner) with a shield icon: **"Anoniem, minimaal 10 kandidaten per groep. Kleinere groepen tonen we als 'te weinig data'."**
- Sections and grid as in `ki-a-desktop.png`:
  - 4 KPI tiles (12-col grid: 4×3)
  - droombanen (7) + map (5)
  - competenties & werk-DNA (7) + wat belangrijk + tip (5)
  - werkvelden / soort werk / trends (4/4/4)
  - vacatures list (12)
- Mobile: single column in the order of `ki-a-mobiel-lang.png` (KPIs 2×2, map, droombanen, vacatures, belangrijk, DNA, soort werk, werkvelden, trends, sticky-free bottom CTA).
- "Te weinig data" is rendered as a small neutral chip (`status-pill` modifier) with an info icon and text. Never colour only.

### 5.2 Story mode = variant B ("Bekijk als story")
- Cards in this order:
  1. Regio + map
  2. Droombanen top 3
  3. Beschikbaarheid
  4. Wat ze belangrijk vinden (+ tip + vacancy checklist)
  5. Soort werk
  6. Jouw vacatures
  7. Werk-DNA
  8. Werkvelden
  9. Trends
  10. Volledige inzichten (overview of what's locked)
- **Mobile:** full-screen viewer as `ki-b-mobiel-story.png`: segmented progress bar, header "Kandidaatinzichten · {vestiging}" + "n van 10 · {radius} · {periode}", close button, swipe left/right (pointer events + CSS scroll-snap), tap zones, prev/next buttons (44 px) and keyboard arrows. Esc closes. `aria-roledescription="carousel"`, each card `role="group"` with a label. Honour `prefers-reduced-motion`. No auto-advance timer (calm UI).
- **Desktop (≥1024 px):** the grid of cards as `ki-b-desktop.png` (the same card components, no viewer).
- **Locked cards 5–10** (when `!isFullAccess`): show the card title + a lock overlay; card 10 is the CTA card (`ki-b-mobiel-story-pro.png`).

### 5.3 Locked state
- Locked blocks: dream jobs 4–10, competenties/DNA/persoonlijkheid, story cards 5–10.
- Visual: blur + lock icon + the text "Volledige inzichten met tokens" + **one** CTA "Volledige inzichten".
- The CTA links to the existing tokens page: `/branch/tokens` for BranchManager, `/employer/tokens` for the other roles.
- **The blurred content must be static placeholder markup, never real data.** The API returns `null` for locked sections, so nothing leaks via the DOM/devtools.
- Remove the word "Pro" from UI texts, because no Pro plan exists (see §6); use "met tokens".

### 5.4 Map
- A small MapLibre map inside the card, or a static rendered map.
  - **Reuse the existing MapLibre setup read-only** (you may call the existing loader) but **do not modify** `jobMap.js`/`jobsyMapLibre.js`. If a map instance is needed, put a tiny separate module in `wwwroot/js/features/kandidaatinzichten-map.js` (loaded only on this page).
- Non-interactive except zoom buttons. Muted/desaturated base map.
- Layers:
  1. **density cells** (green `--success` at 3 opacity bands, no counts)
  2. **radius rings 10/20/30 km** in the banenkaart V3 style: strong blue strokes (3 px; the selected radius 5 px with a white halo), soft graduated fill (darkest inside), readable **pill labels** ("10 km", "20 km", "30 km", selected = filled pill)
  3. the vestiging marker(s)
- **No individual pins.**
- The ring blue is the one extra colour allowed. Define it as a CSS custom property inside `kandidaatinzichten.css` (e.g. `--insights-ring`), matching the V3 blue from `docs/mockups/banenkaart-v3` / `banenkaart.css`. Don't edit `banenkaart.css`.
- Legend below the map: "Minder → Meer kandidaten", "Straal vanaf vestiging", "Geen individuele locaties · gebieden met minder dan 10 kandidaten blijven leeg".

## 6. Freemium gate (one feature check)
- **No "Pro" plan/flag exists in the codebase.** Use **token balance** as the single gate:
  - `isFullAccess = ITokenLedgerService.GetBalanceAsync(walletCompanyId) > 0`
  - `walletCompanyId` = the selected vestiging, or its `ParentCompanyId` when `Company.TokensManagedByEnterprise` is true (`Company.cs:47`, and the same logic `TokensController.cs:46-70` exposes)
  - For "Alle vestigingen": full access if the enterprise wallet (or any selected vestiging wallet) has a positive balance.
- Put it in one method `CandidateInsightsAccess.IsFullAccessAsync(...)` so it can later switch to a subscription flag. **Viewing insights spends no tokens.** Document this choice in the PR.
- **Free:** KPIs, dream jobs top 3, the map, "wat kandidaten belangrijk vinden" (story card 4), and on the main page the soort werk / werkvelden / vacancy reach / trends placeholder, as shown open in mockup A.
- **Locked:** dream jobs 4–10, competenties & werk-DNA & persoonlijkheid, story cards 5–10.
- The server returns `LockedSections` and nulls; the UI never decides access on its own.

## 7. Privacy page
- Add to `Privacy.razor` section 5b (`:126-137`), NL and the translated versions if the page is localised, a sentence like:
  > "Werkgevers kunnen geanonimiseerde regio-inzichten zien (bijv. hoeveel kandidaten binnen 10–30 km wonen, populaire droombanen en werk-DNA-verdelingen). Je profiel telt daarin alleen anoniem mee, in groepen van minimaal 10 kandidaten; werkgevers zien geen namen, adressen of individuele antwoorden. Dit gebeurt alleen als je toestemming voor tests en profielanalyse hebt gegeven."
- **Do not bump** `PrivacyConstants.CandidateProfilingConsentVersion` in this PR (a bump forces every candidate to re-consent). Flag this in the PR description for Dennis to decide.

## 8. Texts (5 languages)
- New `Jobsy.Web/Localization/UiStringsCandidateInsights.cs` with `MergeAll(nl, en, pl, ro, ar)`, registered after the others in `UiStrings.cs:3376-3383`.
- Keys under `Insights.*` for every visible string (tabs, title, filters, anonymity line, KPI labels, sections, chips "Te weinig data", locked texts, CTA, story UI, map legend, tips, checklist rows) + `Nav.CandidateInsights`.
- NL and EN complete; pl/ro/ar with every key present.
- Add a unit test that all 5 dictionaries contain every `Insights.*` key.
- No hard-coded Dutch in the markup.

## 9. Tests (all green in CI)
- **k-anonymity** (unit, pure Core `CandidateInsightsPrivacy` + service with the InMemory DB):
  - cohort 9 → everything `insufficient`
  - a bucket of 9 → `insufficient` with `value == null`
  - 10 → ok
  - rounding to 5
  - dream keys < 10 are dropped
  - density cells < 10 are omitted
  - the differencing scenario (10 km vs 20 km with a group of 7 in the ring) does not reveal the 7
- **AuthZ per role** (Mvc.Testing / controller tests):
  - EnterpriseManager: own branches ok; another company's branch → **403**
  - BranchManager: own ok; a sibling branch of the same company → **403**; `branchId` omitted = own only
  - RegionalManager: region branches ok; others → 403
  - Admin / Intermediary / Candidate → 403; anonymous → 401
  - invalid radius/period → 400
  - age/id query params → 400
- **No PII (reflection guard):** walk `CandidateInsightsDto` recursively. Fail on any property whose name matches `(?i)(userid|candidate(id)?$|name$|email|phone|birth|age$|address|latitude|longitude)` **except** the whitelisted `DensityCell.centerLat/centerLng`, `VacancyReach.title/branchName` and the `Scope` branch names. Also assert that no property type is `User`/`GeoPoint`/entity types.
- **Cohort/consent:** a candidate without current `TestAiConsent`, inactive, without `HomeLocation`, or under 16 without parental consent is not counted.
- **Gate:** balance 0 → `LockedSections` contains `dreamJobs4to10`, `dna`, `story5to10`, and those DTO fields are null / top list length 3; balance > 0 → full. Enterprise-managed wallet uses the parent.
- **Efficiency:** the service issues a bounded number of queries independent of cohort size (e.g. count EF commands via an interceptor for 10 vs 200 candidates).
- **Cache:** a second identical call within 1 h does not hit the DB; a call by an unauthorised user is still 403 even when the key is cached.
- **bUnit:**
  - the page renders the locked state (blur placeholder + lock + "Volledige inzichten" linking to `/employer/tokens`, or `/branch/tokens` for BranchManager) with no real locked values in the markup
  - "Te weinig data" chips render for insufficient buckets
  - story card 6 is locked when `!isFullAccess`
- **Playwright** (soft-skip when `JOBSY_E2E_BASE_URL` is empty, pattern `Acc2709PlaywrightTests.cs:18`; employer credentials via env vars, skip if absent):
  - log in as an employer, open `/employer/kandidaatinzichten`, take a screenshot at **1440×900** and **390×844**
  - open "Bekijk als story" and take a screenshot
  - assert no horizontal overflow at 360/390/430
  - assert no `Voorbeelddata` text is present
- Existing tests (`RoleNavCatalogTests`, `AssetVersionGuardTests`, `PendingModelChangesTests`) stay green. No EF migration should be needed. If you add one, explain why.

## 10. Definition of done / PR description
- `dotnet build` and `dotnet test` are green.
- The PR description includes:
  - screenshots (desktop A, mobile A, story mobile, locked vs full)
  - the cohort/consent rule (4.1), the gate choice (tokens balance, no Pro flag), the "no salary preference → replaced/omitted" note, the source used for werkvelden and soort werk (stage/vrijwilliger), and the privacy-version decision (§7)
  - an explicit list of files **not** touched (§0)
- ONE PR into `acceptatie`. Do not merge, do not deploy, do not use rule 123.

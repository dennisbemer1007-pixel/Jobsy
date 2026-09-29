# 02 · Dashboard + Te doen (RM: Signalen)

> Read `00-README.md` first. §0, §IA, §R and D3, D4, D7, D14 and D20 apply.

| | |
|---|---|
| Branch | `cursor/werkgever-redesign-2` from `cursor/werkgever-redesign-1` (stacked) |
| PR | ONE PR into `acceptatie`: `feat(werkgever): dashboard with KPI's, Te doen and vestigingen overview`. Stacked on #<PR of 01> (`cursor/werkgever-redesign-1`) |
| Mockups | `bm-d1-dashboard.png` (BM), `bm-d7-dashboard-regiomanager.png` (RM, read-only "Signalen"), `bm-m1-dashboard.png` (mobile) |
| Split seam (if too big) | 2a = API + KPI's + vestigingen table; 2b = Te doen page + sidebar counts |

**Goal.** A manager opens Lobsy and immediately sees **what needs them**. Te doen first, numbers second. No generic metrics wall.

## 02.1 Today (verify first)
- `/werkgever` renders the moved `EmployerHomePanel`:
  - `Api.GetMyCompaniesAsync`
  - `Api.GetEmployerMetricsSummaryAsync(period, companyId)` → `api/dashboard/summary` (`MetricCount` keys such as `active_vacancies*`, `applications_pending*`; see `DashboardMemoryCache` / `DashboardLiveOverlay`)
  - `Api.GetVacancyPerformanceAsync` → `api/dashboard/vacancy-performance`
  - `MetricsCategoryBoard`, `RaamflyerTools`
- Data that exists:
  - `Application.CreatedAt` / `RespondedAt` / `Status`
  - `Vacancy.Status` (`Draft`, `Active`, `Archived`, `PendingApproval`, `Fulfilled`) and `PublishedAtUtc`
  - token balance per company (`ITokenLedgerService.GetBalanceAsync`)
  - `EstablishmentTakeoverRequest`
  - company users per company (`CompanyUsersController`)
- **Vacancy end date:** there is none at `a611db40` (§0 known differences). "Vacatures verlopen binnen 7 dagen" is built only if the code has one when you start.

## 02.2 API: `WerkgeverDashboardController` (new, `Jobsy.Api`)
- `[Authorize(Policy = JobsyPolicies.RequireEmployer)]`. Add `[RequiresFeature(PlatformFeature.Employers)]` if Dependencies A applies.
- Every call takes the scope as `companyIds` (from `EmployerScopeState`). The server **intersects** it with `GetAccessibleCompanyIdsAsync(user)`. An empty intersection returns 403 (an empty list never means "all").
- `GET api/werkgever/dashboard?period=7d|30d|90d&companyIds=…` returns `WerkgeverDashboardDto`:
  - `Kpis`:
    - `ActiveVacancies` (+ delta vs previous period)
    - `NewApplications` (+ delta %)
    - `AvgFirstResponseHours` (mean of `RespondedAt - CreatedAt` over applications that got a response in the period; `null` when < 5 samples, shown as "—" with the tooltip "Te weinig reacties om te meten")
    - `Hired` (status `Hired` in the period)
    - `TokenBalance` (BM/VM) **or** `TokensUsed` + `TokensAllocated` (RM, label "Tokenverbruik regio")
    - `TokenRunwayWeeks` (balance / average weekly spend over 8 weeks; `null` when there is no spend)
    - a 7-point `Spark` per KPI
  - `Funnel`: counts per stage for the period, using the 04 stages Sollicitaties → Geaccepteerd (Accepted) → Uitgenodigd (EmployerContacting) → Aangenomen (Hired)
  - `Branches`: rows for the vestigingen in scope with `Name`, `RegionName?`, `LiveVacancies`, `NewApplications`, `AvgFirstResponseHours`, `Status` (`OpSchema` / `Aandacht` / `Achterstand`)
    - Status rule: Achterstand when > 5 applications are pending > 48 h **or** first response > 3 d; Aandacht when there is no manager, the balance is ≤ 5 tokens, or first response > 2 d; otherwise OpSchema. Constants live in `WerkgeverDashboardRules` (Core) with unit tests.
- **Reuse** the existing metrics services and caches where they already compute a number (e.g. `active_vacancies`). Put new aggregates in one query service `IWerkgeverDashboardService` (Infrastructure), `AsNoTracking`, with a per-scope memory-cache entry of 60 s. No per-row N+1 queries: at most one query per aggregate.
- `GET api/werkgever/te-doen?companyIds=…&take=` returns `WerkgeverTodoItemDto[]` (`Kind`, `Severity` `Info|Warning|Danger`, `TitleKey` + args, `MetaKey` + args, `ActionKind`, `Href`, `Count`, `CompanyIds`). Kinds, sorted Danger → Warning → Info, then by count:

  | Kind | Source | BM | RM | VM | Action → target |
  |---|---|---|---|---|---|
  | `PublishRequests` | vacancies `PendingApproval` in scope | ● "Beoordelen" | ◐ text only ("de bedrijfsmanager keurt goed") | own ("wacht op bedrijfsmanager", no action) | `/werkgever/vacatures?tab=wacht` |
  | `ApplicationsOverdue` | `Pending` and `CreatedAt` < now − 48 h | ● "Bekijken" | ◐ "Bekijken" (read-only) | own | `/werkgever/sollicitaties?filter=overdue` |
  | `VacanciesExpiring` | **only if an end date exists** | ● "Verlengen" | ◐ | own | `/werkgever/vacatures?tab=verloopt` |
  | `LowTokens` | vestiging balance ≤ 5 (only when tokens are managed per vestiging) | ● "Tokens verdelen" | — | own: "Tokens aanvragen" (06; until 06 lands, text only) | `/werkgever/tokens/verbruik` |
  | `NoManager` | vestiging without an active VM membership | ● "Iemand uitnodigen" (opens the 05 drawer; until 05, links to Team & rechten) | ◐ text | — | `/werkgever/organisatie/team?invite=…` |
  | `Takeovers` | open takeover requests | ● "Beoordelen" | — | own | `/werkgever/overnames` |
  | `TokenRequests` | added in 06 | | | | |
  | `InsightsRequests` | added in 07 | | | | |

  Each kind is a small `ITodoSource` (DI, one class per kind) so 06 and 07 only add a class. The counts feed the sidebar (`CountKey`) and the dashboard badge from one call.

## 02.3 Page `/werkgever` (redesign, replaces `EmployerHomePanel` for employer roles)
- `WgPageShell`:
  - Title "Dashboard"
  - Lead "{scope label} · {n} vestigingen in {r} regio's · laatste {period}" (RM: "Regio Westland · 5 vestigingen · …"; VM: the vestiging name)
  - Actions: `period-tabs` 7 d / 30 d / 90 d (URL `?period=`), "Exporteren" (CSV of the KPI's + vestigingen table, generated client-side from the DTO; no new endpoint), primary "Vacature plaatsen" (`WgAction RequiresWrite`, → `/werkgever/vacatures/nieuw`; RM gets the disabled lock variant)
- RM: the `wg-readonly-hint` line "Je ziet alle vestigingen in {regio}. Reageren, publiceren en tokens kopen doen de vestigings- en bedrijfsmanagers."
- Row of 5 `EntKpiCard`s, as in d1/d7. The token KPI links to `/werkgever/tokens`.
- Two columns ≥ 1024 (left 1.1fr, right 1fr):
  - **Left:** card "Te doen" (RM: "Signalen in je regio" + pill "Ter info"; eye icon)
    - top 6 items with severity icon, title, meta and **one** action button
    - "Alles bekijken" → `/werkgever/te-doen`
    - empty state "Niets te doen. Alles loopt." (no illustration)
  - **Left:** card "Wervingstrechter" (horizontal bars, numbers right, `aria-label` per bar)
  - **Right:** card "Vestigingen" (RM: "Vestigingen in je regio") as an `EntDataTable` (compact, no checkbox):
    - columns Vestiging (+ regio for BM), Live vac., Nieuw, 1e reactie, Status (`dotst` + label)
    - row click → `/werkgever/vacatures?scope=vestiging:{id}`
    - "Alle {n} vestigingen" → `/werkgever/organisatie/vestigingen` (hidden for VM; VM has no vestigingen table, only the rest)
- Remove `MetricsCategoryBoard` and `RaamflyerTools` from the employer dashboard. The flyer moves to Wervingsmateriaal (05); until 05 lands, keep a small "Raamflyer" link card at the bottom so no feature disappears.
- **No** Kandidaatinzichten upsell card (D20).
- Loading: `PageContentSkeleton` per card. Errors: `PanelErrorBoundary` per card, so one failing aggregate doesn't blank the page.

## 02.4 Page `/werkgever/te-doen`
- Title "Te doen" (RM: "Signalen").
- Full list from `api/werkgever/te-doen` grouped by severity, using the same item component as the dashboard (`Components/Werkgever/Dashboard/WgTodoItem.razor`).
- Filter chips per kind with counts.
- RM: items without action buttons; the item links still open (read-only pages).

## 02.5 Mobile (`bm-m1`)
- h1 "Dashboard" + lead
- 2×2 KPI grid (Actieve vacatures, Nieuwe sollicitaties, Eerste reactie, Tokensaldo; RM: Tokenverbruik)
- "Te doen" list (tap row → target), "Vestigingen met achterstand" (only Status = Achterstand; hidden for VM)
- no funnel on mobile

## 02.6 Sidebar counts
`WerkgeverNavContext` gets `Counts` from the te-doen call (Te doen total, Sollicitaties = pending in scope, Vacatures = publish requests for BM). Refresh on navigation, at most once per 60 s.

## Tests
- `WerkgeverDashboardRulesTests`: status thresholds, runway, first response with < 5 samples → null.
- `WerkgeverDashboardServiceTests` (in-memory/SQLite, as existing service tests): KPI's per scope; a VM sees only their own vestiging; RM only their region; deltas; no data → zeros and an empty state.
- `WerkgeverDashboardApiTests`: 401 anonymous, 403 candidate, 403 for a foreign `companyIds` (BM of another org, VM on a sibling vestiging, RM outside the region); an empty intersection → 403.
- `TodoSourcesTests`: each kind; RM gets no `ActionKind` except "Bekijken"; `VacanciesExpiring` is absent when there is no end-date field (skip-guarded).
- bUnit: dashboard BM/RM/VM (KPI labels, RM hint + disabled primary with tooltip, VM has no vestigingen card, no upsell card), te-doen page grouping + filters, empty states.
- Rights matrix rows for `api/werkgever/dashboard`, `api/werkgever/te-doen` and `/werkgever/te-doen`.
- Playwright: BM 1440 dashboard shows Te doen + table; RM sees "Signalen in je regio"; 390 shows the 2×2 KPI's and the bottom nav.

## Success criteria
- The dashboard matches d1/d7/m1 in structure and copy (data real, or an honest "—"/empty state).
- Every number is scope-correct and server-checked.
- Te doen items link to real filtered pages.
- RM sees everything read-only; VM sees only their own vestiging.
- Page load does ≤ 2 API calls (dashboard + te-doen), each ≤ 300 ms on the seeded test data.

## Done → next
Push, open the PR, note its number. Continue with **`03-vacatures.md`**. If anything is red, stop and report.

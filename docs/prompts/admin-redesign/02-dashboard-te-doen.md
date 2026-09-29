# 02 · Dashboard + Te doen

> Read `00-README.md` first (§0, §IA). Builds on 01's shell and primitives.

| | |
|---|---|
| Branch | `cursor/admin-redesign-2`, created from `cursor/admin-redesign-1` |
| PR | ONE PR into `acceptatie`, title `feat(admin): dashboard with KPI cards, Te doen and system status`. Body starts with `Stacked on #<PR of 01> (cursor/admin-redesign-1)` |
| Mockups | `ad-d1-dashboard.png`, `ad-m1-dashboard.png` |

**Goal.** `/admin` answers "what needs me today?" in one screen: 5 headline KPIs, a Te doen list with one action per row, system status and the platform mode. The existing KPI board stays available below, nothing is lost.

## 02.1 Page `/admin` (`Pages/Admin/AdminDashboard.razor`, moved in 01)
Layout as `ad-d1` (desktop), `ad-m1` (mobile):
- **Header:** `h1` "Goedemorgen, {voornaam}" (time-of-day greeting in the user's culture: Goedemorgen/Goedemiddag/Goedenavond; `Europe/Amsterdam`), lead = date + "alles wat vandaag aandacht vraagt op één plek." Actions: period segment (reuse `period-tabs`: Vandaag · 7 dagen · 30 dagen · Kwartaal → existing `day/week/month/quarter` keys) + "Exporteren" only if an export for the metrics exists; otherwise omit (no dead buttons).
- **KPI row:** 5 × `AdminKpiCard` (label with icon, value, delta vs previous period with ↑/↓ **and** sign text, tiny sparkline from the existing metrics series if available, else none). Sources, **existing** `api/metrics/summary` keys only:
  1. "Actieve kandidaten": `users_active` (if that key counts all roles, add a candidate-only key `users_active_candidates` in the metrics service; don't compute in the UI).
  2. "Actieve werkgevers": `companies_employers`.
  3. "Vacatures live": `active_vacancies`.
  4. "Sollicitaties": `applications` (label adds the period, e.g. "(7 d)").
  5. "Omzet": paid token purchases in the period. If `TokenFinanceController` has no period total, add `GET api/admin/finance/summary?period=` (revenue incl./excl. btw, tokens sold, open at Mollie, btw buffer, open payouts) in a small `AdminFinanceSummaryService`; **06 reuses it**.
  - Each card links to its drilldown (existing `ListHrefFor` mapping, updated to new URLs in 01).
- **Te doen** card (left, wide): table with leading status icon, title (600) + muted subline, "Onderdeel" pill, "Sinds" (relative), one action button per row. Max 6 rows, "Alles bekijken →" to `/admin/te-doen`. Empty state: "Niets te doen. Mooi zo." + muted "We laten het hier zien zodra er iets is."
- **Systeemstatus** card (right): one row per integration from `api/integrations/health` (reuse the health DTO): dot + name + status text ("Operationeel" / "Traag · {ms} gem." / "Storing" / "Nog niet getest"). Link "Systeemlogs →". **No** background-job row (doesn't exist, README Scope).
- **Platform-modus** card (right): read-only rows from `PlatformSettingsCatalog` entries marked `ShowOnDashboard` (05 builds the catalog; in this file read `api/settings/platform-features` directly via a tiny `PlatformModeSummary` builder that 05 then replaces): AI-vacaturemoderatie, Werkgevers actief / Mijn Paspoort only if the fields exist, and "Tweestapsverificatie · Verplicht" (policy, D6). Link "Functies →".
- **Recente beheeracties**: **slot**, not rendered until 07.
- **Alle KPI's** (below, collapsed `details` "Alle KPI's en drilldown"): the existing `MetricsCategoryBoard` + `DrilldownGrid` + `DashboardRefreshButton`, unchanged. Remove the old lead "Modules via Settings of Financieel…" and inline styles in `AdminHomePanel`.
- Mobile (`ad-m1`): 2×2 KPI grid (Omzet hidden < 640 or as 4th), Te doen as tappable rows (≥ 56 px) with chevron, Systeemstatus list.

## 02.2 Te doen: one source for dashboard, page and sidebar counts
- **Core:** `IAdminTodoSource` (`string Key`, `Task<IReadOnlyList<AdminTodoItem>> GetAsync(ct)`) + `AdminTodoItem(Key, Severity (info/warn/danger), TitleKey, Subtitle, Area, SinceUtc, ActionLabelKey, Href, int Count)`. Aggregator `AdminTodoService` merges all sources, sorts danger → warn → info, then oldest first; caches 60 s (`IMemoryCache`), `Invalidate()` after a relevant admin write.
- **Sources** (each one small class, one query, **existing** data only):
  - `KvkFailedRegistrationsSource`: `CompanyRegistration`/`Company` with `KvkVerificationStatus.Failed` → "KvK-controle mislukt", subline company name, area Organisaties, href `/admin/organisaties/aanvragen?filter=kvk` (04 builds that page; until then href the companies list filtered, and 04 retargets).
  - `PendingTakeoversSource`: `EstablishmentTakeoverRequest.Status == Pending` → "Overnameverzoek vestiging", subline "{aanvrager org} → {vestiging}".
  - `ModerationFlaggedVacanciesSource`: `Vacancy.ContentModerationPassed == false` (not deleted) → "{n} vacatures gemarkeerd door moderatie", href `/admin/vacatures/moderatie`.
  - `PendingSalesManagerApplicationsSource`: open `SalesManagerApplication`s (existing `GET api/sales-managers/applications` semantics) → "Aanmelding salesmanager".
  - `OpenPayoutsSource`: salesmanager `SelfBillingInvoice`s that are not `Paid` (the ones `POST api/sales-managers/invoices/{id}/mark-paid` acts on) → "Facturen salesmanagers open", subline "{n} facturen · € {som}", href `/admin/financien/uitbetalingen?tab=uitbetalingen`. (Payouts themselves are self-service via `me/payouts/checkout`; admin has no approve step, so don't invent one.)
  - `NewFeedbackSource`: `PlatformFeedback` with `FeedbackStatus.New` → "{n} nieuwe feedbackmeldingen".
  - **Not built:** AVG requests, job failures (README Scope). Leave a comment slot.
- **API:** `GET api/admin/todo` (`RequireAdmin`) → items + `countsByNavKey` (e.g. `{ "todo": 7, "reg": 3, "mod": 2 }`).
- **Sidebar counts:** `AdminNavItem.CountKey` (01) now filled: Te doen = total, Aanvragen = KvK + takeovers, Moderatie = flagged, Uitbetalingen & btw = open payouts, Feedback = new. Collapsed groups show the sum as one pill (see `ad-d1`). Loaded once per circuit + on `AdminTodoChanged` event, **not** on every navigation.

## 02.3 Page `/admin/te-doen`
- `AdminDataTable` with the same columns as the card + filter chips by Onderdeel and Ernst; no pagination needed (< 100 rows); bulk actions none. Flip `IsAvailable` for Te doen in `AdminNav`.

## 02.4 Moderatie `/admin/vacatures/moderatie`
- Not a new list: the existing admin vacancy list with a server-side filter `moderation=flagged`. Add the optional `moderation` query parameter to `GET api/admin/vacancies` (filters `ContentModerationPassed == false`) and a thin page `VacanciesModerationPage.razor` that renders the existing vacancies section with that filter preset and the lead "Vacatures die de AI-moderatie heeft tegengehouden. Pas de tekst aan of keur handmatig goed." Only existing row actions. Retarget the legacy `/admin/moderation` redirect to this URL. Flip `IsAvailable`.

## Tests
- Each todo source: returns items only for its condition (in-memory DB), correct severity, count and href.
- `AdminTodoService`: ordering, caching, `Invalidate`.
- `GET api/admin/todo`: 403 non-admin; counts per nav key.
- `GET api/admin/vacancies?moderation=flagged` returns only flagged; without it, unchanged (existing tests green).
- bUnit: dashboard renders 5 KPI cards with deltas as text, Te doen max 6 rows + empty state, Systeemstatus rows from health, Platform-modus shows "Verplicht" for 2FA and hides absent flags; sidebar count pills incl. collapsed-group sum.
- Greeting helper: morning/afternoon/evening boundaries in `Europe/Amsterdam`.
- If added: `AdminFinanceSummaryService` totals for a fixed data set.
- Playwright (extend `AdminShellPlaywrightTests`): `/admin` at 1440 fits the KPI row + Te doen without horizontal scroll; at 390 the 2×2 grid shows.

## Success criteria
- `/admin` shows 5 KPIs, Te doen, Systeemstatus, Platform-modus; the old KPI board is still reachable on the same page.
- Te doen, its page and the sidebar counts come from one service.
- Moderatie is a real filtered list; no placeholder anywhere.
- No invented data sources (no AVG queue, no job health).
- Build + tests green; PR body complete.

## Done → next
Push, open the PR, note its number. Continue with **`03-gebruikers-rollen.md`**.

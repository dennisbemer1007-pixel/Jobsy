# Werkgever redesign (bedrijfsmanager · regiomanager · vestigingsmanager): Cursor run book

Cursor: **read this file completely**, then **execute the files below strictly in order**, one at a time. Each file is one PR.

**What this stack builds:** one shared set of employer pages under `/werkgever/…` for the bedrijfsmanager (BM, `EnterpriseManager`), regiomanager (RM, `RegionalManager`) and vestigingsmanager (VM, `BranchManager`). The same page renders per role:
- a **scope chip** in the top bar ("Alle vestigingen ▾" / "Regio Westland ▾" + "Alleen lezen" / fixed "Vestiging Naaldwijk")
- **role-scoped navigation** from **one** catalog
- **server-side authorization** per role, backed by a role × page test matrix

On top of that it adds: a real dashboard with Te doen, table-based Vacatures, a Sollicitaties pipeline, one invite flow, Tokens & facturen in one place, a split of Bedrijfsgegevens, and **Kandidaatinzichten as a paid unlock**.

## Order

| # | File | Branch | Branches from | PR into |
|---|---|---|---|---|
| 01 | `01-werkgever-shell.md`: `WerkgeverLayout` (grouped sidebar, top bar with scope chip, mobile bottom nav + Meer sheet), one nav catalog `WerkgeverNav.cs`, `EmployerScope`, new `/werkgever` URLs + 301s, shared UI primitives, role × page authorization matrix | `cursor/werkgever-redesign-1` | `origin/acceptatie` | `acceptatie` |
| 02 | `02-dashboard-te-doen.md`: dashboard (KPIs, Te doen / Signalen, wervingstrechter, vestigingen table) + Te doen page + sidebar counts | `cursor/werkgever-redesign-2` | `cursor/werkgever-redesign-1` | `acceptatie` |
| 03 | `03-vacatures.md`: vacancies table (status tabs, filters, inline approval, bulk with token cost), plain-Dutch actions, VM/RM variants | `cursor/werkgever-redesign-3` | `cursor/werkgever-redesign-2` | `acceptatie` |
| 04 | `04-sollicitaties.md`: pipeline per vacancy + candidate drawer that follows the privacy stages; list view; "Aangenomen" terminology | `cursor/werkgever-redesign-4` | `cursor/werkgever-redesign-3` | `acceptatie` |
| 05 | `05-organisatie-team.md`: Vestigingen & regio's tree, Team & rechten with ONE invite drawer, Bedrijfsgegevens split into Bedrijfsprofiel / Koppelingen / Wervingsmateriaal, Salaristabellen + Overnames re-homed | `cursor/werkgever-redesign-5` | `cursor/werkgever-redesign-4` | `acceptatie` |
| 06 | `06-tokens-facturen.md`: Saldo & kopen, Verbruik per vestiging (allocation), Mutaties, Facturen (moved out of Bedrijfsgegevens), VM token requests, Partnerprogramma split out, regional TokenControl duplicate removed | `cursor/werkgever-redesign-6` | `cursor/werkgever-redesign-5` | `acceptatie` |
| 07 | `07-kandidaatinzichten-premium.md`: paid unlock spending tokens (replaces "balance > 0"), server-side free/locked split, admin settings (price/duration/scope/on-off) + audit, RM/VM behaviour, mobile | `cursor/werkgever-redesign-7` | `cursor/werkgever-redesign-6` | `acceptatie` |
| 08 | `08-opruimen-termen-docs.md`: jargon + terminology guards, remaining hardcoded strings on touched pages, dead components removed, docs (ROUTES, roles matrix, test scenarios, CHANGELOG), Playwright role smoke | `cursor/werkgever-redesign-8` | `cursor/werkgever-redesign-7` | `acceptatie` |

If a file is too big for one reviewable PR (> ~1.500 changed lines excluding tests/migrations), split it into `a`/`b` at the seam the file names. The next file then branches from the **last** sub-branch (e.g. `cursor/werkgever-redesign-5b`).

## Pointer prompt (the only prompt needed; it runs 01 … 08)
```
Run the Werkgever redesign stack. First: git fetch origin && git show origin/docs/werkgever-redesign:docs/prompts/werkgever-redesign/00-README.md — read it completely.
Then read and execute each file in docs/prompts/werkgever-redesign/ on that branch strictly in the order the README's table lists (01 … 08; a/b splits where a file allows it), one file = one PR.
File 01 branches from origin/acceptatie; every later file branches from the previous file's branch (stacked). Each opens ONE PR into acceptatie whose body starts with "Stacked on #<prev PR>".
Before 01, run the dependency checks in the README's "Dependencies" section and follow the fallback it prescribes for each one; say in PR 01 which case applied.
Build and test after each file; if tests fail or a success criterion can't be met, push, open that PR as draft, stop and report — don't start the next file.
Never merge, never deploy, never use rule 123, never push to main or acceptatie, no force-pushes.
At the end report: file → branch → PR number → status, plus anything deferred.
```

## How to run
1. `git fetch origin`. Read this file, `.cursor/rules/design-system.mdc`, `docs/ROUTES.md`, `docs/security/roles-matrix.md`, `docs/adr/0004-roles-and-scope.md` and `docs/release-flow.md`.
2. Run the **Dependencies** checks below and note the outcome (it goes into PR 01).
3. For each file in the order above:
   1. Read the whole file.
   2. Create its branch from the "Branches from" column. File 01: `git checkout -b cursor/werkgever-redesign-1 origin/acceptatie`. Later files: `git checkout -b <branch> <previous branch>` with the previous branch pushed.
   3. Implement **only** that file's scope, plus the shared rules below.
   4. Run `dotnet build` and `dotnet test`, and the Playwright suites the file names if you can. Everything must be green and the file's **success criteria** must hold.
   5. Small, clear commits. Push (`git push -u origin <branch>`, only `cursor/*` branches). Open **ONE PR into `acceptatie`** with the title from the file. Body starts with `Stacked on #<prev PR> (<prev branch>)`, then the PR body items from §0.
   6. Note the PR number, go on to the next file.
4. **Stop and report** when tests fail and you can't fix them inside the file's scope, when a success criterion can't be met, or when the code contradicts this spec in a way you can't resolve safely. Push what you have, open that PR as **draft** with the failure described, don't continue.
5. At the end, report the table file → branch → PR number → status (green or draft/red), plus anything deferred.
6. **Never** merge, deploy, or use rule `123` (`.cursor/rules/shortcut-123.mdc`). **Never** push to `main` or `acceptatie`. No force-pushes.
   - Migrations: each file adds its own migration on top of the previous one. Never regenerate or edit a lower file's migration.
   - If `acceptatie` moves during the run: don't rebase. Only when a conflict blocks you, `git merge origin/acceptatie` into the current branch (a normal merge commit) and say so in the PR body.

---

## §0. Shared rules (every file)
- **Branches stack** (see above). **ONE PR per file, always into `acceptatie`.** Body starts with `Stacked on #<prev PR> (<prev branch>)`; file 01 says `Stacked on: none (first in the stack)`. The diff includes lower PRs until they merge; say which commits are this file's own.
- **Never** merge, deploy, or use rule `123`. **Never** push to `main` or `acceptatie`. No force-pushes.
- **Stop on red.** `dotnet build` + `dotnet test` after each file. If red and not fixable in scope: stop, push, draft PR with the failure, report.
- Code references are from `origin/acceptatie` @ `a611db40` (2026-09-29 18:29 CEST). Re-check line numbers before editing.
- **Mockups:** branch `docs/werkgever-redesign`, folder `docs/mockups/werkgever-redesign/`. Read with `git fetch origin docs/werkgever-redesign && git show origin/docs/werkgever-redesign:docs/mockups/werkgever-redesign/<file> > /tmp/<file>` and open `/tmp/<file>`. Don't commit mockups to code branches.
  - Desktop 1440×900: `bm-d1-dashboard.png`, `bm-d2-vacatures.png`, `bm-d3-sollicitaties-pipeline.png`, `bm-d4-vestigingen-team.png`, `bm-d5-tokens-facturen.png`, `bm-d6-kandidaatinzichten.png`, `bm-d7-dashboard-regiomanager.png` (RM variant), `bm-d8-vacatures-vestigingsmanager.png` (VM variant).
  - Mobile 390 wide: `bm-m1-dashboard.png`, `bm-m2-sollicitatie.png` (844 tall), `bm-m3-kandidaatinzichten-gratis.png` (1030 tall).
  - The mockups are a **layout and copy reference**. All names, companies, numbers, prices and IDs are **Voorbeelddata**; production shows real data or an honest empty state. The "Voorbeelddata" pill and the rotated "Voorbeelddata" stamps are mockup-only (locked blocks show neutral placeholder bars, see 07).
  - **Where the mockup and this spec differ, this spec wins.** Known differences:
    - **"Online tot" / "Verloopt binnenkort"** (d1, d2, d8): `Vacancy` has **no end date** at `a611db40` (only `PublishedAtUtc`, `ClosedAtUtc`, `HighlightedUntil`, `RequestedExtend`). Render the column, tab and Te doen item **only if** the code has a real vacancy end date when you build (check how `TokenSpendReason.Extend` is applied); otherwise leave them out and say so in the PR.
    - **"Direct beschikbaar"** KPI (d6, m3) doesn't exist in `InsightsKpis`; use the existing **"32+ uur per week"** (`Candidates32PlusHours`) instead.
    - **Export**: CSV of the aggregated numbers only (07); **pdf is deferred**. The premium check reads "Trends en export (CSV)".
    - **"Bekijk voorbeeldrapport"** (d6) and **"Automatisch aanvullen instellen"** (d5) are **not built** (deferred).
    - **"Interne notitie"** in the candidate drawer (d3) is built only if an employer note field already exists on `Application`; otherwise not rendered (deferred).
    - Token pack prices in d5 and the insights price in d6/m3 are examples: real packs come from `TokenPricing`, the insights price from settings (07, default 12 tokens).
    - The "Premium" nav marker is a **small gold lock icon** next to Kandidaatinzichten, shown only while that scope is locked.
- **Design system.** Follow `.cursor/rules/design-system.mdc` plus these rules, which win when they conflict:
  - **Density (D14):** desktop ≥ 1024 list pages (Vacatures, Sollicitaties list, Team & rechten, Tokens tables) may use the enterprise density: 44 px rows, `--text-sm` body, `--text-xs` meta, tabular numbers. Dashboard, drawers, forms and all mobile screens keep the calm spacing.
  - **Colours:** tokens only (`app.css :root`), no new hex values, no inline `style=""` in `.razor`. `--brand-deep` for the top bar. Status pills pair colour **and** a label. `--coral` only for the unread dot on the bell. **Gold** (`--gold`, `--gold-light`, `--gold-deep`, `--gold-ink`, `--gold-soft`) only for the Kandidaatinzichten premium pattern (07), exactly as the paid extended test results use it (`features/testresultaten.css`: `.test-result-lock-chip`, `.test-result-card-locked__*`, `.test-result-premium*`, `.btn-gold`). The gold gradient there is the one allowed gradient.
  - **Type:** weights 400/600 (700 only for the page `h1` and the premium block title); only the type scale; spacing from `--space-*`. If `--space-*`, `--text-*`, `--radius-pill` or `--z-*` are missing from `:root` when you start 01 (the admin stack adds them), file 01 adds them exactly as in the design system, syncs `app.min.css` and bumps its `?v=`.
  - **Layout:** breakpoints 640/900/1024 only; tap targets ≥ 44 px on < 1024; logical properties (RTL `ar`), chevrons flip under `[dir="rtl"]`; `prefers-reduced-motion` fallbacks for drawers/sheets.
  - **Calm UI:** one primary action per card/drawer; destructive actions are outline-danger, the confirming button inside the dialog is filled danger; no decorative emoji.
  - **Icons:** only in the sidebar, bottom nav, KPI cards, tabs, buttons and list leading icons. Never in `h1`–`h3`. New line icons go into `Navigation/NavIcons.cs`. No new icon system.
  - **Reuse:** `detail-card`, `status-pill`, `btn-compact` / `btn-compact--primary`, `data-table`, `RowActionsMenu`, `filter-bar`, `period-tabs`, `LobsyFriendlyDialog`, `PublishOptionsDialog`, `PushBomConfirmDialog`, `TokenTopUpDialog`, `MetricsCategoryBoard`, `PanelErrorBoundary`, `PageContentSkeleton`, `InsightsLockedBlock`, `EmployerTalentTabs`, `RaamflyerTools`.
- **Shared UI primitives (D17):** list pages use one set of primitives with the admin redesign. See Dependencies, check B, for which case applies. **Never create a second variant** of a table/drawer/tabs/KPI primitive.
- **Strings:** all new UI text through `@Culture["…"]` in **nl/en/pl/ro/ar**, in a new `Localization/UiStringsWerkgever.cs` (follow the `UiStringsMatch.MergeAll` pattern, register in `UiStrings.cs`). Dutch copy in this stack is final (zakelijk, kort, "je"). Hardcoded Dutch in every page you touch gets localized on the way (Users, Branches, Regions, Tokens, CompanyDetails, Takeovers, Vacancies, Applicants have many). `LocalizationParityReportTests`, `LocalizationTests` and `UiStringsCandidateInsightsTests` stay green.
- **Terminology (D11), nl final:**

  | Use | Instead of |
  |---|---|
  | Bedrijfsmanager | Enterprisemanager, Enterprise manager |
  | Regiomanager | Regional manager |
  | Vestigingsmanager | filiaalmanager, branchmanager, Branch manager |
  | Aangenomen | Gematcht (status `Hired`) |
  | Uitgenodigd | Contact opgenomen (status `EmployerContacting`) |
  | Uitlichten | Highlight |
  | Pushbericht naar kandidaten | PushBom |
  | Verlengen | Extend |
  | Publicatieaanvraag | goedkeuringsverzoek tokens |
  | Zichtbaarheid & kosten | token-opties popup |
  | Tokens toewijzen | Tokens uitgeven (allocate) |

  Code identifiers (`PushBom`, `Hired`, `TokenSpendReason.PushBom`, API routes) **stay**. Only UI text changes. 08 adds the guard test.
- **No duplicate logic.** Existing pages are **moved and wrapped**, not rewritten, unless the file says "redesign". Extract a page body into `Components/Werkgever/Sections/<Name>Section.razor` when it must live under a tab. **No new endpoints for data an existing endpoint already returns**; new endpoints only where a file says so.
- **Authorization (D4, D5, D6): server first, stricter never looser.**
  - Every `/werkgever` page carries an explicit `[Authorize(Roles = …)]` equal to its §R row (the matrix test in 01 enforces it).
  - Every mutating employer endpoint rejects `RegionalManager` (RM is read-only everywhere) and rejects a VM acting on a company that isn't one of their memberships (`ICompanyAuthorizationService.EnsureCanAccessCompanyAsync`).
  - The UI hiding or disabling a button is **never** the only guard.
  - Never loosen an existing attribute. The only intended tightening is D5 (VM can't buy tokens); list any other tightening in the PR.
- **Werkgevers actief (Dependencies, check A):** when the `EmployersEnabled` flag exists, every `/werkgever` page and every new employer endpoint carries `[RequiresFeature(PlatformFeature.Employers)]`. The 01 guard test enforces it.
- **Privacy (AVG):** candidate data follows the **existing** `LobsyCvAccessRules` stages everywhere, for BM, RM and VM alike (D4):
  - `Pending`: anonymous "Kandidaat #{kort-id}".
  - `Accepted` / `EmployerContacting`: name + Lobsy-cv (if the e-mail is verified).
  - `Hired`: contact details + city.

  Kandidaatinzichten stay aggregated with `KAnonymityThreshold = 10`. Never add identity or age filters (`CandidateInsightsController.ForbiddenIdentityParams`). CV downloads and PII views keep writing `PersonalDataAccessLog` as today.
- **Docs and guards to update when routes change:** `docs/ROUTES.md` (`RoutesDocFreshnessTests`), `Seo/PageSeoCatalog.cs` (private, non-indexable; `PageSeoTests`), `Help/PageHelpDocs.cs` (`PageHelpDocsTests`), `BlazorPageRoleAttributesTests`, `docs/security/roles-matrix.md` when authorization changes, `CHANGELOG.md`. Transactional e-mails that link to employer pages (`Jobsy.Core/Email/TransactionalEmails.cs` and templates) point to the new URLs.
- **CSS:** new `wwwroot/css/features/werkgever.css` (BEM block `wg-…`) for page-level styles, linked in `Components/App.razor` (normal list **and** `<noscript>`) with `?v=YYYYMMDD-werkgever`, added to `Jobsy.Tests/asset-versions.json` (`AssetVersionGuardTests`). Don't append to `app.css`. Shared primitives use their own stylesheet (D17).
- **Migrations:** `dotnet ef migrations add …` in `Jobsy.Infrastructure`, snapshot updated. `EfModelSnapshotTests`, `PendingModelChangesTests`, `EfMigrationDiscoveryTests` stay green.
- **Must NOT touch:**
  - candidate navigation and pages
  - admin pages, except the insights settings entries in 07
  - `features/questionnaire.css`, banenkaart CSS/JS, `app-core.js`, the cookie banner
  - Mollie/checkout flows (`MolliePaymentService`, `/tokens/checkout-return`, `/tokens/checkout-stub`, `/employer/onboarding-checkout` and their return URLs), webhooks
  - token **price values** and **pricing logic** (pages move; calculations don't). The one exception is the new `InsightsUnlock` spend reason in 07.
  - the MFA pages and `MfaEnforcementMiddleware`
  - Intermediary-only pages (`/intermediary`, `/intermediary/team`) beyond moving them into the shell (D1)
- **PR description:**
  - what changed and why
  - screenshots desktop 1440 **per role** (BM, RM, VM) and mobile 390 of each new or changed screen
  - the old → new URL list if routes changed
  - the role × page rows this PR added or changed
  - test list
  - "Out of scope / deferred"
- **Run** `dotnet build` and `dotnet test` (unit + bUnit). Run the Playwright suites you touched if you can; say so if you couldn't.

---

## §IA. Werkgever information architecture (the contract for all files)

Sidebar groups and items, in this order. **Label = page `h1` = breadcrumb** (one naming source: `WerkgeverNav.cs`, see 01). An item whose page doesn't exist yet or that the role may not see is **not rendered** (no "Nog niet beschikbaar" pages, no greyed items). Role columns: ● visible and full, ◐ visible read-only (region), ◯ visible scoped to own vestiging(en), — hidden **and** 403 on the server.

| Group | Item (nl label) | New URL | BM | RM | VM | Built in | Replaces (old → 301) |
|---|---|---|---|---|---|---|---|
| Overzicht | Dashboard | `/werkgever` | ● | ◐ | ◯ | 01 moves, 02 redesign | `/home` (employer roles only), `/branch`, `/regional` |
| | Te doen (RM: **Signalen**) | `/werkgever/te-doen` | ● | ◐ | ◯ | 02 | — |
| Werving | Vacatures | `/werkgever/vacatures` | ● | ◐ | ◯ | 01 moves, 03 redesign | `/employer/vacancies`, `/branch/vacancies` |
| | (no nav item) Nieuwe vacature | `/werkgever/vacatures/nieuw` | ● | — | ◯ | 01 moves | `/branch/vacancies/new` |
| | Sollicitaties | `/werkgever/sollicitaties` | ● | ◐ | ◯ | 01 moves, 04 redesign | `/branch/applicants` |
| | Talentpool | `/werkgever/talentpool` (tabs Zoeken · Contactverzoeken) | ● | ◐ | ◯ | 01 moves + tabs | `/employer/talent`, `/employer/talent-contacts` → `…?tab=contact` |
| | Kandidaatinzichten | `/werkgever/kandidaatinzichten` | ● | ◐ | ◯ | 01 moves, 07 premium | `/employer/kandidaatinzichten` |
| Organisatie (VM: **Mijn vestiging**) | Vestigingen & regio's | `/werkgever/organisatie/vestigingen` | ● | ◐ | — | 01 moves, 05 redesign | `/employer/branches`, `/regional/branches`, `/employer/regions` → `…?tab=regios`, `/employer/organization` |
| | Team & rechten | `/werkgever/organisatie/team` | ● | — | — | 01 moves, 05 redesign | `/employer/users` |
| | Bedrijfsprofiel (VM: **Vestigingsprofiel**) | `/werkgever/organisatie/profiel` (tabs Profiel · Contact & kanalen · Wervingsvoorkeuren · Cultuur) | ● | — | ◯ | 01 moves, 05 split | `/employer/company`, `/employer/culture` + `/branch/culture` → `…?tab=cultuur` |
| | Salaristabellen | `/werkgever/organisatie/salaristabellen` (+ `/{id}`) | ● | — | ◯ read-only | 01 moves | `/employer/salary-tables` (+ `/{id}`) |
| Tokens & facturen (RM: **Tokenverbruik** under "Mijn regio") | Saldo & kopen (VM: **Saldo & aanvragen**) | `/werkgever/tokens` | ● | ◐ | ◯ | 01 moves, 06 redesign | `/employer/tokens`, `/branch/tokens`, `/regional/tokens` |
| | Verbruik per vestiging | `/werkgever/tokens/verbruik` | ● | ◐ | — (own usage is on Saldo) | 06 | — |
| | Mutaties | `/werkgever/tokens/mutaties` | ● | ◐ | ◯ | 06 (was the Logging tab) | `/employer/tokens?tab=logging` |
| | Facturen | `/werkgever/tokens/facturen` | ● | — | — | 06 (moved out of Bedrijfsgegevens) | — |
| Meer | Koppelingen | `/werkgever/koppelingen` (tabs API · CSV-import) | ● | — | — | 05 | `/employer/csv-import` → `…?tab=csv` |
| | Overnames | `/werkgever/overnames` | ● | — | ◯ | 01 moves | `/employer/takeovers` |
| | Wervingsmateriaal | `/werkgever/wervingsmateriaal` | ● | ◐ | ◯ | 05 | `/employer/tokens?tab=tracking` |
| | Partnerprogramma | `/werkgever/partner` (+ `/partner/uitbetalen`) | ● | — | — | 06 | `/employer/sales`, `/employer/sales/payout-checkout` |

**Conditional items (one flag per item in `WerkgeverNav.cs`, not scattered `if`s):**
- **Koppelingen:** only when the company has an API key or CSV import is enabled (today's `CsvImport` rule).
- **Overnames:** only when the takeover inbox is non-empty (today's rule).
- **Partnerprogramma:** only for referred/partner companies (today's `WithSalesReferralNav` rule).
- **Regio's tab:** only with ≥ 2 vestigingen (today's rule).
- **Kandidaatinzichten:** only when `CandidateInsightsEnabled` (07).
- **"Mijn sollicitaties":** stays appended for users with candidate applications (today's `WithOptionalCandidateApplications`).

**Not in the nav:** Banenkaart. It becomes a sidebar footer link "Banenkaart bekijken" (opens `/`) (D16). `/employer/onboarding-checkout`, `/tokens/checkout-return` and `/tokens/checkout-stub` **keep their URLs** (payment return URLs).

**Intermediary (D1):** uses the same shell and pages. Its catalog lists today's items mapped to the new URLs (Dashboard, Vacatures, Talentpool, Klanten `/intermediary`, Team `/intermediary/team`, Tokens), with today's rights. No other intermediary changes in this stack.

**Mobile (< 1024) bottom nav (D15), 5 items; everything else is in the "Meer" sheet:**
- **BM:** Overzicht · Vacatures · Sollicitaties · Tokens · Meer
- **RM:** Overzicht · Vacatures · Sollicitaties · Inzichten · Meer
- **VM:** Overzicht · Vacatures · Sollicitaties · Tokens · Meer

---

## §R. Rights matrix (server-side contract; 01 encodes it, every file keeps it true)

| Page / action | Bedrijfsmanager | Regiomanager | Vestigingsmanager |
|---|---|---|---|
| Scope | all vestigingen of the organisation | vestigingen in own region(s) | own vestiging(en) (memberships) |
| Dashboard, Te doen | full, actionable | read-only ("Signalen", links only) | own scope, actionable |
| Vacatures: view | all | region | own |
| Vacatures: create/edit/publish/verlengen/uitlichten/pushbericht/(de)activeren/dupliceren | yes | **no** | own vestiging; publishing above own balance becomes a **publicatieaanvraag** (existing `PendingApproval` path) |
| Publicatieaanvraag goedkeuren (`approve-publish`) | **yes (only BM)** | no | no |
| Sollicitaties: view (privacy stages as today) | all | region, **same stages as BM** (D4) | own |
| Sollicitaties: accepteren/afwijzen/uitnodigen/aannemen, cv-download | yes | view + cv-download per stage; **no actions** | own |
| Talentpool: search / contact (1 token) | yes / yes | search / **no** | own / from own balance |
| Kandidaatinzichten: free part | yes | yes (region) | yes (own) |
| Kandidaatinzichten: unlock | yes (company-wide or per vestiging) | **no**: "Vraag je bedrijfsmanager" (text) | only if scope = per vestiging **and** own wallet; otherwise "Vraag aan bedrijfsmanager" (request) |
| Vestigingen & regio's | full (KvK add, regions CRUD) | read-only, own region | hidden (403) |
| Team & rechten, uitnodigen | **only BM** (all roles) | hidden (403) | hidden (403) (D6) |
| Bedrijfsprofiel / Vestigingsprofiel | organisation + all vestigingen | hidden (403) | own vestiging profile |
| Koppelingen (API, CSV) | yes | hidden | hidden |
| Salaristabellen | full | hidden | read-only |
| Overnames | yes | hidden | own vestiging |
| Wervingsmateriaal | yes | download (read-only) | own vestiging |
| Tokens: saldo, mutaties | central + all vestigingen | region, read-only | own balance |
| Tokens kopen | yes | **no** | **no** (D5); "Tokens aanvragen" → Te doen item for BM |
| Tokens toewijzen (`allocate`) | yes | no | no |
| Facturen | yes | hidden | hidden (D5) |
| Partnerprogramma | if partner | hidden | hidden |

The matrix test (01) is data-driven from **one** table in `Jobsy.Tests/Werkgever/WerkgeverRightsMatrix.cs`: page × role → allowed/forbidden, plus mutating endpoint × role → expected status. Each later file adds its rows.

---

## Decisions (defaults applied; Dennis can override any of them)
- **D1.** One shared page set under `/werkgever/…` for BM, RM and VM (and Intermediary with its current items). One nav catalog `Navigation/WerkgeverNav.cs` with role-scoped items. Employer roles get `WerkgeverLayout` (desktop sidebar + top bar, mobile bottom nav from the same catalog). `RoleNavCatalog.Enterprise/Regional/Branch/Intermediary` become empty, and `ForUser` returns them for those roles, so the old `BottomNav` renders nothing there. *(Dennis, 29-09)*
- **D2.** Dutch URLs under `/werkgever/{groep}/{item}`. Every old `/employer`, `/branch`, `/regional` URL answers **301** to its new URL (query kept, `tab=` added where §IA says so) for at least one release. `/home` 301s to `/werkgever` **for employer roles only**; candidates and others are unchanged, and admins go to `/admin` if the admin stack landed. Internal links all point to the new URLs; a test forbids old hrefs.
- **D3.** Scope chip in the top bar, backed by `EmployerScope`:
  - **BM:** switcher Alle vestigingen / regio / vestiging.
  - **RM:** own region(s), with a switcher only if > 1 region or to narrow to one vestiging, plus an "Alleen lezen" pill.
  - **VM:** fixed chip with the vestiging name; a switcher only when they have > 1 membership.
  - The chip only **narrows**: every endpoint re-checks that the requested ids are a subset of `GetAccessibleCompanyIdsAsync`. Persisted per circuit + `?scope=` deep link.
- **D4.** Regiomanager is **read-only everywhere**, with the **same privacy stages as the bedrijfsmanager** (name + cv after accepteren, contact after aanname). Read-only UI = actions hidden, and for the one or two primary actions a page shows (e.g. "Vacature plaatsen"), a disabled button with a lock + tooltip "Dit doet de vestigings- of bedrijfsmanager", plus one `wg-readonly-hint` line under the header. *(Dennis default)*
- **D5.** Vestigingsmanager has full rights **within own vestiging(en)**. He **cannot buy tokens**: `BranchManager` is removed from `JobsyRoles.TokenPurchaseRoles` / `CanPurchaseTokens`. He receives tokens from the BM (`allocate`), can send a **tokenaanvraag** (06), and sees **no invoices**. Registration always creates a bedrijfsmanager (`CompanyRegistrationService.ResolveRegistrationRole`), so every organisation keeps a buyer. *(Dennis default)*
- **D6.** **Only the bedrijfsmanager invites** (all roles), through **one** invite drawer (05). The VM/RM invite UI is hidden. The server is already strict (`CompanyUsersController` = EnterpriseManager/Intermediary/Admin); keep it. *(Dennis default)*
- **D7.** Publication approval stays with the bedrijfsmanager (existing `approve-publish` + `PendingApproval` path for VMs whose tokens are managed by the enterprise).
- **D8.** **Kandidaatinzichten is a paid unlock that spends tokens** (07). It replaces the "balance > 0" rule in `CandidateInsightsAccess`:
  - The free/locked split is exactly the mockup's (07.2).
  - Locked data **never reaches the browser**: the server omits it.
  - Unlock is **idempotent**, shows the expiry date, and renewal is possible from 14 days before expiry (it stacks on the current expiry).
  - The expiry reminder e-mail is **deferred** (slot only).
- **D9.** Insights settings are in **admin** (07):
  - price = a `TokenSpendCost` row for the new `TokenSpendReason.InsightsUnlock` (default **12**)
  - duration `CandidateInsightsUnlockDays` (default **90**, 7–365)
  - scope `CandidateInsightsUnlockPerBranch` (default **false** = company-wide)
  - feature on/off `CandidateInsightsEnabled` (default **true**)
  - Placed as in the admin redesign: price under Prijzen & pakketten, the switches in the Functies catalog, when those exist; otherwise in today's `/admin/settings` sections.
- **D10.** Insights settings changes are **audit-logged**:
  - With `IAdminAuditLog` (admin-redesign 07): `settings.platform.update` / `settings.pricing.update`.
  - Otherwise: an interim structured `PlatformLog` row. Not `PersonalDataAccessLog`, which is for personal-data access only.
  - The unlock itself is recorded by the token ledger row + the `CandidateInsightsUnlock` row.
- **D11.** Terminology table in §0. UI text only.
- **D12.** Invoices live under **Tokens & facturen › Facturen** (moved out of Bedrijfsgegevens; same endpoint, same pdf).
- **D13.** Bedrijfsgegevens (`CompanyDetails.razor`, 1.173 lines) is split:
  - **Bedrijfsprofiel**: identity, contact preference, channels, recruitment preferences, culture.
  - **Koppelingen**: API link/key/endpoint + CSV import.
  - **Wervingsmateriaal**: raamflyer + tracking code, which also replaces the Tokens "Trackingcode & flyer" tab.
  - **Facturen**: to Tokens.
- **D14.** Enterprise density allowed on werkgever desktop list pages (§0).
- **D15.** Mobile: 5-item bottom nav per role (§IA) + "Meer" sheet; the desktop sidebar isn't shown < 1024.
- **D16.** Banenkaart leaves the employer nav; sidebar footer link "Banenkaart bekijken".
- **D17.** Shared UI primitives live in `Components/Ui/Enterprise/` (`Ent*` names) and are used by admin and werkgever alike. See Dependencies, check B.
- **D18.** `CandidateInsightsEnabled = false`: the Kandidaatinzichten nav item is hidden and the page + API answer like a paused feature (404 `feature_disabled` via `[RequiresFeature]` when §F exists, else an equivalent check). Existing unlocks keep their expiry. No refunds.
- **D19.** Unlock scope when per vestiging: "Alle vestigingen" shows full data only when **every** selected vestiging is covered; otherwise it shows the free version + "{n} van {m} vestigingen ontgrendeld".
- **D20.** Sidebar shows a small gold lock next to Kandidaatinzichten **only while** the current scope is locked. The dashboard shows **no** upsell card.

## Dependencies (check before 01; say in PR 01 which case applied)
- **A. Werkgevers actief / §F flags** (`docs/mijn-paspoort` file 01, branch `cursor/werkgevers-actief`, unmerged at `a611db40`). Check: `git grep -n "class RequiresFeatureAttribute" origin/acceptatie -- Jobsy.Core`.
  - **Present:** gate every `/werkgever` page and new employer endpoint with `[RequiresFeature(PlatformFeature.Employers)]`. 07 adds `PlatformFeature.CandidateInsights` to the enum (D18).
  - **Absent:** build without the attribute. Add the **reflection guard test** in 01, which passes vacuously when the attribute type doesn't exist and enforces the gate once it does. Whichever stack lands second merges `acceptatie` normally and adds the attributes (and moves the paused-page mapping from the old routes to the new ones in `docs/feature-flags.md`). Werkgevers actief OFF must hide **all** of `/werkgever` and the employer nav.
  - It also refactors `RoleNavCatalog` (`ForUser(user, flags)`). If present, keep that signature; the employer catalogs just become empty.
- **B. Admin redesign UI primitives** (`docs/admin-redesign` file 01, not yet built at `a611db40`). Check: `git ls-tree -r --name-only origin/acceptatie -- Jobsy.Web/Components/Ui/Enterprise Jobsy.Web/Components/Admin/Ui`.
  - **`Components/Ui/Enterprise/` exists:** reuse it.
  - **Only `Components/Admin/Ui/Admin*` exists:** in 01, as its own first commit, move them to `Components/Ui/Enterprise/` with `Ent*` names (`git mv`, update admin usages, move their CSS from `features/admin.css` to `features/enterprise-ui.css`, no behaviour change). Admin keeps working (its tests stay green).
  - **Neither exists:** build them in 01 under `Components/Ui/Enterprise/`:
    - `EntDataTable`, `EntFilterBar`, `EntBulkBar`, `EntPager`, `EntTabs` (URL-synced `?tab=`), `EntKpiCard`, `EntDrawer` (right 460 px / full-screen sheet < 900, focus trap, Esc, returns focus), `EntImpactNote`, `EntScopeChip`
    - CSS `features/enterprise-ui.css`, block `ent-…`, asset-versioned
    - Say in PR 01 that the admin stack must consume these instead of building `Admin*` copies.
  - Either way: **one** set.
  - The shared layout pieces (`CircuitErrorBoundary`, extracted from `MainLayout`) follow the same rule: reuse if admin 01 extracted them, else extract here.
- **C. Admin redesign 05/06/07** (settings catalog, pricing page, audit log). Used by 07 only; each has a fallback in 07.
- **D. Mijn Paspoort stack** (`cursor/mijn-paspoort-*`, unmerged). It touches `RoleNavCatalog.cs`, `PlatformFeatureSettings`, `SettingsAdmin.razor`. No functional overlap with employer pages; whichever lands second merges `acceptatie` normally.
- **Recommended landing order:** Werkgevers actief (paspoort 01) → admin redesign 01 → this stack. Not a hard requirement: every case above has a fallback. Never branch from an unmerged branch of another stack.

# Admin redesign (Beheer): Cursor run book

Cursor: **read this file completely**, then **execute the files below strictly in order**, one at a time. Each file is one PR.

## Order

| # | File | Branch | Branches from | PR into |
|---|---|---|---|---|
| 01 | `01-admin-shell.md`: own admin layout (grouped sidebar, top bar with global search Ctrl K, environment badge, breadcrumbs, mobile drawer), new Dutch URLs + redirects from the old ones, naming, placeholder pages removed | `cursor/admin-redesign-1` | `origin/acceptatie` | `acceptatie` |
| 02 | `02-dashboard-te-doen.md`: dashboard on `/admin` (5 KPI cards, Te doen, Systeemstatus, Platform-modus) + full Te doen page + sidebar counts | `cursor/admin-redesign-2` | `cursor/admin-redesign-1` | `acceptatie` |
| 03 | `03-gebruikers-rollen.md`: user list (role tabs, filters, bulk actions, masked PII), detail drawer (sessions, 2FA reset, support access), Rollen & rechten, Sales & ambassadeurs, Kandidaten | `cursor/admin-redesign-3` | `cursor/admin-redesign-2` | `acceptatie` |
| 04 | `04-organisaties.md`: companies/branches tree + detail panel, Regio's & domeinen, Aanvragen (KvK checks + takeovers) | `cursor/admin-redesign-4` | `cursor/admin-redesign-3` | `acceptatie` |
| 05 | `05-platforminstellingen.md`: Functies (grouped switches from one catalog), Algemeen, Integraties & API; pricing moves out | `cursor/admin-redesign-5` | `cursor/admin-redesign-4` | `acceptatie` |
| 06 | `06-financien.md`: Omzet & transacties, Prijzen & pakketten (one place), Goodwill & tokens (one place), Uitbetalingen & btw | `cursor/admin-redesign-6` | `cursor/admin-redesign-5` | `acceptatie` |
| 07 | `07-beveiliging-audit.md`: new admin audit log (table + writes), Gegevensinzage, 2FA & sessies, Privacy & AVG, Systeemlogs; fills the audit slots from 02/03/05 | `cursor/admin-redesign-7` | `cursor/admin-redesign-6` | `acceptatie` |

If a file is too big for one reviewable PR (> ~1.500 changed lines excluding tests/migrations), split it into `a`/`b` at the seams the file names. The next file then branches from the **last** sub-branch (e.g. `cursor/admin-redesign-3b`).

## Pointer prompt (the only prompt needed; it runs 01 … 07)
```
Run the Admin redesign stack. First: git fetch origin && git show origin/docs/admin-redesign:docs/prompts/admin-redesign/00-README.md — read it completely.
Then read and execute each file in docs/prompts/admin-redesign/ on that branch strictly in the order the README's table lists (01 … 07; a/b splits where a file allows it), one file = one PR.
File 01 branches from origin/acceptatie; every later file branches from the previous file's branch (stacked). Each opens ONE PR into acceptatie whose body starts with "Stacked on #<prev PR>".
Build and test after each file; if tests fail or a success criterion can't be met, push, open that PR as draft, stop and report — don't start the next file.
Never merge, never deploy, never use rule 123, never push to main or acceptatie, no force-pushes.
At the end report: file → branch → PR number → status, plus anything deferred.
```

## How to run
1. `git fetch origin`. Read this file, `.cursor/rules/design-system.mdc`, `docs/ROUTES.md`, `docs/security/roles-matrix.md` and `docs/adr/0005-mfa-local-only.md`.
2. For each file in the order above:
   1. Read the whole file.
   2. Create its branch from the "Branches from" column. File 01: `git checkout -b cursor/admin-redesign-1 origin/acceptatie`. Later files: `git checkout -b <branch> <previous branch>` with the previous branch pushed.
   3. Implement **only** that file's scope, plus the shared rules below.
   4. Run `dotnet build` and `dotnet test`, and the Playwright suites the file names if you can. Everything must be green and the file's **success criteria** must hold.
   5. Small, clear commits. Push (`git push -u origin <branch>`, only `cursor/*` branches). Open **ONE PR into `acceptatie`** with the title from the file. Body starts with `Stacked on #<prev PR> (<prev branch>)`, then the PR body items from §0.
   6. Note the PR number, go on to the next file.
3. **Stop and report** when tests fail and you can't fix them inside the file's scope, when a success criterion can't be met, or when the code contradicts this spec in a way you can't resolve safely. Push what you have, open that PR as **draft** with the failure described, don't continue.
4. At the end, report the table file → branch → PR number → status (green or draft/red), plus anything deferred.
5. **Never** merge, deploy, or use rule `123`. **Never** push to `main` or `acceptatie`. No force-pushes.
   - Migrations: each file adds its own migration on top of the previous one. Never regenerate or edit a lower file's migration.
   - If `acceptatie` moves during the run: don't rebase. Only when a conflict blocks you, `git merge origin/acceptatie` into the current branch (a normal merge commit) and say so in the PR body.

---

## §0. Shared rules (every file)
- **Branches stack** (see above). **ONE PR per file, always into `acceptatie`.** Body starts with `Stacked on #<prev PR> (<prev branch>)`; file 01 says `Stacked on: none (first in the stack)`. The diff includes lower PRs until they merge; say which commits are this file's own.
- **Never** merge, deploy, or use rule `123` (`.cursor/rules/shortcut-123.mdc`). **Never** push to `main` or `acceptatie`. No force-pushes. See `docs/release-flow.md`.
- **Stop on red.** `dotnet build` + `dotnet test` after each file. If red and not fixable in scope: stop, push, draft PR with the failure, report.
- Code references are from `origin/acceptatie` @ `a611db40` (2026-09-29 18:29 CEST), which **includes the merged `fix/2fa-enrollment` work** (`POST api/admin/users/{userId}/mfa/reset`, MFA status in the user list, reset dialog in `UsersAdmin.razor`). Re-check line numbers before editing.
- **Mockups:** branch `docs/admin-redesign`, folder `docs/mockups/admin-redesign/`. Read with `git fetch origin docs/admin-redesign && git show origin/docs/admin-redesign:docs/mockups/admin-redesign/<file> > /tmp/<file>` and open `/tmp/<file>`. Don't commit mockups to code branches.
  - Desktop 1440×900: `ad-d1-dashboard.png`, `ad-d2-gebruikers-2fa.png`, `ad-d3-organisaties.png`, `ad-d4-platforminstellingen.png` (1440×1330, full settings page), `ad-d5-beveiliging-audit.png`, `ad-d6-financien.png`.
  - Mobile 390×844: `ad-m1-dashboard.png`, `ad-m2-2fa-resetten.png`.
  - The mockups are a **layout and copy reference**. All names, numbers, companies, invoices and IDs are **Voorbeelddata**; production shows real data or an honest empty state. The "Voorbeelddata" pill is mockup-only.
  - **Where the mockup and this spec differ, this spec wins.** Known differences: the mockup shows "Tweestapsverificatie" as a switch (it is policy, not a switch, see D6); it shows "Postmark" (the mailer is Resend: show the configured provider name); it shows an AVG request queue and background-job health (neither exists yet, see "Scope").
- **Design system.** Follow `.cursor/rules/design-system.mdc` plus these rules, which win when they conflict:
  - **Enterprise density is allowed in admin only** (desktop ≥ 1024): table rows 44 px, `--text-sm` body, `--text-xs` meta, tabular numbers. Candidate/employer surfaces keep the calm spacing.
  - **Colours:** tokens only (`app.css :root`), no new hex values, no inline `style=""` in `.razor`, no gradients. `--brand-deep` for the admin top bar; status pills pair colour **and** a label; `--coral` only for the unread dot on the bell.
  - **Environment badge colours:** Acceptatie = `--warn` / `--warn-soft`, Productie = `--danger` / `--danger-soft`, Lokaal = `--accent-soft` / `--brand`.
  - **Type:** weights 400/600 (700 only for the page `h1`), only the type scale; spacing from `--space-*`. If `--space-*`, `--text-*`, `--radius-pill` or `--z-*` are missing from `:root`, file 01 adds them exactly as in the design system, syncs `app.min.css` and bumps its `?v=`.
  - **Layout:** breakpoints 640/900/1024 only; tap targets ≥ 44 px on < 1024; logical properties (RTL `ar`), chevrons flip under `[dir="rtl"]`; `prefers-reduced-motion` fallbacks for the drawer/sheet.
  - **Calm UI:** one primary action per card/drawer; destructive actions are outline-danger, and the confirming button inside the dialog is the filled danger one; no decorative emoji.
  - **Icons:** only in the sidebar, KPI cards, tabs, buttons and list leading icons. Never in `h1`–`h3`. New line icons go into `Navigation/NavIcons.cs`. No new icon system.
  - **Reuse:** `detail-card`, `status-pill`, `btn-compact` / `btn-compact--primary`, `data-table`, `RowActionsMenu`, `filter-bar`, `period-tabs`, `LobsyFriendlyDialog`, `SupportAccessDialog`, `GrantTokensDialog`, `IntegrationSettingsTile`, `MetricsCategoryBoard`, `DrilldownGrid`, `DashboardRefreshButton`, `PanelErrorBoundary`, `PageContentSkeleton`, `CnameSetupHelpPanel`.
- **New shared admin UI primitives** (built in 01, used by all later files; don't create variants):
  - `Components/Admin/Ui/AdminDataTable` (header row, checkbox column, row actions slot, empty/loading states, `aria-sort`), `AdminFilterBar` (search input + filter chips), `AdminBulkBar`, `AdminPager`, `AdminTabs` (underline tabs, URL-synced `?tab=`), `AdminKpiCard`, `AdminDrawer` (right, 460 px desktop / full-screen sheet < 900, focus trap, Esc closes, returns focus), `AdminImpactNote` (`warn`/`danger`), `AdminToggleRow` (title, description, control, meta line, optional impact note).
  - CSS: new `wwwroot/css/features/admin.css`, BEM block `admin-…`. Link it in `Components/App.razor` (normal list **and** `<noscript>`) with `?v=YYYYMMDD-admin`, add to `Jobsy.Tests/asset-versions.json` (`AssetVersionGuardTests`). Don't append to `app.css`. Move no existing styles that non-admin pages use.
- **Strings:** all new UI text through `@Culture["…"]` in **nl/en/pl/ro/ar**, in a new `Localization/UiStringsAdmin.cs` (follow the `UiStringsMatch.MergeAll` pattern, register in `UiStrings.cs`). Dutch copy in this stack is final (zakelijk, kort, "je"). Hardcoded Dutch in the pages you touch gets localized on the way. `LocalizationParityReportTests` and `LocalizationTests` stay green.
- **No duplicate logic.** Existing pages are **moved and wrapped**, not rewritten: extract a page's body into a child component (`Components/Admin/Sections/…`) when it needs to live under a new tab, and make the old route redirect. **No new endpoints for data an existing endpoint already returns**; new endpoints only where the file says so.
- **Authorization unchanged or stricter.** Every admin page keeps `[Authorize(Roles = "Admin")]`; every new endpoint lives under `api/admin/…` with `[Authorize(Policy = JobsyPolicies.RequireAdmin)]`. Never loosen an existing attribute. Sensitive writes (2FA reset, support access, role change) keep their server-side checks; the UI hiding a button is never the only guard.
- **Privacy (AVG):** admin sees **masked** personal data by default everywhere (reuse `PersonalDataMasker`); full data only through an active support-access grant (existing flow). New list/search endpoints that touch users log a `PersonalDataAccessLog` entry like `GET api/admin/users` does. Never store raw search queries, raw IPs or unmasked names in new tables.
- **Docs and guards to update when routes change:** `docs/ROUTES.md` (`RoutesDocFreshnessTests`), `Seo/PageSeoCatalog.cs` (private, non-indexable; `PageSeoTests`), `Help/PageHelpDocs.cs` (`PageHelpDocsTests`), `BlazorPageRoleAttributesTests` (new pages carry `[Authorize(Roles = "Admin")]`), `docs/security/roles-matrix.md` when authorization changes, `CHANGELOG.md`.
- **Migrations:** `dotnet ef migrations add …` in `Jobsy.Infrastructure`, snapshot updated. `EfModelSnapshotTests`, `PendingModelChangesTests`, `EfMigrationDiscoveryTests` stay green.
- **Must NOT touch:** candidate and employer navigation (`RoleNavCatalog` non-admin catalogs stay byte-identical), `features/questionnaire.css`, banenkaart CSS/JS, `app-core.js`, the cookie banner, Mollie/checkout flows (`MolliePaymentService`, checkout pages, webhooks), price **values** and pricing **logic** (pages move; calculations don't), the MFA login/enrollment pages (`/account/mfa*`), `MfaEnforcementMiddleware`.
- **PR description:** what changed and why; screenshots desktop 1440 and mobile 390 of each new/changed admin screen; the old → new URL list if routes changed; test list; "Out of scope / deferred".
- **Run** `dotnet build` and `dotnet test` (unit + bUnit). Run the Playwright suites you touched if you can; say so if you couldn't.

---

## §IA. Admin information architecture (the contract for all files)

Sidebar groups and items, in this order. **Label = page `h1` = breadcrumb** (one naming source: `AdminNav.cs`, see 01). An item whose page doesn't exist yet is **not rendered** (no "Nog niet beschikbaar" pages).

| Group | Item (NL label) | New URL | Built in | Replaces (old URL → redirect) |
|---|---|---|---|---|
| Overzicht | Dashboard | `/admin` | 02 (01 moves the current panel) | `/home` (admin only), `/admin/cockpit` |
| | Te doen | `/admin/te-doen` | 02 | — |
| | Feedback | `/admin/feedback` | 01 (moved) | same URL |
| Gebruikers & rollen | Alle gebruikers | `/admin/gebruikers` | 01 moved, 03 redesign | `/admin/users` |
| | Rollen & rechten | `/admin/gebruikers/rollen` | 03 | — |
| | Sales & ambassadeurs | `/admin/gebruikers/sales` (tabs Salesmanagers · Ambassadeurs) | 01 moved, 03 tabs | `/admin/sales-managers`, `/admin/ambassadeurs` → `…/sales?tab=ambassadeurs` |
| Organisaties | Bedrijven & vestigingen | `/admin/organisaties` | 01 moved, 04 redesign | `/admin/companies` |
| | Regio's & domeinen | `/admin/organisaties/regios` | 01 moved, 04 | `/admin/cnames` |
| | Aanvragen | `/admin/organisaties/aanvragen` | 04 | — |
| Kandidaten & tests | Kandidaten | `/admin/kandidaten` (user list preset to role Kandidaat) | 03 | — |
| Vacatures & matching | Vacatures | `/admin/vacatures` | 01 moved | `/admin/vacancies` |
| | ATS-import | `/admin/vacatures/ats` | 01 moved | `/admin/ats-vacancies` |
| | Moderatie | `/admin/vacatures/moderatie` (filtered vacancy list) | 02 | `/admin/moderation` (placeholder removed) |
| | Categorieën & salaris | `/admin/vacatures/categorieen` (tabs Categorieën · Salaris & WML) | 01 moved + tabs | `/admin/vacancy-categories`, `/admin/wages` → `…?tab=salaris` |
| Financiën | Omzet & transacties | `/admin/financien` | 01 moved, 06 redesign | `/admin/finance` |
| | Prijzen & pakketten | `/admin/financien/prijzen` | 01 moved (the `/admin/sales` page), 05 adds the pricing from settings as tabs, 06 finishes | `/admin/sales` → `…/prijzen?tab=sales` from 05 on |
| | Goodwill & tokens | `/admin/financien/goodwill` | 01 moved, 06 merge | `/admin/tokens` |
| | Uitbetalingen & btw | `/admin/financien/uitbetalingen` | 01 moved, 06 | `/admin/token-finance` |
| Content & opleidingen | Pagina's & flyer | `/admin/content/paginas` (tabs Wie zijn wij · Werkgeversflyer) | 01 moved + tabs | `/admin/about`, `/admin/marketing-flyer` → `…?tab=flyer` |
| | Opleidingen | `/admin/content/opleidingen` | 01 moved | `/admin/training` |
| | Stamgegevens | `/admin/content/stamgegevens` (tabs Stamgegevens · Exclusiviteit stages) | 01 moved + tabs | `/admin/masterdata`, `/admin/exclusivity` → `…?tab=exclusiviteit` |
| | E-mails & meldingen | `/admin/content/emails` | 01 moved | `/admin/mail-test`, `/admin/notifications` (placeholder removed) |
| Platforminstellingen | Functies | `/admin/instellingen` | 05 | `/admin/settings` |
| | Algemeen | `/admin/instellingen/algemeen` | 05 | `/admin/company` |
| | Integraties & API | `/admin/instellingen/integraties` (tabs Integraties · API-sleutels) | 01 moved + tabs, 05 | `/admin/integrations`, `/admin/api-keys` → `…?tab=api` |
| Beveiliging & audit | Auditlog | `/admin/beveiliging` | 07 | — |
| | Gegevensinzage | `/admin/beveiliging/gegevensinzage` | 01 moved, 07 | `/admin/personal-data-access-log` |
| | 2FA & sessies | `/admin/beveiliging/2fa` | 07 | — |
| | Privacy & AVG | `/admin/beveiliging/privacy` | 07 | — |
| | Systeemlogs | `/admin/beveiliging/systeemlogs` | 01 moved | `/admin/logging` |

Until 07 lands, `Beveiliging & audit` has only Gegevensinzage and Systeemlogs; until 02 lands, `Overzicht` has Dashboard (current panel) and Feedback, etc. That is the "not rendered until it exists" rule, driven by one flag per item in `AdminNav.cs` (`IsAvailable`), not by scattered `if`s.

**Banenkaart** is not in the admin nav (D1). Admins can still open `/` directly and via the "Naar Lobsy" link in the account menu.

---

## Scope: what doesn't exist yet
| Item | Decision |
|---|---|
| AVG request queue (verwijder-/inzageverzoeken) | **Doesn't exist**: `api/privacy/delete-account` is self-service and immediate. 07 shows performed deletions/anonymisations from the audit log and the retention job; no queue, no "termijn" counter. Deferred. |
| Background-job health ("11/12 taken") | **Doesn't exist.** 02 shows integration health only (`api/integrations/health`). A job heartbeat registry is deferred. |
| Tests & normen admin page | **Doesn't exist.** Not rendered in the sidebar (slot in `AdminNav.cs`). Deferred. |
| Werkgevers actief / Mijn Paspoort switches | Live on unmerged branches (`cursor/werkgevers-actief`, `cursor/mijn-paspoort-1`). 05 includes them **only if** `EmployersEnabled` / `CandidatePassportEnabled` exist on the branch you build on; otherwise their catalog entries stay commented slots (D7). |
| Admin action audit trail | **New in 07.** Before 07, the 2FA reset is logged only in `PersonalDataAccessLog` (as today). |
| Four-eyes (second admin) approval | **Not built** (D3). |

## Decisions (defaults applied; Dennis can override any of them)
- **D1.** Admin gets its own desktop layout (`AdminLayout`) with the grouped sidebar; the 7-item admin bottom nav is removed (`RoleNavCatalog.Admin` becomes empty) and Banenkaart leaves the admin menu. Candidate and employer nav stay exactly as they are. *(Dennis, 29-09)*
- **D2.** Sales & ambassadeurs live under **Gebruikers & rollen**; their payouts stay under Financiën › Uitbetalingen & btw, with a cross-link. *(Dennis, 29-09)*
- **D3.** 2FA reset = **required reason (5–500 chars) + the acting admin's own fresh 2FA code**; no second admin. Admins signed in via Microsoft/Google (external IdP, ADR 0005) confirm via their IdP session; the existing endpoint already implements both paths. Self-reset via admin is refused (existing). *(Dennis, 29-09)*
- **D4.** Support access: the dialog **pre-selects 15 minutes** (as drawn); the existing longer options (60/240) stay selectable for real support cases. Override option: 15 min only. *(Dennis: "keep as drawn")*
- **D5.** New Dutch URLs under `/admin/{groep}/{item}`; every old URL answers with a **301** to its new URL (query string kept) for at least one release. Internal links all point to the new URLs (a test forbids old hrefs in `.razor`/`.cs`).
- **D6.** "Tweestapsverificatie" is **policy, not a switch**: `MfaPolicy` makes TOTP mandatory for Admin/BranchManager/RegionalManager/EnterpriseManager/Intermediary on password login. Platforminstellingen shows it as a read-only row with a lock ("Verplicht voor beheerders en werkgevers"). The existing `AuthenticatorEnabled` flag is the **candidate application authenticator stub** and moves to "Demo & test" with its real name "Authenticator bij sollicitatie (stub)".
- **D7.** Settings rows come from **one catalog** (`PlatformSettingsCatalog`), so a future flag is one entry. Werkgevers actief / Mijn Paspoort are entries only if the fields exist.
- **D8.** Environment badge label comes from the web app's `PublicWebBaseUrl` host (`acceptatie.*` → Acceptatie, `lobsy.nl`/`www.lobsy.nl` → Productie, else Lokaal), with optional override `Deployment:Label`. **Not** from `ASPNETCORE_ENVIRONMENT` (Acceptatie runs as `Production`). No `render.yaml` changes.
- **D9.** Global search (Ctrl K / `/`) searches users (masked results), organisations (name, KvK), vacancies (title, ID) and invoices (number); from 07 also correlation IDs. Max 5 per group; searching users writes one `PersonalDataAccessLog` row (`admin.search`, action `list`) **without** the query text.
- **D10.** Pricing is shown in **one place** (Financiën › Prijzen & pakketten). Where two entities look like the same price (e.g. `TokenSpendCost` vs `VacancyTypeTokenCost`, token packs vs `SalesPackage`), 06 **shows both with a clear "bepaalt:" line** and lists the overlap in the PR; no data merge in this stack.
- **D11.** Admin audit log is append-only (no update/delete API, EF guard), stores **masked** target labels, retention **7 years** (fiscal bewaarplicht for token/payout actions) via the existing retention job. Override option: 2 years like `PersonalDataAccessLog`.
- **D12.** Mobile admin (< 1024): sidebar becomes a left drawer behind a hamburger; no bottom nav for admins. Dense tables become stacked rows on < 640 (label/value), bulk actions hidden on < 640.
- **D13.** Admin post-login lands on `/admin` (was `/home`). `/home` for an admin 301s to `/admin`; for every other role `/home` is unchanged.

## Dependencies
- `fix/2fa-enrollment` is **merged** into `acceptatie` (`cb24244d`), so file 03 wires the drawer to the existing `POST api/admin/users/{userId}/mfa/reset` and `JobsyApiClient.ResetUserMfaAsync`. Before starting 03, verify with `git merge-base --is-ancestor c72fa711 HEAD`; if that fails (branch rebuilt without it), build the drawer button behind `AdminCapabilities.MfaResetAvailable = false` with the copy "Beschikbaar zodra 2FA-inschrijving live staat" and say so in the PR.
- Mijn Paspoort stack (`docs/mijn-paspoort`) touches `SettingsAdmin.razor`, `RoleNavCatalog.cs` and `PlatformFeatureSettings`. Whichever stack lands second merges `acceptatie` in normally (no rebase). 05 re-homes whatever toggles exist at that moment (D7).
- Files 02, 03 and 05 leave **audit slots** (Recente beheeracties card, drawer "Activiteit" tab, "Laatst gewijzigd door" / Wijzigingen panel). 07 fills them. Before 07 they are not rendered.

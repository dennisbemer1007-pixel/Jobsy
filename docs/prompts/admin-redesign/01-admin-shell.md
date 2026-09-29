# 01 · Admin shell: layout, sidebar, top bar, search, URLs

> Read `00-README.md` first: §0, **§IA** (the contract) and the decisions D1, D5, D8, D9, D12, D13 apply. This is the first file.

| | |
|---|---|
| Branch | `cursor/admin-redesign-1`, created from `origin/acceptatie` |
| PR | ONE PR into `acceptatie`, title `feat(admin): own admin layout with grouped sidebar, search and Dutch URLs`. Body starts with `Stacked on: none (first in the stack)` |
| Mockups | `ad-d1-dashboard.png` (shell only: top bar, sidebar, breadcrumbs), `ad-m1-dashboard.png` (mobile top bar + hamburger) |

**Goal.** Every admin page gets the same enterprise shell and a logical place. Page **content** doesn't change in this file (apart from moving into tabs); later files redesign the content.

## 01.1 Today (verify first)
- Admin pages: 30 files in `Components/Pages/Admin/`, all under `MainLayout`, wrapped in `Components/Admin/AdminPageShell.razor` ("← Beheer" link to `/home`, optional `AdminSettingsSubnav` with 18 pill links, `AdminVacanciesSubnav`).
- Admin nav: `RoleNavCatalog.Admin` (7 bottom-nav items incl. Banenkaart; "Settings" has 17 `ExtraActivePaths`). `AdminNavItems.SettingsModules` / `VacancyModules` feed the subnavs.
- Dashboard: `/home` → `Pages/RoleHome.razor` renders `Components/Admin/AdminHomePanel.razor` for admins. `/admin` and `/admin/cockpit` redirect to `/home`.
- Placeholders: `ModerationAdmin.razor`, `NotificationsAdmin.razor` ("Nog niet beschikbaar").
- `app.css :root` has **no** `--space-*`, `--text-*`, `--radius-pill`, `--z-*` at `a611db40`. Add them here if still missing (§0).

## 01.2 One naming + nav source: `Navigation/AdminNav.cs`
- Pure data + pure functions, no Blazor:
  - `AdminNavGroup(string Key, string LabelKey, IReadOnlyList<AdminNavItem> Items)`
  - `AdminNavItem(string Key, string LabelKey, string Href, string Svg, IReadOnlyList<string> Aliases, bool IsAvailable = true, string? CountKey = null)`; `CountKey` is used from 02 on.
  - `AdminNav.Groups` in the exact §IA order. Items whose page arrives later (`Te doen`, `Rollen & rechten`, `Aanvragen`, `Kandidaten`, `Moderatie`, `Auditlog`, `2FA & sessies`, `Privacy & AVG`) are present with `IsAvailable = false`; the file that builds the page flips it to `true`. "Tests & normen" is a comment slot only.
  - `AdminNav.Resolve(string relativePath)` → `(group, item)` for the active state, breadcrumbs, page title. Matches `Href`, then `Aliases`, then the longest prefix.
  - `AdminNav.Crumbs(relativePath, string? detail = null)` → `Beheer › <Group> › <Item> [› detail]`.
- Delete `Components/Admin/AdminNavItems.cs`, `AdminSettingsSubnav.razor`, `AdminVacanciesSubnav.razor` once nothing uses them.
- `RoleNavCatalog.Admin` becomes `[]` and `ForUser` returns it for admins, so `BottomNav` renders nothing for admins (D1). **All other catalogs stay byte-identical** (add a test that snapshots them).

### Labels (nl final; translate the others naturally)
Group labels and item labels exactly as in §IA. Also rename the page `h1`s so **label = h1 = crumb**, e.g. "Systeeminstellingen" → "Functies", "Gebruikers en kandidaten" → "Alle gebruikers", "Bedrijven en intermediairs" → "Bedrijven & vestigingen", "API Beheer" → tab "API-sleutels", "Accesslog PII" → "Gegevensinzage", "Logging" → "Systeemlogs", "Masterdata" → "Stamgegevens", "Mailtest" → "E-mails & meldingen", "Token-administratie" → "Uitbetalingen & btw", "Goodwill / Compensatie" → "Goodwill & tokens", "Financieel" → "Omzet & transacties", "Bedrijfsgegevens" → "Algemeen" (it is Lobsy's own company; lead: "Gegevens van Lobsy zelf, voor facturen en e-mails."), "CNAME / regio-hosts" → "Regio's & domeinen". New string keys go into `UiStringsAdmin.cs` (`AdminNav.*`, `AdminShell.*`); remove the now-unused `Nav.Settings`/`Nav.Cnames`/… keys only if nothing else uses them.

## 01.3 `AdminLayout`
- New `Components/Layout/AdminLayout.razor` (`LayoutComponentBase`). Add `Components/Pages/Admin/_Imports.razor` with `@layout AdminLayout` so all admin pages use it without touching each page.
- **Shared pieces are extracted, not copied**, from `MainLayout.razor`:
  - `Components/Layout/CircuitErrorBoundary.razor`: the `ErrorBoundary` + `LogCircuitError` + Sentry code (MainLayout ~L40–130). `MainLayout` uses it too; behaviour unchanged.
  - Both layouts render `CookieConsentBanner`, `SessionIdleGuard`, `ConsentReacceptDialog`, `FeedbackWidget`, `PageSeoHead`. `AdminLayout` does **not** render `BottomNav`, `LobsyAssistantChat`, `PushPermissionBanner`, `PwaInstallBanner`, `GratisDnaMerge`, `AppFooter`.
- **Structure** (desktop ≥ 1024, see `ad-d1`): fixed top bar 56 px; fixed left sidebar 248 px; main with `admin-main` padding. `< 1024`: sidebar becomes a left drawer (`AdminDrawer` variant `start`) behind a hamburger in the top bar (see `ad-m1`), closes on navigation and Esc, focus-trapped.
- **Top bar** (`Components/Admin/Shell/AdminTopBar.razor`), background `--brand-deep`:
  - `LobsyLogo` (small, on a `--surface` tile) + "Lobsy" + muted "Beheer", links to `/admin`.
  - `AdminEnvBadge` (D8).
  - `AdminGlobalSearch` (01.5).
  - Right: `PageHelp`, `NotificationBell`, `LanguageSelector`, account menu (reuse `AuthHeader`'s menu; add to it for admins: "Naar Lobsy" → `/` and, in `MainLayout`, "Beheer" → `/admin`). Under the name a meta line: "Beheerder · 2FA actief" when the session is MFA-verified or external (same check the API uses: `PersonalDataAccessLogExtensions.IsMfaVerifiedInSession` / `IsExternalAuthMethod`; move the claim reading into a small Web helper, don't duplicate the string constants).
- **Sidebar** (`Components/Admin/Shell/AdminSidebar.razor`): renders `AdminNav.Groups` (available items only).
  - Group header = `h2`-less button with `aria-expanded`; the active item's group is open, others collapsed to one row with a chevron. Open/closed state per circuit in a scoped `AdminSidebarState` service (no JS).
  - Active item: `--accent-soft` fill, 3 px `--brand` inline-start bar, `aria-current="page"`. Count pills come in 02.
  - Keyboard: Tab order top → bottom; arrow keys not required.
- **Breadcrumbs** (`AdminBreadcrumbs`) above the `h1`, from `AdminNav.Crumbs`; `nav aria-label="Kruimelpad"`, last crumb `aria-current="page"`.

## 01.4 `AdminPageShell` becomes the page frame
- Keep the component (all pages already use it). Parameters: `Title` (optional; default = `AdminNav.Resolve(...)` item label), `Lead`, `Wide`, new `Actions` (right-aligned header slot), `CrumbDetail` (optional extra crumb), `ChildContent`. Remove the "← Beheer" link and the `ShowSettingsNav` / `ShowVacanciesNav` parameters and update every caller.
- Test: for every admin page, the rendered `h1` equals its `AdminNav` item label (or the page passes an explicit detail title).

## 01.5 Global search (D9)
- **API:** `GET api/admin/search?q=` in a new `AdminSearchController` (`api/admin/search`, `RequireAdmin`). `q` trimmed, 2–100 chars, else 400. Returns `{ users[], organisations[], vacancies[], invoices[] }`, max 5 each, each `{ id, label, sublabel, href }`.
  - Users: match on email/full name/ID server-side; return **masked** label (`PersonalDataMasker.MaskName`, `MaskEmail`) + role; `href` = `/admin/gebruikers?open=<id>` (03 opens the drawer from it). Writes one `PersonalDataAccessLog` (`admin.search`, `list`, no reason, **no query text**).
  - Organisations: name or KvK (KvK shown masked like the list does, or plain if the companies list shows it plain today; follow the existing list); `href` `/admin/organisaties?open=<id>`.
  - Vacancies: title or ID → `/admin/vacatures?open=<id>` (or the existing detail link).
  - Invoices: invoice number on `TokenPurchaseInvoice` / `SelfBillingInvoice` → the existing finance page with that invoice filtered.
  - Reuse existing query/projection helpers where the list endpoints already have them; don't copy LINQ from `GetUsers`: extract it.
- **UI:** `AdminGlobalSearch.razor` in the top bar: input "Zoek gebruiker, bedrijf, vacature of factuur…", kbd hint "Ctrl K". Debounce 250 ms, min 2 chars, grouped results popover (`role="listbox"`, arrow keys, Enter opens, Esc closes). Ctrl/Cmd+K and `/` (when focus isn't in an input) focus it: tiny module `wwwroot/js/admin-shell.js` (asset-versioned, CSP-safe, no inline script). Mobile: search icon opens a full-screen sheet with the same component.

## 01.6 Environment badge (D8)
- Pure function `DeploymentEnvironment.Resolve(string? publicWebBaseUrl, string? overrideLabel)` → `Acceptatie | Productie | Lokaal` in **`Jobsy.Core/Hosting/`** (the API needs it too in 05). Registered once as a singleton in Web **and** API from config (`PublicWebBaseUrl`, `Deployment:Label`); both services already have `PublicWebBaseUrl` in `render.yaml`, verify for the API and fall back to `Lokaal` if absent.
- `AdminEnvBadge`: pill with dot + label, colours per §0. `title` attribute "Je werkt in Acceptatie" / "Let op: dit is Productie".
- Tests: `https://acceptatie.lobsy.nl` → Acceptatie, `https://lobsy.nl` and `https://www.lobsy.nl` → Productie, `http://localhost:5xxx` → Lokaal, override wins, null → Lokaal.

## 01.7 New URLs, tabs and redirects (D5, §IA)
- Change the `@page` directive of every existing admin page to its §IA URL. Where §IA merges two pages into tabs, create a thin tab host page (e.g. `Pages/Admin/CategorieenSalarisPage.razor` at `/admin/vacatures/categorieen` with `AdminTabs` Categorieën · Salaris & WML) and move each old page body into `Components/Admin/Sections/<Name>Section.razor` unchanged. Tab hosts in this file:
  - Categorieën & salaris (vacancy-categories + wages)
  - Pagina's & flyer (about + marketing-flyer)
  - Stamgegevens (masterdata + exclusivity)
  - Integraties & API (integrations + api-keys)
  - Sales & ambassadeurs (sales-managers + ambassadeurs)
- Settings (`/admin/settings` → `/admin/instellingen`) keeps its current content for now; 05 redesigns it. Its integration tiles, mail-test and CNAME link sections stay until 05.
- **One redirect table** `Navigation/AdminLegacyRoutes.cs`: `(old, new, tab?)` for every old admin URL in §IA plus `/admin/cockpit`, `/admin/moderation` (→ `/admin/vacatures` in this file; 02 retargets it to `/admin/vacatures/moderatie`), `/admin/notifications` (→ `/admin/content/emails`).
  - **Server:** `AdminLegacyRedirectMiddleware` (Web, before endpoint routing, GET/HEAD only) answers **301** to the new URL, keeping the query string and adding `tab=` where the table says so.
  - **In-circuit safety net:** `Pages/Admin/AdminLegacyRedirect.razor` carries the old `@page` routes and calls `NavigateTo(new, replace: true)` from the same table (for enhanced navigation that never hits the server).
  - `/admin` now hosts the dashboard (the current `AdminHomePanel`, moved to `Pages/Admin/AdminDashboard.razor`). `AdminIndex.razor` and `AdminHome.razor` are deleted (covered by the table).
  - `RoleHome.razor`: an admin on `/home` is redirected to `/admin` (D13). `AuthRedirects`: admin default landing `/admin`; all other roles unchanged.
- **Placeholders removed:** delete `ModerationAdmin.razor` and `NotificationsAdmin.razor` and their now-unused strings.
- **Update every internal link** (`href="/admin/…"`, `NavigateTo("/admin/…")`, `PageHelpDocs`, `PageSeoCatalog`, emails that link to admin pages) to the new URLs. Test `AdminLegacyHrefTests`: no `.razor`/`.cs` file under `Jobsy.Web` except `AdminLegacyRoutes.cs`/`AdminLegacyRedirect.razor` contains an old admin URL.

## 01.8 Mobile (< 1024)
- Top bar: hamburger (44 px) · logo · "Beheer" · env badge · search icon · avatar (see `ad-m1`). Sidebar in the drawer with the same groups.
- Admin pages don't reserve bottom-nav clearance (`AdminLayout` has no `has-bottom-nav`).
- Existing admin tables keep working; the stacked-row pattern (D12) is introduced per page in 02–07.

## Tests
- `AdminNavTests`: groups/items in §IA order; every available `Href` resolves to a routable component with `[Authorize(Roles = "Admin")]`; every admin `@page` (except the legacy redirect component) is reachable from `AdminNav` or a tab host; labels unique; `Resolve` + `Crumbs` for exact, alias and prefix paths.
- `AdminLegacyRoutesTests`: every old URL from the 01.1 inventory maps; middleware returns 301 with query + `tab`; non-admin paths untouched; `/home` as admin → `/admin`, as candidate → unchanged.
- `AdminLegacyHrefTests` (01.7).
- `RoleNavCatalog`: `Admin` is empty; candidate/employer/sales/ambassadeur catalogs equal a stored snapshot. Update `Sprint1ShellTests` (admin assertions) and `RoleFunctionalRegressionTests` with the reason in the PR.
- bUnit: `AdminSidebar` (active item, `aria-current`, collapsed groups, unavailable items hidden), `AdminBreadcrumbs`, `AdminEnvBadge`, `AdminPageShell` (title default), `AdminGlobalSearch` (debounce, grouped results, Esc).
- `AdminSearchController`: 403 for non-admin roles; 400 for `q` < 2; masked users; exactly one access-log row without the query text; max 5 per group.
- `DeploymentEnvironment` (01.6). `BlazorPageRoleAttributesTests`, `RoutesDocFreshnessTests`, `PageSeoTests`, `PageHelpDocsTests`, `AssetVersionGuardTests` green.
- Playwright `AdminShellPlaywrightTests`: admin at 1440 sees sidebar + breadcrumbs on `/admin/gebruikers`; `/admin/users?companyId=x` lands on `/admin/gebruikers?companyId=x`; at 390 the hamburger opens the drawer and Esc closes it; Ctrl K focuses search.

## Success criteria
- Every admin page renders in `AdminLayout` with sidebar, top bar, env badge, breadcrumbs; no admin bottom nav; Banenkaart not in admin nav.
- Every old admin URL 301s to its new URL; no internal link uses an old URL.
- Labels = h1 = crumbs; no English nav labels left in admin (nl).
- The two placeholder pages are gone.
- Candidate and employer nav + layout unchanged (snapshot test).
- Global search works with masked users and logs one access row per search.
- Build + tests green; PR body complete (incl. the old → new URL table).

## Done → next
Push, open the PR, note its number. Continue with **`02-dashboard-te-doen.md`**. If anything is red, stop and report.

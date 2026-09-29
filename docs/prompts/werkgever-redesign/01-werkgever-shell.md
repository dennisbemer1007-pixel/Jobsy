# 01 · Werkgever shell: layout, one nav catalog, scope chip, URLs, rights matrix

> Read `00-README.md` first: §0, **§IA**, **§R** and the decisions D1, D2, D3, D4, D14–D17 apply, plus the Dependencies checks A and B. This is the first file.

| | |
|---|---|
| Branch | `cursor/werkgever-redesign-1`, created from `origin/acceptatie` |
| PR | ONE PR into `acceptatie`, title `feat(werkgever): shared employer shell with role-scoped nav, scope chip and Dutch URLs`. Body starts with `Stacked on: none (first in the stack)` and states which case of Dependencies A and B applied |
| Mockups | `bm-d1-dashboard.png` (shell only: top bar with scope chip + token chip, sidebar, breadcrumbs), `bm-d7-dashboard-regiomanager.png` (RM chip + "Alleen lezen"), `bm-d8-vacatures-vestigingsmanager.png` (VM fixed chip, VM nav), `bm-m1-dashboard.png` (mobile top bar + bottom nav) |
| Split seam (if too big) | 1a = primitives + layout + nav + scope; 1b = URL moves + redirects + rights matrix |

**Goal.** Every employer page gets the same shell and a logical place, per role. Page **content** doesn't change in this file (apart from moving into tabs); 02–07 redesign the content.

## 01.1 Today (verify first)
- Employer pages:
  - `Components/Pages/Employer/*.razor` (18): `Vacancies` (`/employer/vacancies` + `/branch/vacancies`), `Tokens` (`/employer/tokens` + `/branch/tokens`), `Branches` (`/employer/branches` + `/regional/branches`), `CultureScan` (`/employer/culture` + `/branch/culture`), `CompanyDetails`, `Users`, `Regions`, `SalaryTables`, `CsvImport`, `Takeovers`, `TalentPool`, `TalentContacts`, `CandidateInsights`, `Organization` (redirect to `/employer/company`), `PartnerSales`, `PartnerSalesPayoutCheckoutStub`, `OnboardingCheckout`
  - `Components/Pages/Branch/`: `Applicants`, `CreateVacancy` (1.800 lines), `BranchDashboard` (`/branch` → `/home`)
  - `Components/Pages/Regional/`: `RegionalDashboard` (`/regional` → `/home`), `TokenControl` (`/regional/tokens`)
- Dashboard: `/home` → `Pages/RoleHome.razor` → `Components/Home/EmployerHomePanel.razor` ("Bedrijf/Regio/Vestiging dashboard", generic `MetricsCategoryBoard` + `RaamflyerTools`).
- Nav:
  - `Navigation/RoleNavCatalog.cs` has `Enterprise` (8 flat items), `Regional` (5), `Branch` (8), `Intermediary` (7), plus `Organization` (desktop-only, `ExtraActivePaths`), `CsvImport`, `Takeovers`, `BalanceAndTracking`, `WithSalesReferralNav`, `WithOptionalCandidateApplications`.
  - The Organisatie subnav: `Components/Employer/EnterpriseOrgSubnav.razor` + `EnterpriseNavItems.cs` (conditional Regio's/CSV/Overnames).
  - All roles share `MainLayout` + `BottomNav`.
- Authorization:
  - Pages carry `[Authorize(Roles = "…")]` (e.g. `Vacancies`: BranchManager,RegionalManager,EnterpriseManager,Intermediary,Admin; `Users`: EnterpriseManager,Admin; `Tokens`: no Admin; `CandidateInsights`: Branch/Regional/Enterprise).
  - API: `CompanyUsersController` = EnterpriseManager/Intermediary/Admin; `RegionsController` = EnterpriseManager/Admin; `TokensController` purchase = `JobsyRoles.TokenPurchaseRoles` (incl. BranchManager), allocate = `TokenAllocateRoles`; `JobsyRoles.EmployerMutateRoles` excludes RegionalManager.
  - Scope: `ICompanyAuthorizationService.GetAccessibleCompanyIdsAsync` / `EnsureCanAccessCompanyAsync`.
- 38 files under `Jobsy.Web` contain `"/employer`, `"/branch` or `"/regional` literals (`rg -l '"/(employer|branch|regional)' Jobsy.Web`).

## 01.2 Shared primitives (Dependencies B)
Apply the case from the README. When you build them here, the minimum set is `EntDataTable` (header, checkbox column, row-actions slot, empty/loading, `aria-sort`, stacked rows < 640), `EntFilterBar`, `EntBulkBar`, `EntPager`, `EntTabs`, `EntKpiCard`, `EntDrawer`, `EntImpactNote`, `EntScopeChip`. bUnit tests per primitive. Nothing page-specific in them.

## 01.3 One nav + naming source: `Navigation/WerkgeverNav.cs`
- Pure data + pure functions, no Blazor:
  - `WerkgeverNavGroup(string Key, string LabelKey, IReadOnlyList<WerkgeverNavItem> Items, IReadOnlyDictionary<EmployerRole,string>? LabelOverrides = null)`
  - `WerkgeverNavItem(string Key, string LabelKey, string Href, string Svg, IReadOnlyList<string> Aliases, RoleVisibility Visibility, string? AvailabilityKey = null, string? CountKey = null, IReadOnlyDictionary<EmployerRole,string>? LabelOverrides = null)`
    - `RoleVisibility` = per role `Full | ReadOnly | OwnScope | Hidden`, taken **1:1 from §IA/§R**.
    - `AvailabilityKey` = the conditional rules in §IA (Koppelingen, Overnames, Partnerprogramma, Kandidaatinzichten, "Mijn sollicitaties"), evaluated by one `WerkgeverNavContext` record (flags + counts + company facts), never by scattered `if`s.
    - `CountKey` is used from 02 on.
  - `EmployerRole` enum: `Bedrijfsmanager, Regiomanager, Vestigingsmanager, Intermediair`, resolved once from claims (`RoleClaimMatching`).
  - `WerkgeverNav.For(EmployerRole, WerkgeverNavContext)` → the visible groups/items in §IA order.
  - `WerkgeverNav.MobileItems(EmployerRole, ctx)` → the 5 bottom-nav items (D15).
  - `WerkgeverNav.Resolve(relativePath)` → `(group, item)` (Href, then Aliases, then the longest prefix).
  - `WerkgeverNav.Crumbs(relativePath, role, detail?)` → `{scope label} › <Group> › <Item> [› detail]`, as in the mockups ("Voorbeeld Tuinbouwgroep BV › Vacatures", "Regio Westland › Dashboard", "Vestiging Naaldwijk › Vacatures").
  - Items whose page arrives later (Te doen, Verbruik per vestiging, Mutaties, Facturen, Koppelingen, Wervingsmateriaal, Partnerprogramma) are present with `IsAvailable = false`; the file that builds the page flips it.
- `RoleNavCatalog.Enterprise`, `Regional`, `Branch` and `Intermediary` become `[]`, and `ForUser` returns them for those roles (D1). Delete `Organization`, `CsvImport`, `Takeovers`, `BalanceAndTracking` once unused, and move the logic of `WithSalesReferralNav` / `WithOptionalCandidateApplications` into `WerkgeverNavContext`. **All non-employer catalogs stay byte-identical**: snapshot test. If Dependencies A applies, keep `ForUser(user, flags)`.
- Labels (nl final; translate the others naturally) exactly as §IA, incl. the role overrides (RM "Signalen", "Mijn regio", "Tokenverbruik"; VM "Mijn vestiging", "Vestigingsprofiel", "Saldo & aanvragen"). Page `h1`s are renamed so label = h1 = crumb (e.g. "Bedrijf dashboard"/"Regio dashboard"/"Vestiging dashboard" → "Dashboard" with the scope in the crumb; "Gebruikers" → "Team & rechten"; "Bedrijfsgegevens" → "Bedrijfsprofiel"; "Mijn tokens" → "Saldo & aanvragen"). Keys go into `UiStringsWerkgever.cs` (`WgNav.*`, `WgShell.*`).

## 01.4 `EmployerScope` (D3)
- `Jobsy.Web/Werkgever/EmployerScopeState` (scoped per circuit):
  - `Role`
  - `AvailableScopes` (Organisation / Region(id) / Vestiging(id), from existing endpoints: memberships, regions, branches; **no new endpoint** unless none returns regions for RM; then one read-only `GET api/werkgever/scopes` in a new `WerkgeverController`, `[Authorize(Policy = JobsyPolicies.RequireEmployer)]`)
  - `Current`
  - `Label`
  - `CompanyIds` (the ids to pass to existing endpoints as their existing `branchId`/`companyId` filters)
- Default: BM = Organisation, RM = own region (or all own regions), VM = own vestiging (first membership).
- The `?scope=org|region:{id}|vestiging:{id}` query deep-links; an invalid or foreign scope falls back to the default with a small info toast "Je hebt geen toegang tot die vestiging." The server is the real guard: every endpoint keeps checking ids against `GetAccessibleCompanyIdsAsync`.
- `EntScopeChip` in the top bar:
  - icon + label + sub-label ("14", "5 vestigingen")
  - chevron only when > 1 scope
  - popover with a search when > 8 entries
  - RM additionally shows the `Alleen lezen` pill (`--accent-soft` / `--brand`, eye icon).

## 01.5 `WerkgeverLayout`
- New `Components/Layout/WerkgeverLayout.razor`. Add `_Imports.razor` with `@layout WerkgeverLayout` in the new `Components/Pages/Werkgever/` folder (01.7 moves the pages there).
- Shared pieces extracted, not copied (Dependencies B rule for `CircuitErrorBoundary`). Renders `CookieConsentBanner`, `SessionIdleGuard`, `ConsentReacceptDialog`, `FeedbackWidget`, `PageSeoHead`, `PushPermissionBanner`, `PwaInstallBanner` like `MainLayout`; **not** `BottomNav`, `GratisDnaMerge`, `AppFooter`. `LobsyAssistantChat` stays if employers have it today (verify).
- **Desktop ≥ 1024** (see `bm-d1`):
  - **Top bar** (`Components/Werkgever/Shell/WgTopBar.razor`, `--brand-deep`, 56 px):
    - logo + "Lobsy" + muted "Werkgever" (→ `/werkgever`)
    - `EntScopeChip` (+ RM pill)
    - search input: only if the admin global search component exists and can be scoped to employer data. Otherwise **not rendered** (deferred; don't build a new search in this stack)
    - token chip: coin icon + balance of the current scope, reusing `TokenWalletChip` logic, hidden for RM
    - `PageHelp`, `NotificationBell`, `LanguageSelector`, account menu (name + role label "Bedrijfsmanager / Regiomanager / Vestigingsmanager")
  - **Sidebar** (`WgSidebar.razor`, 248 px):
    - groups from `WerkgeverNav.For`; all groups open by default except "Meer" (collapsed row with a count badge when it contains a count)
    - active item: `--accent-soft` + 3 px `--brand` bar, `aria-current="page"`
    - footer: "Banenkaart bekijken" link (D16); RM footer line "Je kijkt mee als regiomanager. Wijzigen doen de vestigings- en bedrijfsmanagers."
  - **Breadcrumbs** above the `h1` from `WerkgeverNav.Crumbs` (`nav aria-label="Kruimelpad"`).
- **Mobile < 1024** (see `bm-m1`):
  - top bar: logo · scope chip · bell · avatar
  - bottom nav (`WgBottomNav.razor`) with `WerkgeverNav.MobileItems`, safe-area padding, count pill on Sollicitaties; "Meer" opens a bottom sheet (`EntDrawer` sheet variant) with the remaining visible items grouped as in the sidebar
- `WgPageShell.razor` = the page frame: `Title` (default from `WerkgeverNav.Resolve`), `Lead`, `Actions` slot, `ReadOnlyHint` (renders the `wg-readonly-hint` line for RM automatically when the item's visibility is `ReadOnly`), `CrumbDetail`, `ChildContent`.
- Read-only helper: `<WgAction Kind="Primary" RequiresWrite="true">` renders the action for BM/VM and, for RM, a disabled button with a lock icon + tooltip "Dit doet de vestigings- of bedrijfsmanager" (D4). Use it only for the page's primary action; other write actions are simply not rendered for RM.

## 01.6 Rights matrix foundation (§R)
- `Jobsy.Tests/Werkgever/WerkgeverRightsMatrix.cs`: **one** table (page route × role → expected allow/deny; endpoint × role → expected status) generated from §R. Tests:
  - `WerkgeverPageAuthorizeTests`: every `@page` under `Pages/Werkgever/` has an `[Authorize(Roles=…)]` whose role set equals the matrix row (Admin allowed only where it is today).
  - `WerkgeverNavVisibilityTests`: `WerkgeverNav.For(role)` shows exactly the pages the matrix allows for that role, and never a `Hidden` one.
  - `WerkgeverApiRightsTests` (API test host, pattern: `CandidateInsightsAuthzApiTests`): for the mutating endpoints the pages use today:
    - vacancy create/update/publish/extend/highlight/pushbom/deactivate/duplicate, `approve-publish`
    - application accept/reject/invite/hire
    - talent contact unlock
    - `company-users/invite` + update
    - regions CRUD
    - tokens checkout/allocate
    - company update
    - For each: RM → 403, VM on a foreign company → 403, VM on own → 2xx (where §R allows), BM → 2xx. **Fix any endpoint that lets RM or a foreign VM through** (stricter only) and list it in the PR.
  - **D5 tightening lands here:** remove `BranchManager` from `TokenPurchaseRoles` / `CanPurchaseTokens`. Update the tests that assumed it, and give the reason in the PR. Today's comment says VMs may buy "only when the vestiging is not under enterprise token management": query (or describe for Dennis) how many `BranchManager` users sit in a company **without** an `EnterpriseManager` in its organisation. List the count in the PR; if > 0, keep their purchase path behind a clearly named rule `JobsyRoles.CanPurchaseTokens(role, hasEnterpriseManager)` and say so (Dennis decides), rather than leaving a vestiging with no buyer. The Tokens page hides "Tokens kopen" for VM (06 builds the request flow; until then VM sees "Vraag je bedrijfsmanager om tokens").
  - `WerkgeverFeatureGateTests` (Dependencies A): if `RequiresFeatureAttribute` exists, every `Pages/Werkgever` page and every new employer controller carries `RequiresFeature(PlatformFeature.Employers)`; otherwise the test passes vacuously.

## 01.7 New URLs, tabs and redirects (D2, §IA)
- Move the pages into `Components/Pages/Werkgever/` and change their `@page` to the §IA URL. **Pages with two routes get exactly one new route** (`Vacancies`, `Tokens`, `Branches`, `CultureScan`); role differences come from `EmployerScopeState`, not from the URL.
- Tab hosts in this file (bodies moved unchanged into `Components/Werkgever/Sections/`):
  - **Talentpool**: Zoeken · Contactverzoeken (`TalentPool` + `TalentContacts`; keep `EmployerTalentTabs` behaviour; Kandidaatinzichten stays its own item)
  - **Bedrijfsprofiel**: for now the whole `CompanyDetails` body as tab Profiel + `CultureScan` as tab Cultuur. 05 splits the rest.
- `RegionalDashboard`, `BranchDashboard` and `TokenControl` are covered by redirects. `TokenControl`'s content is compared with the Tokens page in 06; until then `/regional/tokens` → `/werkgever/tokens`. `Organization.razor` is deleted.
- The **dashboard** moves to `Pages/Werkgever/WerkgeverDashboard.razor` at `/werkgever` (today's `EmployerHomePanel`, unchanged). `RoleHome.razor` redirects employer roles to `/werkgever`. `AuthRedirects` sets the employer default landing to `/werkgever`; other roles are unchanged.
- **One redirect table** `Navigation/WerkgeverLegacyRoutes.cs` `(old, new, tab?)` covering every old URL in §IA plus `/branch`, `/regional`, `/regional/tokens`, `/employer/organization`, `/employer/salary-tables/{id}`:
  - **Server:** `WerkgeverLegacyRedirectMiddleware` (Web, before endpoint routing, GET/HEAD only) → **301**, query kept, `tab=` added, route values (`{id}`) carried over. `/home` → `/werkgever` only when the authenticated user has an employer role.
  - **In-circuit safety net:** `Pages/Werkgever/WerkgeverLegacyRedirect.razor` carries the old `@page` routes and `NavigateTo(new, replace: true)` from the same table.
  - **Payment return URLs don't move** (§IA).
- **Update every internal link** (`href`, `NavigateTo`, `PageHelpDocs`, `PageSeoCatalog`, e-mail templates/`TransactionalEmails` links, notification deep links, `docs/ROUTES.md`). Guard test `WerkgeverLegacyHrefTests`: no `.razor`/`.cs` under `Jobsy.Web`/`Jobsy.Core/Email`, except the redirect table/component, contains an old employer URL.

## 01.8 Mobile (< 1024)
- No desktop sidebar; bottom nav + Meer sheet (01.5). Pages keep their current mobile markup until their redesign file.
- Tap targets ≥ 44 px; bottom-nav clearance via a `wg-has-bottom-nav` class on the layout.

## Tests
- `WerkgeverNavTests`: groups/items in §IA order per role; labels unique; `Resolve` + `Crumbs` for exact, alias and prefix paths; conditional items follow `WerkgeverNavContext`; `MobileItems` = 5 per role in the §IA order.
- `RoleNavCatalog`: the employer catalogs are empty; the candidate/admin/sales/ambassadeur catalogs equal a stored snapshot. Update `RoleFunctionalRegressionTests` / UAT scenario assertions that referenced old employer nav, with the reason in the PR.
- The 01.6 matrix tests.
- `WerkgeverLegacyRoutesTests`: every old URL maps; 301 with query + `tab` + `{id}`; `/home` as BM/RM/VM → `/werkgever`, as candidate → unchanged; payment return URLs untouched.
- `WerkgeverLegacyHrefTests`.
- `EmployerScopeStateTests`: defaults per role; a foreign `?scope=` falls back; RM never gets write scope.
- bUnit: `WgSidebar` (active item, `aria-current`, hidden items not rendered, RM footer), `WgTopBar` (chip per role, RM pill, no token chip for RM), `WgBottomNav` (5 items, Meer sheet), `WgPageShell` (title default, RM read-only hint), `WgAction` (disabled + tooltip for RM), the primitives.
- `BlazorPageRoleAttributesTests`, `RoutesDocFreshnessTests`, `PageSeoTests`, `PageHelpDocsTests`, `AssetVersionGuardTests` green.
- Playwright `WerkgeverShellPlaywrightTests`:
  - BM at 1440 sees the sidebar groups + scope chip on `/werkgever/vacatures`
  - RM sees "Alleen lezen" and no Team & rechten
  - VM sees a fixed chip and no Vestigingen & regio's
  - `/employer/vacancies?status=active` lands on `/werkgever/vacatures?status=active`
  - at 390 the bottom nav shows 5 items and Meer opens the sheet

## Success criteria
- Every employer page renders in `WerkgeverLayout` with role-scoped sidebar, scope chip, breadcrumbs; mobile bottom nav from the same catalog.
- Every old `/employer`, `/branch`, `/regional` URL 301s to its new URL; no internal link uses an old URL; payment return URLs unchanged.
- Label = h1 = crumb; no English nav labels in nl.
- The rights matrix tests pass for BM, RM and VM; RM can't mutate anything; VM can't touch foreign companies or buy tokens.
- Candidate and admin nav unchanged (snapshot).
- Build + tests green; PR body complete (incl. old → new URL table and the Dependencies A/B outcome).

## Done → next
Push, open the PR, note its number. Continue with **`02-dashboard-te-doen.md`**. If anything is red, stop and report.

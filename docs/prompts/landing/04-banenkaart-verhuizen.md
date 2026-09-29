# 04. Move the banenkaart to `/banenkaart` (public, indexed); `/banen` → 301

Read `00-README.md` first. Branch `cursor/landing-4` from `cursor/landing-3` (or `-3b`).

> **Rules (same as README §0, repeated on purpose):**
> - Never merge, never deploy, never use rule `123` (`.cursor/rules/shortcut-123.mdc`).
> - Never push to `main` or `acceptatie`; push only this file's `cursor/landing-*` branch; no force-push.
> - ONE stacked PR into `acceptatie`.
> - Red tests or an unmet success criterion: push, open the PR as **draft**, stop and report. Don't start the next file.
> - This file **moves** the map; it doesn't change it. `VacancyDiscovery`, `jobMap*.js`, the banenkaart CSS and the perf behaviour stay byte-identical.

| | |
|---|---|
| Branch | `cursor/landing-4` |
| PR title | `feat(banenkaart): serve the map at /banenkaart (public, indexed); /banen 301; links, nav and perf guards retargeted` |
| PR body starts with | `Stacked on #<PR 03> (cursor/landing-3)` |
| Mockups | none (no visual change) |
| Split seam | **04a** = page/route/redirect/`AuthRedirects`/nav/links (04.2–04.4). **04b** = tests + SEO/sitemap/docs (04.5, 04.6) |

## Goal
The map has its own URL, `/banenkaart`, so "/" is free for the landing page in 05. Everyone who meant "the map" (nav, post-login, back links, deep links, crawlers) lands on `/banenkaart`. During this PR "/" still shows the map (second route), so nothing breaks on the stack before 05.

## 04.1 Today (verify first)
- `Pages/Home.razor`: `@page "/"`, AllowAnonymous, `InteractiveServerRenderMode(prerender: true)`, `CultureAwareComponentBase`, title `Page.JobMapTitle`, `<VacancyDiscovery />`, no MapLibre css/preload (guarded by `HomepagePerformanceGuardTests`).
- `Pages/Banen.razor`: `/banen`, a client `NavigateTo("/")` with prerender false.
- `Auth/AuthRedirects.cs` L12 `BanenkaartPath = "/"`, used by `CandidatePostLoginUrl`, `Login.razor` `_dismissUrl`, `Register.razor` (L18, 393, 566, 841). Tests `ExternalAuthAndInvitePromotionTests` and `PlatformUxSpecTests` assert it.
- `Navigation/RoleNavCatalog.cs` L13, 24 (`Nav.Search`), 41, 77, 86, 100 → "/".
- "Means the map" links (`href="/"`): `MatchUnlockPanel` (L13, 66), `Candidate/Applications` L88, `Candidate/Liked` L33, `Candidate/MatchPage` L28, 56, `Candidate/Shared` L29, `Candidate/Vacancies` L49, 67, 87, 96, `CompanyPublicPage` L38 (back link), `Error.razor` L15, `VacancyDetail` L33, 52, `WestlandTeaser` L211, `HowLobsyRoleGuides` L40, 51, 61, 82, 95, 122 (`Nav.JobMap` / `HowLobsy.ToMap`), `Candidate/HowLobsyWorks.razor` L38 (`NavigateTo("/")` after the how-to).
- "Means home/brand" links (keep "/"): `MainLayout` brand L22, `CompanyPublicPage` logo L27, `VacancyDiscovery` logo L31, `SetUnavailableAction`/`WithdrawOthersAction` "Naar home", `PageSeoHead` breadcrumb root, `StructuredData` `url`.
- Query deep links: `VestigingLanding.razor` L24/34 → `/?company={id}`; `StructuredData` L44 SearchAction → `/?q={search_term_string}`. `VacancyDiscovery` reads `company`, `workType`, `q`, `maxMinutes`, `transport`, `minHours`, `maxHours` (+ any others: grep `TryGetValue("` in `VacancyDiscovery.razor`).
- Tests that open "/" for the map (non-exhaustive): `Banenkaart*PlaywrightTests`, `JobMapPinsClustersPlaywrightTests`, `JobMapPrerenderGuardTests`, `FilterSheetFooterPlaywrightTests`, `PersistLcpPlaywrightTests`, `MobileSmokePlaywrightTests`, `NavFeedbackPlaywrightTests`, `CspSmokePlaywrightTests`, `BlazorReconnectAndHealthzTests`, `FeedbackPipelineTests`, `StaticAssetCacheTests`, `Sprint1ShellTests`, `HomepagePerformanceGuardTests`, `Uat/*`. Get the full list with `git grep -l -E 'Home\.razor|BanenkaartPath|"/banen"|GotoAsync' -- Jobsy.Tests`.

## 04.2 Page + routes
- `git mv Pages/Home.razor Pages/Banenkaart.razor` (keep the history). Routes: `@page "/banenkaart"` **and, for now, `@page "/"`** with the comment `// "/" moves to Landing.razor in landing 05`. The same render mode and everything else unchanged.
- Canonical for both routes = `/banenkaart` (`PageSeoCatalog`: move the `"/"` entry `Public("Page.JobMapTitle","Seo.HomeDescription")` to `"/banenkaart"`, and make "/" temporarily canonicalise to `/banenkaart`; 05 gives "/" its own entry).
- **`/banen` → 301 `/banenkaart`** as a server endpoint (Minimal API `MapGet("/banen")` or a redirect rule), preserving the query string. Delete `Pages/Banen.razor`.
- `AuthRedirects.BanenkaartPath = "/banenkaart"`. Update `ExternalAuthAndInvitePromotionTests` and `PlatformUxSpecTests` expectations (they assert the value; the behaviour "candidates land on the map" is unchanged).

## 04.3 Links and nav
- `RoleNavCatalog`: every `Nav.JobMap` / `Nav.Search` href → `/banenkaart`. Active-state matching must treat `/banenkaart` (and during 04 also "/") as the map item. Update the catalog tests.
- Every "means the map" link in 04.1 → `PublicRoutes.Banenkaart` (or `AuthRedirects.BanenkaartPath` in auth code). Keep labels.
- `VestigingLanding` → `/banenkaart?company={id}`. `StructuredData` SearchAction target → `/banenkaart?q={search_term_string}`.
- `Candidate/HowLobsyWorks.razor` L38 → `/banenkaart`.
- "Means home/brand" links stay "/" (05 makes "/" redirect signed-in users to their home, D1).
- `PublicNavCatalog` (01): flip `/banenkaart` to `IsAvailable = true` (ON).
- JS: check `wwwroot/js/*.js` for any path check that assumes the map is at "/" (e.g. `location.pathname === "/"` for map boot, service worker precache, `jobsyMaps` guards) and make it path-agnostic or add `/banenkaart`. Say what you found in the PR.

## 04.4 Legacy "/" map deep links (prepare for 05)
- Add `Seo/LegacyMapQuery.cs` (pure): `bool IsMapDeepLink(IQueryCollection q)` = any key from the `VacancyDiscovery` list in 04.1. 05 uses it to 301 `/?company=…` etc. to `/banenkaart?…`. Unit test the key list against a reflection/grep of `VacancyDiscovery.razor` (fail when a new query key appears that isn't in the list).

## 04.5 Tests
- Retarget **every test that needs the map or a Blazor circuit** from "/" to `/banenkaart`, via one shared constant (`E2eRoutes.Banenkaart` in the test project). Leave only tests whose subject is "whatever the home page is" on "/" and list them in the PR; 05 re-checks them.
- `HomepagePerformanceGuardTests`: point it at `Pages/Banenkaart.razor` and rename it `BanenkaartPagePerformanceGuardTests`. The assertions are unchanged (no maplibre css/preload in the page, `window.jobsyMaps` lazy-load, `link.media = "print"`, `VersionedAssetCacheMiddleware`).
- New: `/banen` → 301 `/banenkaart` with the query preserved; `/banenkaart` 200 anonymous with the map chrome prerendered; canonical `/banenkaart` on both routes; `RoleNavCatalog` hrefs; `LegacyMapQuery` key list.
- `PageSeoTests`, `RoutesDocFreshnessTests`, `PageHelpDocsTests` (`/banenkaart` help = today's "/" help), `BlazorPageRoleAttributesTests`.

## 04.6 SEO, sitemap, docs
- `SeoEndpoints` sitemap: add `/banenkaart` (priority as "/" had), keep "/". robots unchanged.
- `docs/ROUTES.md`: `/banenkaart` (map), `/banen` (301), "/" ("map until landing 05").
- `docs/performance.md`: a note that the map page is `/banenkaart` and the perf section's "Homepage-kaart" applies to it.
- `CHANGELOG.md`.

## Success criteria
- `/banenkaart` behaves exactly like today's "/" (same HTML chrome, same lazy MapLibre, same query deep links). `/banen?q=zorg` → 301 `/banenkaart?q=zorg`.
- Signing in as a candidate lands on `/banenkaart`. All nav "Banenkaart"/"Zoeken" items point there.
- The whole Playwright map suite passes against `/banenkaart`.
- "/" still works (map) until 05.

Done → next: `05-landing-met-werkgevers.md`.

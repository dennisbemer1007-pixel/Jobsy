# Mijn Paspoort + Werkgevers actief: Cursor run book

Cursor: **read this file completely**, then **execute the files below strictly in order**, one at a time. Each file is one PR.

## Order

| # | File | Branch | Branches from | PR into |
|---|---|---|---|---|
| 01 | `01-werkgevers-actief.md`: admin switch "Werkgevers actief" + §F flag foundation + nav catalog refactor | `cursor/werkgevers-actief` | `origin/acceptatie` | `acceptatie` |
| 01b | `01b-opleidingen-seed-opruimen.md`: stop seeding fake course providers in production | `cursor/opleidingen-seed-opruimen` | `cursor/werkgevers-actief` | `acceptatie` |
| 02 | `02-paspoort-overzicht-mijn-dna.md`: switch "Mijn Paspoort", nav (§N), route + redirects, top overview, tab shell, tab Mijn DNA | `cursor/mijn-paspoort-1` | `cursor/opleidingen-seed-opruimen` | `acceptatie` |
| 03 | `03-mijn-tests.md`: tab Mijn tests + course data model + "Groei verder" | `cursor/mijn-paspoort-2` | `cursor/mijn-paspoort-1` | `acceptatie` |
| 04 | `04-past-deze-baan-carriere.md`: tabs Past deze baan? + Carrière | `cursor/mijn-paspoort-3` | `cursor/mijn-paspoort-2` | `acceptatie` |
| 05 | `05-bewijzen-mijn-gegevens.md`: tabs Bewijzen + Mijn gegevens (all profile forms) | `cursor/mijn-paspoort-4` | `cursor/mijn-paspoort-3` | `acceptatie` |
| 06 | `06-ontdekkingsreis-data.md`: new profile fields (languages, Dutch level, employer preferences, learning goals, hobbies), private dislikes table, shown in the paspoort | `cursor/ontdekkingsreis-1` | `cursor/mijn-paspoort-4` | `acceptatie` |
| 07 | `07-ontdekkingsreis-wizard-stappen-1-6.md`: De ontdekkingsreis shell (zones, scene, lobster plates, progress, "Later verder"), wizard v3 state migration, Start + steps 1–6 + consent | `cursor/ontdekkingsreis-2` | `cursor/ontdekkingsreis-1` | `acceptatie` |
| 08 | `08-ontdekkingsreis-tests-einde-nav.md`: tests 7–10, "Weer een laag eraf" + going deeper, end screen, nav slot, "Verder ontdekken", old onboarding redirect | `cursor/ontdekkingsreis-3` | `cursor/ontdekkingsreis-2` | `acceptatie` |

Files 05 and 07 may each split into a/b (see their size notes). The next file then branches from the **last** sub-branch (for example `cursor/mijn-paspoort-4b`). Mockups for 06–08 are in `docs/mockups/ontdekkingsreis/` on this branch.

## Pointer prompt (the only prompt needed; it runs 01 … 08)
```
Run the Mijn Paspoort + Ontdekkingsreis stack. First: git fetch origin && git show origin/docs/mijn-paspoort:docs/prompts/mijn-paspoort/00-README.md — read it completely.
Then read and execute each file in docs/prompts/mijn-paspoort/ on that branch strictly in the order the README's table lists (01, 01b, 02 … 08; a/b splits where a file says so), one file = one PR.
File 01 branches from origin/acceptatie; every later file branches from the previous file's branch (stacked). Each opens ONE PR into acceptatie whose body starts with "Stacked on #<prev PR>".
Build and test after each file; if tests fail or a success criterion can't be met, push, open that PR as draft, stop and report — don't start the next file.
Never merge, never deploy, never use rule 123, never push to main or acceptatie, no force-pushes.
At the end report: file → branch → PR number → status, plus anything deferred.
```

## How to run
1. `git fetch origin`. Read `00-README.md` (this file) and the design rules `.cursor/rules/design-system.mdc`.
2. For each file in the order above:
   1. Read the whole file.
   2. Create its branch from the "Branches from" column. For file 01: `git checkout -b cursor/werkgevers-actief origin/acceptatie`. For later files, run `git checkout -b <branch> <previous branch>` while the previous branch is checked out locally and pushed.
   3. Implement **only** that file's scope, plus the shared rules below.
   4. Run `dotnet build` and `dotnet test`, and any Playwright suites the file names if you can. Everything must be green, and the file's **success criteria** must hold.
   5. Commit with small, clear commits. Push the branch (`git push -u origin <branch>`, only `cursor/*` branches). Open **ONE PR into `acceptatie`** with the title from the file. The body starts with `Stacked on #<prev PR> (<prev branch>)`, then the PR body items from §0.
   6. Note the PR number, then go on to the next file.
3. **Stop and report** when tests fail and you can't fix them within the file's scope, when a success criterion can't be met, or when something in the code contradicts this spec in a way you can't resolve safely. Push what you have and open that PR as a **draft** with the failure described. Don't continue to the next file.
4. At the end, report the table of files → branch → PR number → status (green or draft/red), plus anything you deferred.
5. **Never** merge, deploy, or use rule `123`. **Never** push to `main` or `acceptatie`. No force-pushes.
   - Migrations: each file adds its own migration on top of the previous one. Never regenerate or edit a lower file's migration.

---

## §0. Shared rules (every file)
- **Branches stack** (see "How to run" above). File 01 branches from `origin/acceptatie`. Every later file branches from the **previous file's branch**, never from `acceptatie` again.
- **ONE PR per file, always into `acceptatie`.** The PR body starts with `Stacked on #<previous PR number> (<previous branch>)`. File 01 says `Stacked on: none`.
  - The PR diff includes the lower PRs until they merge. Say so in the body, and say which commits are this file's own.
- **Never** merge, deploy, or use rule `123` (`.cursor/rules/shortcut-123.mdc`). **Never** push to `main` or `acceptatie`. No force-pushes. See `docs/release-flow.md`.
- **Stop on red.** After each file run `dotnet build` and `dotnet test`. If anything fails and you can't fix it inside that file's scope:
  - **stop**, don't start the next file
  - push the branch, open the PR as **draft** with the failure in the body
  - report back
- Code references are from `origin/acceptatie` @ `20dc8d5b` (2026-09-29 08:56 CEST). Re-check line numbers before editing.
- **Mockups:** on branch `docs/mijn-paspoort`, folder `docs/mockups/mijn-paspoort/`. Read them with `git fetch origin docs/mijn-paspoort && git show origin/docs/mijn-paspoort:docs/mockups/mijn-paspoort/<file> > /tmp/<file>` and open `/tmp/<file>`. Don't commit mockups to your code branches.
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
    - If `--space-*` / `--text-*` / `--radius-pill` / `--z-*` are missing from `:root` in `app.css`, the **first** file that needs them (02) adds them exactly as listed in the design system. That PR syncs `app.min.css` and bumps its `?v=`.
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
  - the testresultaten done page (`TestDetail.razor`), except for reusing its extracted parts as described in file 03
- **PR description:**
  - What changed and why.
  - Screenshots desktop 1440 and mobile 390 of each new screen (flag ON), plus one of the unchanged classic profile (flag OFF).
  - The test list.
  - An "Out of scope / deferred" list.
- **Run** `dotnet build` and `dotnet test` (unit + bUnit). Run the Playwright suites you touched locally if you can, and mention it if you couldn't.

---

## §F. Feature-flag foundation (built in file 01, extended in 02)
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
- **Tests (in file 01):**
  - `FeatureGateFilter` returns 404 for a gated controller and 200 when the flag is on.
  - The Web gate redirects a gated page.
  - A reflection test lists every page and controller that carries `RequiresFeature`. The expected list lives in the test, so nothing gets gated or un-gated by accident.

---

---

## §N. Candidate nav: order and slots (refactor in 01, new order in 02)
There is one `BottomNav`; on desktop it is the main nav too, so this covers desktop and mobile. The design system allows **at most 5 items**.

**Slots, left to right:** `Discovery` (De ontdekkingsreis, **reserved**, filled in **08**) · `Passport` (Mijn Paspoort, or Profiel with the classic profile) · `Search` (Zoeken `/`) · `Applications` (Sollicitaties `/candidate/applications`) · `Career` (Carrière `/carriere`, **always last**).

| Paspoort flag | Werkgevers actief | Candidate nav (left → right) | Count |
|---|---|---|---|
| OFF | ON (default) | Zoeken · Bewaard · Sollicitaties · Carrière · Profiel: **exactly today** (D1) | 5 |
| OFF | OFF | Carrière · Profiel | 2 |
| ON | ON | [Ontdekkingsreis] · Mijn Paspoort · Zoeken · Sollicitaties · Carrière | 4 (5 after 08) |
| ON | OFF | [Ontdekkingsreis] · Mijn Paspoort · Carrière | 2 (3 after 08) |

[ ] = the reserved slot, **empty until file 08 lands**. Nothing is rendered for it and it leaves no gap.

- **Bewaard in the new order (D8).** Bewaard is no longer a nav item. It moves **inside Sollicitaties** as a tab: a small shared component `CandidateJobListTabs` with "Sollicitaties · Bewaard" at the top of `Applications.razor`, `Liked.razor` and `Shared.razor`.
  - The URLs stay the same (`/candidate/liked`, `/candidate/shared`). The Sollicitaties nav item is active on them (aliases). Any count badge the Bewaard item had moves to its tab.
  - The tab bar renders only when `RoleNavCatalog.ShowsSavedInNav(flags)` is false. That's one helper, no scattered checks, so the legacy order (paspoort OFF) is exactly today.
- **Implementation:**
  - `RoleNavCatalog.Candidate` becomes a **pure function** `RoleNavCatalog.CandidateItems(FeatureFlagSnapshot flags)` with an ordered slot list. It has a legacy branch (paspoort OFF) and the slot branch (paspoort ON), plus a named `CandidateNavSlot.Discovery` with no item, so file 08 only adds one entry.
  - `ForUser(user, flags)` passes the flags on. `BottomNav.razor` reads them from `IFeatureFlags`: on init and on `NavRefreshRequested`, **not** on every location change.
  - The Paspoort item keeps the active aliases `/candidate/profile`, `/profiel` and `/home`.
  - **File 01** does the pure-function refactor with today's order and hides the employer items. **File 02** adds the paspoort-ON order and the Bewaard tab.
- **Tests:** a unit test of `CandidateItems` for all 4 combinations (exact order, hrefs, count ≤ 5), plus a bUnit test that `BottomNav` renders that order. File 02 adds a test that `CandidateJobListTabs` shows only when Bewaard is not in the nav.

---

## Scope: what doesn't exist yet
| Item | Decision |
|---|---|
| Profile photo upload | **Deferred.** Initials avatar, no edit badge. |
| "Deel mijn paspoort" (sharing) | **Deferred.** Not rendered. Needs a public link + privacy design. |
| Stamps ("Mijn schalen") | **Built in 02, derived** from existing data (no storage, no dates). |
| Spoken languages + Dutch level | **Built in 06** (new fields), shown in the passport column; collected in the journey (07). |
| Hobbies, learning goals, employer preferences, dislikes | **Built in 06**, collected in 07. Dislikes are private (own table, never shown to employers). |
| Real lobster artwork | **Deferred.** 07/08 use the current mascot plus SVG shell plates behind a swappable component. |
| Removing the old onboarding wizard | **Deferred** until the paspoort flag is permanently ON (D13). |
| Real course / affiliate data | **Model + admin in 03.** The list ships empty; admin curates (`ShowInPassport`). No fake providers anywhere; the mockup providers live only in test fixtures. |
| Member number | Built in 02 as a non-PII display hash. |
| "Nog {x} vragen tot je volgende schaal" | Built in 02 from existing answered/total counts. |

## Decisions (defaults applied; Dennis can override any of them)
- **D1 (was O1).** Paspoort OFF keeps today's nav order exactly. The new order starts only when the paspoort flag is ON.
- **D2 (O2).** Werkgevers OFF also blocks the Salesmanager, Ambassadeur, Werven and Partner portals, because they exist for employer acquisition.
- **D3 (O3).** Werkgevers OFF: `ExternalVacanciesController` (the API-key vacancy import for integrators) returns **404** `feature_disabled`, like every gated endpoint.
- **D4 (O4).** Paspoort ON: candidate `/home` redirects to `/candidate/paspoort`.
- **D5 (O5).** A course block with no curated free option is **hidden**. It never shows only a paid partner link.
- **D6 (O6).** Werkgevers OFF: the candidate nav is Mijn Paspoort (or Profiel) + Carrière until 08 adds De ontdekkingsreis. Accepted.
- **D7 (O7).** The seeded course providers are cleaned up in the small file **01b**.
- **D8 (O8).** Max 5 nav items. With Werkgevers ON: Ontdekkingsreis · Mijn Paspoort · Zoeken · Sollicitaties · Carrière, and Bewaard becomes a tab inside Sollicitaties. With Werkgevers OFF: Ontdekkingsreis · Mijn Paspoort · Carrière.
  - Override option: Bewaard as a filter in Zoeken instead.
- **D9.** The new fields (06) and the journey (07/08) exist only when the paspoort flag is ON. With the flag OFF, the classic profile and the old onboarding stay exactly as they are and show none of the new fields. The stored data is kept either way.
- **D10.** Employer preferences are self-knowledge only for now. They are not shown as a fit result, because there is no employer-side data to compare against.
- **D11.** De ontdekkingsreis uses the **same paspoort flag**. ON: it replaces the onboarding (`/candidate/start` redirects to `/candidate/ontdekkingsreis`, and wizard state migrates to v3). OFF: the old onboarding stays exactly as it is, and v3 state maps back to v2 on read. No separate flag.
- **D12.** Design exception: the journey's scene layer (the background illustration) may use gradients built from `color-mix()` of existing tokens. All UI (cards, buttons, text) stays flat. Override option: stepped flat bands.
- **D13.** The old `OnboardingWizard.razor` is redirected, not deleted, in this stack. It is removed in a later cleanup once the paspoort flag is permanently ON.
- **D14.** Dislikes only down-rank candidate-side vacancy lists where real vacancy data exists (today only night shifts, `LegalNightShift23To06`). They never hide vacancies, and never change any score employers see.

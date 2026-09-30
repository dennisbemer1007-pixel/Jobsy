# 02. Foundation: dependency cases, strings, label catalog, CSS, shared card parts, gating, small UX fixes

Read `00-README.md` first (§0, §S, §F, D2, D7, D9, D10, Dependencies A–G). Branch `cursor/kandidaat-banen-2` from `cursor/kandidaat-banen-hotfix` (or from `origin/acceptatie` when PR 01 is merged, Dependencies G).

> **Rules (same as README §0, repeated on purpose):**
> - Never merge, never deploy, never use rule `123` (`.cursor/rules/shortcut-123.mdc`).
> - Never push to `main` or `acceptatie`; push only this file's `cursor/kandidaat-banen-*` branch; no force-push.
> - ONE stacked PR into `acceptatie`.
> - Red tests or an unmet success criterion: push, open the PR as **draft**, stop and report. Don't start the next file.
> - No visible redesign of whole pages yet: this file builds the shared parts and the small fixes. Don't touch the nav, employer scores or other stacks' entry screens.

| | |
|---|---|
| Branch | `cursor/kandidaat-banen-2` |
| PR title | `feat(kandidaat): foundation — dependency cases, Kb strings + labels, shared card parts, gating, small UX fixes` |
| PR body starts with | `Stacked on #<PR 01> (cursor/kandidaat-banen-hotfix)` (or `Stacked on: none (PR 01 already merged)`) + the **Dependencies A–G outcome table** |
| Mockups | the card parts are visible in every mockup: fit pill + why line (`kd-d1` side list, `kd-d2`), badge row (`kd-d2`, `kd-d5`), travel time (`kd-d1`, `kd-m3`) |
| Split seam | **02a** = 02.2–02.5 (cases, strings, labels, CSS, card parts). **02b** = 02.6–02.9 (focus, strings cleanup, category colour helper, seed photos, gating) |

## Goal
Every later file builds on one set of strings, labels, CSS and card parts, and the dependency cases are known and written down. The small review fixes that aren't tied to one screen are done: the focus box, hardcoded Dutch, inline colours and duplicate seed photos.

## 02.1 Today (verify first)
- Strings: modules like `Localization/UiStringsMatch.cs` (`MergeAll(nl, en, pl, ro, ar)` with `Add(key, nl, en, pl, ro, ar)`), registered in `UiStrings.cs`. Parity: `LocalizationParityReportTests`.
- Status labels: `Localization/UiLabels.cs` (~L44–50 `Apps.Status.*`: "Open", "Geaccepteerd", "Afgewezen", "Werkgever neemt contact op", "Aangenomen", "Ingevuld elders", "Ingetrokken").
- Feature CSS: `wwwroot/css/features/` has `banenkaart.css`, `applications.css`, `match-desktop.css`, … linked in `Components/App.razor` with `?v=` and listed in `Jobsy.Tests/asset-versions.json`.
- Hardcoded Dutch:
  - `VacancyDiscovery.razor` ~L551/L561: "Legenda verbergen" / "Legenda" / "Legenda tonen"
  - ~L1272: "Laden…"
  - ~L1274–1275: "Verberg mijn vacatures" / "Toon mijn vacatures"
  - `VacancyDetail.razor` ~L284: "Video laden"
  - `Shared/PageContentSkeleton.razor` ~L89: default `LoadingLabel = "Laden…"`
  - the intermediary error strings in the vacancy/intermediary code paths (`git grep -n "\"[A-Z][a-z].* [a-z]" -- Jobsy.Web/Components | grep -i intermedi`)
- Inline colours:
  - `style="--category-color:@cat.ColorHex"` in `VacancyDiscovery.razor` ~L332, ~L558, ~L572, ~L798
  - `VacancyDetail.razor` ~L127
- Focus: `Components/Routes.razor` L15 `<FocusOnNavigate RouteData="routeData" Selector="h1" />`. After navigation, the `h1` gets focus and shows a visible box around the page title (review screenshots).
- Seed photos: `ImageUrl` is set in `WestlandVacanciesSeeder.cs`, `HaaglandenVacanciesSeeder.cs`, `EnterpriseWestlandVacanciesSeeder.cs`, `DemoCompaniesSeeder.cs`, `ApplicationsAndWagesSeeder.cs`, `Sprint8MetricsSeeder.cs` and `MediaBackfillSeeder.cs`. Neighbouring cards on the map/list show the same photo.
- Gating (Dependencies C): if paspoort 01 has landed, check which candidate job pages/APIs are already gated (`FeatureRouteGate`, `[RequiresFeature]`, the reflection expected-list test).

## 02.2 Dependency cases + routes
- Run the checks A–G (README). Create `docs/features/kandidaat-banen.md`: one table with dependency → case (present/absent) → what this stack does. Later files update it.
- `Jobsy.Web/KandidaatBanen/KbRoutes.cs`:
  - `Map` = `PublicRoutes.Banenkaart` (E present) or `"/"` with `// KB-FALLBACK(E)`
  - `Applications = "/candidate/applications"`, `Saved = "/candidate/liked"`, `Shared = "/candidate/shared"`
  - `PaspoortTests` = the paspoort test entry the paspoort uses for culture/values (if paspoort 02 has landed), otherwise today's culture/values test routes
  
  Replace string literals for these routes in the files this stack touches.

## 02.3 Strings + label catalog
- `Localization/UiStringsKandidaatBanen.cs` with prefix `Kb.`, all 5 languages, registered in `UiStrings.cs`. Add keys as each file needs them; this file adds:
  - the fit texts: `Kb.Fit.Percent` ("{0}% past bij jou"), `Kb.Fit.Strong` ("Sterke match"), `Kb.Fit.Gate` ("Maak je paspoort af"), `Kb.Fit.GateHint` ("Doe de cultuur- of waardentest, dan zie je hoe goed banen bij je passen.")
  - `Kb.Rank.Lower` ("Staat lager: {0}")
  - `Kb.Travel.Minutes` ("{0} min {1}"), `Kb.Travel.Approx` ("ongeveer"), `Kb.Travel.ToBureau` (intermediair D5 text; reuse that stack's key when A is present)
  - `Kb.Badge.More` ("+{0}")
  - the strings moved in 02.7
- `Jobsy.Web/KandidaatBanen/KbLabels.cs`: one place for every enum shown to candidates:
  - `ApplicationStatus` pills (candidate wording): Pending "Verstuurd", Accepted "In behandeling" (as today, `Apps.Wizard.InReview`), EmployerContacting "Uitgenodigd" (intermediair D4 wording), Hired "Aangenomen", Rejected "Niet gekozen", FilledElsewhere "Vergeven aan iemand anders", Withdrawn "Ingetrokken". Today `Applications.razor` `StatusPillLabel` (~L552) + `PillModifier` do this inline; 07 moves them here
  - the timeline steps (07)
  - the saved-job state (07)
  - transport modes (Fiets/Auto/Lopen/OV via the existing keys)
  - `FitBand`

  Keep `UiLabels` for the employer side unchanged. Reflection test: every value has a label in all 5 languages.

## 02.4 CSS + shared card parts
- `wwwroot/css/features/kandidaat-banen.css` (block `kb-`), linked in `App.razor` (list + `<noscript>`), `?v=YYYYMMDD-kb`, in `asset-versions.json`.
- Components in `Components/KandidaatBanen/`:
  - `KbFitPill`: input `KbFitView` (`bool GateOpen`, `int? Percent`, `FitBand? Band`).
    - Gate closed → a neutral pill "Maak je paspoort af" linking to `KbRoutes.PaspoortTests`.
    - Open → "{n}% past bij jou" with the band class (`kb-fit--strong` ≥ 75, `--good`, `--some`), the sparkle icon as in the mockups, and the colour **and** the text.
  - `KbWhyLine`: one line, sparkle icon, never empty (renders nothing without text).
  - `KbBadgeRow`: takes ordered badges and renders at most `MaxVisible` (default 2 including the fit pill, D7) + "+n" with an accessible label listing the hidden ones.
  - `KbTravelTime`: the mode icon + "{n} min" + the mode word, with variants:
    - `Approx` (circle fallback, adds "ongeveer")
    - `ToBureau` (hidden mode, D3/A)
    - no data → renders nothing
- bUnit tests for each part: gate closed/open, the band boundary (74 → good, 75 → strong), the badge overflow ("+2"), the travel variants, and no enum names rendered.

## 02.5 Focus box after navigation
- Keep `FocusOnNavigate` (screen readers need it). Remove the **visible** box on the programmatically focused page title only: `h1[tabindex="-1"]:focus:not(:focus-visible) { outline: none; }`. Put it next to the existing global focus rules (if those only live in `app.css`, this single rule goes there as the documented exception; say so in the PR).
- Make sure `FocusOnNavigate` still focuses the `h1`, and that keyboard focus on real controls keeps its visible ring.
- Test: a static CSS guard + a Playwright check (desktop 1440) that after an enhanced navigation `document.activeElement` is the `h1` and its computed `outline-style` is `none`.

## 02.6 Category colour helper
- `KbCategoryColor.Style(string? hex)` → `"--category-color:#rrggbb"` for a valid 6-digit hex, otherwise `"--category-color:var(--muted)"`. Replace the five inline `style="--category-color:@…"` sites with `style="@KbCategoryColor.Style(cat.ColorHex)"`.
- Guard test: in `VacancyDiscovery.razor`, `VacancyDetail.razor`, `Applications.razor`, `Liked.razor`, `Shared.razor`, `MatchPage.razor` and `Components/KandidaatBanen/**`, every `style="` attribute is exactly `style="@KbCategoryColor.Style(...)"`.

## 02.7 Hardcoded strings → resources
- Move every string listed in 02.1 to `Kb.*` (or an existing key with the same text). `PageContentSkeleton.LoadingLabel` defaults to `Culture["Common.Loading"]` (reuse it if it exists).
- JS: legend/popup texts in `jobMap.js` that are Dutch literals come in through the map init options (`labels` object built from `Culture`), not literals. List them in the PR; move the ones on the banenkaart path (the rest is for 03).
- Guard test: no Dutch literal in the markup of the files listed in 02.6. The check is a small regex list of common Dutch words between `>` and `<` and in `title=`/`aria-label=` attributes, with an allow-list for brand names.

## 02.8 Duplicate seed photos (seed only)
- In the vacancy seeders, assign photos so that **no two seeded vacancies of the same company share an image**, and neighbouring seeded vacancies (same town + same category) rotate through the available category photos deterministically (by a stable index), with no `Random`.
- Existing seeded rows: only if seeded vacancies can be identified safely (seed company ids / seed markers the seeders already use), let the seeder's idempotent update path reassign `ImageUrl` for those rows. Never touch vacancies of real employers or photos an employer uploaded. If there is no safe identifier, change only the seeders (fresh databases) and say so.
- The app-side fallback (`VacancyPhoto` with `WorkType`) is unchanged.
- Test: run the seeders into the test database and assert there is no duplicate `ImageUrl` per company, and that seeded vacancies within 1 km of each other with the same category never share one.

## 02.9 Gating (Dependencies C)
- **Flags present:** make sure the candidate job pages this stack touches are behind Werkgevers actief: `VacancyDiscovery` host page(s), `VacancyDetail`, `Applications`, `Liked`, `Shared`, `MatchPage`. The same goes for their candidate APIs (vacancy list/detail for candidates, `api/applications/mine…`, liked/shared, match deck, travel isochrones for the banenkaart).
  - Use `FeatureRouteGate` / `[RequiresFeature(PlatformFeature.Employers)]` / `.RequireFeature(...)`.
  - Add anything missing to the expected-list test. What paspoort 01 already gates stays as it is.
  - Anonymous `/banenkaart` follows landing 04's rules (landing 07 = the page without employers).
- **Flags absent:** no code; note it in `docs/features/kandidaat-banen.md` and the PR.
- Test (flags present): with Werkgevers OFF, every listed API returns 404 `feature_disabled` and every listed page renders the gate's page.

## Tests
- bUnit: the card parts (02.4), no enum names, and `KbLabels` completeness in 5 languages
- guards: the focus CSS, the inline-style helper, no hardcoded Dutch, `AssetVersionGuardTests`
- seeder photo uniqueness
- the gating expected-list (flags present)
- `LocalizationParityReportTests`, `dotnet build`, `dotnet test` green

## Success criteria
- PR 02 contains the Dependencies A–G outcome table, and `docs/features/kandidaat-banen.md` exists.
- No visible focus box around page titles after navigation; keyboard focus rings still visible.
- The strings from 02.1 render in all 5 languages; no inline `style=` except the helper.
- Seeded cards next to each other show different photos.
- Werkgevers OFF hides every candidate job page/API listed in 02.9 (or the fallback is documented).

Done → next: `03-banenkaart.md`.

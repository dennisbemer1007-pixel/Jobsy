# Kandidaat polish (banenkaart filters + popup, Bewaard list, passport nav, Carrière on one screen): Cursor run book

Cursor: **read this file completely**, then **execute the files below strictly in order**, one at a time. Each file is one PR.

> **Global rules (they apply to every file):**
> - **Never merge. Never deploy. Never use rule `123`** (`.cursor/rules/shortcut-123.mdc`).
> - **Never push to `main` or `acceptatie`. Never force-push.** Push only the current file's `cursor/kandidaat-polish-*` branch.
> - ONE PR per file into `acceptatie`. 01 is a standalone hotfix; 02–07 are stacked.
> - **Tests red, or a success criterion that can't be met:** push, open the PR as **draft**, stop and report. Don't start the next file.
> - **Release build with 0 warnings.** `TreatWarningsAsErrors` is on (`Directory.Build.props`, code-health 11; .NET 10, `AnalysisLevel` 10.0-recommended), so any warning fails `dotnet build -c Release`. Run `dotnet format` and `.github/scripts/count-build-warnings.sh` before opening the PR. No blanket `<NoWarn>`.
> - Run the **full** test suite (`dotnet test -c Release`), not only the new tests.
> - Add **Playwright and/or bUnit checks at 390×844** wherever a file changes mobile layout. Playwright suites soft-skip without `JOBSY_E2E_BASE_URL`, and that is fine. bUnit and CSS guard tests must always run.
> - **Report per PR**, using the template at the bottom of this file.

**Source:** Dennis tested acceptatie on his phone on 02-10, and on 02-10 at 17:02 he approved all mockups and fixes in this stack. The findings were made on `origin/acceptatie` `3a15b0d7` and the line references re-checked on `f439b186` (after the code-health merge, #516).

**What this stack fixes:**
- **01: a layout bug introduced by `28f3aba4` (PR #450).**
  - On mobile, `.jobsy-discovery` uses `grid-template-columns: 1fr`. A plain `1fr` column grows to fit its widest child, and the filter chip row is 662 px wide.
  - So the whole column becomes 662 px, and `.app-main { overflow-x: hidden }` cuts it off at 390 px.
  - Result: the chip row can't scroll. **Loon, Meer filters, Lijst/Kaart and Match are unreachable**, the map is off-centre, and the cluster sheet and the "328 vacatures" pill are cut off on the right.
  - This one bug explains "I miss the filters" and "I don't see Match anymore".
- **02 + 03: filters.**
  - A clear filter bar per mockup A: search, "Filters (n)", travel-time preset, Match, Lijst/Kaart.
  - A filter sheet per mockup B with **all** old filters. The old sheet still exists in `VacancyDiscovery.razor` but was hidden behind the off-screen "Meer filters" chip.
- **04: map popup / bottom sheet.** Redesigned per mockup A, without the fixed 223 px height. **Thinner rings.**
- **05: Bewaard.** A compact list on mobile (4+ rows per screen, mockup C) and smaller tiles on desktop.
- **06: the passport layout is ON BY DEFAULT everywhere, including production** (Dennis, 02-10 17:09).
  - `CandidatePassportEnabled` defaults to `true` in code.
  - A migration sets the column default to `true` and flips the existing stored `false` values once. Those were never a real choice: the switch showed a raw key.
  - The admin switch stays, so it can be turned off, and then it stays off.
  - Nav order: Ontdekkingsreis · Mijn Paspoort · Zoeken · Sollicitaties · Carrière.
  - Bewaard becomes a tab in Sollicitaties. Add the missing admin strings.
  - Match stays a button on the map.
- **07: Carrière fits on one mobile screen** (carrière mockups).

## Mockups (`docs/mockups/kandidaat-polish/`, 390 px wide, @2x, sample data only)
| File | Shows | Used by |
|---|---|---|
| `bk-a-kaart-popup.png` / `.html` | Banenkaart mobile: filter bar (search + "Filters (2)", "20 min" preset, Match, Lijst), thinner rings, selected cluster (coral), bottom sheet "2 banen in Honselersdijk" with 1/2 paging, close, photo, match, travel/hours/pay, "Waarom" line, "Bekijk deze baan" + heart | 02, 04 |
| `bk-b-filters.png` / `.html` | Full filter sheet with all old filters + Volgorde, "Annuleren" / "Toon 34 banen" (full-height capture of the scrolled sheet) | 03 |
| `bk-c-bewaard.png` / `.html` | Bewaard mobile compact list: tabs Bewaard/Gedeeld, chips Alles/Nog open/Gesloten, count + styled sort, rows with 72 px thumbnail, title, company · place, match %, open/closed, heart | 05 |
| `carriere-v2-mobiel.png` / `.html` | Carrière mobile in one screen: hero with numbers in stones, bubble inside hero, stepper Nu/Stap n/Doel, pencil in the card header, "Nu aan de beurt" collapsed with "Meer", AI line | 07 |
| `carriere-v2-mobiel-meer-open.png` / `.html` | Same, "Meer" open (4 lines incl. "Wat je al hebt"); only here may the page scroll | 07 |

The mockups show the look. In two places they differ from the spec, and **the spec wins**:
- Mockups A/B/C already show the new nav order (06). The carrière mockups still show the old nav. Follow **06** for the nav.
- The "Voorbeelddata" tag in the mockups is not part of the UI.

## Order

| # | File | Branch | Branches from | PR into |
|---|---|---|---|---|
| 01 | `01-hotfix-mobiele-overflow.md`: **standalone hotfix**. `minmax(0,1fr)` + `min-width:0` so the chip row scrolls, cluster sheet + count pill fit, Playwright no-overflow check at 390 | `cursor/kandidaat-polish-hotfix` | `origin/acceptatie` | `acceptatie` |
| 02 | `02-filterbalk.md`: filter bar per mockup A (search + "Filters (n)", travel preset chip 10/20/30 + mode, Match button, Lijst/Kaart), styled address input | `cursor/kandidaat-polish-2` | `cursor/kandidaat-polish-hotfix` (or `origin/acceptatie` when PR 01 is merged) | `acceptatie` |
| 03 | `03-filtersheet.md`: filter sheet per mockup B with all old filters + Volgorde, "Toon n banen" / "Annuleren" | `cursor/kandidaat-polish-3` | `cursor/kandidaat-polish-2` | `acceptatie` |
| 04 | `04-popup-ringen.md`: map popup + bottom sheet per mockup A, no fixed height, thinner rings | `cursor/kandidaat-polish-4` | `cursor/kandidaat-polish-3` | `acceptatie` |
| 05 | `05-bewaard.md`: compact Bewaard list < 640 px, 4 tiles per row on desktop, styled sort | `cursor/kandidaat-polish-5` | `cursor/kandidaat-polish-4` | `acceptatie` |
| 06 | `06-paspoort-nav.md`: passport layout **ON by default everywhere incl. production** (code defaults `true` + migration `SetCandidatePassportDefaultOn` that flips stored `false` once; admin switch can still turn it off), nav order, Bewaard as Sollicitaties tab + redirects, missing admin strings (5 languages) | `cursor/kandidaat-polish-6` | `cursor/kandidaat-polish-5` | `acceptatie` |
| 07 | `07-carriere-een-scherm.md`: Carrière on one mobile screen per carrière mockups, Playwright no-scroll check | `cursor/kandidaat-polish-7` | `cursor/kandidaat-polish-6` | `acceptatie` |

If a file grows past ~1,500 changed lines (not counting tests and resources), split it into `a`/`b` at a natural seam. The next file then branches from the **last** sub-branch.

## Pointer prompt (runs 01 … 07)
```
Run the Kandidaat polish stack. First: git fetch origin && git show origin/docs/kandidaat-polish:docs/kandidaat-polish/00-README.md — read it completely.
Then read and execute each file in docs/kandidaat-polish/ on that branch strictly in the README's order (01 … 07), one file = one PR. Mockups are in docs/mockups/kandidaat-polish/ on the same branch.
File 01 is a standalone hotfix: branch cursor/kandidaat-polish-hotfix from origin/acceptatie, PR body starts with "Standalone hotfix (not stacked)". If PR 01 already exists, reuse its branch.
File 02 branches from cursor/kandidaat-polish-hotfix (or origin/acceptatie if PR 01 is merged); every later file branches from the previous file's branch. Each opens ONE PR into acceptatie whose body starts with "Stacked on #<prev PR>".
After each file: dotnet build -c Release with 0 warnings and the full dotnet test -c Release; add the 390x844 Playwright/bUnit checks the file asks for. If anything is red or a success criterion can't be met: push, open the PR as draft, stop and report.
Never merge, never deploy, never use rule 123, never push to main or acceptatie, never force-push.
Report per PR (README template), and at the end: file → branch → PR number → status.
```

### Pointer prompt: hotfix 01 only
```
Run only file 01 of the Kandidaat polish stack. First: git fetch origin && git show origin/docs/kandidaat-polish:docs/kandidaat-polish/00-README.md and git show origin/docs/kandidaat-polish:docs/kandidaat-polish/01-hotfix-mobiele-overflow.md — read both completely.
Branch cursor/kandidaat-polish-hotfix from origin/acceptatie, ONE PR into acceptatie whose body starts with "Standalone hotfix (not stacked)". Release build 0 warnings, full tests, the Playwright no-overflow check at 390x844. Red → draft PR, stop, report.
Never merge, never deploy, never use rule 123, never push to main or acceptatie, never force-push. Don't start file 02.
```

## How to run
1. `git fetch origin`. Read this file, `.cursor/rules/design-system.mdc`, `docs/ROUTES.md` and `docs/release-flow.md`.
2. For each file in order:
   1. Read the whole file and open the mockups it names.
   2. Create its branch from the "Branches from" column.
   3. Implement **only** that file's scope.
   4. Run `dotnet build -c Release` (0 warnings) and `dotnet test -c Release` (full suite). Add the file's 390×844 checks. Run the Playwright suites it names if `JOBSY_E2E_BASE_URL` is available.
   5. Make small, clear commits. Run `git push -u origin <branch>`, never with `--force`.
   6. Open ONE PR into `acceptatie` with the file's title. The body starts with `Standalone hotfix (not stacked)` (01) or `Stacked on #<prev PR> (<prev branch>)` (02+).
3. **Stop and report** (draft PR) when tests are red, the build has warnings, a success criterion can't be met, or the code differs so much from the "Files to touch" list that the file's approach no longer fits.

## Shared rules for all files
- **Language:** all candidate UI text is plain Dutch at B1 level, in short sentences. Every new string goes into the existing UiStrings module for its area (`UiStringsKandidaatBanen.cs` for banenkaart/Bewaard, the career strings module for Carrière, `UiStringsAdmin.cs` for admin) in **nl/en/pl/ro/ar**. No hardcoded text in Razor or JS. RTL (`ar`) must not break the layout.
- **Design system:**
  - Use tokens only: navy `--brand` `#0f2d5c`, `--coral` for the selected pin, `--gold*` for Match, the ring colours `--map-iso-line` / `--map-iso-fill`. No inline colour `style=`.
  - Use feature CSS files: `features/kandidaat-banen.css`, `features/banenkaart.css`, `features/carriere.css`. Edit `app.css` only where a rule already lives there.
  - Rebuild `app.min.css` / `*.min.js` and update `Jobsy.Tests/asset-versions.json` the way the repo already does.
- **Touch targets** are at least 44×44 px on mobile, and every control has a visible focus style.
- **No horizontal overflow at 390 px** on any page touched by this stack.
- **Feature flags:**
  - Everything respects `EmployersEnabled`, as the current code does.
  - The nav and the passport default only change in 06. From 06 on, the passport layout is ON by default.
  - 01–05 and 07 must work with the passport flag **both ON and OFF**, because an admin can still turn it off.
- **Do not change** server-side match scores, the isochrone service, or employer-side views.
- **Existing tests** that assert old values (e.g. `BanenkaartClusterCardCssTests` asserting `--map-popup-sheet-h: 223px`, nav-order assertions in `FeatureFlagFoundationTests` / `RoleFunctionalRegressionTests`) are **updated on purpose** in the file that changes the behaviour. Say so in the PR body. Never delete a test to make the build green.

## Per-PR report template
```
PR #<n> <title> — <branch> (from <base>) — draft? yes/no
Build: Release, warnings: 0 | Tests: <passed>/<total> (full suite) | Playwright: ran/soft-skipped
Done: <bullets per success criterion, ✓ / ✗>
Tests added/changed: <names>, incl. 390x844 checks
Screenshots: 390x844 before/after (attach)
Deviations from the spec + why; follow-ups
```

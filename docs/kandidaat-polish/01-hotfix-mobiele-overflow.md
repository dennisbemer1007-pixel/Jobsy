# 01: Hotfix: mobile banenkaart overflow (chip row, cluster sheet, count pill)

**Standalone hotfix (not stacked).**
- Branch: `cursor/kandidaat-polish-hotfix` from `origin/acceptatie`.
- ONE PR into `acceptatie`, titled **"fix(banenkaart): mobile grid overflow hides filters, Match and Lijst"**.
- Rules: see README. Never merge or deploy, never use rule 123, never push to main/acceptatie, no force-push, draft PR when red. Release build with 0 warnings, full tests.

## Problem (found on acceptatie `3a15b0d7`, still on `f439b186`)
Measured live at 390×844 as an anonymous visitor:
- `.kb-filter-chips` (added in `28f3aba4`, PR #450) is **662 px** wide. So are `.jobsy-chrome`, `.kb-chrome-search`, `.map-pane` and `#job-map`.
- The parent `.jobsy-discovery.show-map` is a grid. The mobile override in `Jobsy.Web/wwwroot/css/app.css` (~L4884, inside the mobile media block) sets:
  ```css
  .jobsy-discovery, .funda-layout { grid-template-columns: 1fr; grid-template-rows: auto 1fr; }
  ```
  `1fr` means `minmax(auto, 1fr)`, so the column grows to the chip row's natural width (661.78 px). `.jobsy-chrome` has `min-width: auto`, and `.app-main { overflow-x: hidden }` clips everything at 390.
- Effects:
  - The chip row never scrolls. **Loon, Meer filters, Lijst/Kaart and Match (x ≥ 400) can't be reached.**
  - The map centre is off-screen to the right.
  - The cluster sheet `.map-cluster-sheet` uses `inset-inline: 8px` of a 662 px pane, so it is cut off on the right.
  - The "{count} vacatures in beeld" pill `.map-cluster-chip` (`jobMap.js` ~L1105–1112) is centred on 662 px and is cut off.
- Logged-in candidates get one more chip (Match), so for them the row is even wider.

## Fix
1. In `app.css` (mobile override ~L4884), use `grid-template-columns: minmax(0, 1fr);` for `.jobsy-discovery, .funda-layout`. Check the other mobile discovery overrides (~L5170/5174) and apply the same where they set `1fr` on these grids.
2. Set `min-width: 0` on `.jobsy-chrome` (mobile), and on `.kb-chrome-search` and `.map-pane` if they are grid/flex children that can still grow past the column.
3. `.kb-filter-chips` (`features/kandidaat-banen.css` ~L218–330) must be a real horizontal scroller:
   - `overflow-x: auto; overscroll-behavior-x: contain; scroll-snap-type: x proximity;`
   - no scrollbar on touch
   - a soft fade at the end edge (logical `inset-inline-end`, so it works in RTL) that only shows while there is more to scroll
   - chips `flex: 0 0 auto`
4. Cluster sheet and count pill:
   - both are positioned against the **visible** pane width (390), never wider
   - the count pill is centred with `inset-inline: 0; margin-inline: auto; width: max-content; max-width: calc(100% - 24px)`
   - the cluster sheet keeps `inset-inline: 8px` of the now 390-wide pane
5. Once the map pane has its real size, MapLibre needs to know: call `map.resize()` after first layout. Check that `jobMap.js` already does this on `ResizeObserver`; add it if not. Without it the canvas keeps the old 662 px width.
6. Change **nothing else**: no redesign; 02–04 do that.

## Files to touch
- `Jobsy.Web/wwwroot/css/app.css` (mobile `.jobsy-discovery` override ~L4884, `.jobsy-chrome` mobile rule right below it) + `app.min.css`
- `Jobsy.Web/wwwroot/css/features/kandidaat-banen.css` (`.kb-filter-chips`, `.kb-chip` ~L218–330)
- `Jobsy.Web/wwwroot/css/features/banenkaart.css` (`.map-cluster-sheet` L20, mobile block L39–121) only if needed after the grid fix
- `Jobsy.Web/wwwroot/js/jobMap.js` (count pill ~L1105, `map.resize()` on resize) only if needed, + `.min.js`
- `Jobsy.Tests/asset-versions.json`
- Tests: new `Jobsy.Tests/BanenkaartMobileOverflowPlaywrightTests.cs` + a CSS guard (see below)

## Tests
- **Playwright, 390×844, anonymous AND logged-in candidate** (seeded candidate the E2E suite already uses), on `/banenkaart` in map and list mode:
  - `document.documentElement.scrollWidth <= 390`, and no element under `.app-main` has `getBoundingClientRect().right > 391`. The chip row's own scroll content is allowed past 390; its box is not.
  - `.kb-filter-chips` `clientWidth <= 390`, and `scrollWidth > clientWidth` (it scrolls).
  - Scroll the chip row to the end: **Meer filters**, **Lijst** and (logged in) **Match** are visible and clickable. Clicking "Meer filters" opens `#discovery-filters`.
  - `#job-map` width ≤ 390.
  - Open a cluster: `.map-cluster-sheet` right edge ≤ 390 − 8.
  - The count pill is fully inside the viewport.
- **CSS guard** (unit test reading `app.css`): the mobile `.jobsy-discovery` rule contains `minmax(0, 1fr)`, and `.jobsy-chrome` mobile has `min-width: 0`.
- Existing suites stay green: `BanenkaartStartAndFiltersPlaywrightTests`, `BanenkaartClusterCardPlaywrightTests`, `BanenkaartV3PlaywrightTests`, `KandidaatBanenE2ePlaywrightTests`.

## Success criteria
- At 390×844, no horizontal overflow on `/banenkaart` (map and list), anonymous and logged in.
- Every chip in the row can be reached by horizontal swipe. Match and Meer filters work.
- The map is centred on the start point. The cluster sheet and count pill are fully visible.
- Desktop (≥ 900) looks exactly as before. Attach 1440 before/after screenshots.
- Release build with 0 warnings, full tests green.

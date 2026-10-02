# 07: Carrière fits on one mobile screen

**Stacked.**
- Branch: `cursor/kandidaat-polish-7` from `cursor/kandidaat-polish-6`.
- ONE PR into `acceptatie`, titled **"feat(carriere): mobile overview fits one screen (hero numbers, compact stepper, Meer)"**.
- Rules: see README.

Mockups:
- `docs/mockups/kandidaat-polish/carriere-v2-mobiel.png`: collapsed, one screen
- `docs/mockups/kandidaat-polish/carriere-v2-mobiel-meer-open.png`: "Meer" open; only then may the page scroll

The mockups still show the old bottom nav. The nav comes from 06.

## Why
On a phone the Carrière overview (`/carriere`, `CareerDashboard.razor` → `CareerStage` → `CareerOverviewCard`) is much taller than one screen:
- **Stone labels:** the mobile hero renders full text labels under every stone (`.career-scene__label`, `carriere.css` ~L142–161). They have `min-width: 96px` and `inset-inline-start: var(--x)`, so the label's *left edge* sits at the stone centre. The last mobile anchor is x = 356 of 390 (`CareerClimbGeometry.MobileAnchors`), so labels run past the right edge.
- **Speech bubble:** `LobsyBubble` is rendered **below** the scene in `.career-band__say` (`CareerStage.razor` ~L29–31), which adds a whole block of height.
- **Stepper:** `GrowingShellsStepper` is always rendered with `Mobile="false"` (`CareerOverviewCard.razor` ~L49), so it can scroll horizontally on a phone.
- **Pencil:** the edit-dream pencil is `career-only-desktop` in the card header (~L11). On mobile, a separate floating `.career-mobile-bar` (`CareerDashboard.razor` ~L155, CSS `carriere.css` ~L1081 and ~L1109) is sticky at `bottom: 72px` and covers content.
- **Card body:** "Nu aan de beurt" (`.career-now`) always shows all facts, and "Wat je al hebt" (`.career-have`) is a separate full block.

## Scope (mobile < 900 px; desktop unchanged unless noted)
1. **Hero: numbers in the stones only.**
   - Each stone shows a short mark *inside or on* the stone: **Nu**, **1…n**, and the goal as a **gold star stone**.
   - No full text labels under the stones on mobile; the full titles stay in the stepper and the card.
   - Labels are **centred** on the stone (`transform: translateX(-50%)`, or compute the centre in `CareerClimbScene`), with `max-inline-size: 56px`, and **clamped inside the band**: `inset-inline-start: clamp(28px, var(--x), calc(100% - 28px))`.
   - In RTL the SVG is mirrored (`[dir="rtl"] .career-scene__svg { transform: scaleX(-1) }`). Mirror the label positions the same way so they still sit on their stones.
   - Keep `CareerClimbGeometry` mobile anchors unless a label can't fit. If you change them, update `CareerClimbGeometryTests` on purpose and keep `MobileMinDistance` ≥ 44.
   - Hero height about **180 px** on mobile (`.career-scene--mobile`, today 150 px plus the bubble below).
2. **Speech bubble inside the hero.**
   - `LobsyBubble` (`Variant=Scene`) is positioned absolutely in the top part of the hero (as in the mockup: top ~12 px, inline-start ~16 px, max-inline-size ~250 px), not below it.
   - Remove the `.career-band__say` block on mobile.
   - The bubble never covers the "Nu" stone or the lobster. If it would, shorten it with `BubbleMobile` (the short text) and `line-clamp: 2`.
   - The bubble's base style `.passport-bubble--scene` lives in `features/tests.css` ~L308. Override it in `carriere.css` instead of changing the shared rule.
3. **Card overlaps the hero slightly** (negative top margin ~16 px, radius 16) per the mockup, so the page reads as one unit.
4. **Card header:**
   - eyebrow "MIJN CARRIÈRE · STAP n VAN m"
   - title "Naar {droom}"
   - the **pencil** as a 44×44 round icon button at the end of the title row, **on mobile too**: drop `career-only-desktop` and use an icon-only variant on mobile, with the `aria-label` from `Career.EditDream`
   - **Remove the floating `.career-mobile-bar`** from `CareerDashboard.razor` and its CSS
   - other actions in that bar must be reachable elsewhere in the card; list them in the PR
5. **Stepper in mobile mode with equal columns.**
   - `GrowingShellsStepper` gets `Mobile="true"` below 900. Either render both and toggle them with `career-only-mobile` / `career-only-desktop`, or use one CSS-driven variant.
   - Mobile layout: `grid-template-columns: repeat(N, minmax(0, 1fr))`, shells 36 px, captions **Nu / Stap 1 … Stap n / Doel** (12 px, one line, no ellipsis needed).
   - The current step's caption is bold navy, its shell dashed navy. Done steps show a check, the goal a gold star.
   - **No horizontal scroll** at 390 for up to 6 stones (Nu + 4 steps + Doel). With more, captions shorten to numbers only.
6. **"Nu aan de beurt" collapsed by default.**
   - Collapsed: the eyebrow "NU AAN DE BEURT", the step title, **one summary line** ("Nog 2 klauwen · 3 opleidingen", built from the existing facts), a **"Meer ▾"** text button (start) and the primary **"Bekijk stap n ›"** (end).
   - Expanded ("Minder ▴"), up to 4 lines, each with an icon and a muted subline:
     - the claws fact (`Career.Fact.Claws`) + gap names
     - the courses fact (`Career.Fact.Courses`) + "Begin met de gratis online les" when there is a free course
     - the fit band fact (`Career.Fact.Band`)
     - **"Wat je al hebt: n dingen"** + the items, with the tag "in je paspoort" or "in je profiel" depending on `PassportOn`. This **replaces the separate `.career-have` block on mobile**.
   - The toggle is a real `<button aria-expanded aria-controls>`. State is per page view, not stored.
   - Desktop keeps today's layout, or uses the same component expanded by default; choose the simpler option and say which in the PR.
7. **The AI line stays visible**, under the card body, collapsed or not: `.career-card__source` ("Plan gemaakt met hulp van AI, op basis van je paspoort", or the local variant), small, muted, with an icon.
8. **Fits on one screen.**
   - At 390×844 with the bottom nav, from the top of the hero to the end of the AI line is **≤ 560 px**.
   - Collapsed, the page does **not scroll** for the common case: a plan with 4 steps, a 1-line title, no carried-over note, no language note.
   - Carried-over and language notes (`CareerOverviewCard` ~L40–47, ~L104+) may add height. Keep them compact (one line + action).
   - Only "Meer" open may make the page scroll.
9. Other states (`CareerStepDetailCard`, `CareerStepDoneCard`, the empty or dream picker) get the hero changes (1–3) automatically through `CareerStage`. Don't redesign their bodies here; check they have no horizontal overflow at 390.

## Files to touch
- `Jobsy.Web/Components/Pages/Candidate/CareerDashboard.razor` (remove `.career-mobile-bar` ~L155; `CareerStage` usages ~L66–150)
- `Jobsy.Web/Components/Candidate/Career/CareerStage.razor` (mobile band ~L18–32: scene + bubble inside the hero)
- `Jobsy.Web/Components/Candidate/Journey/CareerClimbScene.razor` (labels ~L20–36: short marks, centring and clamp, RTL)
- `Jobsy.Web/Components/Candidate/Journey/CareerClimbGeometry.cs` (mobile anchors and size L13–41, only if needed)
- `Jobsy.Web/Components/Candidate/Journey/CareerClimbSceneBuilder.cs` (stone marks inside the SVG, if the numbers are drawn there)
- `Jobsy.Web/Components/Candidate/Journey/CareerClimbModels.cs` (a short label field on the stone model, if needed)
- `Jobsy.Web/Components/Candidate/Career/GrowingShellsStepper.razor` (mobile mode, captions Nu/Stap n/Doel)
- `Jobsy.Web/Components/Candidate/Career/CareerOverviewCard.razor` (pencil ~L10–20; stepper ~L49; `.career-now` ~L70–85 collapsed/Meer; `.career-have` ~L88–101 moved under Meer on mobile; `.career-card__source` ~L103; `Facts` ~L139–166)
- `Jobsy.Web/Components/Candidate/Passport/LobsyBubble.razor` (only if a class or `line-clamp` hook is needed; the markup stays shared)
- `Jobsy.Web/wwwroot/css/features/carriere.css`:
  - `.career-scene--mobile` L47
  - `.career-scene__label*` L142–161
  - `.career-band*` L179–180
  - `.career-card__edit` ~L340
  - `.career-now*` ~L387–405
  - `.career-have*` ~L409–430
  - `.career-stepper*` L437–502
  - mobile blocks L1059–1107: drop `.career-mobile-bar` L1081 and L1109; drop the 140 px bottom padding on `.career-stage` that existed for the bar
- `Jobsy.Web/Localization/UiStringsCareer.cs`: "Meer", "Minder", "Nu", "Stap {n}", "Doel", the summary line format, "Wat je al hebt: {n} dingen", "Begin met de gratis online les", in 5 languages
- Tests:
  - `CareerPageBunitTests.cs`: ~L228–234 asserts `career-have` presence. Update it on purpose: on mobile it now lives under Meer.
  - `CareerDesignGuardTests.cs`, `CareerClimbGeometryTests.cs`, `CarrierePlaywrightTests.cs` (already has 390 contexts and `CareerE2e.AssertNoHorizontalOverflowAsync`)

## Tests
- **bUnit:**
  - the overview renders the stepper in mobile mode, with captions Nu / Stap 1…n / Doel
  - "Meer" is collapsed by default (`aria-expanded="false"`), and the summary line is present
  - clicking it shows the 4 lines, including "Wat je al hebt"
  - the pencil button is present without `career-only-desktop`
  - no `.career-mobile-bar` in the markup
  - the AI source line is present in both states
- **Guard:** `carriere.css` no longer contains `.career-mobile-bar`. The scene label rule contains a `clamp(` or `translateX(-50%)`.
- **Playwright 390×844** (seeded candidate with a 4-step plan; nl and ar):
  - collapsed: `document.scrollingElement.scrollHeight <= window.innerHeight` (**the page doesn't scroll**)
  - the hero top → AI line bottom distance is ≤ 560 px
  - no horizontal overflow (`CareerE2e.AssertNoHorizontalOverflowAsync`)
  - every `.career-scene__label` is inside `[0, 390]`
  - the bubble's box is inside the hero's box
  - the stepper `scrollWidth <= clientWidth`
  - after clicking "Meer", the "Wat je al hebt" line is visible and the page may scroll
  - screenshots `carriere-390-collapsed-{nl,ar}` and `carriere-390-meer-{nl,ar}` for the PR
- **Playwright 1440:** the desktop overview still renders the rail and climb, and the pencil works.

## Success criteria
- At 390×844, the Carrière overview matches `carriere-v2-mobiel.png`: one screen with no scroll when collapsed, numbers in the stones, the bubble in the hero, an equal-column stepper, the pencil in the header, no floating bar, and the AI line visible.
- "Meer" matches `carriere-v2-mobiel-meer-open.png`.
- RTL is correct. Desktop has no regressions.
- No horizontal overflow at 390. Release build with 0 warnings, full tests green.

# 02: Banenkaart filter bar per mockup A

**Stacked.**
- Branch: `cursor/kandidaat-polish-2` from `cursor/kandidaat-polish-hotfix` (or `origin/acceptatie` once PR 01 is merged).
- ONE PR into `acceptatie`, titled **"feat(banenkaart): clear mobile filter bar with Filters (n), travel preset, Match and Lijst"**.
- Rules: see README.

Mockup: `docs/mockups/kandidaat-polish/bk-a-kaart-popup.png` (top part).

## Why
Dennis "misses the old filters" and "doesn't see Match". After 01 the chips can be reached, but only by swiping. The old bar had a visible **Filters** button with a count, and a visible **Match** button. Mockup A brings that back. Everything fits in 390 px **without** horizontal scrolling.

## Scope (mobile < 900 px)
Two rows, both inside 390 px. No chip scrolling on mobile any more.
- **Row 1:**
  - The search field (`.kb-chrome-search`, placeholder "Wat voor werk zoek je?"), flexible width, height 44.
  - A **"Filters (n)"** button: outlined navy, sliders icon, count badge. n = number of active filters, excluding the travel preset. It is 0 by default and the badge is hidden at 0.
  - The button opens the filter sheet (`#discovery-filters`, redesigned in 03).
- **Row 2:**
  - **Travel preset chip** "🚲 20 min ▾" (mode icon + minutes). Tapping it opens a small popover with presets **10 / 20 / 30 min** (drop 45 from the mobile presets; the sheet in 03 keeps the precise slider) and the modes Fiets / E-bike / Auto / OV / Lopen. Reuse `.kb-chip-popover`.
  - **Match** button: gold style (`--gold-soft` background, `--gold-ink` text, spark icon), shown when logged in as a candidate, links to `/candidate/match`. Anonymous visitors see it too, linking to the existing login flow with `returnUrl=/candidate/match`, as `MatchUnlockPanel` does.
  - Spacer.
  - **Lijst / Kaart** toggle: one chip that shows the *other* view ("☰ Lijst" on the map, "🗺 Kaart" in the list).
- **Remove from mobile:** "Soort werk", "Uren", "Loon" and "Wis filters" as separate chips. They now live in the sheet (03), and "Wis alles" sits in the sheet header.
- **Address field** (`KbAddressField.razor`), on the map start / address popover: style the `input` like `.fld`.
  - height 44, 16 px font (no iOS zoom), `--border` 1px, `--radius-sm`, padding 0 12px, focus ring
  - the geo and clear buttons are 44×44 icon buttons
  - today the input is unstyled (13.33 px default font, square inset border)
- **Desktop (≥ 900):** keep the current desktop chip row (`kb-filter-chips--desktop`, ~L209). Add the same "Filters (n)" button and the gold Match button so the two match. Keep the rest.
- Keep the existing `data-testid`s. Add `kb-filters-button`, `kb-travel-chip`, `kb-match-button` and `kb-view-toggle`.

## Files to touch
- `Jobsy.Web/Components/VacancyDiscovery.razor` (mobile chip row `.kb-filter-chips` ~L66–155, Match chip ~L144, desktop row ~L209+, travel presets loop `new[] { 10, 20, 30, 45 }` ~L89/L232)
- `Jobsy.Web/Components/KandidaatBanen/KbAddressField.razor`
- `Jobsy.Web/wwwroot/css/features/kandidaat-banen.css` (`.kb-filter-chips`, `.kb-chip`, `.kb-chip-popover` ~L218–330; `.kb-address__field` ~L344–360)
- `Jobsy.Web/Localization/UiStringsKandidaatBanen.cs` (new strings, nl/en/pl/ro/ar: "Filters", "Wat voor werk zoek je?", "Match", "Lijst", "Kaart", aria labels)
- Tests: `KbAddressFieldBunitTests.cs`, `BanenkaartStartAndFiltersPlaywrightTests.cs`, a new bUnit test for the bar

## Tests
- **bUnit:**
  - the mobile bar renders the search field, `kb-filters-button`, `kb-travel-chip`, `kb-match-button` (candidate) and `kb-view-toggle`
  - the badge is hidden at 0 and shows n when 2 filters are set
  - the travel popover lists 10/20/30 and the 5 modes
- **Playwright 390×844:**
  - no horizontal overflow (reuse the 01 helper)
  - all four controls are inside the viewport without scrolling
  - "Filters" opens the sheet
  - picking "30 min" updates the chip label and the rings
  - Match navigates to `/candidate/match` (logged in)
  - the address input computed `font-size` is ≥ 16 px
- Desktop 1440 smoke test: the bar still renders and "Filters" opens the sheet.

## Success criteria
- At 390 px, Filters (n), the travel preset, Match and Lijst/Kaart are visible at once, with no swipe needed.
- The address input matches the design system (44 px, 16 px text, focus ring).
- No horizontal overflow at 390. Release build with 0 warnings, full tests green.

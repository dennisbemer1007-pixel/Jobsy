# 03: Filter sheet per mockup B (all old filters back)

**Stacked.**
- Branch: `cursor/kandidaat-polish-3` from `cursor/kandidaat-polish-2`.
- ONE PR into `acceptatie`, titled **"feat(banenkaart): full filter sheet with every filter, Volgorde and 'Toon n banen'"**.
- Rules: see README.

Mockup: `docs/mockups/kandidaat-polish/bk-b-filters.png` (full height of the scrolled sheet).

## Background
- The old sheet (`a611db40`) had: "Toon mijn vacatures" (managers only), Zoekterm, Reistijd (slider 5–90 step 5 + Vervoer select), Minimale match (Alle / Vanaf 60% / Alleen >80%), Uren per week (`HoursRangeSlider` 0–40), Branche (multiselect), Type vacature (category multiselect with colour dots), Leeftijd (Alle, 15…21+), Min./Max. uurloon (€), Straal (1–50 km), and Annuleren/Toepassen.
- All of it is **still in `VacancyDiscovery.razor`** (`#discovery-filters`, `.filter-sheet` ~L782–1050). This file restyles and reorders it. It does not rebuild the filter logic.

## Scope
The sheet is full-height on mobile (below 900 px) and a right-side drawer or dialog on desktop, as it is today. Sticky header and sticky footer.
- **Header:** "Filters", a "Wis alles" text button (resets all except the start address), and a close button (44×44, `aria-label`).
- **Sections, in this order.** Each has an icon and a heading; where the mockup shows one, the current value sits on the right in ring blue.
  1. **Zoekwoord**: text field, placeholder "Bedrijf, baan of woord".
  2. **Reistijd**: presets **10 / 20 / 30 min** as chips, **plus** a precise slider **5–90, step 5**, with ticks "5 min · 30 · 60 · 90 min". The chips and slider stay in sync. "Hoe ga je naar je werk?" shows 5 mode tiles: Fiets / E-bike / Auto / OV / Lopen (selected tile navy filled).
  3. **Match**: segmented "Alle banen / Vanaf 60% / Vanaf 80%", with the hint "Hoe goed past de baan bij jou?". Candidates only, as today. Keep the existing `_minMatchPercent` values. Make the UI label for the 80 option match the real threshold: if the code uses > 80, either change it to ≥ 80 or label it "Meer dan 80%".
  4. **Uren per week**: two-thumb range 0–40, value "16 – 32 uur".
  5. **Soort werk** (Type vacature): wrapping chips with the colour dot and a ✓ when selected (multiselect).
  6. **Branche**: select, "Alle branches".
  7. **Loon per uur**: two fields, "Minimaal" / "Maximaal" (€). Empty max shows "Geen max."
  8. **Leeftijd**: select "Alle leeftijden" (15…21+), with the hint "Sommige banen hebben een minimum leeftijd."
  9. **Afstand** (Straal): slider 1–50 km, value "max. 10 km".
  10. **Volgorde**: select with Past het best / Dichtbij / Soort werk / Startdatum / A-Z / Z-A, the **same options in map and list mode**. Today list-rows mode has only Past het best / Dichtbij / Nieuwste; keep "Nieuwste" as an extra option there if the list needs it.
  - Managers only: "Toon mijn vacatures" stays as the first section, unchanged.
- **Footer:** "Annuleren" (closes the sheet, discards changes) and a primary **"Toon n banen"**.
  - n is the live result count for the pending filters. If the count can't be computed cheaply, debounce it (300 ms) and fall back to "Toon banen".
  - This replaces "Toepassen".
- **Behaviour:**
  - Changes are pending until "Toon n banen".
  - The "Filters (n)" badge from 02 counts the active sections (excluding Reistijd).
  - On desktop, Volgorde stays in the toolbar too; both control the same value.
- **Plain Dutch at B1** in all labels, as in the mockup. "Straal" becomes "Afstand", "Type vacature" becomes "Soort werk", "Vervoer" becomes "Hoe ga je naar je werk?".

## Files to touch
- `Jobsy.Web/Components/VacancyDiscovery.razor` (`#discovery-filters` ~L782–1050; sort select ~L549 `results-meta__sort-select` / `kb-list-sort`; selects ~L890/L905/L1008)
- `Jobsy.Web/Components/Shared/HoursRangeSlider.razor` (styling only)
- `Jobsy.Web/wwwroot/css/app.css` (existing `.filter-sheet*` rules) and/or `features/kandidaat-banen.css`. Move sheet styles into the feature file if that keeps it simpler, and say so in the PR.
- `Jobsy.Web/Localization/UiStringsKandidaatBanen.cs` (+ existing Discovery strings), in 5 languages
- Tests: `FilterSheetFooterNavTests.cs`, `BanenkaartStartAndFiltersPlaywrightTests.cs`, new bUnit `KbFilterSheetBunitTests.cs`

## Tests
- **bUnit:**
  - all 10 candidate sections render in order
  - the Match section is hidden for anonymous visitors (or shown per today's rule)
  - the manager-only section shows only for managers
  - "Wis alles" resets everything except the address
  - Annuleren discards pending changes
  - the slider and the preset chips stay in sync
- **Playwright 390×844:**
  - open the sheet: no horizontal overflow, header and footer are sticky
  - scroll to Volgorde and change it, then "Toon n banen": the result list order changes and the sheet closes
  - set Uren 16–32 + Soort werk Logistiek, then the "Filters (2)" badge shows 2
  - every control is ≥ 44 px high
- Existing `FilterSheetFooterNavTests` is updated: the footer no longer covers the bottom nav.

## Success criteria
- Every old filter is back and reachable on mobile in one sheet, in plain Dutch.
- "Toon n banen" shows a correct count (or the fallback text). Annuleren discards.
- Volgorde has the same options on the map and in the list.
- No horizontal overflow at 390. Release build with 0 warnings, full tests green.

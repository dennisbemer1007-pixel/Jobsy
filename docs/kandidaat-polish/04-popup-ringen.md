# 04: Map popup + bottom sheet per mockup A, thinner rings

**Stacked.**
- Branch: `cursor/kandidaat-polish-4` from `cursor/kandidaat-polish-3`.
- ONE PR into `acceptatie`, titled **"feat(banenkaart): calm vacancy sheet/popup and thinner travel rings"**.
- Rules: see README.

Mockup: `docs/mockups/kandidaat-polish/bk-a-kaart-popup.png` (map + bottom sheet).

## Why
Dennis found the popups "ugly", and the cluster sheet was cut off (fixed in 01). The sheet has a **fixed height of 223 px** (`--map-popup-sheet-h: 223px`, used as `height`, `min-height` and `max-height`), so content gets clipped or leaves empty space. The outer ring lines are too thick.

## Scope: one vacancy card, used in the mobile bottom sheet and the desktop popup
- **Sheet chrome (mobile < 900):**
  - grab handle
  - header: title "**{n} banen in {plaats}**" (one vacancy: "1 baan in {plaats}"), with the subline "{min} min {fietsen|met de e-bike|met de auto|met het OV|lopen} van huis"
  - pager: "‹ 1 / 2 ›" (36 px round buttons, 44 px hit area)
  - close button
  - dots under the card (active dot is a wide pill)
  - swipe left/right changes the card; keep the existing slide logic in `jobMap.js`
- **Card body:**
  - **photo** 88×88, radius 12, `object-fit: cover`, falling back to the company logo the way `VacancyPhoto` does
  - **title** (16 px, semibold, navy, 2 lines max)
  - **company** (building icon), or "via uitzendbureau X" as today for hidden mode
  - **match pill** "69% past bij jou". Keep the fit gate: if it is closed, show the existing "Maak je paspoort af" link instead.
  - **meta row:** travel time (ring blue, mode icon) · hours · pay (€ from)
  - **"Waarom" line:** spark icon + "Waarom: …", reusing the existing why line (`KbWhyLine` text / `why` field). Hide the line when there is no reason.
  - **actions:** primary **"Bekijk deze baan ›"** (full width minus the heart, 48 px) + **heart** button (48×48, toggles Bewaard, filled coral when saved)
  - Drop the separate "Solliciteer" button from the sheet; applying happens on the detail page. Drop culture/specs rows that are not in the mockup.
- **Height:**
  - remove the fixed `--map-popup-sheet-h: 223px` height, min-height and max-height
  - the sheet is `height: auto` with `max-height: min(60vh, 420px)` and `overflow-y: auto` inside the body
  - the map controls (locate button) move up with the sheet (`bottom: calc(sheetHeight + 12px)`) or hide while it is open
- **Desktop (≥ 900):** the docked popup (`map-popup--docked`) uses the same card markup and styling, width 360, same pager.
- **Selected cluster** pin: coral (`--coral`) with a soft ring while its sheet is open.
- **Thinner rings**, in `ensureTravelRingLayers` (`jobMap.js` ~L2405–2470). The chosen ring is the one matching the selected minutes; today 10/20/30 are drawn and 20 is chosen by default.

  | Layer | Now | New |
  |---|---|---|
  | line, chosen | 5.5 | **2.5** |
  | line, other | 3.5 | **1.5** |
  | halo (white), chosen | 10 | **4.5** |
  | halo (white), other | 7 | **3** |
  | dash (30 min and up) | `[1.2, 1.6]` | keep dashed (scale the pattern so it still reads at 1.5 px, e.g. `[2, 2]`) |
  | fill opacity 10 / 20 / 30 | 0.22 / 0.14 / 0.09 (45: 0.06) | **0.12 / 0.09 / 0.06** (45: 0.05) |
  | halo opacity | 0.85 | 0.85 |

  Ring labels (`.map-iso-label`, `banenkaart.css` ~L399–430) become slightly lighter: 12 px text, 1.5 px border. The chosen label keeps its emphasis.

## Files to touch
- `Jobsy.Web/wwwroot/js/jobMap.js`:
  - `buildPopupHtml` L487
  - cluster card chrome ~L700–720 (`.map-cluster-card__title`)
  - `buildClusterPinHtml` L726
  - mobile sheet root `.map-cluster-sheet` ~L1275
  - slide logic ~L905–920
  - `openVacancyPopup` L1956 (docked ~L1992)
  - `ensureTravelRingLayers` ~L2405
  - labels ~L2349
  - plus `.min.js`
- `Jobsy.Web/wwwroot/css/features/banenkaart.css`:
  - `--map-popup-sheet-h` L8 and its uses L20–27
  - mobile block L39–121, desktop block L123+
  - `.map-cluster-card*` L162–350
  - `.map-iso-label*` ~L399–430
- `Jobsy.Web/wwwroot/css/app.css`: the old `.map-popup*` rules (find them with `rg -n "\.map-popup" Jobsy.Web/wwwroot/css/app.css`). Delete the ones that are no longer used, and say which in the PR.
- `Jobsy.Web/Localization/UiStringsKandidaatBanen.cs` and the `uiLabels` block at the top of `jobMap.js` (~L38): the new texts in 5 languages ("{n} banen in {plaats}", "van huis", "Waarom:", "Bekijk deze baan", heart aria labels)
- Tests:
  - `BanenkaartClusterCardCssTests.cs`: it asserts `--map-popup-sheet-h: 223px`. **Update it on purpose** to assert no fixed sheet height.
  - `BanenkaartClusterCardPlaywrightTests.cs`
  - a new JS/CSS guard for the ring widths

## Tests
- **Guard test** reading `jobMap.js`: line widths 2.5/1.5, halo 4.5/3, fill opacities 0.12/0.09/0.06, and the 30 min ring dashed.
- **CSS guard:** `.map-cluster-sheet` has no fixed `height`/`min-height` from `--map-popup-sheet-h`.
- **Playwright 390×844:**
  - tap a cluster with 2+ jobs: the sheet shows title, pager "1 / 2", photo, match/gate, meta and "Bekijk deze baan"
  - next ›: the card changes and the counter reads "2 / 2"
  - the heart toggles saved
  - close hides the sheet
  - the sheet is fully inside the viewport and its content isn't clipped (`scrollHeight <= clientHeight` for a normal card)
  - no horizontal overflow
- **Playwright 1440:** clicking a pin opens the docked popup with the same parts.

## Success criteria
- The sheet and popup match mockup A: no clipped content, no empty fixed space, one primary action plus the heart.
- The rings use the new widths and opacities, and the 30 min ring is still dashed.
- No horizontal overflow at 390. Release build with 0 warnings, full tests green.

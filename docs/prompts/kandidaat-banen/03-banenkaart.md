# 03. Banenkaart: start at home (20 min fiets), address field + address-first autocomplete, filter bar, rings, one docked popup

Read `00-README.md` first (§0, §S, D5, D9, D13, Dependencies C/E). Branch `cursor/kandidaat-banen-3` from `cursor/kandidaat-banen-2`.

> **Rules (same as README §0, repeated on purpose):**
> - Never merge, never deploy, never use rule `123` (`.cursor/rules/shortcut-123.mdc`).
> - Never push to `main` or `acceptatie`; push only this file's `cursor/kandidaat-banen-*` branch; no force-push.
> - ONE stacked PR into `acceptatie`.
> - Red tests or an unmet success criterion: push, open the PR as **draft**, stop and report. Don't start the next file.
> - The profile address is candidate-own data: never log it, never put it in a URL, never send it to a third party except the geocoder call the candidate triggers by typing.

| | |
|---|---|
| Branch | `cursor/kandidaat-banen-3` |
| PR title | `feat(banenkaart): start at home with 20 min fiets, reliable address field + address-first suggestions, filter bar chips, stronger rings, one docked popup` |
| PR body starts with | `Stacked on #<PR 02> (cursor/kandidaat-banen-2)` |
| Mockups | `kd-d1-banenkaart.png` (desktop map + side list), `kd-m1-kaart.png` (mobile map + bottom sheet + chips) |
| Split seam | **03a** = 03.2–03.5 (start origin, address field, geocoder, filter bar + badge). **03b** = 03.6–03.10 (rings + legend, docked popup, side list/bottom sheet, blank strip, breakpoint, glyphs) |

## Goal
The banenkaart opens where the candidate lives, with a realistic 20-minute bike range drawn as real road rings. Typing an address works letter by letter and suggests addresses, not shops. Filtering is a row of clear chips with a visible search. Clicking a pin or a cluster always opens the same docked card.

## 03.1 Today (verify first)
- **Origin:**
  - `VacancyDiscovery.razor` ~L1888 calls `jobsyGeo.ensureLocationOnLaunch` (`wwwroot/js/app-core.js` ~L203). That returns **only** the stored origin from localStorage; geolocation needs a "Mijn locatie" tap.
  - The profile home (`User.HomeLocation` + `PreferencesJson` `HomeAddress`) is **not** used.
  - ~L1386–1394 copy `Preferences.MaxTravelMinutes` / `PreferredTransport` into the filters.
  - Defaults: `_maxTravelMinutes = 30`, `_radiusKm = 15`, `_selectedTransport = "Fiets"` (~L931–933).
- **Address field:** `value="@_addressQuery"` + `@oninput` (~L51 mobile, ~L167 desktop), with the handler at ~L2578 → `SuggestAddressesAsync` (~L2599, 280 ms debounce).
  - Fast typing loses characters because a re-render after an await writes an older `_addressQuery` back into the input. "Herenstraat 20 Wateringen" became "Heta2Wenn" in the review.
- **Geocoder:** `Jobsy.Web/Services/NominatimGeocodingClient.cs` (`/search?q=…&countrycodes=nl&limit=6`, no layer filter), so POIs ("Wateringse Apotheek…") come before addresses.
- **Filter badge:** `ActiveFilterCount` (~L1277) counts `_ageYears` (set from the profile), so a fresh page shows "2". It also counts `_maxTravelMinutes != 30`.
- **Filter form:** the mobile sheet is one long form; desktop uses raw number inputs for wage/hours. The keyword search (`_searchQuery`) is hidden inside the sheet.
- **Rings:** `jobMap.js` `fetchIsochrones` + fallback circles; colours `#2563eb`/`#1d4ed8`, fill opacities 0.16/0.11/0.07 (guarded by `IsochroneServiceTests`). They look faint on the muted basemap.
- **Popups:**
  - a single pin opens a floating `maplibregl.Popup` (`openVacancyPopup`, `jobMap.js` ~L1917)
  - clusters open the docked card (banenkaart v3, `BanenkaartV3PlaywrightTests`, `BanenkaartClusterCardPlaywrightTests`)
- **Layout:**
  - `_wideViewport` switches at **769 px** (~L909, ~L1938); the DS breakpoints are 640/900/1024
  - anonymous mobile (390) shows a blank strip under the map (see `BanenkaartCookiePaddingPlaywrightTests` for the existing padding logic)
- **Glyphs:** the map requests Open Sans glyph ranges that 404 on the OpenFreeMap font server (network log); the app's own layers use `"Noto Sans Bold"` (`jobMap.js` ~L1159, ~L2955).

## 03.2 Start origin (D5)
- **Order of precedence** at launch:
  1. an explicit origin in the URL/state the page already supports (e.g. from the assistant), if any
  2. a change the candidate made in the filter bar **in this browser session** (sessionStorage `jobsy.kb.origin`)
  3. **logged-in candidate with a profile home** → `User.HomeLocation` + label `HomeAddress`, **Fiets, 20 min**
  4. the stored origin (`jobsyGeo` localStorage, as today)
  5. the **location prompt**
- **Server:** extend the existing candidate profile payload that `VacancyDiscovery` already loads with `HomeLatitude` / `HomeLongitude` / `HomeAddressLabel` (candidate-own, only for the signed-in candidate). No new public endpoint.
- **Location prompt** (anonymous, or candidate without a home): an in-page card over the map with the text "Waar woon je?", the address field, a button "Gebruik mijn locatie" (browser permission only after that tap, as today) and a link "Later". It doesn't block the map (the map shows the Netherlands/region view behind it). Strings in `Kb.Start.*`.
- **Filter bar changes** (address, mode, minutes) apply immediately and are kept in sessionStorage. They are **not** written to the profile. The next session starts at the profile home again.
- Stop copying `MaxTravelMinutes` / `PreferredTransport` from the profile into the map filters (README D5 default). The match calculator keeps using them.
- `jobsyGeo`: add `getSessionOrigin` / `setSessionOrigin`. `ensureLocationOnLaunch` stays as it is (stored origin only). Bump the `app-core.js` `?v=`.
- Tests:
  - a unit test of the precedence as a pure function `KbMapStart.Resolve(...)`
  - bUnit: a logged-in candidate with a home → origin = home, Fiets, 20, and the address field shows the label; anonymous → the prompt is visible
  - Playwright (03.11)

## 03.3 Address field that keeps every character
- The input is the source of truth while it has focus. Use `@bind="_addressQuery" @bind:event="oninput" @bind:after="OnAddressChangedAsync"` (or an equivalent the diff proves stable). Never write `_addressQuery` from a stale async continuation: the suggestion task only sets `_suggestions`, guarded by the existing generation counter.
- Selecting a suggestion (click/Enter) sets the label + origin and closes the list. Esc closes the list; arrow keys move through it (`aria-activedescendant`, `role="combobox"`/`listbox`, as the current markup intends).
- The same component serves mobile and desktop (today there are two copies, ~L51 and ~L167). Extract `Components/KandidaatBanen/KbAddressField.razor`.
- Tests:
  - bUnit: simulated fast input events + a delayed suggestion result never change the value
  - Playwright: type "Herenstraat 20 Wateringen" with a 25 ms key delay at 1440 and 390; the value equals the typed text exactly

## 03.4 Address-first suggestions (D5 default)
- New `PdokGeocodingClient : IGeocodingClient` (server-side, `HttpClient` via the existing factory pattern, 3 s timeout):
  - `https://api.pdok.nl/bzk/locatieserver/search/v3_1/free?q={q}&fq=type:(adres OR postcode OR weg OR woonplaats)&rows=8&fl=weergavenaam,type,centroide_ll`
  - order the results by **type rank** adres > postcode > weg > woonplaats, then PDOK score
  - parse `POINT(lon lat)`; return at most 6
- `IGeocodingClient` registration becomes a composite:
  - PDOK first
  - on error/timeout/empty, Nominatim with `&layer=address&addressdetails=1`, results with a house number first
  - `ReverseAsync` stays on Nominatim
- In-memory cache of suggestions per normalised query for 24 h (no user data in the key besides the typed text; the text is not logged).
- Tests:
  - PDOK JSON fixture → type ordering
  - fallback on a 500
  - Nominatim `layer=address` in the URL
  - "Wateringse" → the first suggestion is an address/weg, never a POI

## 03.5 Filter bar, chips, badge (D9, D13)
- **Desktop** (`kd-d1`): one row:
  - address field (03.3)
  - travel chip "20 min fietsen ▾": a popover with mode (Fiets/Auto/Lopen/OV) + minutes presets 10/20/30/45
  - chips "Soort werk", "Uren", "Loon" (presets as chips, **no raw number inputs**; keep a "Precies…" option that shows two inputs only when chosen)
  - a **visible keyword search** "Zoek op functie of bedrijf"
  - "Meer filters"
  - a "Wis filters" link when the count > 0
- **Mobile** (`kd-m1`): a horizontal chip row (travel chip first, then search as an icon that expands, then the others) + a **short** sheet "Meer filters" with collapsed sections and the same presets. The sheet footer stays as `FilterSheetFooterPlaywrightTests` expects.
- **Badge = deviations from the candidate's defaults** (D13): the defaults are the profile address origin, Fiets, 20 min, the age from the profile, the hours from the profile and no search. A fresh page shows **no** badge. Change `ActiveFilterCount` and the "is default" check (~L1310) to compare with one `KbFilterDefaults` record.
- Tests:
  - unit: `KbFilterDefaults` count = 0 for a fresh candidate and a fresh anonymous visitor; each deviation counts once
  - bUnit: the chip row renders; the search input is visible without opening the sheet

## 03.6 Rings + legend (`kd-d1`)
- Stronger rings on the muted basemap, following the mockup:
  - per band: a soft white halo line under a brand-coloured line
  - a graduated fill (10 min strongest)
  - the 30/45 min ring **dashed**
  - small pill labels "10 min / 20 min / 30 min" on the ring edge
  - clusters/pins outside the outer ring muted (lower opacity)
- Colours come from CSS tokens read once at init (e.g. `getComputedStyle(document.documentElement).getPropertyValue('--brand')`), not new hex values.
- **Update `IsochroneServiceTests`' string guard** to the new values in the same PR (intentional change, say so in the PR).
- **Legend** (collapsible, strings from 02): "Echte reistijd over de weg" when the rings are real isochrones, "Reistijd ongeveer (cirkel)" on the circle fallback (OV, or Valhalla down). `jobMap.js` tells the page which case applies (a callback / `data-iso-mode` attribute).
- Tests: the guard + a Playwright check that `data-iso-mode="real"` for Fiets after hotfix 01 and `"approx"` for OV.

## 03.7 One docked popup
- A single pin opens the **same docked card** as a cluster (one item, no pager). Remove the vacancy use of the floating `maplibregl.Popup` in `openVacancyPopup` (keep any non-vacancy popup the code needs). Keyboard: Enter on a focused pin opens it; Esc closes it and returns focus.
- The card content uses the 02 card parts (fit pill + why line + travel time; fit data arrives in 04).
- Tests: Playwright: click a single pin and a cluster; both open `.map-popup--docked` (or the class v3 uses); no `.maplibregl-popup` for vacancies. Keep `BanenkaartV3PlaywrightTests` / `JobMapPinsClustersPlaywrightTests` green.

## 03.8 Side list (desktop) + bottom sheet (mobile)
- Desktop side list header: "{n} banen · {min} min {mode}", with the sub line "Beste match bovenaan. Je 'liever niet' staat lager." (the sub line only when fit is open, 04). The cards use the 02 parts. The top-match tile stays (fixed in 01).
- Mobile: a bottom sheet with peek / half / full states, and the list inside. The existing tab/persist behaviour stays (`BanenkaartPersistSizePlaywrightTests`, `BanenkaartListRerenderPlaywrightTests`).

## 03.9 Blank strip + breakpoint
- Reproduce the blank strip at 390 anonymous (and with/without the cookie banner). Find the padding/height source (bottom-nav padding applied without a nav, or cookie padding not removed) and fix it at the source. Playwright: the map's bottom edge touches the bottom nav or the viewport bottom (±1 px).
- `_wideViewport`: switch at **900 px** (DS) in C# **and** in the JS media query; check `banenkaart.css` for 768/769 media queries and move them to 900. Test: at 899 the mobile layout, at 900 the split view.

## 03.10 Glyph 404s
- Capture the failing glyph URLs, find which style layer asks for "Open Sans …", and rewrite `text-font` to a font the glyph server serves (`"Noto Sans Regular"` / `"Noto Sans Bold"`) when the style loads, in one helper in `jobsyMapLibre.js`.
- Playwright: no 404 responses for `/fonts/` during map load.

## 03.11 Tests
- unit/bUnit from 03.2–03.5
- Playwright `Jobsy.Tests/BanenkaartStartAndFiltersPlaywrightTests.cs` (soft-skip):
  - logged-in candidate at 1440 → the address field shows the profile address, the travel chip shows "20 min fietsen", no filter badge
  - anonymous at 390 → the location prompt is visible, and the map has no blank strip
  - the typing test (03.3)
  - the suggestion order (03.4, against the real PDOK only when `JOBSY_E2E_BASE_URL` is set)
  - the docked popup for a pin and a cluster (03.7)
  - `data-iso-mode` (03.6)
  - no glyph 404s (03.10)
- existing banenkaart Playwright suites green

## Success criteria
- A logged-in candidate with a profile address lands on their home, Fiets, 20 min, badge 0; anonymous visitors get the prompt.
- Typing never drops characters; "Herenstraat 20 Wateringen" suggests that address first.
- The keyword search is visible; wage/hours are chips; a fresh page has no filter badge.
- Rings are clearly visible, labelled and honest ("ongeveer" on fallback); pins and clusters open the same docked card.
- No blank strip on anonymous mobile; the split view starts at 900 px; no glyph 404s.

Done → next: `04-fit-dislikes.md`.

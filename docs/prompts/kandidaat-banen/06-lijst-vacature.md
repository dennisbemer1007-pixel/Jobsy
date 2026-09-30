# 06. Full-width list (desktop) + mobile list, vacancy detail redesign

Read `00-README.md` first (§0, §S, §F, D3, D7, D9). Branch `cursor/kandidaat-banen-6` from `cursor/kandidaat-banen-5`.

> **Rules (same as README §0, repeated on purpose):**
> - Never merge, never deploy, never use rule `123` (`.cursor/rules/shortcut-123.mdc`).
> - Never push to `main` or `acceptatie`; push only this file's `cursor/kandidaat-banen-*` branch; no force-push.
> - ONE stacked PR into `acceptatie`.
> - Red tests or an unmet success criterion: push, open the PR as **draft**, stop and report. Don't start the next file.
> - Layout and composition only: fit, dislikes, employer attributes and hidden mode come from 04/05; don't re-implement them. Keep the apply flow untouched.

| | |
|---|---|
| Branch | `cursor/kandidaat-banen-6` |
| PR title | `feat(vacatures): full-width desktop list, mobile list with Kaart button, calmer vacancy detail with fit panel and travel card` |
| PR body starts with | `Stacked on #<PR 05> (cursor/kandidaat-banen-5)` |
| Mockups | `kd-d2-lijst.png`, `kd-m2-lijst.png`, `kd-d3-vacature.png`, `kd-m3-vacature.png` |
| Split seam | **06a** = 06.2–06.3 (list). **06b** = 06.4–06.6 (detail) |

## Goal
On desktop, a candidate can switch to a calm full-width list that reads like a shortlist: big travel time, fit + why, and a clear reason when something ranks lower. The vacancy detail answers "past dit bij mij, hoe kom ik er, wat doe ik nu" with one primary action.

## 06.1 Today (verify first)
- Desktop = a split view (map + side list, `_wideViewport`, 900 px after 03). There is **no** desktop list-only mode. Mobile toggles map/list with `ToggleMobileView` (`VacancyDiscovery.razor` ~L140–152, `Discovery.List` / `Discovery.Map`).
- `Pages/VacancyDetail.razor` (`/vacancies/{Id:guid}`, 2.015 lines):
  - location block with Route/Street View (~L243–247)
  - a video poster ("Video laden", moved to resources in 02)
  - category swatch (~L127, helper from 02)
  - several equal-weight buttons
- Sort options: `_sortMode` (~L966: title/distance/type/recent/match).

## 06.2 Desktop list mode (`kd-d2`)
- A segmented "Kaart · Lijst" toggle in the filter bar (desktop ≥ 900). **Lijst** hides the map and shows:
  - **Main column:**
    - the header "{n} banen binnen {min} min {mode}" + the sub line "Vanaf {adres}"
    - a sort menu (Past het best / Dichtbij / Nieuwste; "Past het best" only with the fit gate open)
    - rows: photo · title · company line (or "via uitzendbureau") · a **big travel time** (`KbTravelTime`, large variant) · hours/daypart/wage meta · fit pill + why line · the secondary chip (D7) · "Bewaar" + "Bekijk"
    - rows that rank lower show "Staat lager: {reden}" as their chip (04)
  - **Right rail (≥ 1024):**
    - a mini ring map of the current origin (static, uses the same isochrone data; with the circle fallback it says "ongeveer")
    - a "Zo sorteren we" card (short, from `Kb.List.HowWeSort.*`: fit first, then travel; "liever niet" lower; never hidden)
- State: query `?weergave=lijst` + sessionStorage; back/forward restore it. It uses the same data/paging as the side list (no second fetch path). Keyboard: the toggle is a real radio group.
- Test: bUnit (the toggle switches, the rows render the parts, the rail hides < 1024) + Playwright at 1440 (list mode is full width, no map canvas mounted, `?weergave=lijst` survives a reload).

## 06.3 Mobile list (`kd-m2`)
- The same row design (compact variant), with a floating **"Kaart"** button bottom-centre above the bottom nav (replaces the toolbar toggle label on mobile; `ToggleMobileView` stays the handler). The chips row from 03 stays on top.
- Test: Playwright 390: the "Kaart" button is visible and doesn't overlap the bottom nav (bounding boxes).

## 06.4 Vacancy detail layout (`kd-d3`, desktop)
- **Header:** the title (`h1`), company line (or the hidden-mode bureau line, 05), the branche pill (05, when present), the photo.
- **Main column:**
  1. the **fit panel** (04.8): % + band + why + 4 DNA bars + "Nieuw voor jou" (= `MatchGaps`), or the gate text
  2. the key facts grid (uren per week, loon per uur, wanneer, begin) with the existing values
  3. the description (existing)
  4. "Waar {bedrijf} voor staat" (05)
  5. engagement tiles (05)
- **Right column (sticky ≥ 1024):**
  - the **travel card**: a mini isochrone map around the candidate's origin with the vacancy pin (the bureau pin in hidden mode), a transport switch Fiets/Auto/Lopen/OV (OV = "ongeveer", 03.6), and the travel time for the chosen mode. Route and Street View only for non-hidden vacancies, as **secondary** links.
  - the **one primary action "Solliciteer"** (or "Je hebt gesolliciteerd · Bekijk" when applied)
  - "Bewaar" and "Delen" as secondary icon buttons
- The video stays in the media area; the poster label comes from resources.
- Remove duplicate/equal-weight buttons so there is one primary per screen (list the removed ones in the PR). Existing flows (apply, share, save, report) keep their handlers.

## 06.5 Vacancy detail (`kd-m3`, mobile)
- Stacked: photo → title/company → fit panel → key facts → travel card → description → employer blocks.
- A **sticky apply bar** at the bottom (above the bottom nav, safe-area aware) with "Solliciteer" + the save icon. Hidden mode: the travel card note from 05.6.

## 06.6 Detail tests
- bUnit:
  - one primary button (`.btn--primary` count = 1)
  - gate closed/open fit panel
  - hidden mode: no Route/Street View
  - OV shows "ongeveer"
- Playwright 1440/390: the travel card mode switch changes the minutes text; the sticky bar is visible on mobile and doesn't cover the last content (bottom padding).
- `PageSeoTests` (detail meta unchanged except what 05 masks).

## Tests
- the list and detail tests above
- existing detail/discovery tests green (`CandidateTabStabilityPlaywrightTests`, `MobileSmokePlaywrightTests`)
- `dotnet build`, `dotnet test` green

## Success criteria
- Desktop has a full-width list mode that looks like `kd-d2` and survives a reload; mobile has the "Kaart" button.
- The detail matches `kd-d3`/`kd-m3`: fit panel, travel card with transport switch, one primary action, a sticky mobile apply bar; hidden mode has no route to the real workplace.

Done → next: `07-sollicitaties-bewaard.md`.

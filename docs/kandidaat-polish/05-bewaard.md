# 05: Bewaard: compact list on mobile, smaller tiles on desktop

**Stacked.**
- Branch: `cursor/kandidaat-polish-5` from `cursor/kandidaat-polish-4`.
- ONE PR into `acceptatie`, titled **"feat(bewaard): compact saved-jobs list on mobile, 4-up tiles on desktop"**.
- Rules: see README.

Mockup: `docs/mockups/kandidaat-polish/bk-c-bewaard.png`.

## Why
On a phone, one saved job fills the whole screen. Dennis wants at least 4 per screen. The cause, in `Jobsy.Web/wwwroot/css/features/kandidaat-banen.css`:
- Below 640 px `.kb-saved__grid` is one column (~L1504–1516).
- Each card has a full-width `aspect-ratio: 16 / 10` photo (`.kb-saved-card__media` ~L1533), plus title, company, meta, fit and a footer with two buttons. That is about 520 px per card.
- The sort `<select>` (`.kb-saved__sort`) has **no CSS at all**, so it renders as a raw browser select.

## Scope
**Keep:**
- the h1 "Bewaard" and the lead text
- the **Bewaard / Gedeeld** tabs (`CandidateSavedSubnav`)
- the filter chips **Alles · n / Nog open · n / Gesloten · n**
- the sort options (Laatst bewaard = default `newest`, Past het best, Sluit het eerst)
- the undo toast and the unsave behaviour

### Mobile (< 640 px): compact list per mockup C
- **Toolbar:**
  - the chips on one row (36 px high, they may wrap)
  - below them, a row with "**{n} banen**" (muted, start) and the **styled sort** (end): 36 px, border `--border`, radius `--radius-sm`, sort icon + label + chevron, navy semibold, native `<select>` underneath for accessibility (`appearance: none` + background chevron, or a visually-hidden select under a styled label)
- **Each saved job is one row** (`article.kb-saved-row`, grid `72px 1fr 44px`, gap 12, padding 8, radius 12, `--surface`, 1px `--pearl` border, light shadow):
  - **thumbnail** 72×72, radius 8, `object-fit: cover` (`VacancyPhoto`, `Sizes="72px"`)
  - **title**: 15 px semibold navy, one line with ellipsis
  - **company · place**: 13 px muted, one line with ellipsis
  - **tags row:**
    - the **match %** pill (small, colour by fit band; when the gate is closed, a small "Maak je paspoort af" link instead)
    - the **status** as dot + text: Open (success) / "Sluit over n dagen" (warn) / Gesloten or "Baan is al vergeven" (muted). Use the existing `KbLabels.SavedStateLabel`.
    - if the candidate has applied: a small "Gesolliciteerd" tag
  - **heart**: 44×44, filled coral, unsaves with the existing undo toast, `aria-label` "Uit Bewaard halen"
  - **The whole row opens the job** (`/vacancies/{id}`):
    - use one stretched link on the title (`::after { inset: 0 }`), so the heart stays a separate button and no nested interactive elements are created
    - for a closed job, the row opens the "Zoek banen die hierop lijken" link (`SimilarHref`)
  - **Closed rows:** thumbnail grayscale, title muted, opacity 0.85
- Drop the footer buttons (Solliciteer / Bekijk / Weg) on mobile. The row and the heart replace them.
- Rows are about 90 px high (72 + 2×8 + borders), with an 8 px gap. On a 390×844 screen, **at least 4 rows** are visible below the toolbar without scrolling (the mockup shows 5).

### Desktop / tablet
- 640–1023 px: 3 columns.
- **≥ 1024 px: 4 columns** of smaller tiles.
- Tiles:
  - the photo becomes `aspect-ratio: 16 / 9`
  - title max 2 lines
  - meta in one line
  - footer actions as compact text buttons (one primary "Solliciteer" or the "Gesolliciteerd" state, plus "Bekijk")
- Fix the media link: the `<a>` around the photo must be `display: block; height: 100%`. Today it is inline, so `height: 100%` on the image doesn't resolve.

### Gedeeld tab
`Shared.razor` (`/candidate/shared`) uses `.table-list`. It is not part of this redesign, but it must not overflow at 390 and must keep the same tabs.

## Files to touch
- `Jobsy.Web/Components/Pages/Candidate/Liked.razor`:
  - toolbar ~L44–64
  - grid/card markup ~L66–137
  - mobile rows: one markup with CSS-only switching, or a `kb-saved-row` partial. Prefer one markup if it stays readable.
- `Jobsy.Web/wwwroot/css/features/kandidaat-banen.css`: `.kb-saved__toolbar` L1473, `.kb-saved__filters` L1482, `.kb-saved__grid` L1504–1516, `.kb-saved-card*` L1518–1622, new `.kb-saved__sort`, `.kb-saved-row*`
- `Jobsy.Web/Components/Candidate/CandidateSavedSubnav.razor` (styling only, if needed)
- `Jobsy.Web/Localization/UiStringsKandidaatBanen.cs`: "{n} banen", "Uit Bewaard halen", "Gesolliciteerd", in 5 languages
- Tests: `KbSavedJobStateTests.cs` (keep green), new `KbSavedListBunitTests.cs`, new `BewaardMobilePlaywrightTests.cs`

## Tests
- **bUnit:**
  - each saved item renders the thumbnail, title, company · place, match % (or the gate link), status and heart
  - the title link targets `/vacancies/{id}`; for a closed job it targets the similar-jobs link
  - the heart is a `<button>` outside the link
  - the sort select has 3 options and defaults to `newest`
- **Playwright 390×844** (candidate with ≥ 6 saved jobs, seeded):
  - ≥ 4 `.kb-saved-row` elements are fully inside the viewport at scroll 0
  - no horizontal overflow
  - tapping a row (outside the heart) navigates to the vacancy
  - the heart unsaves and the undo toast appears
  - the computed style of the sort control is not the browser default (border radius > 0, height 36)
- **Playwright 1440:** 4 tiles per row (`grid-template-columns` resolves to 4 tracks).

## Success criteria
- On 390×844, at least 4 saved jobs show per screen, as in mockup C, and the tabs, filters and sort are kept.
- The row opens the job and the heart unsaves.
- Desktop shows 4 smaller tiles per row. The sort select is styled.
- No horizontal overflow at 390. Release build with 0 warnings, full tests green.

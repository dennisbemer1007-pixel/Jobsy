# 02: /carriere in the journey style: scene, rail, empty state, overview, dream-change dialog

> **Rules (repeated in every file):**
> - Branch from `origin/acceptatie` (file 01) or from the previous file's branch (stacked). ONE PR per file, always into `acceptatie`.
> - Never merge, never deploy, never use rule `123` (`.cursor/rules/shortcut-123.mdc`). Never push to `main` or `acceptatie`. No force-pushes. Push only `cursor/carriere-*` branches.
> - Red build/tests or an unmet success criterion: push, open that PR as **draft**, stop and report. Don't start the next file.
> - Don't change the candidate nav (order, items, labels). Dennis' order is a separate add-on.

| | |
|---|---|
| Branch | `cursor/carriere-2` from `cursor/carriere-1` |
| PR title | `Carrière 02: journey-style page, climb scene, empty state, overview and dream-change dialog` |
| Body starts with | `Stacked on #<PR 01> (cursor/carriere-1)` + re-check outcome A, A2, C, D |
| Mockups | `cr-d1-geen-droombaan`, `cr-d2-reis-overzicht`, `cr-d5-droombaan-wijzigen`, `cr-m1`, `cr-m2`, `cr-m5` |
| Split if too big | `02a` = §1–§3 (shell, scene, lobster, rail, stepper, CSS), `02b` = §4–§7 (empty state, overview, dialog, copy) |

**Goal:** `/carriere` looks and feels like De ontdekkingsreis. The lobster sits on its stone in the underwater scene and climbs from the deep towards the light where the dream job is. The rail and the growing-shells stepper tell the same story in text. The empty state lets you pick a real job; the overview says what's next; changing the dream job is a calm dialog that promises nothing is lost.

Closes: B2, B3 (UI), B6 (UI hero), B10, B11, B12 (page), B13 (overview/empty copy), B14 (`/carriere`).

## 1. Page shell
- `CareerDashboard.razor` becomes a thin page: `@page "/carriere"`, `Authorize(Roles = "Candidate")`, InteractiveServer, prerender off (unchanged). Wrapper `<div class="journey-page career-page">`.
- Components (new, `Components/Candidate/Career/`):
  - `CareerStage.razor`: the three-column desktop grid (rail · card · climb zone) / mobile stack (band on top, card, sticky footer), as in `build.py` `desk()`/`mob()`
  - `CareerRail.razor`, `GrowingShellsStepper.razor` (reuse if Dependency C built one), `CareerEmptyCard.razor`, `CareerOverviewCard.razor`, `CareerDreamDialog.razor`, `CareerDreamPicker.razor` (suggestions + search; used by the empty card and the dialog)
- State comes only from `CareerPlanViewModel` (01 §9); no logic in markup.
- **Remove:** `HorizonArt` (L763–792), every inline `style=` (e.g. the width % at L320), the JS confirm (`window.confirm` interop L477–516 and its JS function if unused elsewhere; `git grep` first), the `@onblur` commit, `_editingDream`, the datalist and the old `career-dash`/`horizon-*` markup.
- Loading: a calm skeleton inside the card (no spinner over the scene). While generating: the card shows the lobster bubble "Ik maak je plan. Dat duurt even." and the button is disabled with `aria-busy="true"`. **`_busy` is set before any await** so double clicks do nothing (the server guard from 01 is the backstop).
- Errors: `CareerErr.*` via a `role="alert"` line in the card; never `ex.Message`.
- `features/carriere.css` per §0, linked + versioned. Includes the scoped h1 focus fix.

## 2. Climb scene (§S)
- Build `CareerClimbScene.razor` per §S with `Dependency A` (reuse or build `JourneyLobster` + tokens).
- Stones: Nu (start) · the plan steps (2–4) · the dream stone (gold). Place them on the §S anchors; with 4 steps interpolate one extra anchor on the path between anchors 2 and 4 (unit-test the geometry helper `CareerClimbGeometry.Points(stepCount, mobile)` → no two stones closer than 60 px desktop / 44 px mobile, all within the zone).
- The lobster stands on the current stone (`CurrentIndex`), size and plates via the builder (§S). Empty state: lobster on the start stone, size 124 / 66, `platesShed = 2`, **antenna arcs** towards the dream stone (the "Listen" pose from `build.py` `climb_desk(mode="empty")`).
- Old-shell shards on every passed stone; the trail is gold up to the lobster, dotted beyond.
- Labels under stones (HTML, not SVG): step short title + state ("Nieuwe schaal", "Nu", number). They're `aria-hidden` (the rail carries the text).
- Mobile: the 390×150 band at the top (§S), no seaweed, same states.
- `ar`: the scene is mirrored (§S) and the labels use logical positioning. Screenshot in the PR.
- Reduced motion: no bob, no bubbles rising; everything still visible.

## 3. Rail and stepper
- `CareerRail` (desktop only, ≥ 1024 px; at 900–1023 px it collapses into the stepper row above the card), per §S:
  - title "Jouw groeireis"
  - count line "Naar {dream}" / "Nog geen droombaan gekozen"
  - zones "In de diepte · waar je nu bent" / "De klim" / "Naar het licht"
  - rows "Nu" = the candidate's current work or "Waar je nu bent" when unknown (from the paspoort experience; never invent). Steps as in §S; goal "Jouw droombaan · {level}" (level from `CareerDreamCatalog`; omitted for free text)
  - footer segments + "{n} van {total} nieuwe schalen" + "Alles is bewaard. Wisselen mag altijd."
  - Empty rail = `build.py` `rail_empty()` with "Je plan is alleen voor jou."
- `GrowingShellsStepper` per §S with **real step short titles** (fixes "Basis"/"Nu" only). `aria-label` "Je stappen: van Nu naar je droombaan"; `aria-current="step"` on the current. Mobile: compact (−4 px, first word of the title).

## 4. Empty state (cr-d1 / cr-m1)
- Eyebrow "Mijn carrière", h1 **"Waar wil jij naartoe groeien?"** (one h1; no "Stip op de horizon" anywhere), lead "Kies je droombaan. Lobsy maakt een plan met kleine stappen, van waar je nu bent tot daar. Je kunt altijd wisselen."
- "Past bij jou" + "Uit je paspoort: wat je goed kunt en leuk vindt." + up to 3 suggestions as a `radiogroup` (01 §7). Each shows title + reason + the band if known ("Past goed bij je · …"). No suggestions ⇒ this block is hidden and the search is the first thing.
- "of" divider, then the search field "Zelf een beroep zoeken", placeholder "Bijvoorbeeld kok, monteur of leraar", hint "We zoeken in een lijst met echte beroepen. Zo klopt je plan beter."
  - It's a combobox (`role="combobox"`, `aria-expanded`, `aria-activedescendant`, listbox of max 8, 250 ms debounce, arrow keys + Enter). Selecting a result selects it like a suggestion.
  - Below the list: "Ik vind mijn beroep niet" opens a small text field (max 60) with the hint "Schrijf alleen de naam van een beroep." (D1). Validation errors come from `CareerErr.DreamTextInvalid`.
- Desktop hint "Weet je het nog niet? Maak eerst de ontdekkingsreis af" linking to the ontdekkingsreis when it exists (`git grep "/ontdekkingsreis"`), else to `/candidate/profile`. Hidden on mobile (space).
- One primary "Maak mijn groeiplan", disabled until a choice is made. Mobile: sticky footer.
- Bubble (desktop scene / mobile band): "Met mijn antennes voel ik al een paar stenen die bij je passen. Welke wil jij?" / mobile "Welke steen past bij jou?".

## 5. Overview (cr-d2 / cr-m2)
- Eyebrow "Mijn carrière · stap {n} van {total}" + (desktop) link "Droombaan wijzigen" (edit icon); mobile = icon button in the sticky footer with `aria-label`.
- h1 "Op weg naar {dream}" (mobile "Naar {dream}"). Lead **"Een kreeft groeit alleen als hij zijn oude schaal loslaat. Zo groei jij ook: steen voor steen."**
- The stepper.
- "Nu aan de beurt · groeit nu" section with h2 = current step title and max 3 facts:
  - "Nog {n} klauwen laten groeien" (gaps; hidden when 0)
  - "{n} opleidingen · {m} is gratis" (only with Dependency B; else hidden)
  - "Deze steen past al {band} bij jou" (band text lower-case variant; hidden when `Unknown`)
  - Primary "Bekijk deze stap" (→ 03's detail).
- "Wat je al hebt": up to 3 items from the paspoort that the builder matched (completed steps, carried-over items, matching certificates, relevant experience) with the tag "In je paspoort" (D8: "In je profiel" when the paspoort flag is off/absent). Nothing matched ⇒ the section is hidden.
- Carried-over line (after a dream change): "Je hebt al {n} stappen gehaald. Die tellen mee." (success-soft, once, dismissible).
- AI line (`FromAi`): "Plan gemaakt met hulp van AI, op basis van je paspoort. Klopt iets niet? Zeg het ons." ("Zeg het ons" → the existing feedback/contact route; `git grep` for it, else omit the link). Local plan: "Plan gemaakt op basis van je paspoort."
- Language line (D13, `LanguageDiffers`): "Je plan is in het {taal}. Maak je plan opnieuw in het {huidige taal}?" + a text button that calls generate with `force = true`.
- Goal reached (all steps done): the card shows "Je bent er! Je bent klaar voor {dream}." with the lobster on the gold stone (size max) and two actions: primary "Bekijk vacatures" (only with the employer gate on; else "Bekijk je paspoort") + text "Kies een nieuwe droombaan".
- Bubble: "Ik zit nu op steen {n}. Mijn schaal wordt al krap. Dat is goed: dan groei ik." / mobile "Steen {n}. Mijn schaal wordt krap: ik groei!".

## 6. Dream-change dialog (cr-d5 / cr-m5) — D3
- `LobsyFriendlyDialog` (existing) with the lobster (56 px, plates 6) in the header, h2 "Een andere droombaan kiezen?", small "Soms past een andere steen beter. Dat is helemaal goed."
- `CareerDreamPicker` (suggestions excluding the current dream + search + "Ik vind mijn beroep niet").
- Box "Wat gebeurt er met wat je al deed?":
  - "Wat je haalde, blijft in je paspoort: {summary}." (summary from the builder, e.g. "1 cursus en je werkervaring"; generic "alles wat je al deed" when empty)
  - "Je nieuwe plan telt dat mee. Je begint niet opnieuw."
  - "Je oude plan bewaren we 30 dagen. Terugzetten kan."
- Buttons: secondary "Blijf bij {dream}" (mobile "Blijf bij mijn plan") closes; primary "Maak nieuw plan" (disabled until a choice) calls generate. **No auto-yes path:** closing, Esc or an error keeps the current plan. Focus trap + return focus to the trigger.
- After success: close, show the new overview with the carried-over line; announce "Je nieuwe plan staat klaar" in the live region.
- **Archived plans:** under the overview a quiet disclosure "Eerdere plannen ({n})" lists them (dream title, "nog {d} dagen bewaard", completed/total) with a text button "Zet terug" → a small `LobsyFriendlyDialog` "Terug naar {dream}? Je huidige plan bewaren we ook 30 dagen." → restore (01 §2). Hidden when there are none.

## 7. Copy and languages
- Every visible string of this file → `UiStringsCareer.cs` (`Career.*`, `CareerDream.*`, `Career.Say.*`) in nl/en/pl/ro/ar. nl exactly as above. No "✓" in strings (icons only). No "AI" in headings; "stip op de horizon", "DNA", "gap-analyse", "skills gap" gone from the page.
- The old `CareerDash.*` keys used by this page are no longer referenced by it (06 deletes the unused ones).

## Tests
- bUnit:
  - empty state (with/without suggestions; combobox keyboard; free-text validation; button disabled until choice); overview (facts hidden/shown; AI vs local line; language line; goal reached)
  - dialog (no auto-yes: Esc/close keeps plan, error keeps plan; restore list)
  - stepper labels = step short titles; one h1; `aria-current`
  - Werkgevers gate off ⇒ no vacancy action in goal reached
  - `ar` renders mirrored scene class + `dir="rtl"`
- Geometry unit test (§2).
- **Guards** (new `CareerDesignGuardTests`): files under `Components/Candidate/Career/`, `Components/Candidate/Journey/` and `features/carriere.css`:
  - no hex colours (`#[0-9a-fA-F]{3,8}\b`), no `rgb(`
  - no `style="` except `style="--`
  - no `window.confirm`/`confirm(` interop in career code
  - no `ex.Message` in `.razor` files of `Pages/Candidate/CareerDashboard.razor` + career components
- Existing career API/UI tests updated; `AssetVersionGuardTests` green.

## Success criteria
- Visually matches `cr-d1`, `cr-d2`, `cr-d5`, `cr-m1`, `cr-m2`, `cr-m5` (nav excepted; §0 mockup differences).
- No percentages, no "0 jaar", no "Stip op de horizon", no placeholder text as heading, no native confirm, no inline styles, no hex colours.
- Double-clicking "Maak mijn groeiplan" starts one generation.
- The dream change never loses progress, and restore works from the UI.
- `ar` at 390 px: no horizontal overflow; primary at the inline end; the scene mirrored.

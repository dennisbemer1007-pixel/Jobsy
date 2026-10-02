# 08. Puzzle page shell `/leerling/puzzel/{key}` + De schelpenrij (p1)

Read `00-README.md` first (D4, §P, K6) and `puzzels/ontwerpnotitie.md`. Branch `cursor/vragensets-8` from `cursor/vragensets-7`.

> **Rules (same as README §0):**
> - Never merge, deploy or use rule `123`.
> - Never push to `main` or `acceptatie`; push only `cursor/vragensets-8`; no force-push.
> - ONE stacked PR into `acceptatie`.
> - Red or an unmet criterion → draft PR, stop, report.
> - Release build with 0 warnings.
> - No `.github/workflows` changes.

| | |
|---|---|
| Branch | `cursor/vragensets-8` |
| PR title | `feat(scholen): puzzle page + De schelpenrij (groep 7/8, after question 15)` |
| PR body starts with | `Stacked on #<PR 07> (cursor/vragensets-7)` |
| Mockups | `puzzels/p1-schelpenrij-mobiel-vraag.png`, `-mobiel-resultaat.png`, `-desktop.png`, `-variant-b-mobiel-vraag.png`, `-varianten.png`, `overzicht.png`; HTML in `puzzels/html/` |
| Mark | **"Needs content review by Dennis"** (all `LeerlingPuzzel.*` texts) |

## 08.1 Page shell (shared by 08–10)
- **New page** `Jobsy.Web/Components/Pages/Leerling/LeerlingPuzzel.razor`:
  - `@page "/leerling/puzzel/{Key}"`, `@layout LeerlingLayout`, `@rendermode @(new InteractiveServerRenderMode(prerender: false))`
  - `@attribute [Authorize(Policy = JobsyPolicies.PupilSession)]` (same header as `LeerlingEiland.razor`)
- **Loads** `Api.GetPupilPuzzleAsync(key)` (new methods in `Jobsy.Web/Services/ApiClient/JobsyApiClient.Pupil.cs`: `GetPupilPuzzleAsync`, `AnswerPupilPuzzleAsync`, `SkipPupilPuzzleAsync`).
  - **404** (VO pupil, disabled key, unknown key) → navigate to `/leerling/reis`; the flow decides from there.
  - **409** `step_pending` → `/leerling/reis`.
- **Renders** the matching component by `Key`: `PuzzelSchelpenrij` (this file), `PuzzelSchatkaart` (09), `PuzzelVuurtorenlampen` (10). Unknown → back to `/leerling/reis`.
- **Layout per the mockups:**
  - **Header:** "Puzzel {n} van 3 · {x} van 60 klaar" (`LeerlingPuzzel.Of` + the existing progress counts; the full rail/label work is 10), plus "Bewaard".
  - **Lobsy:** the lobster (`LeerlingLobster`) with the question in the balloon (`LeerlingPuzzel.{key}.Bubble`). On desktop, the side line "Puzzeltijd! Geen toets, gewoon voor de lol." (`LeerlingPuzzel.DesktopAside`).
  - **Kicker:** "Puzzel 1 · Koraalrif" (`LeerlingPuzzel.{key}.Kicker`), then the title (`.Title`).
  - **Footer:** `LeerlingPuzzel.Calm` ("Geen tijd · geen cijfer · fout is niet erg"). Left "Sla over" (skip icon), right "Klaar" (primary, disabled until a choice is made).
- **States:**
  1. **Question:** pick an option, then **Klaar** → POST answer → result view.
  2. **Result, correct:** the mockup `-mobiel-resultaat`:
     - "Puzzel 1 klaar" and the headline (`.Correct.Title`, e.g. "Goed gekeken!")
     - Lobsy line (`.Correct.Lobsy`)
     - strength sentence (`StrengthKey`) + explainer (`.Correct.Why`)
     - "Dit is geen toets. Je krijgt geen cijfer." (`LeerlingPuzzel.NoTest`)
     - the next button (`.Next`)
  3. **Result, wrong:** title `LeerlingPuzzel.WrongTitle` ("Lekker gepuzzeld!").
     - A calm "Zo zat het" (`LeerlingPuzzel.Reveal`) reveal: the right answer lights up **green** in place. **No red cross**, no red colour, no "fout".
     - Then the positive strength sentence from the server (`Try`/`Calm`) and the same next button.
  4. **Skipped:** POST skip → go straight on (no result view, no sentence).
  5. **Already done** (reload): show the stored result view (GET returns `Correct`/`Reveal`/`StrengthKey`). Already skipped → go on.
- **Next button:** follows `NextStep`/`NextPuzzleKey` from the response, using the same navigation helper as `LeerlingReis` (03a.3): `question` → `/leerling/reis`, `island` → `/leerling/eiland`, `puzzle` → `/leerling/puzzel/{key}`, `done` → `/leerling/dit-ben-jij`.
- **Never shown:** a timer, a countdown, a score, a "nieuwe puzzel" button, a retry.

## 08.2 De schelpenrij component (`Jobsy.Web/Components/Leerling/Puzzles/PuzzelSchelpenrij.razor`)
- **Shapes:** `PuzzelShape.razor` renders a symbol as inline SVG (the six shapes from 07.3, ported from `puzzels.py`, outline `#0f2d5c`), with `role="img"` and `aria-label` = `PuzzleShapes.Label(...)` ("roze schelp").
- **The row:** 7 symbols + the `?` slot.
  - Mobile (< 640 px): **2 rows of 4** with the curving dotted reading line from the mockup.
  - Desktop: **1 row of 8**.
  - Container label: `LeerlingPuzzel.p1-schelpenrij.RowLabel` = "Een rij van 8 plekjes, de laatste is leeg". The `?` slot has a dashed border + "?" and `aria-label` "Lege plek" (not colour only).
- **Options:** 4 buttons in a `role="radiogroup"` (label `LeerlingPuzzel.p1-schelpenrij.OptionsLabel` = "Kies wat er hierna komt"):
  - each is a `role="radio"` with `aria-checked`, roving tabindex, arrows/Home/End, Enter/Space to pick (same pattern as `LeerlingAnswerScale` in 05; reuse its keyboard helper)
  - height **68 px**, tap target ≥ 44 px
- **Result:** the `?` slot fills with the answer (correct or revealed) with a green ring; the picked wrong option gets no special colour.
- **Copy** (exact from the mockups; add the keys to `UiStringsLeerlingPuzzel.cs`):

  | Key | Text |
  |---|---|
  | `LeerlingPuzzel.p1-schelpenrij.Kicker` | "Puzzel 1 · Koraalrif" |
  | `.Title` | "De schelpenrij" |
  | `.Bubble` | "Wat komt er op de plek van het vraagteken?" |
  | `.Correct.Title` | "Goed gekeken!" |
  | `.Correct.Lobsy` | "Wauw, jij hebt scherpe ogen!" |
  | `.Correct.Why` | "Patronen zitten overal: in muziek, in computercode en in het weer. Daar heb je veel aan!" |
  | `.Next` | "Door naar de schatgrot" |
  | `LeerlingPuzzel.Done.Title` | "Puzzel {0} klaar" |

## 08.3 Accessibility (ontwerpnotitie "Toegankelijkheid"; all checked in tests)
- Meaning is carried by shape. Colours are Okabe-Ito with an outline, and options differ in shape **and** colour (guaranteed by 07).
- Tap targets ≥ 44 px: the options are 68 px; "Sla over" and "Klaar" ≥ 44 px.
- Screen reader: every symbol has a label; the options are a radiogroup; after answering, focus moves to the result title (`tabindex="-1"`), and the strength sentence is in an `aria-live="polite"` region.
- Keyboard only: the whole puzzle is playable with Tab/arrows/Enter/Space.
- `prefers-reduced-motion`: no confetti, no fill animation (CSS `@media (prefers-reduced-motion: reduce)`).
- Contrast: text ≥ AA on the scene background; the `?` uses a dashed border + text.
- **F5:** the ontwerpnotitie's "voorleesknop" doesn't exist in the app. Don't build it.

## 08.4 Optional pupil-only card on "Dit ben jij" (K6)
- `LeerlingDitBenJij.razor` *may* show a small "Jouw puzzels" card with the strength sentences of the done puzzles, loaded from a new pupil endpoint `GET api/pupil/puzzles` (keys + status + strength key for done; no `correct` field).
- Never via `PupilStoryViewDto`, `StoryKeysJson` or the PDF.
- If this adds more than ~150 lines, leave it out and list it under "Out of scope".

## 08.5 Enable the slot, docs, guards
- **Enable** `p1-schelpenrij` in the registry's `enabledPuzzleKeys` (07.6) in DI. A G78 pupil at 15 answers now gets `NextStep = puzzle`.
- **`LeerlingReis.razor`:** navigate to the puzzle on `NextStep == "puzzle"` (wired in 03a; check it now really happens after answer 15).
- **`docs/ROUTES.md`:** add `/leerling/puzzel/{key}` (pupil session).
- **`Jobsy.Web/Seo/PageSeoCatalog.cs`:** `Private("/leerling/puzzel/{key}")` (noindex).
- **`Jobsy.Web/Help/PageHelpDocs.cs`:** an entry if pupil pages have one (follow `LeerlingEiland`).
- **`BlazorPageRoleAttributesTests`:** add the page with the `PupilSession` policy.
- **`UiStringsScholen.IsNlOnlyPrefix`:** `LeerlingPuzzel.` (if not done in 07).
- **CSS:** in `Jobsy.Web/wwwroot/css/features/scholen.css` next to the `ll-*` rules, prefix `ll-puz-`. Use design tokens (`.cursor/rules/design-system.mdc`); no new colours besides the Okabe-Ito tokens (add them as CSS variables `--ll-puz-oranje` …).

## Tests
- **bUnit** `LeerlingPuzzelSchelpenrijTests`:
  - renders 7 symbols + `?` and 4 radio options with labels
  - Klaar is disabled until a pick
  - keyboard selection works (arrows + Enter)
  - correct → strength sentence; wrong → "Lekker gepuzzeld!" + green reveal + no element with a red/error class or text "fout"
  - skip → navigates without a result view
  - reload of a done puzzle shows the stored result
- **API/flow:** a G78 pupil with 15 answers → `NextStep = puzzle`, `NextPuzzleKey = p1-schelpenrij`; after skip → question 16; the VO pupil at 15 → question.
- **No-pressure guard:** the rendered markup contains no `timer`, `countdown`, `score`, `cijfer:` patterns and no `LeerlingPuzzel.*` value contains "fout" except `LeerlingPuzzel.Calm`.
- **Tap targets:** a bUnit/CSS test (or the Playwright smoke in 12) asserts every button ≥ 44 × 44 px at 360 px.
- Docs/SEO/role guards above stay green.

## Success criteria
- A groep 7/8 pupil gets De schelpenrij after question 15, can answer or skip, never sees a timer/score/red cross, and lands on question 16.
- The same pupil sees the same row and options after a reload; a classmate sees a different one.
- Playable with keyboard and screen reader; no motion with reduced motion.

Done → next: `09-puzzel-schatkaart.md`.

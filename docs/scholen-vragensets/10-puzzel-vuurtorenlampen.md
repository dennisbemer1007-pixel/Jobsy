# 10. De vuurtorenlampen (p3) + puzzle steps in the journey rail + "Puzzel n van 3 · x van 60 klaar"

Read `00-README.md` first (D4) and `puzzels/ontwerpnotitie.md` §3 and "Voortgang". Branch `cursor/vragensets-10` from `cursor/vragensets-9`.

> **Rules (same as README §0):**
> - Never merge, deploy or use rule `123`.
> - Never push to `main` or `acceptatie`; push only `cursor/vragensets-10`; no force-push.
> - ONE stacked PR into `acceptatie`.
> - Red or an unmet criterion → draft PR, stop, report.
> - Release build with 0 warnings.
> - No `.github/workflows` changes.

| | |
|---|---|
| Branch | `cursor/vragensets-10` |
| PR title | `feat(scholen): De vuurtorenlampen puzzle + puzzle steps in the journey rail (groep 7/8)` |
| PR body starts with | `Stacked on #<PR 09> (cursor/vragensets-9)` |
| Mockups | `puzzels/p3-vuurtorenlampen-mobiel-vraag.png`, `-mobiel-resultaat.png`, `-desktop.png`, `-variant-b-mobiel-vraag.png`, `-varianten.png`; the rail in `p1-schelpenrij-desktop.png` and `overzicht.png` |
| Mark | **"Needs content review by Dennis"** (p3 texts, the start time, the ouderbrief clause) |

## 10.1 Component `Jobsy.Web/Components/Leerling/Puzzles/PuzzelVuurtorenlampen.razor`
- **Tower:** the lighthouse with **9 windows in 3 rows** (`role="img"`, label `LeerlingPuzzel.p3-vuurtorenlampen.TowerLabel` = "Vuurtoren met 9 ramen in 3 rijen, 1 raam is leeg").
  - Each lit window shows a `PuzzelShape` (08) with its label.
  - The gap has a dashed border + "?" and the label "Leeg raam".
- **Rule hint:** a mini-pictogram + the text `.Rule` = "Elke rij en elke kolom: elke lamp 1 keer."
- **Options:** 4 lamp buttons in a `radiogroup`, 68 px high, with the same keyboard pattern as p1. Never yellow (guaranteed by 07).
- **Result:**
  - **Correct:** all windows lit, "Alle lampen branden!".
  - **Wrong:** "Lekker gepuzzeld!" + "Zo zat het": the right lamp appears in the gap with a green ring. No red.
  - Reduced motion: no light-up animation.

| Key | Text |
|---|---|
| `LeerlingPuzzel.p3-vuurtorenlampen.Kicker` | "Puzzel 3 · Vuurtoren" |
| `.Title` | "De vuurtorenlampen" |
| `.Bubble` | "Eén lamp is uit. Welke lamp past in het lege raam?" |
| `.Correct.Title` | "Alle lampen branden!" |
| `.Correct.Lobsy` | "Alles brandt weer. Dankjewel!" |
| `.Correct.Why` | "Zo werken ook detectives, dokters en monteurs: eerst kijken, dan slim kiezen." |
| `.Next` | "Door naar de lagune" |

- **Enable** `p3-vuurtorenlampen` in `enabledPuzzleKeys` (07.6). All three are now live.

## 10.2 Journey rail + progress label (`LeerlingReis.razor`, `LeerlingPuzzel.razor`)
- **Rail** (`.ll-rail__list`, today one `<li>` per `PupilWorldCatalog.Worlds` entry): build the steps from the **def** (03a) in flow order. For G78:
  1. Het koraalrif
  2. Puzzel 1 · De schelpenrij
  3. De schatgrot
  4. Puzzel 2 · De schatkaart
  5. Pauze-eiland
  6. De vuurtoren
  7. Puzzel 3 · De vuurtorenlampen
  8. De lagune
  9. Dit ben jij
  - **Puzzle steps** use a puzzle icon and the subtitle "Puzzel {n}" (mockup `p1-schelpenrij-desktop.png`).
  - **States:** `is-done` when done **or skipped**, `is-now` on the puzzle page.
  - **"Niet aangeboden"** puzzles (K7) are shown as plain, not done, and never `is-now`.
  - **VO** builds its rail from its def: no puzzle steps.
  - Put the step list in a pure helper `PupilJourneySteps.For(def, state)` (Core), unit-tested.
- **Rail meta on the puzzle page:** "Puzzelpauze {n} van 3" (`LeerlingPuzzel.RailMeta`) instead of "Wereld x van 4".
- **Progress label on the puzzle page:** "Puzzel {n} van 3 · {x} van {total} klaar" (`LeerlingPuzzel.Progress`). Puzzles **never** count in x/total; x = answered items, total = `def.Bank.AllItems.Count`.
- **Desktop aside:** "Alles is bewaard. Pauze mag altijd." (`LeerlingPuzzel.SavedAside`), as in the desktop mockup.

## 10.3 Copy that waited for all three puzzles (07.8)
- `Leerling.Start.NoteTime` (G78) → "Duurt ongeveer 30 minuten."
- **`OuderbriefTemplate`:** add the G78 puzzle clause "Tussendoor maakt je kind drie korte puzzels; we bewaren alleen of een puzzel gedaan of overgeslagen is."
  - Bump `ParentalInfoTexts.CurrentVersion` → `ouders-2026-10-v3`.
  - Existing confirmations of v2 behave as the existing version-bump logic says (don't change that logic). Mention N1 in the PR.
- **`Jobsy.Web/wwwroot/docs/scholen/lesbrief-lobsy.html`:** extend 04's line to "groep 7/8: 60 vragen en 3 korte puzzels, ongeveer 30 minuten".

## Tests
- **bUnit** `LeerlingPuzzelVuurtorenlampenTests`:
  - 8 lit windows + 1 gap with labels; 4 radio options; the rule text
  - correct → "Jij denkt stap voor stap na."; wrong → green reveal, no red
  - keyboard selection works
- **`PupilJourneyStepsTests`:**
  - G78: 9 steps in the order above
  - VO: worlds + island + Dit ben jij, no puzzles
  - a skipped puzzle → done; a K7 "niet aangeboden" puzzle → not done, not now
- **Label:** at 15 answered on p1 → "Puzzel 1 van 3 · 15 van 60 klaar"; at 45 on p3 → "Puzzel 3 van 3 · 45 van 60 klaar".
- **Full G78 flow (API):** 60 answers + 3 puzzles (correct, wrong, skip) + island → `Done`. `PupilResult` is identical to a run without puzzles (same answers).
- **Ouderbrief:** the version is `ouders-2026-10-v3`; the clause appears for a G78 class only.

## Success criteria
- All three puzzles appear at 15, 30 (→ island) and 45 for groep 7/8; the rail shows them as their own steps; the label reads "Puzzel n van 3 · x van 60 klaar".
- VO pupils see no puzzle anywhere.

Done → next: `11-leraar-school-weergave.md`.

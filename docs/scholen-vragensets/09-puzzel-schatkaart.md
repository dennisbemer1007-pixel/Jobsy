# 09. De schatkaart (p2): 5×5 grid, route arrows, cell pick, reveal, hand-off to the Pauze-eiland

Read `00-README.md` first (D4, §P) and `puzzels/ontwerpnotitie.md` §2. Branch `cursor/vragensets-9` from `cursor/vragensets-8`.

> **Rules (same as README §0):**
> - Never merge, deploy or use rule `123`.
> - Never push to `main` or `acceptatie`; push only `cursor/vragensets-9`; no force-push.
> - ONE stacked PR into `acceptatie`.
> - Red or an unmet criterion → draft PR, stop, report.
> - Release build with 0 warnings.
> - No `.github/workflows` changes.

| | |
|---|---|
| Branch | `cursor/vragensets-9` |
| PR title | `feat(scholen): De schatkaart puzzle (after question 30, leads into the Pauze-eiland)` |
| PR body starts with | `Stacked on #<PR 08> (cursor/vragensets-8)` |
| Mockups | `puzzels/p2-schatkaart-mobiel-vraag.png`, `-mobiel-resultaat.png`, `-desktop.png`, `-variant-b-mobiel-vraag.png`, `-varianten.png` |
| Mark | **"Needs content review by Dennis"** (p2 texts) |

## 09.1 Component `Jobsy.Web/Components/Leerling/Puzzles/PuzzelSchatkaart.razor`
- **Route strip** ("Route" label, `LeerlingPuzzel.p2-schatkaart.RouteLabel`):
  - one group per run, with a visible gap between groups
  - each group repeats the arrow icon `Count` times
  - group `aria-label` = `LeerlingPuzzel.Run` "{0} keer {1}" with the direction `LeerlingPuzzel.Dir.Up/Down/Left/Right` = "omhoog/omlaag/naar links/naar rechts" ("3 keer omhoog"). Same words as `ARW_NL` in `puzzels.py`.
- **Grid:** 5×5 `role="grid"` labelled "Schatkaart, 5 bij 5 vakjes" (`.GridLabel`).
  - **Cells:** each is a `button` with `aria-label` "Rij {r}, vak {c}" (`LeerlingPuzzel.Cell`, 1-based).
  - **Size:** ~60 px on mobile and desktop, `min-width/min-height: 44px` enforced.
  - **Start cell:** shows the start marker + "start" (`.Start`) and is labelled "Rij 2, vak 4, start".
  - **Decor cells** (rock/seaweed) are decorative: `aria-hidden` on the decor icon only, so the cell stays a normal, tappable cell.
- **Keyboard:** roving tabindex in the grid. Arrow keys move the focus (no wrap), Home/End go to the row start/end, Enter/Space selects. The selection gets `aria-selected="true"` and a thick outline (not colour only).
- **Klaar** is enabled once a cell is selected → POST `{ row, col }` (0-based in the API, 1-based in labels).
- **Result:**
  - **Correct:** the route draws as a dotted line from start to end, with a treasure chest in the end cell (mockup `-mobiel-resultaat`).
  - **Wrong:** "Lekker gepuzzeld!" + "Zo zat het": the same dotted route and the right cell lights up green. The picked cell gets no red.
  - The route comes from the server `Reveal`. The client never computes it before answering.
  - With reduced motion, no line-draw animation and no chest pop: the final state is shown at once.

## 09.2 Copy (exact from the mockups)
| Key | Text |
|---|---|
| `LeerlingPuzzel.p2-schatkaart.Kicker` | "Puzzel 2 · Schatgrot" |
| `.Title` | "De schatkaart" |
| `.Bubble` | "Ik volg de pijlen. Waar ligt de schat? Tik het vakje." |
| `.Correct.Title` | "Schat gevonden!" |
| `.Correct.Lobsy` | "Mijn schat! Jij bent een echte ontdekker." |
| `.Correct.Why` | "Handig als je later bouwt, bezorgt, vliegt of huizen ontwerpt." |
| `.Next` | "Naar het Pauze-eiland" |

## 09.3 Hand-off to the Pauze-eiland
- **Order:** vraag 30 → puzzel 2 → eiland → vraag 31 (07.6). After answer **or skip**, the server returns `NextStep = island` and the button/skip goes to `/leerling/eiland`.
- **`LeerlingEiland.razor` is unchanged** apart from reading the def (03a). G78 has no part break (`PartBreakAfterIsland = false`), so after the chips the pupil continues to question 31.
- **Back-navigation:** a pupil who opens `/leerling/reis` while p2 is pending gets sent to the puzzle by `NextStep` (03a). A pupil who opens `/leerling/eiland` directly while p2 is pending is sent to `/leerling/puzzel/p2-schatkaart`. Add that check to `LeerlingEiland`: only when `NextStep == puzzle`.
- **Enable** `p2-schatkaart` in `enabledPuzzleKeys` (07.6).

## Tests
- **bUnit** `LeerlingPuzzelSchatkaartTests`:
  - 25 cells with "Rij r, vak c" labels; the start label; run groups labelled "n keer …"
  - arrow-key navigation (no wrap) + Enter selects; Klaar disabled until a selection
  - correct → route + chest + strength "Jij houdt een route goed in je hoofd."
  - wrong → "Lekker gepuzzeld!" + green right cell + no red/error class
  - reduced motion → no animation class (render with a `prefers-reduced-motion` flag or assert the CSS media rule exists)
- **Flow:** 30 answered → Puzzle p2; after answer → Island; after skip → Island; island done → question 31; `/leerling/eiland` with p2 pending → redirect to the puzzle.
- **Tap targets:** cells ≥ 44 px at 360 px (bUnit style check or the 12 smoke).

## Success criteria
- After question 30 a groep 7/8 pupil gets De schatkaart, then the Pauze-eiland, then question 31, whether they answered or skipped.
- The grid is fully usable by keyboard and screen reader; the reveal never uses red.

Done → next: `10-puzzel-vuurtorenlampen.md`.

# 07. Puzzle engine (Core), storage, pupil API, flow gates after 15/30/45 (groep 7/8 only)

Read `00-README.md` first (D4, §P, K5, K6, K7) and **`docs/mockups/scholen-vragensets/puzzels/ontwerpnotitie.md`** (the puzzle contract). Branch `cursor/vragensets-7` from `cursor/vragensets-6`.

> **Rules (same as README §0):**
> - Never merge, deploy or use rule `123`.
> - Never push to `main` or `acceptatie`; push only `cursor/vragensets-7`; no force-push.
> - ONE stacked PR into `acceptatie`.
> - Red or an unmet criterion → draft PR, stop, report.
> - Release build with 0 warnings.
> - No `.github/workflows` changes.

| | |
|---|---|
| Branch | `cursor/vragensets-7` |
| PR title | `feat(scholen): puzzle engine — seeded per pupil, L2, done/skipped only, gates after 15/30/45 (groep 7/8)` |
| PR body starts with | `Stacked on #<PR 06> (cursor/vragensets-6)` |
| Migration | `AddPupilPuzzles` (§DM). The only migration in this file. |
| Mark | **"Needs content review by Dennis"** (strength sentences, Lobsy lines) |

No UI in this file except what's needed to keep the flow working. The puzzle pages are 08–10. **Until 08 is merged the gates are off** (see 07.6), so a G78 pupil is never sent to a page that doesn't exist yet.

## 07.1 Where the code goes
- `Jobsy.Core/Scholen/Puzzles/` (pure, no EF, no DI dependencies):
  - `PupilPuzzleKeys.cs`: `Schelpenrij = "p1-schelpenrij"`, `Schatkaart = "p2-schatkaart"`, `Vuurtorenlampen = "p3-vuurtorenlampen"`.
  - `PuzzleLevel.cs`: `enum PuzzleLevel { L1 = 1, L2 = 2, L3 = 3 }`. Everyone gets `L2` (D4). L1/L3 parameters exist in the generators and are tested, but are never served.
  - `PuzzleSeed.cs`, `PuzzleRng.cs`, `PuzzleShapes.cs` (shape × colour alphabet + labels).
  - `Generators/SchelpenrijGenerator.cs`, `SchatkaartGenerator.cs`, `VuurtorenlampenGenerator.cs`, all implementing `IPuzzleGenerator`.
  - `PuzzleChecker.cs`, `PuzzleStrengths.cs`, `PupilPuzzleState.cs` (the JSON model).
- `Jobsy.Core/Contracts/Scholen/PupilPuzzleDtos.cs`: the API DTOs.
- `Jobsy.Infrastructure/Scholen/PupilPuzzleService.cs` (+ `IPupilPuzzleService`): load/save state, call the engine.
- `Jobsy.Api/Controllers/PupilController.cs`: three new actions (07.5).

## 07.2 Seed and PRNG (K5)
- **Seed:** `seed = SHA-256(UTF-8("{templateVersion}:{puzzleKey}:{PupilCode.Id:D}"))`. The first 8 bytes, read **big-endian**, form a `ulong`.
  - `templateVersion` per generator is a const string, starting at `"1"`.
  - **Why `PupilCode.Id`:** Lobsy stores no plaintext code (only `LookupHash` + `CodeProtected`). Decrypting the code to seed a puzzle is not allowed. `ReplaceAsync` keeps the `Id`, so a replaced code keeps its puzzles.
- **PRNG:** own **SplitMix64** in `PuzzleRng`: `NextULong()`, `NextInt(maxExclusive)` (rejection sampling, no modulo bias), `Shuffle<T>(IList<T>)` (Fisher–Yates), `Pick<T>(IReadOnlyList<T>)`.
  - **Never** use `System.Random`, `Random.Shared`, `HashCode` or `GetHashCode()`; their output isn't stable across runtimes. The architecture test in 07.9 checks this.
  - Don't port the Python `random` module from `puzzels.py`. Port the **generator logic** only; instances will differ from the mockups, which is expected.
- **Optional salt** (ontwerpnotitie "Variatie"): no per-class collision check at runtime. The test in 07.9 proves collisions are rare. Do not add a salt mechanism.

## 07.3 Shapes, colours, labels (shared by all three)
- **Palette:** Okabe-Ito with the outline `#0f2d5c` on every shape (the mockup value; use the matching navy design token if one exists):

  | Colour key | Hex | Adjective (de-word) | het-word (`kristal`) |
  |---|---|---|---|
  | `oranje` | `#E69F00` | oranje | oranje |
  | `lichtblauw` | `#56B4E9` | lichtblauwe | lichtblauw |
  | `groen` | `#009E73` | groene | groen |
  | `geel` | `#F0E442` | gele (**never** in p3) | geel |
  | `blauw` | `#0072B2` | blauwe | blauw |
  | `rood` | `#D55E00` | rode | rood |
  | `roze` | `#CC79A7` | roze | roze |

- **Shapes:** exactly the six in `puzzels/puzzels.py` `SHAPES`: `schelp`, `zeester`, `parel`, `kristal`, `maan`, `vis`. Port the SVG paths (viewBox 48×48) to Web in 08 (`PuzzelShape.razor`), keyed by the shape key.
- **Every symbol** = `(ShapeKey, ColourKey)`. Its label is built by `PuzzleShapes.Label(symbol)` from `LeerlingPuzzel.Shape.{shape}` and `LeerlingPuzzel.Colour.{colour}` (de-word form) or `LeerlingPuzzel.Colour.{colour}.Het` for `kristal`: "roze schelp", "lichtblauwe parel", "blauw kristal" (same rule as `tname()` in `puzzels.py`). Test all 42 combinations.
- **Hard rules:**
  - Within one instance, every shape is unique and every colour is unique. Meaning is carried by **shape**; colour is extra.
  - Options always differ from each other in shape **and** colour.
  - No instance can be solved by colour alone; for p1/p3 this follows from the unique-shape rule.

## 07.4 Generators (L2 is the served level; follow the ontwerpnotitie table per level)
Every generator: `PuzzleInstance Generate(ulong seed, PuzzleLevel level)`. It's deterministic, has no side effects and throws for invalid params. The instance holds what the UI needs **plus** the answer, but the answer never goes to the client (07.5).

### p1 `SchelpenrijGenerator` (patterns)
- **Families:** L1 `AB`, `AAB`; **L2 `ABC`, `ABB`, `AABB`**; L3 `ABCB`, `ABCD`, `AABC`.
- **Steps:**
  1. Pick a family.
  2. Pick the alphabet: as many symbols as the family has distinct letters (2–4), each with a unique shape and colour.
  3. Pick an `offset` in `[0, cycleLength)`.
  4. Produce 8 positions; show **7** and make the 8th the `?`.
- **Options:** 4 = the answer + 3 foils.
  - Foils come from the alphabet's other symbols **plus**, if needed, new symbols with an unused shape and colour.
  - No foil equals the answer.
  - Shuffle the 4 with the PRNG.
- **Instance:** `Sequence[7]`, `Options[4]`, `AnswerIndex` (server-only), `Family`, `TemplateVersion`, `Level`.

### p2 `SchatkaartGenerator` (spatial)
- **Grid:** 5×5. `Start` is a cell.
- **Runs:**
  - L1: 2 runs, 3–4 steps total.
  - **L2: 3 runs, 5–7 steps total; consecutive runs are perpendicular.**
  - L3: 4 runs, any direction, 7–9 steps.
- **Rules:**
  - the route stays inside the grid
  - no cell is visited twice
  - the end cell is at Manhattan distance ≥ 2 from the start
  - on L1–L2, consecutive runs are perpendicular
- **Decor:** 3 cells off the route (rock/seaweed). Pure decoration: never the start, never on the route.
- **Construction:** retry with the PRNG until the constraints hold, max 200 attempts. Prove in a test that L2 never needs more than 200 over 10 000 seeds; else throw.
- **Answer:** the end cell. The pupil taps one of the 25 cells, so there are no options. A wrong-cell tap is wrong; the start cell and decor cells are tappable too.
- **Instance:** `Start`, `Runs[(Direction, Count)]`, `Decor[3]`, `AnswerCell` (server-only), plus the full `Route` for the reveal (server-only until answered).

### p3 `VuurtorenlampenGenerator` (logic)
- **L2:**
  1. Pick 3 symbols (no `geel`).
  2. Take the base latin square `[[0,1,2],[1,2,0],[2,0,1]]` and apply a random row permutation, column permutation and symbol permutation.
  3. Leave one `gap` empty.
- **Options:** 4 = the 3 symbols + 1 `foil` (a 4th shape and colour, not yellow). Shuffle them.
- **L1:** 3 options, no foil. **L3:** 4×4 with 3 gaps, where the asked window is only solvable via row **and** column together (implement and test; never served).
- **Instance:** `Grid[3][3]` with `null` at the gap, `Gap`, `Options[4]`, `AnswerIndex` (server-only).

## 07.5 Storage, checker, API
- **Storage:** `PupilProgress.PuzzlesJson` (`'{}'` default; migration `AddPupilPuzzles`). Model:
  ```json
  { "p1-schelpenrij": { "status": "done", "correct": true, "templateVersion": "1", "level": 2 } }
  ```
  - `status`: `done` | `skipped`.
  - `correct`: bool for `done`, `null` for `skipped`.
  - **Nothing else:** no timestamps, no chosen option, no attempts, no durations (§P). `PupilPuzzleState` has exactly these four properties. A test serialises it and asserts the property set.
  - **Versioning:** generation uses the stored `templateVersion`/`level` if the puzzle has a stored state, else the generator's current version and L2. The state is written once, on answer or skip, and never changes after that. So a template bump changes only puzzles that haven't been answered yet (ontwerpnotitie: "een lopende sessie bewaart haar templateVersion"). Keep old generator versions only if a bump ever happens; with version `"1"` there is nothing to keep.
- **`PuzzleChecker.Check(instance, answer)`:** p1/p3 compare the option index, p2 compares `(row, col)`. The server **regenerates** the instance from the seed and never trusts anything the client sends except the answer.
- **Endpoints** (on `PupilController`, `[Authorize(Policy = JobsyPolicies.PupilSession)]` like the others, `[SchoolsFeatureGate]`, the `pupil` rate limit):

  | Method | Route | Body | Returns |
  |---|---|---|---|
  | GET | `api/pupil/puzzles/{key}` | – | `PupilPuzzleDto`: `Key`, `Index` (1–3), `Kind`, the instance **without** `AnswerIndex`/`AnswerCell`/`Route`, `Status` (`open`/`done`/`skipped`), and when done: `Correct`, `Reveal`, `StrengthKey` |
  | POST | `api/pupil/puzzles/{key}/answer` | `{ "option": n }` or `{ "row": r, "col": c }` | `PupilPuzzleAnswerResponse`: `Correct`, `Reveal` (answer index/cell + p2 route), `StrengthKey`, `NextStep`, `NextPuzzleKey` |
  | POST | `api/pupil/puzzles/{key}/skip` | – | `NextStep`, `NextPuzzleKey` (no strength) |

  - **One answer per puzzle, idempotent.** If the puzzle is already `done`/`skipped`, answer/skip return **200** with the **stored** outcome; nothing changes, and there is no retry. A second, different answer doesn't change `correct`.
  - **404** for an unknown key, and for **every key when the class's test is VO** (no puzzles for VO, F1).
  - **409 `step_pending`** when the pupil isn't at that puzzle's gate yet (fewer than `AfterItem` answers). Answering a puzzle later than its gate is allowed (e.g. after a reload).
  - **400** for a malformed body (option outside 0–3, cell outside 0–4).
  - A window-closed / revoked / completed code behaves as in `SaveAnswerAsync` (same errors).
- **Logs:** log only `puzzleKey` and the `PupilCode.Id`. **Never** log the answer, `correct` or the strength key (§P).

## 07.6 Flow gates (G78 def + `PupilFlow`)
- **G78 def** gets `PuzzleSlots = [(15, p1), (30, p2), (45, p3)]`.
- **Order at 30:** `p2-schatkaart` comes **before** the island: vraag 30 → puzzel 2 → eiland → vuurtoren (ontwerpnotitie). `PupilFlow.Next` already checks the puzzle before the island (03a.2 rule 2 before rule 3). Add the table-test rows:
  - 15 answered, no p1 status → Puzzle p1
  - p1 skipped → Question 15
  - 30 answered, nothing else → Puzzle p2
  - p2 done, island not done → Island
  - island done → Question 30
  - 45 → Puzzle p3
  - 60 → Done (puzzles never block Done)
- **K7, in-progress pupils:**
  - A pupil already past a gate at deploy time is never sent back. `PupilFlow` only offers a puzzle when `first == AfterItem`.
  - Those puzzles stay without status ("niet aangeboden") and count as neither done nor skipped.
- **Feature switch until 08–10 land:**
  - `PupilQuestionSetRegistry` gets a ctor option `IReadOnlySet<string> enabledPuzzleKeys`. The G78 def includes only the slots whose key is in it.
  - DI default in **this** PR: empty, so the G78 flow is unchanged. 08 adds `p1-schelpenrij`, 09 `p2-schatkaart`, 10 `p3-vuurtorenlampen`. Each file enables only its own slot, so the flow never points at a missing page.
  - Engine and flow tests construct the registry with all three keys.
  - The endpoints answer 404 for a key that isn't enabled.
- **Completion and results:** `PupilResultBuilder` ignores `PuzzlesJson`. `CompletedAtUtc` doesn't depend on puzzles. A pupil can finish with 0 of 3 puzzles.

## 07.7 Strength sentences (`PuzzleStrengths`)
| Outcome | Key | Text (nl, new module `UiStringsLeerlingPuzzel.cs`) |
|---|---|---|
| p1 correct | `LeerlingPuzzel.Strength.Patterns` | "Jij ziet snel patronen." |
| p2 correct | `LeerlingPuzzel.Strength.Route` | "Jij houdt een route goed in je hoofd." |
| p3 correct | `LeerlingPuzzel.Strength.StepByStep` | "Jij denkt stap voor stap na." |
| wrong (any) | `LeerlingPuzzel.Strength.Try` | "Jij durft iets nieuws te proberen." |
| wrong (any) | `LeerlingPuzzel.Strength.Calm` | "Jij zoekt rustig naar een oplossing." |
| skipped | – | no sentence |

- **Wrong-answer pick:** deterministic, `Try` when `seed % 2 == 0`, else `Calm`. A reload shows the same sentence.
- **Register** `UiStringsLeerlingPuzzel.MergeNl` in `UiStrings.cs`, and add the prefix `LeerlingPuzzel.` to `UiStringsScholen.IsNlOnlyPrefix` (README §0).
- **Never** in `PupilResult.StoryKeysJson`, `PupilReportPdfService`, `TeacherCodeDetailDto`, `PupilStoryViewDto`, aggregates or logs (§P, K6). The strength key comes only from the pupil puzzle endpoints (and 08's optional pupil-only card).
- **Other copy in the new module** (the titles and Lobsy lines from the mockups):
  - `LeerlingPuzzel.{key}.Title`, `.Bubble` (the question in Lobsy's balloon), `.Next` ("Door naar de schatgrot" / "Naar het Pauze-eiland" / "Door naar de lagune")
  - `LeerlingPuzzel.Calm` = "Geen tijd · geen cijfer · fout is niet erg"
  - `LeerlingPuzzel.Skip` = "Sla over", `LeerlingPuzzel.Done` = "Klaar", `LeerlingPuzzel.WrongTitle` = "Lekker gepuzzeld!", `LeerlingPuzzel.Reveal` = "Zo zat het"
  - `LeerlingPuzzel.Of` = "Puzzel {0} van 3"
  - shape and colour labels (07.3)

  Copy the exact texts from `puzzels/html/*.html`; where a mockup and the ontwerpnotitie differ, the ontwerpnotitie wins. Apply the G78 wordlist guard to all `LeerlingPuzzel.*` values (≤ 14 words per line).

## 07.8 Copy that changes with the puzzles
- **`Leerling.Start.NoteTime`** (G78): "Duurt ongeveer 25 minuten." → "Duurt ongeveer 30 minuten." Change it in **10**, when all three puzzles are live; until then keep 25. Note it in the 10 PR.
- **`OuderbriefTemplate`:** add one clause for G78 classes: "Tussendoor maakt je kind drie korte puzzels; we bewaren alleen of een puzzel gedaan of overgeslagen is." Bump `ParentalInfoTexts.CurrentVersion` to `ouders-2026-10-v3`, also in **10** (same reason). N1 still applies.
- **`SchoolClassForm.razor` set note** (02): already says "3 puzzelpauzes". Keep it, since the stack ships together.

## 07.9 Tests (`Jobsy.Tests/Scholen/Puzzles/`)
- **`PuzzleSeedTests`:**
  - a golden test for fixed inputs: `("1","p1-schelpenrij","00000000-0000-0000-0000-000000000001")` → the exact `ulong`. Compute it once and commit it.
  - big-endian order
  - a different key, version or Id gives a different seed
- **`PuzzleRngTests`:** a golden sequence for seed 0 and seed 42 (first 10 `NextULong`); `NextInt` stays in range and has no bias (chi-square over 100 000 draws, p > 0.001).
- **`PuzzleDeterminismGoldenTests`:**
  - for 5 fixed Guids × 3 puzzles, the serialised instance (incl. answer) equals the committed JSON in `Jobsy.Tests/Scholen/Puzzles/golden/*.json`
  - the same input generated twice is byte-equal
- **`PuzzleAnswerPositionTests`:** over 1 000 seeds (Guids from a fixed list), for p1 and p3 each answer index 0–3 occurs 25 % ± 5 %. For p2, the end cell is not always the same cell (no cell > 15 %).
- **`PuzzleVariationTests`:** over 200 seeded pupils, ≥ 95 % of the distinct pairs get a **different** instance per puzzle (the surface: symbols/order/route), and the answer is on a different button for ≥ 60 % of the pairs (p1/p3).
- **`PuzzleStructureTests`** (10 000 seeds per puzzle at L2, plus 1 000 at L1/L3):
  - **p1:** the family is one of the level's families; 7 + `?`; 4 options unique **by shape** and **by colour**; exactly one option completes the pattern.
  - **p2:** run count, total steps and the perpendicular rule; inside the grid; no revisit; Manhattan(start, end) ≥ 2; 3 decor cells off the route and not the start; ≤ 200 attempts.
  - **p3:** valid latin square; exactly 1 gap (L2); the foil isn't in the grid; no `geel` anywhere; exactly one option is correct.
- **`PupilPuzzleApiTests`:**
  - GET returns no answer field: assert the JSON has no `answerIndex`/`answerCell`/`route` before answering
  - answer → `correct` + strength key; a second answer is idempotent; skip → no strength
  - VO pupil → 404
  - before the gate → 409 `step_pending`
  - after answering, `PuzzlesJson` has exactly the four fields
  - `PupilResult.StoryKeysJson` and the PDF bytes don't change between a pupil with all-correct and all-wrong puzzles (same answers otherwise)
- **`PupilPuzzleArchitectureTests`:**
  - types in `Jobsy.Core.Scholen.Puzzles` are referenced only from `Jobsy.Core.Scholen*`, `Jobsy.Infrastructure.Scholen*`, `Jobsy.Api.Controllers.PupilController`, `Jobsy.Web.Components.Leerling*` / `Pages.Leerling*` and tests
  - none from `Matching`, `Vacancy`, `Employer`, `Candidate`, `Banenkaart` namespaces
  - no `System.Random` / `GetHashCode` in `Jobsy.Core/Scholen/Puzzles`
- **`NoPupilNameFieldsTests`:** extend it to the new DTOs and `PupilPuzzleState`.
- **Teacher DTOs:** reflection test: no property on any `Teacher*Dto`/`SchoolPortal*Dto`/admin DTO is named or contains `Correct`, `Strength` or `Puzzle*Result` (11 adds only the count).

## Success criteria
- The same pupil gets byte-identical puzzles on every reload and device; different pupils get different instances with the answer on varying buttons.
- Storage is exactly status/correct/templateVersion/level; no endpoint or DTO outside the pupil puzzle API exposes `correct` or the strength.
- With no enabled puzzle keys (DI default in this PR) the G78 golden flow from 03a is unchanged.
- VO pupils never see or reach a puzzle (404).

Done → next: `08-puzzel-schelpenrij.md`.

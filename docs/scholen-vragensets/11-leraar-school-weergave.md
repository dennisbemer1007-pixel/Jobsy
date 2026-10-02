# 11. Teacher and school view: "n van 3 puzzels gedaan", test badges, one test at a time, rights matrix

Read `00-README.md` first (§S, §E, §P, D1, D4). Branch `cursor/vragensets-11` from `cursor/vragensets-10`.

> **Rules (same as README §0):**
> - Never merge, deploy or use rule `123`.
> - Never push to `main` or `acceptatie`; push only `cursor/vragensets-11`; no force-push.
> - ONE stacked PR into `acceptatie`.
> - Red or an unmet criterion → draft PR, stop, report.
> - Release build with 0 warnings.
> - No `.github/workflows` changes.

| | |
|---|---|
| Branch | `cursor/vragensets-11` |
| PR title | `feat(scholen): teacher sees "n van 3 puzzels gedaan" (never right/wrong), test badges, rights matrix for puzzle endpoints` |
| PR body starts with | `Stacked on #<PR 10> (cursor/vragensets-10)` |
| Screens | desktop 1440: `LeraarCodes`, `LeraarCodeDetail` (G78 code + VO code), `LeraarKlasOverview`, `SchoolClasses`, `SchoolResults` for a school with both tests (each tab) |

**Teachers still see only their own classes** (`ISchoolScopeService`). Nothing in this file widens a scope.

## 11.1 Puzzle count for the teacher (the only puzzle data any staff role sees)
- **DTO:** append `int? PuzzlesDone` to `TeacherCodeDetailDto` (`Jobsy.Core/Contracts/Scholen/TeacherPortalDtos.cs`) and to `TeacherCodeRowDto`.
  - G78 code: the count of puzzles with `status == done` (0–3). Skipped and "niet aangeboden" (K7) don't count.
  - VO code: `null`, so the UI shows nothing.
- **Computed in `TeacherPortalService`** by a single helper, `PupilPuzzleSummary.DoneCount(PuzzlesJson)` (Core), that reads **only** `status`. It **never** reads or returns `correct`. A test proves `DoneCount` is identical for all-correct vs all-wrong states.
- **UI:**
  - `LeraarCodeDetail.razor`: next to the progress line (~L40/L58): "{n} van 3 puzzels gedaan" (`Leraar.Detail.PuzzlesDone`). Show it for G78 only.
  - `LeraarCodes.razor`: optional small column "Puzzels" with "n/3" (G78 classes only). Leave it out if the table gets too wide at 1440; the detail is enough.
  - **Never** right/wrong, never the strength sentence, never which puzzle was skipped.
- **School admin and admin:** **no** puzzle data at all. No field in `SchoolPortal*Dto`, `SchoolClassDetail`, `SchoolResults`, `ScholenRapportage` or the CSV exports.
- **Aggregates:** puzzles are never aggregated (F6).

## 11.2 Test badges and labels (finishing 02/03b)
- **One class = one test** (README §S). Every code of a class shows the class's test; there is no per-code test, no "started with another test" notice and no mixed progress.
- **Teacher pages:**
  - `LeraarDashboard.razor` (class cards) and `LeraarKlasOverview.razor` show the test pill (`sch-pill`): "Groep 7/8" / "VO", always as text (02 added it to `SchoolClasses.razor`).
  - `LeraarCodeDetail.razor` shows the class's test: "Vragenlijst groep 7/8 (60 vragen)" / "Vragenlijst VO (100 vragen)".
- **Progress:** "x van 60" in a Groep78 class, "x van 100" in a VO class (03a: the total per class). `LeraarKlasOverview`'s progress bar uses that class total.
- **Teacher views are per class** (03b): `LeraarGroup.razor` shows one test, with a caption only. If `LeraarDashboard` shows numbers over several classes, check they're per class or grouped per test, never a total over both tests.
- **School and admin pages** (03b): check that `SchoolResults.razor` and `ScholenRapportage.razor` show **one test at a time** (the switch / required filter), with k ≥ 5 per test. Check that nothing renders both tests side by side, sums them or compares them, and that the CSV export is per test. Fix any gaps found; no new layout.
- **Copy for staff** stays "Leraar" as the role name (§0).

## 11.3 Rights matrix (`Jobsy.Tests/Scholen/ScholenRightsMatrix.cs`) and the roles doc
Add rows (data-driven, same style as the existing ones):

| Endpoint | Pupil (own session) | Teacher | School admin | Admin | Candidate / Employer / anonymous |
|---|---|---|---|---|---|
| `GET api/pupil/puzzles/{key}` | 200 (G78) / 404 (VO) | 401/403 | 401/403 | 401/403 | 401/403 |
| `POST api/pupil/puzzles/{key}/answer` | 200 | 401/403 | 401/403 | 401/403 | 401/403 |
| `POST api/pupil/puzzles/{key}/skip` | 200 | 401/403 | 401/403 | 401/403 | 401/403 |
| `GET api/pupil/puzzles` (08.4, if built) | 200 | 401/403 | 401/403 | 401/403 | 401/403 |
| Teacher code detail of **another** class | – | 403/404 (existing) | – | – | – |
| Class edit `level_locked` (02) | – | 403 | 409 when locked | 409 when locked | 403 |

- Assert the **exact** status the existing pupil endpoints return for staff cookies (match today's behaviour of `api/pupil/progress`); don't invent a new one.
- **Pupil A can't read pupil B's puzzle:** there is no id in the route; the session decides. Test that two pupil sessions get different instances for the same key.
- **`docs/security/roles-matrix.md`:** add the puzzle endpoints and the "teacher sees done-count only" rule.

## Tests
- `TeacherPortalUnitTests`:
  - `PuzzlesDone` counts done only (done/done/skipped → 2)
  - VO → null
  - all-correct and all-wrong give the same DTO (serialised, byte-equal)
- Reflection guard (from 07): still no `Correct`/`Strength` on any staff DTO; `PuzzlesDone` is the only puzzle field and is an int.
- bUnit `LeraarCodeDetail`:
  - G78 shows "2 van 3 puzzels gedaan"; VO shows no puzzle line
  - the test caption matches the class's test for every code
- `ScholenRightsMatrix` rows above.
- `SchoolResults`: a school with a G78 class (5 completed) and a VO class (4 completed) → the G78 tab shows numbers and the VO tab shows the existing "te weinig leerlingen" k-text. No markup contains both tests' numbers at once.
- `LeraarGroup`: one test caption, no sections, no comparison text.

## Success criteria
- A teacher sees "n van 3 puzzels gedaan" for a groep 7/8 pupil and nothing that hints at right or wrong; school admins and admins see no puzzle data.
- Every class shows which test it uses; totals are per test and the two tests are never shown together or compared.

Done → next: `12-tests-e2e-rapport.md`.

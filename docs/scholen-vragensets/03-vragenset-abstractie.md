# 03. Question-set abstraction: registry, test-aware flow (03a) · results and totals strictly per test (03b)

Read `00-README.md` first (§S, §DM, §E). **Always split:**
- **03a:** branch `cursor/vragensets-3a` from `cursor/vragensets-2`.
- **03b:** branch `cursor/vragensets-3b` from `cursor/vragensets-3a`.

> **Rules (same as README §0):**
> - Never merge, deploy or use rule `123`.
> - Never push to `main` or `acceptatie`; push only the current `cursor/vragensets-3*` branch; no force-push.
> - ONE stacked PR per part into `acceptatie`.
> - Red or an unmet criterion → draft PR, stop, report.
> - Release build with 0 warnings.
> - No `.github/workflows` changes.
> - Today's pupil behaviour stays **byte-identical** (for every existing class, via the `LegacyVo` def, and for new Groep78 classes via the G78 def).

| | 03a | 03b |
|---|---|---|
| PR title | `refactor(scholen): question-set registry + PupilFlow step engine (G78 unchanged)` | `feat(scholen): results and totals strictly per test, scoring version per test` |
| PR body starts with | `Stacked on #<PR 02> (cursor/vragensets-2)` | `Stacked on #<PR 03a> (cursor/vragensets-3a)` |
| Migration | none | `AddQuestionSetToAggregates` |

## 03.1 Today: the hardcoded places (verify; every one must go)
| Where | What |
|---|---|
| `Jobsy.Core/Scholen/PupilWorldCatalog.cs` | `ItemCount` 15 per world, `TotalTestItems = 60`, `ItemsPerPlate = 6`, `PlateCount = 10`, `AnswerLabels` (G78 keys), `SceneDepth` uses `/ 6` and `TotalTestItems`; the doc comment says "Pauze-eiland (after 30 items)" |
| `Jobsy.Core/Scholen/PupilQuestionBank.cs` | `MinId 9001`, `MaxId 9060`, `Count 60`, `ScoringVersion 1`, `IndexInWorld` switch with `-15/-30/-45` |
| `Jobsy.Infrastructure/Scholen/PupilPortalService.cs` | `answered >= 30` (×2, ~L361/L459) for the island, `_bank.AllItems.Count`, `PupilWorldCatalog.TotalTestItems`, `PlatesShed` |
| `Jobsy.Infrastructure/Scholen/PupilResultBuilder.cs` | `_bank as PupilQuestionBank ?? new PupilQuestionBank()`, `PupilQuestionBank.ScoringVersion` |
| `Jobsy.Infrastructure/Scholen/SchoolPortalService.cs` | `ProgressTotalQuestions = 60` (L136, used ~L1261/L1271) |
| `Jobsy.Infrastructure/Scholen/TeacherPortalService.cs` | `ProgressTotalQuestions` (L79 and 6 uses) |
| `Jobsy.Web/Components/Pages/Leerling/LeerlingReis.razor` | `new PupilQuestionBank()` (L147), `for i < 10` plates, cheer `(_state.AnsweredCount / 6) % 12`, `AnswerLabels`, rail from `PupilWorldCatalog.Worlds` |
| `Jobsy.Web/Components/Pages/Leerling/LeerlingStart.razor` | world list, `Leerling.Start.NoteTime` "Duurt ongeveer 25 minuten." |
| `Jobsy.Web/Components/Pages/Leerling/LeerlingEiland.razor` | `SceneDepth(AnsweredCount)` |
| Tests | `PupilQuestionBankTests` "exactly 60", `PupilPortalApiTests` totals, `PupilPagesNoCandidateChromeTests` (AnswerLabels) |

Run `git grep -n "\b60\b\|>= 30\|/ 6\b\|% 12" -- Jobsy.Core/Scholen Jobsy.Infrastructure/Scholen Jobsy.Web/Components/Pages/Leerling Jobsy.Web/Components/Leerling` and list every hit in the PR with what replaced it (or why it stays).

---

## 03a. Registry + flow engine (no behaviour change for G78)

### 03a.1 `PupilQuestionSetDef` + registry (Core, `Jobsy.Core/Scholen/QuestionSets/`)
```csharp
public sealed record PupilWorldPlan(string WorldKey, int ItemCount);
public sealed record PupilPuzzleSlot(int AfterItem, string PuzzleKey);   // filled in 07

public sealed record PupilQuestionSetDef(
    PupilQuestionSet Set,
    string Key,                         // "g78" | "vo" | "legacy-vo" (interim, removed in 04)
    string ScoringVersion,              // "g78-1" | "vo-1" | legacy "1" (written from 03b on)
    IPupilQuestionBank Bank,
    IReadOnlyList<PupilWorldPlan> Worlds,   // test worlds only, in order
    int IslandAfter,                    // 30 | 50
    IReadOnlyList<PupilPuzzleSlot> PuzzleSlots,
    IReadOnlyList<(int Value, string Key)> AnswerLabels,
    int ItemsPerPlate,                  // 6 | 10
    int PlateCount,                     // 10
    string CheerKeyPrefix,              // "LeerlingQ.Cheer." | "LeerlingQ.Vo.Cheer."
    int CheerCount,                     // 12
    bool PartBreakAfterIsland,          // false | true
    string StartTimeKey,                // "Leerling.Start.NoteTime" | "Leerling.Vo.Start.NoteTime"
    string LabelKey);                   // "School.QuestionSet.Groep78" | "School.QuestionSet.Vo"

public interface IPupilQuestionSetRegistry
{
    PupilQuestionSetDef ForClass(SchoolClass schoolClass); // = Get(schoolClass.QuestionSet); the ONLY way pupil code reaches a def
    PupilQuestionSetDef Get(PupilQuestionSet set);          // throws for unknown; never falls back to the other test
    bool IsLegacy(PupilQuestionSet set);                    // true for Vo until 04 (README §E)
    PupilQuestionSetDef? FindByItemId(PupilQuestionSet set, string itemId); // only within that test
    IReadOnlyList<PupilQuestionSetDef> All { get; }
}
```
- **`PupilQuestionSetRegistry`** (Core, no DI dependencies) registers two defs in 03a:
  - **`Groep78`** → the G78 def: today's `PupilQuestionBank`, worlds 15×4, island 30, no puzzle slots yet, today's `AnswerLabels`, plates 6/10, today's cheer keys, no part break, today's time key, `ScoringVersion "g78-1"`.
  - **`Vo`** → the interim **`LegacyVo`** def (README §E): the same 60 items and settings as today, `ScoringVersion "1"`, `IsLegacy = true`. It exists only so existing VO classes keep today's behaviour until the cut-over. 04 replaces it with the real VO def and deletes `LegacyVo`.
  - **No fallback.** There is no "serve the other test if this one isn't there" path anywhere. A class always gets the def of its own `QuestionSet`.
  - 04 adds VO; 07 adds the G78 puzzle slots.
- **Methods on the def:** `PlatesShed(answered)`, `SceneDepth(answered)` (same formula with `Bank.AllItems.Count` and `ItemsPerPlate`), `CheerKey(answered)`, `WorldOf(globalIndex)`, `IndexInWorld(globalIndex)`. Remove the static versions from `PupilWorldCatalog`.
  - Keep `PupilWorldCatalog.Worlds` for keys, titles and subtitles (incl. `pauze-eiland`), but **drop** `ItemCount`, `TotalTestItems`, `ItemsPerPlate`, `PlateCount` and `AnswerLabels`. Replace every use; don't mark them `[Obsolete]`, because warnings are errors.
- **`PupilQuestionBank`:**
  - make `IndexInWorld` generic, computed from the world counts in `BuildQuestions()`, not the `-15/-30/-45` switch
  - keep ids, order, keys and contents **identical**
  - keep the parameterless ctor (= G78)
  - rename nothing public that tests use, or update the tests in the same PR
  - extract the shared logic into `internal abstract class PupilQuestionBankBase` so 04 can add `PupilQuestionBankVo` without copy-paste
- **DI:** register `IPupilQuestionSetRegistry` as a singleton where `IPupilQuestionBank` is registered today (`Jobsy.Infrastructure/DependencyInjection.cs` ~L404) **and** in the Web host's service registration (Web renders pupil pages and needs the labels and text keys). Keep `IPupilQuestionBank` → G78 for now; remove it once nothing injects it.

### 03a.2 `PupilFlow` step engine (Core, pure, exhaustively unit-tested)
```csharp
public enum PupilFlowStepKind { Question, Puzzle, Island, Done }
public sealed record PupilFlowStep(PupilFlowStepKind Kind, int Index, string? ItemId, string? WorldKey, string? PuzzleKey);
public static PupilFlowStep Next(PupilQuestionSetDef def, IReadOnlyDictionary<string,int> answers,
                                 bool islandDone, IReadOnlyDictionary<string, PupilPuzzleStatus>? puzzles = null);
```
Rules, in this order:
1. `first` = index of the first unanswered item. If none, the step is `Done`.
2. **Puzzle** (from 07): if a slot has `AfterItem == first` and that puzzle has no status, the step is `Puzzle`. A pupil **at** the gate gets it; a pupil already past it doesn't (README K7).
3. **Island:** if `answers.Count >= def.IslandAfter` and the island isn't done, the step is `Island` (today's rule, set-aware).
4. Otherwise the step is `Question(first)`.

- **Server enforcement (new; also closes today's gap where item 31 could be saved before the island).** `SaveAnswerAsync` accepts an answer only for an item with `index <= first`, and only when the current step is `Question`, or the item was answered before (changing an earlier answer is always allowed).
  - Otherwise: 409 `step_pending` with `{ nextStep }`.
  - The UI never hits this. Test it through the API.
- **Item from the other test:** an item id that isn't in the class's test but is a known pupil id (9001–9060 or 9101–9200) → **400** `wrong_set`. An unknown id → 404 (as today). In 03a both defs hold ids 9001–9060, so this can only be tested with a fake def.

### 03a.3 Wiring
- **`PupilPortalService`:**
  - resolve `def = registry.ForClass(code.SchoolClass)` once per call (load the class with the code; it already is for the window check)
  - write nothing set-related on the code
  - use `def.Bank`, `PupilFlow.Next`, `def.PlatesShed`
  - `FirstUnansweredIndex` takes the def
  - `LoginAsync` returns `total = def.Bank.AllItems.Count`
- **DTOs** (append, defaults keep old callers compiling):
  - `PupilProgressStateDto` + `PupilQuestionSet QuestionSet` (the class's test), `string NextStep` (`question|puzzle|island|done`), `string? NextPuzzleKey`
  - `PupilAnswerResponse` + `NextStep`, `NextPuzzleKey`
  - `PupilChipsResponse` + `NextStep`, `NextPuzzleKey`
  - Keep `NeedsIsland` for compatibility, derived from `NextStep == "island"`.
- **`PupilResultBuilder`:** inject the registry; score with **the class's test** (`ForClass(code.SchoolClass)`); drop the cast and `new`. Completion = all items of **that** test answered. It reads only this code's answers of this test: no lookup of earlier answers, other codes or the other test.
- **Staff progress:**
  - `SchoolPortalService` / `TeacherPortalService` lose `ProgressTotalQuestions`
  - per class: `total = registry.ForClass(class).Bank.AllItems.Count` (the same for every code of the class), `current` = answered count (not `CurrentIndex`; same value today)
  - `SchoolPortalCodeRowDto` / `TeacherCodeRowDto` keep `ProgressCurrent/ProgressTotal`
- **Web:**
  - `LeerlingReis.razor` injects `IPupilQuestionSetRegistry` and uses `registry.Get(_state.QuestionSet)` for items, the rail (worlds from `def.Worlds` + the island + puzzle steps later), plates (`def.PlateCount`), cheer (`def.CheerKey`) and answer labels (`def.AnswerLabels`; 05 replaces the markup).
  - Navigation follows `NextStep`: `island` → `/leerling/eiland`, `puzzle` → `/leerling/puzzel/{key}` (route arrives in 08; until then `puzzle` can't occur), `done` → `/leerling/dit-ben-jij`.
  - `IsWorldDone` uses the def.
  - `LeerlingStart.razor` loads progress once (`Api.GetPupilProgressAsync()`) for the world list and `def.StartTimeKey`.
  - `LeerlingEiland.razor` uses `def.SceneDepth`.

### 03a tests
- **Golden flow:** a scripted run of 60 answers + chips in a VO class (`LegacyVo`) **and** in a Groep78 class (G78). Snapshot every `PupilProgressStateDto` and `PupilAnswerResponse`, minus timestamps. Compare both to the snapshot recorded on the **03a base commit before the refactor** (commit the snapshot first, like the golden scorer tests). Both must be identical to it except the new appended fields.
- **`PupilFlowTests`** (table-driven):
  - empty → Question 0
  - 29 answered → Question 29
  - 30 answered, no island → Island
  - island done → Question 30
  - 60 → Done
  - a fake set with a puzzle slot at 15: at the gate → Puzzle; status set → Question 15; already 20 answered → no puzzle (K7)
- **API:**
  - saving item index 31 before the island → 409 `step_pending`
  - changing an earlier answer while the island is due → 200
  - answer id 9101 in any class → 404 in 03a (no VO items yet); `wrong_set` is asserted with a fake def in 03a, for real from 04 on
- **Registry:**
  - `ForClass` returns the def of the class's own `QuestionSet` for both values, and `Get` throws for an unknown value (no fallback)
  - `IsLegacy(Vo)` is true in 03a; G78 is never legacy
  - a guard test greps `Jobsy.Infrastructure/Scholen` and `Jobsy.Web` for `registry.Get(` outside the registry and tests: pupil-facing code must use `ForClass`
- Existing `PupilQuestionBankTests`, `PupilPortalApiTests`, `TeacherPortal*Tests`, `SchoolPortal*Tests` stay green, with only constant references updated.

---

## 03b. Results and totals strictly per test (migration `AddQuestionSetToAggregates`)

### 03b.1 Data
- **No `PupilResult.QuestionSet` column.** A result belongs to a code, and the code to a class with exactly one test, so the test is `result.SchoolClass.QuestionSet`. `PupilResultBuilder` writes `ScoringVersion = def.ScoringVersion` (`"g78-1"`, legacy `"1"` for `LegacyVo`, `"vo-1"` from 04).
- **Migration `AddQuestionSetToAggregates`:**
  - `UPDATE "PupilResults" SET "ScoringVersion"='g78-1' WHERE "ScoringVersion"='1' AND "SchoolClassId" IN (SELECT "Id" FROM "SchoolClasses" WHERE "QuestionSet"=1)` (normally 0 rows). Results in VO classes keep `"1"`: legacy, deleted by 04's cut-over (README §E).
  - add `SchoolClassAggregates.QuestionSet` (backfill `CASE WHEN "Level"=8 THEN 1 ELSE 2 END`) and `SchoolYearAggregates.QuestionSet` (backfill **2**: every existing year/platform row was computed from VO classes)
  - indexes `(SchoolId, SchoolYearStart, QuestionSet)` on both aggregate tables
  - Down reverses (drop the columns; `'g78-1'` → `'1'`)
- **`SchoolAggregateSnapshotter`:**
  - class aggregates per class: one test by definition, so the row gets `QuestionSet = class.QuestionSet`
  - school-year aggregates **per (school, year, test)**, plus the platform row (`SchoolId null`) **per test**
  - the k ≥ 5 check (`SchoolAnonymity.MinGroupSize`) runs **per test group**
  - never one row over both tests
- **`ClassResultsAggregator`:** unchanged math. **Callers** group results by test (via the class) first:
  - `SchoolPortalService` ~L274 (school RIASEC top 3) and ~L817 (class totals)
  - `TeacherPortalService` ~L176 (overview), ~L272 and ~L631 (group)
  - `SchoolReportingService` (admin rapportage, CSV export)
  - **Guard:** the aggregator takes a `PupilQuestionSet` argument and throws `InvalidOperationException` when any input result's scoring version doesn't belong to that test (`g78-*` for G78; `vo-*` or legacy `"1"` for VO). Cover it with a test.

### 03b.2 DTOs + UI (one test at a time, no comparison)
- **Teacher views** (`LeraarGroup.razor`, `LeraarKlasOverview.razor`, `TeacherGroupInsightsDto`): a class has one test, so there are **no** per-test sections. Add `PupilQuestionSet QuestionSet` to the DTO and show a small caption "Vragenlijst groep 7/8 (60 vragen)" or "Vragenlijst VO (100 vragen)". Any teacher view that combines **several** classes (check `LeraarDashboard`) groups by test and shows only per-class or per-test numbers, never a total over both.
- **School results** (`SchoolResults.razor`, school results DTO): `PupilQuestionSet QuestionSet` query parameter + DTO field.
  - If the school has classes of **one** test: no switch, just the caption.
  - If it has **both**: a test switch (two tabs, "Groep 7/8" / "VO", default = the test with the most classes). The page shows **one test at a time**. No side-by-side view, no combined total, no comparison text.
- **Admin rapportage** (`ScholenRapportage.razor`, `SchoolReportViewDto`): the "Vragenlijst" filter is **required** (Groep 7/8 / VO, default VO; no "Alle"). The CSV export is per test: the file name carries the test (`…-groep78.csv` / `…-vo.csv`) and a `vragenlijst` column. A request without a test → 400.
- **Strings:**
  - `School.QuestionSet.Groep78` = "Vragenlijst groep 7/8 (60 vragen)", `School.QuestionSet.Vo` = "Vragenlijst VO (100 vragen)"
  - `School.Results.TestSwitch` = "Vragenlijst"
  - `AdminScholen.Report.Filter.QuestionSet`
  - **No** "not comparable" note, and no string that compares the tests.
  - While VO is `IsLegacy` (03b–04), the VO label reads "Vragenlijst VO". The "(100 vragen)" part arrives with 04.

### 03b tests
- **Scoring per test:** G78 percentages are byte-identical to today for 200 random complete answer sets (golden snapshot recorded before the change). `ScoringVersion` is `"g78-1"` for a Groep78 class and `"1"` for a VO class (legacy, until 04).
- **Migration:** `"1"` → `"g78-1"` only for results in Groep78 classes; VO-class results unchanged; aggregate backfill as above.
- **Aggregates:**
  - a school with a G78 class (6 results) and a VO class (6 results) → two school-year rows (one per test) and two platform rows; never a row over both
  - k ≥ 5 per test: G78 6 + VO 3 → G78 row written, VO masked
  - the aggregator guard throws on a result from the other test
- **API/bUnit:**
  - teacher group → a caption only, no sections
  - school results for a school with both tests → the switch, and each tab shows only its own test's numbers
  - admin rapportage without a test → 400; CSV per test
  - a guard asserts no rendered page or export contains both test labels in one result block
- **Rights:** unchanged (`ScholenRightsMatrix`): a teacher only gets their own class; the school admin only gets totals.

## Success criteria
- **03a:** the G78 golden flow is identical; no hardcoded 60/30/15/6/12 is left in the pupil flow; `SaveAnswerAsync` enforces the step order; Web no longer creates a bank.
- **03b:** results carry their test's scoring version; no total, page or export combines or compares the two tests; k ≥ 5 per test.

Done → next: `04-vo-set-inhoud.md`.

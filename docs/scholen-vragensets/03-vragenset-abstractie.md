# 03. Question-set abstraction: registry, set-aware flow (03a) · results and totals per set (03b)

Read `00-README.md` first (§S, §DM, K1). **Always split:**
- **03a:** branch `cursor/vragensets-3a` from `cursor/vragensets-2`.
- **03b:** branch `cursor/vragensets-3b` from `cursor/vragensets-3a`.

> **Rules (same as README §0):**
> - Never merge, deploy or use rule `123`.
> - Never push to `main` or `acceptatie`; push only the current `cursor/vragensets-3*` branch; no force-push.
> - ONE stacked PR per part into `acceptatie`.
> - Red or an unmet criterion → draft PR, stop, report.
> - Release build with 0 warnings.
> - No `.github/workflows` changes.
> - Groep 7/8 behaviour stays **byte-identical**.

| | 03a | 03b |
|---|---|---|
| PR title | `refactor(scholen): question-set registry + PupilFlow step engine (G78 unchanged)` | `feat(scholen): results and totals per question set (never mixed), scoring version per set` |
| PR body starts with | `Stacked on #<PR 02> (cursor/vragensets-2)` | `Stacked on #<PR 03a> (cursor/vragensets-3a)` |
| Migration | none | `AddQuestionSetToResults` |

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
    string Key,                         // "g78" | "vo"
    string ScoringVersion,              // "g78-1" | "vo-1" (written from 03b on)
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
    PupilQuestionSetDef Get(PupilQuestionSet set);        // throws for unknown
    bool IsAvailable(PupilQuestionSet set);               // Vo = false until 04
    PupilQuestionSetDef Serve(PupilQuestionSet requested); // requested if available, else Groep78
    PupilQuestionSetDef? FindByItemId(string itemId);
    IReadOnlyList<PupilQuestionSetDef> All { get; }
}
```
- **`PupilQuestionSetRegistry`** (Core, no DI dependencies) registers **G78 only** in 03a: today's `PupilQuestionBank`, worlds 15×4, island 30, no puzzle slots, today's `AnswerLabels`, plates 6/10, today's cheer keys, no part break, today's time key.
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
- **Item from another set:** an item id that `registry.FindByItemId` resolves to another set than the pupil's served set → **400** `wrong_set`. An unknown id → 404 (as today).

### 03a.3 Wiring
- **`PupilPortalService`:**
  - resolve `def = registry.Serve(PupilQuestionSetResolver.Effective(code, class))` once per call
  - pin `code.QuestionSet ??= def.Set` on the first answer (replaces 02.4's constant)
  - use `def.Bank`, `PupilFlow.Next`, `def.PlatesShed`
  - `FirstUnansweredIndex` takes the def
  - `LoginAsync` returns `total = def.Bank.AllItems.Count`
- **DTOs** (append, defaults keep old callers compiling):
  - `PupilProgressStateDto` + `PupilQuestionSet QuestionSet`, `string NextStep` (`question|puzzle|island|done`), `string? NextPuzzleKey`
  - `PupilAnswerResponse` + `NextStep`, `NextPuzzleKey`
  - `PupilChipsResponse` + `NextStep`, `NextPuzzleKey`
  - Keep `NeedsIsland` for compatibility, derived from `NextStep == "island"`.
- **`PupilResultBuilder`:** inject the registry; score with the **code's served set** (`Get(code.QuestionSet ?? class.QuestionSet)`); drop the cast and `new`. Completion = all items of **that** set answered.
- **Staff progress:**
  - `SchoolPortalService` / `TeacherPortalService` lose `ProgressTotalQuestions`
  - per code: `total = registry.Serve(effective).Bank.AllItems.Count`, `current` = answered count (not `CurrentIndex`; same value today)
  - `SchoolPortalCodeRowDto` / `TeacherCodeRowDto` keep `ProgressCurrent/ProgressTotal`
- **Web:**
  - `LeerlingReis.razor` injects `IPupilQuestionSetRegistry` and uses `registry.Get(_state.QuestionSet)` for items, the rail (worlds from `def.Worlds` + the island + puzzle steps later), plates (`def.PlateCount`), cheer (`def.CheerKey`) and answer labels (`def.AnswerLabels`; 05 replaces the markup).
  - Navigation follows `NextStep`: `island` → `/leerling/eiland`, `puzzle` → `/leerling/puzzel/{key}` (route arrives in 08; until then `puzzle` can't occur), `done` → `/leerling/dit-ben-jij`.
  - `IsWorldDone` uses the def.
  - `LeerlingStart.razor` loads progress once (`Api.GetPupilProgressAsync()`) for the world list and `def.StartTimeKey`.
  - `LeerlingEiland.razor` uses `def.SceneDepth`.

### 03a tests
- **G78 golden flow:** a scripted run of 60 answers + chips. Snapshot every `PupilProgressStateDto` and `PupilAnswerResponse`, minus timestamps. Compare it to a snapshot recorded on the **03a base commit before the refactor** (commit the snapshot first, like the golden scorer tests). It must be identical except the new appended fields.
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
  - answer id 9101 for a G78 pupil → 404 in 03a (no VO yet), 400 `wrong_set` from 04 on (assert via a fake set in 03a)
- **Registry:**
  - `Serve(Vo)` returns G78 while VO isn't registered
  - a `Vo` class pupil's first answer pins `Groep78` (K1 safety)
- Existing `PupilQuestionBankTests`, `PupilPortalApiTests`, `TeacherPortal*Tests`, `SchoolPortal*Tests` stay green, with only constant references updated.

---

## 03b. Results and totals per set (migration `AddQuestionSetToResults`)

### 03b.1 Data
- **`PupilResult.QuestionSet`** (int, not null). `PupilResultBuilder` writes the served set and `ScoringVersion = def.ScoringVersion`.
- **Migration:**
  - add `PupilResults.QuestionSet` (backfill **1**) and `UPDATE "PupilResults" SET "ScoringVersion"='g78-1' WHERE "ScoringVersion"='1'`
  - add `SchoolClassAggregates.QuestionSet` and `SchoolYearAggregates.QuestionSet` (backfill **1**)
  - indexes `(SchoolId, SchoolYearStart, QuestionSet)` on both aggregate tables
  - Down reverses (`'g78-1'` → `'1'`)
- **`SchoolAggregateSnapshotter`:** class aggregates are written **per (class, set)** and school-year aggregates **per (school, year, set)**, plus the platform row (`SchoolId null`) per set. The k ≥ 5 check (`SchoolAnonymity.MinGroupSize`) runs **per set group**: a class with 6 VO + 3 G78 results writes the VO row and masks G78.
- **`ClassResultsAggregator`:** unchanged math. **Callers** group results by `QuestionSet` first:
  - `SchoolPortalService` ~L274 (school RIASEC top 3) and ~L817 (class totals)
  - `TeacherPortalService` ~L176 (overview), ~L272 and ~L631 (group)
  - `SchoolReportingService` (admin rapportage, CSV export)
  - Never pass a mixed list into it. Add a guard that throws `InvalidOperationException` on a mixed list, covered by a test.

### 03b.2 DTOs + UI ("not 1:1 comparable")
- **`TeacherGroupInsightsDto`, the school class results DTO and `SchoolReportViewDto`:** add `IReadOnlyList<…PerSetDto> Sets`. Each entry has `PupilQuestionSet Set`, `string SetLabel`, the existing blocks and `bool Masked`. Keep the old top-level fields filled **only** when exactly one set is present (compatibility), otherwise empty.
- **UI** (`LeraarGroup.razor`, `LeraarKlasOverview.razor`, `SchoolResults.razor`, `ScholenRapportage.razor`):
  - **One set present:** a small caption "Vragenlijst groep 7/8 (60 vragen)" or "Vragenlijst VO (100 vragen)".
  - **Two sets:** one section per set with that heading, plus a note (`role="note"`) `School.Results.SetsNotComparable` = "Uitkomsten van verschillende vragenlijsten zijn niet 1-op-1 te vergelijken. Daarom staan ze apart." A masked set shows the existing "minder dan 5" message.
  - **Admin rapportage:** a filter "Vragenlijst" (Alle / Groep 7/8 / VO). "Alle" shows the sets side by side, never summed. The CSV export gets a `vragenlijst` column, one row per set.
- **Strings:** `School.QuestionSet.Groep78` = "Vragenlijst groep 7/8 (60 vragen)", `School.QuestionSet.Vo` = "Vragenlijst VO (100 vragen)", `School.Results.SetsNotComparable`, `AdminScholen.Report.Filter.QuestionSet`.

### 03b tests
- **Scoring per set:** G78 percentages are byte-identical to today for 200 random complete answer sets (golden snapshot recorded before the change). `ScoringVersion` is `"g78-1"`.
- **Migration:** `"1"` → `"g78-1"` and `QuestionSet = 1` on existing results and aggregates.
- **Aggregates:**
  - a class with mixed results → two class rows, k ≥ 5 per set
  - the school-year row per set
  - the platform row per set
  - the aggregator guard throws on mixed input
- **API/bUnit:** teacher group and school results with 2 sets → 2 sections + the note; with 1 set → a caption only, no note.
- **Rights:** unchanged (`ScholenRightsMatrix`): a teacher only gets their own class; the school admin only gets totals.

## Success criteria
- **03a:** the G78 golden flow is identical; no hardcoded 60/30/15/6/12 is left in the pupil flow; `SaveAnswerAsync` enforces the step order; Web no longer creates a bank.
- **03b:** results carry their set and scoring version; no total anywhere mixes sets; k ≥ 5 per set; the note shows when two sets meet.

Done → next: `04-vo-set-inhoud.md`.

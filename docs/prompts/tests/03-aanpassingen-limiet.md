# 03. The 3-change limit really works: enforced on every save, history recorded, attempts completed

> **Rules (repeated in every file):**
> - Never merge, never deploy, never use rule `123` (`.cursor/rules/shortcut-123.mdc`).
> - Never push to `main` or `acceptatie`; push only the current file's `cursor/tests-*` branch; no force-push.
> - ONE PR per file into `acceptatie` (01 standalone, 02+ stacked).
> - Red build/tests or an unmet success criterion: push, open the PR as **draft**, stop and report. Don't start the next file.
> - Nothing unlocks a paid test without a **paid** status from Mollie (or the stub path, only in Development or with `JobsyAuth:AllowStubPayments=true`). The UI never shows `ex.Message`.
> - Don't change the candidate nav (order, items, labels). Dennis' order is a separate add-on.

| | |
|---|---|
| Branch | `cursor/tests-3` from `cursor/tests-2` (or the last 02 sub-branch) |
| PR title | `Tests 03: enforce the 3-change limit on all test saves, record adjustments, complete attempts, correct TestDetail counter` |
| Body starts with | `Stacked on #<PR 02> (cursor/tests-2)` |
| Mockups | `ts-d4`/`ts-m4` tile "Iets aanpassen? Dat mag nog {n} keer" (the page itself is 04) |
| Migration | `AddAssessmentAdjustmentEnforcement` (only if §1 needs columns/indexes) |

**Goal:** the promise on TestDetail ("Je kunt je antwoorden 3 keer aanpassen") becomes true. After finishing, a candidate can adjust a test at most 3 times (edits and retakes together), the count is visible, and history exists.

Closes: R1.

## 0. Today (verify first)
- `AssessmentAdjustmentRules.MaxAdjustments = 3`, `LimitErrorCode = "assessment_adjustment_limit"`, `Remaining(used)`.
- `IAssessmentAdjustmentService`: `GetAsync`, `EnsureRemainingAsync`, `RecordAsync(userId, kind, variant, type Edit|Retake, attemptId?, idempotencyKey?)`. **`RecordAsync` has no caller.**
- `AssessmentRetakeService`: `StartAsync` creates an `Open` attempt, `AbandonAsync`, `ListHistoryAsync` lists `Completed` attempts. **Nothing sets `Completed`.**
- `AssessmentAdjustmentsController`: `GET {kind}/adjustments`, `POST {kind}/retake` (the only `EnsureRemainingAsync` caller), `POST …/abandon`, `GET {kind}/history`.
- The 4 test controllers' PUT (e.g. `CandidateCompetenciesController`) and `DeepAnalysisController.Save` never check the limit, so a completed test can be edited and finished again without limit. Live: TestDetail shows "nog 3 van de 3 keer" for kandidaat, who has completed tests.

## 1. Rules (D5)
- A test (`kind`, `variant` Quick = free test, Deep = uitgebreide test) is **completed** once a save with `complete: true` succeeded.
- **After completion:**
  - A save that changes answers **without** `complete` is allowed, but the change is kept as a **draft**: stored on an `Open` attempt with `AnswersJson`. The scores stay the completed ones until the candidate finishes again.
  - A save **with** `complete: true` that differs from the completed answers:
    - is one **Edit** adjustment
    - it first calls `EnsureRemainingAsync` (limit reached ⇒ 409 `{ code: "assessment_adjustment_limit", remaining: 0 }`)
    - then `RecordAsync(type: Edit, attemptId, idempotencyKey: "{attemptId}:complete")`
    - then the attempt goes to `Completed` (`CompletedAtUtc`, `ScoresJson`, `PreviousSnapshotJson` = the old completed answers/scores) and the new scores become current
  - A save with `complete: true` identical to the completed answers is a no-op (200, no adjustment).
  - `POST retake` stays as is (checks the limit, creates an Open attempt with empty answers) and records **Retake** only when that attempt is completed (not at start), with the same idempotency key pattern.
  - `abandon` discards the draft; nothing is recorded.
- The first completion is not an adjustment. Journey saves (Dependency B) follow the same rules: before the first completion they're free; after it, they're drafts.
- Deep (`variant` Deep) has its own count of 3, independent of Quick.
- Everything runs in one transaction per save; the idempotency key makes retries safe (the 01 autosave may resend).

## 2. Where it's enforced
- A new `AssessmentSaveGuard` (Infrastructure, scoped) used by all 5 save paths:
  - `CandidateCompetencyService`, `CandidateCareerInterestService`, `CandidateCulturePersonalityService` (also `/candidate/disc`), `CandidateValuesService`, `DeepAnalysisService.SaveAsync`
  - `BeginAsync(userId, kind, variant)` → the state (NotCompleted | Completed + open attempt?)
  - `CommitAsync(...)` applies §1
- Controllers map `AssessmentAdjustmentLimitException` to 409 with the code (01.12 reserved it).
- `GET {kind}/adjustments` returns `{ max, used, remaining, hasDraft, lastAdjustedAtUtc }`.

## 3. UI (in today's layout; 04 restyles)
- `TestDetail`: the counter reads the endpoint ("Nog {remaining} van de 3 keer aanpassen"). At 0: "Je hebt je antwoorden 3 keer aangepast. Dit is je uitslag." and no edit/retake buttons.
- **Test pages after completion:**
  - Opening a completed test shows the draft banner "Je past je antwoorden aan. Pas als je op Afronden drukt, telt het als 1 van je 3 keer. Nog {remaining} over."
  - "Stoppen zonder aanpassen" calls `abandon`.
  - At 0 remaining the page shows the result link instead of questions.
- 409 during "Afronden": the friendly text `TestErr.Limit` "Je kunt deze test niet meer aanpassen." + link to the result.

## 4. Privacy
- Attempts and adjustments are in the export (kind, variant, type, dates; answers only for the candidate's own export) and removed on delete. Tests.

## Tests
- Unit/service:
  - first completion = no adjustment
  - edit + complete = 1; identical complete = 0
  - 4th complete ⇒ 409; retake counts at completion
  - Quick and Deep are counted separately
  - idempotent retry doesn't double count
  - the draft doesn't change current scores
- API: each of the 5 save endpoints enforces the limit (parameterised test).
- bUnit: the TestDetail counter from the endpoint and the 0 state; the draft banner on a test page.

## Success criteria
- No path lets a candidate finish a completed test more than 3 extra times per variant.
- TestDetail shows the real remaining count (the kandidaat demo: 3 before, 2 after one edit + finish).
- `ListHistoryAsync` returns completed attempts.

## Done → next
Push, open the PR (stacked on 02), note the number, go to `04-testpaginas.md`.

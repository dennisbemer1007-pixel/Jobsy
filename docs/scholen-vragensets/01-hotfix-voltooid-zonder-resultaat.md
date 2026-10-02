# 01. Hotfix: a pupil gets stuck when the result builder fails after the last answer

Read `00-README.md` first. Branch `cursor/vragensets-1` from `origin/acceptatie`. **Standalone:** this PR must be mergeable on its own, before the rest of the stack.

> **Rules (same as README §0):**
> - Never merge, deploy or use rule `123`.
> - Never push to `main` or `acceptatie`; push only `cursor/vragensets-1`; no force-push.
> - ONE PR into `acceptatie`.
> - Red or an unmet criterion → draft PR, stop, report.
> - Release build with 0 warnings.
> - No `.github/workflows` changes.

| | |
|---|---|
| Branch | `cursor/vragensets-1` |
| PR title | `fix(scholen): never leave a pupil "Completed" without a result; self-heal on read` |
| PR body starts with | `Stacked on: none (first in the stack)`, then the outcome of README "Before you start" |
| Mockups | none |

## 01.1 The bug (verify first with a failing test, then fix)
In `Jobsy.Infrastructure/Scholen/PupilPortalService.cs`, `SaveAnswerAsync` (around L452–476):
1. On the last answer it sets `progress.CompletedAtUtc = now` and `code.Status = PupilCodeStatus.Completed` and **saves**.
2. **Then** it calls `_results.BuildAsync(code.Id)` inside `try/catch` and only logs a warning on failure.

If the builder throws (DB hiccup, a bug in story/fit code), the code is `Completed` **without** a `PupilResult`. From then on:
- **Login** redirects to `/leerling/dit-ben-jij`.
- **`GetResultAsync`** answers 409 `not_completed` ("Je reis is nog niet klaar.") because `code.Result is null`.
- **`SaveAnswerAsync`** answers 409 `already_completed`.
- **Teacher and school:** the code counts as completed, but it has no result (`TeacherPortalService` ~L379 checks `Result is null`).
- Nothing ever retries `BuildAsync`; it's called only here. The pupil is stuck for good, which is serious under README rule 5.

**First commit:** a failing integration test in `Jobsy.Tests/Scholen/PupilPortalApiTests.cs` (or a new `PupilResultSelfHealTests.cs`):
- swap in an `IPupilResultBuilder` fake that throws on its **first** call and delegates to the real `PupilResultBuilder` afterwards
- answer all 60 items, then assert that the pupil can still reach a result

If you can't make the test fail on `origin/acceptatie` (someone fixed it already), **skip 01**: open no PR, say so in PR 02 and branch 02 from `origin/acceptatie`.

## 01.2 Fix (minimal; no behaviour change on the happy path)
- **`SaveAnswerAsync`, last answer:**
  - Don't set `Completed` before building. Save the answer, then call `BuildAsync`. It already sets `code.Status = Completed` and `CompletedAtUtc` on success and is idempotent.
  - **On failure:** keep the status `InProgress` with all answers saved. Log the warning **without** answers or codes (ids only). Return 200 with `Completed = false` and a new optional `ResultPending = true` on `PupilAnswerResponse`.
  - Keep the response record backwards compatible: append an optional parameter with a default.
- **Self-heal on read.** Add `private async Task<bool> EnsureResultAsync(PupilCode code, CancellationToken ct)`:
  - it runs when all items are answered (or the status is `Completed`) **and** `code.Result is null`
  - it calls `BuildAsync` once (try/catch, warning log) and reloads `code.Result`
  - Call it in `LoginAsync` before computing `redirect`, in `GetProgressAsync`, and in `GetResultAsync` before the `not_completed` check.
- **Web.** `LeerlingReis.razor`: when `ResultPending`, show the existing `Leerling.Reis.SaveFailed` style message with a new key `Leerling.Reis.ResultPending` = "Je antwoorden zijn bewaard. We maken je verhaal klaar. Probeer het zo nog eens." and a "Probeer opnieuw" button that reloads progress. `LeerlingDitBenJij.razor`: on 409 `not_completed` with all items answered, show the same text and retry once.
- **Teacher and school.** A `Completed` code without a result shows "Bezig met afronden" (new key `Leraar.Detail.ResultPending`) instead of an empty or broken detail. Don't count it in group results (they read `PupilResult` rows, so check it stays that way).

## Tests
- **Self-heal:** the builder fails once → the last answer returns `ResultPending` → `GET api/pupil/progress` self-heals → `GET api/pupil/result` is 200, with exactly one `PupilResult` row.
- **Builder keeps failing:** the status stays `InProgress`, no 409 dead end, and the teacher detail shows "Bezig met afronden".
- **Happy path unchanged:** the 60th answer gives `Completed = true` and a result exists (existing `PupilPortalApiTests` stay green).
- **Logging:** the warning log contains no answer JSON and no plaintext code (assert on the captured log message).

## Success criteria
- No path leaves `Status == Completed && Result == null` without a self-heal on the next pupil read.
- Release build has 0 warnings and all tests are green.

Done → next: `02-klasniveau-groep78.md`.

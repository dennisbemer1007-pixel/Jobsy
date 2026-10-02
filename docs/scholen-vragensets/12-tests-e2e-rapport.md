# 12. Dev-only scholen seed, `ScholenVragensetsMobileSmokePlaywrightTests`, cross-cutting guards, docs, stack-end report

Read `00-README.md` first (§0 "Playwright in CI", §P). Branch `cursor/vragensets-12` from `cursor/vragensets-11`.

> **Rules (same as README §0):**
> - Never merge, deploy or use rule `123`.
> - Never push to `main` or `acceptatie`; push only `cursor/vragensets-12`; no force-push.
> - ONE stacked PR into `acceptatie`.
> - Red or an unmet criterion → draft PR, stop, report.
> - Release build with 0 warnings.
> - **No `.github/workflows` changes, and no changes to `.github/scripts/start-ci-stack.sh` either.**

| | |
|---|---|
| Branch | `cursor/vragensets-12` |
| PR title | `test(scholen): vragensets e2e smoke (G78 + VO, puzzles, answer scale), dev seed, guards, docs` |
| PR body starts with | `Stacked on #<PR 11> (cursor/vragensets-11)` |

## 12.1 Development-only scholen seed
- **Why:** the CI stack (`.github/scripts/start-ci-stack.sh`) already runs with `ASPNETCORE_ENVIRONMENT=Development` and `Seed__Enabled=true`, so `DatabaseSeedHostedService` → `JobsyDbSeeder.SeedDataAsync` runs there. Nothing in CI needs to change.
- **New** `Jobsy.Infrastructure/Data/ScholenVragensetsDemoSeeder.cs`, called from `JobsyDbSeeder.SeedDataAsync` (both branches, like the other demo seeders, inside a `try/catch` that logs a warning).
- **Hard guard:** it runs **only when `IHostEnvironment.IsDevelopment()`**. `Seed:Enabled` alone is **not** enough, because `allowSeed` is also true for non-Development hosts with `Seed:Enabled` (`DatabaseSeedHostedService` ~L45). Pass the environment in; return immediately otherwise. A unit test proves it's a no-op for `Production` and `Staging` even with `Seed:Enabled=true`.
- **Idempotent:** a marker in `PlatformLogs` (`Category = "Seed"`, `Message = "scholen-vragensets-demo-v1"`), same pattern as `EnterpriseWestlandVacanciesSeeder`.
- **What it creates:**
  - **School:** "Demo Basisschool & College (dev)", agreement recorded, `SchoolsEnabled` turned on in `PlatformFeatureSettings` (Development only).
  - **Classes:**
    - **7A** (`SchoolLevel.Groep78`, Year 7 → G78)
    - **2B** (`SchoolLevel.Havo`, Year 2 → VO)
  - **Test window** open for the current school year; parental info confirmed on the current `ParentalInfoTexts.CurrentVersion`.
  - **Codes:** 3 per class with **fixed** codes, via a new **dev-only** overload `PupilCodeService.CreateWithPlaintextAsync(SchoolClass, string code)`.
    - It's `internal`, or `[EditorBrowsable(Never)]` plus an environment check that throws outside Development.
    - It still stores only `LookupHash` + `CodeProtected`.
    - Codes must pass `PupilCodeFormatTests`' format; pick e.g. `G7A-DEV1`, `G7A-DEV2`, `G7A-DEV3`, `V2B-DEV1` …, adjusted to the real format.
  - **A teacher** (dev account, existing `TestAccountsSeedService` pattern) linked to 7A only, so the smoke can check scope.
- **No names**, no free text. `NoPupilNameFieldsTests` stays green.
- **`TestAccountsSeedService`** (02 added 7A there) stays as it is. Don't merge the two seeders.

## 12.2 `Jobsy.Tests/ScholenVragensetsMobileSmokePlaywrightTests.cs`
- **Naming and skipping:** the name ends in `MobileSmokePlaywrightTests`, so the existing `--filter-class '*MobileSmokePlaywrightTests'` in `pr-tests.yml` runs it with no workflow edit. Soft-skip without `JOBSY_E2E_BASE_URL` (pattern from `MobileSmokePlaywrightTests` / `Acc2709PlaywrightTests`).
- **If `/leerling` shows `School.FeatureDisabled`** (seed didn't run): soft-skip with a clear message. Don't fail CI for a missing seed, but say so in the PR.
- **Viewports:** 360×780, 390×844 and 1366×768.
- **Scenarios** (each a separate test; all log in via `/leerling` with a seeded code):
  1. **G78 login:** choose the school + 7A, enter `G7A-DEV1` → start screen shows "Duurt ongeveer 30 minuten." → first question shows the G78 labels "Nee … Ja!".
  2. **VO login + answer scale:** 2B + `V2B-DEV1` → the 5 labels "Klopt niet … Klopt helemaal", **no emoji** in the scale (assert the text has no characters in the emoji ranges).
     - At each viewport: `document.documentElement.scrollWidth <= window.innerWidth` (no horizontal scroll), and every option's bounding box ≥ 44×44.
     - Keyboard: focus the scale, press `3` → "Klopt deels" is `aria-checked="true"`.
  3. **G78 puzzle 1:** answer 15 items (any value) → lands on `/leerling/puzzel/p1-schelpenrij`.
     - "Geen tijd · geen cijfer · fout is niet erg" is visible; there is no element matching `[class*=timer]`.
     - Reload → the same option labels in the same order.
     - Pick an option + Klaar → a result view with a strength sentence; no element with a red/error class.
  4. **G78 puzzles 2 + island:** continue to 30 → p2. Skip → `/leerling/eiland` → save chips → question 31.
  5. **G78 puzzle 3 + rail:** at 45 → p3. The header shows "Puzzel 3 van 3 · 45 van 60 klaar"; on desktop the rail lists "Puzzel 3".
  6. **VO part break:** answer 50 → island → after chips, the part-break panel → "Stoppen voor nu" lands on `/leerling/stop?done=deel1`. Log in again → question 51.
  7. **Puzzle determinism across pupils:** `G7A-DEV1` and `G7A-DEV2` at puzzle 1 → the option label sequences differ (allowed to be equal only if the 07 variation test says the pair collides; then use DEV3).
  8. **Reduced motion:** `ReducedMotion.Reduce` context → the result view has no running CSS animations (`document.getAnimations().length == 0`).
- **Class-create drawer:** the school portal needs MFA.
  - Use the TOTP helper from `AuthMfaE2EPlaywrightTests` **if** it can log in a seeded school admin. Then: open "Nieuwe klas", pick "Basisschool" → only groep 7/8 years; pick "Middelbare school" → levels + klas 1–6; screenshot at 390 and 1366.
  - **If that helper can't be reused without new secrets**, cover the drawer with bUnit (`SchoolClassFormBunitTests`, which 02 may already have) and say so in the PR. Don't add secrets or workflow env.
- **Screenshots:** save to `artifacts/playwright-smoke/<utc-timestamp>/scholen-vragensets/` (same pattern as `MobileSmokePlaywrightTests`) and attach the key ones to the PR.

## 12.3 Cross-cutting guards (unit; add what isn't there yet)
- **Scoring per test** (`PupilScoringPerSetTests`):
  - for G78 and VO: all-1 → 0, all-5 → 100, all-3 → 50 per dimension
  - a reversed item flips (6 − raw)
  - G78 matches the pre-stack golden values (`LikertCategoryScorerGoldenTests` untouched)
  - VO dimension keys == G78 dimension keys
  - `ScoringVersion` is `g78-1` / `vo-1`; no result with legacy `"1"` can be written any more (04)
- **Class level → test** (`SchoolLevelRulesTests` + API): every `SchoolLevel` value maps to exactly one test (Groep78 → G78, all others → VO); year validation (7/8 vs 1–6).
  - The class level is the only source: login, progress, answers, result and teacher detail all report the class's test for every code of the class.
  - The level lock (02) holds once any code has started.
- **Puzzle seeding determinism:** 07's golden tests stay green. Add one test that re-runs the golden inputs in a **fresh process-independent** way: no static state, a new generator instance per call.
- **Separate tests, no carry-over:**
  - a school with a G78 class and a VO class → every aggregate row has one `QuestionSet`, and no DTO, page or CSV combines or compares the two tests
  - a code's result is built only from its own answers in its class's test: a test with a second code (other test) in the same school proves nothing from it is read
  - the reflection guard from 02 (no `PupilQuestionSet` on `PupilCode`/`PupilProgress`/`PupilResult`) stays green
- **Privacy sweep:**
  - `NoPupilNameFieldsTests` covers all new DTOs and entities
  - a log-capture test over a full G78 run (answers, puzzles, chips) asserts no log line contains an answer value pattern, `correct`, a strength key or a plaintext code
- **Strings:** `UiStringsParityTests` green, with nl-only prefixes incl. `LeerlingPuzzel.`; no literal Dutch in new `.razor` files (existing guard).
- **No "klas 2"** left in droombaan routes (06), and no "60" literal left in pupil-facing strings except G78-only keys (04/03 guards).

## 12.4 Docs
- `docs/scholen/vragensets.md` (new, short):
  - the §S table
  - how the class level picks the test (only source; one class = one test; the lock rule)
  - the cut-over of existing VO classes (README §E: legacy answers deleted, codes restart; no archive, no carry-over)
  - totals strictly per test, never compared
  - the puzzle privacy rules
  - how to run the dev seed and the smoke locally (`ASPNETCORE_ENVIRONMENT=Development`, `JOBSY_E2E_BASE_URL=http://localhost:5201`)
- `docs/adr/0006-school-roles-and-pupil-codes.md`: an "Addendum 2026-10: question sets and puzzles" paragraph (two separate anonymous tests chosen by the class level; pupil codes stay code-only; no answers carried over between tests; puzzles store done/skipped + correct; the teacher sees the count only).
- `docs/ROUTES.md`, `docs/security/roles-matrix.md`: check that 08 and 11 updated them.
- `CHANGELOG.md`: one entry for the stack (Unreleased).

## 12.5 Stack-end report (in the PR body of 12 and as the final message)
- file → branch → PR number → status (open / draft + why)
- migrations: `AddClassQuestionSet` (02), `AddQuestionSetToAggregates` (03b), `StartVoTestFresh` (04, data only), `AddPupilPuzzles` (07), with backfill/cut-over counts from the local seeded run
- **"Needs content review by Dennis":**
  - the VO strings vs the CSV
  - the VO cheers
  - the 10 mbo routes + needs
  - the "docent" variants
  - the puzzle texts and strength sentences
  - the ouderbrief clause (v2/v3)
  - K8 (60 items for groep 7/8)
- **Known conflicts:** K2–K10 (K1 withdrawn, see §E), each with how it was handled.
- **Follow-ups:** F1–F6.
- **Non-code to-dos for Dennis:** N1 (parent letter for groep 7/8 + DPIA/verwerkersovereenkomst/privacy page), N2 (production check before release: no real school data that the cut-over would delete), N3 (content review).
- Anything deferred ("Out of scope" items from every PR, e.g. 08.4 if skipped, the class-create Playwright test if replaced by bUnit).

## Success criteria
- On a fresh Development stack, the smoke runs green for G78 and VO at 360/390/1366, covering login, answer scale, the 3 puzzles, the island and the VO part break.
- The seed never runs outside Development.
- All guards green; Release build 0 warnings; no workflow or CI-script change.

**This is the last file.** Do not merge anything. Report and stop.

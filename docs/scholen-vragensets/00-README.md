# Lobsy voor scholen: two question sets (groep 7/8 · VO), class level picker, droombaan fixes, puzzle breaks (Cursor run book)

Cursor: **read this file completely**. Then **execute the files below strictly in order**, one at a time. Each file is one PR.

> **Rules (repeated in every file):**
> - **Branches:** file 01 branches from `origin/acceptatie`; every later file branches from the previous file's branch (stacked). ONE PR per file, into `acceptatie`.
> - **Never** merge, never deploy, never use rule `123` (`.cursor/rules/shortcut-123.mdc`).
> - **Pushes:** never push to `main` or `acceptatie`. Push only the current file's `cursor/vragensets-*` branch. No force-push.
> - **Red tests** or an unmet success criterion: push, open the PR as **draft**, stop and report. Don't start the next file.
> - **Release build has 0 warnings.** `TreatWarningsAsErrors` is on (`Directory.Build.props`). Fix every warning in the same PR. Never add `NoWarn`, `#pragma warning disable` or `[SuppressMessage]` to get green.
> - **Don't touch `.github/workflows/`.** New Playwright classes are named `*MobileSmokePlaywrightTests`, so the existing smoke filter in `pr-tests.yml` already picks them up (see §0).
> - **Pupils:** code only, no names. No AI, no vacancies, no employer visibility and no matching for anything pupil-related. Teachers see only their own classes (existing `ISchoolScopeService` rule).

**What this stack builds.** Dennis approved these decisions on **2 Oct 2026**:
- **Class level → question set.** When the school creates a class, it picks the kind of class: **Basisschool (groep 7 or 8)** or **Middelbare school** (vmbo-b … vwo, Mix, Anders), plus the leerjaar. The class level decides which question set every pupil code of that class gets. The pupil chooses nothing.
- **Two question sets:**
  - **Groep 7/8** = today's 60 questions, **unchanged**, with today's scale (Nee / Niet echt / Soms / Best wel / Ja!).
  - **VO** = the 100 approved questions in `vo-vragenset-100.csv`, with the scale **Klopt niet / Klopt meestal niet / Klopt deels / Klopt meestal / Klopt helemaal**, no smileys. Values stay 1–5.
  - **Scoring and totals:** the same dimension keys and the same scoring `(avg − 1) / 4 × 100`. Each set has its own id range and scoring version. Totals are shown per set and never mixed.
- **VO lesson flow:** two lesson parts, with a pause after the Pauze-eiland, and a calmer VO copy variant for the mascot.
- **Droombaan-checker:**
  - the "Nu: …" step comes from the class instead of the fixed "klas 2"
  - mbo occupations are added
  - the VO hint is "docent of mentor"
- **Puzzle breaks (groep 7/8 only).** Three visual puzzles come after questions 15, 30 and 45:
  1. *De schelpenrij*: patterns.
  2. *De schatkaart*: spatial; it leads into the Pauze-eiland.
  3. *De vuurtorenlampen*: logic.
  - **Generated per pupil:** a template seeded per pupil code gives the same puzzle on reload, and different children get a different instance with the answer on a different button.
  - **One level for everyone:** all puzzles use the middle level L2.
  - **No pressure:** no timer, no score, always skippable, and a wrong answer has no consequence. The result is a strength sentence.
  - **Minimal storage:** only done/skipped and correct are stored.
  - **Teacher view:** the teacher sees at most "3 van 3 gedaan", never right or wrong. Puzzles are never in matching and never visible to employers.
  - **VO:** no puzzles now. That is a follow-up.
- **Privacy for 10–11-year-olds:** code only, data minimisation, and a parent letter that Dennis handles outside the code (non-code to-dos below).

## Order

| # | File | Branch | Branches from | PR into |
|---|---|---|---|---|
| 01 | `01-hotfix-voltooid-zonder-resultaat.md`: a pupil can get stuck when `PupilResultBuilder` fails after the last answer (`Completed` without `PupilResult`): build first, self-heal on read. **Standalone:** mergeable on its own. **Conditional:** skip only if the failing test can't be written because the bug is gone. | `cursor/vragensets-1` | `origin/acceptatie` | `acceptatie` |
| 02 | `02-klasniveau-groep78.md`: `SchoolLevel.Groep78`, leerjaar rules (groep 7/8 vs klas 1–6), `PupilQuestionSet` on `SchoolClass` + pin on `PupilCode`, migration `AddClassQuestionSet` + backfill, class create/edit level picker (school portal) with lock rule, labels everywhere a level is shown | `cursor/vragensets-2` | `cursor/vragensets-1` | `acceptatie` |
| 03 | `03-vragenset-abstractie.md`: **03a** `IPupilQuestionSetRegistry` + `PupilQuestionSetDef` (G78 = today's bank, byte-identical), `PupilFlow` step engine (questions / puzzle slots / island / part break / done), every hardcoded 60/30/15/6 made set-aware, Web stops doing `new PupilQuestionBank()`. **03b** results + aggregates per set (`PupilResult.QuestionSet`, scoring version per set, aggregate rows per set, k ≥ 5 per set), migration `AddQuestionSetToResults`, "not 1:1 comparable" note | `cursor/vragensets-3a` → `-3b` | `cursor/vragensets-2` | `acceptatie` |
| 04 | `04-vo-set-inhoud.md`: VO bank (ids 9101–9200) from the CSV, `UiStringsLeerlingVragenVo.cs`, generated doc, VO guards (wordlist with `collega`/`klant` allowed), island after 50, two lesson parts (part-break screen), calm VO mascot/copy variant ("docent"), start-screen time, ouderbrief/lesbrief count lines | `cursor/vragensets-4` | `cursor/vragensets-3b` | `acceptatie` |
| 05 | `05-antwoordschaal-ui.md`: `LeerlingAnswerScale` component, set-specific labels (VO 5 labels without smileys, G78 unchanged), real radio semantics, long-label layout on 360–390 px, keyboard 1–5 | `cursor/vragensets-5` | `cursor/vragensets-4` | `acceptatie` |
| 06 | `06-droombaan.md`: "Nu: {nu}" from class level + year, 10 mbo occupations (catalog + routes + icons), hint and "leraar" lines per set ("docent of mentor" for VO) | `cursor/vragensets-6` | `cursor/vragensets-5` | `acceptatie` |
| 07 | `07-puzzels-engine.md`: Core puzzle engine (seed, own PRNG, 3 generators at L2, checker, strength sentences), `PupilProgress.PuzzlesJson` (migration `AddPupilPuzzles`), pupil API, flow gates after 15/30/45 for G78 | `cursor/vragensets-7` | `cursor/vragensets-6` | `acceptatie` |
| 08 | `08-puzzel-schelpenrij.md`: puzzle page shell `/leerling/puzzel/{key}` + De schelpenrij (question + result), skip, a11y, reduced motion | `cursor/vragensets-8` | `cursor/vragensets-7` | `acceptatie` |
| 09 | `09-puzzel-schatkaart.md`: De schatkaart (5×5 grid, route arrows, cell pick, reveal) + hand-off to the Pauze-eiland | `cursor/vragensets-9` | `cursor/vragensets-8` | `acceptatie` |
| 10 | `10-puzzel-vuurtorenlampen.md`: De vuurtorenlampen (3×3 latin square, 4 options) + puzzle steps in the journey rail and "Puzzel n van 3 · x van 60 klaar" | `cursor/vragensets-10` | `cursor/vragensets-9` | `acceptatie` |
| 11 | `11-leraar-school-weergave.md`: teacher sees "n van 3 puzzels gedaan" (never right/wrong), question-set badge on class lists and details, per-set sections polished, rights matrix for the new endpoints | `cursor/vragensets-11` | `cursor/vragensets-10` | `acceptatie` |
| 12 | `12-tests-e2e-rapport.md`: Development-only scholen seed (G78 + VO class, fixed codes), `ScholenVragensetsMobileSmokePlaywrightTests`, cross-cutting guards, docs, stack-end report | `cursor/vragensets-12` | `cursor/vragensets-11` | `acceptatie` |

- **Too big for one PR?** If a file is too big for one reviewable PR (> ~1.500 changed lines excluding tests, migrations, string resources and snapshots), split it into `a`/`b` at the seam the file names.
- **03 is always split** (03a/03b). The next file branches from the **last** sub-branch.

## Pointer prompt (the only prompt needed; it runs 01 … 12)
```
Run the Lobsy "scholen vragensets" stack. First: git fetch origin && git show origin/docs/scholen-vragensets:docs/scholen-vragensets/00-README.md — read it completely.
Then read and execute each file in docs/scholen-vragensets/ on that branch strictly in the order the README's table lists (01 … 12; 03 is always 03a + 03b; other a/b splits where a file allows it), one file = one PR.
File 01 branches from origin/acceptatie; every later file branches from the previous file's branch (stacked). Each opens ONE PR into acceptatie whose body starts with "Stacked on #<prev PR> (<prev branch>)" (01: "Stacked on: none (first in the stack)").
Before 01, run the checks in the README's "Before you start" section and say the outcome in PR 01.
Build in Release (0 warnings; TreatWarningsAsErrors is on) and test after each file; if tests fail or a success criterion can't be met, push, open that PR as draft, stop and report — don't start the next file.
Never merge, never deploy, never use rule 123, never push to main or acceptatie, no force-pushes, never edit .github/workflows.
At the end report: file → branch → PR number → status, the migration list, the content-review items for Dennis, the non-code to-dos and anything deferred.
```

## How to run
1. `git fetch origin`. Read:
   - this file
   - `.cursor/rules/design-system.mdc`, `docs/ROUTES.md`, `docs/security/roles-matrix.md`, `docs/adr/0006-school-roles-and-pupil-codes.md` and `docs/release-flow.md`
   - the earlier scholen spec for context: `git show origin/docs/scholen:docs/prompts/scholen/00-README.md`, plus its `05-vragenbank.md` and `06-verhaal-droombaan-pdf.md`
2. Run **Before you start** below.
3. For each file in the order above:
   1. Read the whole file.
   2. Create its branch from the "Branches from" column.
   3. Implement **only** that file's scope, plus §0.
   4. Run `dotnet build Jobsy.sln -c Release` (0 warnings) and `dotnet test --project Jobsy.Tests/Jobsy.Tests.csproj -c Release --filter-not-class '*Playwright*'`. Also run the Playwright classes the file names, against a local stack if you can.
   5. Small, clear commits. Push `cursor/vragensets-*` only. Open ONE PR into `acceptatie` with the file's title and the PR body items from §0.
   6. Note the PR number and go on.
4. **Stop and report** if:
   - tests fail and you can't fix them inside the file's scope
   - a success criterion can't be met
   - the code contradicts this spec in a way you can't resolve safely

   Push what you have, open that PR as **draft** with the failure described, and don't continue.
5. **Hotfix rule:** if you find a **serious** bug outside the current file's scope, don't fix it in the feature PR.
   - **Serious** means: data loss, a pupil stuck for good, a pupil seeing another pupil's data, a teacher seeing another class, or a name being stored.
   - Before 01 is pushed, add it to file 01 (the standalone hotfix).
   - After that, stop, describe it in the current PR body under "Serious bug found", and report. Dennis decides.
6. **Never** merge, deploy, or use rule `123`. **Never** push to `main` or `acceptatie`. No force-pushes.
   - **Migrations:** only 02 (`AddClassQuestionSet`), 03b (`AddQuestionSetToResults`) and 07 (`AddPupilPuzzles`) add one. Never regenerate or edit a lower file's migration.
   - **If `acceptatie` moves during the run:** don't rebase. Only when a conflict blocks you, `git merge origin/acceptatie` into the current branch (a normal merge commit) and say so in the PR body. If a newer migration landed on `acceptatie`, regenerate **only your own** migration after the merge.

## Before you start (say the outcome in PR 01)
- **Base.** Code references are from `origin/acceptatie` @ `32f47798` (2026-10-02 20:50 CEST). Re-check line numbers before editing.
- **Still 60-only?** `git grep -n "TotalTestItems = 60\|ProgressTotalQuestions = 60\|answered >= 30\|answers.Count >= 30" origin/acceptatie -- Jobsy.Core Jobsy.Infrastructure`.
  - Expect hits in `PupilWorldCatalog.cs`, `SchoolPortalService.cs` and `PupilPortalService.cs`. If they're gone, someone already made the flow set-aware: stop and report.
- **Level enum.** `git show origin/acceptatie:Jobsy.Core/Enums/SchoolLevel.cs`.
  - Expect `VmboB=0 … Anders=7`. If a primary level already exists, reuse it and say so.
- **Production data.** You can't query production. The migrations must be correct for **any** mix of classes, codes, progress and results (see 02 and 03b backfill rules). Never assume an empty table.
- **Feature gate.** Scholen sits behind `SchoolsEnabled` (`SchoolsFeatureGate`, `SchoolsFeatureMiddleware`). Nothing in this stack changes the gate.

---

## §0. Shared rules (every file)
- **Branches and PRs:**
  - Branches stack (see Order). ONE PR per file, always into `acceptatie`.
  - The PR body starts with `Stacked on #<prev PR> (<prev branch>)`. For 01 it is `Stacked on: none (first in the stack)`.
  - The diff includes lower PRs until they merge, so say which commits are this file's own.
- **Stop on red.** Release build with **0 warnings** plus the unit/integration tests after each file. If it's red and not fixable in scope: stop, push, draft PR, report.
- **Mockups.** They live on branch `docs/scholen-vragensets`, folder `docs/mockups/scholen-vragensets/`. Read them with `git fetch origin docs/scholen-vragensets && git show origin/docs/scholen-vragensets:docs/mockups/scholen-vragensets/<file> > /tmp/<file>`. Don't commit mockups to code branches.
  - `klas-niveau/`:
    - `kl-d01-nieuwe-klas-basisschool.png` (desktop 1366)
    - `kl-m01-nieuwe-klas-vo.png` (mobile 390 @2x)
    - `kl-m02-klas-bewerken-vergrendeld.png` (mobile, lock rule)
    - HTML in `html/`; builder `build_klas.py` (sample data; reference only, its font path points to the author's machine)
  - `puzzels/`:
    - `ontwerpnotitie.md` is the **puzzle contract**: where, why, privacy, seed, templates, levels, accessibility
    - PNGs: `overzicht.png`, `p{1,2,3}-*-mobiel-vraag.png`, `-mobiel-resultaat.png`, `-desktop.png`, `-variant-b-mobiel-vraag.png`, `-varianten.png`
    - HTML in `html/`
    - `puzzels.py` holds the reference generators `gen_p1`/`gen_p2`/`gen_p3`. They are **reference only**: they import a design module that is not in the repo, so they don't run here. Port the logic, not the Python RNG (see 07).
  - `vo-set/`:
    - `vo-vragenset-100.csv`: **the VO content source**; columns `world,dimension,reversed,text,scenario`, display order
    - `vo-vragenset-100.md`: the same 100 with counts, readability, the checks and the approved judgement calls
- **Strings.**
  - Pupil, school and teacher strings are nl-only modules (prefixes `School.`, `Leraar.`, `Leerling.`, `LeerlingQ.`, `LeerlingStory.`, `LeerlingDroom.`, `LeerlingPdf.` are exempt from parity; see the header of `UiStringsScholen.cs`).
  - The exemption is `UiStringsScholen.IsNlOnlyPrefix`. `LeerlingQ.Vo.*` and `Leerling.Vo.*` are already covered by it. The new prefix `LeerlingPuzzel.` must be added there.
  - Register new modules next to the existing ones in `UiStrings.cs` (`UiStringsLeerlingVragen.MergeNl(nl)` …).
  - No literal Dutch in `.razor`.
- **Tone.** B1, "je", short. G78 copy stays as it is today; the VO copy is calmer and less childish (no "schaaltje af", no "Ja!").
  - VO uses **"docent"**.
  - G78 keeps **"leraar"** (pupil-facing; staff UI keeps "Leraar" as the role name).
- **Docs and guards to update when routes or pages change:**
  - `docs/ROUTES.md` (`RoutesDocFreshnessTests`)
  - `Jobsy.Web/Seo/PageSeoCatalog.cs` (`Private(...)` for every pupil page; `PageSeoTests`)
  - `Jobsy.Web/Help/PageHelpDocs.cs` (`PageHelpDocsTests`)
  - `BlazorPageRoleAttributesTests` (pupil pages: `[Authorize(Policy = JobsyPolicies.PupilSession)]`)
  - `CHANGELOG.md`
- **Playwright in CI.** Name every new Playwright class `…MobileSmokePlaywrightTests`. The existing `--filter-class '*MobileSmokePlaywrightTests'` in `.github/workflows/pr-tests.yml` runs it in the smoke step, and `--filter-not-class '*Playwright*'` keeps it out of the unit step. **Do not edit any workflow file.** Soft-skip without `JOBSY_E2E_BASE_URL` (existing pattern, e.g. `Acc2709PlaywrightTests`).
- **Must NOT touch:**
  - adult scoring and catalogs beyond what a file names: `CompetencyTestCatalog`, `CareerTestCatalog`, `SchwartzValuesCatalog`, `CulturePersonalityCatalog`, `LikertCategoryScorer` (the golden tests in `LikertCategoryScorerGoldenTests` stay byte-identical)
  - the candidate, employer and matching code (`MatchingService`, `VacancyDiscovery`, banenkaart)
  - auth, MFA and the `Pupil` cookie scheme (`PupilAuthEndpoints`, `PupilSessionAuthorizationHandler`): only reuse them
  - `.github/workflows/*`, `render.yaml`, Cloudflare and Render settings
  - other stacks' in-progress `cursor/*` branches: never branch from or merge them
- **PR description:**
  - what changed and why, and which decision (D1–D5) it implements
  - screenshots: mobile 390 and desktop 1366 (pupil pages), desktop 1440 (school/teacher pages), for **both sets** where the screen differs
  - migration summary and backfill counts from a local run on a seeded DB
  - test list
  - "Out of scope / deferred"
  - "Needs content review by Dennis" for every new pupil-facing text

---

## §S. Question-set contract (the one table every file follows)

| | **Groep 7/8** (`PupilQuestionSet.Groep78 = 1`) | **VO** (`PupilQuestionSet.Vo = 2`) |
|---|---|---|
| Classes | `SchoolLevel.Groep78` (new, value **8**), `Year` 7 or 8 | every other level (`VmboB … Vwo`, `Mix`, `Anders`), `Year` 1–6 |
| Items | **60**, ids **9001–9060**, today's `PupilQuestionBank` + `UiStringsLeerlingVragen.cs`, **unchanged** | **100**, ids **9101–9200**, from `vo-vragenset-100.csv` → new `UiStringsLeerlingVragenVo.cs` |
| Per world | 15 · 15 · 15 · 15 | 25 · 25 · 25 · 25 |
| Distribution | today's (05.3 of the old spec) | Koraalrif 5 per competency (1 rev each) · Schatgrot R 5, others 4 · Vuurtoren 5 per value · Lagune Collaboration 5, others 4 (Informal 1 rev) |
| Pauze-eiland | after item 30 | after item 50 |
| Puzzles | after 15, 30 (→ island), 45 | none (follow-up F1) |
| Answer labels (values 1–5) | Nee · Niet echt · Soms · Best wel · Ja! (today's keys `Leerling.Answer.*`, today's look) | Klopt niet · Klopt meestal niet · Klopt deels · Klopt meestal · Klopt helemaal (`Leerling.Vo.Answer.*`), no smileys/emoji |
| Scoring | per dimension `(avg − 1) / 4 × 100`, reverse = 6 − raw, via the existing `Score(answers, items)` overloads | same |
| `PupilResult.ScoringVersion` | `"g78-1"` (backfill from `"1"`) | `"vo-1"` |
| Shell plates | 10 plates, 1 per 6 items | 10 plates, 1 per 10 items |
| Lesson parts | 1 (≈ 30 min incl. puzzles) | 2: **deel 1** = Koraalrif + Schatgrot + Pauze-eiland, then a part-break screen; **deel 2** = Vuurtoren + Lagune + Droombaan (≈ 40–45 min total) |
| Mascot / copy | today's `LeerlingQ.Cheer.1–12` | `LeerlingQ.Vo.Cheer.1–12` (calm, see 04) and "docent" variants |
| Forbidden words (text + example) | today's list (incl. `collega`, `klant`) | today's list **minus** `collega`, `klant` |
| Review doc (generated) | `docs/scholen/vragenbank-leerlingen.md` (only its title gets "groep 7/8") | `docs/scholen/vragenbank-leerlingen-vo.md` |
| UI label of the set | "Vragenlijst groep 7/8 (60 vragen)" | "Vragenlijst VO (100 vragen)" |

- **Which set a pupil gets:**
  - `SchoolClass.QuestionSet` is derived from `Level` on create and edit.
  - `PupilCode.QuestionSet` is **pinned on the first saved answer** to the set that was actually served. Before that it's `null` and follows the class.
  - Every read uses `code.QuestionSet ?? class.QuestionSet`.
  - This keeps in-progress answers valid, whatever happens to the class level.
- **Ids never overlap** (adult catalogs use 1–~200; pupil G78 9001–9060; VO 9101–9200). An answer id that doesn't belong to the pupil's set → **400** `wrong_set`.
- **Comparability:** percentages from different sets are **not 1:1 comparable**. Every total (teacher group, school results, admin rapportage, aggregates) is computed and shown **per set**, never mixed. k ≥ 5 applies **per set**.

## §DM. Data-model changes (only these)
| Migration (file) | Change |
|---|---|
| `AddClassQuestionSet` (02) | `SchoolClass.QuestionSet` int not null (backfill: `Level == 8 ? 1 : 2` → all existing rows **2**). `PupilCode.QuestionSet` int **null**: backfill **1** where the code has a `PupilProgress` with `AnswersJson` other than `{}`/empty **or** a `PupilResult`; otherwise `null`. Index `PupilCodes(SchoolClassId, QuestionSet)`. Enum `SchoolLevel` gets `Groep78 = 8` (no renumbering; stored as int). |
| `AddQuestionSetToResults` (03b) | `PupilResult.QuestionSet` int not null (backfill **1**, because every existing result was scored on the 60-set). `ScoringVersion` `"1"` → `"g78-1"`. `SchoolClassAggregate.QuestionSet` and `SchoolYearAggregate.QuestionSet` int not null (backfill **1**). Indexes `(SchoolId, SchoolYearStart, QuestionSet)`. |
| `AddPupilPuzzles` (07) | `PupilProgress.PuzzlesJson` text not null default `'{}'`. |

- **Why existing data is "groep 7/8 set".** Every class that exists today is a VO class, but its pupils answered the 60 items. Those 60 items are now the groep 7/8 set.
  - Their results keep their meaning, labelled "Vragenlijst groep 7/8 (60 vragen)".
  - Codes in those classes that **haven't started** get the VO set once 04 is live.
  - Dennis approved "existing classes default to VO". This pin rule is the safe reading of that decision. **Say it in PR 02.**

## §P. Privacy (every file; 10–11-year-olds now in scope)
- **No names, ever.**
  - No new free-text field anywhere in the pupil flow; the puzzles take taps only.
  - `NoPupilNameFieldsTests` stays green and covers the new columns and DTOs.
  - The `PupilNameGuard` and "Iets anders" rules stay as they are.
- **Data minimisation:**
  - Puzzles store exactly `status` (done/skipped), `correct` (bool, null when skipped), `templateVersion` and `level`, per puzzle.
  - No timestamps per puzzle, no attempts, no durations, no chosen option.
  - The question set is a class property, not personal data.
- **Who sees what:**
  - The **pupil** sees the strength sentence.
  - The **teacher** sees at most "n van 3 puzzels gedaan".
  - The **school admin** sees no puzzle data.
  - **Admin rapportage** has no puzzle data.
  - **Employers and matching:** nothing. Puzzle and pupil data never leave the Scholen namespaces (guard test in 07).
- **Never put puzzle outcomes in shared outputs.** Puzzle outcomes never go into `PupilResult.StoryKeysJson`, the PDF (`PupilReportPdfService`), teacher DTOs, aggregates or logs.
  - Reason: the teacher can open the pupil's story and PDF, and a different sentence for wrong answers would reveal right/wrong.
  - This deliberately narrows the ontwerpnotitie line "Die mag ook in 'Dit ben jij' staan": a pupil-only card is allowed (08), the story and PDF are not.
- **Logs and telemetry:** never log answers, puzzle outcomes, plaintext codes or `CodeProtected`. Ids are fine.
- **Retention:** puzzle data lives in `PupilProgress`, so the existing school-year retention (`SchoolRetentionService`) deletes it. Aggregates never include puzzles.

## Decisions (Dennis, 2 Oct 2026; *extra* = a default this spec adds, overridable)
- **D1. Class level picks the set.**
  - The school picks Basisschool (groep 7/8) or Middelbare school + level, and the leerjaar.
  - The pupil chooses nothing.
  - Migration + backfill: existing classes default to VO (§DM).
  - Teachers keep seeing only their own classes.
  - *extra:* a cross-family level change (basisschool ↔ middelbare school) is **blocked** once any code of the class has a pinned set. A change within VO is always allowed (mockup `kl-m02`).
- **D2. Two sets** per §S.
  - VO content = `vo-vragenset-100.csv`, as-is.
  - The VO wordlist allows `collega`/`klant`.
  - Totals are per set, with a note that they are not 1:1 comparable.
  - VO has two lesson parts, a pause after the Pauze-eiland and a calmer mascot copy.
  - *extra:* the G78 answer look stays exactly as today (text + dot). The "smiley" faces exist only in the printed booklet; see Known conflicts K2.
- **D3. Droombaan:**
  - the current year/level comes from the class
  - add mbo occupations
  - VO hint "Praat erover met je docent of mentor."
  - *extra:* VO also gets "docent" in the other pupil lines that say "leraar".
- **D4. Puzzles (G78 only)** per `puzzels/ontwerpnotitie.md`, with these concrete choices:
  - **seed** = `SHA-256("{templateVersion}:{puzzleKey}:{PupilCode.Id:D}")`. Lobsy doesn't keep the plaintext code (only an HMAC + a protected payload), and the `Id` survives a code replacement, which keeps progress.
  - **level** L2 for everyone
  - **one answer per puzzle**: no retry, idempotent
  - the teacher's "gedaan" counts `done` only
  - VO puzzles = follow-up F1
- **D5. Privacy** per §P. The parent letter is a non-code to-do (N1).
  - *extra:* 04 fixes only the factual count/time sentence in `OuderbriefTemplate` and bumps `ParentalInfoTexts.CurrentVersion` to `ouders-2026-10-v2`.

## Known conflicts between the decisions and the code (handled as described; say so in the PR named)
- **K1 (02/03). "Existing classes default to VO" vs existing answers.** Existing pupils answered the 60 items, so their codes and results are pinned to the 60-set (now called groep 7/8) and labelled that way, even in VO classes. Only unstarted codes move to VO.
- **K2 (05). "Smiley scale" for groep 7/8.** The app has **no** smileys today: it shows a dot + label (`.ll-answer__dot` in `LeerlingReis.razor`). The decision also says "unchanged", so the app look stays. Smileys are only in the printed groep 7 booklet. If Dennis wants faces in the app, that's a small follow-up (F4).
- **K3 (06). `DreamJobCatalog` is shared.** The candidate onboarding uses it too (`Components/Candidate/Onboarding/DreamJobStep.razor`). Adding 10 mbo occupations also adds them there. The spec accepts that, since they're real jobs. The PR says so.
- **K4 (06). Route texts after "Nu: …".** Lines like "Kies straks biologie en scheikunde" assume a lower VO year. With `{nu}` they read fine for groep 7/8 and klas 1–3. For klas 4–6 the "kies straks" detail is outdated (the profile is already chosen). That is follow-up F3 (content). 06 only fixes the "Nu:" step.
- **K5 (07). Seed source.** The ontwerpnotitie seeds with the printed pupil code. The server can't use it without decrypting `CodeProtected`, so it uses `PupilCode.Id` (D4). Same properties: deterministic per pupil, different per child.
- **K6 (07). Strength sentence in "Dit ben jij".** See §P: pupil-only card, never in story keys or PDF.
- **K7 (07). In-progress pupils at deploy.** A G78 pupil who is already past item 15/30/45 when 07 ships is **not** sent back to a puzzle. Puzzles are offered only when the pupil reaches the gate. Missed puzzles stay "niet aangeboden" and don't count as "gedaan".
- **K8 (04). The 60 items were written for 12–16-year-olds** (old spec 05: "Pupils (12–16, vmbo to vwo)"). Dennis decided they become the groep 7/8 set unchanged. Don't edit them; that's content review for Dennis (N3).
- **K9 (04). `OuderbriefTemplate` and `lesbrief-lobsy.html`** say "60 kindvriendelijke vragen … één lesuur", which is wrong for VO. 04 makes only the count/time line set-aware; the full letter text is N1.
- **K10 (all). "Leerling" error texts in `PupilPortalService`** are hardcoded Dutch strings ("Je leraar zet het weer open."). 04 moves the ones shown to pupils to string keys with a VO variant.

## Follow-ups (not in this stack; list them in the stack-end report)
- **F1** Puzzles for VO (other templates/levels; never in matching).
- **F2** Pauze-eiland chip labels for VO ("Bouwen & knutselen" → "Bouwen & klussen", "Kleine kinderen" → "Met kinderen werken"; extra VO chips). Chip **keys** must stay stable for `PupilDreamJobFit` (`ChipBouwen`, `ChipKleineKinderen`).
- **F3** Droombaan route details per year (klas 4–6, profile already chosen; groep 7/8 "naar de middelbare school" step).
- **F4** Optional smiley faces in the G78 answer scale (in-app), if Dennis wants it.
- **F5** Read-aloud button: the ontwerpnotitie mentions "een voorleesknop zoals bij de gewone vragen", but the app has no read-aloud today.
- **F6** Pilot measurement "±80 % lost de puzzel op", aggregated per template/level only. Not built now: nothing about puzzles is aggregated.

## Non-code to-dos for Dennis (not for Cursor; repeat them in the stack-end report)
- **N1 Parent letter for groep 7/8.** Write and approve an ouderbrief for parents of 10–11-year-olds (primary school), including what the puzzles store. Check that the verwerkersovereenkomst, the DPIA and the privacy page (`SchoolPrivacy.razor`) cover primary-school pupils. Then decide whether `ParentalInfoTexts.CurrentVersion` needs another bump.
- **N2 School communication.** Tell the existing schools that unstarted codes will get the VO set (100 questions, two lesson parts) once 04 is live.
- **N3 Content review.**
  - the VO strings (checked against the CSV)
  - the VO cheer lines (04)
  - the 10 mbo routes (06)
  - the puzzle strength sentences (07)
  - whether the 60 items fit groep 7/8 as they are (K8)

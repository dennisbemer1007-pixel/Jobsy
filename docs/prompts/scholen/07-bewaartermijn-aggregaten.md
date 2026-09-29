# 07. Retention job + anonymous aggregates + admin reporting

Read `00-README.md` first. Branch `cursor/scholen-7` from `cursor/scholen-6` (or `-6b`).

> **Rules (same as README §0, repeated on purpose):**
> - Never merge, never deploy, never use rule `123` (`.cursor/rules/shortcut-123.mdc`).
> - Never push to `main` or `acceptatie`; push only this file's `cursor/scholen-*` branch; no force-push.
> - ONE stacked PR into `acceptatie`.
> - Red tests or an unmet success criterion: push, open the PR as **draft**, stop and report. Don't start the next file.
> - No pupil names anywhere, no AI / partner links / vacancies in anything pupil-related, and server-side authorization per §R.

| | |
|---|---|
| Branch | `cursor/scholen-7` |
| PR title | `feat(scholen): school-year retention job + k≥5 aggregates + admin Scholen-rapportage` |
| PR body starts with | `Stacked on #<PR 06> (cursor/scholen-6)` |
| Mockups | none (admin enterprise patterns; reuse the Ent* primitives) |
| Split seam | **07a** = aggregate snapshotter + retention job + early-deletion paths (07.2–07.4). **07b** = admin reporting pages + CSV export (07.5) |

## Goal
Individual pupil data never lives longer than one school year (D8). On the cutoff date (default 31 July) the job first snapshots anonymous totals for every class with ≥ 5 completed pupils into separate aggregate tables, then deletes the classes, codes, progress and results of that school year. Lobsy admin can report on the aggregates per school, school year, level and platform-wide, without any link to codes or pupils.

## 07.1 Today (verify first)
- `Jobsy.Infrastructure/Jobs/DataRetentionHostedService.cs` (BackgroundService, 2 min initial delay, every 24 h, `PrivacyConstants`), `PrivacyDataService`.
- 01's aggregate tables (`SchoolClassAggregate`, `SchoolYearAggregate`, `SchoolRetentionRun`) and settings `SchoolRetentionCutoffMonth/Day`; `SchoolYear` helper.
- 02's `ClassResultsAggregator` (k-anonymity rules).

## 07.2 Aggregate snapshotter
- `SchoolAggregateSnapshotter` (Infrastructure), pure mapping via `ClassResultsAggregator`:
  - per class with `CompletedCount ≥ 5` → `SchoolClassAggregate` (class label snapshot, level, year, counts, RIASEC top-3 counts, top value counts, top culture counts, competence band counts, dream job counts with < 2 → "overig")
  - per school + school year → `SchoolYearAggregate` (sums of the class aggregates **plus** the counts of classes < 5 merged into the school total only when the school total ≥ 5)
  - one platform row (`SchoolId = null`)
- Idempotent per (school, school year): re-running replaces that year's rows. No FK to classes/codes; no code ids, no timestamps finer than the date.
- Also callable on demand by admin ("Totalen nu bijwerken") and automatically when a class window closes (so reporting doesn't wait for July). Deletion still only happens at the cutoff.

## 07.3 Retention job
- `SchoolRetentionHostedService` (own BackgroundService, pattern `DataRetentionHostedService`; runs daily at ~03:00 Europe/Amsterdam; **runs regardless of `SchoolsEnabled`**).
  - For every `SchoolClass` whose school year ended (`SchoolYear.EndsOn(SchoolYearStart, cutoff) < today`):
    1. snapshot (07.2)
    2. in one transaction delete `PupilResult`, `PupilProgress`, `PupilCode`, `TeacherClassAssignment`, `SchoolClass`
    3. write `SchoolRetentionRun`
  - Batch by school (≤ 50 classes per transaction).
  - A failure in one school doesn't stop the others; `Outcome` records it and the admin sees it (07.5).
- Teachers whose last class is gone stay as accounts (they get new classes next year). Staff accounts of **deactivated** schools are removed after 90 days (reuse the existing inactive-account retention if it exists; else document as deferred).
- `PersonalDataAccessLog` rows keep their existing retention (they hold ids only).
- 30 days before the cutoff: the Te doen item from 02 appears, plus a banner on `/school` and `/leraar`.
- Settings validation: moving the cutoff **earlier** than today for the running school year shows an impact note in the admin UI ("Dit verwijdert bij de volgende run de gegevens van {n} klassen.") and needs a confirm dialog. The job uses the saved value on its next run.

## 07.4 Early deletion paths (objection / school leaves)
- Already built: **Code verwijderen** (02) and **Klas verwijderen** (02), immediate.
- Add: admin **"School verwijderen"** on `/admin/scholen/{id}` (danger, type the school name). It snapshots aggregates first, then deletes all classes/codes/results, removes staff accounts and deactivates the school. The aggregates stay (anonymous).
- Add: school **"Alle leerlinggegevens van dit schooljaar nu verwijderen"** on `/school/privacy` (SchoolAdmin, danger + typed confirm), same as the retention path for that school.
- Every deletion writes an audit row (admin audit or `PlatformLog`, Dependencies B) with counts only.

## 07.5 Admin reporting `/admin/scholen/rapportage`
- Filters: school year (default latest), school (all/one), niveau, leerjaar.
- KPIs: Scholen actief · Klassen · Leerlingen gestart · Afgerond (%).
- Cards:
  - RIASEC top-3 distribution
  - top drijfveren
  - top sfeer (culture)
  - droombanen top-10 (+ Overig)
  - per level/year table
- Every figure comes from the aggregate tables only (never from live pupil tables) and respects k ≥ 5 at the chosen filter level: a cell whose underlying count is < 5 shows "< 5".
- **Bewaartermijn** tab: last runs (`SchoolRetentionRun`: date, cutoff, deleted counts, outcome) + the next scheduled cutoff + a "Nu uitvoeren (proefdraai)" dry-run button that reports what **would** be deleted without deleting.
- CSV export of the current aggregate view (aggregates only; header documented in the page help). Audited.
- `RequireAdmin`. Private, non-indexable, help docs, ROUTES.md.

## Tests
- Snapshotter: class with 4 completed → no class row, but counted in the school total only if the school total ≥ 5; dream jobs < 2 → overig; idempotent re-run; no code ids in any aggregate column (reflection + JSON scan).
- Retention (fake clock):
  - 31-07-2027 → classes of 2026 deleted on the next run (01-08-2027), classes of 2027 untouched
  - custom cutoff (30-06) works
  - job runs with `SchoolsEnabled = false`
  - failure isolation per school
  - `SchoolRetentionRun` written
  - aggregates survive deletion (FK-free)
- Early deletion paths: counts, audit rows, aggregates kept.
- Reporting: only aggregate tables queried (repository test), "< 5" masking at filter level, dry run deletes nothing, CSV header.
- §R rows: reporting/export admin-only; SchoolAdmin/Teacher/pupil 403/404.

## Success criteria
- With a fake clock past 31 July, all individual pupil data of the ended school year is gone while the aggregates remain and show in `/admin/scholen/rapportage`.
- No report value can be traced to a code or pupil (k ≥ 5 everywhere).
- All guards green; the stack's final report lists every PR.

Done → end of stack. Report file → branch → PR number → status, plus anything deferred (search, Klasrapport PDF, other languages, SignalR live status if skipped).

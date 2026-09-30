# 07. Sollicitaties: status history table + dated timeline, "Wat nu?", kind rejection; Bewaard tab redesign

Read `00-README.md` first (§0, §S, §D, D3, D4, D6, Dependencies A/C). Branch `cursor/kandidaat-banen-7` from `cursor/kandidaat-banen-6`.

> **Rules (same as README §0, repeated on purpose):**
> - Never merge, never deploy, never use rule `123` (`.cursor/rules/shortcut-123.mdc`).
> - Never push to `main` or `acceptatie`; push only this file's `cursor/kandidaat-banen-*` branch; no force-push.
> - ONE stacked PR into `acceptatie`.
> - Red tests or an unmet success criterion: push, open the PR as **draft**, stop and report. Don't start the next file.
> - **No invented history.** Existing applications get no backfilled rows and no guessed dates (D4). Don't change the nav (D6), the apply flow, or employer-side status semantics.

| | |
|---|---|
| Branch | `cursor/kandidaat-banen-7` |
| PR title | `feat(sollicitaties): status history with dates, candidate timeline and "Wat nu?", Bewaard tab with job status` |
| PR body starts with | `Stacked on #<PR 06> (cursor/kandidaat-banen-6)` + the migration name + the Dependencies C case (tabs present/absent) |
| Mockups | `kd-d4-sollicitaties.png`, `kd-m4-sollicitaties.png` (timeline), `kd-d5-bewaard.png` (Sollicitaties › tab Bewaard) |
| Split seam | **07a** = 07.2–07.4 (table, recorder, viewed event, API). **07b** = 07.5–07.7 (Sollicitaties UI, Bewaard UI) |

## Goal
From now on, every application status change is stored with its date, so a candidate sees an honest, dated timeline: Verstuurd · Gezien door werkgever · Gesprek · Uitslag, with one clear "Wat nu?" per status. Older applications show what is actually known. Saved jobs tell the candidate whether they are still open.

## 07.1 Today (verify first)
- `Jobsy.Core/Entities/Application.cs` has `Status`, `CreatedAt` and `RespondedAt`, and no history. `ApplicationStatus` = Pending, Accepted, Rejected, EmployerContacting, Hired, FilledElsewhere, Withdrawn (`Jobsy.Core/Enums/ProductEnums.cs` ~L26).
- **Status writes** (non-seeder):
  - `ApplicationsController`:
    - ~L742, ~L832, ~L894 → Pending (apply / re-apply)
    - ~L1133 → Withdrawn (`POST {id}/withdraw`)
    - ~L1195 `ExecuteUpdateAsync(... SetProperty RespondedAt ...)` + ~L1203/~L1211 → Accepted/Rejected (`POST {id}/react`)
    - ~L1302 → EmployerContacting (`POST {id}/contact`)
    - ~L1381 → Hired and ~L1405 → FilledElsewhere (`POST vacancies/{vacancyId}/fulfill/{applicationId}`)
  - `CandidateActionsController` ~L220 → Withdrawn (withdraw others)
- Candidate list: `GET api/me/applications` (`JobsyApiClient.GetMyApplicationsAsync`) → `Pages/Candidate/Applications.razor` (648 lines). Pill labels are inline (`StatusPillLabel` ~L552, `PillModifier`); `Apps.Wizard.*` steps.
- Saved: `VacancyLike` (`VacancyId`, `UserId`, `CreatedAt`) → `Pages/Candidate/Liked.razor` (167 lines, `/candidate/liked`). Vacancy state: `VacancyStatus` (Draft, Active, Archived, PendingApproval, Fulfilled), `Vacancy.EndDate`, `Vacancy.ClosedAtUtc`.
- Existing Playwright: `CandidateApplicationsPlaywrightTests`.

## 07.2 Table + one recorder (§D)
- Entity `ApplicationStatusHistory` + migration `AddApplicationStatusHistory` (§D fields; the index `(ApplicationId, OccurredAtUtc)`; a filtered unique index on `ApplicationId` where `Kind = EmployerViewed`).
- `Jobsy.Core/Rules/ApplicationStatusTransitions.cs` + `IApplicationStatusRecorder` (Infrastructure). **Every** status write goes through one call:
  - `SetStatus(application, newStatus, actorKind, actorUserId, nowUtc)`
  - it sets `Status` (and `RespondedAt` where the code does today)
  - it adds one `StatusChanged` row (`FromStatus` → `ToStatus`) in the **same** unit of work
  - apply adds a `Created` row
- The `ExecuteUpdateAsync` path in `react` (~L1195) inserts its history row explicitly **in the same transaction** as the update. It only inserts when the update affected a row; the `InMemory` fallback path does the same.
- No row when the status doesn't actually change (idempotent re-posts).
- **Guard test:** no assignment to `Application.Status` and no `SetProperty(a => a.Status` outside `ApplicationStatusTransitions` and the seeders. It scans `Jobsy.Api` / `Jobsy.Infrastructure` sources with an allow-list of seeder files.
- **Seeders:** keep today's seeded statuses. For the seed candidates' demo applications (`ApplicationsAndWagesSeeder` `UpsertStatus(..., now.AddDays(-n))` and friends), the seeder idempotently adds history rows with the seed dates it already uses. This way the demo and the E2E (09) show a full timeline. Identify the rows the way the seeder already upserts them; **never** add rows for real applications.

## 07.3 "Gezien door werkgever" (D4 default)
- Record `EmployerViewed` once per application, the first time an employer user (not admin support access, not the candidate) does one of these:
  - opens the applicant in the employer UI
  - downloads the Lobsy-CV (`GET {id}/lobsy-cv.pdf`) or the uploaded CV (`GET {id}/uploaded-cv`)
  - reacts or contacts
- If the employer applicant view has no single-applicant request, add `POST api/applications/{id}/viewed` (employer policy, same company scope checks as `react`, idempotent, 204). The employer page calls it when an applicant is opened. The list endpoint (`GET api/applications`) does **not** count as viewed.
- Unique-index violations on concurrent views are caught and ignored.
- Admin support access (`_supportAccess`) never records a view.

## 07.4 Candidate API: timeline
- `ApplicationTimelineBuilder` (Core, pure): input = application (`CreatedAt`, `Status`, `RespondedAt`) + history rows. Output: 4 steps (Verstuurd · Gezien door werkgever · Gesprek · Uitslag), each with state (done / current / upcoming / skipped), a date (or null) and a label key (`KbLabels`).

  | Case | Timeline |
  |---|---|
  | has history | Verstuurd = `Created` row date (else `CreatedAt`) · Gezien = the first `EmployerViewed` or the first change to Accepted · Gesprek = the change to EmployerContacting · Uitslag = the change to Hired / Rejected / FilledElsewhere |
  | Withdrawn | the steps up to the withdrawal + a closing line "Ingetrokken op {datum}" |
  | **no history (older application, D4)** | only "Verstuurd {CreatedAt}" + the **current status** as the current step, dated with `RespondedAt` when set, otherwise undated. No other steps are marked done. The sub-line says "Eerdere stappen zijn niet bewaard" |

- Extend `GET api/me/applications` items with `Timeline` (steps) and `NextStepKey` ("Wat nu?", 07.5). **Candidate-own only**: `ActorUserId` is never in the payload.
- Hidden-mode intermediary vacancies (D3): the item uses the 05 identity. The opdrachtgever reveal happens only where intermediair D4 allows it (Dependencies A present). Without intermediair 03, there is no reveal.
- Unit tests: every status × history/no-history, and Withdrawn.

## 07.5 Sollicitaties UI (`kd-d4`, `kd-m4`)
- **Tab bar:**
  - Dependencies C present: `CandidateJobListTabs` "Sollicitaties · Bewaard" on top (paspoort D8; the Sollicitaties nav item stays active).
  - Absent: no tabs.
- **Counters:** "Alles · Loopt nog · Afgerond" as filter chips (Loopt nog = Pending/Accepted/EmployerContacting; Afgerond = the rest).
- **Active application card (desktop left column):**
  - photo, title, company line, status pill (`KbLabels`)
  - the **dated timeline** (horizontal on desktop, vertical on mobile, `kd-m4`)
  - a "Wat nu?" box with one line + at most one action:

    | Status | "Wat nu?" |
    |---|---|
    | Pending | "Je hoort het hier zodra de werkgever reageert." |
    | Accepted | "De werkgever heeft je gegevens. Houd je telefoon en mail in de gaten." |
    | EmployerContacting | "Oefen je gesprek met Lobsy" → the existing mock interview (`MockInterview.*`) |
    | Hired | "Gefeliciteerd! Spreek je startdatum af met de werkgever." |
    | Rejected / FilledElsewhere | the kind rejection below |
    | Withdrawn | none |

    Strings go in `Kb.Next.*`; don't promise response times.
- **Other applications:** compact rows with a 4-segment progress bar + "Gezien op {datum} · tik voor alle stappen" (opens the vertical timeline).
- **Kind rejection** (Rejected/FilledElsewhere): "Niet gekozen" (never "Afgewezen" to the candidate) + "Deze banen lijken erop": up to 3 similar open vacancies (same category/work type, within the candidate's travel, fit gate respected), from the existing discovery query, with no new ranking logic.
- **Right rail (desktop ≥ 1024):** a short "Wat betekenen de stappen?" explanation (`Kb.Steps.*`). An optional Lobsy tip only from existing copy (§0 mockup differences).
- Move `StatusPillLabel` / `PillModifier` into `KbLabels` (no enum names rendered). Keep withdraw and Lobsy-CV download working as today.
- Interview times/places are **not** shown (not stored; §0 mockup differences).

## 07.6 Bewaard UI (`kd-d5`)
- **Location:**
  - Dependencies C present: inside the tab bar (tab "Bewaard" active) at `/candidate/liked`, with the page `h1` "Mijn sollicitaties" and the tab label "Bewaard" (as in `kd-d5`).
  - Absent: `/candidate/liked` redesigned in place with `h1` "Bewaard".
- **Filters:** Alles · Nog open · Gesloten (chips with counts) + sort (Past het best / Laatst bewaard / Sluit het eerst).
- **Card grid** (3 columns ≥ 1024, 2 ≥ 640, 1 on mobile). Each card has:
  - photo, a **state pill** (`KbSavedJobState`, pure, from the vacancy):

    | State | When |
    |---|---|
    | "Open" | `Active` and `EndDate` > today + 7 |
    | "Sluit over {n} dagen" | `Active` and `EndDate` within 7 days; n in Europe/Amsterdam days |
    | "Baan is al vergeven" | `Fulfilled` |
    | "Gesloten" | `Archived`, `ClosedAtUtc` set, or `EndDate` past |
    | not shown | Draft/PendingApproval (shouldn't be liked) |

  - an unsave heart (with a confirm-free undo toast)
  - title, company line, travel, hours, "Bewaard {datum}" (`VacancyLike.CreatedAt`)
  - fit pill + why line (gate respected)
  - one action:
    - "Solliciteer" (open, not applied)
    - "Je hebt gesolliciteerd · Bekijk" (applied → Sollicitaties)
    - for closed/vergeven: "Zoek banen die hierop lijken" (discovery pre-filtered on the category) + "Weg" (unsave)

    Closed cards are greyed (not only by colour: the pill says so).
- `/candidate/shared` gets the same tab bar when present, but keeps its content, **without** fit / "Staat lager" (privacy §0).

## 07.7 Tests
- migration + `EfModelSnapshotTests`, `PendingModelChangesTests`, `EfMigrationDiscoveryTests`
- recorder: each controller path writes exactly one row with the right from/to/actor (API tests); the ExecuteUpdate path in one transaction; the guard test (07.2)
- viewed: the first view records once, the list endpoint doesn't, support access doesn't, concurrent views → one row
- `ApplicationTimelineBuilder` matrix (07.4), `KbSavedJobState` boundaries (7/8 days, Amsterdam midnight)
- bUnit: the timeline (with/without history), "Wat nu?" per status, the kind rejection shows "Niet gekozen", the Bewaard states and actions, no enum names, `/candidate/shared` has no fit
- Playwright: extend `CandidateApplicationsPlaywrightTests` (timeline visible, tab bar when present at 1440/390)

## Success criteria
- Every status change after this PR has a dated history row; older applications show only "Verstuurd" + the current status.
- The candidate sees the dated timeline and one "Wat nu?"; rejections read "Niet gekozen" with similar jobs.
- Bewaard lives in Sollicitaties as a tab (when paspoort D8 is present), shows each saved job's state, and offers the right single action.

Done → next: `08-match.md`.

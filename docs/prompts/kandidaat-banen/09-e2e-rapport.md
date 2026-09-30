# 09. Playwright E2E for the candidate job flows + docs + stack-end report

Read `00-README.md` first (§0, §S, §F, all Decisions, Dependencies). Branch `cursor/kandidaat-banen-9` from `cursor/kandidaat-banen-8`.

> **Rules (same as README §0, repeated on purpose):**
> - Never merge, never deploy, never use rule `123` (`.cursor/rules/shortcut-123.mdc`).
> - Never push to `main` or `acceptatie`; push only this file's `cursor/kandidaat-banen-*` branch; no force-push.
> - ONE stacked PR into `acceptatie`.
> - Red tests or an unmet success criterion: push, open the PR as **draft**, stop and report.
> - Tests + docs only. A failing scenario that shows a product bug from 01–08 is fixed in **this** PR only when the fix is small and inside that file's scope; otherwise mark it `Skip` with a reason, list it in the report and open the PR as draft. Never toggle the Werkgevers switch on a shared environment.

| | |
|---|---|
| Branch | `cursor/kandidaat-banen-9` |
| PR title | `test(kandidaat): E2E for banenkaart, lijst, vacature, sollicitaties, bewaard and Match + stack report` |
| PR body starts with | `Stacked on #<PR 08> (cursor/kandidaat-banen-8)` + the stack-end report (09.4) |
| Mockups | all `kd-*` (visual reference for the screenshot step) |
| Split seam | none |

## Goal
One Playwright suite proves the candidate job flows end to end on desktop and mobile, for a candidate with and without a complete paspoort, anonymous visitors, hidden-mode vacancies and the Werkgevers switch. A short report tells Dennis what landed, which dependency cases applied and what was deferred.

## 09.1 Conventions (verify first)
- The existing suites live in `Jobsy.Tests/*PlaywrightTests.cs`:
  - `[Collection("PlaywrightSmoke")]`
  - they soft-skip without `JOBSY_E2E_BASE_URL`
  - `Microsoft.Playwright.Program.Main(["install", "chromium"])`
  - login via `TryLoginAsync` with `JOBSY_E2E_CANDIDATE_EMAIL` / `_PASSWORD` (default `kandidaat@jobsy.local`)
- Reuse that pattern. Extract a small shared helper `Jobsy.Tests/E2e/KbE2e.cs` for the following (don't rewrite the existing suites):
  - login as a named seed user
  - viewports (1440×900 desktop, 390×844 mobile with touch)
  - `pageerror`/console capture
  - the circuit-error check (`Circuit.ErrorTitle` visible = fail)
  - the route constants (`E2eRoutes.Banenkaart` when landing 04 is present, else `KbRoutes.Map`)
- Seed users:
  - `kandidaat@jobsy.local` (complete paspoort; culture + values done)
  - `valentine@jobsy.local` (0/3 tests; the gate stays closed)
  - anonymous
  - Override with `JOBSY_E2E_CANDIDATE_EMAIL` / `JOBSY_E2E_INCOMPLETE_EMAIL` (+ passwords).
- Werkgevers OFF scenarios run **only** when `JOBSY_E2E_ALLOW_FEATURE_TOGGLE=1` **and** the base URL host is `localhost`/`127.0.0.1`. Otherwise they skip with a reason. They switch back to ON in `finally`.

## 09.2 Scenarios (`Jobsy.Tests/KandidaatBanenE2ePlaywrightTests.cs`)
Every scenario asserts: no `pageerror`, no circuit error, no console error containing "Maximum call stack", no 404 for `/fonts/` or `/api/travel/isochrones` (Fiets).

| # | Who / viewport | Flow | Asserts |
|---|---|---|---|
| S1 | kandidaat, 1440 | open the banenkaart | address field = profile address; chip "20 min fietsen"; no filter badge; `data-iso-mode="real"`; side list cards with "% past bij jou" + a why line; top-match tile visible; wait 10 s, no crash (hotfix 01) |
| S2 | kandidaat, 390 | open the banenkaart | bottom sheet peek; chips row; no blank strip (map bottom = nav top ±1 px); tap a pin → docked card |
| S3 | anonymous, 390 + 1440 | open the banenkaart | location prompt visible; no fit anywhere; type "Herenstraat 20 Wateringen" (25 ms delay) → the value is exact and the first suggestion is an address; pick it → rings drawn |
| S4 | valentine, 1440 | banenkaart + detail + Match | "Maak je paspoort af" on cards and detail; no `\d+%` in fit areas or in the `api` payload (intercept the response); Match shows the unlock panel |
| S5 | kandidaat, 1440 | switch to **Lijst** | full width, no map canvas; `?weergave=lijst` survives a reload; sort "Past het best" first; a dislike vacancy (when Dependencies D present) shows "Staat lager: …" and is still listed |
| S6 | kandidaat, 1440 + 390 | open a vacancy detail | fit panel with 4 bars; transport switch changes the minutes; OV shows "ongeveer"; one primary button; the mobile sticky apply bar |
| S7 | kandidaat, 390 | open a **hidden-mode** intermediary vacancy (seed; skip with a reason if none exists) | "via uitzendbureau"; no Route/Street View; the intercepted detail payload contains no client name / workplace coordinates |
| S8 | kandidaat, 1440 + 390 | Sollicitaties | tab bar "Sollicitaties · Bewaard" (when Dependencies C present); the active application shows a dated timeline + "Wat nu?"; a rejected one reads "Niet gekozen" with similar jobs |
| S9 | kandidaat, 1440 | tab Bewaard | state pills (Open / Sluit over … / Gesloten / Baan is al vergeven where seeded); unsave → undo toast; the Sollicitaties nav item is active on `/candidate/liked` |
| S10 | kandidaat, 1440 | Match dialog from the top-match tile | "Waarom jij past" rows; ← moves the job to the end of "Hierna"; Esc returns focus to the tile |
| S11 | kandidaat, 390 | `/candidate/match` | swipe left/right smoke; the actions don't overlap the bottom nav |
| S12 | local only, Werkgevers OFF | banenkaart, applications, liked, match, detail + their APIs | gated page / 404 `feature_disabled` (skip when Dependencies C absent or not allowed, 09.1) |
| S13 | kandidaat, 1440 | two enhanced navigations (map → detail → back → Sollicitaties) | no "Maximum call stack size exceeded"; `h1` focused without a visible box |

- Each scenario saves a screenshot to `artifacts/e2e/kandidaat-banen/S{n}-{viewport}.png` (not committed; attach the key ones to the PR).

## 09.3 Docs
- `docs/features/kandidaat-banen.md`: final dependency table, the fit rules (§F in short), the timeline rules, the hidden-mode rule, and how to run the E2E (env vars).
- `docs/ROUTES.md`: `?weergave=lijst`, `POST api/applications/{id}/viewed` (if added in 07), any gated endpoints. `RoutesDocFreshnessTests` green.
- `CHANGELOG.md`: one entry per file 01–09 (candidate-facing wording).
- `PageHelpDocs`: help text for the banenkaart (start at home, rings, "Staat lager"), Sollicitaties (timeline) and Bewaard.

## 09.4 Stack-end report (PR body + final chat message)
A table: file → branch → PR number → status (green / draft-red) → notes. Then:
- the **dependency cases** A–G as applied (present/absent, and which fallbacks exist, with `git grep -n "KB-FALLBACK"` output)
- the fit **distribution** before/after (from 04.4) and whether werkgever-aanmelding 01 was present
- the crash root cause from 01 in one line
- the **nav check** (Dependencies F): the actual candidate nav order found vs Dennis's order; this stack changed nothing
- the E2E results per scenario (pass / skip + reason / fail)
- "Out of scope / deferred" (e.g. interview times, a server-side persisted skip, the reveal when intermediair 03 is absent)

## Tests
- `KandidaatBanenE2ePlaywrightTests` (09.2) + the existing Playwright suites green or soft-skipped
- `dotnet build`, `dotnet test` green

## Success criteria
- All 13 scenarios pass against a local run (S7/S12 may skip with a stated reason); no page errors, no circuit errors.
- The docs are updated and the report is complete.

Done → stack complete. Report to Dennis with the 09.4 table.

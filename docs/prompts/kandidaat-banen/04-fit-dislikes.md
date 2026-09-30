# 04. Fit %: gate on culture/values, candidate calibration 55–90, strong ≥ 75, why line, dislikes rank lower

Read `00-README.md` first (§0, §F, D2, D8, Dependencies B/D). Branch `cursor/kandidaat-banen-4` from `cursor/kandidaat-banen-3`.

> **Rules (same as README §0, repeated on purpose):**
> - Never merge, never deploy, never use rule `123` (`.cursor/rules/shortcut-123.mdc`).
> - Never push to `main` or `acceptatie`; push only this file's `cursor/kandidaat-banen-*` branch; no force-push.
> - ONE stacked PR into `acceptatie`.
> - Red tests or an unmet success criterion: push, open the PR as **draft**, stop and report. Don't start the next file.
> - **Employer-side scores must not change**: `ProfileVacancyMatchCalculator` weights, `Application.MatchPercent`, `CandidateMatchSnapshotService` and its version, `MatchScoreWeights.StrongMatchThreshold`. A test proves it (04.6).

| | |
|---|---|
| Branch | `cursor/kandidaat-banen-4` |
| PR title | `feat(match): honest candidate fit — only with culture/values test, calibrated 55–90 %, strong from 75 %, why line, dislikes rank lower` |
| PR body starts with | `Stacked on #<PR 03> (cursor/kandidaat-banen-3)` + the **distribution table** (before/after, 04.4) + Dependencies B (wa-01) / D case |
| Mockups | `kd-d1` side list ("82% past bij jou" + why line), `kd-d2` (the dislike card "Staat lager: nachtdienst"), `kd-d3` fit panel (4 DNA bars) |
| Split seam | **04a** = 04.2–04.5 (gate, calibration, why, wiring). **04b** = 04.6–04.7 (dislikes + detail DNA bars) |

## Goal
A candidate only sees a percentage when it means something (culture or values test done). Percentages are spread out, so 90 % is rare and special. Each card says **why** in one line, and a job the candidate said they'd rather not do ranks lower with a visible reason, but is never hidden.

## 04.1 Today (verify first)
- **Candidate scoring:**
  - `Jobsy.Core/Rules/ProfileVacancyMatchCalculator.cs` blends travel/hours/dayparts with experience, competencies, interests, culture (`CultureFitBuilder.TotalScoreWeight`) and Schwartz values; `DisplayThreshold = 60`.
  - `ProfileVacancyMatchService` (`Jobsy.Infrastructure/Services`) builds the context: `CultureScores` / `ValuesScores` are non-null **only when complete** (~L141–176).
- **Candidate DTO seam:** `VacanciesController.WithCandidateMatch(dto, match, cultureStatus)` (~L2530–2545) sets `MatchPercent`, `MatchColorBand`, `MatchWhySummary`, `MatchWhy`, `MatchGaps`, `IsBroadMatch`, and culture fields (`CultureFitLabel`, `CultureFitWhy`). Also check the list/deck endpoints (`minMatchPercent` ~L127/L403) and `SwipeViewModel.FromVacancy` (`MatchPercentage`, `ShowMatchPercentage`).
- **Employer side:** `ApplicationsController` ~L793 `application.MatchPercent = match.TotalPercent`, plus `CandidateMatchSnapshotService`. They must stay the same.
- **Symptoms** (review, `kandidaat@jobsy.local` and valentine): scores cluster at **91–95 %**. Valentine (0/3 tests) sees "95% Match". The detail page shows 4 DNA bars (Cultuur/Waarden/Competenties/Interesses).
- **Strong threshold:** `MatchScoreCalculator` `StrongMatchThreshold = 70` (employer + candidate share it today).

## 04.2 Gate (D2)
- `CandidateFitGate` (Core): `IsOpen = hasCompleteCulture || hasCompleteValues`, from the same context values (`CultureScores is not null || ValuesScores is not null`). Provisional answers don't open the gate.
- Closed gate, on every candidate surface (card, side list, docked popup, list, detail, top-match tile, Match, Bewaard):
  - `KbFitPill` shows "Maak je paspoort af"
  - no number, no band, no "Sterke match", no "Beste match" sort option (the sort falls back to "Dichtbij")
  - the `minMatchPercent` filter chip is hidden
- The API sends `MatchPercent = null` + `FitGate = "closed"` for candidate DTOs when closed, so the number isn't even in the payload.
- The Match page keeps its own existing profile gate (`MatchProfileGateViewModel`) and adds this gate: with neither test done, Match shows the unlock panel pointing to the culture/values test.

## 04.3 Calibration (display layer, D2)
- `Jobsy.Core/Rules/CandidateFitDisplay.cs` (pure, no I/O):
  - **Composition** (candidate view only), using the component scores the calculator already computes (`ProfileVacancyMatch`: core travel/hours/dayparts, `ExperienceScore01`, `CompetencyScore01`, `InterestScore01`, culture fit, values fit):
    - DNA part = the weighted mean over the **available** dimensions: Cultuur 0.30, Waarden 0.25, Competenties 0.25, Interesses 0.20, renormalised over the ones present
    - practical part = travel/hours/dayparts ratio
    - `s = 0.70 × DNA + 0.30 × practical`
  - **Mapping:** a piecewise-linear map from `s` to `Percent`, with anchors stored as one constant table `CandidateFitDisplay.Anchors`. The output is clamped to **55–90**.
  - **Bands:** `Strong` ≥ 75, `Good` 65–74, `Some` < 65 (`CandidateFitDisplay.StrongThreshold = 75`, separate from the employer's 70).
- **Deriving the anchors (04.4)** is part of this file. The weights above are the starting point; you may adjust them if the distribution needs it, and say so in the PR.
- The shown `Percent` is the **only** candidate number: the card, detail, Match, sort, the `minMatchPercent` filter and the top-match choice all use it. `TopMatchTileVisibility` uses the candidate band (≥ 75) instead of the employer threshold.

## 04.4 Distribution report
- A test-only harness `CandidateFitDistributionReportTests` loads the seeded candidates (`kandidaat@jobsy.local`, valentine and the other seed candidates with tests) × all seeded vacancies through the real `ProfileVacancyMatchService` + `CandidateFitDisplay`. It prints p10/p50/p90/min/max and the share ≥ 75 per candidate.
- **Targets (assertions)** for candidates with the gate open:
  - min ≥ 55, max ≤ 90, p90 − p10 ≥ 20 points
  - the share ≥ 75 is between 10 % and 40 %
  - for `kandidaat@jobsy.local`, the top 5 by fit are **not** simply the 5 nearest vacancies (a sanity check that DNA drives the order, not travel)
- Put the before/after table (raw vs calibrated) in the PR body. If werkgever-aanmelding 01 (`ICompanyCultureLookup`) is absent, say the anchors must be re-run when it lands; the harness makes that a one-command job.

## 04.5 Why line + wiring
- `WhyLine`: the first `MatchWhy` point mapped to a short candidate sentence (`Kb.Why.{kind}`: culture, values, competency, interest, travel, hours), with at most 2 fragments joined by " · " (e.g. "Je helpt graag mensen · rustig team"). Use the existing `ProfileVacancyMatchCalculator.WhyHeadline` / `CultureFitWhy` data; don't invent reasons. Omit the line when there is no point.
- `WithCandidateMatch` fills new DTO fields: `FitGate`, `FitPercent`, `FitBand`, `FitWhyLine`, `FitDimensions` (4 × int?). Keep the old fields for the employer DTOs; candidate DTOs set `MatchPercent = FitPercent` so old clients stay consistent.
- `SwipeViewModel.FromVacancy`, the side list, docked popup, list, detail and the top-match tile render `KbFitPill` + `KbWhyLine` from these fields.

## 04.6 Employer scores unchanged (test)
- A test that takes a fixed candidate + vacancy set and asserts that `ProfileVacancyMatchCalculator.Calculate(...).TotalPercent`, `Application.MatchPercent` at apply and the `CandidateMatchSnapshotService` output are **byte-identical** to the values before this PR (golden values captured in the test before the change).
- The snapshot version constant is unchanged (assert).

## 04.7 Dislikes (D8, Dependencies D)
- **Present:**
  - `IKbDislikeSource` reads `CandidatePrivatePreferences.DislikesJson` for the signed-in candidate, and `DislikeMatchRules` decides which vacancies match (e.g. `night-shifts` ↔ `Vacancy.LegalNightShift23To06`).
  - The candidate list sort applies `KbRanking.DislikePenalty` (15) to the sort key only.
  - The DTO gets `RankLowerReason` (a short label key, e.g. `Kb.Dislike.night-shifts` → "nachtdienst").
  - The card shows "Staat lager: nachtdienst" as its secondary chip (D7 priority), and the detail shows it under the fit panel.
  - **Never** filtered, never hidden on the map, the shown percentage unchanged.
- **Absent:** `IKbDislikeSource` returns none (`// KB-FALLBACK(D)`); no chip, no penalty.
- **Privacy:** `RankLowerReason` is only set on candidate-own DTOs for the signed-in candidate. It is absent from anonymous, shared, employer, SEO/OG and assistant payloads. It is not logged.
- Tests:
  - unit: the penalty ordering (a 85 % night-shift job sorts below a 75 % one, above a 69 % one)
  - the "never hidden" count test (same count with and without dislikes)
  - a privacy test on the shared/public DTOs

## 04.8 Detail DNA bars
- The detail fit panel (`kd-d3`) shows the 4 bars from `FitDimensions` (null → "Nog niet gedaan" with a link to that test), the percentage + band, the why line and the existing `MatchGaps` as "Nieuw voor jou". Closed gate → the panel shows the gate text and the two test links only.

## Tests
- unit: gate, calibration monotonicity (higher `s` → higher or equal percent), clamps 55/90, band boundaries 74/75, why line mapping
- `CandidateFitDistributionReportTests` (04.4), the employer-unchanged golden test (04.6), dislike ordering/never-hidden/privacy (04.7)
- bUnit: card/list/detail/Match with gate closed (no digits followed by "%" in the fit area) and open
- `dotnet build`, `dotnet test` green

## Success criteria
- valentine (no culture/values test) sees "Maak je paspoort af" everywhere and no percentage in any candidate payload.
- `kandidaat@jobsy.local` sees percentages spread between 55 and 90; "Sterke match" only from 75; one why line per card.
- A night-shift job ranks lower with "Staat lager: nachtdienst" (Dependencies D present) and is still on the map and in the list.
- Employer-side numbers are identical to before (golden test).

Done → next: `05-werkgever-uitzendbureau.md`.

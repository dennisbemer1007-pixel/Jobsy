# 08. Match: desktop dialog + mobile swipe refresh, "Waarom jij past" per DNA dimension, calibrated fit, "Laten schieten" never hides

Read `00-README.md` first (§0, §F, D2, D3, D12). Branch `cursor/kandidaat-banen-8` from `cursor/kandidaat-banen-7`.

> **Rules (same as README §0, repeated on purpose):**
> - Never merge, never deploy, never use rule `123` (`.cursor/rules/shortcut-123.mdc`).
> - Never push to `main` or `acceptatie`; push only this file's `cursor/kandidaat-banen-*` branch; no force-push.
> - ONE stacked PR into `acceptatie`.
> - Red tests or an unmet success criterion: push, open the PR as **draft**, stop and report. Don't start the next file.
> - Keep the Match mechanics (swipe gestures, keyboard, `MatchDeck` position/restore, `LikeAsync`). Only the presentation, the fit source and the "Laten schieten" behaviour (D12) change.

| | |
|---|---|
| Branch | `cursor/kandidaat-banen-8` |
| PR title | `feat(match): refreshed Match with calibrated fit, "Waarom jij past" per DNA dimension, skip moves a job to the end` |
| PR body starts with | `Stacked on #<PR 07> (cursor/kandidaat-banen-7)` |
| Mockups | `kd-d6-match.png` (desktop dialog over the map, variant B from match-desktop), `kd-m5-match.png` (mobile swipe card) |
| Split seam | none expected (presentation). If > 1.500 lines: **08a** desktop dialog, **08b** mobile page |

## Goal
Match feels like a short, kind conversation about a few good jobs: each card says **why** this job fits the candidate per DNA dimension, uses the same honest percentage as the rest of the app, and skipping a job never makes it disappear.

## 08.1 Today (verify first)
- Desktop:
  - `Components/Match/MatchDeckDialog.razor` (246 lines): progress bar, `SwipeCard` stack, keyboard hints, the "Match.UpNext" side column (`Deck.UpNext(3)`)
  - opened from the top-match tile (`TopMatchTile.razor`, fixed in 01)
- Mobile: `Pages/Candidate/MatchPage.razor` (`/candidate/match`, 234 lines) with `SwipeCard.razor` (432 lines; "Match.WhyYouFit" single line ~L111).
- `Services/MatchDeck.cs`: `LoadAsync`, `Advance`, `ResumeAt`, `UpNext`, `LikeAsync`. `HandleReject` only shows a toast ("Match.ToastSkipped"); nothing is stored (D12).
- The deck items are `SwipeViewModel.FromVacancy(...)` (`MatchPercentage`, `ShowMatchPercentage`), built from the candidate DTO fields that 04 filled.
- CSS: `features/match-desktop.css`.

## 08.2 Card content (both)
- **Top:** photo, title, company line (hidden mode → bureau, 05), `KbTravelTime`, `KbFitPill` (calibrated, gate from 04).
- **"Waarom jij past"** block: up to 4 rows, one per DNA dimension that has data (Cultuur / Waarden / Competenties / Interesses). Each row has a small bar (0–100 from `FitDimensions`) + one short reason from the existing why points for that dimension (`Kb.Why.*`). Dimensions without data show "Nog niet gedaan" + a link, **only** on the desktop dialog (the mobile card stays compact: max 3 rows, no links).
- **"Staat lager: …"** (04) as a small chip when it applies. Match still shows the job; the deck order already includes the penalty (04).
- **Key facts:** hours, daypart, wage (existing).
- **Actions** (unchanged handlers): "Laten schieten" (←), "Meer info", "Snel kennismaken" (→, primary).

## 08.3 Desktop dialog (`kd-d6`)
- Variant B layout: the dialog over the dimmed map, with:
  - a header "Jouw top-matches" + "1 van 8 · je kunt niets fout doen" (progress text + bar)
  - the card stack centre
  - the **"Hierna"** column (next 3, title + fit pill + travel; click = `ResumeAt`)
  - keyboard hints (← → Enter Esc)
- Focus is trapped in the dialog and returns to the top-match tile on close (keep the existing behaviour, test it).

## 08.4 Mobile (`kd-m5`)
- Header with the mascot (existing asset) + "Jouw top-matches" + the progress text; the swipe card fills the width; the actions sit in a bottom row above the bottom nav (safe-area aware).

## 08.5 "Laten schieten" (D12)
- A skip moves the current card to the **end of this deck session** (`MatchDeck.Defer(vacancyId)`: append to the end if not already deferred once; a second skip in the same session just advances). The toast says "We laten hem later nog eens zien" (`Kb.Match.Skipped`).
- Nothing is stored server-side. The map, list and top-match tile are not affected. Restore after reload keeps today's behaviour (`RestoreIndex`); deferred order may reset, which is fine.
- Unit tests on `MatchDeck`: defer appends once, a second skip advances, `Position`/`Count`/`IsFinished` stay consistent, and `UpNext` shows the deferred card at the end.

## 08.6 Gate + empty states
- Fit gate closed (04.2): Match shows the unlock panel (`MatchUnlockPanel`) with "Maak je paspoort af" and the culture/values test links; no cards with percentages.
- Deck finished: "Je hebt ze allemaal gezien" + two actions: "Bekijk bewaarde banen" (→ `KbRoutes.Saved`) and "Terug naar de kaart" (→ `KbRoutes.Map`).
- Werkgevers OFF: Match is gated (02.9).

## Tests
- bUnit:
  - the card renders 4 / 3 / 0 DNA rows depending on the data
  - gate closed → unlock panel, no "%"
  - hidden-mode card shows the bureau
  - "Staat lager" chip
  - the dialog focus trap + return focus
  - no enum names
- `MatchDeck` unit tests (08.5), existing `MatchDesktopBunitTests` green (update the string list it checks if keys were added)
- Playwright: extend the desktop top-match test from 01 (open the dialog, press ←, the same job appears again at the end of "Hierna"); mobile 390 swipe smoke
- `dotnet build`, `dotnet test` green

## Success criteria
- Match shows the same calibrated percentage and gate as the rest of the app, with "Waarom jij past" per DNA dimension.
- Skipping never hides a job; it comes back at the end of the deck.
- Desktop dialog and mobile page match `kd-d6` / `kd-m5` within the §0 differences.

Done → next: `09-e2e-rapport.md`.

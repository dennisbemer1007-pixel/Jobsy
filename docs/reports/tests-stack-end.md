# Tests stack end report

Stack: candidate tests redesign (`docs/prompts/tests/01`–`07`). Base for 02+: `origin/cursor/tests-hotfix` (PR #461 draft). No merges, no deploys, no push to `main`/`acceptatie`.

## File → branch → PR / body → status

| File | Branch | Tip SHA | PR / body | Status |
|---|---|---|---|---|
| 01 hotfix | `cursor/tests-hotfix` | (reuse #461) | **PR #461** draft | Done — do not redo |
| 02 vraagflow-diepte | `cursor/tests-2` | `a036cfe1` | `/tmp/tests-02-pr-body.md` (`gh` denied) | Done |
| 03 aanpassingen-limiet | `cursor/tests-3` | `58491286` | `/tmp/tests-03-pr-body.md` | Done |
| 04 testpaginas | `cursor/tests-4` | `8dfe10a5` | `/tmp/tests-04-pr-body.md` | Done; A–E PRESENT |
| 05 uitgebreide test | `cursor/tests-5` | `4514acb7` | `/tmp/tests-05-pr-body.md` | Done; A–E PRESENT |
| 06 talen-rtl | `cursor/tests-6` | `13660b03` | `/tmp/tests-06-pr-body.md` | Done |
| 07 e2e-rapport | `cursor/tests-7` | *(this tip)* | `/tmp/tests-07-pr-body.md` | Done (E2E soft-skip without `JOBSY_E2E_BASE_URL`) |

## Dependencies A–E

| Dep | Case | Notes |
|---|---|---|
| A JourneyLobster/Scene | **PRESENT** | Reused; ontdekkingsreis tokens |
| A2 LobsyBubble Scene | **PRESENT** | Variant=Scene |
| B OnboardingMiniTest + DeeperQuestionIds | **PRESENT** | MiniTest → TestQuestionFlow Compact; TestDepthRules |
| C TestsOverviewBuilder / PassportTestsTab | **PRESENT** | TestDepth.* labels |
| D IFeatureFlags / Employers | **PRESENT** | Fit vacancy line gated |
| E CandidateFitDisplay + Gate | **PRESENT** | Bands only when gate open |
| F EmailRenderer/Mailer | **PRESENT** (01) | Receipt uses checkout Locale (06) |
| G Nav | never edited | — |

Recheck at 04 and 05 against `origin/acceptatie`: all still **PRESENT** (no case flip).

## Coverage vs bug / scenario ids (high level)

| Area | Covered by |
|---|---|
| Shared flow + Likert a11y + consent | 02 unit/bUnit |
| 3-change limit / 409 | 03 AssessmentSaveGuard tests |
| Free pages shell | 04 |
| Deep parts / pause / offer / checkout / free-first TestDetail | 05 + DeepTestMotivationTests |
| 5 langs + RTL + deep JSON | 06 TestItemTranslationParityTests |
| Playwright desktop/mobile/RTL/reduced-motion | 07 CandidateTestsPlaywrightTests (soft-skip) |
| Mollie unlock only when paid / stub gate | 01 DeepTestPaymentAndAutosaveTests |

## Deferred

- Global h1 focus rule polish
- OSS btw for EU consumers / automatic refunds
- Report PDF **content** translation (title uses “Uitgebreide test”; body lang param exists)
- Dropping `FlexCommercialSettings.DeepAnalysisPriceEuro` (still read by DTOs/settings UI — keep until admin fully on per-kind prices only)
- Removing long-list `QuestionnaireShell` body (still used outside this stack)
- Full Playwright runs against live Acc (need `JOBSY_E2E_BASE_URL` + stub seed)
- Native psychometric sign-off for pl/ro/ar item CSVs (mechanical B1 draft in place)
- Candidate chrome leftovers for older Kompas copy still mentioning legacy names outside the stack keys guarded in 07

## For Dennis

1. Set the four deep-test prices in Admin (today often still € 2,99 each).
2. Confirm invoice/btw (incl. btw, series `LOB-KT`) with the accountant.
3. Native review: `docs/i18n/tests-items-review.csv` + `docs/i18n/tests-deep-items-review.csv`.
4. Confirm “Werkgevers zien…” sentence on free finish (04).
5. Invoice retention after account deletion (privacy §0).
6. Native pl/ro/ar product voice for deep items (CSV flag).

## Tip SHA of tests-7

See git tip of `cursor/tests-7` after this commit (reported in agent return).

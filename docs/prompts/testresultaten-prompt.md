# Cursor prompt: Testresultaten, the done state of the 4 candidate tests (+ paid report parity)

Branch from `acceptatie`. ONE PR into `acceptatie`. Do not merge, do not deploy, do not use rule 123. Never push to main/acceptatie.

> **Size warning.** This is large. Split it into **two PRs** that each follow the rules above, and open them in this order:
> - **PR A: result page.** Done-state template, locked cards (placeholder only), gold block, "Bekijk voorbeeld-PDF" button (hidden behind a flag until B lands), "Antwoorden wijzigen" / "Test opnieuw doen", and the **3-adjustments limit** (API + UI). All UI strings are in nl/en/pl/ro/ar. See §§2–5, §7.1 and the A-marked tests in §7.4.
> - **PR B: paid report and PDF content.** Everything the locked cards promise, for real, **in Dutch and polished English** (the report language follows the UI language), plus the sample PDF in both languages. See §6, §7.2 and the B-marked tests in §7.4.
>
> What goes where: the adjustment counter and its API enforcement are in **A**. Report and PDF localization (resource catalogs, EN texts, the "no NL leftovers" tests) are in **B**. A's own UI strings are localized in A.
>
> PR A must not show any card whose paid counterpart is not yet delivered (see §4.3, "card gating"). If you do it in one PR, the same rule applies.

## 0. Rules (read first)
- **Branch:** `git fetch origin && git checkout -b cursor/testresultaten origin/acceptatie`. For the split, use `cursor/testresultaten-a` and `cursor/testresultaten-b`, with B branched from A's branch or rebased after A merges.
  - Code references are from `origin/acceptatie` @ `e495b4b9` (2026-09-28 12:52 CEST). Re-check the line numbers.
- Never push to `acceptatie` or `main`. See `docs/release-flow.md`.
- **Reference mockups** are on branch `docs/testresultaten`, folder `docs/mockups/testresultaten/` (PNG + HTML source). Read them with `git show origin/docs/testresultaten:docs/mockups/testresultaten/<file>`.
  - Done-state page, desktop 1280 (full page) and mobile 390 (full page), per test:
    - `tr-competentie-{desktop,mobiel}.png`
    - `tr-beroepen-{desktop,mobiel}.png`
    - `tr-cultuur-{desktop,mobiel}.png`
    - `tr-waarden-{desktop,mobiel}.png`
  - Sample PDF preview (pages 1–2): `tr-pdf-{competentie,beroepen,cultuur,waarden}-desktop.png` and `tr-pdf-competentie-mobiel.png`.
  - Edit mode: `tr-wijzigen-{desktop,mobiel}.png`.
  - Redo confirmation (bottom sheet): `tr-opnieuw-mobiel.png`.
  - Adjustment limit reached (0 of 3 left, both buttons disabled with an explanation): `tr-limiet-{desktop,mobiel}.png`.
  - The quota line "Je kunt deze test nog 2 van de 3 keer aanpassen" is under the action buttons on every `tr-*-{desktop,mobiel}.png` result page, in the edit banner and footer (`tr-wijzigen-*`), and in the redo sheet (`tr-opnieuw-mobiel`).
  - Question screen with the sticky bar fixed: `tr-vraag-{desktop,mobiel}.png`. This is a **reference only**. The bar/scroll fix itself belongs to the separate questionnaire fix PR (see §5.1).
  - The mockup HTML uses inline styles and raw hex values. Do **not** copy that. Rebuild with design-system tokens and classes.
  - All numbers, occupations and texts in the mockups are **example data**. The "Mockup · Voorbeelddata" watermark is mockup-only. The per-card "Voorbeelddata" stamp and the section label **are** part of the design (§4).
- **Design system:** `.cursor/rules/design-system.mdc`.
  - Tokens only. The gold tokens already exist in `app.css`: `--gold`, `--gold-light`, `--gold-deep`, `--gold-ink`, `--gold-soft`.
  - Weights 400/600, 700 only for `h1`.
  - Tap targets ≥ 44 px.
  - Logical properties (RTL for `ar`).
  - Breakpoints 640/900/1024. Mobile first.
  - Reuse `panel-page`, `test-detail__*`, `test-score-bars`, `test-status-badge`, `btn-outline-brand`, `engagement-btn`, `lobsy-dialog` / `LobsyFriendlyDialog`, and `ScoreRadarChart` (`Jobsy.Web/Components/Candidate/ScoreRadarChart.razor`).
  - New BEM block: `test-result-…`. Keep the existing `btn-gold` class but fix it (§3).
- **CSS:** new rules go in a new file `Jobsy.Web/wwwroot/css/features/testresultaten.css`.
  - Link it in `App.razor` next to the other feature sheets and in `<noscript>`, with its own `?v=20260929-testresultaten`.
  - Add it to `Jobsy.Tests/asset-versions.json` (checked by `AssetVersionGuardTests`).
  - Only touch `app.css` / `app.min.css` for the `.btn-gold` contrast fix, and then regenerate `app.min.css` with the pinned lightningcss build from #364 and bump its `?v`.
- **Must NOT touch:**
  - `features/questionnaire.css`, `QuestionnaireShell.razor`, `LikertScaleQuestion.razor` (these are owned by the questionnaire fix PR)
  - banenkaart files
  - `app-core.js`
  - cookie banner, bottom-nav CSS
  - Mollie/checkout flow (`DeepAnalysisCheckout*`, `MolliePaymentService`). No new payment flow and no price change: the price stays from commercial settings (`DeepAnalysisPriceEuro`).
- **Honesty rules (hard):**
  1. Every locked card has a real paid counterpart. It must appear **in the paid in-app report and in the paid PDF**, computed from the candidate's own answers. If a counterpart isn't built yet, the card is **hidden**, not shown "coming soon". Nothing is promised that isn't delivered.
  2. The locked cards render **fake placeholder content that ships with the client** (static sample data in a Razor component or JSON resource).
     - The server sends **no** real locked data, not even blurred.
     - Add a test that the done-state API/DTO for a non-paying candidate contains no deep-report fields.
  3. **No validation claims.**
     - Norm comparisons say exactly what they compare against (§6.2).
     - Occupation and employer lists are "op basis van je profiel". They are never "gekoppeld aan echte vacatures" unless they really are.
     - No vacancy counts unless they are computed live.

## 1. Current state (audit @ e495b4b9)
These are the gaps this prompt closes. Line numbers are approximate.

**Done page:** `Jobsy.Web/Components/Pages/Candidate/TestDetail.razor:33–171`.
- Competence shows `CompetenceDeepTeaser` with 2 small blurred cards (`Components/Candidate/DeepReport/CompetenceDeepTeaser.razor`). The other 3 tests have **no** locked section.
- The gold button's price sits outside the button, as grey text (`:43–58`). For Competence no amount is shown at all.
- `:43` checks only `!_deepCompleted`. A candidate who already **paid** (unlocked, not completed) still sees "Voer uitgebreide test uit" plus the price. Fix: when unlocked and not completed, show only "Ga verder met de uitgebreide test (x/150)".
- There is no PDF preview, no edit and no redo.

**Paid in-app report:**
- Only Competence has one: `CompetenceDeepReportView.razor`, built by `CompetenceDeepReportBuilder` / `CompetenceDeepReportService`.
- For Career/Culture/Values, the completed page `DeepAnalysis.razor:72–78` shows only "Deep.Completed" and a PDF button. `TestDetail` shows the bars (overwritten with the deep scores by `DeepAnalysisService.MergeTagsIntoQuickScanAsync:376–520`) and, for Career, `CareerCompassPanel`.

**Paid PDF:** `Jobsy.Infrastructure/Services/AssessmentReportPdfService.cs`.
- Competence: rich 9 pages (`RenderCompetenceDeep:243`).
- Career: 1 page with occupation bands and practical notes (`RenderCareer:161`). **No scores.**
- Culture: 1 page of plain-text score lines (`:108–119`, `RenderCulture:641`). The "Toelichting" uses `DeepAnalysisCatalog.CareerAdviceParagraphs`, whose first line calls `CareerCompassBuilder.TypeLabel` and so prints "Werk dat bij je past (88%)" for every culture domain. **Bug.**
- **Values falls into the `else` branch (`:120–150`).**
  - It renders as **"Jouw competentie-rapport"**.
  - Its score lines are raw English codes (`Autonomy: 78%`, since `LabelCompetence` has no values labels).
  - It uses the same wrong career advice first line. **Bug**, fix in PR B.

**Audit of the 6 locked cards per test** (real / partial / missing, against the paid report and PDF):

| Test | Card | Status | Where / note |
|---|---|---|---|
| Competentie | Radar vs norm | **Real** (app) / partial (PDF has norm bars, no radar) | `CompetenceDeepReportView.razor:25–32` (`ScoreRadarChart` + `_traitNorms`); PDF overview `:311–357` |
| Competentie | Vergelijking met anderen | **Real** (Johnson 2014 NL, n=2,707, marked as an indication) | View `:34–73`, PDF `:311–357`; `Johnson2014NormProvider.cs`, `Data/Norms/big-five-norms.json` |
| Competentie | Deelscores per eigenschap | **Real** (6 facets per trait + norm markers) | View `:93–110`, PDF trait pages `:360–456` |
| Competentie | Beroepen die bij je passen | **Real**, but not linked to vacancies | View `:158–173`, PDF `:458–528`; `report.Occupations` (match % + reason) |
| Competentie | Actieplan | **Real** (OpenAI or template) | View `:177–195`, PDF `:530–578` |
| Competentie | Sterke punten & valkuilen | **Real**, per trait (Strength / Pitfall / Tip) | View `:113–135`, PDF trait pages |
| Beroepen | Radar (RIASEC) vs norm | **Missing**. Only bars; no RIASEC norm set | `TestDetail` bars; PDF has no scores |
| Beroepen | Holland-code uitgelegd | **Partial**. Computed and stored (`DeepAnalysisService.cs:412`) but only shown to employers (`TalentPool.razor:81–83`) | — |
| Beroepen | Beroepen die bij je passen | **Real** (compass bands) | `CareerCompassPanel.razor`, PDF `RenderCareer`; the catalog is general occupations, "not live Lobsy vacancies" (`CareerCompassBuilder.cs:227`) |
| Beroepen | Vergelijking met anderen | **Missing** | no norm data |
| Beroepen | Actieplan | **Partial**. `PracticalNotes` only ("Wat betekent dit voor jou?") | app + PDF |
| Beroepen | Sterke punten & valkuilen | **Partial**. `Compass.Strengths` plus per-occupation strengths/gaps (`CareerCompassPanel.razor:98–110`); no profile-level pitfalls | — |
| Cultuur & persoonlijkheid | Radar vs norm | **Missing** in the candidate report. `CultureScorePanel` has a radar but is only used employer-side (`Employer/CultureScan.razor:103`) | — |
| Cultuur & persoonlijkheid | Werkgevers die bij je passen | **Partial**. A per-vacancy culture fit exists (`CandidateVacancyCultureFit`, `ICandidateVacancyCultureFitService`) but isn't in the report; no organisation-type profiles | — |
| Cultuur & persoonlijkheid | Vergelijking met anderen | **Missing** | — |
| Cultuur & persoonlijkheid | Deelscores | **Partial**. 11 domain scores (6 culture + 5 personality); items have no facet codes (`DeepAnalysisCultureItems.cs`) | — |
| Cultuur & persoonlijkheid | Actieplan | **Missing** (only the mis-labelled career advice in the PDF) | — |
| Cultuur & persoonlijkheid | Sterke punten & valkuilen | **Missing** | — |
| Waarden op werk | Radar vs norm | **Missing** | — |
| Waarden op werk | Waarden op volgorde | **Partial**. The 5 scores exist; no ranking or explanation shown. True trade-offs would need forced-choice items, so they are not claimed | — |
| Waarden op werk | Werkgevers die bij je passen | **Missing** | — |
| Waarden op werk | Vergelijking met anderen | **Missing** | — |
| Waarden op werk | Actieplan | **Missing** | — |
| Waarden op werk | Sterke punten & valkuilen | **Missing** | — |

**Answers per question:** stored as `AnswersJson` on `CandidateCompetency`, `CandidateCareerInterest`, `CandidateCulturePersonalityProfile`, `CandidateValuesProfile` and `CandidateDeepAnalysis`. Edit mode is therefore possible (§5.1).

## 2. Done-state template (all 4 tests), `TestDetail.razor` done branch
Order, top to bottom (see `tr-*-desktop.png` / `tr-*-mobiel.png`):
1. **Back link**, "Terug naar mijn tests" (existing).
2. **Header:**
   - `h1` with the test name.
   - Status row: `test-status-badge--success` "Gedaan", then date · Quick-Scan|Uitgebreid · n scores.
   - Actions on the **right** on desktop. On mobile they go below as a 2-column grid of equal-width buttons:
     - `btn-outline-brand` "Antwoorden wijzigen" (pencil icon)
     - ghost button "Test opnieuw doen" (redo icon)
   - Directly under the buttons: the **quota line** (§5.3), e.g. "ⓘ Je kunt deze test nog **2 van de 3** keer aanpassen." At 0, both buttons are disabled and the line becomes a `--warn-soft` note (see `tr-limiet-*.png`).
   - When the extended test is completed, the badge is gold "★ Uitgebreid" (existing).
3. **Result card + score bars.** Keep the current `test-result-card` and `test-score-bars`. Cultuur keeps its second bar group, "Persoonlijkheid op het werk".
4. **"Wat betekent dit voor jou?"** accordion (existing).
5. **Locked section "Uitgebreid rapport"**, only when the extended test is **not** completed (§4).
6. **Gold premium block** (§3). When the candidate paid but hasn't finished, replace the block with a navy "Ga verder met de uitgebreide test" and progress (x/150 or x/200).
7. When the extended test **is** completed:
   - Sections 5–6 are replaced by the real paid report (§6) with "Download PDF".
   - Competence reuses `CompetenceDeepReportView`. The other 3 get the new views from PR B.
   - Until PR B lands, keep today's behaviour for those 3.

**Remove** the gold button and price at the top of the page (`:43–58`). There is exactly **one** premium CTA per page, the block in step 6.

## 3. Gold premium block and `.btn-gold` contrast
- **Block:**
  - `--gold-soft` background, 2px `--gold` border, 5px top strip with the gold gradient, `radius 14px`, subtle gold shadow.
  - Tag pill "★ Uitgebreide test": `--gold-ink` background, white text.
  - Title: "Haal alles uit je {test}".
  - Lead text.
  - 3 checkmarks. They must match the cards that are actually shown.
- **CTA inside the block:** button "Start uitgebreide test" with the **price inside it** as a navy pill ("€ 2,99", from settings, formatted per culture) and the question count "150 vragen · ca. 20 min" (Beroepen: "200 vragen · ca. 25 min").
  - On mobile the count moves into the fine print (see `tr-cultuur-mobiel.png`).
  - Fine print inside the block: shield icon, "Eenmalig · geen abonnement · veilig betalen via Mollie".
  - Secondary button, full width inside the block: "Bekijk voorbeeld-PDF" (§6.4).
- **Contrast fix:** `.btn-gold` text becomes `var(--brand)` on `linear-gradient(135deg, var(--gold-light), var(--gold))`.
  - That is about 5.6–8:1, where today's `--gold-ink` on the gradient is about 2.4–3.6:1 and fails AA.
  - Border 1px `--gold-deep`, `box-shadow: 0 2px 0 var(--gold-deep)`.
  - This applies to every `.btn-gold` in the app. Check the other usages still look right.
- The block has `aria-labelledby` pointing at its title, and the price is readable by screen readers ("Start uitgebreide test, 2,99 euro").

## 4. Locked "Uitgebreid rapport" section (PR A)
### 4.1 Layout
- Heading with a lock icon: "Uitgebreid rapport".
- Lead: "Zo ziet jouw rapport eruit na de uitgebreide test: {n} vragen, {pages} pagina's, volledig op jou afgestemd. Hieronder zie je een voorproefje." `{pages}` is the real page count of the paid PDF for that test.
- Pill: `--warn-soft`, dashed, eye icon, **"Voorbeelddata – niet jouw resultaat"**.
- Card grid: 3 columns from 900px, 2 from 640px, 1 on mobile.
- Each card:
  - Title and subtitle are readable.
  - Chip "🔒 Vergrendeld" in the gold-soft style.
  - Body uses `filter: blur(3px)` with a bottom fade and a rotated dashed stamp **"Voorbeelddata"**.
  - Body gets `aria-hidden="true"`, plus visually hidden text: "Voorbeeld van {card}; beschikbaar na de uitgebreide test".
- Under the grid, 3 checkmarks: "{pages} pagina's als PDF · Gebruik bij sollicitaties · Scherpere matches met vacatures". The last one is true because deep tags merge into matching (`MergeTagsIntoQuickScanAsync`).

### 4.2 Cards per test
Placeholder content comes from a static client-side sample. No real data.

| Test | Cards (in order) |
|---|---|
| Competentie | Radar vs gemiddelde · Vergelijking met anderen · Deelscores per eigenschap · Beroepen die bij je passen · Jouw actieplan · Sterke punten & valkuilen |
| Beroepen | Radar vs gemiddelde · Jouw Holland-code uitgelegd · Beroepen die bij je passen · Vergelijking met anderen · Jouw actieplan · Sterke punten & valkuilen |
| Cultuur & persoonlijkheid | Radar vs gemiddelde · Werkgevers die bij je passen (type organisatie) · Vergelijking met anderen · Deelscores · Jouw actieplan · Sterke punten & valkuilen |
| Waarden op werk | Radar vs gemiddelde · Jouw waarden op volgorde · Werkgevers die bij je passen (type organisatie) · Vergelijking met anderen · Jouw actieplan · Sterke punten & valkuilen |

**Card subtitles must be honest:**
- **Competence comparison:** "Indicatie t.o.v. 2.707 Nederlandse volwassenen (Johnson, 2014)".
- **Other tests' comparison:** "Jij tegenover alle Lobsy-kandidaten die deze test deden".
- **Occupations:** "Top 10 op basis van je profiel, met uitleg".
- **No** vacancy counts.
- **Radar legend:** Competence "Gemiddelde NL (Johnson, 2014)"; others "Gemiddelde Lobsy-kandidaten".
- **Values ranking** is "Wat het zwaarst weegt – en wat minder". Do not call it trade-offs.

### 4.3 Card gating (hard)
- **Single source of truth:** a `DeepReportCapabilities` (Core) that lists, per `AssessmentKind`, which card keys the paid report and PDF **deliver**.
  - `TestDetail` shows only those locked cards.
  - The gold block's checkmarks and the "{pages} pagina's" number read from it too.
- Comparison cards are also gated at runtime by norm availability (§6.2). If there are no norms, the card is hidden, and it is hidden in the paid report too.
- **Tests:**
  - For each kind, every capability key has a rendered section in the paid view **and** a PDF section. This is a unit test over the report model and a PDF text-extraction smoke test.
  - A card that isn't in capabilities is not rendered in the locked grid.

## 5. Edit, redo and the 3-adjustments limit (PR A)
### 5.1 "Antwoorden wijzigen" (edit mode)
- **Depends on the separate questionnaire fix PR**, which does two things:
  - It adds the **non-destructive pending-edit save**: editing a completed test never downgrades it to Draft or clears scores until you explicitly re-complete. Today it does, in `CandidateCompetencyService.cs:98–109`, `CandidateCareerInterestService.cs:104`, `CandidateCulturePersonalityService.cs:97`, `CandidateValuesService.cs:98` and `DeepAnalysisService.cs:344–350`.
  - It fixes the sticky bar/scroll (desktop `overflow: clip` at `questionnaire.css:480`, the bottom padding, the `GoNextAsync` no-op, and the Career numbering jump 25→151).
- **Do not re-implement either of those here.** If that PR has not merged into `acceptatie` when you start, rebase on it, or keep the edit button behind a feature flag (`Features:TestEditMode`, default off) and say so in the PR.
- **Route:** `/candidate/{test}?mode=edit`, or `/candidate/deep-analysis/{Kind}?mode=edit` for a completed extended test. Uses `QuestionnaireShell` in edit mode (see `tr-wijzigen-*.png`):
  - Title "Antwoorden wijzigen"; the subtitle is the test name.
  - Right of the header: a counter "{n} gewijzigd" in `--warn`.
  - Progress bar full, in `--success`.
  - Info banner: "Je huidige resultaat blijft staan tot je op 'Opslaan en resultaat bijwerken' tikt. Opslaan telt als 1 van je 3 aanpassingen."
  - Under the save button: "Daarna kun je deze test nog {remaining-1} van de 3 keer aanpassen" (§5.3).
  - All questions show collapsed with their value and a pencil. Tap one to expand.
  - A changed question shows "was {old}" (pill) and the old value outlined on the scale.
  - Footer: primary "Opslaan en resultaat bijwerken" (disabled when 0 changes) and ghost "Annuleren – niets wijzigen". Leaving with unsaved changes opens a confirm dialog.
- **Save** uses the pending-edit API from the fix PR. It rescores, updates tags and matching, and for the extended test regenerates the report and PDF (invalidating the PDF cache key). No new payment.
- **A successful save consumes 1 adjustment** (§5.3). Opening edit mode or cancelling costs nothing. Edit mode can't be opened when 0 are left (the API returns 409).

### 5.2 "Test opnieuw doen"
- Bottom sheet on mobile, dialog on desktop (see `tr-opnieuw-mobiel.png`):
  - Title "Test opnieuw doen?"
  - Text: "Je begint met lege antwoorden. Je huidige resultaat blijft zichtbaar tot je de nieuwe test afrondt…"
  - Info box: "Opnieuw doen telt als 1 van je 3 aanpassingen. Daarna kun je deze test nog {remaining-1} van de 3 keer aanpassen."
  - A hint pointing to "Antwoorden wijzigen" ("dat telt ook als 1 aanpassing").
  - Buttons: "Opnieuw beginnen" / "Liever antwoorden wijzigen" / "Annuleren".
- **The adjustment is consumed when the new attempt is completed**, not when it starts. Abandoning costs nothing.
  - Starting requires ≥ 1 adjustment left.
  - Only one open attempt per test and variant at a time.
- **The old result is kept until the new attempt completes.**
  - Store the attempt separately: new entity `CandidateAssessmentAttempt` (`UserId`, `Kind`, `Variant` Quick|Deep, `AnswersJson`, `Status`, `StartedAtUtc`, `CompletedAtUtc`, `ScoresJson`, `ReportJson`, `ReportVersion`), with an EF migration.
  - On completion, snapshot the previous result into the history and promote the new one.
  - Abandoning leaves the current result untouched.
- **Earlier results:** a list "Eerdere resultaten" at the bottom of the done page (date and headline). Each opens a read-only view with the bars (and the stored report/PDF for extended attempts).
- **Privacy:**
  - History is included in the AVG export (`PrivacyDataService`) and in deletion.
  - Employers only ever see the current result.
- **Extended redo never charges again** (Dennis's decision): the candidate bought the test, not one attempt. No checkout is involved in redo or edit.

### 5.3 The 3-adjustments limit (hard, API-enforced)
- **Rule (Dennis):** each test can be **adjusted at most 3 times in total**.
  - "Antwoorden wijzigen" (a saved edit) and "Test opnieuw doen" (a completed retake) each count as **1**.
  - The first completion of a test is **not** an adjustment.
  - This replaces any time-based rule. There is **no** 30-day limit.
- **Scope:** one counter per candidate per test and variant: Competentie, Beroepen, Cultuur & persoonlijkheid, Waarden op werk, each as Quick-Scan **and** as extended test, so 8 counters.
  - The extended counter covers edits and retakes of the paid 150/200-question test.
  - Keep the maximum in one constant, `AssessmentAdjustmentRules.MaxAdjustments = 3` (Core).
- **Storage:** new table `CandidateAssessmentAdjustment` (`Id`, `UserId`, `Kind`, `Variant` Quick|Deep, `Type` Edit|Retake, `AtUtc`, `AttemptId?`), with an EF migration.
  - Used = row count per (`UserId`, `Kind`, `Variant`).
  - Insert the row in the **same DB transaction** as the edit-save or the retake completion, so a failed save doesn't consume one.
  - Guard against double submits with a unique `AttemptId` / an idempotency key.
- **API:**
  - Every done-state/test DTO returns `adjustments: { used, remaining, max }`.
  - Edit-open, edit-save, retake-start and retake-complete all check `remaining > 0` on the server and otherwise return **409** `{ code: "assessment_adjustment_limit", remaining: 0, max: 3 }`.
  - The UI never decides on its own.
- **UI copy (quota line), with correct plurals per language:**
  - NL: "Je kunt deze test nog {n} van de 3 keer aanpassen." When n=1: "…nog 1 van de 3 keer…"
  - EN: "You can adjust this test {n} more time(s) (out of 3)." Use proper singular/plural: "1 more time" / "2 more times".
  - pl/ro/ar: use the plural forms of the existing localization helper. Polish needs one/few/many.
  - **At 0:** NL "Je hebt deze test al 3 van de 3 keer aangepast. Wijzigen en opnieuw doen kan niet meer; je huidige resultaat blijft staan." EN "You've already adjusted this test 3 times (the maximum). Editing and retaking are no longer possible; your current result stays as it is."
  - Both buttons are `disabled` with `aria-describedby` pointing at that note.
- **Where it shows:**
  - result page under the action buttons
  - edit-mode banner and footer
  - redo sheet
  - the 409 error state (friendly dialog with the same text)
- **Privacy:** adjustment rows are included in the AVG export and in account deletion.
- **Admin:** no UI in this PR. If support ever needs a reset, that is a later admin feature. Log it as a follow-up issue.

## 6. Paid report and PDF parity (PR B)
### 6.1 Report models
- Generalise the stored report. `CandidateDeepAnalysis.ReportJson` / `ReportVersion` already exist per row, so no new column is needed.
  - Add `CareerDeepReport`, `CultureDeepReport` and `ValuesDeepReport` in `Jobsy.Core/Reports/{Career,Culture,Values}/`, modelled on `CompetenceDeepReport` (builder + JSON + texts, with an optional OpenAI summary/action plan and a template fallback, like `CompetenceDeepReportService`).
  - Build on completion and on edit-save. Render the in-app view and the PDF from the stored report only (no AI on download).
  - **Language-neutral storage:** the stored report holds scores, levels and **text keys**, not rendered Dutch sentences. Texts are resolved at render time from the resource catalogs (§7.2).
    - AI-generated parts (summary, action plan) are stored **per language** (`{ "nl": …, "en": … }`), generated lazily for a language on first request, then stored.
    - The template fallback exists in both languages.
    - Bump `ReportVersion` and migrate/rebuild existing Competence reports. Today they store Dutch prose in `Meaning`, `WorkQuote`, `Pitfall`, … (`CompetenceDeepReport.cs`).
- **In-app views:** `CareerDeepReportView`, `CultureDeepReportView` and `ValuesDeepReportView`, in the style of `CompetenceDeepReportView`. They are shown in `TestDetail` and on `DeepAnalysis.razor`'s completed page.
- **Per card, the minimum content:**

| Card | Competence | Career | Culture | Values |
|---|---|---|---|---|
| Radar vs gemiddelde | exists | 6 RIASEC axes | 6 culture axes (+5 personality as a second radar or bars) | 5 value axes |
| Vergelijking met anderen | exists (Johnson) | Lobsy norm (§6.2) | Lobsy norm | Lobsy norm |
| Deelscores | exists (facets) | — | 11 domain scores grouped "Werkcultuur" / "Persoonlijkheid op het werk", each with a 1-line meaning. Do **not** invent facets. | — |
| Holland-code | — | 3-letter code with explanation and fitting work environments (from `HollandCode` + `CareerCompassBuilder.TypeLabel`) | — | — |
| Beroepen / werkgevers | exists | compass bands (exists; add reasons) | organisation-type fit: static rule-based profiles (start-up, familiebedrijf, projectorganisatie, corporate, overheid, zorginstelling…) scored against the 6 culture axes. Optionally list the candidate's top real vacancies with `CandidateVacancyCultureFit` ≥ threshold, labelled as vacancies. | same, scored against the 5 value axes |
| Waarden op volgorde | — | — | — | the 5 values ranked, each with a meaning and "wat betekent dit bij het kiezen van werk" |
| Actieplan | exists | 3 steps (AI/template) from the top-3 types | 3 steps from the top/bottom culture axes | 3 steps from the top-2 values |
| Sterke punten & valkuilen | exists | profile level: 3 + 3, template per RIASEC type (top-3 / bottom-2) | 3 + 3 from the axes | 3 + 3 from the values |

- **PDF:** new `RenderCareerDeep`, `RenderCultureDeep` and `RenderValuesDeep` in the same visual style as `RenderCompetenceDeep`: cover, overview with radar or bars, one section per card, action plan, sources/disclaimer.
  - Target 7–9 pages. Expose the real page count to `DeepReportCapabilities`.
  - Competence PDF: add the radar to the overview page, since the app has it and the PDF doesn't.
- **Fix the bugs:**
  - Values must no longer render as "Jouw competentie-rapport" with raw codes.
  - Culture/Values must no longer print the career advice ("Werk dat bij je past").
  - Keep the legacy fallback only for rows without a stored report, but with the correct title and labels per kind.

### 6.2 Norms, honest interim
- **Competence:** keep Johnson (2014), NL, n=2,707, IPIP-NEO-120, with the existing disclaimer (`Johnson2014NormProvider.SourceDisclaimerText`). Always label it "indicatie". Our items are not identical to the source instrument.
- **Career, Culture, Values:** there is **no** literature norm that fits our scoring. The items are custom Dutch workplace items scored as percent of maximum, not RIASEC/PVQ instruments with published norms. **Do not** use literature norms.
  - Use an internal norm instead: **"alle Lobsy-kandidaten die de uitgebreide {test} afrondden"**.
  - Nightly aggregate job: per kind and domain, mean and p25/p50/p75 over completed deep attempts (current results only, one per candidate). Store only aggregates in a new table `AssessmentNormSnapshot` (`Kind`, `Domain`, `N`, `Mean`, `P25`, `P50`, `P75`, `ComputedAtUtc`). No per-candidate data leaves the row.
  - Show it **only when N ≥ 100** for that kind. Otherwise hide the comparison card in the locked grid, the paid view and the PDF, and remove the norm polygon from the radar ("Jij" only).
  - Label: "Vergeleken met {N} Lobsy-kandidaten ({maand jaar}). Dit is geen wetenschappelijk gevalideerde normgroep."
  - Demo/test accounts are excluded from the aggregate.
- **Never** write "normgroep uit de regio", "18–35 jaar" or "gevalideerd" anywhere.

### 6.3 Capabilities after PR B
All 6 cards for all 4 tests are delivered. The comparison card remains runtime-gated by N ≥ 100 for Career, Culture and Values.

**If B is split further:** ship each kind's cards only when its view **and** PDF sections exist, and keep the rest out of `DeepReportCapabilities`. PR A then shows fewer cards for that test, and no "binnenkort" labels.

### 6.4 Sample PDF, "Bekijk voorbeeld-PDF"
- Generated by the **same** PDF renderers with a fixed sample candidate ("Voorbeeldkandidaat") and fixed sample answers per kind: a static JSON under `Jobsy.Core/Data/SampleReports/`. No AI.
  - Every page carries a diagonal watermark: **"VOORBEELD"** in nl, **"SAMPLE"** in en. The cover says "Voorbeeld – niet jouw resultaat" / "Sample – not your result".
  - Endpoint: `GET /api/assessments/{kind}/sample-report.pdf?lang={nl|en}`. Available to any authenticated candidate, cached in memory by `kind` + `lang` + `ReportVersion`.
- **UI** (see `tr-pdf-*.png`):
  - Desktop: dialog with pages 1–2 side by side, header "Voorbeeld-PDF · {test}", "Pagina 1–2 van {pages}", "Download voorbeeld", and in the footer the gold CTA with the price inside.
  - Mobile: bottom sheet with pages stacked, then the gold CTA and "Download voorbeeld (PDF)".
  - Render page images server-side (PNG of pages 1–2 via the PDF library's image export, if available), or open the PDF in a new tab as the fallback.
- **Until PR B:** the button is hidden (flag `Features:SamplePdf`).

## 7. Localization, accessibility, tests
### 7.1 UI strings (PR A)
- All new UI strings (result page, locked cards, gold block, edit/redo, quota, dialogs) go in `UiStringsCompetencies.cs` or a new `UiStringsTestResults.cs`, for **nl, en, pl, ro, ar**.
- No hard-coded text in Razor.
- The locked-card placeholder sample content is localized too. At minimum nl and en; pl/ro/ar may fall back to en for the sample content only.
- Update the `LocalizationParityReportTests` baseline only for genuinely identical strings.
- RTL check for `ar`: logical properties, mirrored chevrons.

### 7.2 Paid report, PDF and sample PDF in Dutch **and** English (PR B)
- **Language rule:** the report language follows the user's UI language (`CultureState` / language cookie; for the API, pass `lang`).
  - `en` → English.
  - Everything else (`nl`, `pl`, `ro`, `ar`) → Dutch for now.
  - pl/ro/ar report texts are a **later follow-up**. Open an issue listing the catalogs to translate.
  - The in-app report view, the paid PDF and the sample PDF all follow the same rule. The PDF cache key includes the language.
  - File names are localized: `Lobsy-{slug}-rapport-{date}.pdf` / `Lobsy-{slug}-report-{date}.pdf`.
- **All report strings live in resource catalogs:**
  - section titles, trait/facet/domain/type/value labels, level bands, template sentences (meaning, strengths, pitfalls, tips, work fit, action-plan templates), organisation-type names and descriptions, occupation titles and reasons, the norm/source disclaimers (incl. an EN version of `Johnson2014NormProvider.SourceDisclaimerText`), PDF headers/footers, cover and watermark.
  - Use `.resx`, or Core catalogs in the existing `UiStrings*` style, **one per report kind**.
  - **No string literals** in builders, report services or PDF renderers. That includes today's Dutch literals in `AssessmentReportPdfService.cs` (e.g. "Jouw loopbaanrapport", "Toelichting", "Facetten") and in `CompetenceDeepReportTexts.cs` / `CareerCompassBuilder` labels used by the report.
- **English quality: polished and natural, not machine-literal.** Write it the way a native UK-English careers adviser would.
  - Short sentences, second person, warm and plain.
  - No Dutch word order, no calques. For example, "Aanpakken met je handen" → "Hands-on work", not "Tackling with your hands"; "Netjes organiseren" → "Organising and order"; "Kalm onder druk" → "Calm under pressure"; "Waarden op werk" → "Work values".
  - **Glossary** (use consistently):

| Dutch | English |
|---|---|
| Quick-Scan | quick scan |
| Uitgebreide test | extended test |
| Uitgebreid rapport | full report |
| Voorbeelddata – niet jouw resultaat | Sample data – not your result |
| Antwoorden wijzigen | Edit answers |
| Test opnieuw doen | Retake test |
| Normgroep | comparison group |
| indicatie | indication |
| Holland-code | Holland code |
| Beroepentest | Career test |
| Competentietest | Competency test |
| Cultuur & persoonlijkheid | Culture & personality |
| Actieplan | Action plan |
| Sterke punten & valkuilen | Strengths & pitfalls |

  - Occupation titles need real English equivalents (a map per occupation key). Never output a Dutch occupation title in the EN report.
  - Put all EN report copy in the PR description as a table (key → nl → en), so Dennis can review it.
- **AI parts:** the OpenAI prompt receives the target language explicitly ("Write in natural UK English…"). Validate the output language with the leftover check below, and fall back to the EN template if it fails.

### 7.3 Accessibility
- Locked bodies are `aria-hidden` with visually hidden descriptions.
- Dialogs have focus trap, Esc and focus return.
- Buttons ≥ 44 px.
- Contrast AA for the gold block (add a unit test on the token pair if there's a contrast helper; otherwise document the ratios in the PR).

### 7.4 Tests (xUnit)
- **(A)** Done-state DTO for an unpaid candidate contains no deep-report data.
- **(A + B)** `DeepReportCapabilities` ↔ view ↔ PDF parity per kind (§4.3).
- **(B)** Norm snapshot: hidden when N < 100, shown when ≥ 100, demo accounts excluded.
- **Adjustment limit (A):**
  - The first completion doesn't count.
  - Edit-save and retake-complete each count 1 (3 total, per test and variant).
  - The 4th attempt returns 409 `assessment_adjustment_limit` on edit-open, edit-save, retake-start and retake-complete.
  - A cancelled edit or abandoned retake doesn't count.
  - A failed save (exception) doesn't count (transaction).
  - A double submit counts once.
  - Quick-Scan and extended counters are independent.
  - The DTO returns `used/remaining/max`.
  - Rows are in the AVG export and in deletion.
- **Retake (A):** the old result stays until completion; abandoning doesn't change the current result; the previous result is in history; the extended retake creates no checkout.
- **Quota copy (A):** NL/EN singular and plural render correctly (n = 3, 2, 1, 0); Polish few/many forms exist.
- **(A)** Edit mode: save rescores without a Draft downgrade (via the fix PR's API); cancel changes nothing.
- **(B)** PDF: the Values PDF title is "Jouw waardenrapport" / EN "Your work values report" (not competence); Culture/Values contain no "Werk dat bij je past".
- **(B)** Sample PDF: every page contains "VOORBEELD" (nl) / "SAMPLE" (en); the endpoint works without a paid row.
- **English report (B):** for all 4 kinds, render the in-app report model **and** the paid PDF, plus the sample PDF, with `lang=en`. Use the sample answers and a few random answer sets. Then:
  - **No Dutch leftovers:** extract the PDF text and the view text, and assert none of a Dutch stop-word/marker list appears as whole words: `je`, `jij`, `jouw`, `het`, `een`, `van`, `werk`, `niet`, `ook`, `bij`, `voor`, `rapport`, `vragen`, `pagina`, `Uitgebreid`, `Voorbeeld`, `Werk dat bij je past`, plus any `ij`-diphthong word from an allowlisted detector.
  - **No raw codes:** assert none of the following appear: `Autonomy`, `Connection`, `Achievement`, `Stability`, `Impact` as bare domain codes, `Realistic`, `Investigative`, `Artistic`, `Social`, `Enterprising`, `Conventional` as codes, `Consciëntieusheid`, `EmotioneleStabiliteit`, `PeopleFirst`, facet codes like `C1`/`N6`, unresolved keys (`Deep.`, `Report.`, `{`, `}`).
  - Where an English label legitimately equals a code (e.g. "Impact"), compare against the **label map**, not the raw code. The test asserts every rendered label came from the catalog.
  - The same test with `lang=nl` asserts no English template leftovers and no raw codes.
  - `pl`/`ro`/`ar` UI → the report is Dutch.
- **(A)** `TestDetail`: an unlocked-but-not-completed candidate sees no price.
### 7.5 PR screenshots
Take them with Playwright against a local/dev build, as the demo candidate, and compare each side by side with the mockups:
- done state for each of the 4 tests at 1280 and 390, full page
- the PDF dialog at 1280 and the sheet at 390
- edit mode at 390
- the redo sheet at 390
- the limit-reached state (0 of 3) at 1280 and 390, plus the 409 dialog
- for an EN UI user: the result page, the in-app report and PDF pages 1–2 for one kind, plus the EN sample PDF
- one completed extended report per kind, plus pages 1–2 of each real PDF, with a demo account

## 8. Done when
- The 4 done pages match the mockups (tokens, not inline styles).
- Every visible locked card has a real counterpart in the paid view **and** PDF, computed from the candidate's answers.
- The Values/Culture PDF bugs are fixed.
- The norms are honest (§6.2).
- The gold block passes AA with the price inside.
- Edit mode depends on the fix PR (flagged if it isn't merged).
- Each test and variant can be adjusted at most 3 times (edit or retake), enforced by the API. The quota is visible on the result page, in edit mode and in the redo sheet. Both buttons are disabled at 0. The extended retake is never charged again.
- UI strings exist in all 5 languages.
- The paid report, PDF and sample PDF exist in Dutch and polished English, following the UI language. All report texts come from catalogs, and the EN/NL leftover and raw-code tests are green.
- All tests are green, and the screenshots are in the PR.
- Nothing merged, nothing deployed.

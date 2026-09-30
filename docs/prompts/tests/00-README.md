# Candidate tests in the ontdekkingsreis style: payment + autosave hotfix, one question flow, depth levels, uitgebreide test (Cursor run book)

Cursor: **read this file completely**, then **execute the files below strictly in order**, one at a time. Each file is one PR.

> **Rules (repeated in every file):**
> - Never merge, never deploy, never use rule `123` (`.cursor/rules/shortcut-123.mdc`).
> - Never push to `main` or `acceptatie`; push only the current file's `cursor/tests-*` branch; no force-push.
> - ONE PR per file into `acceptatie` (01 standalone, 02+ stacked).
> - Red build/tests or an unmet success criterion: push, open the PR as **draft**, stop and report. Don't start the next file.
> - Nothing unlocks a paid test without a **paid** status from Mollie (or the stub path, only in Development or with `JobsyAuth:AllowStubPayments=true`). The UI never shows `ex.Message`.
> - Don't change the candidate nav (order, items, labels). Dennis' order is a separate add-on.

**What this stack builds.** Dennis approved the phase-1 review, the `ts-*` mockups and all six defaults on 30-09 ("Akkoord"). He named **no price**: the price of the uitgebreide test becomes an admin value **per test type, incl. btw**, and the default is today's code value (**€ 2,99**, `FlexCommercialSettings.DefaultDeepAnalysisPriceEuro`; acceptatie shows € 2,99 as well). The € 2,99 in the mockups is only an example.
- **01 is a standalone hotfix** that can run first and on its own:
  - **Real payment for the uitgebreide test.** It reuses the token path: `MolliePaymentService`, `MollieWebhooksController` and a reconcile job. The test unlocks only when the status is paid. Stubs are only for Development/acceptatie.
  - **What the candidate sees before and after paying:**
    - an order summary with the price incl. btw
    - a waiver checkbox for the 14-day cancellation right
    - receipt + invoice by mail
  - **Code fixes:**
    - the test type comes from the checkout row, not from a paymentId substring
    - the "Veilig betalen via Mollie" claim only shows when Mollie is real
    - the price is set per test type in admin
  - **Autosave:**
    - flush when leaving the page
    - no raw "A task was canceled."
    - no `ex.Message` anywhere on the test pages
- **02–07 (stacked)** rebuild the candidate test pages in the language of De ontdekkingsreis (`docs/mijn-paspoort` 07/08) and Carrière (`docs/carriere`): the scene, `JourneyLobster`, `LobsyBubble`, the rail. The metaphor is **diving**: Eerste indruk 5 · Iets dieper 10 · Heel diep 25 (Cultuur 18) = the free test, and **De bodem** = the paid uitgebreide test.
  - **Shared question flow.** One `TestQuestionFlow` (one question at a time) and one `TestDepthRules` set of levels and counts, shared with ontdekkingsreis steps 7–10.
  - **Rules for every test:**
    - consent is checked before the first question
    - the 3-change limit really works
    - "Afronden" always leads to `/profiel/tests/{key}`
    - career matches show as bands, and only with Werkgevers ON
  - **The uitgebreide test** comes in 5 parts with pause points and inline motivation (no pop-ups).
  - **Language and access:**
    - one product name
    - B1 copy
    - accessibility: radiogroup, arrow keys, focus kept, no black title box
    - all 5 languages incl. the uitgebreide test, with `ar` RTL
  - **Last file:** Playwright E2E + the stack-end report.

## Bugs fixed (review at `a611db40`, live acceptatie 30-09 ~08:30 CEST, kandidaat@ / valentine@jobsy.local, nothing submitted)

| # | Bug | Evidence | Fixed in |
|---|---|---|---|
| P1 | **No real payment for the uitgebreide test.** `StartCheckoutAsync` always creates `stub_deep_{slug}_{guid}` and returns our own return URL. In Production (`AllowStubPayments=false`) it throws a raw Dutch developer message, so the product can't be bought at all | `DeepAnalysisService.cs` L155–218; `render.yaml` L57 (prod false) / L229 (acc true) | 01 |
| P2 | On acceptatie the **return page marks the checkout paid** and unlocks: `CompleteCheckout` calls `TryFulfillPaidCheckoutAsync(allowDevStubMarkPaid: true)`. There is no webhook and no reconcile for deep checkouts (tokens have both) | `DeepAnalysisController.cs` L68–95; `MollieWebhooksController` only looks at `TokenPurchaseCheckouts` | 01 |
| P3 | The test type after payment is guessed from a **paymentId substring** (`_career_`/`_values_`/`_culture_`, else Competence). The row already stores `Kind` | `DeepAnalysisController.cs` L88–92 | 01 |
| P4 | "Veilig betalen via Mollie" is shown although Mollie is never used | `TestDetail.razor` L321, `Tests.SecureMollie` | 01 |
| P5 | One click on "Uitgebreide test" starts checkout. There is no order summary, no "incl. btw", no waiver of the 14-day cancellation right for digital content, and no receipt or invoice | `TestDetail.razor` L730–758; `DeepAnalysis.razor` L435 | 01 (summary, waiver, mail), 05 (design) |
| P6 | Checkout return page: it sets the error and navigates away at once (never seen). Without `paymentId` it shows a dead end "Geen betalingskenmerk gevonden." | `DeepAnalysisCheckout.razor` L25–48 | 01 (states), 05 (design) |
| P7 | One price for all tests; admin can't set it per test type | `FlexCommercialSettings.DeepAnalysisPriceEuro`, `SettingsAdmin.razor` | 01 |
| A1 | **Last answer lost**: `QuestionnaireAutosave.DisposeAsync` cancels the 800 ms debounce without saving | `QuestionnaireAutosave.cs` `DisposeAsync` | 01 |
| A2 | A new answer during a running save cancels it. `OperationCanceledException` escapes `SetAnswerAsync`, and the page shows **"A task was canceled."** | `SetAnswerAsync` `_debounceCts` reuse; page `catch (Exception ex) { _banner = ex.Message }` | 01 |
| A3 | "Bewaard" shows before any answer (`Status` starts `Saved`) | `QuestionnaireAutosave.cs` | 01 |
| A4 | Raw `ex.Message` on every test page (load, banner, retry, finish, pay) | `CompetencyTest` L181/234/286/311, `CareerTest` L163/229/256/282, `CultureScan` L110/164/191/216, `ValuesScan` L109/163/190/215, `DeepAnalysis` L371/417/440/458/480/523, `DeepAnalysisCheckout` L42/44, `TestDetail` L488/722/752/754/776/813/842 | 01 |
| Q1 | Every page renders all questions as one long list; the uitgebreide test renders **150 at once** (live kandidaat: 99 open fieldsets, 495 Likert buttons) | live `k-deep-*.txt`; `DeepAnalysis.razor` | 02, 05 |
| Q2 | Two scales for the same question: full pages "Past niet / Past wel", onboarding "Past niet / Past heel goed" (same `LikertScaleQuestion`) | `Questionnaire.Likert.*` vs `Onboarding.Likert.*` (`UiStringsOnboardingV2.cs` L77) | 02 |
| Q3 | `CompetencyTest` and `DeepAnalysis` duplicate the question markup instead of `QuestionnairePageBody`. Prerender: Competency/Career `true`, the others `false` | the razor headers | 02 |
| Q4 | Double numbering "1/5" + "1. …"; the dimmed next question has low contrast; RIASEC jargon "R — Aanpakken met je handen" | live `v-car-d.png` | 02, 04 |
| C1 | No consent check up front: without test/parental consent the candidate answers everything while every autosave fails with a 400 | Save endpoints `CanUseTests`; pages don't check `HasCurrentTestAiConsent` | 02 |
| X1 | Likert = 5 `aria-pressed` buttons "{0} van 5": no radiogroup, no arrow keys, 5 tab stops, anchor labels not tied to the options | `LikertScaleQuestion.razor` | 02 |
| X2 | After answering, the fieldset is replaced by a collapsed button, so **focus is lost** to `<body>` | `QuestionnaireShell` / page markup | 02 |
| X3 | Black focus box on the h1 after navigation (all test pages except competencies) | live screenshots | 02 |
| R1 | **The 3-change limit doesn't work**: `AssessmentAdjustmentService.RecordAsync` is never called and attempts are never Completed. Only `StartRetake` checks `EnsureRemainingAsync`; the Save endpoints don't. TestDetail always says "nog 3 van de 3 keer" | `AssessmentRetakeService.cs`, `CandidateCompetenciesController` PUT etc.; live kandidaat | 03 |
| F1 | "Afronden" navigates straight away, with no finish moment. The targets differ (`/candidate/profile`, `…?tab=culture`, `/profiel`), and so do the back links | the 4 pages | 04 |
| F2 | Career matches panel "Top 10 vacatures … 60% of hoger" with percentages, no Werkgevers gating | `CareerTest.razor` | 04 |
| N1 | Five names for one product: Uitgebreide test / Uitgebreide competentie-analyse / diepte-analyse / Diepteanalyse / Uitgebreid rapport; "officiële PDF-rapportage", "Quick-Scan", raw DOI citation, "We maken tags voor matching" | `DeepAnalysisService.FormatUpsellCopy` L64–78, `UiStringsCompetencies.cs`, `TestDetail` | 04, 05 |
| D1 | `DeepAnalysisBoosters` = modal pop-ups with odd copy ("Halverwege de eerste helft? Nee…", "Halverwege!" at 75 also for the 200-question career test, "Bijna thuis" at 150 = finished), hardcoded Dutch in Core | `Jobsy.Core/Rules/DeepAnalysisBoosters.cs` | 05 |
| D2 | Offer = a bare card with one button; checkout errors are swallowed (`_message` only shows in the load-failed branch); invalid `{Kind}` silently becomes competence; checkout page in focus mode without a way back | live `v-deep-d.png`, `v-chk-d.png`; `MainLayout.IsQuestionnairePath` | 05 |
| D3 | TestDetail: the paid gold button comes before the free one; "20/25 min" for 150/200 questions | `TestDetail.razor` ~L210–330 | 05 |
| L1 | `UiStringsCompetencies`/`UiStringsCulture`/`UiStringsValues` merge the **Dutch** dictionary into pl/ro/ar. Culture (18) and values (25) questions are Dutch even in `en` (`En()` copies `Nl()`) | the three files | 06 |
| L2 | The uitgebreide test is Dutch-only: `PromptNl`, `ExampleNl`, `DomainLabel`, server errors, upsell copy | `DeepAnalysisCatalog`, `DeepAnalysisQuestionHelp` | 06 |
| L3 | RTL untested; plain LTR layout | — | 02–06, 07 |

No forced-choice or ranking question type exists (all tests are Likert 1–5), so this stack has none.

## Order

| # | File | Branch | Branches from | PR into |
|---|---|---|---|---|
| 01 | `01-hotfix-betaling-autosave.md`: **standalone hotfix** (not stacked). Real Mollie for the uitgebreide test via the token path (create, webhook, reconcile, return status); unlock only on paid; stubs only Dev/`AllowStubPayments`; `Kind` from the checkout row; price per test type (admin, incl. btw, default € 2,99); order summary + waiver checkbox; receipt + invoice mail; "Veilig betalen via Mollie" only when real; autosave flush/cancel/status fixes; no `ex.Message` on test pages | `cursor/tests-hotfix` | `origin/acceptatie` | `acceptatie` |
| 02 | `02-vraagflow-diepte.md`: `TestQuestionFlow` (one question at a time, radiogroup, arrow keys, focus kept) + `TestDepthRules` (levels + counts) shared with ontdekkingsreis 07/08; consent gate up front; save error codes; prerender consistent; h1 focus fix | `cursor/tests-2` | `cursor/tests-hotfix` (or `origin/acceptatie` when PR 01 is merged) | `acceptatie` |
| 03 | `03-aanpassingen-limiet.md`: the 3-change limit enforced on every save endpoint, adjustment history, attempts completed, correct TestDetail counter | `cursor/tests-3` | `cursor/tests-2` | `acceptatie` |
| 04 | `04-testpaginas.md`: the 4 free test pages in the journey style (intro with depth choice, question screen, "Weer een laag eraf"), finish → `/profiel/tests/{key}`, career matches as bands only with Werkgevers ON, one product name, B1 copy, TestDetail alignment | `cursor/tests-4` | `cursor/tests-3` | `acceptatie` |
| 05 | `05-uitgebreide-test.md`: uitgebreide test on `TestQuestionFlow` in 5 parts with pause points and inline motivation; offer screen, checkout states and TestDetail offer in the new design; invalid kind; free first | `cursor/tests-5` | `cursor/tests-4` | `acceptatie` |
| 06 | `06-talen-rtl.md`: every test question, example, label and message in nl/en/pl/ro/ar (incl. culture/values `en`, the uitgebreide test and the invoice/receipt), RTL pass | `cursor/tests-6` | `cursor/tests-5` | `acceptatie` |
| 07 | `07-e2e-rapport.md`: Playwright E2E (desktop + mobile, 5 languages incl. RTL, reduced motion, Werkgevers OFF, payment states), docs, cleanup, stack-end report | `cursor/tests-7` | `cursor/tests-6` | `acceptatie` |

If a file is too big for one reviewable PR (> ~1.500 changed lines excluding tests/migrations/strings), split it into `a`/`b` at the seam the file names. The next file then branches from the **last** sub-branch.

## Pointer prompt (the only prompt needed; it runs 01 … 07)
```
Run the Lobsy candidate-tests stack. First: git fetch origin && git show origin/docs/tests:docs/prompts/tests/00-README.md — read it completely.
Then read and execute each file in docs/prompts/tests/ on that branch strictly in the order the README's table lists (01 … 07; a/b splits where a file allows it), one file = one PR.
File 01 is a standalone hotfix (payment + autosave): it branches from origin/acceptatie, is not stacked, and its PR body starts with "Standalone hotfix (not stacked)". If PR 01 already exists (the hotfix was run on its own), don't redo it: reuse its branch.
File 02 branches from cursor/tests-hotfix (or from origin/acceptatie if PR 01 is already merged); every later file branches from the previous file's branch (stacked). Each opens ONE PR into acceptatie whose body starts with "Stacked on #<prev PR>".
Before 01 and 02, run the dependency checks in the README's "Dependencies" section and follow the fallback it prescribes for each one; say in the PR which case applied. Re-run A–E before 04 and 05.
Build and test after each file; if tests fail or a success criterion can't be met, push, open that PR as draft, stop and report — don't start the next file.
Never merge, never deploy, never use rule 123, never push to main or acceptatie, no force-pushes. Nothing unlocks a paid test without a paid status. Never show ex.Message. Don't change the candidate nav.
At the end report: file → branch → PR number → status, the dependency cases, what Dennis still has to decide (prices per test type, invoice/btw handling, native review pl/ro/ar) and anything deferred (07 writes the stack-end report).
```

### Pointer prompt: hotfix 01 only
```
Run only the payment + autosave hotfix from the Lobsy candidate-tests stack. First: git fetch origin && git show origin/docs/tests:docs/prompts/tests/00-README.md (read "How to run", §0, §P and Dependencies D and F) and git show origin/docs/tests:docs/prompts/tests/01-hotfix-betaling-autosave.md — read it completely.
Execute only file 01: branch cursor/tests-hotfix from origin/acceptatie (not stacked on anything), ONE PR into acceptatie whose body starts with "Standalone hotfix (not stacked)".
Fix (a) the uitgebreide test payment: real Mollie via the token path (MolliePaymentService create, MollieWebhooksController, a reconcile job, server-side return status); unlock only on a paid status; stubs only in Development or with JobsyAuth:AllowStubPayments=true; the test type from the checkout row, never from a paymentId substring; an admin price per test type incl. btw (default = today's € 2,99); an order summary with a required 14-day-waiver checkbox; a receipt + invoice mail; "Veilig betalen via Mollie" only when real Mollie is active. (b) autosave: flush on dispose, no cancellation of an in-flight save, no raw "A task was canceled.", "Bewaard" only after a real save. (c) no ex.Message on any test page or the checkout page.
Build and test; if red or a success criterion can't be met, push, open the PR as draft, stop and report.
Never merge, never deploy, never use rule 123, never push to main or acceptatie, no force-pushes. Don't start file 02.
Report: branch → PR number → status, the migration name, the new endpoints, the dependency cases D and F, the test list, what Dennis must decide (prices per test type, invoice/btw) and anything deferred.
```

## How to run
1. `git fetch origin`. Read:
   - this file, `.cursor/rules/design-system.mdc`, `docs/ROUTES.md`, `docs/release-flow.md`
   - on `origin/docs/mijn-paspoort`: `docs/prompts/mijn-paspoort/00-README.md` (§0, §F, §N, D12), `03-mijn-tests.md`, `07-ontdekkingsreis-wizard-stappen-1-6.md` (scene, lobster, `OnboardingMiniTest`) and `08-ontdekkingsreis-tests-einde-nav.md` (08.1–08.3 levels and the "Weer een laag eraf" moment)
   - `docs/prompts/carriere/00-README.md` §S on `origin/docs/carriere`
   - `docs/prompts/emails/00-README.md` + `02-layout-fundament.md` on `origin/docs/emails`
2. Run the **Dependencies** checks below and note the outcome (it goes into PR 01 for D/F and into PR 02 for all). Re-run A–E before 04 and 05; other stacks may have landed in between.
3. For each file in order:
   1. Read the whole file.
   2. Create its branch:
      - File 01: `git checkout -b cursor/tests-hotfix origin/acceptatie`.
      - File 02: `git checkout -b cursor/tests-2 cursor/tests-hotfix` (or `origin/acceptatie` if PR 01 is merged).
      - Later files: `git checkout -b <branch> <previous branch>` (previous pushed).
   3. Implement **only** that file's scope plus §0.
   4. `dotnet build` + `dotnet test` (unit + bUnit), and the Playwright suites the file names if you can. Everything green and the file's **success criteria** hold.
   5. Small, clear commits. Push `git push -u origin <branch>` (only `cursor/tests-*`). Open **ONE PR into `acceptatie`** with the file's title. The body starts with `Standalone hotfix (not stacked)` (01) or `Stacked on #<prev PR> (<prev branch>)` (02+), then the §0 PR body items.
   6. Note the PR number; go on.
4. **Stop and report** when tests fail and you can't fix them inside the file's scope, when a success criterion can't be met, or when the code contradicts this spec in a way you can't resolve safely: push, open the PR as **draft** with the failure described, don't continue.
5. **Never** merge, deploy, or use rule `123`. **Never** push to `main` or `acceptatie`. No force-pushes.
   - Migrations: each file adds its own migration on top of the previous one; never edit a lower file's migration. If PR 01 is merged before 02 starts, 02's migrations sit on `acceptatie`'s latest.
   - If `acceptatie` moves during the run: don't rebase. Only when a conflict blocks you (or a dependency check flips at a re-check point), `git merge origin/acceptatie` into the current branch (a normal merge commit) and say so in the PR body.

---

## §0. Shared rules (every file)
- **Branches, PRs, stop-on-red:** as above. The diff includes lower PRs until they merge; say which commits are this file's own.
- **Code references** are from `origin/acceptatie` @ `a611db40` (2026-09-29 18:29 CEST). Re-check line numbers before editing. Key places:
  - Pages (`Jobsy.Web/Components/Pages/Candidate/`):
    - `CompetencyTest.razor` (327 lines, `/candidate/competencies`)
    - `CareerTest.razor` (298, `/candidate/career`)
    - `CultureScan.razor` (231, `/candidate/culture` + `/candidate/disc`)
    - `ValuesScan.razor` (230, `/candidate/values`)
    - `DeepAnalysis.razor` (540, `/candidate/deep-analysis` + `/{Kind}`)
    - `DeepAnalysisCheckout.razor` (49, `/candidate/deep-analysis/checkout`)
    - `TestDetail.razor` (857, `/profiel/tests/{TestKey}`; already restyled, stay consistent with it)
  - Shared questionnaire (`Jobsy.Web/Components/Shared/Questionnaire/`): `QuestionnaireShell` (263), `LikertScaleQuestion` (80), `QuestionnairePageBody`, `QuestionnaireAutosave.cs` (148), `QuestionnaireFocus`, `QuestionnaireFlow`
  - Layout: `Components/Layout/MainLayout.razor` (`IsQuestionnairePath` = focus mode, `dir`), `Localization/CultureState.cs` (`IsRightToLeft`)
  - Onboarding: `Components/Pages/Candidate/OnboardingWizard.razor` (`LoadMiniStepAsync` ~L1200–1281), `OnboardingWizardCatalog`, `GratisDnaMergeService`
  - Catalogs (Core `Rules/`):
    - `CompetencyTestCatalog` (25), `CareerTestCatalog` (25), `CulturePersonalityCatalog` (18), `SchwartzValuesCatalog` (25)
    - `DeepAnalysisCatalog` (150/200/150/150; 30 per domain for competence/values, 33–34 career, 12–15 culture)
    - `DeepAnalysisQuestionHelp`, `DeepAnalysisBoosters`
  - API (`Jobsy.Api/Controllers/`):
    - the 4 test controllers: `CandidateCompetenciesController`, `CandidateCareerInterestsController`, `CandidateCultureController`, `CandidateValuesController`
    - `DeepAnalysisController.cs` (190), `AssessmentAdjustmentsController`, `MollieWebhooksController.cs` (129), `SettingsController.cs` (`lobsy-commercial` L93–118)
  - Infrastructure services:
    - `DeepAnalysisService.cs` (628), `AssessmentAdjustmentService`, `AssessmentRetakeService`
    - `MolliePaymentService.cs` (450), `MolliePaymentStub.cs`
    - `TokenPurchaseFulfillmentService`, `TokenPurchaseInvoiceService` (QuestPDF, `LOB-TK-{yyyy}-`), `VatDeclarationService`, `Jobs/TokenCheckoutReconcileHostedService.cs` (105)
  - Core:
    - `Entities/DeepAnalysisCheckout.cs`, `Entities/FlexCommercialSettings.cs`
    - `Rules/TokenVatPricing.cs` (21 %, cents), `Rules/MolliePaymentMethods.cs`, `Rules/AssessmentAdjustmentRules.cs` (`MaxAdjustments = 3`)
    - `Email/EmailLayout.cs`, `Email/TransactionalEmails.cs`
- **Mockups:** branch `docs/tests`, folder `docs/mockups/tests/`. Read with `git show origin/docs/tests:docs/mockups/tests/<file> > /tmp/<file>`. Don't commit mockups to code branches.
  - Desktop 1440×900: `ts-d1-test-intro`, `ts-d2-vraag-likert`, `ts-d3-vraag-uitgebreid`, `ts-d4-test-klaar`, `ts-d5-uitgebreid-aanbod`, `ts-d6-checkout-gelukt`, `ts-d7-checkout-mislukt` (`.png`).
  - Mobile 390 wide (full page): `ts-m1-test-intro`, `ts-m2-vraag-likert`, `ts-m3-vraag-uitgebreid`, `ts-m4-test-klaar`, `ts-m5-uitgebreid-aanbod`, `ts-m6-checkout-bezig`.
  - All data is **Voorbeelddata**; the pill and the dashed "Ontwerpnotitie" boxes are mockup-only. **Where mockup and spec differ, the spec wins.** Known differences:
    - **Nav:** the mockups show Dennis' order (De ontdekkingsreis · Mijn Paspoort · Carrière · Banenkaart · Sollicitaties). **Don't change the nav**; render whatever `BottomNav` gives today.
    - **Price** € 2,99 is an example; render the admin price for that test type (01.6).
    - **Payment methods** "iDEAL · Bancontact · Creditcard" are an example; render `MolliePaymentMethods.PrimaryMethods` (today iDEAL + creditcard).
    - **Minutes** per level ("+ 1 min", "± 4 min", "± 35 min") are examples; they come from `TestDepthRules` (02; the journey 08.3 numbers).
    - **"9 pagina's"** is an example; render the report's real page count from the report definition or leave the number out.
    - **Receipt reference** `LB-DA-7F3K2` is an example; the invoice number is `LOB-KT-{yyyy}-{0000}` (01.8).
    - **"Iets aanpassen? Dat mag nog 3 keer"** (d4/m4) is the live remaining count from 03.
    - **Header** (tagline, language selector, name) is the existing `MainLayout`; don't rebuild it.
- **Design system:** `.cursor/rules/design-system.mdc` + paspoort §0.
  - Tokens only; weights 400/600 (700 only h1); max 2 badges and ONE primary action per card; no decorative emoji; icons only from `NavIcons`/the line set (wave, shell, check, clock, lock, down, pause, file, bars, retry, alert).
  - Tap targets ≥ 44 px; breakpoints 640/900/1024; logical properties only.
  - **Scene exception (paspoort D12):** only the scene layer may use `linear-gradient`/`color-mix()` of the derived scene tokens. Cards, buttons and text stay flat. **No hex colours** in markup or CSS of this stack (guard test in 02).
  - **No inline `style=`** in this stack's components, except CSS custom properties for geometry (`style="--y:…;--size:…"`). Guard test in 02.
  - Gold (`--gold-*`) marks only the paid level ("De bodem") and its offer, never a free action.
- **Motion:** gentle only. The lobster bobs (6 s), bubbles rise, the shard falls **once** (1.2 s) in the finish moment. Under `prefers-reduced-motion: reduce` all animation is off and the still state is complete. No modal pop-ups during a test.
- **Accessibility (all files):**
  - The scene and depth ruler are `aria-hidden="true"`. Every state they show is also in text: the rail (`<ol>`, `aria-current="step"`), "Vraag {i} van {n}", the level line.
  - The question is a `role="radiogroup"` (02). One `aria-live="polite"` region per page for save status and the finish moment, announcing only changes ("Bewaard", "Niet bewaard. Probeer opnieuw", "Weer een laag eraf").
  - Focus: after navigation on the h1 **without** the black box (`.journey-page h1:focus:not(:focus-visible){outline:none}` in this stack's CSS); after an answer on the next question's first radio; keyboard focus keeps the brand outline. Don't change the global rule (listed as deferred in 07).
- **Languages (§L):** below. Every new or touched string in **nl/en/pl/ro/ar**.
- **Strings:** new `Localization/UiStringsTests.cs` (prefixes `TestFlow.`, `TestDepth.`, `TestDone.`, `TestErr.`, `DeepTest.`, `DeepPay.`). Follow the `UiStringsMatch.MergeAll` pattern and register it in `UiStrings.cs`. Question texts stay in their catalogs (06 localizes them). Old keys that become unused are removed in 07, not earlier. `LocalizationParityReportTests` and `LocalizationTests` stay green.
- **Server errors:** every endpoint this stack touches answers with `{ code: "<snake_case>" }` (ProblemDetails extension; keep `message` only for old clients until 07). The UI maps codes to `TestErr.*`/`DeepPay.Err.*`. **The UI never shows `ex.Message`**; unknown errors show `Common.Error` and log the exception. Guard test (01): no `ex.Message` in the razor files listed above.
- **Authorization:** all pages are Candidate only; every endpoint resolves the user from the claims; a foreign checkout id answers 404. `BlazorPageRoleAttributesTests` updated where pages change.
- **Feature flags:** only through the Werkgevers seam (Dependency D). Werkgevers OFF ⇒ no vacancy data, links or counts on test pages; the server returns none.
- **Privacy (AVG):** answers, attempts, adjustments, checkouts and invoices are personal data and are part of the existing export/deletion (`PrivacyDataService`). Invoices are **kept** after account deletion for the legal retention period (7 years, fiscal) with the candidate's name + e-mail only, anonymised otherwise; say so in the privacy text (06) (**flag for Dennis**). No answers ever go to Mollie; Mollie gets the amount, description, redirect/webhook URLs and metadata ids only.
- **CSS:** new `wwwroot/css/features/tests.css` (BEM `test-…`, plus `journey-page` scoping). Link it in `Components/App.razor` (normal list **and** `<noscript>`) with `?v=YYYYMMDD-tests`, add it to `Jobsy.Tests/asset-versions.json` (`AssetVersionGuardTests`). Don't append to `app.css`; `features/questionnaire.css` rules that become unused are removed in 07.
- **Docs/guards when routes or pages change:** `docs/ROUTES.md` (`RoutesDocFreshnessTests`), `Seo/PageSeoCatalog.cs` (all test pages private; `PageSeoTests`), `Help/PageHelpDocs.cs` (`PageHelpDocsTests`), `CHANGELOG.md`.
- **Must NOT touch:**
  - the candidate nav (`RoleNavCatalog`, `BottomNav`, `MainLayout` nav slots), the header, the cookie banner, the MFA pages
  - token prices and the token purchase logic (01 only **extracts** shared Mollie helpers; the token flow's behaviour and tests stay identical)
  - scoring, norms and report content of the tests (only their texts/translations in 06)
  - the paspoort, carriere and emails stacks' files beyond the reuse named in Dependencies
- **PR description:**
  - what changed and why, and the bug ids closed
  - screenshots desktop 1440 + mobile 390 of each changed screen (nl, plus one `ar` RTL screenshot from 04 on)
  - the test list
  - "Strings for native review"
  - "Out of scope / deferred"

## §P. Payment contract (01 builds, 05 restyles)
- **Price source:** `DeepAnalysisPricing.For(settings, kind)` (Core, pure) → incl.-btw euro amount for that test type. `TokenVatPricing.SplitInclVatEuros` gives ex-btw/btw/total cents. The UI always shows the total **incl. btw** plus "inclusief btw".
- **States of a checkout:** `Pending → Paid` (unlock + invoice + mail) or `Pending → Failed | Expired | Cancelled`. `Paid` is set **only** by the fulfillment service after Mollie says `paid` (webhook, reconcile or return-page status check), or by the stub path when allowed.
- **Stub path:** allowed only when `IHostEnvironment.IsDevelopment()` **or** `JobsyAuth:AllowStubPayments=true` (acceptatie). Production has it `false` (`render.yaml` L57; a guard test reads that line). Stub ids start with `stub_deep_` and never reach Mollie.
- **Return page** `/candidate/deep-analysis/checkout?checkoutId={guid}` shows server status only: bezig (`ts-m6`), gelukt (`ts-d6`), niet gelukt (`ts-d7`). The URL never decides the outcome.
- **Waiver:** the candidate must tick "Ik wil meteen beginnen. Ik weet dat ik dan niet meer binnen 14 dagen kan annuleren." before "Naar betalen". The server rejects a checkout without it (`waiver_required`) and stores the time + text version. The receipt mail repeats it (durable medium).
- **Receipt + invoice mail** after paid: what, amount incl. btw (and btw split on the invoice), date/time Europe/Amsterdam, invoice number, the waiver sentence, a link to start. The PDF invoice is attached.

## §Q. Question flow and depth contract (02 builds, 04/05 and the journey use it)
- **`TestDepthRules`** (Core, pure, one source for every count in the product):
  - `Levels(kind)` → `[First(5), Deeper(10), Full(QuestionCount)]` (Cultuur Full = 18), then `Bottom(DeepAnalysisCatalog.QuestionCountFor(kind))` = the paid level.
  - `Reached(kind, answeredCount)` counts **answered questions**, never sets (08.2).
  - `MinutesFor(kind, level)` = the 08.3 numbers (Iets dieper + 2 min, Heel diep + 6 min, Cultuur alle 18 + 4 min). For the uitgebreide test it is `ceil(questions × 12 s / 60)` rounded to 5 minutes (competence 150 ≈ 30 min, career 200 ≈ 40 min).
  - Names (keys `TestDepth.*`): Eerste indruk · Iets dieper · Heel diep · **De bodem**. "Zo laten" stays the journey's *choice* label for stopping (08.3).
  - `Parts(kind)` for the uitgebreide test = **5 parts** (05): competence and values = their 5 domains of 30. Career = 5 parts of 40 and culture = 5 parts of 30; each part groups whole domains in catalog order, and a domain is never split unless one domain alone is bigger than a part. Ids don't change; only the display order does.
- **`TestQuestionFlow.razor`** (`Components/Shared/Questionnaire/`): one question at a time. It is the only question renderer for the 4 free pages, the uitgebreide test and `OnboardingMiniTest` (Dependency B).
  - Parameters: `Kind`, `Questions` (id, text, optional example), `Answers` (from `QuestionnaireAutosave`), `StartAt`, `Target` (a `TestDepthRules` level), `ScaleLabels` (one set: "Past niet" … "Past heel goed"), `OnLevelReached`, `OnFinished`, `Compact` (journey).
  - Header: "Vraag {i} van {target}" + a segment bar with level ticks; the statement in quotes without an id prefix; the optional "Voorbeeld uit de praktijk" `<details>`.
  - The "Beantwoord ({n})" list holds the last 3 answers (mobile 1) with "Aanpassen" for going back.
  - Footer: Terug · "Later verder" · Volgende. Volgende is disabled until answered; answering auto-advances after 250 ms, or at once under reduced motion.
- **Scene:** `TestDiveScene` = `JourneyLobster` (Dependency A) on a depth ruler with ticks at the levels, and the scene tokens deepening with depth (`scene(depth)` in `build.py`: depth 8 at level 1, 9 at level 2/3, 10 at De bodem). The lobster keeps the candidate's journey plate state; finishing a test level **doesn't** shed extra plates (D2).

## §L. Languages (every file)
- nl is the source and is **final as written in these files** (B1: short sentences, "je", no jargon, no "AI"/"DNA"/"Quick-Scan" in headings).
- en/pl/ro/ar are written by you in the same plain B1 register. Keep placeholders (`{0}`) and **Lobsy** untranslated.
- Each file lists its new keys in the PR body under "Strings for native review" (07 compiles one list). **pl/ro/ar texts are machine-quality until a native speaker checks them** (flag for Dennis). Test questions are psychometric items: a translation must keep the meaning and the direction (reverse-keyed stays reverse-keyed); 06 adds a reviewer note column for that.
- **RTL (`ar`):** `MainLayout` already sets `dir="rtl"`. Use logical properties only. Chevrons flip (`[dir="rtl"] .i-chevron{transform:scaleX(-1)}`). The Likert scale **keeps 1 → 5 in reading direction** (1 at inline-start, so on the right in RTL), with the anchor labels following. The depth ruler moves to inline-end. Numbers stay Western digits; dates/times use the current culture with the Europe/Amsterdam zone.

## Decisions (Dennis "Akkoord" 30-09 on the six defaults; extra defaults marked *extra*)
- **D1. Level names and one product name.** Eerste indruk 5 · Iets dieper 10 · Heel diep 25 (Cultuur alle 18) = the free test; **De bodem · Uitgebreide test** (150; Beroepen 200) = paid. "Uitgebreide test" is the only product name everywhere (UI, mail, invoice, PDF title). "diepte-analyse", "Diepteanalyse", "Uitgebreide competentie-analyse", "officiële" and "Quick-Scan" disappear from candidate copy. *(Dennis, 30-09)*
- **D2. Plates stay tied to the 10 journey steps.** "Weer een laag eraf" is the finish moment of each depth level on the full pages too (a card shard labelled with the level); the lobster doesn't lose extra plates. *(Dennis, 30-09)*
- **D3. One question at a time** on the full pages and the uitgebreide test, with the journey's component (§Q). The uitgebreide test comes in 5 parts with a pause point after each; motivation is inline text, never a modal. *(Dennis, 30-09)*
- **D4. Payment.**
  - Real Mollie through the token path, with an order summary, "incl. btw", the waiver checkbox and a receipt + invoice mail.
  - Stubs only in Development/acceptatie. "Veilig betalen via Mollie" only when real.
  - The free option comes first on TestDetail.
  - **Price = admin value per test type**, default € 2,99 (today's code value). *(Dennis, 30-09; **flag for Dennis:** set the four prices; invoice/btw handling below)*
- **D5. Changes to a finished test.** `MaxAdjustments = 3` is enforced on the save endpoints once a test is completed. Every save that changes a completed test's answers **and** is finished again counts as **one** adjustment (per finish, not per answer). History is recorded and attempts are completed. *(Dennis, 30-09)*
- **D6. Languages.** All test questions, examples, labels, messages, the uitgebreide test, the receipt mail and the invoice in nl/en/pl/ro/ar; pl/ro/ar machine-quality until native review. *(Dennis, 30-09; **flag for Dennis:** native review)*
- **D7 (*extra*). Finish target.** "Afronden" always ends in the finish moment and then `/profiel/tests/{key}`. Back links go to `/profiel/tests/{key}` too (when the paspoort flag is OFF: `/candidate/profile`). Career matches only as bands (kandidaat-banen labels) and only with Werkgevers ON; no percentages.
- **D8 (*extra*). Invoice and btw.**
  - A new consumer invoice series `LOB-KT-{yyyy}-{0000}`, 21 % NL btw on every candidate purchase, and a PDF in the style of the token invoice.
  - The invoices count in the btw declaration totals as their own line "Kandidaat-aankopen" (`VatDeclarationService`).
  - The VAT buffer queue (`QueueForInvoiceAsync`) gets them too.
  - **Flag for Dennis/accountant:** B2C digital services to consumers living in another EU country fall under the OSS rules (the customer's btw rate). This stack charges 21 % for everyone and stores the candidate's country if known, so a later OSS fix is possible.
- **D9 (*extra*). Pending checkouts.** One open checkout per candidate and test type; a new one cancels the old one at Mollie (best effort) and locally. Pending expires after 48 h (`Expired`). A paid checkout for an already unlocked test is fulfilled idempotently and flagged for a manual refund in the admin log (no automatic refund).
- **D10 (*extra*). Price validation.** Admin price per type: > € 0,00 and ≤ € 100,00, 2 decimals, incl. btw. A price change never touches existing checkouts (the amount is stored on the checkout).
- **D11 (*extra*). Prerender** off for all test pages (the interactive state and autosave need the circuit; it matches Culture/Values/Deep today). The loading state uses the card skeleton, not a spinner page.
- **D12 (*extra*). Consent gate.** Without current test consent or (for minors) parental consent, the test pages show the intro with a consent card instead of "Begin". No question is shown and no save is attempted.

## Dependencies (check before 01 (D, F) and 02 (all); re-check A–E before 04 and 05; say in the PR which case applied)
- **A. Ontdekkingsreis scene + lobster + tokens** (`docs/mijn-paspoort` 07, or `docs/carriere` 02 if that landed first). Check: `git grep -n "JourneyLobster\|JourneyScene\|CareerClimbScene" origin/acceptatie -- Jobsy.Web` and `git ls-tree origin/acceptatie Jobsy.Web/wwwroot/css/features/ontdekkingsreis.css Jobsy.Web/wwwroot/css/features/journey-tokens.css`. At `a611db40`: **absent**.
  - **Present (either stack):** reuse `JourneyLobster` and the scene tokens from whichever file defines them (`ontdekkingsreis.css` or `journey-tokens.css`). `TestDiveScene` is new (02) and uses only those tokens. Don't copy tokens.
  - **Absent:** 02 builds `Components/Candidate/Journey/JourneyLobster.razor` exactly per paspoort 07 (same name, parameters, plate paths from `build.py` `PLATES`/`BODY`/`CRACKS`/`SHARD`, mascot `mascot-256.webp`) and the scene tokens in a **separate** `wwwroot/css/features/journey-tokens.css`, verbatim, the same contract as carriere §S. Say in PR 02 that paspoort 07 and carriere 02 must reuse both. Never branch from the paspoort or carriere branches.
- **A2. `LobsyBubble`** (paspoort 02). Check: `git grep -n "LobsyBubble" origin/acceptatie -- Jobsy.Web`. At `a611db40`: absent.
  - **Present:** reuse it (`Variant="Scene"`; add it if missing).
  - **Absent:** 02 builds `Components/Shared/LobsyBubble.razor` per paspoort 02 (`Variant` = `Inline | Scene`) and says so in the PR.
- **B. Ontdekkingsreis 07/08 mini-tests and levels.** Check: `git grep -n "OnboardingMiniTest\|DeeperQuestionIds\|class TestDepthRules" origin/acceptatie -- Jobsy.Web Jobsy.Core`. At `a611db40`: absent (the mini-test is inline in `OnboardingWizard.razor`).
  - **`OnboardingMiniTest` present (07 landed):** 02 makes it render through `TestQuestionFlow` (`Compact="true"`), keeping its skip logic, GratisDna merge and save calls. Its tests stay green.
  - **`DeeperQuestionIds` present (08 landed):** `TestDepthRules` reads its level sets from `OnboardingWizardCatalog` (no second copy). If 08 defined its own level numbers or minutes, move them into `TestDepthRules` and make 08's code call it; its tests stay green.
  - **Absent:** 02 adds `TestDepthRules` with the 08.2 numbers and the `…DeeperQuestionIds` sets as specified in 08.2 (same names, in `OnboardingWizardCatalog`), plus a doc comment "paspoort 07/08 must use TestDepthRules and TestQuestionFlow". The old wizard's inline mini-test is **not** changed except that it renders through `TestQuestionFlow` (`Compact`), so both share one component and one scale.
- **C. Paspoort Mijn tests (03).** Check: `git grep -n "class TestsOverviewBuilder\|PassportTestsTab" origin/acceptatie -- Jobsy.Web`.
  - **Present:** its labels "Quick-Scan / Uitgebreid / Rapport" become `TestDepth.*` names (D1) and its progress reads `TestDepthRules`; its tests stay green.
  - **Absent:** only `TestsOverviewPanel` and `TestDetail` are aligned (04/05).
- **D. Werkgevers switch** (paspoort 01). Check: `git grep -n "interface IFeatureFlags\|class RequiresFeatureAttribute\|interface ICareerEmployerGate" origin/acceptatie -- Jobsy.Core Jobsy.Web Jobsy.Api`. At `a611db40`: absent.
  - **`IFeatureFlags` present:** use `IsEnabled(PlatformFeature.Employers)` for every gate here.
  - **`ICareerEmployerGate` present (carriere landed first):** reuse it (don't add a second seam).
  - **Absent:** add the same one-method seam as carriere D, `ICareerEmployerGate` (Web + Api, returns **true** today, `// replace with IFeatureFlags (paspoort 01)`), so paspoort 01 changes one class for both stacks. Tests cover both values.
- **E. Kandidaat-banen fit bands** (`docs/kandidaat-banen`). Check: `git grep -n "class CandidateFitDisplay\|class CandidateFitGate\|FitBand" origin/acceptatie -- Jobsy.Core Jobsy.Api`.
  - **Present:** the career matches block (04) shows vacancy bands from `CandidateFitDisplay`, only when `CandidateFitGate.IsOpen`.
  - **Absent:** the block shows at most 3 vacancy titles without any band or percentage and the link "Bekijk passende vacatures" to the banenkaart; no count.
- **F. E-mail layout** (`docs/emails` 02). Check: `git grep -n "class EmailRenderer\|class EmailTemplateRegistry\|interface ITransactionalMailer" origin/acceptatie -- Jobsy.Core Jobsy.Infrastructure`. At `a611db40`: absent (`EmailLayout.Wrap` + `TransactionalEmails` exist).
  - **Present:** the receipt mail (01) is a registry template `deep_test_receipt` (kind Transactional, candidate) sent through `ITransactionalMailer`, with the PDF invoice attached.
  - **Absent:** add `TransactionalEmails.DeepTestReceipt(…)` using today's `EmailLayout.Wrap`/`FactCard`/`PrimaryButton`, sent through `IEmailService`, with a `// TODO(emails-02): move to the registry` at one seam. Say in PR 01 that emails 02 must migrate it (key `deep_test_receipt`).
- **G. Nav add-on** (Dennis' order, separate). Nothing to check: this stack never edits the nav.
- **Landing order:** 01 as soon as possible. The rest in any order after it; every case above has a fallback. Recommended after paspoort 07/08 when they are close to merging. Never branch from an unmerged branch of another stack.

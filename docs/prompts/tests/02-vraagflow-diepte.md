# 02. One question flow and one set of depth levels (shared with the ontdekkingsreis), consent gate, save error codes, accessible Likert

> **Rules (repeated in every file):**
> - Never merge, never deploy, never use rule `123` (`.cursor/rules/shortcut-123.mdc`).
> - Never push to `main` or `acceptatie`; push only the current file's `cursor/tests-*` branch; no force-push.
> - ONE PR per file into `acceptatie` (01 standalone, 02+ stacked).
> - Red build/tests or an unmet success criterion: push, open the PR as **draft**, stop and report. Don't start the next file.
> - Nothing unlocks a paid test without a **paid** status from Mollie (or the stub path, only in Development or with `JobsyAuth:AllowStubPayments=true`). The UI never shows `ex.Message`.
> - Don't change the candidate nav (order, items, labels). Dennis' order is a separate add-on.

| | |
|---|---|
| Branch | `cursor/tests-2` from `cursor/tests-hotfix` (or `cursor/tests-hotfix-b` if 01 was split; `origin/acceptatie` if PR 01 is merged) |
| PR title | `Tests 02: shared TestQuestionFlow + TestDepthRules (with the ontdekkingsreis), consent gate, save error codes, accessible Likert radiogroup` |
| Body starts with | `Stacked on #<PR 01> (cursor/tests-hotfix)` + the Dependencies outcome A–G (which case applied) |
| Mockups | `ts-d2`/`ts-m2` (question screen, component level only; the page shell is 04), `ts-d3`/`ts-m3` (the example `<details>`) |
| Split if too big | `02a` = §1–§3 (rules, component, a11y); `02b` = §4–§7 (scene/lobster/bubble foundation, consent gate, codes, prerender, journey wiring) |

**Goal:** one question component and one rule set for levels and counts, used by the full test pages, the uitgebreide test and the ontdekkingsreis steps 7–10. The pages keep their current look in this file except that questions come **one at a time** (the journey-style shell is 04). Nobody answers a question that can't be saved.

Closes: Q1 (free pages), Q2, Q3, Q4 (numbering, contrast), C1, X1, X2, X3.

## 1. `TestDepthRules` (Core, pure; §Q)
- `Jobsy.Core/Rules/TestDepthRules.cs`:
  - `enum TestDepthLevel { First, Deeper, Full, Bottom }`
  - `Levels(AssessmentKind kind)` → `[(First, 5), (Deeper, 10), (Full, FullCount(kind))]`, where `FullCount` = `CompetencyTestCatalog`/`CareerTestCatalog`/`SchwartzValuesCatalog.QuestionCount` (25) and `CulturePersonalityCatalog.QuestionCount` (18)
  - `Bottom(kind)` = `DeepAnalysisCatalog.QuestionCountFor(kind)` (150 / 200 / 150 / 150)
  - `Reached(kind, answeredCount)` = the highest free level with `answered ≥ count` (null below 5)
  - `Next(kind, answeredCount)`
  - `MinutesFor(kind, level)` (§Q numbers)
  - `QuestionIdsFor(kind, level)`:
    - First = the mini set (`OnboardingWizardCatalog` mini ids)
    - Deeper = mini + `…DeeperQuestionIds` (Dependency B)
    - Full = every catalog id
    - order: the level's own ids first, then the rest in catalog order
  - `Parts(kind)` for the uitgebreide test (§Q; used in 05)
- Unit tests:
  - counts per kind; Cultuur Full 18; Bottom 200 for career
  - `Reached` at 4/5/9/10/24/25 and culture 17/18
  - the sets have no duplicates, contain only valid ids, and Beroepen's Deeper set includes Artistic (08.2)
  - minutes values
  - `Parts` sizes (5 parts; 30/40/30/30) with domains not split unless they must be
- **Dependency B present:** 08's numbers move here and 08's code calls `TestDepthRules` (no second copy). Say in the PR which lines moved.

## 2. `TestQuestionFlow.razor` (§Q)
- `Components/Shared/Questionnaire/TestQuestionFlow.razor` + `.razor.css`-free (styles in `features/tests.css`):
  - Parameters as in §Q. It owns only presentation and navigation; saving stays in `QuestionnaireAutosave` (01) through the `Autosave` parameter.
  - Header: "Vraag {i} van {target}" (`TestFlow.QuestionOf`) and a segment bar. The level segments `First`, `Deeper` and `Full` get ticks; answered = `--brand`, current level range = `--accent-soft`, beyond = `--pearl`. For > 50 questions the bar uses 25 blocks.
  - The level line under the bar: check icon + "**Eerste indruk** gedaan · nog {n} tot **Iets dieper**" (icon from the line set, no emoji; `TestDepth.*` names).
  - Statement in quotes **without** the id prefix (strip leading `^\d+\.\s*` in the catalog getter, tested). Optional `Example` in `<details class="test-example">` "Voorbeeld uit de praktijk" (open by default on desktop, closed on mobile).
  - `LikertRadioGroup` (§3) with the **one** label set "Past niet" … "Past heel goed" (`TestFlow.Scale.Low/High`, replacing both `Questionnaire.Likert.*` and `Onboarding.Likert.*` usage; the old keys stay until 07).
  - "Beantwoord ({n})" with the last 3 answered statements + value (mobile 1) and "Aanpassen" (jumps back, keeps the target).
  - Footer: "Terug" (previous question), "Later verder" (flush, then `OnLater`), primary "Volgende" (disabled until answered). On the target question the primary says "Afronden" and calls `OnFinished` after a flush.
  - Answering auto-advances after 250 ms (reduced motion: at once). Never auto-advance past the target.
  - `OnLevelReached(level)` fires once per level crossing (04 shows the finish moment; the journey shows its own).
  - `Compact="true"` (journey): no "Beantwoord" list, no footer "Later verder" (the wizard owns navigation).
- `QuestionnaireShell`/`QuestionnairePageBody` long-list rendering is no longer used by the four free pages after this file (they render `TestQuestionFlow` in their current page chrome). `CompetencyTest` and `DeepAnalysis` stop duplicating markup. Remove dead code only in 07.

## 3. Accessibility of the question (X1, X2, X3)
- `LikertRadioGroup.razor` replaces the button set in `LikertScaleQuestion`:
  - `<fieldset role="radiogroup" aria-labelledby="{statementId}" aria-describedby="{scaleHintId}">` with 5 `<input type="radio">` + `<label>` pairs styled as the 56 px boxes
  - accessible names "1, past niet" … "5, past heel goed" (`TestFlow.Scale.Aria.{1..5}`)
  - **roving tabindex:** one tab stop; arrow keys move and select (Left/Up = previous, Right/Down = next; mirrored in RTL by the browser's native radio behaviour); Home/End; Space selects
  - the visible anchor labels are the `aria-describedby` hint
- **Focus kept (X2):** after an answer and the auto-advance, focus moves to the next question's radiogroup (the checked or first radio) via `QuestionnaireFocus`. After "Aanpassen" it goes to that question; after "Terug" to the previous one. Never to `<body>`. bUnit + Playwright check `document.activeElement` after each step.
- **Contrast:** no dimmed next question anymore (one at a time). Muted texts use `--muted` on `--surface` (≥ 4.5:1, checked in the axe run).
- **Title focus (X3):** `.journey-page h1:focus:not(:focus-visible){outline:none}` in `features/tests.css`; the test pages get the `journey-page` class on their root. Keyboard focus keeps the brand outline.
- Live region: one `aria-live="polite"` per page for save status. It announces only changes to `Saved` after a failure, and `Failed` ("Je laatste antwoord is nog niet bewaard"). There is no announcement for every routine save (less chatty).

## 4. Scene, lobster and bubble foundation (Dependencies A, A2)
- Per Dependency A/A2: reuse or build `JourneyLobster`, the scene tokens and `LobsyBubble`.
- Add `Components/Candidate/Tests/TestDiveScene.razor`:
  - background layer: depth ruler with ticks per `TestDepthRules` level, the lobster at the current depth, rays/bubbles; `aria-hidden="true"`
  - parameters: `Kind`, `Answered`, `Target`, `Deep` (bool), `Mobile`, `Celebrate`, `FallingShard`
  - geometry from `build.py` `depth_zone()` (desktop zone right of the card; mobile band 390×170 above the card)
  - scene depth token per §Q
  - RTL: the SVG mirrors (`scaleX(-1)`), the ruler sits at inline-end, labels are HTML with logical positions
  - lobster plate state = the candidate's journey state (`JourneyLobster` `platesShed` from the journey progress when available; else 10 + `newShell` for candidates who finished onboarding, 0 otherwise)
- Not placed on the pages yet (04 does). A bUnit render test and the no-hex/no-inline-style guard tests (§0) are added here.

## 5. Consent gate (C1, D12)
- Web `TestConsentGate` (component): loads the candidate's consent state once (existing `me` endpoint fields for `CanUseCandidateFeatures` and `HasCurrentTestAiConsent`; add them to the DTO if missing).
  - **Missing test consent:** in place of the question flow, a card "Eerst even toestemming" + one sentence on what the tests do with answers (reuse the existing consent copy keys) + primary "Toestemming geven" (the existing consent action/page) + link "Waarom vragen we dit?".
  - **Missing parental consent:** "Je ouder of verzorger moet eerst toestemming geven." + the existing parental-consent status/resend link.
  - No question is rendered and `QuestionnaireAutosave` is not created while the gate is closed.
- Used by the four free pages and `DeepAnalysis` (and the order dialog of 01 disables "Naar betalen" with the same text).

## 6. Error codes and prerender (D11)
- The codes of 01.12 are now complete on all test save/complete endpoints (`unknown_question`, `invalid_answer` 1–5, consent codes); the UI maps all of them. API tests per controller.
- `@rendermode InteractiveServer(prerender: false)` on `CompetencyTest` and `CareerTest` (now consistent with the others). Loading = the card skeleton (`test-skeleton`), no bare spinner page. The SEO catalog keeps them private.

## 7. Journey wiring (Dependency B)
- **`OnboardingMiniTest` present:** it renders `TestQuestionFlow Compact` with its current ids, skip logic, GratisDna merge and save calls; its tests stay green. Screenshot in the PR.
- **Absent:** `OnboardingWizard.razor`'s inline mini-test (`LoadMiniStepAsync` path) renders `TestQuestionFlow Compact` instead of its own Likert markup, with the same ids and saves; nothing else in the wizard changes. The wizard now uses the shared scale labels ("Past niet … Past heel goed", unchanged for the journey).
- Either way: answers from the journey and the full pages are the same rows, so a level reached in one place is reached in the other (`TestDepthRules.Reached` over the stored answers). Test: 5 answers from the mini-test ⇒ the full Beroepen page starts at question 6.

## Tests
- Unit (§1), bUnit:
  - `TestQuestionFlow`: counter, level line, auto-advance, "Afronden" on target, `OnLevelReached` once, "Aanpassen", compact mode
  - `LikertRadioGroup`: roles, names, roving tabindex, arrow keys, RTL
  - consent gate states
  - scene renders `aria-hidden`
- API: error codes on each test controller.
- Guards: no hex / no inline style in this stack's components; no `ex.Message` (from 01).
- Playwright (if you can): keyboard-only run through 3 questions of `/candidate/career` (Tab once into the group, arrows, focus lands on the next group); axe on the question screen desktop + mobile.

## Success criteria
- Every free test page shows **one question at a time**, with the same component and the same labels as the onboarding mini-test.
- The scale is one radiogroup with one tab stop and arrow keys; focus is never lost after an answer.
- A candidate without consent sees the consent card and no questions; no save request is sent.
- All counts on these pages come from `TestDepthRules`; there is no hardcoded 5/10/25/18/150/200 in Web code (grep in the PR).
- Competency/Career no longer prerender; no black title box after navigation.

## Done → next
Push, open the PR (stacked on 01), note the number, go to `03-aanpassingen-limiet.md`.

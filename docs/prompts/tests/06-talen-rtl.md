# 06. Every test in five languages (incl. the uitgebreide test, receipt and invoice) + RTL pass

> **Rules (repeated in every file):**
> - Never merge, never deploy, never use rule `123` (`.cursor/rules/shortcut-123.mdc`).
> - Never push to `main` or `acceptatie`; push only the current file's `cursor/tests-*` branch; no force-push.
> - ONE PR per file into `acceptatie` (01 standalone, 02+ stacked).
> - Red build/tests or an unmet success criterion: push, open the PR as **draft**, stop and report. Don't start the next file.
> - Nothing unlocks a paid test without a **paid** status from Mollie (or the stub path, only in Development or with `JobsyAuth:AllowStubPayments=true`). The UI never shows `ex.Message`.
> - Don't change the candidate nav (order, items, labels). Dennis' order is a separate add-on.

| | |
|---|---|
| Branch | `cursor/tests-6` from `cursor/tests-5` (or the last 05 sub-branch) |
| PR title | `Tests 06: all test questions, examples, labels, messages, receipt and invoice in nl/en/pl/ro/ar; RTL pass` |
| Body starts with | `Stacked on #<PR 05> (cursor/tests-5)` |
| Mockups | all `ts-*` (layout must hold in every language; `ar` mirrored) |
| Split if too big | `06a` = §1–§2 (free tests + UI strings); `06b` = §3–§5 (uitgebreide test catalogs, server texts, mail/invoice, RTL) |

**Goal:** a Polish, Romanian or Arabic speaker can do every test, pay and read the receipt in their own language. English is complete. Nothing falls back to Dutch silently.

Closes: L1, L2, L3.

## 1. Free tests (L1)
- `UiStringsCompetencies`, `UiStringsCulture`, `UiStringsValues` (and the career strings): stop merging the Dutch dictionary into pl/ro/ar. Write real `Pl()`, `Ro()`, `Ar()` dictionaries for every key they contain.
- Culture (18) and values (25) question texts get real `en` translations; `En()` no longer copies `Nl()` for `Q*` keys.
- The competency and career items: verify all five languages exist for every question; fill the gaps.
- **Psychometric care (§L):**
  - Keep meaning, intensity and direction; reverse-keyed items stay reverse-keyed.
  - No idioms; B1.
  - Add `docs/i18n/tests-items-review.csv` (key, nl, en, pl, ro, ar, `reverse` flag, reviewer note) for native review (**flag for Dennis**).
- A test `TestItemTranslationParityTests`: every item key exists in all five languages, is non-empty, differs from nl for en/pl/ro/ar (except allow-listed proper nouns), and keeps placeholders.

## 2. UI strings
- All `TestFlow.*`, `TestDepth.*`, `TestDone.*`, `TestErr.*`, `DeepTest.*`, `DeepPay.*` keys from 01–05: pl/ro/ar written (they were en copies), en reviewed.
- The level names in every language keep the diving image (en: First look · A bit deeper · Very deep · The bottom; pl/ro/ar equivalents chosen for plain meaning, listed for review).
- The one scale label set in five languages ("Past niet … Past heel goed"), identical in the journey and the tests.
- `docs/i18n/untranslated-for-translators.csv` no longer lists any test key for pl/ro/ar.

## 3. The uitgebreide test (L2)
- `DeepAnalysisCatalog` items (`PromptNl`, `ExampleNl`): add localized prompt/example per language for all 650 items (150 + 200 + 150 + 150). Store them in `Jobsy.Core/Rules/DeepItems/{lang}/…` resource classes or JSON embedded resources (choose one, say why). `DeepAnalysisQuestion` gets `Prompt(lang)`/`Example(lang)` with an explicit nl fallback that is **logged** (never silent) and counted in a test that must be zero for shipped languages.
- Part and domain labels (`DeepAnalysisQuestionHelp.DomainLabel`, `DimensionLabels`) localized.
- `DeepTestMotivation` keys in five languages.
- The same review CSV as §1 (`tests-deep-items-review.csv`).

## 4. Server texts, mail and invoice
- All messages the test and payment endpoints still send (`message` next to `code`) become neutral English for logs only. Since 01 the UI renders codes; add a test that no candidate-facing razor reads `message`.
- Receipt mail (01.9) and PDF invoice (01.8) in all five languages, chosen by the checkout's `Locale`. The invoice keeps the legal Dutch fiscal terms next to the translation where required ("BTW / VAT").
  - Dependency F present ⇒ the registry template's localized strings.
  - Absent ⇒ `TransactionalEmails` localized texts.
- Report PDF: the report title uses "Uitgebreide test" in every language (content translation of the report itself is out of scope unless it already has a language parameter; the existing `lang` query of `report` stays).

## 5. RTL (`ar`) pass (L3)
- Check every test screen at 390 and 1440 in `ar`:
  - `dir="rtl"`; the rail on the inline-start side; the scene mirrored and the ruler at inline-end
  - chevrons flipped; the Likert keeps 1 → 5 in reading direction with the labels following (§L)
  - the depth cards, receipt `<dl>` and price block mirrored with logical properties
  - no horizontal overflow; the primary button at inline-end
  - numbers Western digits; dates via the culture with Europe/Amsterdam
- Fix any physical property (`left`/`right`/`margin-left`…) in `features/tests.css` (guard test: none allowed).
- Arabic line length: the bubble and statements don't clip (Playwright screenshot check).

## Tests
- Parity tests above; the existing `LocalizationParityReportTests`/`LocalizationTests` green.
- bUnit: each page renders with `ar`/`pl` without Dutch fallback for test keys (snapshot of visible text contains no nl-only marker words from a small list).
- Playwright (if you can): `ar` and `pl` runs of Beroepen intro → 3 questions → finish, and the offer page; screenshots.

## Success criteria
- No test question, example, label, message, mail or invoice is Dutch in en/pl/ro/ar (parity tests zero).
- `ar` screens have no overflow and a correct mirrored layout at 390/1440.
- The review CSVs exist and are linked in the PR under "Strings for native review" (**flag for Dennis**).

## Done → next
Push, open the PR (stacked on 05), note the number, go to `07-e2e-rapport.md`.

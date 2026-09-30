# 07. Playwright E2E, docs, cleanup, stack-end report

> **Rules (repeated in every file):**
> - Never merge, never deploy, never use rule `123` (`.cursor/rules/shortcut-123.mdc`).
> - Never push to `main` or `acceptatie`; push only the current file's `cursor/tests-*` branch; no force-push.
> - ONE PR per file into `acceptatie` (01 standalone, 02+ stacked).
> - Red build/tests or an unmet success criterion: push, open the PR as **draft**, stop and report. Don't start the next file.
> - Nothing unlocks a paid test without a **paid** status from Mollie (or the stub path, only in Development or with `JobsyAuth:AllowStubPayments=true`). The UI never shows `ex.Message`.
> - Don't change the candidate nav (order, items, labels). Dennis' order is a separate add-on.

| | |
|---|---|
| Branch | `cursor/tests-7` from `cursor/tests-6` (or the last 06 sub-branch) |
| PR title | `Tests 07: Playwright E2E (desktop + mobile, 5 languages, RTL, reduced motion, Werkgevers OFF, payment states), cleanup, docs, stack-end report` |
| Body starts with | `Stacked on #<PR 06> (cursor/tests-6)` |
| Mockups | all `ts-*` for the visual comparison |

## 1. Playwright E2E
- New `Jobsy.Tests/CandidateTestsPlaywrightTests.cs`, same conventions as `Acc2709PlaywrightTests`: `[Collection("PlaywrightSmoke")]`, soft-skip without `JOBSY_E2E_BASE_URL`.
- Run against a local stack with seed + stub payments (`AllowStubPayments=true`). The real Mollie path is covered by the API tests with a fake Mollie handler (01); say so in the PR.
- **Free test, desktop 1440 + mobile 390:** Beroepen with 5 answers from the journey → intro "Ga verder bij vraag 6" → Heel diep → keyboard-only answers (Tab once, arrows, Space) → finish moment → "Bekijk je uitslag" on `/profiel/tests/career`.
- **Autosave:** answer then leave via the header within 0.5 s → reopen → the answer is there. Answer 5 questions fast → no error banner, "Bewaard" after the last save.
- **Consent:** a seeded candidate without test consent → consent card, no radiogroup, no PUT request.
- **Limit:** complete → edit + finish 3 times → the 4th "Afronden" shows the limit text; TestDetail says "Nog 0 van de 3 keer".
- **Uitgebreide test:** TestDetail (free primary first) → offer (price incl. btw from admin, waiver required) → stub pay → paid state + receipt data → "Begin met vraag 1" → DOM holds one radiogroup → after 30 answers the pause point → "Later verder" → resume at 31. Invalid kind → friendly state.
- **Payment states:** failed and checking states via seeded checkouts; the unknown id state; opening an old `?paymentId=` link never unlocks.
- **Languages:** `en`, `pl`, `ro`, `ar` runs of intro + 3 questions + offer; `ar` asserts `dir=rtl`, no horizontal overflow at 390, the primary at inline-end.
- **Reduced motion:** `reducedMotion: 'reduce'`: no running animations (`getAnimations()` empty), the shard is shown lying.
- **Werkgevers OFF:** no vacancy line on the Beroepen finish, and the API returns none.
- **axe:** intro, question, finish, offer, checkout paid, desktop + mobile: no serious/critical issues.
- Screenshots of every state next to the mockup names in the PR (nl + one `ar`).

## 2. Cleanup
- Remove:
  - the unused `Questionnaire.Likert.*`/`Onboarding.Likert.*` keys
  - the `DeepAnalysisBoosters` keys/class and `IDeepAnalysisService.FormatUpsellCopy` leftovers
  - the obsolete `checkout/{paymentId}/complete` shim and any other `[Obsolete]` leftovers of this stack
  - `FlexCommercialSettings.DeepAnalysisPriceEuro` (migration `DropLegacyDeepAnalysisPrice`, after confirming no reader)
  - the dead long-list rendering in `QuestionnaireShell`/`QuestionnairePageBody` if nothing uses it
  - unused rules in `features/questionnaire.css` (+ `app.min.css` if it bundles them)
- The `message` field next to `code` in test/payment API errors: remove where no client reads it (grep the Web client), else list it.
- Old strings with "diepte-analyse", "Diepteanalyse", "Quick-Scan", "officiële": none left in candidate copy (string test).

## 3. Docs
- `docs/ROUTES.md`, `Help/PageHelpDocs.cs`, `Seo/PageSeoCatalog.cs` final.
- `CHANGELOG.md` entry.
- `docs/TESTSCENARIOS_PER_ROL.md` + `docs/testscenarios-per-rol.csv`: candidate scenarios for the flows in §1 (nl), in the file's existing format.
- `docs/FUNCTIONELE_SPECIFICATIES_LOBSY_PLATFORM.md`: update the tests / deep analysis sections if they exist (levels, one product name, payment, limit).
- `docs/payments.md` (new or the existing payments doc):
  - the deep test payment flow: create → Mollie → webhook/reconcile/return status → fulfill, with the states
  - stub rules, invoice series `LOB-KT`, the btw line, refunds by hand
- `docs/i18n`: the compiled "Strings for native review" list of 01–06 + the two review CSVs.

## 4. Stack-end report (`docs/reports/tests-stack-end.md`)
- File → branch → PR → status.
- Dependency cases per re-check point.
- Bug ids closed (README table), with the test that covers each.
- Deferred:
  - the global h1 focus rule
  - OSS btw for EU consumers
  - automatic refunds
  - the report PDF content translation, if not done
  - anything else
- **For Dennis:**
  - set the four prices in Admin (today all € 2,99)
  - confirm the invoice/btw handling (D8) with the accountant
  - native review pl/ro/ar (the CSVs)
  - the "Werkgevers zien…" sentence choice (04 §2)
  - invoice retention after account deletion (§0 privacy)

## Success criteria
- The suite is green locally (or the parts that need infrastructure are marked and covered by API tests, listed).
- The cleanup leaves no unused keys/classes of this stack (analyzer/grep in the PR).
- The report exists and names every open decision for Dennis.

## Done
Push, open the PR (stacked on 06), and report file → branch → PR → status for the whole stack.

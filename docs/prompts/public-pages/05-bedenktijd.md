# 05. Bedenktijd: the € 2,99 uitgebreide test can't start without the waiver checkbox (align with docs/tests 01, or guard + follow-up) ⚖️

Read `00-README.md` first (D7, Dependency C) and `04-voorwaarden.md` §04.7. Branch `cursor/public-pages-5` from `cursor/public-pages-4`.

> **Rules (same as README §0, repeated on purpose):**
> - Branch `cursor/public-pages-5` from `cursor/public-pages-4` (stacked). ONE PR into `acceptatie`; the body starts with `Stacked on #<prev PR> (cursor/public-pages-4)`.
> - Never merge, never deploy, never use rule `123` (`.cursor/rules/shortcut-123.mdc`).
> - Never push to `main` or `acceptatie`; push only `cursor/public-pages-5`; no force-push.
> - Red tests or an unmet success criterion: push, open the PR as **draft**, stop and report. Don't start the next file.
> - Never show `ex.Message`, stack traces, placeholders or internal ids to visitors.
> - Don't touch Mollie/checkout internals or `FlexCommercialSettings` semantics. `docs/tests` 01 owns the payment flow; this file only aligns and guards.

| | |
|---|---|
| Branch | `cursor/public-pages-5` |
| PR title | `feat(payments,legal): bedenktijd waiver required before the uitgebreide test checkout; one waiver sentence and version shared with the terms` |
| Mockups | `pb-m05-gebruiksvoorwaarden` §6 (text); `docs/tests` `ts-d5`/`ts-m5` for the checkbox in the order summary (when present) |
| Migration | none (tests 01 owns `WaiverAcceptedAtUtc`/`WaiverTextVersion`) |

## Goal
Nobody can pay for and start the uitgebreide test without explicitly giving up the 14-day bedenktijd, and the sentence they tick is the same one the terms describe.

## 05.1 Re-check Dependency C
`git grep -n "WaiverAcceptedAtUtc\|waiver_required" origin/acceptatie -- Jobsy.Core Jobsy.Api Jobsy.Infrastructure`. Write the case at the top of the PR.

Today (`a611db40`, absent case):
- `DeepAnalysisController` L44 `[HttpPost("checkout")]` → `IDeepAnalysisService.StartCheckoutAsync(userId, kind, ct)` (`DeepAnalysisService.cs` L155) with no waiver parameter.
- UI: `Pages/Candidate/DeepAnalysis.razor` L45/L430 and the offer in `TestDetail.razor` (~L730–758): one click starts the checkout.
- In Production `AllowStubPayments=false` makes `StartCheckoutAsync` throw (`docs/tests` P1), so nobody can buy there yet.

## 05.2a Present (tests 01 landed)
- The checkbox label is **`Terms.Waiver.Checkbox`** (04.7), word for word. If tests 01 added its own key (e.g. `Tests.Waiver.*`), make it an alias that reads the same value, or switch the markup to `Terms.Waiver.Checkbox`; keep one source of truth.
- `WaiverTextVersion` stored on new checkouts = `LegalDocumentVersions.Terms.Version` (replace the literal `"2026-09"`). Existing rows are untouched.
- The order summary gets a link "Lees meer over bedenktijd" → `/gebruiksvoorwaarden#bedenktijd`.
- The receipt mail's waiver sentence (tests 01: "Je koos ervoor om meteen te beginnen. Daarom kun je niet binnen 14 dagen annuleren.") stays. Add the terms version in the mail's small print if the template has a facts block; otherwise leave it.

## 05.2b Absent (tests 01 not landed)
- **API:** `StartCheckoutAsync(userId, kind, waiverAccepted, ct)`. `DeepAnalysisController` checkout reads `{ waiverAccepted: bool }` from the body (default false). If false → **400 `{ code: "waiver_required" }`** before anything else happens (no Mollie call, no stub row). Log `PlatformLog` `deep.waiver-accepted` (user id, kind, `LegalDocumentVersions.Terms.Version`) when true.
- **UI** (today's markup, `DeepAnalysis.razor` and the `TestDetail.razor` offer):
  - a checkbox with `Terms.Waiver.Checkbox` and a link to `/gebruiksvoorwaarden#bedenktijd`
  - the pay button is `disabled` until it's ticked
  - `waiver_required` from the API → `Terms.Waiver.Required` "Zet eerst het vinkje." (no `ex.Message`)
- No migration, no receipt mail (that's tests 01).
- Add to `docs/public-pages-followups.md`: "tests 01: persist `WaiverAcceptedAtUtc` + `WaiverTextVersion` = `LegalDocumentVersions.Terms.Version`, reuse `Terms.Waiver.Checkbox`, keep `BedenktijdGuardTests` green."

## 05.3 Tests (both cases)
- `BedenktijdGuardTests` (API): checkout without `waiverAccepted` → 400 `waiver_required`, and no checkout row / Mollie call (stub/mocked service asserts zero calls); with `true` → the existing happy path (stub on acceptatie/Development).
- `BedenktijdUiTests` (bUnit): the pay button is disabled until the box is ticked; the label equals `Terms.Waiver.Checkbox` in nl and en.
- Present case: `WaiverTextVersion` on a new checkout equals `LegalDocumentVersions.Terms.Version`.

## Success criteria
- There is no code path that starts a deep-analysis checkout without `waiverAccepted == true` (test).
- One string key for the waiver sentence across the terms and the checkout.
- PR body: dependency case C, "⚖️ Lawyer review needed" (waiver wording), follow-up line if absent.

Done → next: `06-meldknop.md`.

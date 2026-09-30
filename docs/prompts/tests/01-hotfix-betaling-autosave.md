# 01. Hotfix: real Mollie payment for the uitgebreide test, price per test type, order summary + waiver + receipt/invoice; autosave flush/cancel fixes; no raw errors on test pages

Read `00-README.md` first ("How to run", §0, §P, Dependencies D and F). Branch `cursor/tests-hotfix` from `origin/acceptatie`. **Standalone: not stacked on anything.**

> **Rules (same as README §0, repeated on purpose):**
> - Never merge, never deploy, never use rule `123` (`.cursor/rules/shortcut-123.mdc`).
> - Never push to `main` or `acceptatie`; push only `cursor/tests-hotfix`; no force-push.
> - ONE PR into `acceptatie`; the body starts with `Standalone hotfix (not stacked)`.
> - Red build/tests or an unmet success criterion: push, open the PR as **draft**, stop and report. Don't start file 02.
> - Nothing unlocks a paid test without a **paid** status from Mollie (or the stub path, only in Development or with `JobsyAuth:AllowStubPayments=true`). The UI never shows `ex.Message`.
> - Don't redesign the pages here. Keep today's layout and only change what this file names; the ontdekkingsreis design is 02+. Don't change the candidate nav, and don't change the token purchase behaviour.

| | |
|---|---|
| Branch | `cursor/tests-hotfix` |
| PR title | `fix(payments,tests): real Mollie checkout for the uitgebreide test (webhook + reconcile, unlock only when paid), price per test type, order summary + waiver + receipt/invoice; autosave flush and cancel fixes; no raw errors` |
| PR body starts with | `Standalone hotfix (not stacked)` + Dependencies D and F outcome |
| Mockups | `ts-d5`/`ts-m5` (summary content and the waiver sentence), `ts-d6`, `ts-d7`, `ts-m6` (the three return states). Render them **in today's layout**, restyled in 05 |
| Migration | `AddDeepTestPaymentsAndPrices` (the only one in this file) |
| Split seam | if > ~1.500 lines: **01a** = payment (01.2–01.10) on `cursor/tests-hotfix`; **01b** = autosave + raw errors (01.11–01.12) on `cursor/tests-hotfix-b`, stacked on 01a, body "Stacked on #<01a> (standalone hotfix part 2)". File 02 then branches from `cursor/tests-hotfix-b` |

## Goal
- The uitgebreide test can really be bought in Production, and only a real payment unlocks it.
- The candidate sees what they pay (incl. btw) and agrees to start right away, and they get a receipt and an invoice.
- Admin sets the price per test type.
- Answers are never lost when leaving a test, and a candidate never sees a technical error text.

Closes: P1, P2, P3, P4, P5 (data + minimal UI), P6 (states), P7, A1, A2, A3, A4.

## 01.1 Today (verify first)
- `DeepAnalysisService.StartCheckoutAsync` (L155–218):
  - Outside Development without `JobsyAuth:AllowStubPayments` it throws "Diepte-analyse-betalingen zijn buiten Development alleen beschikbaar met Mollie of AllowStubPayments.", so **Production can't sell it**.
  - Otherwise it writes `DeepAnalysisCheckout` with `PaymentId = stub_deep_{slug}_{guid}` and returns `/candidate/deep-analysis/checkout?kind=…&paymentId=…`.
- `DeepAnalysisController.CompleteCheckout` (L68–95) calls `TryFulfillPaidCheckoutAsync(allowDevStubMarkPaid: true)`: on acceptatie (`AllowStubPayments=true`, `render.yaml` L229) **opening the return URL marks it paid**. It then guesses the kind from the paymentId substring (L88–92).
- `MollieWebhooksController` only handles `TokenPurchaseCheckouts`. `TokenCheckoutReconcileHostedService` only reconciles tokens.
- `MolliePaymentService`:
  - `CreateTokenPurchaseCheckoutAsync` (L63–195): API key via `IIntegrationCredentialService`, `EnsureLiveKeyOutsideDevelopment`, redirect via `PublicWebBaseUrl`, `ResolveWebhookUrl()` L343–366, metadata, `MolliePaymentMethods.PrimaryMethods`.
  - `GetPaymentStatusAsync` (L197+) is tied to token sessions.
- Price: one `FlexCommercialSettings.DeepAnalysisPriceEuro` (default `DefaultDeepAnalysisPriceEuro = 2.99m`), admin field in `SettingsAdmin.razor`, `PUT api/settings/lobsy-commercial`.
- UI:
  - `TestDetail.StartExtendedAsync` (L730–758) starts checkout on one click and shows `ex.Message`.
  - `Tests.SecureMollie` is at L321.
  - `DeepAnalysisCheckout.razor` sets `_message` and navigates away.
- Autosave: `QuestionnaireAutosave.cs` as described in bugs A1–A3.

## 01.2 Data (`AddDeepTestPaymentsAndPrices`)
- `FlexCommercialSettings` gets four columns: `DeepTestPriceCompetenceEuro`, `DeepTestPriceCareerEuro`, `DeepTestPriceValuesEuro`, `DeepTestPriceCultureEuro` (precision 10,2).
  - The migration fills all four from the **current** `DeepAnalysisPriceEuro` value of the row, so an admin-set price carries over. For a fresh seed use `DefaultDeepAnalysisPriceEuro` (€ 2,99).
  - `DeepAnalysisPriceEuro` stays for one release (marked `[Obsolete]`, no longer read). 07 lists its removal.
- `DeepAnalysisCheckout` gets:
  - `AmountExVatCents`, `VatAmountCents`, `TotalAmountCents` (int; `AmountEuro` stays filled for old readers)
  - `PaymentMethod` (max 32, nullable), `ProviderStatus` (max 32, nullable), `IsStub` (bool)
  - `WaiverAcceptedAtUtc` (DateTime), `WaiverTextVersion` (max 16)
  - `Locale` (max 8, the UI language at checkout)
  - `InvoiceId` (Guid?), `FailedAtUtc`, `ExpiresAtUtc`
  - `ReceiptSentAtUtc` (nullable; idempotent mail)
  - status enum adds `Failed = 3`, `Expired = 4` (append only)
- Indexes: **unique** on `PaymentId` (filtered `WHERE "PaymentId" <> ''`); `(UserId, Kind, Status)`; `(Status, CreatedAtUtc)` for reconcile.
- New `ConsumerPurchaseInvoice`:
  - `Id`, `InvoiceNumber` (unique, `LOB-KT-{yyyy}-{0000}`), `DeepAnalysisCheckoutId` (unique FK), `UserId` (nullable, `SetNull` on user delete)
  - `CustomerName`, `CustomerEmail`, `CustomerCountry` (nullable, ISO-2 if known)
  - `Description`, `AmountExVatCents`, `VatAmountCents`, `TotalAmountCents`, `VatRate` (0.21), `MolliePaymentId`
  - `IssuedAt`, `CreatedAt`, `VatDeclarationId` (nullable) + `VatDeclarationStatusLabel`, the same pattern as `TokenPurchaseInvoice`
- Existing rows: checkouts get cents from `AmountEuro` via `TokenVatPricing.SplitInclVatEuros`, `IsStub = PaymentId LIKE 'stub_deep_%'`, `WaiverTextVersion = 'legacy'`. `PendingModelChangesTests` green.

## 01.3 Price per test type (P7, D4, D10)
- Core `DeepAnalysisPricing.For(FlexCommercialSettingsDto s, AssessmentKind kind)` returns the incl.-btw price; `Split(...)` returns cents via `TokenVatPricing`. Every place that reads `DeepAnalysisPriceEuro` switches to it:
  - `CandidateCompetencyService` L33/59/120, `CandidateCareerInterestService` L170, `CandidateCulturePersonalityService` L34/60, `CandidateValuesService`, `CandidateKompasService`, `RoleFitCheckService`, `DeepAnalysisService`
  - grep `DeepAnalysisPriceEuro` and list them in the PR
  - the DTO fields keep their names, now filled per kind
- Admin (`SettingsAdmin.razor`, `SettingsController` `lobsy-commercial`):
  - four inputs "Uitgebreide test Competenties / Beroepen / Waarden / Cultuur (incl. btw)" replace the single field
  - validation `> 0` and `≤ 100`, 2 decimals (`ArgumentException` → `{ code: "invalid_price" }`)
  - the audit log entry that exists for commercial updates includes the four values
- Candidate copy always shows the **total incl. btw** + "inclusief btw". A price change doesn't touch existing checkouts.

## 01.4 Mollie create (P1; §P)
- Extract from `MolliePaymentService` the private helpers needed by both flows, **without changing token behaviour**:
  - `TryGetApiKeyAsync`, `SendMollieAsync`, `ResolveWebhookUrl`, `EnsureLiveKeyOutsideDevelopment`
  - the web base resolution
  - a new `FetchMolliePaymentAsync(paymentId)` returning `{ status, method, amount, metadata }` without touching token sessions
  - Token tests stay byte-identical green.
- New `IDeepTestPaymentService` (Core interface, Infrastructure `DeepTestPaymentService`):
  - `CreateCheckoutAsync(userId, kind, waiverAccepted, locale)`
  - `GetStatusAsync(userId, checkoutId)`
  - `TryFulfillAsync(checkoutId, source)`, where source is `Webhook | Reconcile | Return | Stub`
- `CreateCheckoutAsync`:
  1. Candidate exists and passes `CanUseTests` (parental + test consent); the kind supports the uitgebreide test; not already unlocked (409 `already_unlocked`); `waiverAccepted == true` (400 `waiver_required`).
  2. Cancel older `Pending` checkouts of the same user+kind: locally `Cancelled`; at Mollie `DELETE payments/{id}` best effort, only when cancelable, errors logged, never fatal.
  3. Price via `DeepAnalysisPricing`, cents via `TokenVatPricing`.
  4. **Stub path** (§P: Development or `AllowStubPayments`): `PaymentId = stub_deep_{guid}` (no slug), `IsStub = true`, the checkout URL is our return page. Otherwise **Mollie** `POST payments`:
     - `amount` EUR `0.00` invariant; `description` "Lobsy Uitgebreide test {testnaam}" (nl name, max 255)
     - `redirectUrl` `{PublicWebBaseUrl}/candidate/deep-analysis/checkout?checkoutId={id:D}`; `webhookUrl` = `ResolveWebhookUrl()` (the same `/api/webhooks/mollie`)
     - `method` = `MolliePaymentMethods.PrimaryMethods`; `locale` mapped from the UI language (`nl_NL`, `en_US`, `pl_PL`, fallback `en_US` for ro/ar)
     - `metadata` = `{ purpose: "deep_test", checkoutId, kind }`; **no names, e-mail or answers**
     - Store `PaymentId`, `ExpiresAtUtc = now + 48 h` (D9), `Locale`, the waiver fields (`WaiverTextVersion = "2026-09"`).
  5. No API key and no stub allowed ⇒ 503 `{ code: "payments_unavailable" }` (log a warning). Never the raw developer text.
- Result `{ checkoutId, checkoutUrl, totalCents, isStub }`. The Web client navigates to `checkoutUrl` (external for Mollie).

## 01.5 Webhook, reconcile, return status (P2)
- `MollieWebhooksController.MolliePayment`: after the token lookup finds nothing, look up `DeepAnalysisCheckouts` by `PaymentId`. If found, `TryFulfillAsync(checkout.Id, Webhook)`. Unknown ids are still acknowledged `200`. Transient failures → `503` + `PlatformLog` (existing helper). Token behaviour unchanged.
- `TryFulfillAsync(checkoutId, source)` (idempotent, one transaction, `SELECT … FOR UPDATE` on the checkout row or an optimistic concurrency token):
  - Already `Paid` ⇒ ensure unlock + invoice + mail exist (fill what's missing) and return.
  - `IsStub` ⇒ allowed only when §P stub rules hold **and** source is `Stub` (the return page in stub mode); otherwise return without change.
  - Else fetch Mollie:
    - `paid` ⇒ `Status = Paid`, `PaidAtUtc`, `PaymentMethod`, then `UnlockForUserAsync(userId, checkout.Kind)` (**the kind from the row**, P3), invoice (01.8) and receipt mail (01.9)
    - `failed`/`canceled`/`expired` ⇒ `Failed`/`Cancelled`/`Expired` + `FailedAtUtc`
    - `open`/`pending`/`authorized` ⇒ unchanged
  - Paid for a test that is already unlocked (double payment): still `Paid` + invoice, plus a `PlatformLog` warning "manual refund" (D9).
- `DeepTestCheckoutReconcileHostedService` (`Infrastructure/Jobs`, the `TokenCheckoutReconcileHostedService` pattern: 90 s startup delay, every 3 min, min age 2 min, batch 50):
  - `Pending` non-stub checkouts younger than 48 h → `TryFulfillAsync(Reconcile)`
  - older → `Expired`
  - `Paid` without invoice or receipt → complete them
  - disabled in tests by the same options pattern; registered like the other jobs
- **API** (`DeepAnalysisController`):
  - `POST api/me/deep-analysis/checkout?kind=` with body `{ waiverAccepted: bool }` → 01.4.
  - `GET api/me/deep-analysis/checkout/{checkoutId:guid}` → `{ status: pending|paid|failed|cancelled|expired, kind, testSlug, totalCents, paidAtUtc?, invoiceNumber?, isStub }`.
    - Foreign or unknown id ⇒ 404.
    - When `pending` and not stub, it calls `TryFulfillAsync(Return)` **once** before answering, so the return page resolves fast without trusting the URL.
  - `POST api/me/deep-analysis/checkout/{checkoutId:guid}/stub-pay` exists **only** when the stub rules hold (else 404) and calls `TryFulfillAsync(Stub)`.
  - The old `POST checkout/{paymentId}/complete` becomes a thin shim that looks the row up by `PaymentId` for the user and returns the same status DTO. **It never marks paid.** Mark it `[Obsolete]`; 07 removes it.
- `IDeepAnalysisService.TryFulfillPaidCheckoutAsync(… allowDevStubMarkPaid)` is deleted; its callers use `IDeepTestPaymentService`. Update the tests that used it (`DeepAnalysisPrivacySecurityTests`, `CompletedTestAutosaveTests`).

## 01.6 Order summary before paying (P5, today's layout)
- `TestDetail` "Uitgebreide test" and the `DeepAnalysis` offer button no longer start checkout directly. They open `DeepTestOrderDialog.razor` (`Components/Candidate/Tests/`, built on `LobsyFriendlyDialog`) with the content of `ts-d5` in plain form:
  - **What:** "Uitgebreide test {testnaam}" + "{n} vragen" + what you get (the 4 lines of `ts-d5`, keys `DeepPay.Get.*`)
  - **Price:** "€ {prijs}" + "Eenmalig · inclusief btw"
  - **Methods:** from `MolliePaymentMethods.PrimaryMethods` (display names)
  - **Checkbox** (required, unchecked by default): "Ik wil meteen beginnen. Ik weet dat ik dan niet meer binnen 14 dagen kan annuleren."
  - "Je gratis uitslag blijft van jou."
  - Buttons: "Nu niet" (secondary, closes) · **"Naar betalen · € {prijs}"** (primary, disabled until the box is ticked)
  - Link "Voorwaarden" → the existing terms page
- Server errors map to `DeepPay.Err.*`:
  - `waiver_required` "Zet eerst het vinkje."
  - `already_unlocked` "Je hebt deze test al. Begin meteen." + a link
  - `payments_unavailable` "Betalen lukt nu even niet. Probeer het later nog eens."
  - `consent_required`: opens the existing consent card link
  - unknown → `Common.Error`
- **`Tests.SecureMollie` ("Veilig betalen via Mollie")** is shown only when the checkout will go to Mollie: a new `GET api/me/deep-analysis/payment-mode` → `{ mode: "mollie" | "stub" | "unavailable" }`, cached per circuit. In stub mode show "Testbetaling: er wordt niets afgeschreven." (only Dev/acc); when unavailable, hide the buy button and show `payments_unavailable` text.

## 01.7 Return page states (P6, today's layout)
- `DeepAnalysisCheckout.razor` reads `checkoutId` (Guid) from the query. Legacy links with `paymentId` go through the shim to find the id.
  - It calls `GET …/checkout/{id}` and polls every 2 s up to 60 s while `pending`, then stops polling and shows the "duurt lang" state.
  - **Bezig** (`ts-m6`): "We checken je betaling" + "Dit duurt meestal een paar seconden." (`role="status"`). After 60 s: "Duurt het langer dan een minuut? Je mag deze pagina sluiten. We sturen je een mail als het klaar is." + link "Terug naar de test".
  - **Gelukt** (`ts-d6`): "Betaald. Je kunt beginnen", the receipt lines (wat, bedrag incl. btw, datum Europe/Amsterdam, factuurnummer), "Je krijgt de factuur ook per e-mail.", primary "Begin met vraag 1" (`/candidate/deep-analysis/{slug}`), link "Later beginnen" (`/profiel/tests/{slug}`).
  - **Niet gelukt** (`ts-d7`): "De betaling is niet gelukt" + "Er is niets afgeschreven.", primary "Opnieuw betalen" (opens the order dialog on `/profiel/tests/{slug}?buy=1`), link "Terug naar de test".
  - **Stub mode** (Dev/acc only): the page shows a clearly labelled "Testbetaling afronden" button that calls `stub-pay`. Nothing auto-pays on page load.
  - **No / unknown / foreign id:** "We vinden deze betaling niet." + link "Naar Mijn tests" (`/profiel`). Never a dead end.
- Remove `candidate/deep-analysis/checkout` from `MainLayout.IsQuestionnairePath` so the page has the normal header/nav (there is a way back). The other deep-analysis paths stay in focus mode.

## 01.8 Invoice (D8)
- `ConsumerInvoiceService` (Infrastructure):
  - `CreateForDeepCheckoutAsync(checkoutId)`: idempotent, one invoice per checkout, a unique-number race handled like `TokenPurchaseInvoiceService.NextInvoiceNumberAsync`, prefix `LOB-KT-{yyyy}-`
  - `RenderPdfAsync(invoiceId, culture)`: QuestPDF, the token invoice layout
- **PDF content:**
  - Lobsy's legal details: the same source as the token invoice
  - customer name + e-mail, invoice number, date (Europe/Amsterdam)
  - line "Uitgebreide test {testnaam}", ex btw, btw 21 %, total
  - payment method, "Betaald via Mollie" (stub invoices are never created in Production; on acc they carry "TESTFACTUUR – geen betaling")
- `GET api/me/deep-analysis/invoices/{invoiceId:guid}/pdf` (owner only, 404 otherwise); a download link on the Gelukt state and in the mail.
- btw (D8, **flag for Dennis/accountant**):
  - `VatDeclarationService` adds consumer invoices of the period as their own line "Kandidaat-aankopen" in the totals, and confirming a declaration stamps `VatDeclarationId` on them like token invoices
  - `QueueForInvoiceAsync` gets an overload for consumer invoices, so the btw buffer covers them
  - tests in `VatDeclarationServiceTests`
- Admin: the existing token-finance list gets a filter "Kandidaat-aankopen" showing number, date, amount and test type, **without** candidate answers. If that page has no filter seam, a simple list page under the same admin section is fine; say which in the PR.

## 01.9 Receipt mail (Dependency F)
- Sent once per paid checkout (`ReceiptSentAtUtc` guard), in the checkout's `Locale` (nl/en now; pl/ro/ar fall back to en until 06).
- Subject "Je uitgebreide test staat klaar". The body contains:
  - greeting with the first name; "Betaald: Uitgebreide test {testnaam}"
  - a fact card with amount incl. btw, date/time Europe/Amsterdam and invoice number
  - the waiver sentence "Je koos ervoor om meteen te beginnen. Daarom kun je niet binnen 14 dagen annuleren."
  - primary button "Begin met vraag 1"; support line
  - PDF invoice attached (`{invoiceNumber}.pdf`)
- **F present:** registry key `deep_test_receipt` via `ITransactionalMailer`. **F absent:** `TransactionalEmails.DeepTestReceipt(…)` with `EmailLayout.Wrap`/`FactCard`/`PrimaryButton` via `IEmailService` + `// TODO(emails-02)`. Either way the admin mail preview lists it with sample data.
- A mail failure never rolls back the payment; the reconcile job retries unsent receipts (max 5 tries, then `PlatformLog`).

## 01.10 Guards and privacy
- **Stub guard test:** with `Production` + `AllowStubPayments=false`:
  - `CreateCheckoutAsync` never returns a stub
  - `stub-pay` is 404
  - `TryFulfillAsync` on a `stub_deep_` row is a no-op
  - the old `complete` shim never unlocks
  - `render.yaml` Production has `JobsyAuth__AllowStubPayments` `"false"`
- `PrivacyDataService`:
  - export includes checkouts (kind, amount, status, dates) and invoices (number, amount, date)
  - delete removes checkouts and keeps invoices with `UserId = null` (fiscal retention, §0 privacy)
  - tests for both

## 01.11 Autosave fixes (A1–A3)
`QuestionnaireAutosave.cs` (used by the 4 test pages, the deep analysis and the onboarding mini-test):
- **Two tokens:** the debounce token (cancelled by a newer answer) and a separate **save token** that only the page's lifetime token cancels. A newer answer **never cancels a running save**. It waits for it through `_gate`, and the save that follows persists the newest snapshot (latest wins; stale results are ignored by `_version`).
- `SetAnswerAsync` never throws `OperationCanceledException` to the caller: debounce supersede returns quietly, and lifetime cancel returns quietly. Persist failures set `Status = Failed` and raise `OnFailed(QuestionnaireSaveError)` with a code; they don't throw.
- **Flush on leave:** `DisposeAsync` flushes pending changes. If there are unsaved answers (`_dirtyVersion > _savedVersion`) it runs one final save with a 5 s timeout (not linked to the cancelled component token), then disposes the gate after the save completes. Pages call `await _autosave.FlushAsync()` before any own navigation (×, "Later verder", "Afronden", back link).
- **Status:** starts as `Idle` (new enum value) and becomes `Saved` only after a successful persist. The UI shows nothing for `Idle`. `ReplaceAll` sets `Idle`.
- Disposal safety: no `ObjectDisposedException` when an answer arrives during dispose (ignored after `_disposed`).
- Tests (`QuestionnaireAutosaveTests`, fake persist with delays):
  - an answer within 800 ms then dispose ⇒ persisted
  - an answer during an in-flight save ⇒ both saves complete, the last snapshot wins, no exception
  - persist throws ⇒ `Failed` + callback, no throw
  - initial `Idle`
  - lifetime cancel ⇒ no throw

## 01.12 No raw errors (A4)
- Every `catch (Exception ex)` in `CompetencyTest`, `CareerTest`, `CultureScan`, `ValuesScan`, `DeepAnalysis`, `DeepAnalysisCheckout` and `TestDetail` (line numbers in the README bug table):
  - logs via `ILogger`
  - maps an API problem `code` to a `TestErr.*`/`DeepPay.Err.*` key, or else to `Common.Error`
- The test save/complete endpoints (the 4 controllers + `DeepAnalysisController`) add a `code` next to today's `message`:
  - `parental_consent_required`, `test_consent_required`, `unknown_question`, `invalid_answer`, `already_completed_limit` (reserved for 03), `not_found`
  - the Web client parses `code` (`ApiProblem` helper, the one the token pages use if present)
- Save failure copy (nl, B1):
  - banner "Je laatste antwoord is nog niet bewaard. Probeer het opnieuw." + button "Opnieuw proberen" (calls `RetryAsync`)
  - consent codes: "Je moet eerst toestemming geven voor de tests." + link to the consent card
- **Guard test** `NoRawExceptionMessageInTestPagesTests`: the listed razor files contain no `ex.Message` / `.Message` of an exception bound to UI state.
- Strings (`UiStringsTests.cs`, created here with the prefixes from §0; nl/en now, pl/ro/ar = en copy + listed for 06).

## 01.13 Tests
- **Unit:**
  - `DeepAnalysisPricing`: per kind, fallback, split cents incl./ex btw
  - invoice number series
  - `TryFulfillAsync` state machine (paid/failed/expired/double paid/stub rules)
  - Mollie create body (amount format, metadata without PII, redirect/webhook URLs)
  - autosave (01.11)
- **API** (`DeepTestPaymentApiTests`, fake `IMollieHttp` handler):
  - create requires the waiver
  - webhook paid ⇒ unlocked + invoice + mail once, and replaying the webhook changes nothing
  - webhook for a token payment still credits tokens (regression)
  - status endpoint 404 for a foreign id
  - the `complete` shim never unlocks in Production
  - admin price per kind is used on the next checkout
- **bUnit:**
  - the order dialog (button disabled until ticked, price incl. btw, the SecureMollie line only in `mollie` mode)
  - the return page's 3 states + unknown id
  - `TestDetail` shows no raw error for a failing API
- Playwright (if you can, on the local stub): open dialog → tick → stub pay → Gelukt → "Begin met vraag 1" opens the uitgebreide test.

## Success criteria
- In a Production-configured test host without stubs, no request path can set a deep checkout to `Paid` except a Mollie `paid` status (webhook, reconcile or status check).
- On acceptatie (stubs on), paying requires the waiver and the explicit stub button; opening a return URL alone changes nothing.
- The kind after payment always equals the checkout row's `Kind` (culture via `/candidate/disc` included).
- Four admin prices exist, carried over from today's value (€ 2,99 in the seed); candidates see the price for that test incl. btw.
- One invoice (`LOB-KT-…`) and one receipt mail per paid checkout; it appears in the btw declaration totals as "Kandidaat-aankopen".
- "Veilig betalen via Mollie" is not rendered in stub/unavailable mode.
- Leaving a test within 0.8 s of an answer keeps the answer; fast answering never shows a technical text; "Bewaard" appears only after a real save.
- The guard tests are green; token purchase tests are unchanged and green.

## Out of scope (later files)
The ontdekkingsreis design of the offer and return pages (05), the one-question flow (02), the limit (03), translations beyond nl/en (06), automatic refunds (not planned), OSS btw (flag).

# 08. Admin: monthly payout runs, approve/reject, invoices, SEPA export, mark paid, corrections, reassign

Read `00-README.md` first (§0, §IA "Lobsy admin", §R, §D, D3, Dependencies B + C). Branch `cursor/salesmanager-8` from `cursor/salesmanager-7`.

> **Rules (same as README §0, repeated on purpose):**
> - Never merge, never deploy, never use rule `123` (`.cursor/rules/shortcut-123.mdc`).
> - Never push to `main` or `acceptatie`; push only this file's `cursor/salesmanager-*` branch; no force-push.
> - ONE stacked PR into `acceptatie`.
> - Red tests or an unmet success criterion: push, open the PR as **draft**, stop and report. Don't start the next file.
> - Beneficiary is resolved server-side from the signed-in user, no employer contact/candidate data in any portal DTO, money writes are idempotent (§0, §P, §R).

| | |
|---|---|
| Branch | `cursor/salesmanager-8` (from `cursor/salesmanager-7`) |
| PR title | `feat(sales-admin): monthly payout runs — approve, self-billing invoices, SEPA export, mark paid; corrections + reassign UI` |
| PR body starts with | `Stacked on #<PR 07> (cursor/salesmanager-7)` + the Dependencies B case + the note for admin redesign 06.4 (README Dependencies B) |
| Mockups | none for admin (follow the admin redesign's `AdminTabs`/`EntDataTable`/`EntDrawer` patterns). Beneficiary-side states: `sm-d4-wallet.png` Uitbetalingen tab |
| Split seam | **08a** = run job + run service + API + SEPA/bank-transfer provider (08.2–08.4). **08b** = admin UI (runs, corrections, reassign) + mails + docs (08.5–08.7) |

## Goal
Lobsy stays in control of every euro that goes out (D3): once a month a run collects the payout requests, an admin approves or rejects each line, approval issues the self-billing invoices, the admin exports one SEPA file (or pays by hand), and marks the run paid. Until Mollie payouts exist, that's the only way money leaves.

## 08.1 Today (verify first)
- Admin pages: `/admin/sales-managers` (`SalesManagersAdmin.razor`: invite, applications, create invoice for an SM, mark-paid), `/admin/ambassadeurs` (404 while parked, 01.10), `/admin/sales` (commercial settings), `/admin/token-finance` (payout/VAT tabs). Admin redesign moves them to `/admin/gebruikers/sales` and `/admin/financien/uitbetalingen` (06.4: "Markeer als betaald" = existing `mark-paid`; **no approve step**).
- `POST api/sales-managers/{userId}/invoices` (create) and `invoices/{id}/mark-paid` (`RequireAdmin`), `CommissionLedgerService.RecordPayoutAsync`.
- Run the Dependencies B check again (the admin stack may have landed meanwhile) and use the matching case.

## 08.2 Run job + service
- `SalesPayoutRunHostedService` (pattern of the other `BackgroundService` jobs): wakes every 15 min; when `SalesClock.Today()` is the first workday of the month (`SalesClock.FirstWorkdayOfMonth`) and local time ≥ 06:00 and no scheduled run exists for that `RunDate`, creates a `SalesPayoutRun` (`Draft`, `IsExtra = false`) and moves every `Requested` request created before now into it (`InRun`). Single-instance safe (unique index; catch the conflict).
- `ISalesPayoutRunService`:
  - `CreateExtraRunAsync(admin)` (MFA session): same as the job with `IsExtra = true`.
  - `RejectLineAsync(admin, requestId, reason 5–500)`: `Rejected`, unlink the ledger lines (they're available again), mail the beneficiary with the reason.
  - `ApproveRunAsync(admin, runId)` (MFA session, confirm dialog): for every line still `InRun`:
    - **IBAN hold:** if `IbanPayoutHoldUntilUtc > now`, the line goes back to `Requested` (next run) with the note "Nieuwe rekening: wacht tot {datum}".
    - **Consent / profile** re-check; failing lines go back to `Requested` with the reason shown to admin.
    - otherwise: issue the self-billing invoice (`IssueForRequestAsync`, 07.5; status `Issued`), request `Approved`, link invoice.
    - run `Approved`, `ApprovedByUserId`; audit `sales.payout.run.approve`. Mail beneficiaries "Je uitbetaling is goedgekeurd. Je krijgt € {x} binnen 3 werkdagen."
  - `ExportAsync(admin, runId)` → the provider (08.3) file; run `Exported`, `ExportFileSha256`.
  - `MarkPaidAsync(admin, runId | invoiceIds)`: calls the **existing** mark-paid per invoice (so admin redesign 06.4's button stays the same code path), sets requests `Paid`, `PaidAtUtc`, writes the `Payout` ledger line (`RecordPayoutAsync`), run `Paid` when all lines are paid, then `Closed`. Mail "Je uitbetaling is betaald." Mark-paid on an invoice from anywhere (old page, admin 06) must close its request too (hook in the existing mark-paid service method, not the controller).
- Amount 0 or negative lines are never paid; they stay `Requested` and admin sees "Saldo te laag".

## 08.3 Payout provider seam (bank transfer now, Mollie later)
- `ISalesPayoutProvider` (Core): `Key`, `ExportAsync(run, lines) → (fileName, contentType, bytes)`, `SupportsAutomaticPayout` (false).
- `BankTransferPayoutProvider` (default, `Sales:Payout:Provider = "bank-transfer"`):
  - **SEPA credit transfer** `pain.001.001.03` XML (one `PmtInf`, `ReqdExctnDt` = next workday, debtor = config `Sales:Payout:DebtorName`, `DebtorIban`, `DebtorBic`; creditor = holder name + full IBAN (decrypted only in memory for the file), `EndToEndId` = invoice number, remittance "Lobsy commissie {factuurnummer}"). Validate against the XSD in tests (add the XSD under `Jobsy.Tests/Fixtures/`).
  - also a CSV "handmatig overmaken" list (name, masked IBAN, amount, invoice number) for small runs.
  - missing debtor config → export disabled with a clear admin message; nothing else breaks.
- The export download writes `PersonalDataAccessLog` (`sales.payout-account`, `export`) and audit `sales.payout.run.export`. The file is not stored (only its hash).
- A `MolliePayoutProvider` is **not** built; the seam and a `// Mollie payouts: implement ISalesPayoutProvider when available` note are enough.

## 08.4 API (`api/admin/sales/*`, `RequireAdmin`; money actions + MFA session)
- `GET payout-runs` (list with totals), `GET payout-runs/{id}` (lines: beneficiary display name (masked like the admin users list), role, amount ex/incl VAT, VAT treatment, masked IBAN, flags: nieuwe rekening, geen toestemming, saldo te laag), `POST payout-runs` (extra run), `POST payout-runs/{id}/lines/{requestId}/reject`, `POST payout-runs/{id}/approve`, `GET payout-runs/{id}/export?format=sepa|csv`, `POST payout-runs/{id}/mark-paid` (all or `invoiceIds[]`).
- Corrections (API from 02.7) and reassign (03.5) are reused.

## 08.5 Admin UI
- Components (self-contained, Dependencies B): `Components/Admin/Sales/PayoutRunsSection.razor`, `PayoutRunDetailDrawer.razor`, `LedgerCorrectionDialog.razor`, `AttributionSection.razor`.
- **Rondes** (in `/admin/financien/uitbetalingen?tab=rondes` or the fallback tab): table of runs (Datum, Soort (Maandelijks/Extra), Status pill, Regels, Totaal incl. btw, Goedgekeurd door (masked)); primary "Extra ronde maken" (confirm). Drawer per run: lines table with flags and per-line "Afwijzen" (reason dialog), footer actions in order **Ronde goedkeuren** → **SEPA-bestand downloaden** (+ "CSV") → **Markeer als betaald** (bulk confirm "Markeer {n} als betaald"). Each action enabled only in the right status; `EntImpactNote` explains the next step ("Na goedkeuring maakt Lobsy de facturen. Daarna download je het SEPA-bestand voor je bank.").
- **Salesmanagers** detail drawer (on `/admin/gebruikers/sales` or the fallback pages): tabs Profiel (masked), Werkgevers (attributed roots with status and commission; "Toewijzing wijzigen" per row → dialog: new beneficiary picker (salesmanagers) or "Geen", reason, history list), Grootboek (lines with states; "Correctie boeken" → dialog amount ± + reason + optional company), Uitbetalingen (requests and invoices; links to the run).
- **Geparkeerde ambassadeurs met saldo** (panel under the runs table in Rondes, only when the 01.10 service returns rows; replaces the 01 `EntImpactNote`): intro "Het ambassadeursprogramma staat uit. Deze tegoeden worden niet automatisch uitbetaald."; table Naam (masked), Openstaand saldo excl. btw, Laatste regel; per row only "Grootboek bekijken" (read-only ledger) and "Correctie boeken" (existing dialog, reason required, MFA session). **No pay/request/run action** for these lines; they never enter a run while parked.
- Sales settings form: add the fields from 02.7 if 02 didn't render them in the admin form yet.
- Cross-links both ways (D2 of the admin redesign): "Uitbetalingen staan bij Financiën › Uitbetalingen & btw" / "Salesmanagers beheer je bij Gebruikers & rollen."

## 08.6 Mails
- `SalesMail.PayoutApproved`, `PayoutRejected` (with reason), `PayoutPaid`, `PayoutDeferredIbanHold`, `PayoutDeferredMissingConsent`; all respect the "uitbetaling" preference except rejection (always sent).

## 08.7 Docs
- `docs/ROUTES.md` (admin routes + the one-line note for admin redesign 06.4 when the admin stack hasn't run, Dependencies B), `docs/security/roles-matrix.md` ("Sales payout approve: Admin + MFA session"), `CHANGELOG.md`, a short runbook `docs/runbooks/sales-payout-run.md` (monthly steps: open run → check flags → approve → download SEPA → upload in the bank → mark paid).

## Tests
- Job: creates exactly one run on the first workday (incl. a month whose 1st is a Saturday, and January with 1 jan), not before 06:00, not twice; picks only `Requested` lines created before the run.
- Approve: hold → deferred; missing consent → deferred; normal → invoice issued with the right VAT; audit row; MFA session required (403 without).
- Reject: lines released; mail sent.
- Export: SEPA XML validates against the XSD; amounts and `EndToEndId`s match; debtor config missing → disabled; hash stored, file not stored; access log written.
- Mark paid: existing mark-paid closes the request (called from the old endpoint too); `Payout` ledger line once (idempotent); run closes when all lines are paid.
- Rights matrix rows for every admin endpoint (non-admin 403, admin without MFA session 403 on money actions).
- Parked balances: panel hidden with no rows; shown with masked name + balance; ambassadeur lines are never picked into a run while `AmbassadorsEnabled` is off.
- bUnit: run table, drawer action enabling per status, correction and reassign dialogs (validation).

## Success criteria
- `dotnet build` + `dotnet test` green.
- On acceptatie-like data: a salesmanager requests € 1.284,50 in September; the run of 1 October 2026 contains the line; admin approves (invoice `SB-2026-…` with "Factuur uitgereikt door afnemer"), downloads a valid SEPA file and marks it paid; the salesmanager sees "Uitbetaald" and the invoice.
- There is still no code path where a salesmanager (or a parked ambassadeur) can mark anything paid.

Done → next: `09-aanbevelen-avg-opruimen.md`.

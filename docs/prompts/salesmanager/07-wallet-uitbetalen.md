# 07. Wallet & uitbetalingen: balances by state, request a payout, invoice preview, invoice PDF text + KOR, jaaroverzicht

Read `00-README.md` first (§0, §IA, §R, §D, D3, D5). Branch `cursor/salesmanager-7` from `cursor/salesmanager-6`.

> **Rules (same as README §0, repeated on purpose):**
> - Never merge, never deploy, never use rule `123` (`.cursor/rules/shortcut-123.mdc`).
> - Never push to `main` or `acceptatie`; push only this file's `cursor/salesmanager-*` branch; no force-push.
> - ONE stacked PR into `acceptatie`.
> - Red tests or an unmet success criterion: push, open the PR as **draft**, stop and report. Don't start the next file.
> - Beneficiary is resolved server-side from the signed-in user, no employer contact/candidate data in any portal DTO, money writes are idempotent (§0, §P, §R).

| | |
|---|---|
| Branch | `cursor/salesmanager-7` (from `cursor/salesmanager-6`) |
| PR title | `feat(sales): wallet — commission states, payout requests (≥ € 50), self-billing invoice text + KOR, jaaroverzicht` |
| PR body starts with | `Stacked on #<PR 06> (cursor/salesmanager-6)` + worked example (lines → request → invoice amounts for 21 % and KOR) |
| Mockups | `sm-d4-wallet.png`, `sm-d5-uitbetaling-aanvragen.png`, `sm-m2-wallet.png` |
| Split seam | **07a** = request service + API + stub removal + invoice service changes + PDF text (07.2, 07.3, 07.5). **07b** = wallet page + request drawer + jaaroverzicht (07.4, 07.6) |

## Goal
The beneficiary understands their money: what's pending, available, requested and paid, and can request a payout that Lobsy approves in the monthly run (08). The self-completing stub goes away, and invoices say what the law requires, with the right VAT (21 % or KOR).

## 07.1 Today (verify first)
- `SalesManagerPayoutService` (`me/payouts/preview|checkout|complete`, `api/sales-managers` L426–500; ambassadeur twins L295–360, parked behind the 01.10 gate: leave them): `CreateCheckoutAsync` only works with stub payments (`AllowStubPayouts`, L626); in production it throws "Live uitbetaling (Mollie) is nog niet geconfigureerd". Completing the stub creates the invoice **and marks it paid instantly**. No minimum, no approval, no schedule.
- `SelfBillingInvoiceService`: numbering `SB-{year}-{seq}` (L343–360), `VatTreatment` always `Standard21`, PDF says "SELF-BILLING" / "Self-billing factuur gegenereerd door Lobsy"; the text "factuur uitgereikt door afnemer" is missing.
- `Invoices.razor` (moved to `/sales/wallet` in 01). The ambassadeur `Finance.razor` stays at its old route behind the 01.10 gate (404 while parked).
- Admin: `POST api/sales-managers/{userId}/invoices` (create for an SM) and `invoices/{id}/mark-paid`.

## 07.2 Payout request service
- `ISalesPayoutRequestService` (Infrastructure):
  - `PreviewAsync(beneficiary)` → amount = **all** available (ex VAT, incl. negative corrections), VAT per `VatTreatment` (21 % → `SalesCommissionRules.VatOn`; KOR → 0), total, masked IBAN + holder name, expected run date, the invoice preview fields, and `Blockers[]`.
  - Blockers (each with an nl label and a link): below `PayoutMinimumEuro` ("Je kunt uitbetalen vanaf € 50. Nu beschikbaar: € 32,10."), no self-billing consent (06), incomplete payout profile (IBAN, holder, company data, VAT number when 21 %), an open request exists, no MFA-verified session (an external IdP login counts, ADR 0005; same helper as the admin checks).
  - `RequestAsync(beneficiary)`: re-checks blockers; in one transaction creates `SalesPayoutRequest` (`Requested`, snapshots amount/VAT/total/`MaskedIban`/`VatTreatment`) and links every available line (`SalesPayoutRequestId`). Lines that become available later go into the next request. Idempotent per beneficiary via the "one open request" unique index (a double click returns the existing request).
  - `CancelAsync(beneficiary, requestId)`: only while `Requested` (not yet in an approved run); unlinks the lines.
- Mail `SalesMail.PayoutRequested` ("We hebben je aanvraag. Lobsy keurt uitbetalingen goed op {datum}.").
- **Partial amounts are not supported** (spec-wins difference: the mockup's editable amount field with "Alles" becomes a read-only amount).

## 07.3 API + stub removal
- `GET api/sales/me/wallet` (balances by state, next run date, last 3 invoices), `GET api/sales/me/wallet/entries?period=&kind=&state=&page=` (lines with labels, company display name via the same D4 rule, `AvailableOn` for pending), `GET api/sales/me/payouts` (requests with status timeline), `GET api/sales/me/invoices` (+ `/{id}/pdf`), `GET api/sales/me/payouts/preview`, `POST api/sales/me/payouts` (request), `POST api/sales/me/payouts/{id}/cancel`, `GET api/sales/me/jaaroverzicht/{year}.pdf`.
- Remove the self-complete flow: `me/payouts/checkout` and `me/payouts/complete` on `api/sales-managers` return **410 Gone** with "Uitbetalen gaat nu via een aanvraag." (keep the routes one release so old tabs fail gracefully; 09 deletes them). Delete `PayoutCheckoutStubView` usage from the portal; `/sales/wallet/uitbetalen` now opens the request drawer. Existing `SalesManagerPayoutCheckout` rows stay read-only and appear in the Uitbetalingen tab as "Eerdere uitbetaling".
- `PartnerSalesPayoutCheckoutStub` (werkgever partner programme) is **not** touched.

## 07.4 Wallet page `/sales/wallet` (`sm-d4`, `sm-m2`)
- Header "Wallet & uitbetalingen", lead "Je commissie, je uitbetalingen en je facturen. Lobsy maakt de factuur voor je (self-billing)." Right: secondary "Jaaroverzicht {jaar} (PDF)".
- KPI row: hero **Beschikbaar** (amount, "excl. btw · + € {btw} btw" or "excl. btw · geen btw (KOR)", primary "Uitbetaling aanvragen" or the first blocker as a calm note + link) · **In behandeling** ("vrij na {n} dagen (bedenktijd)") · **Aangevraagd** (open request amount or "geen open aanvraag") · **Uitbetaald in {jaar}** ("{n} facturen").
- `EntTabs` (URL `?tab=`):
  - **Mutaties**: filters Periode (year), Soort, Status; columns Datum · Omschrijving (label · company display name; sub-line "Jaar {n} · {p} % over € {aankoop}" or the correction reason) · Status (pill; pending shows "vrij op {dd-MM}") · Bedrag excl. btw (signed, right). 25 per page.
  - **Uitbetalingen**: requests with a status stepper (Aangevraagd → In ronde → Goedgekeurd → Betaald; rejected shows the reason), amount, invoice link; legacy checkouts as "Eerdere uitbetaling".
  - **Facturen**: number (mono), date, total incl. btw, status pill, PDF.
- Right column: "Zo werkt uitbetalen" (4 steps: Commissie komt binnen · Jij vraagt uitbetaling aan (vanaf € {min}) · Lobsy keurt goed (1e werkdag van de maand) · Geld op je rekening (binnen 3 werkdagen op {masked IBAN})), "Laatste facturen" (3), a small "Uitbetaalrekening / Btw" card with "Wijzigen" → `/sales/profiel`.
- Mobile (`sm-m2`): hero card, a neutral "Zo werkt het" strip (Aanvraag · Akkoord · Betaald; not a live status), segmented tabs, list rows (company/omschrijving, date · soort, signed amount + state pill).

## 07.5 Invoice service + PDF (D5)
- `SelfBillingInvoiceService.IssueForRequestAsync(request, consent)` (called by 08 on approval): uses the profile's `VatTreatment` (21 % or KOR → 0 VAT), stores `SelfBillingConsentId` and `SalesPayoutRequestId`, lines = one per ledger line (description from labels: "Commissie · {werkgever} · {maand}", corrections negative), numbering unchanged (`SB-{year}-{seq:0000}`) but protected by the unique index + retry.
- PDF (`SalesPdf.*` strings; accountant check flagged in the PR):
  - title **"Factuur"**, the line **"Factuur uitgereikt door afnemer"** (prominent, under the title), and "Self-billing volgens de afspraak van {consent date} (versie {v})";
  - supplier = the beneficiary (company name, address, KvK, btw-nummer when present); customer = Lobsy (from the existing company settings used by the current PDF);
  - invoice number, invoice date, period, lines, subtotal, VAT line: "Btw 21 %" **or** "Btw-vrijgesteld op grond van de kleineondernemersregeling (KOR)";
  - payment line "Betaald aan rekening {masked IBAN}" once paid.
  - Remove the English "SELF-BILLING" heading.
- Preview in the request drawer uses the same view model (number shown as "wordt toegekend bij goedkeuring").

## 07.6 Request drawer `/sales/wallet/uitbetalen` (`sm-d5`, `EntDrawer`)
- Title "Uitbetaling aanvragen", sub-line "Lobsy maakt de factuur namens jou (self-billing)."
- Amount (read-only, 07.2) with "Beschikbaar € … · minimaal € {min}"; lines Commissie excl. btw · Btw 21 % / "Geen btw (KOR)" · **Jij ontvangt**; Naar rekening (masked + holder); Uitbetaling "{run date} · daarna binnen 3 werkdagen" (or "na {date}" when the IBAN hold applies, 06).
- "Voorbeeld factuur" card (07.5 fields, compact) and an info note "Klopt je btw niet? Gebruik je bijvoorbeeld de kleineondernemersregeling (KOR)? Pas dit eerst aan bij Profiel & gegevens."
- Footer: "Annuleren" + primary "Aanvragen · € {total}". Blockers replace the primary with the blocker note + link.

## Tests
- Preview/Request: minimum, consent, profile, open request, MFA session blockers; all available lines linked; negative corrections included; double submit → one request; cancel only while `Requested`.
- VAT: 21 % and KOR amounts; invoice PDF contains "Factuur uitgereikt door afnemer", the consent reference, and the KOR text for KOR; no "SELF-BILLING".
- Numbering unique under parallel issue (retry).
- 410 on the old checkout/complete endpoints; werkgever partner stub unaffected.
- Rights matrix: other beneficiary's request/invoice → 404; werkgever/candidate → 403.
- bUnit: tabs + filters → URL, stepper states, drawer blockers, mobile layout.

## Success criteria
- `dotnet build` + `dotnet test` green.
- In the stub environment a salesmanager with € 1.284,50 available requests a payout and sees it as "Aangevraagd"; nothing is marked paid until admin acts (08).
- No production code path can mark a payout paid from the salesmanager side.

Done → next: `08-admin-uitbetaalrondes.md`.

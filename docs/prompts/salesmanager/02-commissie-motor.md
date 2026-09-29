# 02. Commission engine: window from first purchase, snapshots, bonus-token window, per-organisation, hold, refunds → corrections

Read `00-README.md` first (§0, §D, §P, D1, D3, D16, Dependencies E). Branch `cursor/salesmanager-2` from `cursor/salesmanager-1`.

> **Rules (same as README §0, repeated on purpose):**
> - Never merge, never deploy, never use rule `123` (`.cursor/rules/shortcut-123.mdc`).
> - Never push to `main` or `acceptatie`; push only this file's `cursor/salesmanager-*` branch; no force-push.
> - ONE stacked PR into `acceptatie`.
> - Red tests or an unmet success criterion: push, open the PR as **draft**, stop and report. Don't start the next file.
> - Beneficiary is resolved server-side from the signed-in user, no employer contact/candidate data in any portal DTO, money writes are idempotent (§0, §P, §R).

| | |
|---|---|
| Branch | `cursor/salesmanager-2` (from `cursor/salesmanager-1`) |
| PR title | `feat(sales): commission engine — window from first purchase, rate snapshots, 14-day hold, refund corrections` |
| PR body starts with | `Stacked on #<PR 01> (cursor/salesmanager-1)` + a worked-example table (§0 PR description) |
| Mockups | none (engine). The states it produces are shown in `sm-d4-wallet.png` ("In behandeling · vrij op 10-10", "Beschikbaar", "Correctie · Aankoop terugbetaald") |
| Split seam | **02a** = rules + activation/snapshots + bonus window + per-organisation + backfill (02.2–02.5, 02.8). **02b** = hold/derived state + refunds/chargebacks + manual correction API + settings (02.4 state part, 02.6, 02.7) |

## Goal
Commission is computed the way Dennis approved (D1) and is safe against refunds (D3): the 3-year window starts at the organisation's first purchase, all rates are frozen at that moment, the company's 15 % bonus tokens stop with the window, an organisation with vestigingen is one commercial unit, every commission line waits 14 days, and refunds/chargebacks create negative corrections automatically.

## 02.1 Today (verify first)
- Trigger: Mollie webhook (`MollieWebhooksController`) → `TokenPurchaseFulfillmentService` (L101 reads `GetPaymentStatusAsync`) → `RevenueShareService.ApplyTokenPurchaseShareAsync` (L29–245). `PaymentStatusResult` = `PaymentId, Status, IsPaid, Method`; a non-paid status is acknowledged and ignored, so **refunds and chargebacks are never seen**.
- Rates: `SalesCommissionRules.TokenCommissionRate(firstYearStartedAt, asOf, directRate, durationDays, year2, year3)`. The window start is `Company.FirstYearStartedAt`, set at **registration** in `CompanyRegistrationService` (~L1553 branch, ~L1565 org).
- Snapshots: `ResolveCommissionTermsAsync` (L265) reads `CommissionDirectRateSnapshot`/`IndirectRateSnapshot`/`DurationDaysSnapshot`, but **year-2/3 rates are read live** from `SalesCommercialSettings` (L283–284). `SnapshotCommissionTermsAsync` runs at registration for both branch and org.
- 15 % bonus tokens: `SalesCommissionRules.AmbassadorTokens(packSize)` granted in `ApplyTokenPurchaseShareAsync` L134–155 for **every** purchase of a referred company, **no window check** (the name "Ambassador" in `RevenueShareRecipientKind.Ambassador` means "the referred company", not the ambassadeur role).
- Ledger credits: `CommissionLedgerService.TryCreditTokenCommissionAsync` / `TryCreditIndirectTokenCommissionAsync` / `TryCreditAmbassadeurTokenCommissionAsync` / `TryCreditFounderBonusAsync`; balance `GetBalanceExVatAsync` / `GetUninvoicedBalanceExVatAsync`. Credits are immediately spendable.
- Org + vestiging registration sets `ReferredBySalesManagerUserId` on **both** rows (window and snapshots per row).
- Tests to keep green: `SalesCommissionRulesTests`, `SalesManagerCommissionTests`, `RevenueShareServiceTests`, `MollieWebhookCommissionSettlementTests`, `TokenPurchaseFulfillmentIdempotencyTests`, `SalesManagerReferralHierarchyTests`, `AmbassadeurCommissionRulesTests`.

## 02.2 Rules (pure, Core)
- `SalesCommissionRules` gets one entry point: `CommissionTerms` record (`DirectYear1Rate`, `Year2Rate`, `Year3Rate`, `IndirectRate`, `DurationDays`, `StartsAtUtc`) and `CommissionYear? YearFor(CommissionTerms, DateTime purchaseAtUtc)` → 1/2/3 or null (before start or after `StartsAtUtc + DurationDays`). Year boundaries: year 1 = `[start, start+365d)`, year 2 = `[+365d, +730d)`, year 3 = `[+730d, +DurationDays)`.
- `DirectRate(terms, year)`, `IndirectRate(terms, year)` (indirect only in year 1, as today), `BonusTokensAllowed(terms, purchaseAt)` = `YearFor(...) != null`.
- Keep the old method signatures as thin wrappers until nothing calls them, then delete them (same PR). Unit tests for boundaries: day 0, day 364/365, day 729/730, day 1094/1095, purchase before start (never happens after 02.3, but guarded).

## 02.3 Activation + snapshots (D1)
- **Registration** only records attribution (`ReferredBySalesManagerUserId`, `SalesAttributedAtUtc`, `SalesAttributionSource`; no ambassadeur attribution while parked, 01.10) on the **root** company (and, for backwards compatibility with code that reads the vestiging row, on the vestiging as today). It **no longer** snapshots terms and no longer sets `FirstYearStartedAt` for commission (founder-slot logic keeps using `FirstYearStartedAt` as today; don't change the founder slot or start-highlight behaviour).
- **Activation** happens in the fulfilment path on the first credited token purchase of any company in the organisation: in one transaction, on the root, set `CommissionStartsAtUtc = purchase paid time` and snapshot all terms (`CommissionDirectRateSnapshot`, `CommissionYear2RateSnapshot`, `CommissionYear3RateSnapshot`, `CommissionIndirectRateSnapshot`, `CommissionIndirectSalesManagerUserId`, `CommissionDurationDaysSnapshot`, `CommissionTermsSnapshottedAtUtc`). If a registration-time snapshot already exists (legacy), keep its direct/indirect/duration values and only fill year 2/3.
- Guard against races: activation uses a conditional update (`WHERE CommissionStartsAtUtc IS NULL`) and re-reads.
- `ResolveCommissionTermsAsync` reads **only** the root's snapshot (no live settings). Delete the live `year2`/`year3` reads.
- Ambassadeur (parked, D8): while `AmbassadorsEnabled` is off, the ambassadeur credit path books nothing (01.10 gate; keep that gate intact when you rework `ApplyTokenPurchaseShareAsync`). Don't add ambassadeur-specific rules here. The new `SalesCommissionTerms` / hold / correction code works on a beneficiary line regardless of role, so a later ambassadeur stack can plug its own rate in without touching it. `CommissionAmbassadeurRateSnapshot` and `AmbassadeurCommissionRules` stay as they are.

## 02.4 Per-organisation unit + derived state
- `SalesCommercialUnit.RootOf(Company)` = `ParentCompanyId ?? Id` (verify there is only one parent level; if deeper trees exist, walk up with a depth limit of 5 and say so in the PR). Attribution, window, snapshots and **counting** use the root. A vestiging's purchase is commissionable when its root is attributed.
- `ISalesWalletReadService` (Infrastructure) is the only place that computes states and balances. Derived `CommissionEntryState` per line:
  - `Pending` ("In behandeling"): commission kinds with `AvailableFromUtc > now` and no request.
  - `Available` ("Beschikbaar"): `AvailableFromUtc <= now`, no `SalesPayoutRequestId`, no `SelfBillingInvoiceId`.
  - `Requested` ("Aangevraagd"): linked to a request in `Requested/InRun/Approved`.
  - `Paid` ("Uitbetaald"): linked to a paid request or a paid legacy invoice.
  - `Settled` ("Verrekend"): a correction that has been netted into a payout.
- Balances: `Available` = sum of available lines **including negative corrections** (can be negative); `Pending`, `Requested`, `PaidThisYear`, `EarnedThisYear` (commission kinds, Europe/Amsterdam calendar year). The wallet chip (01) switches to `Available`.
- Legacy payout checkouts and invoices keep their links; their lines count as `Paid` when the invoice is `Paid`.

## 02.5 Bonus-token window + per-organisation fix
- In `ApplyTokenPurchaseShareAsync`: grant the 15 % bonus tokens only when `BonusTokensAllowed(terms, paidAt)`; after the window, no grant and no `RevenueShareLog` "Ambassador" line with tokens (log a zero-token line so idempotency still works).
- Direct/indirect commission use `YearFor` on the root terms; after the window, no ledger line.
- Dashboard/list counting is fixed in 04 on top of `SalesCommercialUnit`; 02 adds `ISalesEmployerReadService.ListUnitsAsync(beneficiary)` returning one row per **root** with `BranchCount`, used by 04 and by a test that proves an org + 1 vestiging counts once.

## 02.6 Hold + refunds + chargebacks (D3)
- Every commission-kind credit (`TokenCommission`, `IndirectTokenCommission`, `FounderBonus`; the ambassadeur credit too, once re-enabled) sets `AvailableFromUtc = SalesClock.EndOfLocalDayUtc(paidAt + CommissionHoldDays)` (setting, default 14).
- Payment status (Dependencies E): `PaymentStatusResult` gains `AmountRefundedEuro`, `AmountChargedBackEuro` (and `AmountEuro` if not already available). The webhook handler no longer ignores a **paid** payment that later reports refunds/chargebacks: after the existing (idempotent) fulfilment, it calls `ISalesCorrectionService.ApplyPaymentReversalsAsync(checkoutId, amountPaid, refunded, chargedBack)`.
- `ApplyPaymentReversalsAsync`, per beneficiary line of that checkout (direct, indirect, and legacy ambassadeur lines booked before parking; corrections still apply to those, D8):
  - target correction = `–round(originalCommission × reversed / amountPaid)` for refunds and chargebacks separately;
  - book **only the delta** between the target and the corrections already booked for that checkout/beneficiary/kind (`RefundCorrection` / `ChargebackCorrection`), with `SourceRefundKey = "{kind}:{cumulative cents}"`, `CorrectsEntryId` = the original line, `AvailableFromUtc = now`, `Note` = label key only (no amounts in free text);
  - the unique index (§D) makes replays no-ops. Partial refunds work; a later full refund books the remainder.
  - if the original line is still `Pending`, the pair nets to zero in the same state bucket; if it was paid, the negative line reduces the next payout (balance may go negative; 07 blocks requests below the minimum).
- D16: also write `PlatformLog` warning `sales.refund.tokens-not-reversed` with checkout id and amounts (no personal data).
- Founder bonus: if the € 2.500 start-package payment is refunded, the same service corrects the founder bonus (verify the onboarding checkout reaches the same webhook; if it uses another path, hook it there and say so).

## 02.7 Manual correction API + settings
- `POST api/admin/sales/ledger/corrections` (`RequireAdmin` + MFA session): `{ beneficiaryUserId, companyId?, amountExVat (≠ 0, |x| ≤ 10.000), reason (5–500) }` → `Adjustment` line, `AvailableFromUtc = now`, `CreatedByUserId`, `Reason`; audited `sales.ledger.correction` (Dependencies C). UI in 08.
- Settings: `CommissionHoldDays`, `PayoutMinimumEuro`, `IbanChangeHoldDays`, `AttributionCookieDays` added to the existing sales settings form and `PUT api/sales-commercial/admin/settings` (nullable = keep; validated ranges §D). Help texts: "Zo lang staat nieuwe commissie op 'In behandeling'. Dan kan een klant nog geld terugvragen." etc. (`SalesAdmin.Settings.*`).

## 02.8 Backfill (one-off, idempotent)
- `SalesCommissionBackfill` (run once from the migration's companion hosted step or a startup task guarded by a `PlatformMigrationMarker`, whichever pattern the repo uses for data backfills, e.g. `IbanEncryptionMigrationHostedService`):
  - for each attributed root with credited purchases: `CommissionStartsAtUtc` = first credited purchase paid time of any company in the organisation; fill year-2/3 snapshots from the current settings; keep existing direct/indirect/duration snapshots;
  - attributed vestigingen whose own snapshot differs from the root's: log both in the PR (count only), use the root's from now on;
  - **no ledger recomputation**; write a summary `PlatformLog` row (units activated, units without purchases).

## Tests
- Rules: boundaries (02.2), referred-SM year-1 20 % + indirect 5 %, after window → 0 and no bonus tokens.
- Activation: first purchase sets start + all snapshots once (parallel purchases → one activation); a settings change afterwards doesn't change a snapshotted unit; a legacy registration snapshot is kept.
- Per-organisation: org + vestiging registration → one unit; vestiging purchase uses the root window; count = 1.
- Hold: credit on 29-09 16:00 CEST with 14 days → available after 13-10 23:59:59 CEST (`SalesClock`); state transitions Pending → Available.
- Refunds: full refund → one correction = –commission; partial 40 % then full → two corrections summing to –commission; webhook replay → no extra rows; chargeback separate kind; indirect + legacy ambassadeur lines corrected (switch off); `PlatformLog` warning written.
- Manual correction: validation, MFA session required (403 without), audit row.
- Existing tests updated only where the window start moved; add a note per changed expectation in the PR.

## Success criteria
- `dotnet build` + `dotnet test` green.
- Worked example in the PR: org registers 12-03-2026 via SM-K7Q2MP, first purchase 15-03-2026 € 800 → € 200 commission, "In behandeling" until 29-03; refund of € 400 on 20-03 → correction – € 100; a purchase on 20-03-2027 is year 2 at the **snapshotted** 10 % even if the setting is changed to 8 %.
- No bonus tokens are granted for a purchase more than 1095 days after activation.

Done → next: `03-attributie-klikken.md`.

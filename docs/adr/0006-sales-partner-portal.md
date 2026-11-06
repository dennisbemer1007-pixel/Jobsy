# ADR 0006: Lobsy Partner (salesmanager) portal

**Status:** Accepted  
**Date:** 2026-09-30

## Context

Salesmanagers needed a calm partner portal (earnings, funnel, toolkit, wallet, payouts) with privacy by design, mandatory 2FA, and fair treatment of people they recommend. The Ambassadeur role is not ready for product use and must stay parked without deleting data.

## Decision

Short form of D1–D16 from the salesmanager stack:

- **D1 Commission:** 25 % / 10 % / 5 % over three years from first credited purchase; rates snapshotted at activation; org-root window; 15 % employer bonus tokens only inside the window.
- **D2 Attribution:** 30-day first-click cookie `lobsy_sales_ref`; typed code wins; self-referral blocked; admin reassign with reason (future purchases only).
- **D3 Payouts:** 14-day hold; ≥ € 50 request; monthly run on 1st workday, admin approve; SEPA bank transfer until Mollie payouts.
- **D4 Visibility:** trade name + place (not eenmanszaak) + own commission only — never contacts, candidates, vacancies.
- **D5 Tax:** self-billing with separate consent; 21 % btw or KOR.
- **D6 Security:** 2FA mandatory for SalesManager (and Ambassadeur when re-enabled); IBAN change needs step-up + hold.
- **D7 Menu:** Overzicht · Verkopen · Geld · Account under `/sales/*`.
- **D8 Ambassadeur parked:** `AmbassadorsEnabled` default off; pages/APIs/cookies/commission gated; data kept; shared ledger/invoice/payout stays role-agnostic; re-enable needs its own spec (portal pages, commission tier, candidate counts only, candidate notice).
- **D9** URL prefix `/sales/*`. **D10** Dutch-only UI module. **D11** No rename of beneficiary FK columns. **D12** `SalesLabels` for enums. **D13** Retention 60/30/30 (aanbevelen), 7y fiscal, 25m clicks. **D14** Real token pack prices on sales surfaces. **D15** Recommended person gets notice mail + objection link. **D16** Refunds correct commission only (tokens not reversed).

## Consequences

- Portal nav lives in `SalesNav`; legacy `/salesmanager/*` 301 via `SalesLegacyRoutes`.
- Admin approves payout runs and salesmanager applications; parked ambassadeur balances are flagged, not auto-paid.
- Turning Ambassadors back on is a future stack; this ADR does not redesign ambassadeur UX.

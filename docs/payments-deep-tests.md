# Deep test payments (candidate uitgebreide test)

## Flow
1. Candidate opens offer (`/candidate/deep-analysis/{kind}` when not unlocked).
2. Waiver required → `POST` start checkout with locale → Mollie Hosted Checkout (or stub URL in Dev / `JobsyAuth:AllowStubPayments=true`).
3. Return → `/candidate/deep-analysis/checkout` polls server status.
4. Webhook / reconcile / stub fulfill → **Paid** only; then unlock. Never unlock on pending/failed/unknown id.
5. Receipt mail + PDF invoice (series **LOB-KT**), amount **incl. btw**; Locale from checkout (`nl|en|pl|ro|ar`).

## States
- checking / pending (≤60s hint)
- paid (+ receipt `<dl>`)
- failed / cancelled / expired (nothing charged)
- not found (unknown checkout / payment id)

## Stub
Only Development or `AllowStubPayments=true`. UI never shows `ex.Message`.

## Refunds
Manual only (deferred automatic refunds).

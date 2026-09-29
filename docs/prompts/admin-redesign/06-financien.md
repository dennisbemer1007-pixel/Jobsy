# 06 · Financiën

> Read `00-README.md` first (§0, §IA, D2, D10). Builds on 01–05. **Money rule:** pages move and get one structure; **no** change to amounts, VAT logic, invoice numbering, Mollie calls or webhooks.

| | |
|---|---|
| Branch | `cursor/admin-redesign-6`, created from `cursor/admin-redesign-5` |
| PR | ONE PR into `acceptatie`, title `feat(admin): finance in four clear places; goodwill and pricing de-duplicated`. Body starts with `Stacked on #<PR of 05> (cursor/admin-redesign-5)` |
| Mockups | `ad-d6-financien.png` |

## 06.1 Today (verify)
- `FinanceAdmin.razor` (now `/admin/financien`): period KPIs + tab "Tokenlog" + a link list (Token-administratie, "Goodwill / Compensatie", Sales beheer, Salesmanagers, Ambassadeurs).
- `TokenFinanceAdmin.razor` (now `/admin/financien/uitbetalingen`): tabs Aankopen · Inkoop / Salesmanagers · **Goodwill** · BTW-buffer · BTW Aangiftes & Facturen (`api/tokens/finance/*`, `api/vat/*`).
- `TokenAdmin.razor` (now `/admin/financien/goodwill`): goodwill/compensation grants → **Goodwill lives in two places**.
- Pricing: after 05, all on `/admin/financien/prijzen` (tabs).
- Payouts: salesmanagers/ambassadeurs start payouts themselves (`me/payouts/checkout`, `SalesManagerPayoutCheckout` Pending → Paid → Completed); admin's action is `POST api/sales-managers/invoices/{id}/mark-paid` on `SelfBillingInvoice`s.

## 06.2 Omzet & transacties `/admin/financien` (`ad-d6`)
- Header: `h1`, lead "Betalingen via Mollie, facturen en tokenaankopen. Prijzen staan bij Prijzen & pakketten." Actions: month/quarter segment + "Export voor boekhouding" (the existing `finance/purchases/export`).
- KPI row (5 × `AdminKpiCard`) from `AdminFinanceSummaryService` (added in 02, else add it here): Omzet (periode, incl. btw) · Tokens verkocht · Open bij Mollie ({n} · € {som}, "oudste {d} dagen") · Btw-buffer (current, "Q{n} aangifte {datum}" from `vat/open-periods`) · Open facturen salesmanagers (€, {n}).
- `AdminTabs`: **Transacties** (the Aankopen data from `api/tokens/finance/purchases` as `AdminDataTable`: Datum · Factuur (mono) · Organisatie · Product (+ muted payment method) · Mollie status pill (Betaald/Open/Mislukt/Terugbetaald/Verlopen = the statuses the DTO has) · Bedrag incl. btw · row menu with the existing "Factuur (pdf)" link) · **Tokenlog** (existing tab) · **KPI's** (the existing metrics board of `FinanceAdmin`).
- Remove the link list; people pages live under Gebruikers & rollen (D2).
- Right column (≥ 1024): **Open facturen salesmanagers** card (list, total, one primary "Bekijken" → Uitbetalingen tab) and **Btw Q{n}** card (omzet excl., af te dragen, gereserveerd; from existing VAT preview). The mockup's "Alle 5 goedkeuren" becomes this "Bekijken" link (no approve step exists).

## 06.3 Goodwill & tokens `/admin/financien/goodwill` (one place)
- Top card: "Tokens toekennen" (primary, existing `GrantTokensDialog` / the `TokenAdmin` form, moved into `Sections/GoodwillGrantSection.razor`).
- Below: the goodwill history (the Goodwill tab body from `TokenFinanceAdmin`, moved into `Sections/GoodwillHistorySection.razor`) with filters (periode, organisatie) and the existing `finance/goodwill/export`.
- Remove the Goodwill tab from Uitbetalingen & btw; `…/uitbetalingen?tab=goodwill` redirects here (add to `AdminLegacyRoutes`).

## 06.4 Uitbetalingen & btw `/admin/financien/uitbetalingen`
- `AdminTabs`: **Uitbetalingen** (salesmanager self-billing invoices + payout checkouts of salesmanagers and ambassadeurs, read from the existing endpoints; action per invoice "Markeer als betaald" = existing `mark-paid`; bulk "Markeer {n} als betaald" runs the same endpoint per id with one confirm) · **Inkoop / salesmanagers** (existing) · **Btw-buffer** (existing) · **Btw-aangifte & facturen** (existing).
- Masking: payee names masked like the users list; IBAN only as the stored `MaskedIban`.
- Cross-link: "Salesmanagers en ambassadeurs beheer je bij Gebruikers & rollen."

## 06.5 Prijzen & pakketten `/admin/financien/prijzen` (finish D10)
- Tabs from 05 stay. Add at the top a muted **"Wat bepaalt welke prijs?"** table (static strings, reviewed by you in the PR): each pricing entity → what it drives (e.g. "Tokenpakketten: wat werkgevers betalen voor tokens", "Kosten per actie (`TokenSpendCost`): tokens per publicatie/uitlichten", "Token-kosten per vacaturetype (`VacancyTypeTokenCost`): …", "Salespakketten (`SalesPackage`): …"). Verify each line against the code that reads the entity; where two entities overlap, add an info `AdminImpactNote` "Let op: {A} en {B} bepalen allebei {x}. Welke wint: {regel uit de code}." and list it in the PR under "Overlap for Dennis". **No data merge.**
- Every price edit form keeps its existing endpoint and validation.

## Tests
- `AdminFinanceSummaryService` (if added/changed): totals on a fixed data set equal the sums the existing finance pages show.
- Legacy `?tab=goodwill` redirect; goodwill appears only under Goodwill & tokens (no second tab).
- Bulk mark-paid calls the existing endpoint once per id; partial failures reported.
- bUnit: Transacties table status pills with text, KPI cards, Goodwill page = grant + history.
- Existing finance/VAT/pricing tests green (`LobsyCommercialSettingsTests`, `Sprint6AdminSuiteTests`, VAT tests) with updated URLs only.

## Success criteria
- Four finance items, each with one job; goodwill and pricing each in exactly one place.
- No amount, VAT, invoice or Mollie logic changed (diff review: no changes in `MolliePaymentService`, VAT services, invoice numbering).
- Build + tests green; PR body includes the "Overlap for Dennis" list.

## Done → next
Push, open the PR, note its number. Continue with **`07-beveiliging-audit.md`**.

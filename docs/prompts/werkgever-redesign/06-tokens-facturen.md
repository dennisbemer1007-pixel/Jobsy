# 06 · Tokens & facturen: overview, verbruik per vestiging, mutaties, facturen, tokenaanvraag

> Read `00-README.md` first. §IA, §R and D5 and D12 apply, plus §0 "Must NOT touch" (Mollie/checkout, prices).

| | |
|---|---|
| Branch | `cursor/werkgever-redesign-6` from `cursor/werkgever-redesign-5` (stacked) |
| PR | ONE PR into `acceptatie`: `feat(werkgever): tokens & facturen with verbruik per vestiging and tokenaanvragen`. Stacked on #<PR of 05> (`cursor/werkgever-redesign-5`) |
| Mockups | `bm-d5-tokens-facturen.png` (BM overview) |
| Split seam (if too big) | 6a = pages (overview, verbruik, mutaties, facturen, partner move); 6b = tokenaanvraag entity + flow |

**Goal.** Everything about tokens and money is in one place. The bedrijfsmanager buys and distributes; the vestigingsmanager sees his balance and **asks**; the regiomanager looks.

## 06.1 Today (verify first)
- The moved `Tokens.razor` (873 lines), tabs Saldo / Logging / Trackingcode & flyer / Referral-overzicht / Uitleg:
  - `GetTokenBalancesAsync` → `GET api/tokens/balance`
  - `GetTokenPacksAsync` → `packs`
  - `GetTokenLogsAsync` → `GET api/tokens/logs` (`TokenLogsController`)
  - `UpdateTokenManagementAsync` → `PUT api/companies/{id}/token-management`
  - `UpdateBillingPreferenceAsync`
  - `CreateTokenCheckoutAsync` → `POST api/tokens/checkout` (`TokenPurchaseRoles`)
  - `AllocateTokensAsync` → `POST api/tokens/allocate` (`TokenAllocateRoles` = EnterpriseManager, Admin)
  - partner affiliate calls (`GetPartnerAffiliateMeAsync`, `…ToolkitAsync`, `…ReferralsAsync`)
- `Regional/TokenControl.razor` (193 lines): a duplicate view of balances + allocate + managed vacancies. **RM can't allocate server-side**, so the page is misleading.
- `TokenPricing` packs (1/5/10/50/100) and `TokenSpendCost` rows (`GET api/tokens/costs`).
- Invoices: `GET api/companies/{id}/billing-history` + `…/billing/invoices/{invoiceId}/pdf` (`CompanyBillingHistoryItem`), still in the 05 Profiel link.
- Notifications: `IUserNotificationService` (in-app) + `IPushNotificationService`.
- D5 was applied in 01: `BranchManager` is no longer in `TokenPurchaseRoles`.

## 06.2 `/werkgever/tokens`: overview (BM "Saldo & kopen", VM "Saldo & aanvragen", RM "Tokenverbruik")
- `WgPageShell`: title "Tokens & facturen" for BM, else the role label. Lead "Saldo, verdeling over vestigingen, aankopen en facturen op één plek."
  - Actions: "Alle mutaties" (→ `/werkgever/tokens/mutaties`), primary "Tokens kopen" (BM only; opens today's checkout flow unchanged)
- Sub-nav as `EntTabs` rendered as links (each tab is its own URL): Overzicht · Verbruik per vestiging · Mutaties · Facturen. Per role, only what §IA allows.
- **KPI row (BM)**, `EntKpiCard` × 4:
  - "Centraal saldo" (sub "niet toegewezen: {n}")
  - "Toegewezen aan vestigingen" (sub "over {n} vestigingen")
  - "Verbruikt (30 dagen)" (sub "vorige 30 dagen: {n}")
  - "Gereserveerd voor aanvragen" (sum of pending publish requests' costs + open tokenaanvragen; sub "{n} publicatieaanvragen")
  - Numbers come from `balance` + `logs`; add at most **one** read endpoint `GET api/werkgever/tokens/summary?companyIds=` in `WerkgeverDashboardController` if composing them client-side needs > 2 calls.
- **Left:** the "Verbruik per vestiging" card (top 5 + "Overige {n} vestigingen" + a link to the full page), same table as 06.3.
- **Right, "Tokens kopen" card (BM):**
  - the 3 most relevant packs from `packs` (real prices, "€ x per token", the middle one pre-selected)
  - a primary "{n} tokens kopen · € {p} excl. btw" button that starts the **existing** checkout. **Don't change** Mollie, return URLs or price logic.
  - **"Wat kost wat?"** collapsible: all active `TokenSpendCost` rows from `costs` with plain-Dutch labels (Vacature publiceren, Verlengen, Uitlichten, Pushbericht naar kandidaten, Contact via talentpool, and from 07 "Kandidaatinzichten ontgrendelen")
  - The "Automatisch aanvullen instellen" link is **not built** (§0).
- **Below, "Recente facturen" (BM):** the last 3 from `billing-history`, with the pdf download and "Alle facturen" → `/werkgever/tokens/facturen`.
- **VM view:**
  - KPI's "Saldo vestiging {x}" (sub "toegewezen door de bedrijfsmanager") and "Verbruikt (30 dagen)"
  - the "Wat kost wat?" panel
  - primary **"Tokens aanvragen"** (06.6)
  - no buy card, no invoices
  - When the vestiging is **not** enterprise-managed and 01 kept a purchase path for it (see 01.6), show the buy card instead, per that rule.
- **RM view:** "Tokenverbruik regio" KPI's (used / allocated in own region) + the verbruik table read-only; no buy, allocate or invoices.
- The **Trackingcode & flyer** tab is removed (moved to Wervingsmateriaal in 05). The **Uitleg** tab content becomes the "Wat kost wat?" panel + `PageHelp`. The **Referral-overzicht** moves to Partnerprogramma (06.5).

## 06.3 `/werkgever/tokens/verbruik`: Verbruik per vestiging (BM full, RM read-only)
- `EntDataTable`: Vestiging (+ regio), Toegewezen, Verbruikt (period), Over, a usage bar (warning when Over ≤ 5), and a row action "Verdelen" (BM).
- **"Verdelen"** opens an `EntDrawer`:
  - from the central balance to this vestiging (or back), amount stepper, `EntImpactNote` "Centraal saldo na verdelen: {n}"
  - calls the **existing** `POST api/tokens/allocate`
  - Negative amounts only if `allocate` supports taking back today; otherwise leave it out.
- The token-management toggle (`UpdateTokenManagementAsync`: tokens central vs per vestiging) moves here as a settings row at the top: "Tokens beheren: centraal (aanbevolen) / per vestiging", with a confirm dialog explaining the effect. BM only.
- Period filter 30/90 d. `?node=` from the 05 tree.
- Delete `Regional/TokenControl.razor`. Its old URL already 301s (01).

## 06.4 `/werkgever/tokens/mutaties` and `/werkgever/tokens/facturen`
- **Mutaties** (the old Logging tab):
  - `EntDataTable` over `api/tokens/logs`: Datum, Omschrijving (plain Dutch per `TokenSpendReason`: Publiceren, Uitlichten, Pushbericht, Verlengen, Contact talentpool, Aankoop, Toegewezen, Goodwill, and from 07 "Kandidaatinzichten"), Vestiging, Door (actor name as today), Mutatie (+/−, tabular), Saldo na
  - filters type/vestiging/period, CSV export (client-side)
  - RM region read-only; VM own vestiging
- **Facturen** (BM only; D12):
  - `EntDataTable` from `billing-history`: Factuur, Datum, Omschrijving, Bedrag incl. btw, Status pill, pdf download (same endpoints)
  - above it, the moved **Factuurgegevens / Financiën** section from CompanyDetails (`UpdateBillingPreferenceAsync`)
  - Remove the 05 interim link on Profiel.

## 06.5 Partnerprogramma
- `/werkgever/partner` gets the `PartnerSales` page body + the Tokens "Referral-overzicht" tab (partner affiliate calls). `/werkgever/partner/uitbetalen` gets `PartnerSalesPayoutCheckoutStub`.
- Same role attributes as today (EnterpriseManager, Intermediary). Conditional nav item (§IA).

## 06.6 Tokenaanvraag (new, small)
- Entity `TokenRequest` (Core):
  - `Id`
  - `OrganisationCompanyId` (the wallet owner, resolved like `CandidateInsightsAccess.ResolveWalletCompanyId`)
  - `BranchCompanyId`
  - `RequestedByUserId`
  - `Amount` (1–500)
  - `Reason` (enum: `Publiceren`, `Verlengen`, `Uitlichten`, `Kandidaatinzichten`, `Overig`)
  - `Note?` (max 280)
  - `Status` (`Open`, `Toegewezen`, `Afgewezen`, `Ingetrokken`)
  - `HandledByUserId?`, `HandledAtUtc?`, `CreatedAtUtc`
  - Migration + indexes `(OrganisationCompanyId, Status)`.
- API in `WerkgeverTokenRequestsController` (`api/werkgever/token-requests`):
  - `POST` (VM for an own vestiging; RM 403; BM 400 "Je kunt zelf tokens verdelen")
  - `GET ?status=` (BM: organisation; VM: own requests)
  - `POST {id}/approve` (BM: allocates via the **existing** `ITokenLedgerService.AllocateAsync` in the same transaction and sets `Toegewezen`; idempotent on repeated calls)
  - `POST {id}/reject` (BM, optional reason)
  - `POST {id}/withdraw` (VM, own, while `Open`)
  - At most 3 `Open` requests per vestiging (409 `too_many_open_requests`).
- UI:
  - VM "Tokens aanvragen" drawer: amount, reason, note, `EntImpactNote` "De bedrijfsmanager krijgt je aanvraag in Te doen."
  - VM overview lists own requests with status.
  - BM gets the 02 Te doen kind **`TokenRequests`** (a new `ITodoSource`): "{vestiging} vraagt {n} tokens aan" + "Toewijzen" (opens the drawer prefilled: amount, central balance after) / "Afwijzen".
- Notifications:
  - BM: in-app via `IUserNotificationService` when a request is created
  - VM: in-app when it's handled
  - No e-mail in this stack (deferred).
- Also used by 03 (insufficient balance → "Tokens aanvragen") and 07 (VM unlock request, `Reason = Kandidaatinzichten`; 07 adds its own request type, see there).

## Tests
- Matrix rows:
  - `checkout`/`top-up-quote`: VM → 403, RM → 403, BM → 2xx
  - `allocate`: VM/RM → 403
  - `billing-history`/pdf: VM/RM → 403
  - token-requests: VM create own 2xx, VM create for a sibling vestiging 403, RM 403, BM approve 2xx, VM approve 403
- `TokenRequestServiceTests`:
  - approve allocates exactly once (twice → one ledger row)
  - approve with an insufficient central balance → 409 with nothing written
  - withdraw only when Open
  - the max-3-open rule
  - the Te doen item appears and disappears
- bUnit:
  - the overview per role (BM KPI's + buy card + invoices; VM no buy/invoices + request button; RM read-only)
  - "Wat kost wat?" renders from mocked costs (no hardcoded numbers)
  - the Verdelen drawer impact note
  - the facturen table + download
- `TokenControl.razor` is gone; its old URL 301s. The Tokens tabs Trackingcode/Uitleg/Referral are gone and their content lives at the new homes (checklist in the PR).
- Checkout: the existing checkout tests stay green unchanged.

## Success criteria
- d5 structure for BM.
- The VM can never buy and never sees invoices, but can ask in two clicks.
- The RM sees usage only.
- Nothing about payment or pricing changed.

## Done → next
Push, open the PR, note its number. Continue with **`07-kandidaatinzichten-premium.md`**. If anything is red, stop and report.

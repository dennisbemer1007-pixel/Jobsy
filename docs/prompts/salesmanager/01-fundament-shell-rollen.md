# 01. Foundation: 2FA, data model, SalesLayout + nav, /sales URLs, beneficiary scope, labels, Ambassadeur parked

Read `00-README.md` first (§0 shared rules, §IA, §R, §D, §P, Decisions, Dependencies). Branch `cursor/salesmanager-1` from `origin/acceptatie`.

> **Rules (same as README §0, repeated on purpose):**
> - Never merge, never deploy, never use rule `123` (`.cursor/rules/shortcut-123.mdc`).
> - Never push to `main` or `acceptatie`; push only this file's `cursor/salesmanager-*` branch; no force-push.
> - ONE stacked PR into `acceptatie`.
> - Red tests or an unmet success criterion: push, open the PR as **draft**, stop and report. Don't start the next file.
> - Beneficiary is resolved server-side from the signed-in user, no employer contact/candidate data in any portal DTO, money writes are idempotent (§0, §P, §R).

| | |
|---|---|
| Branch | `cursor/salesmanager-1` (from `origin/acceptatie`) |
| PR title | `feat(sales): foundation — 2FA for salesmanagers, partner data model, SalesLayout + /sales URLs, labels; park Ambassadeur` |
| PR body starts with | `Stacked on: none (first in the stack)` + the Dependencies outcome (A–H, which case applied) + the count of ambassadeur users/attributions/ledger lines kept (no personal data) |
| Mockups | shell only: top bar, sidebar, footer and mobile bottom nav of `sm-d1-dashboard.png` and `sm-m1-dashboard.png` |
| Split seam | **01a** = MfaPolicy + entities + migration + backfill + `SalesBeneficiary`/`SalesClock`/`SalesMoney` + labels + guards (01.2, 01.3, 01.6, 01.8, 01.9). **01b** = `SalesLayout` + `SalesNav` + page moves + 301s + strings module + CSS (01.4, 01.5, 01.7). **01c** = park the Ambassadeur role (01.10) |

## Goal
Everything later files build on. After 01, salesmanagers are forced through 2FA, the complete §D model exists (so later migrations stay small), every existing salesmanager page lives under `/sales/*` inside the new `SalesLayout` (content still the old components), the old URLs 301, and there's one place for beneficiary scope, money formatting, time boundaries and labels. The **Ambassadeur role is parked** behind `AmbassadorsEnabled` (default off): unreachable, no new commission, data kept (01.10). **No other behaviour change to commission or payouts yet.**

## 01.1 Today (verify first)
- Pages (all `[Authorize(Roles = "SalesManager")]`): `Components/Pages/SalesManager/Dashboard.razor` (`/salesmanager` → redirects to `/home`, which renders `Components/Home/SalesManagerHomePanel.razor` via `RoleHome.razor`), `SalesToolkit.razor` (`/salesmanager/toolkit`), `Referrals.razor` (`/salesmanager/referrals`), `Onboarding.razor` (`/salesmanager/onboarding`), `Invoices.razor` (`/salesmanager/invoices`), `PayoutCheckoutStub.razor` (`/salesmanager/payout-checkout`).
- Ambassadeur: `Components/Pages/Ambassadeur/Dashboard.razor` (`/ambassadeur` → `/home`, `AmbassadeurHomePanel.razor`), `Toolkit.razor`, `Finance.razor`, `Onboarding.razor`, `PayoutCheckoutStub.razor`, public `Landing.razor` (`/werven/{code}`, `/ambassadeur/ref/{code}`).
- `Components/Layout/SalesWalletChip.razor` (top bar "Wallet €" → invoices). `Navigation/RoleNavCatalog.cs` has `SalesManager` (L111) and `Ambassadeur` (L120) item arrays (snapshot-tested).
- `Jobsy.Core/Security/MfaPolicy.cs`: `IsRequired` = Admin + BranchManager/RegionalManager/EnterpriseManager/Intermediary. **SalesManager and Ambassadeur are missing.** Tests: `Jobsy.Tests/MfaForcedEnrollmentTests.cs`.
- Entities (§D "existing"): `CommissionLedgerEntry` (+ `CommissionEntryKind`), `SalesManagerProfile`, `AmbassadeurProfile`, `SelfBillingInvoice` (+ lines, `SalesManagerVatTreatment` in `VatDeclaration.cs`), `SalesManagerPayoutCheckout`, `SalesManagerApplication`, `SalesCommercialSettings`, `Company` (referral + commission snapshot fields L94–157).
- API: `Jobsy.Api/Controllers/SalesManagersController.cs` (`api/sales-managers`, `me/*` with `RequireSalesManager`), `AmbassadeursController.cs` (`api/ambassadeurs`), `SalesCommercialController.cs` (`api/sales-commercial`, anonymous `catalog` + `flyer.pdf`).
- Hardcoded Dutch and inline `style=` exist in the SalesManager pages; English "Suppliers" and raw `Kind` values are rendered in `SalesManagerHomePanel`.

## 01.2 2FA
- `MfaPolicy.IsRequired(UserRole.SalesManager) == true`. Also `IsRequired(UserRole.Ambassadeur) == true`, with the code comment `// Ambassadeur is parked (AmbassadorsEnabled = false); 2FA stays required when the role is re-enabled.` Nothing else in the MFA code changes.
- Tests (extend `MfaForcedEnrollmentTests`): a local-password salesmanager is forced to `/account/2fa/instellen` before `/sales`; an Entra/Google-signed-in one is not forced (ADR 0005); admin 2FA reset works for salesmanagers (existing flow, verify). A unit test pins `IsRequired(Ambassadeur) == true`.
- `docs/security/roles-matrix.md`: 2FA column "verplicht" for Salesmanager. The Ambassadeur row becomes **"Geparkeerd"**: "Rol geparkeerd (`AmbassadorsEnabled` uit). Geen toegang tot ambassadeur-functies; gegevens blijven bewaard. 2FA blijft verplicht als de rol weer aan gaat."
- Existing salesmanagers are forced at their next sign-in; mention it in the PR and `CHANGELOG.md`.

## 01.3 Entities + migration + backfill
- Add every §D change: new fields on `CommissionLedgerEntry`, `Company`, `SalesManagerProfile`, `AmbassadeurProfile`, `SalesCommercialSettings`, `SalesManagerApplication`, `SelfBillingInvoice`; new enum values (`CommissionEntryKind.RefundCorrection = 5`, `ChargebackCorrection = 6`, `SalesManagerVatTreatment.SmallBusinessScheme = 3`; append only); new entities `SalesSelfBillingConsent`, `SalesPayoutRequest`, `SalesPayoutRun`, `SalesAttributionChange`, `SalesLinkClickDaily`. Namespaces: `Jobsy.Core.Entities` (next to the existing sales entities), enums in `Jobsy.Core.Enums` (`CompanyLegalForm`, `SalesAttributionSource`, `SalesPayoutRequestStatus`, `SalesPayoutRunStatus`, `SalesLinkChannel`).
- `ISalesPayoutProfile` (Core) implemented by `SalesManagerProfile` and `AmbassadeurProfile`: `UserId`, `CompanyName`, `KvkNumber`, `VatNumber`, address fields, `Iban`, `VatTreatment`, `PayoutAccountHolderName`, `IbanChangedAtUtc`, `IbanPayoutHoldUntilUtc`, `EmailPrefsJson`, `TrackingCode`, `AgreementVersion`, `IsOnboardingComplete`.
- EF config: indexes and unique keys exactly as §D (filtered unique index for correction idempotency, one open payout request per beneficiary, one scheduled run per `RunDate`, unique `SelfBillingInvoice.InvoiceNumber` if missing). `SalesPayoutRequest.SalesPayoutRunId` `SetNull`; `CommissionLedgerEntry.SalesPayoutRequestId` `SetNull`; `CorrectsEntryId` `Restrict`.
- One migration `AddSalesPartnerFoundation`. Data backfill in the migration (SQL, idempotent):
  - `CommissionLedgerEntry.AvailableFromUtc` = `CreatedAt + 14 days` for `FounderBonus`, `TokenCommission`, `IndirectTokenCommission`; `= CreatedAt` for `Payout`, `Adjustment`.
  - `Company.SalesAttributedAtUtc` = `COALESCE(FirstYearStartedAt, PartnerReferredAtUtc)` where a salesmanager/ambassadeur is set; `SalesAttributionSource = Legacy`.
  - Everything else stays null/default. `CommissionStartsAtUtc` and year-2/3 snapshots are backfilled by **02** (it needs the purchase data and the rules).
- Guards green (`EfModelSnapshotTests`, `PendingModelChangesTests`, `EfMigrationDiscoveryTests`).

## 01.4 SalesLayout
- `Components/Layout/SalesLayout.razor` (+ `SalesLayout.razor.css` only if the design system allows scoped CSS; else `sales.css`): the §IA top bar (logo, "Lobsy Partner", role chip via `EntScopeChip` in fixed mode, search slot (empty until 04), `SalesWalletChipV2` "Beschikbaar € …" → `/sales/wallet`, help, bell, account menu with "2FA aan"), the grouped 248 px sidebar from `SalesNav`, the privacy footer line (§IA), breadcrumbs "Partner › {group} › {page}".
- The wallet chip reads **available** balance from `ISalesWalletReadService.GetAvailableAsync(beneficiary)`. In 01 this is today's balance (`CommissionLedgerService.GetBalanceExVatAsync`); 02 switches it to the derived state. It replaces `SalesWalletChip` (delete the old component and its test in 01; keep `SalesWalletChipTests` behaviour for the new chip).
- Mobile < 1024: sidebar becomes a sheet (from "Meer"); 5-item bottom nav (§IA; icons from `NavIcons`); the wallet chip shows the amount only.
- `lang="nl"`, no `LanguageSelector`.

## 01.5 SalesNav + page moves + 301s
- `Navigation/SalesNav.cs`: one catalog with groups, items, `IsAvailable`, visibility flags (`RequiresCanRecruit`), bottom-nav flags. Labels are `Sales.Nav.*` keys. Keep the item model role-agnostic (a `Roles` set per item, today only `SalesManager`) so a future ambassadeur stack can add items without reshaping it. Snapshot test. `RoleNavCatalog.SalesManager` returns an empty array (its nav now comes from `SalesNav`); `RoleNavCatalog.Ambassadeur` stays in the code but is never rendered while parked (01.10); existing snapshot tests for other roles unchanged.
- Move the existing pages to the §IA URLs under `SalesLayout`, **content unchanged** except removing the page-local chrome that duplicates the layout:
  - `/sales` renders `SalesManagerHomePanel` (04 replaces it).
  - `/sales/link` = `SalesToolkit` (05 redesigns).
  - `/sales/aanbevelen` = `Referrals`.
  - `/sales/start` = the salesmanager `Onboarding` (06 redesigns).
  - `/sales/wallet` = `Invoices` (07 redesigns); `/sales/wallet/uitbetalen` = the existing salesmanager payout-checkout stub (07 removes it).
  - The ambassadeur pages are **not** moved; they stay at `/ambassadeur/*` behind the parking gate (01.10).
- Post-login redirect for salesmanagers → `/sales` (`AuthRedirects` / `RoleHome`). `/home` for that role 301s to `/sales`.
- `SalesLegacyRoutes` (one table, §IA "Legacy URLs") mapped as 301s (query string preserved). Test every row.
- Onboarding gate: until `IsOnboardingComplete`, portal pages except `/sales/start`, `/sales/hulp`, `/sales/profiel` redirect to `/sales/start` (move today's check into one `SalesOnboardingGate`).
- `docs/ROUTES.md`, `PageSeoCatalog` (private, non-indexable), `PageHelpDocs` + `HowLobsyRoleGuides` (the salesmanager guide links `/sales/link`), `BlazorPageRoleAttributesTests` rows.

## 01.6 Scope, clock and money helpers (single source of truth)
- `Jobsy.Core/Sales/SalesBeneficiary.cs` + `Jobsy.Infrastructure/Sales/SalesBeneficiaryService.cs`: `GetOrThrow(ClaimsPrincipal)` → `(UserId, Kind (SalesBeneficiaryKind.SalesManager | Ambassadeur), TrackingCode?, IsOnboardingComplete, CanRecruit)`; it refuses the `Ambassadeur` kind while parked (01.10), but the type stays role-agnostic. `CanSeeCompany(beneficiaryId, companyId)` (company or its root is attributed to the beneficiary, directly or indirectly); `CanSeeInvoice`, `CanSeePayoutRequest`. Every later controller/page calls these; no inline role logic.
- `Jobsy.Core/Sales/SalesClock.cs`: `Today()` (Europe/Amsterdam), `ToLocal`, `EndOfLocalDayUtc`, `AddLocalDays` (for hold/IBAN boundaries), `NextWorkday`, `FirstWorkdayOfMonth(year, month)` with `DutchHolidays` (1 jan, Paasmaandag, Koningsdag (27 apr, 26 apr when 27 is a Sunday), Hemelvaart, Pinkstermaandag, 25–26 dec; Goede Vrijdag counts as a workday). Unit tests for 2026–2028.
- `Jobsy.Core/Sales/SalesMoney.cs`: `Format(decimal, SalesMoneyKind ExVat|InclVat|Plain)` → `€ 1.284,50`, `FormatSigned` → `+ € 43,75` / `– € 900,00`. Used everywhere in the portal, mails and PDFs.
- `Jobsy.Tests/Sales/SalesRightsMatrix.cs`: the data-driven table + runner (WebApplicationFactory). 01 fills the page rows (every `/sales/*` route × Candidate/werkgever roles/Admin/SalesManager/Ambassadeur/anonymous), the existing `api/sales-managers/me/*` rows, and the parked rows (every `/ambassadeur*` page and `api/ambassadeurs/*` endpoint → 404 for every actor while off, including Admin). Later files append.

## 01.7 Strings module + CSS
- `Localization/UiStringsSales.cs` + the parity exemption (§0 Strings). Move every hardcoded Dutch string of the moved pages that you touch in 01 (layout, nav, gate, 301 pages) into it; the page bodies are replaced in 04–09.
- `wwwroot/css/features/sales.css` created + linked + asset version.

## 01.8 Labels (D12)
- `Jobsy.Core/Sales/SalesLabels.cs` (keys) + nl texts in `UiStringsSales.cs`: `CommissionEntryKind` → "Commissie", "Founder-bonus", "Uitbetaling", "Correctie", "Commissie via aanbeveling", "Correctie: terugbetaling", "Correctie: chargeback"; states "In behandeling", "Beschikbaar", "Aangevraagd", "Uitbetaald", "Verrekend"; payout request/run statuses; invoice statuses "Concept", "Uitgereikt", "Betaald", "Geannuleerd"; VAT treatments "21 % btw", "Kleineondernemersregeling (KOR)"; attribution sources "Via code", "Via link", "Door Lobsy", "Eerder"; application statuses.
- Replace the raw `Kind`/status renders and "Suppliers" in the moved panels with these labels now (small, mechanical change).

## 01.9 Guards
- `SalesLabelsCompletenessTests`: every value of every enum listed in §0 has a non-empty nl label.
- `SalesPortalNoInlineStyleTests`: no `style="` in `Components/Pages/Sales/**`, `Components/Sales/**`, `Components/Layout/SalesLayout.razor` The moved legacy components go on a named allow-list; each later file removes the entries it replaces, and 09 asserts the list is empty.
- `SalesPortalDtoPrivacyTests`: reflection over every type in `Jobsy.Core.Contracts.Sales` (new namespace; later DTOs live there). Fail on property names matching `(?i)(kvk|address|adres|street|postal|postcode|email|phone|telefoon|contact|firstname|lastname|fullname|initials|vacancy|vacature|candidate|kandidaat|applicant)`, with the allow-list `Sales*Profile*` DTOs (own profile), `MaskedIban`, `CandidateCount`, `ApplicationCount`.
- `BlazorPageRoleAttributesTests` for all `/sales/*` pages.

## 01.10 Park the Ambassadeur role (D8)
- **Switch:** `PlatformFeatureSettings.AmbassadorsEnabled` (bool, default **false**) wired per Dependencies H (feature attribute or `AmbassadorsFeatureGate`; settings catalog entry or `/admin/settings` section). Help text: "Zet het ambassadeursprogramma aan of uit. Uit: geen pagina's, geen links, geen nieuwe commissie. Gegevens blijven bewaard." Changing it needs an MFA-verified admin session and is audited `sales.ambassadors.toggle` (Dependencies C).
- **Pages and menus (server-enforced):** every page under `Components/Pages/Ambassadeur/*` (dashboard, toolkit, finance, onboarding, payout checkout) answers 404 `feature_disabled` while off. `RoleNavCatalog.Ambassadeur`, the `/home` ambassadeur panel and the ambassadeur help guide (`HowLobsyRoleGuides`) are not rendered. `PageSeoCatalog`/`PageHelpDocs` entries stay (pages are private anyway); `docs/ROUTES.md` marks them "geparkeerd".
- **Link/cookie flow:** `/werven/{code}` and `/ambassadeur/ref/{code}` 302 to `/` without setting `lobsy_ambassadeur_ref`. `CompanyRegistrationService.ApplyAmbassadeurReferralAsync` and the candidate-side ambassadeur attribution (`User.ReferredByAmbassadeurUserId`, read in `Login.razor` / `AuthServiceCollectionExtensions.cs`) are skipped: `AM-` codes in the code field, an old cookie, or `/p/AM-…` (03) give **no** attribution and no error.
- **API:** every `api/ambassadeurs/*` endpoint (self-service **and** admin: invite, list, settings, commission-override, dashboards, flyers) answers 404 `feature_disabled`. Admin `/admin/ambassadeurs` and, when admin redesign 03 landed, its Ambassadeurs tab are hidden (tab not rendered, route 404).
- **Sign-in:** an account whose role is `Ambassadeur` is refused at sign-in (local password and external login) while off, with the login-page message from D8 and no session; the check lives in the one place that issues the auth cookie. Existing sessions of such accounts are signed out on their next request (a small check in the same pipeline, no change to `MfaEnforcementMiddleware`).
- **No new commission:** `RevenueShareService.ApplyTokenPurchaseShareAsync` and `CommissionLedgerService.TryCreditAmbassadeurTokenCommissionAsync` book **no** ambassadeur lines while off (return early, `PlatformLog` debug `sales.ambassadors.parked-skip` with checkout id only). Existing ambassadeur attributions on companies stay in the data but stop accruing; purchases while parked are never credited later (D8). Salesmanager commission on the same purchase is unaffected.
- **Data kept:** no deletion, no anonymisation, no migration that drops ambassadeur columns, rows or tables (`AmbassadeurProfile`, `AmbassadeurSettings`, `User.ReferredByAmbassadeur*`, `Company.ReferredByAmbassadeurUserId`, `CommissionAmbassadeurRateSnapshot`, ledger lines, invoices). A migration guard test asserts the ambassadeur tables and columns still exist.
- **Parked balances flag:** `ISalesParkedBalanceService.ListAsync()` → per ambassadeur user with a non-zero unpaid ledger balance: masked display name (like the admin users list), open balance ex VAT, last line date. `GET api/admin/sales/parked-balances` (`RequireAdmin`). Until 08 builds the panel, show one `EntImpactNote` on the admin payouts page that exists today (`/admin/financien/uitbetalingen` or `/admin/token-finance`): "{n} geparkeerde ambassadeurs hebben nog € {x} tegoed. Het programma staat uit; betaal of verreken dit met de hand." (hidden when n = 0).
- **Role-agnostic shared code:** the ledger, invoice, payout request/run and payout-profile code must not branch on `SalesManager` vs `Ambassadeur`; they work on a beneficiary user id and `ISalesPayoutProfile`. Only the gate (this section) and `SalesBeneficiary` know about parking. A test runs the payout-profile, ledger-balance and invoice code paths once with an `AmbassadeurProfile` fixture to prove re-enabling needs no shared-code change.

## Tests
- MFA forced enrollment for salesmanagers; external login not forced; `IsRequired(Ambassadeur)` pinned.
- Parking (switch off, default): every `/ambassadeur*` page and `api/ambassadeurs/*` endpoint → 404 for all actors; `/werven/{code}` → 302 without cookie; `AM-` code/cookie → no attribution; ambassadeur sign-in refused with the message, existing session ended; a token purchase by an ambassadeur-attributed company books no ambassadeur line (and the salesmanager line still books); nothing deleted (guard); parked-balance service numbers on a fixed data set. Switch on: today's ambassadeur behaviour works unchanged (smoke test of dashboard, landing cookie and one commission credit).
- Migration applies on an empty DB and on a copy of acceptatie's schema; backfill values on a seeded data set.
- 301 table; post-login redirect; onboarding gate.
- `SalesNav` snapshots (SM with and without `CanRecruitSalesManagers`); no item is visible to the Ambassadeur role while parked.
- `SalesClock` holidays/workdays; `SalesMoney` formatting (incl. negative, zero, thousands).
- Rights matrix rows from 01.6; guard tests 01.9; localization parity with the exemption; asset version; routes/SEO/help.

## Success criteria
- `dotnet build` + `dotnet test` green.
- The seed salesmanager (`sales@jobsy.local`, `SalesManagerDemoSeeder`) signs in, is forced through 2FA, lands on `/sales` inside the new layout with the role chip "Salesmanager SM-…" and the wallet chip; every old URL 301s to its new home.
- With `AmbassadorsEnabled` off (default), an ambassadeur account can't sign in (calm message), every ambassadeur URL 404s or redirects, no ambassadeur commission is booked, all ambassadeur data is still there, and the admin payouts page shows the parked-balance note when there is an open balance.
- No raw enum name or "Suppliers" is rendered on the moved pages.

Done → next: `02-commissie-motor.md`.

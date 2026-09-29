# 01. Foundation: 2FA for both roles, data model, SalesLayout + nav, /sales URLs, beneficiary scope, labels

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
| PR title | `feat(sales): foundation — 2FA for sales roles, partner data model, SalesLayout + /sales URLs, labels` |
| PR body starts with | `Stacked on: none (first in the stack)` + the Dependencies outcome (A–G, which case applied) |
| Mockups | shell only: top bar, sidebar, footer and mobile bottom nav of `sm-d1-dashboard.png` and `sm-m1-dashboard.png` |
| Split seam | **01a** = MfaPolicy + entities + migration + backfill + `SalesBeneficiary`/`SalesClock`/`SalesMoney` + labels + guards (01.2, 01.3, 01.6, 01.8, 01.9). **01b** = `SalesLayout` + `SalesNav` + page moves + 301s + strings module + CSS (01.4, 01.5, 01.7) |

## Goal
Everything later files build on. After 01, both roles are forced through 2FA, the complete §D model exists (so later migrations stay small), every existing salesmanager/ambassadeur page lives under `/sales/*` inside the new `SalesLayout` (content still the old components), the old URLs 301, and there's one place for beneficiary scope, money formatting, time boundaries and labels. **No behaviour change to commission or payouts yet.**

## 01.1 Today (verify first)
- Pages (all `[Authorize(Roles = "SalesManager")]`): `Components/Pages/SalesManager/Dashboard.razor` (`/salesmanager` → redirects to `/home`, which renders `Components/Home/SalesManagerHomePanel.razor` via `RoleHome.razor`), `SalesToolkit.razor` (`/salesmanager/toolkit`), `Referrals.razor` (`/salesmanager/referrals`), `Onboarding.razor` (`/salesmanager/onboarding`), `Invoices.razor` (`/salesmanager/invoices`), `PayoutCheckoutStub.razor` (`/salesmanager/payout-checkout`).
- Ambassadeur: `Components/Pages/Ambassadeur/Dashboard.razor` (`/ambassadeur` → `/home`, `AmbassadeurHomePanel.razor`), `Toolkit.razor`, `Finance.razor`, `Onboarding.razor`, `PayoutCheckoutStub.razor`, public `Landing.razor` (`/werven/{code}`, `/ambassadeur/ref/{code}`).
- `Components/Layout/SalesWalletChip.razor` (top bar "Wallet €" → invoices). `Navigation/RoleNavCatalog.cs` has `SalesManager` (L111) and `Ambassadeur` (L120) item arrays (snapshot-tested).
- `Jobsy.Core/Security/MfaPolicy.cs`: `IsRequired` = Admin + BranchManager/RegionalManager/EnterpriseManager/Intermediary. **SalesManager and Ambassadeur are missing.** Tests: `Jobsy.Tests/MfaForcedEnrollmentTests.cs`.
- Entities (§D "existing"): `CommissionLedgerEntry` (+ `CommissionEntryKind`), `SalesManagerProfile`, `AmbassadeurProfile`, `SelfBillingInvoice` (+ lines, `SalesManagerVatTreatment` in `VatDeclaration.cs`), `SalesManagerPayoutCheckout`, `SalesManagerApplication`, `SalesCommercialSettings`, `Company` (referral + commission snapshot fields L94–157).
- API: `Jobsy.Api/Controllers/SalesManagersController.cs` (`api/sales-managers`, `me/*` with `RequireSalesManager`), `AmbassadeursController.cs` (`api/ambassadeurs`), `SalesCommercialController.cs` (`api/sales-commercial`, anonymous `catalog` + `flyer.pdf`).
- Hardcoded Dutch and inline `style=` exist in the SalesManager pages; English "Suppliers" and raw `Kind` values are rendered in `SalesManagerHomePanel`.

## 01.2 2FA for both roles
- `MfaPolicy.IsRequired(UserRole.SalesManager) == true`, `IsRequired(UserRole.Ambassadeur) == true`. Nothing else in the MFA code changes.
- Tests (extend `MfaForcedEnrollmentTests`): a local-password salesmanager and ambassadeur are forced to `/account/2fa/instellen` before `/sales`; an Entra/Google-signed-in one is not forced (ADR 0005); admin 2FA reset works for both roles (existing flow, verify).
- `docs/security/roles-matrix.md`: 2FA column "verplicht" for both rows; the Ambassadeur row says "shares the Lobsy Partner pages (role chip)".
- Existing salesmanagers/ambassadeurs are forced at their next sign-in; mention it in the PR and `CHANGELOG.md`.

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
- `Navigation/SalesNav.cs`: one catalog with groups, items, `IsAvailable`, per-role visibility (`ForSalesManager`, `ForAmbassadeur`, `RequiresCanRecruit`, `RequiresAttributedCompanies`), bottom-nav flags. Labels are `Sales.Nav.*` keys. Snapshot test per role. `RoleNavCatalog.SalesManager`/`.Ambassadeur` return empty arrays (their nav now comes from `SalesNav`); existing snapshot tests for other roles unchanged.
- Move the existing pages to the §IA URLs under `SalesLayout`, **content unchanged** except removing the page-local chrome that duplicates the layout:
  - `/sales` renders a `SalesHomeHost` that shows `SalesManagerHomePanel` or `AmbassadeurHomePanel` by role (04 replaces both).
  - `/sales/link` hosts `SalesToolkit` or the ambassadeur `Toolkit` by role (05 unifies).
  - `/sales/aanbevelen` = `Referrals` (SalesManager only).
  - `/sales/start` = the salesmanager or ambassadeur `Onboarding` by role (06 redesigns).
  - `/sales/wallet` = `Invoices` (SM) / `Finance` (AM) by role (07 redesigns); `/sales/wallet/uitbetalen` = the existing payout-checkout stubs (07 removes them).
- Post-login redirect for both roles → `/sales` (`AuthRedirects` / `RoleHome`). `/home` for these roles 301s to `/sales`.
- `SalesLegacyRoutes` (one table, §IA "Legacy URLs") mapped as 301s (query string preserved). Test every row.
- Onboarding gate: until `IsOnboardingComplete`, portal pages except `/sales/start`, `/sales/hulp`, `/sales/profiel` redirect to `/sales/start` (move today's check into one `SalesOnboardingGate`).
- `docs/ROUTES.md`, `PageSeoCatalog` (private, non-indexable), `PageHelpDocs` + `HowLobsyRoleGuides` (the salesmanager guide links `/sales/link`), `BlazorPageRoleAttributesTests` rows.

## 01.6 Scope, clock and money helpers (single source of truth)
- `Jobsy.Core/Sales/SalesBeneficiary.cs` + `Jobsy.Infrastructure/Sales/SalesBeneficiaryService.cs`: `GetOrThrow(ClaimsPrincipal)` → `(UserId, Role, TrackingCode?, IsOnboardingComplete, CanRecruit)`; `CanSeeCompany(beneficiaryId, companyId)` (company or its root is attributed to the beneficiary, directly, indirectly or as ambassadeur); `CanSeeInvoice`, `CanSeePayoutRequest`. Every later controller/page calls these; no inline role logic.
- `Jobsy.Core/Sales/SalesClock.cs`: `Today()` (Europe/Amsterdam), `ToLocal`, `EndOfLocalDayUtc`, `AddLocalDays` (for hold/IBAN boundaries), `NextWorkday`, `FirstWorkdayOfMonth(year, month)` with `DutchHolidays` (1 jan, Paasmaandag, Koningsdag (27 apr, 26 apr when 27 is a Sunday), Hemelvaart, Pinkstermaandag, 25–26 dec; Goede Vrijdag counts as a workday). Unit tests for 2026–2028.
- `Jobsy.Core/Sales/SalesMoney.cs`: `Format(decimal, SalesMoneyKind ExVat|InclVat|Plain)` → `€ 1.284,50`, `FormatSigned` → `+ € 43,75` / `– € 900,00`. Used everywhere in the portal, mails and PDFs.
- `Jobsy.Tests/Sales/SalesRightsMatrix.cs`: the data-driven table + runner (WebApplicationFactory). 01 fills the page rows (every `/sales/*` route × Candidate/werkgever roles/Admin/SalesManager/Ambassadeur/anonymous) and the existing `api/sales-managers/me/*` / `api/ambassadeurs/me/*` rows. Later files append.

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

## Tests
- MFA forced enrollment for both roles; external login not forced.
- Migration applies on an empty DB and on a copy of acceptatie's schema; backfill values on a seeded data set.
- 301 table; post-login redirect; onboarding gate.
- `SalesNav` snapshots per role (SM with and without `CanRecruitSalesManagers`, AM with and without attributed companies).
- `SalesClock` holidays/workdays; `SalesMoney` formatting (incl. negative, zero, thousands).
- Rights matrix rows from 01.6; guard tests 01.9; localization parity with the exemption; asset version; routes/SEO/help.

## Success criteria
- `dotnet build` + `dotnet test` green.
- The seed salesmanager (`sales@jobsy.local`, `SalesManagerDemoSeeder`) signs in, is forced through 2FA, lands on `/sales` inside the new layout with the role chip "Salesmanager SM-…" and the wallet chip; every old URL 301s to its new home.
- An ambassadeur sees the same shell with "Ambassadeur AM-…" and without "Salesmanager aanbevelen".
- No raw enum name or "Suppliers" is rendered on the moved pages.

Done → next: `02-commissie-motor.md`.

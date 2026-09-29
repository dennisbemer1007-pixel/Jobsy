# Lobsy Partner (Salesmanager): Cursor run book

Cursor: **read this file completely**, then **execute the files below strictly in order**, one at a time. Each file is one PR.

**What this stack builds:** a redesign of the salesmanager role into one clean partner portal, **Lobsy Partner**, and it **parks** the Ambassadeur role until Dennis defines it.
- **Salesmanager** (`SalesManager`, **mandatory 2FA**) shares a personal link / code / QR with employers. When a referred employer buys tokens, the salesmanager earns commission: **25 % · 10 % · 5 %** over 3 years, and the 3 years start at the **first purchase**. They see an honest dashboard (earnings, funnel, referred employers), a sales toolkit with real prices, and a wallet. Commission waits **14 days** ("In behandeling"), then becomes available. From **€ 50** they request a payout; **Lobsy admin approves** one **monthly run** and pays by bank transfer (SEPA file) until Mollie payouts exist. Lobsy creates the invoice for them (**self-billing**, with recorded consent, "factuur uitgereikt door afnemer" and a **KOR** option).
- **Ambassadeur is parked (D8).** A server-enforced admin switch `AmbassadorsEnabled` (default **off**) hides every ambassadeur page, menu item, link/cookie flow, admin invite/approve screen and dashboard, and stops new ambassadeur commission. All existing ambassadeur data is kept, open balances are flagged for admin in Financiën, and the shared ledger/invoice/payout code stays role-agnostic, so turning the role back on later is simple.
- **Lobsy admin** approves payout runs, exports the SEPA file, marks invoices paid, books corrections and reassigns attributions (with a reason), inside the admin redesign's **Gebruikers & rollen › Sales & ambassadeurs** (Salesmanagers tab; the Ambassadeurs tab is hidden while parked) and **Financiën › Uitbetalingen & btw** (Dependencies B).
- **Privacy by design:** a salesmanager sees a referred employer's **trade name, place and their own commission only**. They see no contacts, no KvK, no address, no vacancies and no candidates, and an eenmanszaak shows its trade name only. Link clicks are counted per day without IP or device data. Third-party data from "Salesmanager aanbevelen" has a retention period.

## Order

| # | File | Branch | Branches from | PR into |
|---|---|---|---|---|
| 01 | `01-fundament-shell-rollen.md`: 2FA mandatory for SalesManager (`MfaPolicy`; Ambassadeur keeps it for when re-enabled), **Ambassadeur parked behind `AmbassadorsEnabled` (default off)**, full data model + migration, `SalesLayout` ("Lobsy Partner", role chip, wallet chip), one nav catalog `SalesNav.cs`, `/sales/*` URLs + 301s (existing pages moved, not yet redesigned), `SalesBeneficiary` scope service, rights-matrix scaffold, label catalog (no raw enum names), nl-only strings module, CSS | `cursor/salesmanager-1` | `origin/acceptatie` | `acceptatie` |
| 02 | `02-commissie-motor.md`: window starts at first purchase, all three rates snapshotted at activation, the company's 15 % bonus tokens only inside the window, one window per organisation (fixes the org + vestiging double count), 14-day hold, refunds/chargebacks → negative corrections (Mollie webhook), manual admin correction API, backfill | `cursor/salesmanager-2` | `cursor/salesmanager-1` | `acceptatie` |
| 03 | `03-attributie-klikken.md`: 30-day first-click cookie, `/p/{code}` short link, the typed code wins, attribution survives external login, self-referral guard, admin reassign with reason (API + history), daily click counters for the funnel | `cursor/salesmanager-3` | `cursor/salesmanager-2` | `acceptatie` |
| 04 | `04-dashboard-werkgevers.md`: Dashboard (KPIs, commission per month, funnel, Te doen, best employers), Mijn werkgevers list + detail drawer (privacy-safe DTO, eenmanszaak rule), top-bar search | `cursor/salesmanager-4` | `cursor/salesmanager-3` | `acceptatie` |
| 05 | `05-link-materiaal.md`: Mijn link & materiaal (link, code, QR, share, "Bekijk wat de werkgever ziet"), materials (flyer, visitekaartje, prijskaart, presentatie, e-mail, WhatsApp), pitch, real prices from token packs, flyer endpoint fix | `cursor/salesmanager-5` | `cursor/salesmanager-4` | `acceptatie` |
| 06 | `06-profiel-afspraken-iban.md`: Profiel & gegevens (company, btw / KOR, payout account, agreements, security, e-mail preferences), separate self-billing consent, IBAN mod-97 + 2FA step-up + notification mail + 3-day hold, `/sales/start` onboarding redesign, Hulp & afspraken | `cursor/salesmanager-6` | `cursor/salesmanager-5` | `acceptatie` |
| 07 | `07-wallet-uitbetalen.md`: Wallet & uitbetalingen (balances by status, Mutaties / Uitbetalingen / Facturen), payout request (≥ € 50) with invoice preview, cancel request, the self-complete stub removed, invoice PDF legal text + KOR, jaaroverzicht PDF | `cursor/salesmanager-7` | `cursor/salesmanager-6` | `acceptatie` |
| 08 | `08-admin-uitbetaalrondes.md`: monthly payout run (job on the 1st workday), admin approve/reject per line, invoices issued on approval, SEPA pain.001 export + manual bank-transfer list, mark paid (existing), IBAN-hold flags, correction and reassign UI, payout provider seam for Mollie later | `cursor/salesmanager-8` | `cursor/salesmanager-7` | `acceptatie` |
| 09 | `09-aanbevelen-avg-opruimen.md`: Salesmanager aanbevelen redesign with third-party notice + retention, privacy statement paragraph, old salesmanager pages/endpoints removed (parked ambassadeur code stays), docs | `cursor/salesmanager-9` | `cursor/salesmanager-8` | `acceptatie` |

If a file is too big for one reviewable PR (> ~1.500 changed lines excluding tests/migrations), split it into `a`/`b` at the seam the file names. The next file then branches from the **last** sub-branch (e.g. `cursor/salesmanager-2b`).

## Pointer prompt (the only prompt needed; it runs 01 … 09)
```
Run the Salesmanager stack. First: git fetch origin && git show origin/docs/salesmanager:docs/prompts/salesmanager/00-README.md — read it completely.
Then read and execute each file in docs/prompts/salesmanager/ on that branch strictly in the order the README's table lists (01 … 09; a/b splits where a file allows it), one file = one PR.
File 01 branches from origin/acceptatie; every later file branches from the previous file's branch (stacked). Each opens ONE PR into acceptatie whose body starts with "Stacked on #<prev PR>".
Before 01, run the dependency checks in the README's "Dependencies" section and follow the fallback it prescribes for each one; say in PR 01 which case applied.
Build and test after each file; if tests fail or a success criterion can't be met, push, open that PR as draft, stop and report — don't start the next file.
Never merge, never deploy, never use rule 123, never push to main or acceptatie, no force-pushes.
At the end report: file → branch → PR number → status, plus anything deferred.
```

## How to run
1. `git fetch origin`. Read this file, `.cursor/rules/design-system.mdc`, `docs/ROUTES.md`, `docs/security/roles-matrix.md`, `docs/adr/0004-roles-and-scope.md`, `docs/adr/0005-mfa-local-only.md` and `docs/release-flow.md`.
2. Run the **Dependencies** checks below and note the outcome (it goes into PR 01).
3. For each file in the order above:
   1. Read the whole file.
   2. Create its branch from the "Branches from" column. File 01: `git checkout -b cursor/salesmanager-1 origin/acceptatie`. Later files: `git checkout -b <branch> <previous branch>` with the previous branch pushed.
   3. Implement **only** that file's scope, plus the shared rules below.
   4. Run `dotnet build` and `dotnet test`, and the Playwright suites the file names if you can. Everything must be green and the file's **success criteria** must hold.
   5. Small, clear commits. Push (`git push -u origin <branch>`, only `cursor/*` branches). Open **ONE PR into `acceptatie`** with the title from the file. Body starts with `Stacked on #<prev PR> (<prev branch>)`, then the PR body items from §0.
   6. Note the PR number, go on to the next file.
4. **Stop and report** when tests fail and you can't fix them inside the file's scope, when a success criterion can't be met, or when the code contradicts this spec in a way you can't resolve safely. Push what you have, open that PR as **draft** with the failure described, don't continue.
5. At the end, report the table file → branch → PR number → status (green or draft/red), plus anything deferred.
6. **Never** merge, deploy, or use rule `123` (`.cursor/rules/shortcut-123.mdc`). **Never** push to `main` or `acceptatie`. No force-pushes.
   - Migrations: each file adds its own migration on top of the previous one. Never regenerate or edit a lower file's migration.
   - If `acceptatie` moves during the run: don't rebase. Only when a conflict blocks you, `git merge origin/acceptatie` into the current branch (a normal merge commit) and say so in the PR body.

---

## §0. Shared rules (every file)
- **Branches stack** (see above). **ONE PR per file, always into `acceptatie`.** Body starts with `Stacked on #<prev PR> (<prev branch>)`; file 01 says `Stacked on: none (first in the stack)`. The diff includes lower PRs until they merge; say which commits are this file's own.
- **Never** merge, deploy, or use rule `123`. **Never** push to `main` or `acceptatie`. No force-pushes.
- **Stop on red.** `dotnet build` + `dotnet test` after each file. If red and not fixable in scope: stop, push, draft PR with the failure, report.
- Code references are from `origin/acceptatie` @ `a611db40` (2026-09-29 18:29 CEST), which includes the merged 2FA work (`MfaPolicy`, `MfaEnforcementMiddleware`, forced enrollment, admin 2FA reset). Re-check line numbers before editing.
- **Mockups:** branch `docs/salesmanager`, folder `docs/mockups/salesmanager/`. Read with `git fetch origin docs/salesmanager && git show origin/docs/salesmanager:docs/mockups/salesmanager/<file> > /tmp/<file>` and open `/tmp/<file>`. Don't commit mockups to code branches.
  - Desktop 1440×900: `sm-d1-dashboard.png`, `sm-d2-link-materiaal.png`, `sm-d3-werkgevers-detail.png` (list + drawer), `sm-d4-wallet.png`, `sm-d5-uitbetaling-aanvragen.png` (drawer with invoice preview), `sm-d6-profiel-gegevens.png`.
  - Mobile 390 wide @2x: `sm-m1-dashboard.png`, `sm-m2-wallet.png`, `sm-m3-link-qr.png`.
  - The mockups are a **layout and copy reference**. Persona (Tom Hendriks, Hendriks Sales & Advies), employers, amounts and dates are **Voorbeelddata**. The "Voorbeelddata" pill is mockup-only. "Nieuw" tags in the mockups mark features that don't exist today; they are **not** rendered in the product.
  - **Where the mockup and this spec differ, this spec wins.** Known differences:
    - **Invoice numbers** in the mockups read `LOB-SB-2026-0042`. Keep the existing format `SB-{year}-{seq:0000}` (`SelfBillingInvoiceService.NextInvoiceNumberAsync`).
    - **Short link** `lobsy.nl/p/SM-K7Q2MP`: `/p/{code}` is a new short alias (03) that 302s to `/partner/{code}`; both work.
    - **Package names** in the drawer ("Pakket Groei · 50 tokens") come from the real purchase (the `TokenPricing` pack size or the `SalesPackage` name); invent no package names.
    - **"+38 % t.o.v. 2025"** and **"+2 deze maand"** deltas: computed from the ledger / attribution dates; hidden when the comparison period has no data.
    - **"Vrij op 10-10"** under a pending line = `AvailableFromUtc` (02) shown as a date in Europe/Amsterdam.
    - **Mobile bottom nav** has 5 items (Overzicht, Mijn link, Werkgevers, Wallet, Meer); keep 5.
    - The **"Presentatie voor een klant"** material is a generated PDF (05), not a slide file.
    - **Commission sub-line** on `sm-d2` says "Jaar 1 start op de dag van aanmelding"; the product says "Jaar 1 start bij de eerste aankoop" (D1).
    - **Payout amount** on `sm-d5` is an editable field with "Alles"; the product always requests the **full** available amount (read-only field, 07.2).
    - **"Bekijk openbare vacatures"** in the `sm-d3` drawer footer is **not built** (D4: no vacancy details in the portal); only "Vraag Lobsy om hulp" stays.
    - **"Nieuw"** tags (e.g. on KOR, "Veilig wijzigen", the funnel) are mockup-only markers of new features.
- **Design system.** Follow `.cursor/rules/design-system.mdc` plus these rules, which win when they conflict:
  - **Enterprise visual language** (as the admin and werkgever redesigns and scholen): 56 px `--brand-deep` top bar, 248 px grouped sidebar, breadcrumbs, desktop list density (44 px rows, `--text-sm`, tabular numbers, amounts right-aligned). Reuse the shared enterprise primitives (Dependencies A). Mobile < 1024: same pages, stacked, with the 5-item bottom nav + "Meer" sheet (01.5). Never create a second variant of a table/drawer/tabs/KPI primitive.
  - **Money:** always `€ 1.284,50` (nl-NL, non-breaking space after €), always labelled **excl. btw** or **incl. btw**, `font-variant-numeric: tabular-nums`. Positive ledger amounts in `--success`, negative in the default text colour with a "–" sign (never red for normal payouts; red only for corrections that make the balance negative). One formatter: `SalesMoney.Format(decimal, SalesMoneyKind)` in Core.
  - **Hero card:** the one dark "Beschikbaar" card (`--brand-deep`) per page, on Dashboard and Wallet only. All other cards are light.
  - **Colours:** tokens only (`app.css :root`), no new hex values, **no inline `style=""` in `.razor`** (guard test, 01.9). Status pills pair colour **and** a label.
  - **Type:** weights 400/600 (700 only for the page `h1`); only the type scale; spacing from `--space-*`.
  - **Layout:** breakpoints 640/900/1024 only; logical properties; chevrons flip under `[dir="rtl"]`.
  - **Calm UI:** one primary action per card/drawer/screen; destructive actions are outline-danger, the confirming button inside the dialog is filled danger; no decorative emoji.
- **Strings (D10):** **Dutch only for now, through the localization pattern.** All new portal text via `@Culture["…"]` in a new nl-only module `Localization/UiStringsSales.cs` (prefixes `Sales.`, `SalesAdmin.`, `SalesMail.`, `SalesPdf.`). Register it like the other modules (`UiStrings.cs`). Add **one** named exemption in `LocalizationParityReportTests` (`Every_key_exists_in_all_languages` + `Identical_to_nl_counts…`) for exactly these prefixes, with a comment pointing to D10. Other languages fall back to nl for these keys: verify `UiStrings.Get` falls back to nl; if it doesn't, add the fallback **only** for these prefixes. Portal pages render `lang="nl"` and hide the `LanguageSelector`. **No hardcoded Dutch in markup, services or PDFs.** The public landings (`/partner/{code}`, `/werven/{code}`) stay multilingual with their existing keys.
- **No raw enum names in the UI (D12).** Every enum shown to users goes through `SalesLabels` (01.8): `CommissionEntryKind`, the derived `CommissionEntryState`, `SalesPayoutRequestStatus`, `SalesPayoutRunStatus`, `SelfBillingInvoiceStatus`, `SalesManagerVatTreatment`, `SalesAttributionSource`, `SalesManagerApplicationStatus`. Reflection test: every value has a non-empty nl label; bUnit test: no rendered portal page contains an enum member name.
- **Terminology (nl final):**

  | Use | Instead of |
  |---|---|
  | Salesmanager | Sales manager, SM (in UI text) |
  | Werkgever | Supplier, leverancier, klant (in labels) |
  | Commissie | Provisie, fee, revenue share |
  | In behandeling · Beschikbaar · Aangevraagd · Uitbetaald | Pending, uninvoiced, balance |
  | Uitbetaling aanvragen | Uitbetalen, checkout |
  | Uitbetaalrekening | IBAN-gegevens, bankrekening (as a title) |
  | Correctie | Adjustment, chargeback (as a label; the sub-line says why) |
  | Mijn link / jouw code | Trackingcode, referral code |
  | Aanmelding | Registratie, signup |
  | Self-billing (Lobsy maakt je factuur) | Creditnota |

- **Authorization: server first (§R).** Every portal page carries `[Authorize(Roles = "SalesManager")]` (or the narrower §R row). Ambassadeur pages and APIs are additionally behind the `AmbassadorsEnabled` gate (01.10). Every `api/sales/me/*` endpoint resolves the beneficiary from the **signed-in user** via `SalesBeneficiary` (01.6), never from a route or body value. Admin money actions (approve run, mark paid, correction, reassign) require `RequireAdmin` **and** an MFA-verified session (`PersonalDataAccessLogExtensions.IsMfaVerifiedInSession`, or an external IdP login per ADR 0005). The UI hiding a button is **never** the only guard. The matrix test (01) enforces it.
- **Money correctness:** amounts are `decimal`, rounded once per ledger line with `MidpointRounding.AwayFromZero` to 2 decimals (reuse `SalesCommissionRules.ShareEuro`). Every write that moves money is idempotent (unique keys, §D) and runs in one DB transaction. Times are stored in UTC, shown in Europe/Amsterdam; day boundaries (hold end, run date, IBAN hold) are computed in Europe/Amsterdam via one `SalesClock` helper (01.6).
- **Audit:** admin money actions and reassignments go through `IAdminAuditLog` (admin redesign 07) when it exists, else an interim structured `PlatformLog` row (Dependencies C). An admin viewing a salesmanager's IBAN or invoices writes `PersonalDataAccessLog` (resource `sales.payout-account`, actions `view`/`export`).
- **E-mail:** new mails go through the existing e-mail catalog (`EmailCatalogService`) with nl templates from `UiStringsSales.cs` (`SalesMail.*`), plain and calm, no marketing. Security mails (IBAN changed, 2FA) are always sent; the other mails follow the preferences from 06.
- **Docs and guards to update when routes change:** `docs/ROUTES.md` (`RoutesDocFreshnessTests`), `Seo/PageSeoCatalog.cs` (portal pages private, non-indexable; `PageSeoTests`), `Help/PageHelpDocs.cs` + `Help/HowLobsyRoleGuides.cs` (`PageHelpDocsTests`), `BlazorPageRoleAttributesTests`, `docs/security/roles-matrix.md`, `CHANGELOG.md`.
- **CSS:** new `wwwroot/css/features/sales.css` (BEM block `sp-…`), linked in `Components/App.razor` (normal list **and** `<noscript>`) with `?v=YYYYMMDD-sales`, added to `Jobsy.Tests/asset-versions.json` (`AssetVersionGuardTests`). Don't append to `app.css`. Shared enterprise primitives keep their own stylesheet.
- **Migrations:** `dotnet ef migrations add …` in `Jobsy.Infrastructure`, snapshot updated. `EfModelSnapshotTests`, `PendingModelChangesTests`, `EfMigrationDiscoveryTests` stay green.
- **Must NOT touch:**
  - the Mollie **checkout** creation, return URLs and token price logic (`TokenPricing` values, `TokenPurchaseFulfillmentService` crediting). 02 only **reads** refund/chargeback amounts in the webhook path and books corrections.
  - the werkgever partner programme (BM/IM partners, `PartnerAffiliateService`, the 0.5-token reward, `/werkgever/partner` from werkgever redesign 06); it keeps its own flow. Shared code (`/partner/{code}` landing, code field at registration) must keep partner codes working exactly as today.
  - candidate and employer navigation catalogs, `MfaEnforcementMiddleware` and the MFA pages (only `MfaPolicy` gets the two roles), the cookie banner, `app-core.js`
  - the parked ambassadeur code and data: no deletion, no migration that drops columns/rows, no refactor beyond the gate (01.10) and the role-agnostic interfaces
  - price data: no merge of `SalesPackage` / `TokenPricing` / `SalesCommercialSettings.BaseTokenValueEuro` (admin redesign D10); 05 only changes what the sales surfaces **display**
- **PR description:**
  - what changed and why
  - screenshots desktop 1440 and mobile 390 of each new or changed screen
  - the new URL list (+ 301s)
  - the §R rows this PR added or changed
  - for money changes: a worked example table (input → ledger lines)
  - test list
  - "Out of scope / deferred"
- **Run** `dotnet build` and `dotnet test` (unit + bUnit). Run the Playwright suites you touched if you can; say so if you couldn't.

---

## §IA. Information architecture and URLs (the contract for all files)

**Label = page `h1` = breadcrumb**, from one catalog `Navigation/SalesNav.cs` (01). Items whose page arrives in a later file are present with `IsAvailable = false` and **not rendered** until that file flips them. Breadcrumb root is "Partner".

### Top bar
Logo + product label **"Lobsy Partner"** · role chip (fixed, not a switcher): **"Salesmanager SM-K7Q2MP"** (code in mono; "Nog geen code" before onboarding) · search "Zoek een werkgever…" (04; Ctrl K) · wallet chip **"Beschikbaar € 1.284,50"** → `/sales/wallet` (replaces `SalesWalletChip`) · help · notifications (existing bell) · account menu (name, role, "2FA aan", Uitloggen).

### Salesmanager (`SalesLayout`)
| Group | Item (nl) | URL | Built in |
|---|---|---|---|
| Overzicht | Dashboard | `/sales` | 01 move, 04 content |
| Verkopen | Mijn link & materiaal | `/sales/link` | 01 move, 05 |
| | Mijn werkgevers | `/sales/werkgevers` (+ `?open={companyId}` drawer, `?q=`) | 04 |
| | Salesmanager aanbevelen | `/sales/aanbevelen` (only when `CanRecruitSalesManagers`) | 01 move, 09 |
| Geld | Wallet & uitbetalingen | `/sales/wallet` (`?tab=mutaties\|uitbetalingen\|facturen`) | 01 move, 07 |
| | (no nav item) Uitbetaling aanvragen | `/sales/wallet/uitbetalen` (drawer over the wallet; deep link works) | 07 |
| Account | Profiel & gegevens | `/sales/profiel` | 06 |
| | Hulp & afspraken | `/sales/hulp` | 06 |
| (no nav item) | Starten (onboarding: gegevens, afspraken, code) | `/sales/start` | 01 move, 06 |

Sidebar footer (shield icon): "Je ziet bedrijfsnamen en je eigen commissie. Geen contactpersonen, kandidaten of vacaturedetails."
Until onboarding is complete, every portal page except `/sales/start`, `/sales/hulp` and `/sales/profiel` redirects to `/sales/start` (today's behaviour, kept).

### Ambassadeur (parked, D8)
No pages in this stack. While `AmbassadorsEnabled` is **off** (default), 01.10 makes every ambassadeur surface unreachable:

| Surface | While off |
|---|---|
| `/ambassadeur`, `/ambassadeur/*` (dashboard, toolkit, finance, onboarding, payout checkout) | 404 `feature_disabled` |
| Public `/werven/{code}`, `/ambassadeur/ref/{code}` | 302 to `/` without setting a cookie; no click counted |
| `AM-` codes on `/p/{code}`, in the registration code field and in an existing `lobsy_ambassadeur_ref` cookie | ignored (no attribution, no error for the user) |
| Nav items, `/home` panel, post-login redirect for the `Ambassadeur` role | not rendered; sign-in of an Ambassadeur-only account is refused with a calm message (01.10) |
| `api/ambassadeurs/*` (self-service **and** admin invite/settings/override/list/dashboard) | 404 `feature_disabled` |
| Admin `/admin/ambassadeurs` and the admin redesign's Ambassadeurs tab | 404 / tab hidden |
| Admin Financiën | shows the **parked balances** flag (01.10, 08) |

When the switch is turned **on** later, today's ambassadeur pages come back unchanged; redesigning them onto the Lobsy Partner pages is a future stack (see D8 for the notes to carry over).

### Legacy URLs (301, via one `SalesLegacyRoutes` table, 01)
| Old | New |
|---|---|
| `/salesmanager`, `/home` (for the SalesManager role) | `/sales` |
| `/salesmanager/toolkit` | `/sales/link` |
| `/salesmanager/referrals` | `/sales/aanbevelen` |
| `/salesmanager/onboarding` | `/sales/start` |
| `/salesmanager/invoices` | `/sales/wallet?tab=facturen` |
| `/salesmanager/payout-checkout` | `/sales/wallet/uitbetalen` |

The `/ambassadeur*` URLs are **not** redirected into `/sales`; they follow the parked rules above.

Public URLs stay: `/partner/{code}` (salesmanager and BM/IM partner landing). New: `/p/{code}` (03). The ambassadeur landings are parked (above).

### Lobsy admin
| Item | URL | Built in |
|---|---|---|
| Salesmanagers (list, detail drawer: profile, attributed employers, ledger, "Toewijzing wijzigen", "Correctie boeken") | tab Salesmanagers on `/admin/gebruikers/sales` when admin redesign 01/03 landed, else today's `/admin/sales-managers` (Dependencies B). The Ambassadeurs tab / `/admin/ambassadeurs` is hidden while parked | 03 API, 08 UI |
| Uitbetaalrondes (runs, approve/reject, SEPA export, mark paid) | tab **Rondes** on `/admin/financien/uitbetalingen` when admin redesign 06 landed, else a tab **Uitbetalingen** on `/admin/sales-managers` (Dependencies B) | 08 |
| Geparkeerde ambassadeurs met saldo (flag: masked name, open balance, last line date) | `/admin/financien/uitbetalingen` (or the fallback payouts tab) | 01 note, 08 panel |
| Switch `AmbassadorsEnabled` | admin settings catalog group "Sales" or today's `/admin/settings` (Dependencies H) | 01 |
| Sales settings (hold days, minimum, IBAN hold days, run day) | the existing sales settings (`/admin/sales` or its admin-redesign home, Financiën › Prijzen & pakketten › Sales) | 02, 08 |

### API prefixes
`api/sales/me/*` (SalesManager; beneficiary = signed-in user) · `api/admin/sales/*` (`RequireAdmin`; money actions + MFA session) · existing `api/sales-managers/*` keeps working until 09 removes the self-service endpoints no page uses (admin endpoints stay) · `api/ambassadeurs/*` stays in the code but answers 404 `feature_disabled` while parked · public `api/sales-commercial/*` (catalog, generic flyer).

---

## §R. Rights matrix (server-side contract; 01 encodes it, every file keeps it true)

● = allowed · ◯ = own scope only · — = 403. "Own" = the signed-in user is the beneficiary (`SalesBeneficiary`). **Ambassadeur (parked):** every row is — (404 `feature_disabled` / sign-in refused) while `AmbassadorsEnabled` is off; the matrix test has an Ambassadeur actor that proves it.

| Page / action | Lobsy Admin | Salesmanager | Werkgever (any role) | Kandidaat / anonymous |
|---|---|---|---|---|
| Portal pages `/sales/*` | — (admin uses the admin pages) | ◯ | — | — |
| Dashboard numbers, ledger, invoices, runs of a beneficiary | ● (admin pages) | ◯ | — | — |
| Referred employers: trade name, place (not for eenmanszaak), attribution date, status, own commission per purchase | ● | ◯ attributed to me | — | — |
| Employer KvK, address, contacts, users, vacancies, candidates | ● (existing admin pages) | — | own company | — |
| Personal link, code, QR, personal flyer and materials | — | ◯ | — | — |
| Generic flyer (no code), public landing, `/p/{code}` | ● | ● | ● | ● |
| Recommend a salesmanager | — | ◯ if `CanRecruitSalesManagers` | — | — |
| Approve / reject a salesmanager application | ● | — | — | — |
| Request a payout (≥ minimum, consent, complete profile) / cancel own open request | — | ◯ | — | — |
| Approve / reject run lines, SEPA export, mark paid | ● + MFA session | — | — | — |
| Book a manual correction (±, reason) | ● + MFA session | — | — | — |
| Reassign an attribution (reason) | ● + MFA session | — | — | — |
| Edit own company data, btw / KOR, e-mail preferences, give self-billing consent | — (read-only in admin) | ◯ | — | — |
| Change the payout IBAN (2FA step-up) | — | ◯ | — | — |
| See the full IBAN | — (masked; `MaskedIban` only) | — (masked; own last 4) | — | — |
| Download invoices / jaaroverzicht | ● all | ◯ | — | — |
| Commercial settings (rates, hold, minimum) | ● | — | — | — |
| Switch `AmbassadorsEnabled`; see the parked-balance flag | ● (+ MFA session for the switch) | — | — | — |

The matrix test is data-driven from **one** table in `Jobsy.Tests/Sales/SalesRightsMatrix.cs` (page route × role → allow/deny; endpoint × actor → expected status, including **other beneficiary's company**, **other beneficiary's invoice**, **werkgever of the referred company**, **admin without MFA session** cases). Each later file adds its rows.

---

## §D. Data model (01 builds it; later files only add what they name)

All ids `Guid`, times UTC, money `decimal(18,2)`, rates `decimal(9,4)`. The beneficiary column on existing tables stays named `SalesManagerUserId` and also holds the (parked) ambassadeurs' lines (D11).

| Entity | Fields | Notes |
|---|---|---|
| `CommissionLedgerEntry` (existing) | + `AvailableFromUtc` (DateTime), `SalesPayoutRequestId?`, `CorrectsEntryId?` (self FK, `Restrict`), `SourceRefundKey?` (≤ 80), `Reason?` (≤ 500), `CreatedByUserId?`. Enum `CommissionEntryKind` + `RefundCorrection = 5`, `ChargebackCorrection = 6` (append; `Adjustment = 3` = manual correction) | Backfill `AvailableFromUtc = CreatedAt + 14 days` for commission kinds, `= CreatedAt` for `Payout`/`Adjustment`. Filtered unique index `(SourceTokenCheckoutId, SalesManagerUserId, Kind, SourceRefundKey)` where `SourceRefundKey IS NOT NULL`. State is **derived**, not stored (02.4) |
| `Company` (existing) | + `CommissionStartsAtUtc?`, `CommissionYear2RateSnapshot?`, `CommissionYear3RateSnapshot?`, `SalesAttributedAtUtc?`, `SalesAttributionSource?` (enum `TypedCode/LinkCookie/Admin/Legacy`), `LegalForm?` (enum `Eenmanszaak/Vof/Bv/Nv/Stichting/Vereniging/Other`, null = unknown) | Window and snapshots live on the **organisation root** (`ParentCompanyId ?? Id`, D1). `FirstYearStartedAt` keeps its founder-slot meaning only |
| `SalesManagerProfile`, `AmbassadeurProfile` (existing) | + `VatTreatment` (`SalesManagerVatTreatment`, default `Standard21`), `VatTreatmentChangedAtUtc?`, `PayoutAccountHolderName?` (≤ 70), `IbanChangedAtUtc?`, `IbanPayoutHoldUntilUtc?`, `EmailPrefsJson` (new employer, commission available, payout status; default all on) | Both implement `ISalesPayoutProfile` (Core) so the payout code stays role-agnostic; one `SalesPayoutProfileService` works on either (06). Only salesmanagers use it while the ambassadeur is parked. IBAN stays encrypted via the existing `IbanValueConverter` |
| `SalesManagerVatTreatment` (existing enum) | + `SmallBusinessScheme = 3` (nl "Kleineondernemersregeling (KOR)") | `ReverseCharge`/`Exempt` stay but are not offered in the UI |
| `SalesCommercialSettings` (existing) | + `CommissionHoldDays` (int, default 14, 0–60), `PayoutMinimumEuro` (decimal, default 50, 0–1000), `IbanChangeHoldDays` (int, default 3, 0–14), `AttributionCookieDays` (int, default 30, 1–90) | Edited with the existing sales settings (02 / 08); validated server-side |
| `PlatformFeatureSettings` (existing) | + `AmbassadorsEnabled` (bool, default **false**) | The parking switch (01.10, D8). Nullable in the PUT = keep |
| `SalesSelfBillingConsent` (new) | `Id`, `UserId`, `Version` (e.g. `2026-10-self-billing-v1`), `TextSha256`, `AcceptedAtUtc`, `RevokedAtUtc?` | Separate from the agreement (D5). Latest non-revoked row with the current version = consent present |
| `SalesPayoutRequest` (new) | `Id`, `BeneficiaryUserId`, `AmountExVat`, `VatTreatment`, `VatAmount`, `TotalInclVat`, `MaskedIban`, `Status` (`Requested/InRun/Approved/Rejected/Paid/Cancelled`), `RequestedAtUtc`, `SalesPayoutRunId?`, `SelfBillingInvoiceId?`, `RejectionReason?`, `DecidedAtUtc?`, `DecidedByUserId?`, `PaidAtUtc?` | Max **one** open request (`Requested/InRun/Approved`) per beneficiary (filtered unique index) |
| `SalesPayoutRun` (new) | `Id`, `RunDate` (DateOnly, Europe/Amsterdam), `IsExtra` (bool), `Status` (`Draft/Approved/Exported/Paid/Closed`), `CreatedAtUtc`, `CreatedByUserId?` (null = job), `ApprovedAtUtc?`, `ApprovedByUserId?`, `ExportedAtUtc?`, `ExportFileSha256?`, `ProviderKey` (`bank-transfer`) | Filtered unique `RunDate` where `IsExtra = false` (one scheduled run per month); extra runs by hand (08) |
| `SalesAttributionChange` (new) | `Id`, `CompanyId` (root), `FromUserId?`, `ToUserId?`, `Source` (`Admin`), `Reason` (5–500), `ChangedByUserId`, `ChangedAtUtc` | History for reassignments (03) |
| `SalesLinkClickDaily` (new) | `BeneficiaryUserId`, `Date` (DateOnly, Europe/Amsterdam), `Channel` (`Link/Qr/Flyer/Other`), `Count` | PK all but `Count`. **No IP, no user agent, no cookie id** stored (03) |
| `SalesManagerApplication` (existing) | + `SubjectNotifiedAtUtc?`, `SubjectObjectedAtUtc?`, `PersonalDataClearedAtUtc?`, `ReferrerConfirmedPermission` (bool), `CandidateEmailSha256?` (for the 60-day duplicate rule after clearing); enum `SalesManagerApplicationStatus` + `Expired` (append) | Retention in 09 |
| `SelfBillingInvoice` (existing) | + `SelfBillingConsentId?`, `SalesPayoutRequestId?`; unique index on `InvoiceNumber` (add if missing, with retry on conflict) | `VatTreatment` finally honoured (07) |

---

## §P. Privacy and security rules (every file)
- **What a beneficiary sees (D4):** per referred employer only `DisplayName` (trade name = `Company.Name` of the organisation root), `Place` (city from the root's address) **only when `LegalForm` is known and not `Eenmanszaak`**, attribution date, status, commission year + rate, and their **own** commission lines (date, package label, purchase amount excl. btw, own commission, state). Never KvK, address, contact person, e-mail, phone, users, vacancies, applications or candidates. One DTO `SalesEmployerDto` (Core contracts) is the only shape the portal gets; a reflection guard (01.9) fails on forbidden property names.
- **Parked ambassadeur data (D8):** users, profiles, ledger lines, invoices and attributions are **kept as they are**; nothing is deleted or anonymised by this stack (existing retention rules still apply). While parked, the only place it shows is the admin parked-balance flag (masked names, amounts).
- **Link clicks (03):** counted per beneficiary, day and channel. No IP, user agent, fingerprint or cookie id is stored. The attribution cookie holds only the code.
- **Money security (D6):** 2FA is mandatory for salesmanagers (and stays required for ambassadeurs, for when the role is re-enabled). An IBAN change needs a 2FA step-up, sends a notification mail and holds payouts to the new account for 3 days. Admin money actions need an MFA-verified session. The full IBAN is never returned by any API after it is saved (masked only).
- **Retention (D13):** "Salesmanager aanbevelen" data (name, e-mail, motivation of the recommended person) is cleared: **60 days** after creation if still pending (the application expires), **30 days** after rejection, **30 days** after an approved application is provisioned. Invoices, ledger and payout records are kept **7 years** (fiscal bewaarplicht; same as admin redesign D11). Click counters are kept **25 months**, then deleted. All via the existing `DataRetentionHostedService`.
- **Account deletion:** keep today's `PrivacyDataService` behaviour (attribution cleared when a salesmanager deletes their account); invoices and ledger rows stay (fiscal) but the beneficiary's name on them is replaced by the invoice snapshot fields already stored on the invoice.

---

## Decisions (Dennis approved D1–D8 on 29-09; the "Default" sub-bullets and D9–D16 are defaults Dennis can override)
- **D1. Commission model.** 25 % (year 1) · 10 % (year 2) · 5 % (year 3) of every token purchase excl. btw by a referred employer. A salesmanager recommended by another salesmanager gets 20 % in year 1 and the recommender 5 % extra in year 1 (existing rules). The 3-year window (1095 days) starts at the employer's **first credited token purchase** ("activation"), not at registration. **All three rates (plus indirect rate and duration) are snapshotted at activation.** The company's **15 % bonus tokens** are granted only for purchases inside that window. The founder bonus (20 % of the € 2.500 start package, slots 1–10) stays as today. *(Dennis, 29-09)*
  - *Default:* the window is per **organisation**: the root company (`ParentCompanyId ?? Id`) holds `CommissionStartsAtUtc` and the snapshots; a purchase by any vestiging of that organisation counts in the same window. This is also the fix for the double count.
  - *Default (legacy):* for companies that already have credited purchases, `CommissionStartsAtUtc` = their first credited purchase (always ≥ registration, so no salesmanager loses commission); existing year-1/indirect snapshots are kept; year-2/3 snapshots are filled from the current settings. Booked ledger lines are never recomputed.
- **D2. Attribution.** A **30-day first-click cookie** on the salesmanager link; a code **typed at registration wins** over the cookie; self-referral is blocked (same user, same KvK as the salesmanager's profile, or same non-freemail e-mail domain); admin can reassign with a reason. *(Dennis, 29-09)*
  - *Default:* the cookie works for salesmanager and BM/IM partner codes on `/partner/{code}` and `/p/{code}`; resolution of what a code means stays as today (SM / BM / IM). `AM-` codes are ignored while the ambassadeur is parked (D8). A reassignment affects **future** purchases only.
- **D3. Payouts.** Commission is **"In behandeling" for 14 days** (refund window); refunds and chargebacks create a **negative correction**. From **€ 50 available** the beneficiary requests a payout. **One monthly run on the 1st workday**, **approved by Lobsy admin** in Financiën › Uitbetalingen. Payment by **bank transfer (SEPA file)** until Mollie payouts exist. *(Dennis, 29-09)*
  - *Default:* no automatic payout request; requests made before the run is created join it; admin may create an **extra run** by hand. "Workday" = Monday–Friday, skipping 1 January and the fixed/Easter-based Dutch public holidays (one `DutchHolidays` helper). The run is created at 06:00 Europe/Amsterdam.
- **D4. What a salesmanager sees.** Company trade name + place + own commission per purchase (package + amount). No contact persons, candidates or vacancy details; an eenmanszaak shows its trade name only (no place). *(Dennis, 29-09)*
  - *Default:* the place is shown only when `LegalForm` is known and not an eenmanszaak (unknown = name only). `LegalForm` is filled from the KvK lookup when the response has it (03.6), otherwise stays null.
- **D5. Tax and self-billing.** Keep self-billing, with **explicit, separately recorded consent** and the legal text **"Factuur uitgereikt door afnemer"**. Support **21 % btw or KOR (no btw)**. Private persons without KvK are not supported. *(Dennis, 29-09)*
  - *Default:* with KOR the btw number is optional and the invoice says "Btw-vrijgesteld op grond van de kleineondernemersregeling (KOR)". All invoice texts are reviewable strings (`SalesPdf.*`) and the PR asks Dennis to have the accountant check them.
- **D6. Security.** 2FA mandatory for SalesManager (`MfaPolicy`); Ambassadeur stays in `MfaPolicy` so 2FA is required the moment the role is re-enabled. IBAN change: 2FA code, notification mail, next payout to the new account waits 3 days. *(Dennis, 29-09)*
  - *Default:* users who sign in with Microsoft/Google (no local TOTP, ADR 0005) confirm an IBAN change through a one-time e-mail link (30 min) instead of a TOTP code; the same notification and hold apply.
- **D7. Menu structure** as in phase 1 (§IA): Overzicht · Verkopen · Geld · Account. *(Dennis, 29-09)*
- **D8. Ambassadeur is parked** until Dennis defines the role. Server-enforced switch `AmbassadorsEnabled` (default **off**) hides every ambassadeur page, menu item, link/cookie flow, admin invite/approve screen and dashboard (routes 404 or redirect, §IA). No new ambassadeur commission is booked and existing ambassadeur attributions stop accruing. Existing data is kept (no deletion, no dropping migration). Ambassadeur users can't use ambassadeur features; open balances are flagged for admin in Financiën. Shared ledger/invoice/payout code stays role-agnostic. 2FA stays required for the role. `docs/security/roles-matrix.md` shows Ambassadeur as "Geparkeerd". *(Dennis, 29-09)*
  - *Default:* sign-in of an account whose only role is `Ambassadeur` is refused on the login page with "Het ambassadeursprogramma is gepauzeerd. Je gegevens en je tegoed blijven bewaard. Vragen? Mail {support}." (no session, so no forced 2FA enrollment for a feature that's off); existing sessions of such accounts are ended on their next request.
  - *Default:* purchases made while parked are **never** credited retroactively when the switch goes on again; commission resumes only for purchases after re-enabling.
  - *Default:* open balances of parked ambassadeurs are **not** paid automatically; admin sees them flagged and settles them by hand (outside this stack) or waits for the future ambassadeur stack. Refund/chargeback corrections (02) still apply to lines booked before parking, so balances stay correct.
  - *Notes for the future ambassadeur stack (not built here):* reuse the Lobsy Partner pages with a role chip; show referred candidates as **counts only** (today's dashboard shows initials, a phase-1 GDPR finding); tier card instead of 25/10/5; tell candidates who arrive via an ambassadeur link.
- **D9. URL prefix `/sales/*`** (role-neutral, so a re-enabled ambassadeur can share it; `/partner/*` is already the public landing and the werkgever partner programme). *(default)*
- **D10. Dutch only**, via an nl-only localization module (§0 Strings), so other languages can be added later. *(default, same as scholen D12)*
- **D11. No rename** of `CommissionLedgerEntry.SalesManagerUserId` / `SelfBillingInvoice.SalesManagerUserId` in this stack (migration churn for no user value). Code talks about "beneficiary" through `SalesBeneficiary`; a `// beneficiary: SalesManager (or a parked Ambassadeur)` comment on the property. *(default)*
- **D12. Labels:** no raw enum names anywhere; one `SalesLabels` catalog. *(default)*
- **D13. Retention periods** as in §P (aanbevelen 60/30/30 days, fiscal records 7 years, clicks 25 months). *(default)*
- **D14. Prices on sales surfaces** come from the active `TokenPricing` packs ("vanaf € 3,00 per token excl. btw", per-pack prices) via one `SalesPriceQuote` service; `BaseTokenValueEuro` is no longer shown on the toolkit, flyer or `/partner` landing. No price data changes. *(default, follows the "use the real token price" instruction and admin D10)*
- **D15. The recommended person is informed:** when a salesmanager recommends someone, Lobsy sends that person one short info mail with a "Verwijder mijn gegevens" link, and the salesmanager must tick "Deze persoon weet dat ik hem of haar aanmeld." *(default; GDPR art. 14)*
- **D16. Refunded token purchases:** this stack corrects **commission** only. Reversing the purchased tokens or the 15 % bonus tokens after a refund is **not** built (no refund handling exists for tokens today); 02 logs a `PlatformLog` warning `sales.refund.tokens-not-reversed` so admin can act by hand. *(default; flagged for Dennis)*

## Dependencies (check before 01; say in PR 01 which case applied)
- **A. Shared enterprise UI primitives** (admin redesign 01 / werkgever redesign 01). Check: `git ls-tree -r --name-only origin/acceptatie -- Jobsy.Web/Components/Ui/Enterprise Jobsy.Web/Components/Admin/Ui`.
  - **`Components/Ui/Enterprise/` exists:** reuse `EntDataTable`, `EntFilterBar`, `EntPager`, `EntTabs`, `EntKpiCard`, `EntDrawer`, `EntImpactNote`, `EntScopeChip`.
  - **Only `Admin*` exists:** move them to `Components/Ui/Enterprise/` exactly as werkgever README Dependencies B prescribes (own first commit, `git mv`, no behaviour change).
  - **Neither exists:** build the minimal `Ent*` set under `Components/Ui/Enterprise/` as werkgever 01.2 describes, and say in PR 01 that the admin/werkgever stacks must consume it.
  - Either way: **one** set.
- **B. Admin redesign 01/03/06 (admin shell, Sales & ambassadeurs, Uitbetalingen & btw).** (The admin Ambassadeurs tab is hidden while parked, 01.10.) Check: `git grep -n '"/admin/gebruikers/sales"' origin/acceptatie -- Jobsy.Web` and `git grep -n '"/admin/financien/uitbetalingen"' origin/acceptatie -- Jobsy.Web`.
  - **Present:** 08 adds the tab **Rondes** to `/admin/financien/uitbetalingen` (before the existing tabs) and the drawer actions to `/admin/gebruikers/sales`. The existing Uitbetalingen tab's "Markeer als betaald" (admin 06.4) keeps working and now also closes the matching payout request.
  - **Absent:** 08 adds a tab **Uitbetalingen** to today's `/admin/sales-managers` (`SalesManagersAdmin.razor`) with the same components (`Components/Admin/Sales/PayoutRunsSection.razor`, `…/AttributionSection.razor`), each marked `// moves to /admin/financien/uitbetalingen (admin-redesign 06.4)` / `// moves to /admin/gebruikers/sales`. The components are self-contained so the admin stack only has to host them.
  - **Either way:** this stack **adds an approve step** that admin redesign 06.4 doesn't have ("no approve step exists" there). 08's PR body contains the note "Admin redesign 06.4 must host `PayoutRunsSection` in a tab Rondes and keep mark-paid closing payout requests". If the admin stack hasn't run yet, 08 also adds that note as one line under the admin finance route in `docs/ROUTES.md`. Don't edit the admin spec branch.
- **C. Admin redesign 07 (audit log).** Check: `git grep -n "interface IAdminAuditLog" origin/acceptatie -- Jobsy.Core`. **Present:** `[AdminAudit("sales.payout.run.approve")]`, `sales.payout.mark-paid`, `sales.ledger.correction`, `sales.attribution.reassign`, `sales.settings.update`. **Absent:** interim structured `PlatformLog` row with the same action names.
- **D. 2FA.** `MfaPolicy` (`Jobsy.Core/Security/MfaPolicy.cs`), `TotpAuthenticator` (`Jobsy.Core/Security/TotpAuthenticator.cs`) and the MFA-verified session claim (`JobsyClaimTypes.MfaVerified`, `IsMfaVerifiedInSession`) are present at `a611db40`. 01 only adds the two roles (Ambassadeur for when it is re-enabled); 06 builds the step-up on `TotpAuthenticator` the way `POST api/admin/users/{userId}/mfa/reset` verifies a fresh code. If any moved, keep the semantics and say so.
- **E. Mollie refund data.** Check `PaymentStatusResult` (`Jobsy.Core/Interfaces/IPaymentService.cs`, today `PaymentId, Status, IsPaid, Method`) and what `MolliePaymentService.GetPaymentStatusAsync` reads. **The Mollie client exposes `amountRefunded` / `amountChargedBack`:** extend the record with `AmountRefundedEuro` and `AmountChargedBackEuro` (default 0). **It doesn't:** read both fields from the payment JSON in `MolliePaymentService` (same request, no extra call). The stub (`MolliePaymentStub`) gets test hooks to set both amounts. Never change checkout creation.
- **F. QuestPDF + QRCoder** are present (`Directory.Packages.props`; `QRCoder` 1.6.0 is used by `AmbassadeurFlyerPdfService` and `MarketingFlyerPdfService`). 05 (materials) and 07 (invoice, jaaroverzicht) use them. Extract one `SalesQr.Png(url, pixelsPerModule)` helper from the existing flyer code; add no second QR library.
- **G. Scholen / other stacks.** If `docs/adr/0006-*.md` already exists, the ADR in 09 takes the next free number. Never branch from an unmerged branch of another stack.
- **H. Feature switch + settings catalog** (for `AmbassadorsEnabled`). Check: `git grep -n "class RequiresFeatureAttribute" origin/acceptatie -- Jobsy.Core` and `git grep -n "class PlatformSettingsCatalog" origin/acceptatie -- Jobsy.Web`.
  - **`RequiresFeatureAttribute` present:** add `PlatformFeature.Ambassadors` and put `[RequiresFeature(PlatformFeature.Ambassadors)]` on every ambassadeur page and controller. **Absent:** an `AmbassadorsFeatureGate` (endpoint filter + page base check) reading `AmbassadorsEnabled`, plus a reflection guard test that every type under `Components/Pages/Ambassadeur` and `AmbassadeursController` is gated.
  - **Settings catalog present:** add the entry to group **"Sales"** (create the group if missing). **Absent:** add a "Sales" section to today's `/admin/settings` (`SettingsAdmin.razor`) backed by `PlatformFeatureSettings.AmbassadorsEnabled` + `PUT api/settings/platform-features` (nullable = keep), marked `// moves into PlatformSettingsCatalog group "Sales"`.
  - Either way the switch is read server-side on every request (cached with the existing settings cache); the UI never decides alone.
- **Recommended landing order:** admin redesign 01 → 03 → 06 (optional) → this stack. Not a hard requirement: every case above has a fallback.

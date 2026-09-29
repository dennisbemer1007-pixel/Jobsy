# 05 · Organisatie: Vestigingen & regio's, Team & rechten (one invite flow), Bedrijfsprofiel split, Koppelingen, Wervingsmateriaal

> Read `00-README.md` first. §IA, §R and D6, D12, D13 and D14 apply.

| | |
|---|---|
| Branch | `cursor/werkgever-redesign-5` from `cursor/werkgever-redesign-4` (stacked) |
| PR | ONE PR into `acceptatie`: `feat(werkgever): organisatie tree, one invite flow and Bedrijfsprofiel split`. Stacked on #<PR of 04> (`cursor/werkgever-redesign-4`) |
| Mockups | `bm-d4-vestigingen-team.png` (tree + team + invite drawer) |
| Split seam (if too big) | 5a = Vestigingen & regio's + Team & rechten + invite drawer; 5b = CompanyDetails split into Bedrijfsprofiel / Koppelingen / Wervingsmateriaal |

**Goal.** One place for the organisation structure and who can do what. One way to invite. The 1.173-line Bedrijfsgegevens page becomes three clear pages.

## 05.1 Today (verify first)
- `Branches.razor` (384 lines):
  - `GetMyCompaniesAsync`
  - KvK add via `GetKvkEstablishmentsAsync` + `RegisterEstablishmentAsync` (`POST api/companies/from-kvk`)
  - **two** inline invite forms (`InviteCompanyUserAsync`)
- `Regions.razor` (330 lines): `RegionsController` CRUD (`EnterpriseManager`, `Admin`) + its own invite form.
- `Users.razor` (625 lines): `GetCompanyUsersAsync`, `UpdateCompanyUserAsync` (`PUT api/company-users/{id}`), `InviteCompanyUserAsync` (`POST api/company-users/invite`). `CompanyUsersController` = `EnterpriseManager`, `Intermediary`, `Admin`. Role labels are Bedrijfsmanager / Regiomanager / Vestigingsmanager.
- `CompanyDetails.razor` (1.173 lines, sections by line):
  - Bedrijfsidentiteit ~L90
  - Wervingsmateriaal ~L122 (`RaamflyerTools`)
  - Contactvoorkeur ~L132 (`UpdateContactPreferenceAsync`, `UpdateEmailVerificationPreferenceAsync`)
  - Financiën/facturatie + Factuurhistorie ~L222/245 (`UpdateBillingPreferenceAsync`, `GetCompanyBillingHistoryAsync`, `DownloadCompanyBillingInvoicePdfAsync`)
  - Wervingsvoorkeuren ~L281
  - CSV Batch Import ~L308 (`UpdateCsvBatchImportAsync`)
  - API-koppeling ~L342 (`GetCompanyApiKeysAsync`, `GenerateCompanyApiKeyAsync`, `DeactivateCompanyApiKeyAsync`, `EmailCompanyApiKeyCredentialsAsync`)
- Flyers: `EmployerFlyersController` (`branch/{companyId}.pdf`, `overview.pdf`). The Tokens tab "Trackingcode & flyer" has the tracking code.
- `User` has `LastLoginAtUtc` and `IsActive`, but **no invite-state field**. Check what `GetCompanyUsersAsync` returns for pending invites.

## 05.2 `/werkgever/organisatie/vestigingen`: Vestigingen & regio's (redesign)
- `WgPageShell`:
  - Title "Vestigingen & regio's"
  - Lead "Je organisatie, regio's, vestigingen en wie waar bij kan."
  - Actions: "Exporteren" (CSV), primary "Iemand uitnodigen" (opens the 05.4 drawer; BM only)
- Two columns ≥ 1024 (300 px tree | detail):
  - **Tree** (`WgOrgTree.razor`, `role="tree"`, keyboard arrows):
    - the organisation (count) › regio's (count) › vestigingen
    - a pill "Geen manager" (warning) on vestigingen without an active VM
    - footer rows "Regio toevoegen" (BM, only with ≥ 2 vestigingen) and "Vestiging toevoegen via KvK" (BM; opens today's KvK flow in an `EntDrawer`)
    - Selection is URL-synced `?node=org|region:{id}|vestiging:{id}`
    - A flat list < 1024.
  - **Detail card** for the selected node:
    - header with icon, name, sub-line ("5 vestigingen · 20 live vacatures"), "Bewerken" (BM: region name / vestiging data using the existing endpoints)
    - `EntTabs`: **Vestigingen** (child list with live vacancies, manager, token balance) · **Team** (people with access to this node; same table as 05.3, filtered) · **Tokens** (a link-through summary to `/werkgever/tokens/verbruik?node=…`)
- `?tab=regios` (old `/employer/regions`) opens the tree with the regions expanded and the org node selected. Region CRUD moves into the tree (add/rename/delete with a confirm; delete keeps vestigingen, which fall back to "Zonder regio"). `Regions.razor` is deleted.
- **RM:** read-only. The tree shows own region(s) only, with no add rows, "Bewerken" or invite. **VM:** 403 (§R).

## 05.3 `/werkgever/organisatie/team`: Team & rechten (redesign of Users)
- Title "Team & rechten". Lead "Wie heeft toegang, met welke rol en voor welke vestigingen." Primary "Iemand uitnodigen".
- `EntDataTable`:
  - columns **Naam** (avatar, name, e-mail), **Rol** (pill: Bedrijfsmanager info / Regiomanager line / Vestigingsmanager neutral), **Bereik** ("Alle vestigingen" / "Regio {x}" / vestiging name(s)), **Status**, **Actief** (relative `LastLoginAtUtc`), row menu
  - Status: Actief / Gedeactiveerd, plus "Uitgenodigd" **only if** the API exposes a pending invite; otherwise don't invent one
  - filters Rol, Vestiging, Status, search
- Row menu (BM):
  - "Rol of bereik wijzigen" (drawer, `PUT company-users/{id}`)
  - "Uitnodiging opnieuw sturen" (only for invites, if an endpoint exists; otherwise leave it out and say so)
  - "Deactiveren" / "Activeren" (existing `IsActive` update, confirm)
  - Never "Verwijderen" unless an endpoint exists today.
- Guard: the last active Bedrijfsmanager can't be deactivated or demoted (server check if missing: 409 `last_enterprise_manager`, plus a test).
- What each role may do is explained in a collapsible "Wat mag elke rol?" panel, rendered from the same role-card copy as the drawer.

## 05.4 One invite drawer (D6)
- `Components/Werkgever/Team/WgInviteDrawer.razor` (`EntDrawer`), **the only invite UI** in the product for employers. Open it from Team & rechten, Vestigingen & regio's, the Te doen item `NoManager` (`?invite=vestiging:{id}` prefill) and the dashboard.
- Fields:
  - **E-mailadres**
  - **Rol**: three radio cards with the exact copy
    - "Bedrijfsmanager — Alles voor alle vestigingen: vacatures goedkeuren, tokens kopen en verdelen, facturen, team en bedrijfsprofiel."
    - "Regiomanager — Kijkt mee in de vestigingen van één regio. Alleen lezen: geen wijzigingen, geen tokens kopen."
    - "Vestigingsmanager — Alle rechten, maar alleen voor de eigen vestiging: vacatures, sollicitaties, tokens en profiel."
  - **Bereik**: select Vestiging (VM) / Regio (RM), hidden for BM
  - privacy note: "Kandidaatgegevens volgen de privacyregels: naam na accepteren, contactgegevens pas na aanname."
- Footer: primary "Uitnodiging versturen" (`POST api/company-users/invite`, the same payload as today; map role/scope to the existing request fields) + "Annuleren". On success: toast + the row appears in Team.
- **Remove** the invite forms from `Branches` and `Regions` (and any other page: `rg -n "InviteCompanyUserAsync" Jobsy.Web`, which must hit only the drawer after this PR, guard test).
- Server: `CompanyUsersController` stays EnterpriseManager/Intermediary/Admin. Matrix rows: RM/VM → 403 on invite and update. Validate server-side that the scope belongs to the inviter's organisation (add it if missing).

## 05.5 CompanyDetails split (D13)
Extract the sections into `Components/Werkgever/Sections/*Section.razor` **moved, not rewritten** (strings localized on the way). Then delete `CompanyDetails.razor`.
- **`/werkgever/organisatie/profiel`**, Bedrijfsprofiel (VM: **Vestigingsprofiel**, scoped to their own vestiging), with `EntTabs`:
  - **Profiel**: Bedrijfsidentiteit (name, KvK, address, logo/description as today)
  - **Contact & kanalen**: contact preference + e-mail verification preference
  - **Wervingsvoorkeuren**
  - **Cultuur**: the `CultureScan` body (moved in 01)
  - One "Opslaan" per tab (sticky footer on mobile). Unsaved-changes guard when switching tabs.
- **`/werkgever/koppelingen`**, Koppelingen (BM only; conditional nav item), with `EntTabs`:
  - **API**: keys table, generate, deactivate (confirm, danger), e-mail credentials, endpoint docs link
  - **CSV-import**: the toggle (`UpdateCsvBatchImportAsync`) + the moved `CsvImport.razor` body (old `/employer/csv-import` → `?tab=csv`)
- **`/werkgever/wervingsmateriaal`**, Wervingsmateriaal:
  - raamflyer per vestiging (`RaamflyerTools`, `EmployerFlyersController` pdfs), overview pdf, tracking code + QR (moved from the Tokens tab "Trackingcode & flyer", which is removed in 06)
  - RM: download only; VM: own vestiging
- **Financiën/facturatie + Factuurhistorie** → moved to **06** (Tokens › Facturen). In this PR, the Profiel page shows a small link "Facturen en factuurgegevens staan nu bij Tokens & facturen", so nothing disappears between 05 and 06.

## 05.6 Salaristabellen and Overnames
- Salaristabellen: keep the page and move it into `WgPageShell`. The VM view is **read-only** (no edit/create/delete rendered, server rejects VM mutations; verify the `SalaryTables` endpoints and tighten if needed, listing that in the PR).
- Overnames: into `WgPageShell`, "Beoordelen" actions stay; the VM sees own-vestiging requests only.

## Tests
- `WgOrgTreeTests`: tree shape per role (BM all, RM own region, VM → page 403), the "Geen manager" pill, keyboard navigation, `?node=` sync.
- `WgInviteDrawerTests`: the three role cards and copy, scope field per role, the payload maps to the existing request, success adds the row, validation (e-mail, scope required).
- `InviteSingleEntryGuardTests`: `InviteCompanyUserAsync` is referenced only from `WgInviteDrawer`.
- Matrix rows:
  - RM/VM → 403 on `company-users/invite`, `company-users/{id}`, regions CRUD, `companies/from-kvk`, company updates of another company, API keys, csv toggle
  - VM → 2xx on own vestiging profile updates
  - the last-BM guard → 409
- Section tests: each moved section renders and saves via the same endpoint as before (bUnit with a mocked API client). `CompanyDetails.razor` no longer exists; the old URL 301s to `/werkgever/organisatie/profiel`.
- Localization parity for the moved strings.

## Success criteria
- d4 layout (tree + detail + drawer) works for BM; RM read-only; VM has no access.
- Exactly one invite UI.
- Bedrijfsprofiel, Koppelingen and Wervingsmateriaal are separate pages with tabs; no section is lost (checklist in the PR: every original section → its new home).

## Done → next
Push, open the PR, note its number. Continue with **`06-tokens-facturen.md`**. If anything is red, stop and report.

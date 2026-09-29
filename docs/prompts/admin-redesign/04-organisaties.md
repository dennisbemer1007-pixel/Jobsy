# 04 · Organisaties

> Read `00-README.md` first (§0, §IA). Builds on 01–03.

| | |
|---|---|
| Branch | `cursor/admin-redesign-4`, created from `cursor/admin-redesign-3` |
| PR | ONE PR into `acceptatie`, title `feat(admin): organisations tree with detail panel, regions & domains, requests inbox`. Body starts with `Stacked on #<PR of 03> (cursor/admin-redesign-3)` |
| Mockups | `ad-d3-organisaties.png` |

## 04.1 Today (verify)
- `CompaniesAdmin.razor` (now `/admin/organisaties`): flat list + "Nieuw bedrijf / intermediair (KVK)" form, links to users/vacancies/tokenlog per company.
- `GET api/admin/companies` returns **all** companies unpaged with `ParentCompanyId`, type, sales manager name, …
- `Company`: `ParentCompanyId` / `ChildCompanies` (vestigingen), `Type` (Employer/Intermediary), `KvkVerificationStatus` (Verified/Pending/Failed) + attempts.
- `Region` is **per organisation** (`OrganizationCompanyId`, `RegionCompany` links) via `RegionsController` (Enterprise/Admin). `RegionHost` = platform domains/CNAMEs (`RegionHostsController`, admin CRUD) on `CnamesAdmin.razor` (now `/admin/organisaties/regios`).
- Takeovers: `GET api/registration/takeovers` (admin sees all pending) + approve/reject endpoints (Enterprise/Branch/Admin). DTO includes **`RequesterEmail` in plain text**.
- **Never** `Include()` full `Company` graphs (PostGIS `Location` caused 500s; see comment in `RegionsController.List`). Project scalars only.

## 04.2 Bedrijven & vestigingen `/admin/organisaties` (`ad-d3`)
- **Master-detail** at ≥ 1024: table left, detail panel right (320 px, sticky). < 1024: row opens `AdminDrawer`.
- **API:** extend `GET api/admin/companies` with optional `q`, `type`, `region`, `status` (`active`, `kvk-failed`, `inactive` = no activity for `InactiveCompanyDays`), `page`, `pageSize` (default all → keep today's behaviour when no paging params, so existing callers don't break), and per-row counts: vestigingen, gebruikers, vacatures (active), tokensaldo. **Counts via grouped subqueries**, one round trip; no N+1.
- **Table** (`AdminDataTable`, tree mode): top-level rows = companies without parent; caret expands the vestigingen (children, indented with a connector line, `aria-expanded`). Columns: Organisatie (name 600 + muted "{type} · {n} vest.") · KvK (as the current list shows it) · Regio · Gebr. · Vac. · Tokens · Status (dot + text: Actief / KvK mislukt / Inactief {n} d / Overname / Niet live). Segment "Boom · Plat" (flat = all rows incl. vestigingen, sortable).
  - Filter bar: search (name/KvK), Type, Regio, Status. Note row above the table when there are open requests: "{n} registraties wachten op KvK-controle of overname. Aanvragen bekijken" (count from 02's todo service).
- **Detail panel:** icon tile + name (`h2`) + "{type} · klant sinds {mm-jjjj}". kv: KvK + "Geverifieerd"/"Mislukt" pill, Regio, Domein (matching `RegionHost` if any), Enterprisemanager (masked name), Tokensaldo (+ goodwill part if the token API gives it), Pakket (if known). A warn `AdminImpactNote` when a pending takeover targets this org/vestiging, with **one primary** "Overname beoordelen" (→ Aanvragen with it opened). Secondary: "Tokens geven" (existing `GrantTokensDialog`), "{n} gebruikers" (→ `/admin/gebruikers?companyId=`), ghost "Volledig openen" (→ existing per-company detail/edit if one exists; else omit).
- "Organisatie toevoegen" (primary header action) opens the **existing** KvK create form in a drawer (move the form into `Components/Admin/Sections/CompanyCreateSection.razor`, unchanged logic).

## 04.3 Regio's & domeinen `/admin/organisaties/regios`
- `AdminTabs`: **Domeinen** (the existing CNAME/`RegionHost` page body, unchanged logic, restyled as `AdminDataTable` + drawer for edit, keep `CnameSetupHelpPanel`) · **Regio's** (read-only list of all `Region`s across organisations: naam, organisatie, #vestigingen; via `GET api/regions` which already returns all for admin; editing stays with the enterprise manager, show "Beheerd door de organisatie" muted).
- Lead: "Domeinen bepalen branding en kaartfocus (bijv. westland.lobsy.nl). Regio's zijn groepen vestigingen binnen één organisatie."

## 04.4 Aanvragen `/admin/organisaties/aanvragen`
- One inbox with `AdminTabs`: **KvK-controle** (companies/registrations with `KvkVerificationStatus` Failed or Pending: name, KvK, attempts, last attempt; action "Opnieuw controleren" only if a retry endpoint/service method exists (`KvkVerificationRetryHostedService` logic, extract a `RetryNowAsync(companyId)` if it's inside the job) · **Overnames** (existing takeovers list; approve/reject with the existing endpoints; confirm dialog with reason for reject). Counts in tabs.
- **Privacy:** the requester e-mail is **masked** for admin in the takeover list (apply `PersonalDataMasker.MaskEmail` in the admin projection only, or mask in the UI if the same DTO serves enterprise managers; enterprise managers keep today's view). Full e-mail only with an active support grant.
- Retarget 02's todo hrefs (KvK, takeovers) to this page with `?tab=`. Flip `IsAvailable`.

## Tests
- `GET api/admin/companies`: no params = today's shape/order; filters; counts correct on a seeded tree; single query (no N+1: assert command count or use a query-count interceptor if the test project has one); no `Location` materialization.
- Takeover list for admin masks the e-mail; enterprise manager response unchanged.
- KvK retry (if built): calls the same service method the job uses.
- bUnit: tree expand/collapse with `aria-expanded`, Boom/Plat toggle, detail panel shows the takeover note + one primary, drawer on < 1024.
- Playwright: `/admin/organisaties` at 1440 shows table + panel without horizontal scroll.

## Success criteria
- Companies with their vestigingen in one tree; detail panel with the next action; create form reused.
- Domains and regions under one item with clear wording; region editing not duplicated.
- One Aanvragen inbox for KvK problems and takeovers; requester e-mail masked for admin.
- Build + tests green; PR body complete.

## Done → next
Push, open the PR, note its number. Continue with **`05-platforminstellingen.md`**.

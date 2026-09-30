# 02. Vacancy ownership + data separation: bureau owns the vacancy, link = opdrachtgever, public pin, 25 km, migration, money, metrics

Read `00-README.md` first. Branch `cursor/intermediair-2` from `cursor/intermediair-1` (or `-1b`).

> **Rules (same as README §0, repeated on purpose):**
> - Never merge, never deploy, never use rule `123` (`.cursor/rules/shortcut-123.mdc`).
> - Never push to `main` or `acceptatie`; push only this file's `cursor/intermediair-*` branch; no force-push.
> - ONE stacked PR into `acceptatie`.
> - Red tests or an unmet success criterion: push, open the PR as **draft**, stop and report. Don't start the next file.
> - Location only from KvK (no free location field anywhere), the opdrachtgever's identity never leaves the server in hidden mode, and server-side authorization per §R.

| | |
|---|---|
| Branch | `cursor/intermediair-2` |
| PR title | `feat(intermediair): bureau owns its vacancies, opdrachtgever via link, public pin + 25 km rule, data migration, bureau wallet and invoices` |
| PR body starts with | `Stacked on #<PR 01> (cursor/intermediair-1)` |
| Mockups | `im-d3-vacature-opdrachtgever-locatie.png`, `im-m2-vacature-locatie.png` (form step), `im-d2-opdrachtgevers.png` (vacancies table + Tokenverbruik only as data needs) |
| Split seam | **02a** = model + write paths + form step + old endpoint removal + migration service (02.2–02.8). **02b** = authorization/separation fixes, money, client performance, company page, registration no-conflict (02.9–02.13) |

## Goal
An intermediary vacancy belongs to the **bureau vestiging** (`Vacancy.CompanyId`) and points to its opdrachtgever through `IntermediaryClientId` (D11). `Vacancy.Location` is always the **public pin**: the bureau vestiging when hidden, the KvK werklocatie when real (D12). Hidden mode is allowed only ≤ 25 km (D5). With that, the wallet, invoices, agency subscription, applicant visibility, company pages and metrics are the bureau's by construction. Existing data is converted once, and bureau users lose every membership on opdrachtgever companies.

## 02.1 Today (verify first)
- Write paths that set the owner/location:
  - `VacanciesController.SaveDraftAsync` (~L1047, `vacancy.Location = company.Location`), with the intermediary block ~L886 (`IntermediaryCompanyId`, `ValidateEndClientKvk`)
  - `VacancyDraftCreationService`
  - `ExternalVacanciesController`
  - `VacancyCsvImportController` + `VacancyCsvSchema` (`vestiging_id`, `kvk_nummer`, `vestigingsnummer`, `toon_opdrachtgever_adres`; the importer only compares the KvK with the target company)
- `CreateVacancy.razor` intermediary fieldset (~L39–120): KvK number → "Zoek vestigingen" → select, a "Bekende opdrachtgever" select, and the checkbox `_hideClientAddressOnMap` ("Vervang bedrijfsnaam en adres door die van het intermediair", default true).
- Spends in `VacancyProductService` use `vacancy.CompanyId` (~L248/433/506/607/662); `HasActiveAgencySubscriptionAsync(vacancy.CompanyId)` (~L115/385); `FlexCommercialService.HasActiveAgencySubscriptionAsync` matches `s.CompanyId == companyId`.
- `TokensController` checkout (~L161–200) buys into `request.CompanyId` after an access check. `TokenPurchaseInvoiceService` (~L42) addresses the invoice to `checkout.CompanyId`.
- `MetricsQueryService.GetClientPerformanceAsync` (~L320) groups vacancies by `v.CompanyId` for the given company ids.
- `CompaniesController.EnsureActorMembershipAsync` adds `UserCompany` rows. `Company.KvkEstablishmentId` is **unique**. `CompanyRegistrationService` (~L254/295) turns an existing row into a takeover request to the current owner.
- `ITokenLedgerService.AllocateAsync(from, to, …)` exists (check its constraints before using it in 02.8).

## 02.2 Model (§D)
- `Vacancy`:
  - + `IntermediaryClientId?` (FK `Restrict`, index)
  - + `HoldReason?` (`VacancyHoldReason`) + `HoldSinceUtc?`
  - invariant check in the domain (`IntermediaryVacancyRules.EnsureConsistent`): client ⇔ intermediary org; `CompanyId` must be a vestiging of that org
- `Company.IsLegacyIntermediaryShell` (bool).
- **Discovery, search, matching and talent pool exclude vacancies with `HoldReason != null`.** Find the one place each of them filters on `Status == Active` and add the hold check there. No scattered checks: add `VacancyVisibility.IsPubliclyListed(v)` in Core and use it.

## 02.3 One location resolver (D5, D12, B1)
`Jobsy.Core/Rules/IntermediaryVacancyLocation.cs` (pure):
- `Resolve(bool showClientLocation, BureauVestiging owner, IntermediaryClient link)` → `GeoPoint`: real → `link.Location`; hidden → `owner.Location`. **Never** the link location in hidden mode.
- `DistanceKm(owner, link)`: reuse the existing haversine helper (`TravelReach` or the geo math it uses; check), rounded to 0.1.
- `CanHide(owner, link)` ⇔ `DistanceKm ≤ IntermediaryMaskRules.MaxMaskDistanceKm` (25).
- `DefaultOwner(bureau, link)`: nearest bureau vestiging with `DistanceKm ≤ 25`, else `bureau.PrimaryVestigingId` (D17).

**Every** write path calls `IntermediaryVacancyWriter.Apply(vacancy, bureau, link, ownerId, showClientLocation)` (Infrastructure). It is the only code that sets `CompanyId`, `IntermediaryCompanyId`, `IntermediaryClientId`, `ShowClientAddressOnMap` and `Location` for intermediary vacancies:
- Validates:
  - link Active, own bureau
  - owner ∈ bureau vestigingen with `LocationSource ∈ {Kvk, Pdok}` (else 409 `vestiging_location_unverified`, D24)
  - hidden ⇒ `CanHide` (else 409 `mask_radius_exceeded` with `{ distanceKm }`)
  - declaration present (409 `declaration_required`, D20)
- Category stays forced via `ResolveCategoryId`.

## 02.4 Write paths
- **Form/API** (`CreateVacancyRequest`, update, `SaveDraftAsync`):
  - replace the client company id with `intermediaryClientId`, `ownerVestigingId?` and `showClientLocation?`; `null` = the link default, but beyond 25 km `null` is rejected (D16)
  - for intermediary callers, `CompanyId` in the request is ignored (the writer sets it)
  - non-intermediary callers can't send `intermediaryClientId` (400)
- **Mode switch:** `PUT api/vacancies/{id}/map-mode { showClientLocation }` (own bureau). Free of charge (D15); real → hidden only ≤ 25 km. Recompute `Location`, refresh the discovery entry. The response carries `note` for real → hidden (D15 text).
- **Owner change** (another bureau vestiging) only while Draft/PendingApproval. Once published, the wallet that paid stays the owner.
- **`VacancyDraftCreationService` + CSV:**
  - `kvk_nummer` + `vestigingsnummer` must match an **Active link of this bureau**, else a row error "Opdrachtgever {kvk}/{vestiging} niet gevonden. Voeg die eerst toe via KvK." No KvK lookup, no auto-create.
  - `vestiging_id` = owner bureau vestiging (optional → `DefaultOwner`).
  - `toon_opdrachtgever_adres`: `ja` = real, `nee`/empty = the link default; hidden beyond 25 km → row error.
  - Update `VacancyCsvSchema` docs/help text and the CSV template.
- **External API** (`ExternalVacanciesController`):
  - a bureau API key identifies the bureau; the payload uses `intermediaryClientId` **or** `clientKvkNumber` + `clientVestigingsnummer` (active link), plus `showClientLocation?` and `ownerVestigingId?`
  - **Compatibility for one release:** a payload that still targets a legacy client company id resolves through `IntermediaryClient.LegacyCompanyId` (02.8) and gets the response header `Deprecation: true` + `Link` to the docs
  - update the OpenAPI docs
- **Remove** `POST api/companies/intermediary-clients/from-kvk` and `EnsureActorMembershipAsync`'s use there → `410 Gone { code: "use_api_intermediary_clients" }`. Remove `RegisterIntermediaryClientFromKvkAsync` from the web client.

## 02.5 Vacancy form step "Opdrachtgever & locatie" (d3, m2)
Replace the fieldset in `CreateVacancy.razor` (it moves to `/werkgever/vacatures/nieuw` with werkgever 01; build it as a component `Components/Werkgever/Intermediair/OpdrachtgeverLocatieStep.razor` so both routes use it). Only for Intermediary users.
- **Opdrachtgever** picker: Active links only (PendingReview shown disabled with "Wacht op controle door Lobsy"), each showing avatar initials, name, "KvK … · vestiging … · {gemeente}". Below it: link **"+ Nieuwe opdrachtgever via KvK"** (opens the 01 drawer; on success the new link is selected) and the hint "Alleen opdrachtgevers die je via KvK hebt toegevoegd".
- **Werklocatie** (read-only): address + "Bezoekadres uit KvK · opgehaald op {datum}" + lock "Uit KvK · niet te wijzigen". Under it: "Klopt dit adres niet? Kies een andere KvK-vestiging van deze opdrachtgever." (opens the drawer prefilled with the KvK number). **No input.**
- **Vestiging** select (only with > 1 bureau vestiging, D17), default `DefaultOwner`, each option with "{n} km van de werklocatie".
- **"Waar staat de vacature op de kaart?"**: two radio cards (`im-…` styles):
  - **"Toon op locatie van onze vestiging (standaard)"**: "Pin bij {bureau vestiging, adres}. De naam en het adres van de opdrachtgever blijven verborgen." + "Kandidaten zien: **{bureau}** · {plaats} · label 'Uitzendbureau'"
  - **"Toon op echte werklocatie"**: "Pin op het KvK-adres van de opdrachtgever in {plaats} ({n} km van je vestiging). Kandidaten zien de naam van de opdrachtgever." + "Kandidaten zien: **{opdrachtgever}** · {plaats} · 'via {bureau}'"
  - Beyond 25 km: the first card is disabled with the D16 text and nothing is preselected.
- **Travel note (info box):** "Reistijd rekenen we vanaf de plek op de kaart. Bij 'onze vestiging' staat erbij: 'reistijd tot de vestiging van het bureau, de werklocatie ligt in de regio {gemeente}'."
- **Candidate preview** (right column desktop, below on mobile): map with pin + 15-min ring from a sample point, and the job card as the candidate sees it (uses 03's `IntermediaryPublicIdentity` once it exists; in this file a local preview model with the same rules). The bureau-only grey dot marks the werklocatie ("Grijze stip = echte werklocatie, alleen zichtbaar voor jou").
- The old checkbox and its copy (B8) are removed.

## 02.6 Authorization without memberships (B3)
- For Intermediary users, the accessible companies (`CompanyAuthorizationService.GetAccessibleCompanyIdsAsync`) are **only** their bureau's Type-`Intermediary` companies. After 02.8 there are no other memberships. Add a defensive filter for the Intermediary role anyway: drop non-Intermediary company ids.
- Vacancy/applicant access (`CanManageVacancyAsync` and the applicant endpoints) works through `vacancy.CompanyId` (= bureau vestiging), so no special case is needed. Add explicit tests (02.14).
- `IntermediaryClient` rows are never reachable through company endpoints (`api/companies/*`), only through `api/intermediary/clients`.

## 02.7 Money (D7, B4)
- **Spends:** unchanged code (`vacancy.CompanyId` is now the bureau vestiging), with a test per reason (Publish, Highlight, PushBom, Extend, ContactUnlock) that the ledger row is on the bureau wallet (or the bureau org wallet when `TokensManagedByEnterprise`, like employers) and never on anything linked to the opdrachtgever.
- **Agency subscription:** `VacancyProductService.HasActiveAgencySubscriptionAsync` checks the owner vestiging **and** its organisation (`ParentCompanyId ?? Id`). The subscription row can sit on either, as admin creates it today (check `FlexCommercialService` / admin UI and say which it uses).
- **Checkout:** for Intermediary users `request.CompanyId` must be one of the bureau's companies (`IIntermediaryContext`), else 403. The invoice debtor follows `checkout.CompanyId` → the bureau. Test that the invoice name/KvK/address are the bureau's.
- **Token overview** for Intermediary users lists only bureau companies (automatic after 02.6; test).

## 02.8 One-shot data migration (D14, B3)
`IntermediaryOwnershipMigrationHostedService` (Infrastructure/Jobs; idempotent; each step in its own transaction; guarded by a `IntermediaryMigrationRun` with `Outcome = Completed` so it runs only until it has completed once; admin can re-run it from the Migratie tab, which is harmless when nothing is left).

1. **Links.** For every vacancy with `IntermediaryCompanyId != null && IntermediaryClientId == null`:
   - find/create `IntermediaryClient(bureau org, client.KvkEstablishmentId)` from the client company's snapshot: name, KvK, vestigingsnummer, address, and location via the 01 resolver when it is (0,0)/Unknown
   - set `LegacyCompanyId` = the client company id; `DeclarationAcceptedAtUtc = null` (Te doen "Bevestig je opdracht", D20); `Status = Active`
   - geo unresolved → keep the link, `GeoSource`/location empty-marker per 01 rules, and put the vacancies of that link in real mode on hold `WerklocatieUnknown`
2. **Re-own vacancies.**
   - Owner = the creator's Type-`Intermediary` company if it is in the bureau org, else the org's primary vestiging.
   - Set `CompanyId`, `IntermediaryClientId`, recompute `Location` (02.3).
   - Hidden and > 25 km → **stay hidden** (never reveal automatically) + hold `OutsideMaskRadius`.
   - Applications, likes, shares and impressions follow the vacancy id. **Token ledger history is not rewritten.**
3. **Memberships.** Delete every `UserCompany` row where the user's role is Intermediary and the company isn't a Type-`Intermediary` company of that user's bureau. A `User.CompanyId` that points to such a company is reset to the bureau's primary vestiging. Record user id → company id pairs in the report (ids only).
4. **Shell companies.** A company is a shell when, after step 3, it has no users and no memberships, all its historical memberships removed in step 3 belonged to intermediary users, and it has no vacancies left.
   - No token transactions, checkouts, invoices or agency subscriptions → delete it, with its auto-created salary tables (`WmlSalaryTableService.EnsureForCompanyAsync` created them) and any other dependent rows you find (FK scan).
   - Otherwise: `IsLegacyIntermediaryShell = true`, `KvkEstablishmentId = null` (frees the unique index), excluded from public company pages, discovery, admin default lists and `IsInUse`.
   - Balance > 0 → move it to the bureau wallet with `ITokenLedgerService.AllocateAsync` **only if** its rules allow shell → bureau without side effects. Otherwise leave it and list it.
5. **Real employers** that had intermediary memberships (they have other users): only the memberships go (step 3). Their own vacancies are untouched.
6. **Report** (`IntermediaryMigrationRun.SummaryJson`): counts plus id lists of
   - links created
   - vacancies re-owned / held (by reason)
   - memberships removed
   - shells deleted / retired
   - balances moved / not moved
   - **invoices issued in a client's name for a checkout paid by an intermediary user**: "handmatig crediteren en opnieuw factureren op naam van het bureau"; never altered automatically
   - external API keys that still target legacy client ids
7. Full discovery-index rebuild at the end.

Admin tab **Migratie** on `/admin/intermediairs`: the runs with counts and the lists (company/vacancy names resolved at read time), plus the button "Opnieuw uitvoeren" (idempotent, audit-logged).

## 02.9 Client performance (B5)
- New `MetricsQueryService.GetIntermediaryClientPerformanceAsync(Guid bureauOrgId, IReadOnlyCollection<Guid>? clientIds, string period)`: groups **only** vacancies with `IntermediaryCompanyId == bureauOrgId`, by `IntermediaryClientId`. Same KPIs as today (active, expiring, boosts, applications, …) plus `LiveHidden` / `LiveReal` (for the "Op de kaart" pill: Via onze vestiging / Echte werklocatie / Gemengd).
- `api/dashboard/client-performance` for Intermediary callers uses it (links = rows). The admin/platform view stays on the old method, which now naturally excludes bureau vacancies from client companies.

## 02.10 Company pages and discovery (B2, part 1)
- The discovery record's `CompanyId`/`KvkNumber`/`Vestigingsnummer` are the **owner bureau vestiging's** (automatic after 02.2; test).
- `CompanyPublicPage`: legacy shells → 404. A werkgever with the same KvK as a link lists **0** bureau vacancies (test). The bureau's page lists its own vacancies (both modes).
- The rest of the public display (names, badge, detail link, JSON-LD, reveal) is 03.

## 02.11 Registration and takeovers (D18)
- A werkgever registering a KvK vestiging that some bureau has as a link registers **normally**: no takeover request, no `IsInUse`. The link table is never consulted by registration. Test with a retired shell (02.8) on the same KvK number: normal registration.

## 02.12 Opdrachtgever counts in the 01 API
Wire the live vacancy/application counts and the 409 `client_has_live_vacancies` on archive. `PUT default-map-mode` rejects `showClientLocation = false` beyond 25 km **of every bureau vestiging** (409 `mask_radius_exceeded`).

## 02.13 Current intermediary pages
`IntermediaryDashboard.razor` (`/intermediary`) switches to 02.9's data (rows = opdrachtgevers). No redesign here; that is 06.

## Tests (02.14)
- `IntermediaryVacancyLocationTests`: hidden → owner location (never the link); real → link; 24.9 km allowed, 25.1 km rejected; `DefaultOwner` nearest ≤ 25 else primary.
- Writer: every error code; category forced; non-intermediary can't send `intermediaryClientId`; request `latitude/longitude/address` fields ignored or rejected by the DTO guard (extended to the new DTOs).
- Mode switch: free; discovery entry updated; real → hidden > 25 km → 409.
- CSV: unknown link → row error, no lookup (KvK stub asserts 0 calls); `toon_opdrachtgever_adres` rules. External API: link by id / by KvK; legacy company id → mapped + `Deprecation` header.
- **Separation (the B3 regression suite):**
  - bureau A can't see or act on bureau B's vacancies/applicants/links (404/403)
  - the werkgever with the same KvK sees 0 bureau vacancies/applicants, and the bureau can't reach that werkgever's vacancies, applicants or tokens
  - Intermediary accessible companies = bureau companies only
- Money: ledger rows on the bureau wallet for every spend reason; agency subscription on org or vestiging applies; checkout to a non-bureau company → 403; the invoice debtor is the bureau.
- Migration: seeded "before" data (two bureaus sharing a client, a real employer linked by the bug, a shell with a balance, a shell with an invoice, a (0,0) client, a hidden vacancy at 40 km) → the expected "after" state + report entries. A second run changes nothing.
- Metrics: only own vacancies, grouped per link; Gemengd.
- Registration with a retired shell / linked KvK: normal flow. bUnit: the form step (both cards, disabled beyond 25 km, no inputs for address).

## Success criteria
- `git grep` finds no write of `Vacancy.Location` for intermediary vacancies outside `IntermediaryVacancyWriter` (guard test via a Roslyn/reflection scan or a simple source grep test).
- After the migration no Intermediary user has a membership on a non-bureau company, and every bureau vacancy has `CompanyId` = a bureau vestiging and `IntermediaryClientId` set.
- Tokens, invoices and the agency subscription are the bureau's in every tested path.

Done → next: `03-publieke-weergave-privacy.md`.
